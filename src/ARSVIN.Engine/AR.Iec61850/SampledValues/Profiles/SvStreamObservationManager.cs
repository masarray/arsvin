using System.Collections.Concurrent;
using AR.Iec61850.Scl;

namespace AR.Iec61850.SampledValues.Profiles;

public enum SvObservationInputKind
{
    Unknown,
    LiveCapture,
    PcapReplay
}

public sealed record SvObservedStreamKey
{
    public string SourceMac { get; init; } = string.Empty;
    public string DestinationMac { get; init; } = string.Empty;
    public ushort? VlanId { get; init; }
    public ushort AppId { get; init; }
    public string SvId { get; init; } = string.Empty;
    public string DataSetReference { get; init; } = string.Empty;

    public string Id
    {
        get
        {
            var vlan = VlanId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "-";
            return $"SV|{AppId:X4}|{SourceMac}|{DestinationMac}|{vlan}|{SvId}|{DataSetReference}";
        }
    }

    public static SvObservedStreamKey FromFrame(SampledValuesFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        var first = frame.Pdu.Asdus.FirstOrDefault()
            ?? throw new ArgumentException("An observed SV frame requires at least one ASDU.", nameof(frame));

        return new SvObservedStreamKey
        {
            SourceMac = frame.Source.ToString(),
            DestinationMac = frame.Destination.ToString(),
            VlanId = frame.Vlan?.VlanId,
            AppId = frame.AppId,
            SvId = first.SvId,
            DataSetReference = first.DataSetReference
        };
    }
}

public sealed record SvStreamObservationSnapshot
{
    public SvObservedStreamKey Key { get; init; } = new();
    public SvObservedStreamFacts Facts { get; init; } = new();
    public IReadOnlyList<SvObservationInputKind> InputKinds { get; init; }
        = Array.Empty<SvObservationInputKind>();
    public SvObservationInputKind LastInputKind { get; init; }
    public bool IsBoundToScl { get; init; }
    public string ControlBlockReference { get; init; } = string.Empty;
    public SvExpectedStreamConfiguration? ExpectedConfiguration { get; init; }
    public SvConfigurationComparisonResult? ConfigurationComparison { get; init; }
    public string ConfigurationMatchSummary => ConfigurationComparison?.Summary ?? "Not configured";
    public IReadOnlyList<string> Diagnostics { get; init; } = Array.Empty<string>();
}

public sealed class SvStreamObservationManager
{
    private sealed class StreamState
    {
        private readonly object _gate = new();
        private readonly HashSet<SvObservationInputKind> _inputKinds = [];
        private readonly Queue<string> _diagnostics = new();

        public StreamState(int maximumObservations, TimeSpan maximumAge)
        {
            Accumulator = new SvObservationAccumulator(maximumObservations, maximumAge);
        }

        public SvObservationAccumulator Accumulator { get; }
        public SvObservationInputKind LastInputKind { get; private set; }
        public bool IsBoundToScl { get; private set; }
        public string ControlBlockReference { get; private set; } = string.Empty;

        public void Add(
            SvFrameObservation observation,
            SvObservationInputKind inputKind,
            SampledValuesPublisherProfile? profile,
            IEnumerable<string> diagnostics)
        {
            Accumulator.Add(observation);
            lock (_gate)
            {
                LastInputKind = inputKind;
                _inputKinds.Add(inputKind);
                if (profile is not null)
                {
                    IsBoundToScl = true;
                    ControlBlockReference = profile.Stream.ControlBlockReference;
                }

                foreach (var diagnostic in diagnostics.Where(item => !string.IsNullOrWhiteSpace(item)))
                {
                    if (_diagnostics.Contains(diagnostic, StringComparer.Ordinal))
                        continue;

                    _diagnostics.Enqueue(diagnostic);
                    while (_diagnostics.Count > 16)
                        _diagnostics.Dequeue();
                }
            }
        }

        public SvStreamObservationSnapshot Snapshot(SvObservedStreamKey key)
        {
            var facts = Accumulator.BuildFacts();
            lock (_gate)
            {
                return new SvStreamObservationSnapshot
                {
                    Key = key,
                    Facts = facts,
                    InputKinds = _inputKinds.OrderBy(item => item).ToArray(),
                    LastInputKind = LastInputKind,
                    IsBoundToScl = IsBoundToScl,
                    ControlBlockReference = ControlBlockReference,
                    Diagnostics = facts.Diagnostics.Concat(_diagnostics).Distinct(StringComparer.Ordinal).ToArray()
                };
            }
        }
    }

    private readonly ConcurrentDictionary<SvObservedStreamKey, StreamState> _streams = new();
    private readonly int _maximumObservations;
    private readonly TimeSpan _maximumAge;

    public SvStreamObservationManager(
        int maximumObservations = SvObservationAccumulator.DefaultMaximumObservations,
        TimeSpan? maximumAge = null)
    {
        if (maximumObservations < 2)
            throw new ArgumentOutOfRangeException(nameof(maximumObservations));

        _maximumAge = maximumAge ?? SvObservationAccumulator.DefaultMaximumAge;
        if (_maximumAge <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(maximumAge));

        _maximumObservations = maximumObservations;
    }

    public int Count => _streams.Count;

