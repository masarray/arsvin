using System.Buffers.Binary;
using System.Globalization;
using AR.Iec61850.Mms;
using AR.Iec61850.SampledValues;
using AR.Iec61850.SampledValues.Measurements;
using AR.Iec61850.SampledValues.Profiles;
using AR.Iec61850.Scl;

namespace ARSVIN.Subscriber.Models;

internal sealed class SvStreamRuntime
{
    private const int MaxWaveformPoints = 640;
    private static readonly TimeSpan CurrentHealthWindow = TimeSpan.FromSeconds(2);

    private readonly object _gate = new();
    private readonly Queue<WaveformPoint> _waveform = new(MaxWaveformPoints + 8);
    private readonly List<string> _diagnostics = new();
    private readonly SvSampleCounterTracker _counterTracker = new();
    private DateTimeOffset? _firstSeen;
    private DateTimeOffset? _lastSeen;
    private DateTimeOffset? _lastSequenceWarningAt;
    private DateTimeOffset? _lastOutOfOrderAt;
    private DateTimeOffset? _lastPayloadIssueAt;
    private DateTimeOffset? _lastSclMismatchAt;
    private long _frameCount;
    private long _asduCount;
    private double _gapTotalMs;
    private double _maxGapMs;
    private int _gapSamples;
    private int _sequenceGaps;
    private int _duplicates;
    private int _outOfOrder;
    private int _payloadIssues;
    private int _sclMismatches;
    private int _waveformIndex;
    private int _qualityGood;
    private int _qualityNonZero;
    private string _lastHealthDetail = string.Empty;
    private string _lastSequenceDetail = string.Empty;
    private string _layoutBinding = string.Empty;
    private string _scalingSummary = "Raw counts";
    private string _scalingReason = "Engineering scaling has not been resolved.";
    private IReadOnlyList<DecodedValueRow> _decodedValues = Array.Empty<DecodedValueRow>();
    private SvStreamObservationSnapshot? _observationSnapshot;
    private SvTimebaseResolution _timebase = new();
    private uint? _lastConfigurationRevision;

    public SvStreamRuntime(string key)
    {
        Key = key;
    }

