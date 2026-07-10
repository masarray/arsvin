using AR.Iec61850.SampledValues;
using AR.Iec61850.Scl;

namespace AR.Iec61850.SvPublisher.Models;

public enum InjectionMode
{
    Manual,
    Ramp,
    Sequencer
}

public enum SvSyncPolicyMode
{
    ExternalPtpAuto = 0,
    HonestUnsynchronized = 1,
    LocalCompatibility = 2,
    GlobalCompatibility = 3,

    // Legacy aliases retained so older saved plans and older code references keep the same numeric meaning.
    AutoPtp = ExternalPtpAuto,
    ForceUnsynchronized = HonestUnsynchronized,
    ForceLocal = LocalCompatibility,
    ForceGlobal = GlobalCompatibility
}

public sealed class SvSyncPolicyChoice
{
    public required SvSyncPolicyMode Mode { get; init; }
    public required string Label { get; init; }
    public required string ShortLabel { get; init; }
    public required string HelpText { get; init; }

    public override string ToString() => Label;
}

public enum PtpPublisherMode
{
    MonitorOnly,
    LabPublisher
}


public sealed class SampleQualityChoice
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public required string ShortLabel { get; init; }
    public required string HelpText { get; init; }
    public required SampledValueQuality Quality { get; init; }

    public override string ToString() => Label;
}

public enum PublisherSignalSource
{
    Manual,
    ComtradeReplay
}

public sealed class SvStreamChoice
{
    public required int Index { get; init; }
    public required SclSampledValuesStream Stream { get; init; }

    public string Label =>
        $"#{Index} {Stream.ControlBlockReference}  svID={TextOrDash(Stream.SvId)}  entries={Stream.Entries.Count}";

    private static string TextOrDash(string value)
        => string.IsNullOrWhiteSpace(value) ? "-" : value;

    public override string ToString() => Label;
}


public sealed class RampSignalChoice
{
    public required string KeyCsv { get; init; }
    public required string Name { get; init; }
    public string Quantity { get; init; } = "Magnitude";
    public string Unit { get; init; } = string.Empty;

    public IReadOnlyList<string> Keys =>
        KeyCsv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    public bool AppliesTo(string channelKey) =>
        Keys.Any(key => string.Equals(key, channelKey, StringComparison.OrdinalIgnoreCase));
}

public enum LivePreflightSeverity
{
    Info,
    Warning,
    Error
}

public sealed class LivePreflightDiagnostic
{
    public LivePreflightSeverity Severity { get; init; }
    public string Area { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;

    public string SeverityText => Severity.ToString().ToUpperInvariant();

    public override string ToString()
        => string.IsNullOrWhiteSpace(Detail)
            ? $"{SeverityText}: {Area} — {Message}"
            : $"{SeverityText}: {Area} — {Message} ({Detail})";
}

public sealed class LivePreflightReport
{
    public LivePreflightReport(IReadOnlyList<LivePreflightDiagnostic> diagnostics)
    {
        Diagnostics = diagnostics;
    }

    public IReadOnlyList<LivePreflightDiagnostic> Diagnostics { get; }
    public int ErrorCount => Diagnostics.Count(diagnostic => diagnostic.Severity == LivePreflightSeverity.Error);
    public int WarningCount => Diagnostics.Count(diagnostic => diagnostic.Severity == LivePreflightSeverity.Warning);
    public int InfoCount => Diagnostics.Count(diagnostic => diagnostic.Severity == LivePreflightSeverity.Info);
    public bool HasErrors => ErrorCount > 0;
    public bool HasWarnings => WarningCount > 0;

    public string SummaryText => HasErrors
        ? $"Looptest check blocked: {ErrorCount} fatal error(s), {WarningCount} warning(s)."
        : HasWarnings
            ? $"Looptest check OK with {WarningCount} warning(s)."
            : "Looptest check OK.";
}

public sealed class AdapterChoice
{
    public required string Selector { get; init; }
    public required string DisplayName { get; init; }
    public string MacAddress { get; init; } = string.Empty;

    public override string ToString() => DisplayName;
}

public sealed class SignalChannelSnapshot
{
    public string Key { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public double Magnitude { get; init; }
    public double AngleDegrees { get; init; }
    public double FrequencyHz { get; init; }
    public double DcOffsetPercent { get; init; }
    public double HarmonicPercent { get; init; }
    public int HarmonicOrder { get; init; } = 2;
    public double ClipPercent { get; init; } = 100;
}

public sealed class SequenceStateSnapshot
{
    public string Name { get; init; } = string.Empty;
    public double DurationSeconds { get; init; }

    // Legacy balanced-set fields retained for simple scenarios and older saved plans.
    public double CurrentScale { get; init; } = 1;
    public double VoltageScale { get; init; } = 1;
    public double AngleShiftDegrees { get; init; }
    public double FrequencyHz { get; init; } = 50;