    public bool TryObserve(
        DateTimeOffset timestamp,
        SampledValuesFrame frame,
        SvObservationInputKind inputKind,
        SampledValuesPublisherProfile? profile,
        out SvStreamObservationSnapshot snapshot,
        double? nominalFrequencyHz = null,
        SvComparisonMode comparisonMode = SvComparisonMode.Compatible)
    {
        ArgumentNullException.ThrowIfNull(frame);
        snapshot = new SvStreamObservationSnapshot();

        var asdus = frame.Pdu.Asdus;
        var first = asdus.FirstOrDefault();
        if (first is null || first.SamplePayload.Length <= 0)
            return false;

        var key = SvObservedStreamKey.FromFrame(frame);
        var diagnostics = ValidateFrameConsistency(asdus).ToList();
        var boundProfile = ValidateProfileBinding(frame, profile, diagnostics);
        var signature = boundProfile?.Entries.Select(ToSignature).ToArray()
            ?? Array.Empty<SvDatasetElementSignature>();
        var payloadLengths = asdus.Select(item => item.SamplePayload.Length).Distinct().ToArray();
        var payloadLength = payloadLengths.Length == 1 ? payloadLengths[0] : first.SamplePayload.Length;

        var observation = new SvFrameObservation
        {
            Timestamp = timestamp,
            EtherType = 0x88BA,
            AppId = frame.AppId,
            DestinationMac = frame.Destination.ToString(),
            VlanId = frame.Vlan?.VlanId,
            VlanPriority = frame.Vlan?.PriorityCodePoint,
            SvId = first.SvId,
            DataSetReference = first.DataSetReference,
            ConfigurationRevision = first.ConfigurationRevision,
            PayloadBytesPerAsdu = payloadLength,
            SampleCounts = asdus.Select(item => item.SampleCount).ToArray(),
            DeclaredSampleRate = StableNullable(asdus.Select(item => item.SampleRate)),
            DeclaredSampleMode = StableNullable(asdus.Select(item => item.SampleMode)),
            NominalFrequencyHz = nominalFrequencyHz,
            DataSetSignature = signature
        };

        var state = _streams.GetOrAdd(
            key,
            _ => new StreamState(_maximumObservations, _maximumAge));
        state.Add(observation, inputKind, boundProfile, diagnostics);

        var observedSnapshot = state.Snapshot(key);
        if (boundProfile is null)
        {
            snapshot = observedSnapshot;
            return true;
        }

        var expected = SvExpectedStreamConfigurationFactory.Create(boundProfile);
        var comparison = new SvConfigurationComparer().Compare(
            expected,
            observedSnapshot.Facts,
            comparisonMode);
        snapshot = observedSnapshot with
        {
            ExpectedConfiguration = expected,
            ConfigurationComparison = comparison
        };
        return true;
    }

    public bool TryGetSnapshot(SvObservedStreamKey key, out SvStreamObservationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (_streams.TryGetValue(key, out var state))
        {
            snapshot = state.Snapshot(key);
            return true;
        }

        snapshot = new SvStreamObservationSnapshot();
        return false;
    }

    public IReadOnlyList<SvStreamObservationSnapshot> SnapshotAll()
        => _streams.Select(pair => pair.Value.Snapshot(pair.Key))
            .OrderBy(item => item.Key.AppId)
            .ThenBy(item => item.Key.SvId, StringComparer.Ordinal)
            .ThenBy(item => item.Key.DataSetReference, StringComparer.Ordinal)
            .ToArray();

    public void Clear() => _streams.Clear();

    private static SampledValuesPublisherProfile? ValidateProfileBinding(
        SampledValuesFrame frame,
        SampledValuesPublisherProfile? profile,
        ICollection<string> diagnostics)
    {
        if (profile is null)
            return null;

        var appIdMatches = profile.AppId == frame.AppId;
        var destinationMatches = string.Equals(
            profile.Destination.ToString(),
            frame.Destination.ToString(),
            StringComparison.OrdinalIgnoreCase);
        var vlanMatches = profile.Vlan?.VlanId == frame.Vlan?.VlanId;
        if (appIdMatches && destinationMatches && vlanMatches)
            return profile;

        diagnostics.Add(
            $"Rejected SCL candidate {profile.Stream.ControlBlockReference}: " +
            "APPID, destination MAC, and VLAN must identify the same configured stream before comparison.");
        return null;
    }

    private static IReadOnlyList<string> ValidateFrameConsistency(IReadOnlyList<SampledValueAsdu> asdus)
    {
        var diagnostics = new List<string>();
        var first = asdus[0];

        if (asdus.Any(item => !string.Equals(item.SvId, first.SvId, StringComparison.Ordinal)))
            diagnostics.Add("ASDUs inside one Ethernet frame expose different svID values.");
        if (asdus.Any(item => !string.Equals(item.DataSetReference, first.DataSetReference, StringComparison.Ordinal)))
            diagnostics.Add("ASDUs inside one Ethernet frame expose different dataset references.");
        if (asdus.Any(item => item.ConfigurationRevision != first.ConfigurationRevision))
            diagnostics.Add("ASDUs inside one Ethernet frame expose different confRev values.");
        if (asdus.Select(item => item.SamplePayload.Length).Distinct().Count() > 1)
            diagnostics.Add("ASDUs inside one Ethernet frame expose different payload lengths.");

        return diagnostics;
    }

    private static SvDatasetElementSignature ToSignature(SclDataSetEntry entry)
        => new()
        {
            BType = entry.BType,
            Cdc = entry.Cdc,
            IsQuality = entry.IsQuality,
            IsTimestamp = entry.IsTimestamp
        };

    private static ushort? StableNullable(IEnumerable<ushort?> values)
    {
        var distinct = values.Distinct().ToArray();
        return distinct.Length == 1 ? distinct[0] : null;
    }
}
