namespace AR.Iec61850.SampledValues.Profiles;

public sealed record SvFrameObservation
{
    public DateTimeOffset Timestamp { get; init; }
    public ushort EtherType { get; init; }
    public ushort AppId { get; init; }
    public string DestinationMac { get; init; } = string.Empty;
    public ushort? VlanId { get; init; }
    public byte? VlanPriority { get; init; }
    public string SvId { get; init; } = string.Empty;
    public string DataSetReference { get; init; } = string.Empty;
    public uint ConfigurationRevision { get; init; }
    public int PayloadBytesPerAsdu { get; init; }
    public IReadOnlyList<ushort> SampleCounts { get; init; } = Array.Empty<ushort>();
    public ushort? DeclaredSampleRate { get; init; }
    public ushort? DeclaredSampleMode { get; init; }
    public double? NominalFrequencyHz { get; init; }
    public SvFactProvenance NominalFrequencyProvenance { get; init; } = SvFactProvenance.TrustedContext;
    public IReadOnlyList<SvDatasetElementSignature> DataSetSignature { get; init; }
        = Array.Empty<SvDatasetElementSignature>();
    public SvFactProvenance DataSetSignatureProvenance { get; init; } = SvFactProvenance.SclDerived;

    public int AsduPerFrame => SampleCounts.Count;

    public void Validate()
    {
        if (SampleCounts.Count == 0)
            throw new ArgumentException("An SV frame observation requires at least one ASDU sample counter.");
        if (PayloadBytesPerAsdu <= 0)
            throw new ArgumentOutOfRangeException(nameof(PayloadBytesPerAsdu));
        if (NominalFrequencyHz.HasValue && NominalFrequencyProvenance == SvFactProvenance.Unavailable)
            throw new ArgumentException("A supplied nominal frequency requires explicit provenance.");
        if (DataSetSignature.Count > 0 && DataSetSignatureProvenance == SvFactProvenance.Unavailable)
            throw new ArgumentException("A supplied dataset signature requires explicit provenance.");
    }
}

public sealed class SvObservationAccumulator
{
    private const int RecentCounterHistory = 32;

    private readonly object _gate = new();
    private readonly List<SvFrameObservation> _observations = [];
    private readonly SvObservationWindowPolicy _policy;

    public SvObservationAccumulator(SvObservationWindowPolicy? policy = null)
    {
        _policy = policy ?? new SvObservationWindowPolicy();
        _policy.Validate();
    }

    public SvObservationWindowPolicy Policy => _policy;

    public int Count
    {
        get
        {
            lock (_gate)
                return _observations.Count;
        }
    }

    public void Add(SvFrameObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        observation.Validate();

        lock (_gate)
        {
            _observations.Add(observation);
            TrimUnsafe();
        }
    }

    public void Clear()
    {
        lock (_gate)
            _observations.Clear();
    }

    public IReadOnlyList<SvFrameObservation> Snapshot()
    {
        lock (_gate)
            return _observations.OrderBy(item => item.Timestamp).ToArray();
    }