    // P2 per-phase multipliers. Defaults keep legacy balanced behavior.
    public double CurrentScaleA { get; init; } = 1;
    public double CurrentScaleB { get; init; } = 1;
    public double CurrentScaleC { get; init; } = 1;
    public double CurrentScaleN { get; init; }
    public double VoltageScaleA { get; init; } = 1;
    public double VoltageScaleB { get; init; } = 1;
    public double VoltageScaleC { get; init; } = 1;
    public double VoltageScaleN { get; init; }
    public double AngleOffsetA { get; init; }
    public double AngleOffsetB { get; init; }
    public double AngleOffsetC { get; init; }
    public double AngleOffsetN { get; init; }

    // P2 publisher-side waveform shaping for lab stress scenarios. These are intentionally
    // lightweight publisher approximations, not calibrated transient simulation models.
    public double CurrentDcOffsetPercent { get; init; }
    public double VoltageDcOffsetPercent { get; init; }
    public double CurrentHarmonicPercent { get; init; }
    public double VoltageHarmonicPercent { get; init; }
    public int HarmonicOrder { get; init; } = 2;
    public double CurrentClipPercent { get; init; } = 100;
    public double VoltageClipPercent { get; init; } = 100;

    public string ScenarioTag { get; init; } = string.Empty;
}


public sealed class PublisherScenarioPresetChoice
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public required string ShortLabel { get; init; }
    public required string HelpText { get; init; }
    public required IReadOnlyList<SequenceStateSnapshot> States { get; init; }

    public override string ToString() => Label;
}

public sealed class SvPublisherSlotConfigSnapshot
{
    public int Index { get; init; }
    public bool IsEnabled { get; init; }
    public string StreamControlBlock { get; init; } = string.Empty;
    public string StreamId { get; init; } = string.Empty;
    public string DataSetReference { get; init; } = string.Empty;
    public string AppId { get; init; } = string.Empty;
    public string DestinationMac { get; init; } = string.Empty;
    public bool UseVlan { get; init; }
    public int VlanId { get; init; }
    public int VlanPriority { get; init; }
    public string SourceMac { get; init; } = string.Empty;
    public double SampleRateHz { get; init; }
    public double NominalFrequencyHz { get; init; }
    public string SampleRatePresetKey { get; init; } = string.Empty;
    public double CurrentDlsb { get; init; }
    public double VoltageDlsb { get; init; }
    public string ManualSetMode { get; init; } = "Direct";
    public PublisherSignalSource SignalSource { get; init; } = PublisherSignalSource.Manual;
    public string ComtradePath { get; init; } = string.Empty;
    public bool ComtradeLoop { get; init; }
    public string SampleQualityKey { get; init; } = "good";
    public IReadOnlyList<SignalChannelSnapshot> Channels { get; init; } = Array.Empty<SignalChannelSnapshot>();
}

public sealed class SvPublisherConfigSnapshot
{
    public string SclPath { get; init; } = string.Empty;
    public string StreamControlBlock { get; init; } = string.Empty;
    public string StreamId { get; init; } = string.Empty;
    public string DataSetReference { get; init; } = string.Empty;
    public string AppId { get; init; } = string.Empty;
    public string DestinationMac { get; init; } = string.Empty;
    public bool UseVlan { get; init; }
    public int VlanId { get; init; }
    public int VlanPriority { get; init; }
    public string SourceMac { get; init; } = string.Empty;
    public double SampleRateHz { get; init; }
    public double NominalFrequencyHz { get; init; }
    public double CurrentDlsb { get; init; }
    public double VoltageDlsb { get; init; }
    public double DurationSeconds { get; init; }
    public bool Continuous { get; init; }
    public bool LoopSequence { get; init; }
    public InjectionMode Mode { get; init; }
    public string ManualSetMode { get; init; } = "Direct";
    public IReadOnlyList<SvPublisherSlotConfigSnapshot> Publishers { get; init; } = Array.Empty<SvPublisherSlotConfigSnapshot>();
    public bool AutoApplyWhileRunning { get; init; } = true;
    public bool LinkFrequencies { get; init; } = true;
    public SvSyncPolicyMode SyncPolicyMode { get; init; } = SvSyncPolicyMode.GlobalCompatibility;
    public int ExpectedPtpDomain { get; init; }
    public bool PtpAllowLocalFallback { get; init; } = true;
    public PtpPublisherMode PtpPublisherMode { get; init; } = PtpPublisherMode.MonitorOnly;
    public string SampleQualityKey { get; init; } = "good";
    public string PtpClockIdentity { get; init; } = "02:00:00:FF:FE:00:00:01";
    public int PtpAnnounceIntervalMs { get; init; } = 1000;
    public int PtpSyncIntervalMs { get; init; } = 250;
    public bool PtpRespondToPeerDelay { get; init; } = true;
    public string AdapterSelector { get; init; } = string.Empty;
    public string AdapterMacAddress { get; init; } = string.Empty;
    public string RampSignalKey { get; init; } = string.Empty;
    public string ScenarioPresetKey { get; init; } = string.Empty;
    public double RampTargetMagnitude { get; init; }
    public double RampDurationSeconds { get; init; }
    public IReadOnlyList<SignalChannelSnapshot> Channels { get; init; } = Array.Empty<SignalChannelSnapshot>();
    public IReadOnlyList<SequenceStateSnapshot> SequenceStates { get; init; } = Array.Empty<SequenceStateSnapshot>();
}