    public string Key { get; }
    public ushort AppId { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public string Destination { get; private set; } = string.Empty;
    public ushort? VlanId { get; private set; }
    public byte? VlanPriority { get; private set; }
    public string SvId { get; private set; } = string.Empty;
    public string DataSet { get; private set; } = string.Empty;
    public uint? ConfRev { get; private set; }
    public int NofAsdu { get; private set; }
    public ushort? SampleRate { get; private set; }
    public ushort? SampleMode { get; private set; }
    public byte? SmpSynch { get; private set; }
    public bool IsBoundToScl { get; private set; }
    public string ControlBlockReference { get; private set; } = string.Empty;
    public string LayoutBinding { get; private set; } = string.Empty;

    public void Observe(
        DateTimeOffset timestamp,
        SampledValuesFrame frame,
        SampledValuesPublisherProfile? profile,
        SvStreamObservationSnapshot observationSnapshot)
    {
        var asdus = frame.Pdu.Asdus;
        var first = asdus.FirstOrDefault();
        var diagnostics = new List<string>();
        var latestRows = new List<DecodedValueRow>();
        var points = new List<WaveformPoint>();
        var qualityGood = 0;
        var qualityNonZero = 0;
        var scalingSummary = "Raw counts";
        var scalingReason = "Engineering scaling has not been resolved.";
        var fixedLegacyLayout = false;

        if (asdus.Count == 0)
            diagnostics.Add("SV frame contains no ASDU.");

        var layoutBinding = string.Empty;
        if (profile is not null)
        {
            layoutBinding = $"SCL: {profile.Stream.ControlBlockReference}";
            ValidateAgainstScl(frame, asdus, profile, diagnostics);
            foreach (var asdu in asdus)
            {
                var rawRows = DecodePayload(asdu, profile.PayloadLayout, diagnostics).ToArray();
                var rows = ApplyEngineeringScaling(
                    rawRows,
                    isSclBound: true,
                    asdu,
                    observationSnapshot,
                    out var localFixedLayout,
                    out var localSummary,
                    out var localReason);
                fixedLegacyLayout |= localFixedLayout;
                scalingSummary = localSummary;
                scalingReason = localReason;

                if (latestRows.Count == 0)
                    latestRows.AddRange(rows);

                points.Add(BuildWaveformPoint(asdu.SampleCount, rows, scalingSummary));
                CountQuality(rows, ref qualityGood, ref qualityNonZero);
            }
        }
        else
        {
            foreach (var asdu in asdus)
            {
                if (!TryDecodeAutoPayload(asdu, diagnostics, out var rawRows, out var binding))
                    continue;

                layoutBinding = binding;
                var rows = ApplyEngineeringScaling(
                    rawRows,
                    isSclBound: false,
                    asdu,
                    observationSnapshot,
                    out var localFixedLayout,
                    out var localSummary,
                    out var localReason);
                fixedLegacyLayout |= localFixedLayout;
                scalingSummary = localSummary;
                scalingReason = localReason;

                if (latestRows.Count == 0)
                    latestRows.AddRange(rows);

                points.Add(BuildWaveformPoint(asdu.SampleCount, rows, scalingSummary));
                CountQuality(rows, ref qualityGood, ref qualityNonZero);
            }

            if (latestRows.Count == 0)
                diagnostics.Add("No SCL binding and payload layout is unknown. Import SCL or inspect raw payload bytes.");
            else
                diagnostics.Add($"{layoutBinding}. SCL not loaded; channel names and scaling confidence are shown explicitly.");
        }

        var resolvedTimebase = SvTimebaseResolver.Resolve(new SvTimebaseEvidence
        {
            DeclaredSampleRate = first?.SampleRate,
            DeclaredSampleMode = first?.SampleMode,
            ObservedSamplesPerSecond = observationSnapshot.Facts.ObservedSamplesPerSecond,
            IsFixedLegacyProtectionLayout = fixedLegacyLayout
        });

        lock (_gate)
        {
            if (_firstSeen is null)
                _firstSeen = timestamp;

            if (_lastSeen is { } previous)
            {
                var gap = (timestamp - previous).TotalMilliseconds;
                if (gap >= 0)
                {
                    _gapTotalMs += gap;
                    _gapSamples++;
                    _maxGapMs = Math.Max(_maxGapMs, gap);
                }
            }

            _lastSeen = timestamp;
            _frameCount++;
            _asduCount += asdus.Count;
            AppId = frame.AppId;
            Source = frame.Source.ToString();
            Destination = frame.Destination.ToString();
            VlanId = frame.Vlan?.VlanId;
            VlanPriority = frame.Vlan?.PriorityCodePoint;
            NofAsdu = asdus.Count;
            IsBoundToScl = profile is not null;
            ControlBlockReference = profile?.Stream.ControlBlockReference ?? string.Empty;
            LayoutBinding = layoutBinding;
            _layoutBinding = layoutBinding;
            _scalingSummary = scalingSummary;
            _scalingReason = scalingReason;

            if (resolvedTimebase.IsResolved || !_timebase.IsResolved)
                _timebase = resolvedTimebase;

            var configurationRevisionChanged = first is not null &&
                                               _lastConfigurationRevision.HasValue &&
                                               first.ConfigurationRevision != _lastConfigurationRevision.Value;

            if (first is not null)
            {
                SvId = first.SvId;
                DataSet = first.DataSetReference;
                ConfRev = first.ConfigurationRevision;
                SampleRate = first.SampleRate;
                SampleMode = first.SampleMode;
                SmpSynch = first.SampleSynchronization;
                _lastConfigurationRevision = first.ConfigurationRevision;
            }

            for (var index = 0; index < asdus.Count; index++)
            {
                var transition = _counterTracker.Observe(
                    asdus[index].SampleCount,
                    _timebase.SampleCounterWrap,
                    restartHint: configurationRevisionChanged && index == 0);
                RecordTransition(transition, timestamp);
            }

            foreach (var point in points)
            {
                _waveform.Enqueue(point);
                while (_waveform.Count > MaxWaveformPoints)
                    _waveform.Dequeue();
            }

            _qualityGood += qualityGood;
            _qualityNonZero += qualityNonZero;

            var hasPayloadIssue = asdus.Count == 0 || diagnostics.Any(x =>
                x.Contains("payload", StringComparison.OrdinalIgnoreCase) ||
                x.Contains("decode", StringComparison.OrdinalIgnoreCase));
            if (hasPayloadIssue)
            {
                _payloadIssues++;
                _lastPayloadIssueAt = timestamp;
            }

            var hasSclMismatch = diagnostics.Any(x =>
                x.Contains("mismatch", StringComparison.OrdinalIgnoreCase) ||
                x.Contains("differs", StringComparison.OrdinalIgnoreCase));
            if (hasSclMismatch)
            {
                _sclMismatches++;
                _lastSclMismatchAt = timestamp;
            }

            _diagnostics.Clear();
            _diagnostics.AddRange(diagnostics.Take(12));
            _decodedValues = latestRows.ToArray();
            _observationSnapshot = observationSnapshot;
            _lastHealthDetail = diagnostics.Count == 0 ? "SV stream is stable." : diagnostics[0];
        }
    }

    public SvStreamSnapshot Snapshot()
    {
        lock (_gate)
        {
            var duration = _firstSeen.HasValue && _lastSeen.HasValue
                ? Math.Max(0.001, (_lastSeen.Value - _firstSeen.Value).TotalSeconds)
                : 0.001;
            var fps = _frameCount / duration;
            var referenceTime = _lastSeen ?? DateTimeOffset.Now;
            var (health, healthDetail) = ResolveCurrentHealth(referenceTime);
            var allPoints = _waveform.ToArray();
            var visiblePoints = BuildLockedTwoCycleWindow(allPoints, _timebase.SamplesPerCycle);
            var phasors = ComputePhasors(visiblePoints, _timebase.SamplesPerCycle).ToArray();
            var observationFacts = _observationSnapshot?.Facts;

            return new SvStreamSnapshot
            {
                Key = Key,
                Health = health,
                HealthDetail = healthDetail,
                AppId = AppId,
                Source = Source,
                Destination = Destination,
                VlanId = VlanId,
                VlanPriority = VlanPriority,
                SvId = SvId,
                DataSet = DataSet,
                ConfRev = ConfRev,
                NofAsdu = NofAsdu,
                LastSmpCnt = _counterTracker.Last,
                SampleRate = SampleRate,
                SampleMode = SampleMode,
                SmpSynch = SmpSynch,
                NominalFrequencyHz = _timebase.NominalFrequencyHz,
                SamplesPerCycle = _timebase.SamplesPerCycle,
                ResolvedCounterWrap = _timebase.SampleCounterWrap,
                TimebaseSource = _timebase.Source,
                TimebaseReason = _timebase.Reason,
                ScalingSummary = _scalingSummary,
                ScalingReason = _scalingReason,
                FrameCount = _frameCount,
                AsduCount = _asduCount,
                ActualFps = fps,
                AverageFrameGapMilliseconds = _gapSamples == 0 ? 0 : _gapTotalMs / _gapSamples,
                MaxFrameGapMilliseconds = _maxGapMs,
                SequenceGapCount = _sequenceGaps,
                DuplicateCount = _duplicates,
                OutOfOrderCount = _outOfOrder,
                PayloadIssueCount = _payloadIssues,
                SclMismatchCount = _sclMismatches,
                IsBoundToScl = IsBoundToScl,
                ControlBlockReference = ControlBlockReference,
                LayoutBinding = LayoutBinding,
                LastSeen = _lastSeen?.ToLocalTime().ToString("HH:mm:ss.fff") ?? "-",
                Diagnostics = _diagnostics.ToArray(),
                Values = _decodedValues,
                WaveformPoints = visiblePoints,
                Phasors = phasors,
                CursorSummary = BuildCursorSummary(visiblePoints),
                QualitySummary = _qualityGood + _qualityNonZero == 0
                    ? "Quality not decoded"
                    : $"Quality good {_qualityGood:N0}, non-zero {_qualityNonZero:N0}",
                ObservationInputKinds = _observationSnapshot?.InputKinds ?? Array.Empty<SvObservationInputKind>(),
                ObservationWindowFrames = observationFacts?.ObservationCount ?? 0,
                ObservationWindowSamples = observationFacts is null
                    ? 0
                    : observationFacts.ObservationCount * Math.Max(1, observationFacts.AsduPerFrame ?? 1),
                ObservationWindowDurationSeconds = ResolveObservationDuration(observationFacts),
                ObservedFramesPerSecond = observationFacts?.ObservedFramesPerSecond,
                ObservedSamplesPerSecond = observationFacts?.ObservedSamplesPerSecond,
                ObservedCounterWrap = observationFacts?.ObservedCounterWrap,
                IsWaveformWindowReady = _timebase.SamplesPerCycle is > 0 &&
                                        visiblePoints.Length >= _timebase.SamplesPerCycle.Value * 2,
                ProfileDetection = _observationSnapshot?.ProfileDetection,
                ConfigurationComparison = _observationSnapshot?.ConfigurationComparison,
                ObservationDiagnostics = _observationSnapshot?.Diagnostics ?? Array.Empty<string>(),
                FactProvenance = observationFacts?.Provenance
                    ?? new Dictionary<string, SvFactSource>(StringComparer.Ordinal)
            };
        }
    }

    private static double ResolveObservationDuration(SvObservedStreamFacts? facts)
    {
        if (facts?.FirstTimestamp is not { } first || facts.LastTimestamp is not { } last)
            return 0;
        return Math.Max(0, (last - first).TotalSeconds);
    }

    private (string Health, string Detail) ResolveCurrentHealth(DateTimeOffset referenceTime)
    {
        if (IsRecent(_lastPayloadIssueAt, referenceTime))
            return ("BAD", _lastHealthDetail.Length == 0 ? "Recent SV payload or decode failure." : _lastHealthDetail);
        if (IsRecent(_lastOutOfOrderAt, referenceTime))
            return ("BAD", _lastSequenceDetail.Length == 0 ? "Recent out-of-order smpCnt transition." : _lastSequenceDetail);
        if (IsRecent(_lastSclMismatchAt, referenceTime))
            return ("WARN", _lastHealthDetail.Length == 0 ? "Observed traffic differs from the SCL expectation." : _lastHealthDetail);
        if (IsRecent(_lastSequenceWarningAt, referenceTime))
            return ("WARN", _lastSequenceDetail.Length == 0 ? "Recent smpCnt gap or duplicate." : _lastSequenceDetail);
        if (!IsBoundToScl)
            return ("WARN", "Stream is not SCL-bound; layout, channel names or scaling may be inferred.");
        if (_scalingSummary.Equals("Raw counts", StringComparison.OrdinalIgnoreCase))
            return ("WARN", "Payload is decoded, but engineering scaling is not proven; waveform remains in raw counts.");
        return ("GOOD", "SV stream is stable, SCL-bound and displayed with evidence-backed engineering scaling.");
    }

    private static bool IsRecent(DateTimeOffset? eventTime, DateTimeOffset referenceTime)
        => eventTime.HasValue && referenceTime >= eventTime.Value &&
           referenceTime - eventTime.Value <= CurrentHealthWindow;

    private void RecordTransition(SvSampleCounterTransition transition, DateTimeOffset timestamp)
    {
        switch (transition.Kind)
        {
            case SvSampleCounterTransitionKind.Gap:
                _sequenceGaps++;
                _lastSequenceWarningAt = timestamp;
                _lastSequenceDetail = transition.Detail;
                break;
            case SvSampleCounterTransitionKind.Duplicate:
                _duplicates++;
                _lastSequenceWarningAt = timestamp;
                _lastSequenceDetail = transition.Detail;
                break;
            case SvSampleCounterTransitionKind.OutOfOrder:
                _outOfOrder++;
                _lastOutOfOrderAt = timestamp;
                _lastSequenceDetail = transition.Detail;
                break;
            case SvSampleCounterTransitionKind.Restart:
                _lastSequenceDetail = transition.Detail;
                break;
        }
    }

    private static DecodedValueRow[] ApplyEngineeringScaling(
        IReadOnlyList<DecodedValueRow> rawRows,
        bool isSclBound,
        SampledValueAsdu asdu,
        SvStreamObservationSnapshot observation,
        out bool fixedLegacyLayout,
        out string scalingSummary,
        out string scalingReason)
    {
        var analogRows = rawRows
            .Where(row => row.NumericValue.HasValue && !string.IsNullOrWhiteSpace(ClassifyAnalogChannel(row.Signal, row.Kind)))
            .ToArray();
        fixedLegacyLayout = analogRows.Length == 8 && asdu.SamplePayload.Length == 64;
        var scaled = new List<DecodedValueRow>(rawRows.Count);
        var resolvedScales = new List<SvEngineeringScale>();

        foreach (var row in rawRows)
        {
            if (!row.NumericValue.HasValue)
            {
                scaled.Add(row);
                continue;
            }

            var channel = ClassifyAnalogChannel(row.Signal, row.Kind);
            var scale = SvEngineeringScaleResolver.Resolve(new SvEngineeringScaleEvidence
            {
                Channel = channel,
                Kind = row.Kind,
                IsSclBound = isSclBound,
                IsFixedFourCurrentFourVoltageLayout = fixedLegacyLayout,
                AnalogChannelCount = analogRows.Length,
                PayloadBytesPerAsdu = asdu.SamplePayload.Length,
                DeclaredSampleRate = asdu.SampleRate,
                DeclaredSampleMode = asdu.SampleMode,
                ObservedSamplesPerSecond = observation.Facts.ObservedSamplesPerSecond
            });
            resolvedScales.Add(scale);

            scaled.Add(new DecodedValueRow
            {
                Index = row.Index,
                Signal = row.Signal,
                Kind = row.Kind,
                Value = row.Value,
                Raw = row.Raw,
                NumericValue = row.NumericValue,
                EngineeringValue = scale.HasEngineeringUnit ? scale.Apply(row.NumericValue.Value) : null,
                EngineeringUnit = scale.HasEngineeringUnit ? scale.Unit : string.Empty,
                ScalingSource = scale.Source,
                ScalingConfidence = scale.Confidence,
                ScalingReason = scale.Reason
            });
        }

        var engineeringScale = resolvedScales.FirstOrDefault(scale => scale.HasEngineeringUnit);
        if (engineeringScale is null)
        {
            scalingSummary = "Raw counts";
            scalingReason = resolvedScales.FirstOrDefault()?.Reason ?? "No numeric analog channels were available for scaling.";
        }
        else
        {
            scalingSummary = engineeringScale.Confidence == SvEngineeringScaleConfidence.SclBacked
                ? "Engineering A/V · SCL-backed 9-2LE-style"
                : "Engineering A/V · inferred 9-2LE-style";
            scalingReason = engineeringScale.Reason;
        }

        return scaled.ToArray();
    }

    private static bool TryDecodeAutoPayload(
        SampledValueAsdu asdu,
        ICollection<string> diagnostics,
        out DecodedValueRow[] rows,
        out string layoutBinding)
    {
        rows = Array.Empty<DecodedValueRow>();
        layoutBinding = string.Empty;

        var payload = asdu.SamplePayload.AsSpan();
        if (payload.Length < 8 || payload.Length % 8 != 0)
        {
            diagnostics.Add($"Auto payload decode skipped: expected value+quality pairs of 8 bytes, got {payload.Length} byte(s).");
            return false;
        }

        var pairCount = payload.Length / 8;
        var channels = ResolveAutoAnalogChannels(pairCount);
        if (channels.Count == 0)
        {
            diagnostics.Add($"Auto payload decode skipped: unsupported fixed pair count {pairCount}.");
            return false;
        }

        var result = new List<DecodedValueRow>(pairCount * 2);
        for (var pair = 0; pair < pairCount; pair++)
        {
            var offset = pair * 8;
            var channel = channels[pair];
            var valueBytes = payload.Slice(offset, 4);
            var qualityBytes = payload.Slice(offset + 4, 4);
            var value = BinaryPrimitives.ReadInt32BigEndian(valueBytes);
            var signalReference = BuildAutoSignalReference(channel);

            result.Add(new DecodedValueRow
            {
                Index = result.Count + 1,
                Signal = signalReference,
                Kind = channel.StartsWith('V') ? "Voltage" : "Current",
                Value = value.ToString(CultureInfo.InvariantCulture),
                Raw = Convert.ToHexString(valueBytes),
                NumericValue = value
            });

            result.Add(new DecodedValueRow
            {
                Index = result.Count + 1,
                Signal = signalReference + ".q",
                Kind = "Quality",
                Value = Convert.ToHexString(qualityBytes),
                Raw = Convert.ToHexString(qualityBytes),
                NumericValue = null
            });
        }

        rows = result.ToArray();
        layoutBinding = pairCount == 8
            ? "Auto fixed 4I+4V value-quality layout"
            : $"Auto fixed value+quality layout ({pairCount} analog channels)";
        return true;
    }

    private static IReadOnlyList<string> ResolveAutoAnalogChannels(int pairCount)
    {
        return pairCount switch
        {
            3 => new[] { "Ia", "Ib", "Ic" },
            8 => new[] { "Ia", "Ib", "Ic", "In", "Va", "Vb", "Vc", "Vn" },
            12 => new[] { "Ia", "Ib", "Ic", "I4", "I5", "I6", "I7", "I8", "Va", "Vb", "Vc", "Vn" },
            15 => new[] { "Ia", "Ib", "Ic", "I4", "I5", "I6", "I7", "I8", "I9", "Va", "Vb", "Vc", "V4", "V5", "V6" },
            16 => new[] { "Ia", "Ib", "Ic", "I4", "I5", "I6", "I7", "I8", "I9", "I10", "I11", "I12", "Va", "Vb", "Vc", "Vn" },
            20 => new[] { "Ia", "Ib", "Ic", "I4", "I5", "I6", "I7", "I8", "I9", "I10", "I11", "I12", "Va", "Vb", "Vc", "Vn", "V5", "V6", "V7", "V8" },
            _ => Array.Empty<string>()
        };
    }

    private static string BuildAutoSignalReference(string channel)
    {
        return channel switch
        {
            "Ia" => "TCTR1/AmpSv.instMag.i",
            "Ib" => "TCTR2/AmpSv.instMag.i",
            "Ic" => "TCTR3/AmpSv.instMag.i",
            "In" => "TCTR4/AmpSv.instMag.i",
            "Va" => "TVTR1/VolSv.instMag.i",
            "Vb" => "TVTR2/VolSv.instMag.i",
            "Vc" => "TVTR3/VolSv.instMag.i",
            "Vn" => "TVTR4/VolSv.instMag.i",
            _ when channel.StartsWith('V') => channel + "/VolSv.instMag.i",
            _ => channel + "/AmpSv.instMag.i"
        };
    }

    private WaveformPoint BuildWaveformPoint(ushort? smpCnt, IEnumerable<DecodedValueRow> rows, string scalingSummary)
    {
        var rowArray = rows.ToArray();
        var currentUnit = rowArray.FirstOrDefault(row =>
            row.HasEngineeringValue && SvEngineeringScaleResolver.ResolveDomain(row.Signal, row.Kind) == SvMeasurementDomain.Current)?.EngineeringUnit ?? "count";
        var voltageUnit = rowArray.FirstOrDefault(row =>
            row.HasEngineeringValue && SvEngineeringScaleResolver.ResolveDomain(row.Signal, row.Kind) == SvMeasurementDomain.Voltage)?.EngineeringUnit ?? "count";
        var point = new WaveformPoint
        {
            Index = _waveformIndex++,
            SampleCount = smpCnt,
            CurrentUnit = currentUnit,
            VoltageUnit = voltageUnit,
            ScalingSummary = scalingSummary
        };

        foreach (var row in rowArray)
        {
            var value = row.EngineeringValue ?? row.NumericValue;
            if (!value.HasValue)
                continue;

            var channel = ClassifyAnalogChannel(row.Signal, row.Kind);
            switch (channel)
            {
                case "Ia": point.Ia = value.Value; break;
                case "Ib": point.Ib = value.Value; break;
                case "Ic": point.Ic = value.Value; break;
                case "In": point.In = value.Value; break;
                case "Va": point.Va = value.Value; break;
                case "Vb": point.Vb = value.Value; break;
                case "Vc": point.Vc = value.Value; break;
                case "Vn": point.Vn = value.Value; break;
            }
        }

        return point;
    }

    private static string ClassifyAnalogChannel(string reference, string kind)
    {
        if (kind.Contains("Quality", StringComparison.OrdinalIgnoreCase) ||
            kind.Contains("Timestamp", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        var text = reference.Replace('$', '.').Replace('/', '.').ToLowerInvariant();
        if (text is "ia" or "amp.ia" or "current.ia") return "Ia";
        if (text is "ib" or "amp.ib" or "current.ib") return "Ib";
        if (text is "ic" or "amp.ic" or "current.ic") return "Ic";
        if (text is "in" or "amp.in" or "current.in") return "In";
        if (text is "va" or "vol.va" or "voltage.va") return "Va";
        if (text is "vb" or "vol.vb" or "voltage.vb") return "Vb";
        if (text is "vc" or "vol.vc" or "voltage.vc") return "Vc";
        if (text is "vn" or "vol.vn" or "voltage.vn") return "Vn";

        var isVoltage = text.Contains("tvtr", StringComparison.Ordinal) ||
                        text.Contains("vol", StringComparison.Ordinal) ||
                        text.Contains("voltage", StringComparison.Ordinal);
        var prefix = isVoltage ? "V" : "I";

        if (text.Contains("tctr4", StringComparison.Ordinal) || text.Contains("tvtr4", StringComparison.Ordinal) ||
            text.Contains("neut", StringComparison.Ordinal) || text.Contains("phsn", StringComparison.Ordinal) ||
            text.Contains(".n", StringComparison.Ordinal))
            return prefix + "n";
        if (text.Contains("tctr3", StringComparison.Ordinal) || text.Contains("tvtr3", StringComparison.Ordinal) ||
            text.Contains("phsc", StringComparison.Ordinal) || text.Contains("ic", StringComparison.Ordinal) ||
            text.Contains("vc", StringComparison.Ordinal))
            return prefix + "c";
        if (text.Contains("tctr2", StringComparison.Ordinal) || text.Contains("tvtr2", StringComparison.Ordinal) ||
            text.Contains("phsb", StringComparison.Ordinal) || text.Contains("ib", StringComparison.Ordinal) ||
            text.Contains("vb", StringComparison.Ordinal))
            return prefix + "b";
        if (text.Contains("tctr1", StringComparison.Ordinal) || text.Contains("tvtr1", StringComparison.Ordinal) ||
            text.Contains("phsa", StringComparison.Ordinal) || text.Contains("ia", StringComparison.Ordinal) ||
            text.Contains("va", StringComparison.Ordinal))
            return prefix + "a";

        return string.Empty;
    }

    private static void CountQuality(IEnumerable<DecodedValueRow> rows, ref int good, ref int nonZero)
    {
        foreach (var row in rows.Where(x => x.Kind.Contains("Quality", StringComparison.OrdinalIgnoreCase)))
        {
            if (string.IsNullOrWhiteSpace(row.Raw) || row.Raw.All(c => c is '0'))
                good++;
            else
                nonZero++;
        }
    }

    private static IEnumerable<PhasorVector> ComputePhasors(
        IReadOnlyList<WaveformPoint> points,
        int? samplesPerCycle)
    {
        if (samplesPerCycle is not > 0 || points.Count < samplesPerCycle.Value)
            return Array.Empty<PhasorVector>();

        var window = points.TakeLast(samplesPerCycle.Value).ToArray();
        var currentUnit = window.FirstOrDefault()?.CurrentUnit ?? "count";
        var voltageUnit = window.FirstOrDefault()?.VoltageUnit ?? "count";
        var phasors = new List<PhasorVector>();
        foreach (var item in new[]
        {
            (Name: "Ia", Kind: "Current", Unit: currentUnit, Values: window.Select(x => x.Ia)),
            (Name: "Ib", Kind: "Current", Unit: currentUnit, Values: window.Select(x => x.Ib)),
            (Name: "Ic", Kind: "Current", Unit: currentUnit, Values: window.Select(x => x.Ic)),
            (Name: "In", Kind: "Current", Unit: currentUnit, Values: window.Select(x => x.In)),
            (Name: "Va", Kind: "Voltage", Unit: voltageUnit, Values: window.Select(x => x.Va)),
            (Name: "Vb", Kind: "Voltage", Unit: voltageUnit, Values: window.Select(x => x.Vb)),
            (Name: "Vc", Kind: "Voltage", Unit: voltageUnit, Values: window.Select(x => x.Vc)),
            (Name: "Vn", Kind: "Voltage", Unit: voltageUnit, Values: window.Select(x => x.Vn))
        })
        {
            var values = item.Values.ToArray();
            if (values.Any(value => !value.HasValue))
                continue;

            var numeric = values.Select(value => value!.Value).ToArray();
            if (numeric.Length != samplesPerCycle.Value)
                continue;

            var rms = Math.Sqrt(numeric.Select(value => value * value).Average());
            var peak = numeric.Select(Math.Abs).DefaultIfEmpty(0).Max();
            var mean = numeric.Average();
            var sin = 0.0;
            var cos = 0.0;
            for (var i = 0; i < numeric.Length; i++)
            {
                var theta = 2.0 * Math.PI * i / numeric.Length;
                var ac = numeric[i] - mean;
                sin += ac * Math.Sin(theta);
                cos += ac * Math.Cos(theta);
            }

            var angle = Math.Atan2(cos, sin) * 180.0 / Math.PI;
            phasors.Add(new PhasorVector
            {
                Channel = item.Name,
                Kind = item.Kind,
                Unit = item.Unit,
                Rms = rms,
                Peak = peak,
                AngleDegrees = NormalizeAngle(angle)
            });
        }

        var va = phasors.FirstOrDefault(x =>
            string.Equals(x.Channel, "Va", StringComparison.OrdinalIgnoreCase) && x.Rms > 0);
        if (va is null)
            return phasors;

        return phasors.Select(item => new PhasorVector
        {
            Channel = item.Channel,
            Kind = item.Kind,
            Unit = item.Unit,
            Rms = item.Rms,
            Peak = item.Peak,
            AngleDegrees = NormalizeAngle(item.AngleDegrees - va.AngleDegrees),
            IsValid = item.IsValid,
            InvalidReason = item.InvalidReason
        }).ToArray();
    }

    private static WaveformPoint[] BuildLockedTwoCycleWindow(
        IReadOnlyList<WaveformPoint> points,
        int? samplesPerCycle)
    {
        if (points.Count == 0)
            return Array.Empty<WaveformPoint>();
        if (samplesPerCycle is not > 0)
            return points.TakeLast(Math.Min(512, points.Count)).ToArray();

        var window = Math.Clamp(samplesPerCycle.Value * 2, 32, 512);
        var slots = new WaveformPoint?[window];
        foreach (var point in points)
        {
            var slot = point.SampleCount.HasValue
                ? point.SampleCount.Value % window
                : point.Index % window;
            slots[slot] = point;
        }

        var result = new List<WaveformPoint>(window);
        for (var slot = 0; slot < slots.Length; slot++)
        {
            if (slots[slot] is not { } point)
                continue;

            result.Add(new WaveformPoint
            {
                Index = slot,
                SampleCount = point.SampleCount,
                CurrentUnit = point.CurrentUnit,
                VoltageUnit = point.VoltageUnit,
                ScalingSummary = point.ScalingSummary,
                Ia = point.Ia,
                Ib = point.Ib,
                Ic = point.Ic,
                In = point.In,
                Va = point.Va,
                Vb = point.Vb,
                Vc = point.Vc,
                Vn = point.Vn
            });
        }

        return result.Count >= window
            ? result.ToArray()
            : points.TakeLast(Math.Min(window, points.Count)).ToArray();
    }

    private static double NormalizeAngle(double angle)
    {
        while (angle > 180) angle -= 360;
        while (angle <= -180) angle += 360;
        return angle;
    }

    private static string BuildCursorSummary(IReadOnlyList<WaveformPoint> points)
    {
        if (points.Count < 2)
            return "Cursor compare: waiting for waveform samples.";

        var a = points[Math.Max(0, points.Count - 2)];
        var b = points[^1];
        var iaDelta = Delta(a.Ia, b.Ia, b.CurrentUnit);
        var vaDelta = Delta(a.Va, b.Va, b.VoltageUnit);
        return $"Cursor compare: ΔIa={iaDelta}, ΔVa={vaDelta}, smpCnt {a.SampleCount?.ToString() ?? "-"} → {b.SampleCount?.ToString() ?? "-"}";
    }

    private static string Delta(double? a, double? b, string unit)
        => a.HasValue && b.HasValue
            ? $"{b.Value - a.Value:0.###} {unit}".TrimEnd()
            : "-";

    private static void ValidateAgainstScl(
        SampledValuesFrame frame,
        IReadOnlyList<SampledValueAsdu> asdus,
        SampledValuesPublisherProfile profile,
        ICollection<string> diagnostics)
    {
        if (!string.Equals(profile.Destination.ToString(), frame.Destination.ToString(), StringComparison.OrdinalIgnoreCase))
            diagnostics.Add($"SCL mismatch: destination MAC expected {profile.Destination}, got {frame.Destination}.");

        if (profile.Vlan?.VlanId != frame.Vlan?.VlanId)
            diagnostics.Add($"SCL mismatch: VLAN expected {profile.Vlan?.VlanId.ToString() ?? "untagged"}, got {frame.Vlan?.VlanId.ToString() ?? "untagged"}.");

        if (profile.AppId != frame.AppId)
            diagnostics.Add($"SCL mismatch: APPID expected 0x{profile.AppId:X4}, got 0x{frame.AppId:X4}.");

        if (profile.AsduPerFrame != asdus.Count)
            diagnostics.Add($"SCL mismatch: nofASDU expected {profile.AsduPerFrame}, got {asdus.Count}.");

        foreach (var asdu in asdus)
        {
            if (!string.Equals(profile.Stream.SvId, asdu.SvId, StringComparison.OrdinalIgnoreCase))
                diagnostics.Add($"SCL mismatch: svID expected {profile.Stream.SvId}, got {asdu.SvId}.");

            if (profile.Stream.ConfigurationRevision != asdu.ConfigurationRevision)
                diagnostics.Add($"SCL mismatch: confRev expected {profile.Stream.ConfigurationRevision}, got {asdu.ConfigurationRevision}.");

            if (profile.Stream.SampleRate != 0 && asdu.SampleRate.HasValue &&
                profile.Stream.SampleRate != asdu.SampleRate.Value)
                diagnostics.Add($"SCL mismatch: sample rate expected {profile.Stream.SampleRate}, got {asdu.SampleRate.Value}.");

            if (asdu.SamplePayload.Length != profile.PayloadLayout.PayloadByteLength)
                diagnostics.Add($"Payload length mismatch: expected {profile.PayloadLayout.PayloadByteLength} byte(s), got {asdu.SamplePayload.Length}.");
        }
    }

    private static IEnumerable<DecodedValueRow> DecodePayload(
        SampledValueAsdu asdu,
        SampledValuesPayloadLayout layout,
        ICollection<string> diagnostics)
    {
        var decode = SampledValuesPayloadDecoder.Decode(layout, asdu.SamplePayload);
        foreach (var issue in decode.Diagnostics)
            diagnostics.Add(issue);

        foreach (var value in decode.Values)
        {
            yield return new DecodedValueRow
            {
                Index = value.Element.Index,
                Signal = value.Element.SignalReference,
                Kind = value.Element.Kind.ToString(),
                Value = MmsDataValueRenderer.ToCompactString(value.Value),
                Raw = Convert.ToHexString(value.RawBytes),
                NumericValue = ExtractNumeric(value.Value)
            };
        }
    }

    private static double? ExtractNumeric(MmsDataValue value)
        => value.Kind switch
        {
            MmsDataKind.Integer => Convert.ToDouble(value.Value, CultureInfo.InvariantCulture),
            MmsDataKind.Unsigned => Convert.ToDouble(value.Value, CultureInfo.InvariantCulture),
            MmsDataKind.FloatingPoint => Convert.ToDouble(value.Value, CultureInfo.InvariantCulture),
            _ => null
        };
}