    public SvObservedStreamFacts BuildFacts()
    {
        var ordered = Snapshot();
        if (ordered.Count == 0)
        {
            return new SvObservedStreamFacts
            {
                WindowQuality = SvObservationWindowQuality.Empty
            };
        }

        var diagnostics = new List<string>();
        var first = ordered[0];
        var last = ordered[^1];
        var duration = last.Timestamp - first.Timestamp;
        var durationSeconds = duration.TotalSeconds;

        double? framesPerSecond = null;
        double? samplesPerSecond = null;
        if (ordered.Count >= 2 && durationSeconds > 0)
        {
            framesPerSecond = (ordered.Count - 1) / durationSeconds;
            var averageAsduPerFrame = ordered.Average(item => item.AsduPerFrame);
            samplesPerSecond = framesPerSecond * averageAsduPerFrame;
        }
        else
        {
            diagnostics.Add("At least two timestamped frames are required to estimate stream rate.");
        }

        var counterSequence = AnalyzeCounters(ordered, diagnostics);
        var provenance = BuildProvenance(ordered, framesPerSecond, samplesPerSecond, counterSequence.ConfirmedWrap);

        var etherType = StableValue(ordered.Select(item => item.EtherType), "EtherType", diagnostics);
        var appId = StableValue(ordered.Select(item => item.AppId), "APPID", diagnostics);
        var destinationMac = StableString(ordered.Select(item => item.DestinationMac), "destination MAC", diagnostics);
        var vlanId = StableNullableValue(ordered.Select(item => item.VlanId), "VLAN ID", diagnostics);
        var vlanPriority = StableNullableValue(ordered.Select(item => item.VlanPriority), "VLAN priority", diagnostics);
        var svId = StableString(ordered.Select(item => item.SvId), "svID", diagnostics);
        var dataSetReference = StableString(ordered.Select(item => item.DataSetReference), "dataset reference", diagnostics);
        var configurationRevision = StableValue(ordered.Select(item => item.ConfigurationRevision), "confRev", diagnostics);
        var asduPerFrame = StableValue(ordered.Select(item => item.AsduPerFrame), "ASDU count", diagnostics);
        var payloadBytesPerAsdu = StableValue(ordered.Select(item => item.PayloadBytesPerAsdu), "payload length", diagnostics);
        var declaredSampleRate = StableNullableValue(ordered.Select(item => item.DeclaredSampleRate), "declared sample rate", diagnostics);
        var declaredSampleMode = StableNullableValue(ordered.Select(item => item.DeclaredSampleMode), "declared sample mode", diagnostics);
        var nominalFrequency = StableNullableValue(ordered.Select(item => item.NominalFrequencyHz), "nominal frequency", diagnostics);
        var dataSetSignature = StableSignature(ordered.Select(item => item.DataSetSignature), diagnostics);

        var minimumSatisfied =
            ordered.Count >= _policy.MinimumObservations &&
            duration >= _policy.MinimumDuration;

        if (!minimumSatisfied)
        {
            diagnostics.Add(
                $"Observation window is insufficient: {ordered.Count}/{_policy.MinimumObservations} frame(s), " +
                $"{duration.TotalMilliseconds:0.###}/{_policy.MinimumDuration.TotalMilliseconds:0.###} ms.");
        }

        var changedFields = diagnostics.Any(item => item.Contains("changed within", StringComparison.Ordinal));
        var quality = !minimumSatisfied
            ? SvObservationWindowQuality.Insufficient
            : counterSequence.HasAnomalies || changedFields
                ? SvObservationWindowQuality.Degraded
                : SvObservationWindowQuality.Ready;

        return new SvObservedStreamFacts
        {
            EtherType = etherType,
            AppId = appId,
            DestinationMac = destinationMac,
            VlanId = vlanId,
            VlanPriority = vlanPriority,
            SvId = svId,
            DataSetReference = dataSetReference,
            ConfigurationRevision = configurationRevision,
            AsduPerFrame = asduPerFrame,
            PayloadBytesPerAsdu = payloadBytesPerAsdu,
            ObservedFramesPerSecond = framesPerSecond,
            ObservedSamplesPerSecond = samplesPerSecond,
            ObservedCounterWrap = counterSequence.ConfirmedWrap,
            DeclaredSampleRate = declaredSampleRate,
            DeclaredSampleMode = declaredSampleMode,
            NominalFrequencyHz = nominalFrequency,
            DataSetSignature = dataSetSignature,
            ObservationCount = ordered.Count,
            FirstTimestamp = first.Timestamp,
            LastTimestamp = last.Timestamp,
            ObservationDuration = duration,
            WindowQuality = quality,
            CounterSequence = counterSequence,
            Provenance = provenance,
            Diagnostics = diagnostics
        };
    }

    private void TrimUnsafe()
    {
        var newestTimestamp = _observations.Max(item => item.Timestamp);
        var cutoff = newestTimestamp - _policy.MaximumAge;
        _observations.RemoveAll(item => item.Timestamp < cutoff);

        while (_observations.Count > _policy.MaximumObservations)
        {
            var oldestIndex = 0;
            for (var index = 1; index < _observations.Count; index++)
            {
                if (_observations[index].Timestamp < _observations[oldestIndex].Timestamp)
                    oldestIndex = index;
            }

            _observations.RemoveAt(oldestIndex);
        }
    }

