using AR.Iec61850.SampledValues.Measurements;
using AR.Iec61850.SampledValues.Profiles;

namespace ARSVIN.Subscriber.Models;

public sealed class SvStreamSnapshot
{
    public string Key { get; init; } = string.Empty;
    public string Health { get; init; } = "IDLE";
    public string HealthDetail { get; init; } = string.Empty;
    public ushort AppId { get; init; }
    public string Source { get; init; } = string.Empty;
    public string Destination { get; init; } = string.Empty;
    public ushort? VlanId { get; init; }
    public byte? VlanPriority { get; init; }
    public string SvId { get; init; } = string.Empty;
    public string DataSet { get; init; } = string.Empty;
    public uint? ConfRev { get; init; }
    public int NofAsdu { get; init; }
    public ushort? LastSmpCnt { get; init; }
    public ushort? SampleRate { get; init; }
    public ushort? SampleMode { get; init; }
    public byte? SmpSynch { get; init; }
    public double? NominalFrequencyHz { get; init; }
    public int? SamplesPerCycle { get; init; }
    public ushort? ResolvedCounterWrap { get; init; }
    public SvTimebaseSource TimebaseSource { get; init; } = SvTimebaseSource.Unknown;
    public string TimebaseReason { get; init; } = string.Empty;
    public string ScalingSummary { get; init; } = "Raw counts";
    public string ScalingReason { get; init; } = string.Empty;
    public long FrameCount { get; init; }
    public long AsduCount { get; init; }
    public double ActualFps { get; init; }
    public double AverageFrameGapMilliseconds { get; init; }
    public double MaxFrameGapMilliseconds { get; init; }
    public int SequenceGapCount { get; init; }
    public int DuplicateCount { get; init; }
    public int OutOfOrderCount { get; init; }
    public int PayloadIssueCount { get; init; }
    public int SclMismatchCount { get; init; }
    public bool IsBoundToScl { get; init; }
    public string ControlBlockReference { get; init; } = string.Empty;
    public string LayoutBinding { get; init; } = string.Empty;
    public string LastSeen { get; init; } = string.Empty;
    public IReadOnlyList<string> Diagnostics { get; init; } = Array.Empty<string>();
    public IReadOnlyList<DecodedValueRow> Values { get; init; } = Array.Empty<DecodedValueRow>();
    public IReadOnlyList<WaveformPoint> WaveformPoints { get; init; } = Array.Empty<WaveformPoint>();
    public IReadOnlyList<PhasorVector> Phasors { get; init; } = Array.Empty<PhasorVector>();
    public string CursorSummary { get; init; } = string.Empty;
    public string QualitySummary { get; init; } = string.Empty;
    public IReadOnlyList<SvObservationInputKind> ObservationInputKinds { get; init; }
        = Array.Empty<SvObservationInputKind>();
    public int ObservationWindowFrames { get; init; }
    public int ObservationWindowSamples { get; init; }
    public double ObservationWindowDurationSeconds { get; init; }
    public double? ObservedFramesPerSecond { get; init; }
    public double? ObservedSamplesPerSecond { get; init; }
    public int? ObservedCounterWrap { get; init; }
    public bool IsWaveformWindowReady { get; init; }
    public SvProfileDetectionResult? ProfileDetection { get; init; }
    public SvConfigurationComparisonResult? ConfigurationComparison { get; init; }
    public IReadOnlyList<string> ObservationDiagnostics { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, SvFactSource> FactProvenance { get; init; }
        = new Dictionary<string, SvFactSource>(StringComparer.Ordinal);
}
