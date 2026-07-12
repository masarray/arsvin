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
    public IReadOnlyList<SvDatasetElementSignature> DataSetSignature { get; init; }
        = Array.Empty<SvDatasetElementSignature>();

    public int AsduPerFrame => SampleCounts.Count;

    public void Validate()
    {
        if (SampleCounts.Count == 0)
            throw new ArgumentException("An SV frame observation requires at least one ASDU sample counter.");
        if (PayloadBytesPerAsdu <= 0)
            throw new ArgumentOutOfRangeException(nameof(PayloadBytesPerAsdu));
    }
}

public sealed class SvObservationAccumulator
{
    private readonly List<SvFrameObservation> _observations = [];

    public int Count => _observations.Count;

    public void Add(SvFrameObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        observation.Validate();
        _observations.Add(observation);
    }

    public void Clear() => _observations.Clear();

    public SvObservedStreamFacts BuildFacts()
    {
        if (_observations.Count == 0)
            return new SvObservedStreamFacts();

        var ordered = _observations.OrderBy(item => item.Timestamp).ToArray();
        var diagnostics = new List<string>();
        var first = ordered[0];
        var last = ordered[^1];

        var durationSeconds = (last.Timestamp - first.Timestamp).TotalSeconds;
        double? framesPerSecond = null;
        double? samplesPerSecond = null;
        if (ordered.Length >= 2 && durationSeconds > 0)
        {
            framesPerSecond = (ordered.Length - 1) / durationSeconds;
            var averageAsduPerFrame = ordered.Average(item => item.AsduPerFrame);
            samplesPerSecond = framesPerSecond * averageAsduPerFrame;
        }
        else
        {
            diagnostics.Add("At least two timestamped frames are required to estimate stream rate.");
        }

        var counterWrap = DetectCounterWrap(ordered, diagnostics);

        return new SvObservedStreamFacts
        {
            EtherType = StableValue(ordered.Select(item => item.EtherType), "EtherType", diagnostics),
            AppId = StableValue(ordered.Select(item => item.AppId), "APPID", diagnostics),
            DestinationMac = StableString(ordered.Select(item => item.DestinationMac), "destination MAC", diagnostics),
            VlanId = StableNullableValue(ordered.Select(item => item.VlanId), "VLAN ID", diagnostics),
            VlanPriority = StableNullableValue(ordered.Select(item => item.VlanPriority), "VLAN priority", diagnostics),
            SvId = StableString(ordered.Select(item => item.SvId), "svID", diagnostics),
            DataSetReference = StableString(ordered.Select(item => item.DataSetReference), "dataset reference", diagnostics),
            ConfigurationRevision = StableValue(ordered.Select(item => item.ConfigurationRevision), "confRev", diagnostics),
            AsduPerFrame = StableValue(ordered.Select(item => item.AsduPerFrame), "ASDU count", diagnostics),
            PayloadBytesPerAsdu = StableValue(ordered.Select(item => item.PayloadBytesPerAsdu), "payload length", diagnostics),
            ObservedFramesPerSecond = framesPerSecond,
            ObservedSamplesPerSecond = samplesPerSecond,
            ObservedCounterWrap = counterWrap,
            DeclaredSampleRate = StableNullableValue(ordered.Select(item => item.DeclaredSampleRate), "declared sample rate", diagnostics),
            DeclaredSampleMode = StableNullableValue(ordered.Select(item => item.DeclaredSampleMode), "declared sample mode", diagnostics),
            NominalFrequencyHz = StableNullableValue(ordered.Select(item => item.NominalFrequencyHz), "nominal frequency", diagnostics),
            DataSetSignature = StableSignature(ordered.Select(item => item.DataSetSignature), diagnostics),
            ObservationCount = ordered.Length,
            FirstTimestamp = first.Timestamp,
            LastTimestamp = last.Timestamp,
            Diagnostics = diagnostics
        };
    }

    private static int? DetectCounterWrap(
        IReadOnlyList<SvFrameObservation> observations,
        List<string> diagnostics)
    {
        var sampleCounts = observations.SelectMany(item => item.SampleCounts).ToArray();
        if (sampleCounts.Length < 2)
            return null;

        var wraps = new HashSet<int>();
        for (var index = 1; index < sampleCounts.Length; index++)
        {
            var previous = sampleCounts[index - 1];
            var current = sampleCounts[index];
            if (current < previous)
                wraps.Add(previous + 1);
        }

        if (wraps.Count == 1)
            return wraps.Single();

        if (wraps.Count > 1)
            diagnostics.Add($"Multiple sample-counter wrap candidates were observed: {string.Join(", ", wraps.Order())}.");

        return null;
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
        var normalized = signatures
            .Select(signature => signature.Select(ToKey).ToArray())
            .ToArray();

        if (normalized.Length == 0 || normalized.All(signature => signature.Length == 0))
            return Array.Empty<SvDatasetElementSignature>();

        var first = normalized[0];
        if (normalized.All(signature => signature.SequenceEqual(first, StringComparer.Ordinal)))
            return signatures.First().ToArray();

        diagnostics.Add("Observed dataset signature changed within the analysis window.");
        return Array.Empty<SvDatasetElementSignature>();
    }

    private static string ToKey(SvDatasetElementSignature element)
        => $"{element.NormalizedBType}|{element.NormalizedCdc}|{element.IsQuality}|{element.IsTimestamp}";
}