    private SvCounterSequenceSummary AnalyzeCounters(
        IReadOnlyList<SvFrameObservation> observations,
        List<string> diagnostics)
    {
        var sampleCounts = observations.SelectMany(item => item.SampleCounts).ToArray();
        if (sampleCounts.Length < 2)
            return new SvCounterSequenceSummary();

        var continuous = 0;
        var gaps = 0;
        var missingSamples = 0;
        var duplicates = 0;
        var wraps = 0;
        var outOfOrder = 0;
        var resets = 0;
        var wrapCandidates = new Dictionary<int, int>();
        var recentQueue = new Queue<ushort>();
        var recentSet = new HashSet<ushort>();

        AddRecent(sampleCounts[0], recentQueue, recentSet);

        for (var index = 1; index < sampleCounts.Length; index++)
        {
            var previous = sampleCounts[index - 1];
            var current = sampleCounts[index];

            if (current == previous)
            {
                duplicates++;
                AddRecent(current, recentQueue, recentSet);
                continue;
            }

            if (current > previous)
            {
                var delta = current - previous;
                if (delta == 1)
                {
                    continuous++;
                }
                else
                {
                    gaps++;
                    missingSamples += delta - 1;
                }

                AddRecent(current, recentQueue, recentSet);
                continue;
            }

            var isStrongWrapCandidate =
                current <= _policy.WrapLowWatermark &&
                previous >= _policy.MinimumWrapPredecessor;

            if (isStrongWrapCandidate)
            {
                wraps++;
                var candidate = previous + 1;
                wrapCandidates[candidate] = wrapCandidates.GetValueOrDefault(candidate) + 1;
            }
            else if (recentSet.Contains(current) || previous - current <= _policy.ReorderTolerance)
            {
                outOfOrder++;
            }
            else
            {
                resets++;
            }

            AddRecent(current, recentQueue, recentSet);
        }

        int? confirmedWrap = null;
        if (wrapCandidates.Count == 1 && resets == 0 && outOfOrder == 0)
        {
            confirmedWrap = wrapCandidates.Keys.Single();
        }
        else if (wrapCandidates.Count > 1)
        {
            diagnostics.Add(
                $"Multiple sample-counter wrap candidates were observed: " +
                $"{string.Join(", ", wrapCandidates.Keys.Order())}.");
        }

        if (gaps > 0)
            diagnostics.Add($"Observed {gaps} forward sample-counter gap transition(s), estimating {missingSamples} missing sample(s).");
        if (duplicates > 0)
            diagnostics.Add($"Observed {duplicates} duplicate sample-counter transition(s).");
        if (outOfOrder > 0)
            diagnostics.Add($"Observed {outOfOrder} out-of-order sample-counter transition(s).");
        if (resets > 0)
            diagnostics.Add($"Observed {resets} sample-counter reset transition(s).");

        return new SvCounterSequenceSummary
        {
            ContinuousTransitions = continuous,
            GapTransitions = gaps,
            EstimatedMissingSamples = missingSamples,
            DuplicateTransitions = duplicates,
            WrapTransitions = wraps,
            OutOfOrderTransitions = outOfOrder,
            ResetTransitions = resets,
            ConfirmedWrap = confirmedWrap
        };
    }

    private static void AddRecent(
        ushort value,
        Queue<ushort> recentQueue,
        HashSet<ushort> recentSet)
    {
        recentQueue.Enqueue(value);
        recentSet.Add(value);

        while (recentQueue.Count > RecentCounterHistory)
        {
            var removed = recentQueue.Dequeue();
            if (!recentQueue.Contains(removed))
                recentSet.Remove(removed);
        }
    }

    private static IReadOnlyList<SvFactProvenanceEntry> BuildProvenance(
        IReadOnlyList<SvFrameObservation> observations,
        double? framesPerSecond,
        double? samplesPerSecond,
        int? counterWrap)
    {
        var first = observations[0];
        var dataSetProvenance = StableProvenance(
            observations.Select(item => item.DataSetSignatureProvenance));
        var nominalFrequencyProvenance = StableProvenance(
            observations.Select(item => item.NominalFrequencyProvenance));

        return
        [
            Wire(nameof(SvObservedStreamFacts.EtherType)),
            Wire(nameof(SvObservedStreamFacts.AppId)),
            Wire(nameof(SvObservedStreamFacts.DestinationMac)),
            Wire(nameof(SvObservedStreamFacts.VlanId)),
            Wire(nameof(SvObservedStreamFacts.VlanPriority)),
            Wire(nameof(SvObservedStreamFacts.SvId)),
            Wire(nameof(SvObservedStreamFacts.DataSetReference)),
            Wire(nameof(SvObservedStreamFacts.ConfigurationRevision)),
            Wire(nameof(SvObservedStreamFacts.AsduPerFrame)),
            Wire(nameof(SvObservedStreamFacts.PayloadBytesPerAsdu)),
            Wire(nameof(SvObservedStreamFacts.DeclaredSampleRate)),
            Wire(nameof(SvObservedStreamFacts.DeclaredSampleMode)),
            new SvFactProvenanceEntry(
                nameof(SvObservedStreamFacts.ObservedFramesPerSecond),
                framesPerSecond.HasValue ? SvFactProvenance.CaptureCalculated : SvFactProvenance.Unavailable,
                "Calculated from capture timestamps and frame count."),
            new SvFactProvenanceEntry(
                nameof(SvObservedStreamFacts.ObservedSamplesPerSecond),
                samplesPerSecond.HasValue ? SvFactProvenance.CaptureCalculated : SvFactProvenance.Unavailable,
                "Calculated from capture timestamps and observed ASDU packing."),
            new SvFactProvenanceEntry(
                nameof(SvObservedStreamFacts.ObservedCounterWrap),
                counterWrap.HasValue ? SvFactProvenance.CaptureCalculated : SvFactProvenance.Unavailable,
                "Inferred from ordered sample-counter transitions."),
            new SvFactProvenanceEntry(
                nameof(SvObservedStreamFacts.NominalFrequencyHz),
                first.NominalFrequencyHz.HasValue ? nominalFrequencyProvenance : SvFactProvenance.Unavailable,
                "Supplied by trusted configuration or engineering context; not inferred from one frame."),
            new SvFactProvenanceEntry(
                nameof(SvObservedStreamFacts.DataSetSignature),
                first.DataSetSignature.Count > 0 ? dataSetProvenance : SvFactProvenance.Unavailable,
                "Derived from the bound dataset layout rather than raw payload bytes alone."),
            new SvFactProvenanceEntry(
                nameof(SvObservedStreamFacts.ObservationCount),
                SvFactProvenance.CaptureCalculated,
                "Calculated from the bounded observation window.")
        ];
    }

    private static SvFactProvenanceEntry Wire(string field)
        => new(field, SvFactProvenance.WireObserved, "Observed directly from decoded Ethernet or SV fields.");

    private static SvFactProvenance StableProvenance(IEnumerable<SvFactProvenance> values)
    {
        var distinct = values.Distinct().ToArray();
        return distinct.Length == 1 ? distinct[0] : SvFactProvenance.Unavailable;
    }

    private static T? StableValue<T>(
        IEnumerable<T> values,
        string field,
        List<string> diagnostics)
        where T : struct
    {
        var distinct = values.Distinct().ToArray();
        if (distinct.Length == 1)
            return distinct[0];

        diagnostics.Add($"Observed {field} changed within the analysis window.");
        return null;
    }

    private static T? StableNullableValue<T>(
        IEnumerable<T?> values,
        string field,
        List<string> diagnostics)
        where T : struct
    {
        var distinct = values.Distinct().ToArray();
        if (distinct.Length == 1)
            return distinct[0];

        diagnostics.Add($"Observed {field} changed within the analysis window.");
        return null;
    }

    private static string StableString(
        IEnumerable<string> values,
        string field,
        List<string> diagnostics)
    {
        var distinct = values
            .Select(value => value ?? string.Empty)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (distinct.Length == 1)
            return distinct[0];

        diagnostics.Add($"Observed {field} changed within the analysis window.");
        return string.Empty;
    }

    private static IReadOnlyList<SvDatasetElementSignature> StableSignature(
        IEnumerable<IReadOnlyList<SvDatasetElementSignature>> signatures,
        List<string> diagnostics)
    {
        var materialized = signatures.ToArray();
        var normalized = materialized
            .Select(signature => signature.Select(ToKey).ToArray())
            .ToArray();

        if (normalized.Length == 0 || normalized.All(signature => signature.Length == 0))
            return Array.Empty<SvDatasetElementSignature>();

        var first = normalized[0];
        if (normalized.All(signature => signature.SequenceEqual(first, StringComparer.Ordinal)))
            return materialized[0].ToArray();

        diagnostics.Add("Observed dataset signature changed within the analysis window.");
        return Array.Empty<SvDatasetElementSignature>();
    }

    private static string ToKey(SvDatasetElementSignature element)
        => $"{element.NormalizedBType}|{element.NormalizedCdc}|{element.IsQuality}|{element.IsTimestamp}";
}
