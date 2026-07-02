using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using AR.Iec61850.Comtrade;
using AR.Iec61850.Ethernet;
using AR.Iec61850.Mms;
using AR.Iec61850.SampledValues;
using AR.Iec61850.Scl;
using AR.Iec61850.SvPublisher.Models;
using AR.Iec61850.Transports;
using AR.Iec61850.Transports.Npcap;
using AR.Iec61850.TimeSync.Health;
using AR.Iec61850.TimeSync.Monitoring;
using AR.Iec61850.TimeSync.Ptp;
using AR.Iec61850.TimeSync.PtpRuntime;
using Microsoft.Win32;

namespace AR.Iec61850.SvPublisher.ViewModels;

public sealed class SvPublisherViewModel : ObservableObject
{
    private const string DirectSetMode = "Direct";
    private const string LineLineSetMode = "Line-Line";
    private const string SymmetricalSetMode = "Symmetrical components";
    private const string PtpCaptureFilter = "ether proto 0x88f7";
    private const double NominalVoltageLn = 57.735;
    private const double NominalVoltageLl = 100.0;
    private const double NominalCurrent = 1.0;

    private readonly List<string> _eventLines = new();
    private SvPublisherSlotViewModel? _selectedPublisherSlot;
    private bool _isLoadingPublisherSlot;
    private SvStreamChoice? _selectedStream;
    private AdapterChoice? _selectedAdapter;
    private SignalChannelViewModel? _selectedRampChannel;
    private RampSignalChoice? _selectedRampSignalChoice;
    private bool _isSyncingRampSignalChoice;
    private RampStateViewModel? _selectedRampState;
    private SequenceStateViewModel? _selectedSequenceState;
    private CancellationTokenSource? _publisherStop;
    private string _sclPath = string.Empty;
    private string _sclSummary = "Open an SCL file to resolve SV streams.";
    private string _statusText = "Idle";
    private string _publishText = "No active publisher.";
    private string _txTimingHealthText = "TX Timing: idle";
    private string _evidenceText = string.Empty;
    private string _liveApplyText = "Auto apply ready.";
    private string _livePreflightSummaryText = "Looptest quick mode: preflight not run.";
    private string _streamId = string.Empty;
    private string _streamControlBlock = string.Empty;
    private string _dataSetReference = string.Empty;
    private string _appIdText = string.Empty;
    private string _destinationMac = string.Empty;
    private string _sourceMac = "02:00:00:00:20:01";
    private bool _useVlan;
    private int _vlanId;
    private int _vlanPriority = 4;
    private double _sampleRateHz = 4000;
    private double _nominalFrequencyHz = 50;
    private SampleRatePreset? _selectedSampleRatePreset;
    private SampleQualityChoice? _selectedSampleQualityChoice;
    private PublisherScenarioPresetChoice? _selectedScenarioPresetChoice;
    private double _currentDlsb = 0.001;
    private double _voltageDlsb = 0.01;
    private double _durationSeconds = 1;
    private bool _continuous = true;
    private bool _loopSequence;
    private bool _isLiveArmed;
    private bool _isPublishing;
    private bool _autoApplyWhileRunning = true;
    private bool _linkFrequencies = true;
    private SvSyncPolicyMode _syncPolicyMode = SvSyncPolicyMode.GlobalCompatibility;
    private SvSyncPolicyChoice? _selectedSyncPolicyChoice;
    private int _expectedPtpDomain;
    private bool _ptpAllowLocalFallback = true;
    private PtpPublisherMode _ptpPublisherMode = AR.Iec61850.SvPublisher.Models.PtpPublisherMode.MonitorOnly;
    private string _ptpClockIdentityText = "02:00:00:FF:FE:00:00:01";
    private int _ptpAnnounceIntervalMs = 1000;
    private int _ptpSyncIntervalMs = 250;
    private bool _ptpRespondToPeerDelay = true;
    private string _ptpPublisherStatusText = "PTP TX: off";
    private string _ptpStatusText = "PTP RX: idle";
    private string _smpSynchStatusText = "smpSynch=2 compatibility";
    private bool _isUpdatingManualRows;
    private ManualOutputRowViewModel? _contextManualRow;
    private string _contextColumnHeader = string.Empty;
    private string _signalNamingScheme = "L1L2L3E";
    private string _manualSetMode = DirectSetMode;
    private InjectionMode _mode;
    private double _rampTargetMagnitude = 5;
    private double _rampDurationSeconds = 1;
    private int _dataSetEntryCount;
    private int _mappedSignalCount;
    private int _payloadBytes;

    public SvPublisherViewModel()
    {
        Channels =
        [
            new SignalChannelViewModel("Ia", "I L1", "I", "A", NominalCurrent, 0, _nominalFrequencyHz),
            new SignalChannelViewModel("Ib", "I L2", "I", "A", NominalCurrent, -120, _nominalFrequencyHz),
            new SignalChannelViewModel("Ic", "I L3", "I", "A", NominalCurrent, 120, _nominalFrequencyHz),
            new SignalChannelViewModel("In", "I N", "I", "A", 0.000, 0, _nominalFrequencyHz) { IsEnabled = false },
            new SignalChannelViewModel("Va", "V L1-E", "V", "V", NominalVoltageLn, 0, _nominalFrequencyHz),
            new SignalChannelViewModel("Vb", "V L2-E", "V", "V", NominalVoltageLn, -120, _nominalFrequencyHz),
            new SignalChannelViewModel("Vc", "V L3-E", "V", "V", NominalVoltageLn, 120, _nominalFrequencyHz),
            new SignalChannelViewModel("Vn", "V N", "V", "V", 0.000, 0, _nominalFrequencyHz) { IsEnabled = false }
        ];

        ManualRows = new ObservableCollection<ManualOutputRowViewModel>();
        PublisherSlots = new ObservableCollection<SvPublisherSlotViewModel>
        {
            new(1),
            new(2),
            new(3)
        };
        foreach (var slot in PublisherSlots)
        {
            slot.Channels = Channels.Select(c => c.ToSnapshot()).ToArray();
            slot.PropertyChanged += OnPublisherSlotPropertyChanged;
        }

        RampPreviewChannels = CreatePreviewChannels();
        SequencePreviewChannels = CreatePreviewChannels();
        RampStates =
        [
            new RampStateViewModel("Ramp 1", "Ia", "I L1", "Magnitude", 1.000, 5.000, 0.200, 0.100, 21, 2.100),
            new RampStateViewModel("Ramp 2", "Ia", "I L1", "Magnitude", 5.000, 4.000, -0.050, 0.100, 21, 2.100)
        ];

        SequenceStates =
        [
            new SequenceStateViewModel("Prefault", 1.000, 1.0, 1.0, 0, 50),
            new SequenceStateViewModel("Fault", 0.200, 4.0, 0.25, 0, 50),
            new SequenceStateViewModel("Recovery", 1.000, 1.0, 1.0, 0, 50)
        ];

        foreach (var rampState in RampStates)
            AttachRampState(rampState);
        foreach (var sequenceState in SequenceStates)
            AttachSequenceState(sequenceState);

        SelectedRampSignalChoice = RampSignalChoices.FirstOrDefault(choice => string.Equals(choice.KeyCsv, "Ia", StringComparison.OrdinalIgnoreCase));
        SelectedRampChannel = Channels.FirstOrDefault(c => c.Key == "Ia");
        SelectedRampState = RampStates.FirstOrDefault();
        SelectedSequenceState = SequenceStates.FirstOrDefault();
        SelectedSampleRatePreset = SampleRatePresets.FirstOrDefault(preset => preset.Key == "9-2LE-80-50");
        _selectedSampleQualityChoice = SampleQualityChoices.FirstOrDefault(choice => choice.Key == "good");
        SelectedScenarioPresetChoice = ScenarioPresetChoices.FirstOrDefault(choice => choice.Key == "protection-fault");
        SelectedPublisherSlot = PublisherSlots.FirstOrDefault();
        _selectedSyncPolicyChoice = ResolveSyncPolicyChoice(_syncPolicyMode);
        SmpSynchStatusText = FormatSmpSynchStatus(ResolveSampleSynchronization(null));

        OpenSclCommand = new AsyncRelayCommand(OpenSclAsync, () => !IsPublishing);
        ImportComtradeCommand = new AsyncRelayCommand(ImportComtradeAsync, () => !IsPublishing && SelectedPublisherSlot is not null);
        ClearComtradeCommand = new RelayCommand(ClearComtrade, () => !IsPublishing && SelectedPublisherSlot is not null);
        RefreshAdaptersCommand = new RelayCommand(RefreshAdapters, () => !IsPublishing);
        RunPreflightCommand = new RelayCommand(RunLivePreflight, () => !IsPublishing);
        SaveProfileCommand = new AsyncRelayCommand(SaveProfileAsync, () => !IsPublishing);
        ExportGeneratedPcapCommand = new AsyncRelayCommand(ExportGeneratedPcapAsync, () => !IsPublishing);
        ExportPublisherEvidenceReportCommand = new AsyncRelayCommand(ExportPublisherEvidenceReportAsync, () => !IsPublishing);
        ApplyScenarioPresetCommand = new RelayCommand(ApplySelectedScenarioPreset, () => !IsPublishing && SelectedScenarioPresetChoice is not null);
        RunDryCommand = new AsyncRelayCommand(() => RunPublishAsync(live: false), () => !IsPublishing);
        RunLiveCommand = new AsyncRelayCommand(() => RunPublishAsync(live: true), () => !IsPublishing);
        StopCommand = new RelayCommand(StopPublisher, () => IsPublishing);
        ApplyBalancedDefaultsCommand = new RelayCommand(ApplyBalancedDefaults);
        AddSequenceStateCommand = new RelayCommand(AddSequenceState, () => !IsPublishing);
        RemoveSequenceStateCommand = new RelayCommand(RemoveLastSequenceState, () => !IsPublishing && SequenceStates.Count > 0);
        SelectSequenceStateCommand = new ParameterRelayCommand(parameter => SelectSequenceState(parameter as SequenceStateViewModel));
        AddRampStateCommand = new RelayCommand(AddRampState, () => !IsPublishing);
        RemoveRampStateCommand = new RelayCommand(RemoveSelectedRampState, () => !IsPublishing && RampStates.Count > 0);
        ApplyNominalCommand = new RelayCommand(ApplyNominalValues);
        ZeroOutputCommand = new RelayCommand(ZeroOutputs);
        EqualMagnitudesCommand = new RelayCommand(EqualMagnitudes);
        HundredPercentLoadCommand = new RelayCommand(ApplyHundredPercentLoad);
        FiftyPercentLoadCommand = new RelayCommand(ApplyFiftyPercentLoad);
        UnloadCommand = new RelayCommand(ApplyUnload);
        BalanceAnglesCommand = new RelayCommand(BalanceAngles);
        NominalValueFromContextCommand = new RelayCommand(NominalValueFromContext);
        ZeroFromContextCommand = new RelayCommand(ZeroFromContext);
        EqualMagnitudesFromContextCommand = new RelayCommand(EqualMagnitudesFromContext);
        LineAngleFromContextCommand = new RelayCommand(LineAngleFromContext);
        BalanceAnglesFromContextCommand = new RelayCommand(BalanceAnglesFromContext);
        ReverseRotationFromContextCommand = new RelayCommand(ReverseRotationFromContext);
        NominalFrequencyFromContextCommand = new RelayCommand(NominalFrequencyFromContext);
        DcFrequencyFromContextCommand = new RelayCommand(DcFrequencyFromContext);
        EqualFrequenciesFromContextCommand = new RelayCommand(EqualFrequenciesFromContext);
        ReverseRotationCommand = new RelayCommand(ReverseRotation);
        SetSignalNamingCommand = new ParameterRelayCommand(parameter => ApplySignalNaming(parameter?.ToString() ?? "L1L2L3E"));
        CopyTableCommand = new RelayCommand(CopyManualTable);
        PasteTableCommand = new RelayCommand(PasteManualTable);

        ApplyChannelNaming();
        RebuildManualRowsFromChannels();
        UpdateRampPreview();
        UpdateSequencePreview();
        RefreshAdapters();
    }

    public ObservableCollection<SignalChannelViewModel> Channels { get; }
    public ObservableCollection<ManualOutputRowViewModel> ManualRows { get; }
    public ObservableCollection<SignalChannelViewModel> RampPreviewChannels { get; }
    public ObservableCollection<SignalChannelViewModel> SequencePreviewChannels { get; }
    public ObservableCollection<RampStateViewModel> RampStates { get; }
    public ObservableCollection<SequenceStateViewModel> SequenceStates { get; }
    public ObservableCollection<SvPublisherSlotViewModel> PublisherSlots { get; }

    public IReadOnlyList<RampSignalChoice> RampSignalChoices { get; } =
    [
        new RampSignalChoice { KeyCsv = "Va", Name = "V L1-E", Unit = "V" },
        new RampSignalChoice { KeyCsv = "Vb", Name = "V L2-E", Unit = "V" },
        new RampSignalChoice { KeyCsv = "Vc", Name = "V L3-E", Unit = "V" },
        new RampSignalChoice { KeyCsv = "Ia", Name = "I L1", Unit = "A" },
        new RampSignalChoice { KeyCsv = "Ib", Name = "I L2", Unit = "A" },
        new RampSignalChoice { KeyCsv = "Ic", Name = "I L3", Unit = "A" },
        new RampSignalChoice { KeyCsv = "Va,Vb,Vc", Name = "V L1-E, L2-E, L3-E", Unit = "V" },
        new RampSignalChoice { KeyCsv = "Ia,Ib,Ic", Name = "I L1, L2, L3", Unit = "A" }
    ];

    public IReadOnlyList<string> RampQuantities { get; } =
    [
        "Magnitude"
    ];

    public double RampTotalTimeSeconds => RampStates.Sum(state => Math.Max(0.001, state.TimeSeconds));

    public string RampTotalTimeText => $"{RampTotalTimeSeconds:0.000} s";
    public ObservableCollection<SvStreamChoice> Streams { get; } = new();
    public ObservableCollection<AdapterChoice> Adapters { get; } = new();
    public ObservableCollection<LivePreflightDiagnostic> LivePreflightDiagnostics { get; } = new();

    public IReadOnlyList<SampleRatePreset> SampleRatePresets { get; } =
    [
        new SampleRatePreset { Key = "9-2LE-80-50", Label = "9-2LE protection — 80 spc / 50 Hz / 4000 fps", SampleRateHz = 4000, NominalFrequencyHz = 50, SamplesPerCycle = 80 },
        new SampleRatePreset { Key = "9-2LE-80-60", Label = "9-2LE protection — 80 spc / 60 Hz / 4800 fps", SampleRateHz = 4800, NominalFrequencyHz = 60, SamplesPerCycle = 80 },
        new SampleRatePreset { Key = "9-2LE-256-50", Label = "9-2LE power quality — 256 spc / 50 Hz / 12800 fps", SampleRateHz = 12800, NominalFrequencyHz = 50, SamplesPerCycle = 256 },
        new SampleRatePreset { Key = "9-2LE-256-60", Label = "9-2LE power quality — 256 spc / 60 Hz / 15360 fps", SampleRateHz = 15360, NominalFrequencyHz = 60, SamplesPerCycle = 256 },
        new SampleRatePreset { Key = "61869-9-96-50", Label = "IEC 61869-9 profile — 96 spc / 50 Hz / 4800 fps", SampleRateHz = 4800, NominalFrequencyHz = 50, SamplesPerCycle = 96 },
        new SampleRatePreset { Key = "61869-9-96-60", Label = "IEC 61869-9 profile — 96 spc / 60 Hz / 5760 fps", SampleRateHz = 5760, NominalFrequencyHz = 60, SamplesPerCycle = 96 },
        new SampleRatePreset { Key = "61869-9-288-50", Label = "IEC 61869-9 profile — 288 spc / 50 Hz / 14400 fps", SampleRateHz = 14400, NominalFrequencyHz = 50, SamplesPerCycle = 288 },
        new SampleRatePreset { Key = "61869-9-288-60", Label = "IEC 61869-9 profile — 288 spc / 60 Hz / 17280 fps", SampleRateHz = 17280, NominalFrequencyHz = 60, SamplesPerCycle = 288 }
    ];

    public IReadOnlyList<PublisherScenarioPresetChoice> ScenarioPresetChoices { get; } =
    [
        new PublisherScenarioPresetChoice
        {
            Key = "protection-fault",
            Label = "Protection fault — prefault / 3-phase fault / recovery",
            ShortLabel = "3P fault",
            HelpText = "Balanced three-phase protection scenario: nominal prefault, high-current low-voltage fault, recovery.",
            States =
            [
                new SequenceStateSnapshot { Name = "Prefault", DurationSeconds = 0.200, CurrentScale = 1.000, VoltageScale = 1.000, FrequencyHz = 50.000, ScenarioTag = "prefault" },
                new SequenceStateSnapshot { Name = "3P fault", DurationSeconds = 0.120, CurrentScale = 5.000, VoltageScale = 0.200, FrequencyHz = 50.000, ScenarioTag = "three-phase-fault" },
                new SequenceStateSnapshot { Name = "Recovery", DurationSeconds = 0.250, CurrentScale = 1.000, VoltageScale = 1.000, FrequencyHz = 50.000, ScenarioTag = "recovery" }
            ]
        },
        new PublisherScenarioPresetChoice
        {
            Key = "single-phase-a-ground",
            Label = "Single-phase A-G fault — per-phase",
            ShortLabel = "A-G fault",
            HelpText = "Per-phase publisher scenario: phase-A current rises, phase-A voltage collapses, B/C remain near nominal.",
            States =
            [
                new SequenceStateSnapshot { Name = "Prefault", DurationSeconds = 0.200, CurrentScale = 1.000, VoltageScale = 1.000, FrequencyHz = 50.000, ScenarioTag = "prefault" },
                new SequenceStateSnapshot { Name = "A-G fault", DurationSeconds = 0.160, CurrentScale = 1.000, VoltageScale = 1.000, CurrentScaleA = 7.000, CurrentScaleB = 0.900, CurrentScaleC = 0.900, CurrentScaleN = 3.500, VoltageScaleA = 0.080, VoltageScaleB = 1.000, VoltageScaleC = 1.000, FrequencyHz = 50.000, ScenarioTag = "single-phase-a-ground" },
                new SequenceStateSnapshot { Name = "Recovery", DurationSeconds = 0.300, CurrentScale = 1.000, VoltageScale = 1.000, FrequencyHz = 50.000, ScenarioTag = "recovery" }
            ]
        },
        new PublisherScenarioPresetChoice
        {
            Key = "phase-bc-fault",
            Label = "Phase-to-phase B-C fault — per-phase",
            ShortLabel = "B-C fault",
            HelpText = "Per-phase publisher scenario: B/C currents rise with opposing angle bias, B/C voltages depress, A remains near nominal.",
            States =
            [
                new SequenceStateSnapshot { Name = "Prefault", DurationSeconds = 0.200, CurrentScale = 1.000, VoltageScale = 1.000, FrequencyHz = 50.000, ScenarioTag = "prefault" },
                new SequenceStateSnapshot { Name = "B-C fault", DurationSeconds = 0.160, CurrentScale = 1.000, VoltageScale = 1.000, CurrentScaleA = 0.700, CurrentScaleB = 6.000, CurrentScaleC = 6.000, VoltageScaleA = 1.000, VoltageScaleB = 0.250, VoltageScaleC = 0.250, AngleOffsetB = 18.000, AngleOffsetC = -18.000, FrequencyHz = 50.000, ScenarioTag = "phase-bc-fault" },
                new SequenceStateSnapshot { Name = "Recovery", DurationSeconds = 0.300, CurrentScale = 1.000, VoltageScale = 1.000, FrequencyHz = 50.000, ScenarioTag = "recovery" }
            ]
        },
        new PublisherScenarioPresetChoice
        {
            Key = "negative-sequence",
            Label = "Negative sequence / unbalance — per-phase",
            ShortLabel = "Negative sequence",
            HelpText = "Unbalanced per-phase magnitude and angle offsets for subscriber negative-sequence behavior checks.",
            States =
            [
                new SequenceStateSnapshot { Name = "Balanced", DurationSeconds = 0.250, CurrentScale = 1.000, VoltageScale = 1.000, FrequencyHz = 50.000, ScenarioTag = "balanced" },
                new SequenceStateSnapshot { Name = "Neg-seq bias", DurationSeconds = 0.400, CurrentScale = 1.000, VoltageScale = 1.000, CurrentScaleA = 1.400, CurrentScaleB = 0.700, CurrentScaleC = 1.150, VoltageScaleA = 0.950, VoltageScaleB = 0.820, VoltageScaleC = 1.060, AngleOffsetA = 0.000, AngleOffsetB = 28.000, AngleOffsetC = -22.000, FrequencyHz = 50.000, ScenarioTag = "negative-sequence-unbalance" },
                new SequenceStateSnapshot { Name = "Balanced restore", DurationSeconds = 0.250, CurrentScale = 1.000, VoltageScale = 1.000, FrequencyHz = 50.000, ScenarioTag = "balanced" }
            ]
        },
        new PublisherScenarioPresetChoice
        {
            Key = "zero-sequence",
            Label = "Zero sequence / residual stress — per-phase",
            ShortLabel = "Zero sequence",
            HelpText = "Residual-current/neutral scenario. Neutral channels publish only when the dataset and channel enablement include In/Vn.",
            States =
            [
                new SequenceStateSnapshot { Name = "Balanced", DurationSeconds = 0.250, CurrentScale = 1.000, VoltageScale = 1.000, CurrentScaleN = 0.000, VoltageScaleN = 0.000, FrequencyHz = 50.000, ScenarioTag = "balanced" },
                new SequenceStateSnapshot { Name = "Residual", DurationSeconds = 0.350, CurrentScale = 0.800, VoltageScale = 0.950, CurrentScaleA = 1.100, CurrentScaleB = 0.900, CurrentScaleC = 0.850, CurrentScaleN = 2.000, VoltageScaleN = 0.120, AngleOffsetN = 0.000, FrequencyHz = 50.000, ScenarioTag = "zero-sequence-residual" },
                new SequenceStateSnapshot { Name = "Balanced restore", DurationSeconds = 0.250, CurrentScale = 1.000, VoltageScale = 1.000, CurrentScaleN = 0.000, VoltageScaleN = 0.000, FrequencyHz = 50.000, ScenarioTag = "balanced" }
            ]
        },
        new PublisherScenarioPresetChoice
        {
            Key = "ct-saturation",
            Label = "CT saturation stress — clipping / DC / harmonic",
            ShortLabel = "CT saturation",
            HelpText = "Publisher-side CT saturation approximation using high current, DC offset, 2nd harmonic, and current clipping. Not a calibrated CT transient model.",
            States =
            [
                new SequenceStateSnapshot { Name = "Prefault", DurationSeconds = 0.200, CurrentScale = 1.000, VoltageScale = 1.000, FrequencyHz = 50.000, ScenarioTag = "prefault" },
                new SequenceStateSnapshot { Name = "Fault inception", DurationSeconds = 0.040, CurrentScale = 9.000, VoltageScale = 0.300, CurrentDcOffsetPercent = 45.000, CurrentHarmonicPercent = 10.000, HarmonicOrder = 2, CurrentClipPercent = 85.000, FrequencyHz = 50.000, ScenarioTag = "ct-saturation-inception" },
                new SequenceStateSnapshot { Name = "CT saturated", DurationSeconds = 0.160, CurrentScale = 7.000, VoltageScale = 0.250, AngleShiftDegrees = -10.000, CurrentDcOffsetPercent = 30.000, CurrentHarmonicPercent = 28.000, HarmonicOrder = 2, CurrentClipPercent = 60.000, FrequencyHz = 50.000, ScenarioTag = "ct-saturation-clipped" },
                new SequenceStateSnapshot { Name = "Recovery", DurationSeconds = 0.300, CurrentScale = 1.000, VoltageScale = 1.000, FrequencyHz = 50.000, ScenarioTag = "recovery" }
            ]
        },
        new PublisherScenarioPresetChoice
        {
            Key = "vt-fuse-a",
            Label = "VT fuse fail A-phase — per-phase",
            ShortLabel = "VT fuse A",
            HelpText = "Per-phase VT fuse fail approximation: phase-A voltage collapses while B/C remain available.",
            States =
            [
                new SequenceStateSnapshot { Name = "Normal VT", DurationSeconds = 0.250, CurrentScale = 1.000, VoltageScale = 1.000, FrequencyHz = 50.000, ScenarioTag = "normal-vt" },
                new SequenceStateSnapshot { Name = "A fuse fail", DurationSeconds = 0.300, CurrentScale = 0.800, VoltageScale = 1.000, VoltageScaleA = 0.020, VoltageScaleB = 1.000, VoltageScaleC = 1.000, FrequencyHz = 50.000, ScenarioTag = "vt-fuse-a" },
                new SequenceStateSnapshot { Name = "VT restored", DurationSeconds = 0.250, CurrentScale = 1.000, VoltageScale = 1.000, FrequencyHz = 50.000, ScenarioTag = "vt-restored" }
            ]
        },
        new PublisherScenarioPresetChoice
        {
            Key = "harmonic-injection",
            Label = "Harmonic injection — 5th harmonic",
            ShortLabel = "5th harmonic",
            HelpText = "Publisher-side harmonic approximation for power-quality subscriber checks. Fundamental remains balanced.",
            States =
            [
                new SequenceStateSnapshot { Name = "Fundamental", DurationSeconds = 0.250, CurrentScale = 1.000, VoltageScale = 1.000, FrequencyHz = 50.000, ScenarioTag = "fundamental" },
                new SequenceStateSnapshot { Name = "5th harmonic", DurationSeconds = 0.500, CurrentScale = 1.000, VoltageScale = 1.000, CurrentHarmonicPercent = 18.000, VoltageHarmonicPercent = 6.000, HarmonicOrder = 5, FrequencyHz = 50.000, ScenarioTag = "harmonic-5" },
                new SequenceStateSnapshot { Name = "Clean restore", DurationSeconds = 0.250, CurrentScale = 1.000, VoltageScale = 1.000, FrequencyHz = 50.000, ScenarioTag = "clean" }
            ]
        },
        new PublisherScenarioPresetChoice
        {
            Key = "dc-offset-transient",
            Label = "DC offset transient — decaying steps",
            ShortLabel = "DC offset",
            HelpText = "DC offset approximation using stepped states with decreasing offset magnitude.",
            States =
            [
                new SequenceStateSnapshot { Name = "Prefault", DurationSeconds = 0.150, CurrentScale = 1.000, VoltageScale = 1.000, FrequencyHz = 50.000, ScenarioTag = "prefault" },
                new SequenceStateSnapshot { Name = "DC offset high", DurationSeconds = 0.080, CurrentScale = 5.000, VoltageScale = 0.500, CurrentDcOffsetPercent = 60.000, FrequencyHz = 50.000, ScenarioTag = "dc-offset-high" },
                new SequenceStateSnapshot { Name = "DC offset mid", DurationSeconds = 0.120, CurrentScale = 4.000, VoltageScale = 0.700, CurrentDcOffsetPercent = 30.000, FrequencyHz = 50.000, ScenarioTag = "dc-offset-mid" },
                new SequenceStateSnapshot { Name = "DC offset low", DurationSeconds = 0.160, CurrentScale = 2.500, VoltageScale = 0.900, CurrentDcOffsetPercent = 12.000, FrequencyHz = 50.000, ScenarioTag = "dc-offset-low" },
                new SequenceStateSnapshot { Name = "Recovery", DurationSeconds = 0.250, CurrentScale = 1.000, VoltageScale = 1.000, FrequencyHz = 50.000, ScenarioTag = "recovery" }
            ]
        },
        new PublisherScenarioPresetChoice
        {
            Key = "frequency-steps",
            Label = "Frequency steps — 49 to 51 Hz",
            ShortLabel = "Frequency steps",
            HelpText = "Discrete frequency-step publisher scenario for testing subscriber tracking. This is a step sequence, not a continuous ramp waveform.",
            States =
            [
                new SequenceStateSnapshot { Name = "49.0 Hz", DurationSeconds = 0.200, CurrentScale = 1.000, VoltageScale = 1.000, FrequencyHz = 49.000, ScenarioTag = "frequency-49" },
                new SequenceStateSnapshot { Name = "49.5 Hz", DurationSeconds = 0.200, CurrentScale = 1.000, VoltageScale = 1.000, FrequencyHz = 49.500, ScenarioTag = "frequency-49-5" },
                new SequenceStateSnapshot { Name = "50.0 Hz", DurationSeconds = 0.200, CurrentScale = 1.000, VoltageScale = 1.000, FrequencyHz = 50.000, ScenarioTag = "frequency-50" },
                new SequenceStateSnapshot { Name = "50.5 Hz", DurationSeconds = 0.200, CurrentScale = 1.000, VoltageScale = 1.000, FrequencyHz = 50.500, ScenarioTag = "frequency-50-5" },
                new SequenceStateSnapshot { Name = "51.0 Hz", DurationSeconds = 0.200, CurrentScale = 1.000, VoltageScale = 1.000, FrequencyHz = 51.000, ScenarioTag = "frequency-51" }
            ]
        },
        new PublisherScenarioPresetChoice
        {
            Key = "phase-jump",
            Label = "Phase jump — ±20 degrees",
            ShortLabel = "Phase jump",
            HelpText = "Balanced three-phase phase-angle jump sequence for subscriber behavior checks.",
            States =
            [
                new SequenceStateSnapshot { Name = "Reference", DurationSeconds = 0.250, CurrentScale = 1.000, VoltageScale = 1.000, AngleShiftDegrees = 0.000, FrequencyHz = 50.000, ScenarioTag = "reference" },
                new SequenceStateSnapshot { Name = "+20 deg jump", DurationSeconds = 0.180, CurrentScale = 1.000, VoltageScale = 1.000, AngleShiftDegrees = 20.000, FrequencyHz = 50.000, ScenarioTag = "phase-jump-plus20" },
                new SequenceStateSnapshot { Name = "-20 deg jump", DurationSeconds = 0.180, CurrentScale = 1.000, VoltageScale = 1.000, AngleShiftDegrees = -20.000, FrequencyHz = 50.000, ScenarioTag = "phase-jump-minus20" },
                new SequenceStateSnapshot { Name = "Reference restore", DurationSeconds = 0.250, CurrentScale = 1.000, VoltageScale = 1.000, AngleShiftDegrees = 0.000, FrequencyHz = 50.000, ScenarioTag = "reference" }
            ]
        },
        new PublisherScenarioPresetChoice
        {
            Key = "load-reversal",
            Label = "Load reversal — 180 degree shift",
            ShortLabel = "Load reversal",
            HelpText = "Balanced 180-degree angle reversal approximation for directional element lab checks.",
            States =
            [
                new SequenceStateSnapshot { Name = "Forward", DurationSeconds = 0.250, CurrentScale = 1.000, VoltageScale = 1.000, AngleShiftDegrees = 0.000, FrequencyHz = 50.000, ScenarioTag = "forward" },
                new SequenceStateSnapshot { Name = "Reverse", DurationSeconds = 0.300, CurrentScale = 1.000, VoltageScale = 1.000, AngleShiftDegrees = 180.000, FrequencyHz = 50.000, ScenarioTag = "reverse" },
                new SequenceStateSnapshot { Name = "Forward restore", DurationSeconds = 0.250, CurrentScale = 1.000, VoltageScale = 1.000, AngleShiftDegrees = 0.000, FrequencyHz = 50.000, ScenarioTag = "forward" }
            ]
        }
    ];

    public IReadOnlyList<string> ManualSetModes { get; } =
    [
        DirectSetMode,
        LineLineSetMode,
        SymmetricalSetMode
    ];

    public IReadOnlyList<SvSyncPolicyChoice> SyncPolicyChoices { get; } =
    [
        new()
        {
            Mode = SvSyncPolicyMode.GlobalCompatibility,
            Label = "Global compatibility — smpSynch=2",
            ShortLabel = "global compatibility",
            HelpText = "Makes the SV stream report smpSynch=2 so stricter subscribers can accept point-to-point lab traffic. This is compatibility behavior, not proof of real PTP accuracy."
        },
        new()
        {
            Mode = SvSyncPolicyMode.LocalCompatibility,
            Label = "Local compatibility — smpSynch=1",
            ShortLabel = "local compatibility",
            HelpText = "Makes the SV stream report smpSynch=1 for subscribers that accept locally synchronized lab traffic."
        },
        new()
        {
            Mode = SvSyncPolicyMode.HonestUnsynchronized,
            Label = "Honest unsynchronized — smpSynch=0",
            ShortLabel = "honest unsync",
            HelpText = "Publishes smpSynch=0. Use this when you want the SV stream to declare that timing is not synchronized."
        },
        new()
        {
            Mode = SvSyncPolicyMode.ExternalPtpAuto,
            Label = "External PTP auto — monitor based",
            ShortLabel = "external PTP auto",
            HelpText = "Derives smpSynch from observed external PTP health. If no valid PTP evidence is visible, the stream does not claim global synchronization."
        }
    ];

    public IReadOnlyList<SampleQualityChoice> SampleQualityChoices { get; } =
    [
        new()
        {
            Key = "good",
            Label = "Good quality",
            ShortLabel = "good",
            HelpText = "Default SV quality: valid measurement for normal publisher output.",
            Quality = SampledValueQuality.Good
        },
        new()
        {
            Key = "invalid",
            Label = "Invalid",
            ShortLabel = "invalid",
            HelpText = "Sets the IEC 61850 quality validity bits to invalid for quality fields in the SV dataset.",
            Quality = SampledValueQuality.Invalid
        },
        new()
        {
            Key = "questionable",
            Label = "Questionable",
            ShortLabel = "questionable",
            HelpText = "Sets the quality validity bits to questionable for relay behavior tests.",
            Quality = SampledValueQuality.Questionable
        },
        new()
        {
            Key = "oldData",
            Label = "Old data",
            ShortLabel = "oldData",
            HelpText = "Publishes good validity with the oldData detail bit set.",
            Quality = SampledValueQuality.OldDataGood
        },
        new()
        {
            Key = "test",
            Label = "Test bit",
            ShortLabel = "test",
            HelpText = "Publishes good validity with the test bit set.",
            Quality = SampledValueQuality.TestGood
        },
        new()
        {
            Key = "operatorBlocked",
            Label = "Operator blocked",
            ShortLabel = "operatorBlocked",
            HelpText = "Publishes good validity with the operatorBlocked detail bit set.",
            Quality = SampledValueQuality.OperatorBlockedGood
        }
    ];

    public IReadOnlyList<PublisherSignalSource> SignalSources { get; } =
    [
        PublisherSignalSource.Manual,
        PublisherSignalSource.ComtradeReplay
    ];

    public IReadOnlyList<PtpPublisherMode> PtpPublisherModes { get; } =
    [
        AR.Iec61850.SvPublisher.Models.PtpPublisherMode.MonitorOnly,
        AR.Iec61850.SvPublisher.Models.PtpPublisherMode.LabPublisher
    ];

    public IReadOnlyList<InjectionMode> Modes { get; } =
    [
        InjectionMode.Manual,
        InjectionMode.Ramp,
        InjectionMode.Sequencer
    ];

    public ICommand OpenSclCommand { get; }
    public ICommand ImportComtradeCommand { get; }
    public ICommand ClearComtradeCommand { get; }
    public ICommand RefreshAdaptersCommand { get; }
    public ICommand RunPreflightCommand { get; }
    public ICommand SaveProfileCommand { get; }
    public ICommand ExportGeneratedPcapCommand { get; }
    public ICommand ExportPublisherEvidenceReportCommand { get; }
    public ICommand ApplyScenarioPresetCommand { get; }
    public ICommand RunDryCommand { get; }
    public ICommand RunLiveCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand ApplyBalancedDefaultsCommand { get; }
    public ICommand AddSequenceStateCommand { get; }
    public ICommand RemoveSequenceStateCommand { get; }
    public ICommand SelectSequenceStateCommand { get; }
    public ICommand AddRampStateCommand { get; }
    public ICommand RemoveRampStateCommand { get; }
    public ICommand ApplyNominalCommand { get; }
    public ICommand ZeroOutputCommand { get; }
    public ICommand EqualMagnitudesCommand { get; }
    public ICommand HundredPercentLoadCommand { get; }
    public ICommand FiftyPercentLoadCommand { get; }
    public ICommand UnloadCommand { get; }
    public ICommand BalanceAnglesCommand { get; }
    public ICommand NominalValueFromContextCommand { get; }
    public ICommand ZeroFromContextCommand { get; }
    public ICommand EqualMagnitudesFromContextCommand { get; }
    public ICommand LineAngleFromContextCommand { get; }
    public ICommand BalanceAnglesFromContextCommand { get; }
    public ICommand ReverseRotationFromContextCommand { get; }
    public ICommand NominalFrequencyFromContextCommand { get; }
    public ICommand DcFrequencyFromContextCommand { get; }
    public ICommand EqualFrequenciesFromContextCommand { get; }
    public ICommand ReverseRotationCommand { get; }
    public ICommand SetSignalNamingCommand { get; }
    public ICommand CopyTableCommand { get; }
    public ICommand PasteTableCommand { get; }

    public string SclPath
    {
        get => _sclPath;
        private set => SetProperty(ref _sclPath, value);
    }

    public string SclSummary
    {
        get => _sclSummary;
        private set => SetProperty(ref _sclSummary, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string PublishText
    {
        get => _publishText;
        private set => SetProperty(ref _publishText, value);
    }

    public string TxTimingHealthText
    {
        get => _txTimingHealthText;
        private set => SetProperty(ref _txTimingHealthText, value);
    }

    public string AdapterStatusText => SelectedAdapter is null
        ? "Adapter: not selected"
        : $"Adapter: {SelectedAdapter.DisplayName}";

    public string PtpStatusText
    {
        get => _ptpStatusText;
        private set
        {
            if (SetProperty(ref _ptpStatusText, value))
                OnPropertyChanged(nameof(SyncStatusBarText));
        }
    }

    public string SmpSynchStatusText
    {
        get => _smpSynchStatusText;
        private set
        {
            if (SetProperty(ref _smpSynchStatusText, value))
                OnPropertyChanged(nameof(SyncStatusBarText));
        }
    }

    public string PtpPublisherStatusText
    {
        get => _ptpPublisherStatusText;
        private set
        {
            if (SetProperty(ref _ptpPublisherStatusText, value))
                OnPropertyChanged(nameof(SyncStatusBarText));
        }
    }

    public string SyncStatusBarText => $"{PtpStatusText}  |  {PtpPublisherStatusText}  |  {SmpSynchStatusText}";

    public string EvidenceText
    {
        get => _evidenceText;
        private set => SetProperty(ref _evidenceText, value);
    }

    public string LiveApplyText
    {
        get => _liveApplyText;
        private set => SetProperty(ref _liveApplyText, value);
    }

    public string LivePreflightSummaryText
    {
        get => _livePreflightSummaryText;
        private set
        {
            if (SetProperty(ref _livePreflightSummaryText, value))
                OnPropertyChanged(nameof(LiveSafetyStatusText));
        }
    }

    public bool HasLivePreflightErrors => LivePreflightDiagnostics.Any(diagnostic => diagnostic.Severity == LivePreflightSeverity.Error);
    public bool HasLivePreflightWarnings => LivePreflightDiagnostics.Any(diagnostic => diagnostic.Severity == LivePreflightSeverity.Warning);
    public int LivePreflightErrorCount => LivePreflightDiagnostics.Count(diagnostic => diagnostic.Severity == LivePreflightSeverity.Error);
    public int LivePreflightWarningCount => LivePreflightDiagnostics.Count(diagnostic => diagnostic.Severity == LivePreflightSeverity.Warning);
    public int LivePreflightInfoCount => LivePreflightDiagnostics.Count(diagnostic => diagnostic.Severity == LivePreflightSeverity.Info);

    public bool IsConfigComtradeWorkspaceVisible => SelectedPublisherSlot?.SignalSource == PublisherSignalSource.ComtradeReplay;
    public bool IsConfigManualWorkspaceVisible => !IsConfigComtradeWorkspaceVisible && Mode == InjectionMode.Manual;
    public bool IsConfigRampWorkspaceVisible => !IsConfigComtradeWorkspaceVisible && Mode == InjectionMode.Ramp;
    public bool IsConfigSequencerWorkspaceVisible => !IsConfigComtradeWorkspaceVisible && Mode == InjectionMode.Sequencer;

    public string SelectedStreamHeaderText => SelectedPublisherSlot is null
        ? "No stream selected"
        : $"SV{SelectedPublisherSlot.Index} — {SelectedPublisherSlot.StreamIdOrFallback}";

    public string LiveSafetyStatusText => HasLivePreflightErrors
        ? $"LIVE CHECK: FATAL · {LivePreflightSummaryText}"
        : HasLivePreflightWarnings
            ? $"LIVE CHECK: OK WITH WARNING · {LivePreflightSummaryText}"
            : $"LIVE CHECK: QUICK LOOPTEST · {LivePreflightSummaryText}";

    public SvPublisherSlotViewModel? SelectedPublisherSlot
    {
        get => _selectedPublisherSlot;
        set
        {
            if (ReferenceEquals(_selectedPublisherSlot, value))
                return;

            SaveCurrentPublisherSlot();
            if (SetProperty(ref _selectedPublisherSlot, value) && value is not null)
            {
                LoadPublisherSlot(value);
                RaiseConfigWorkspaceStateChanged();
            }
        }
    }


    private void OnPublisherSlotPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, SelectedPublisherSlot))
            return;

        if (e.PropertyName is nameof(SvPublisherSlotViewModel.SignalSource)
            or nameof(SvPublisherSlotViewModel.IsEnabled)
            or nameof(SvPublisherSlotViewModel.SelectedStream)
            or nameof(SvPublisherSlotViewModel.StreamId)
            or nameof(SvPublisherSlotViewModel.StreamControlBlock)
            or nameof(SvPublisherSlotViewModel.AppIdText)
            or nameof(SvPublisherSlotViewModel.UseVlan)
            or nameof(SvPublisherSlotViewModel.VlanId)
            or nameof(SvPublisherSlotViewModel.SampleRateHz)
            or nameof(SvPublisherSlotViewModel.ComtradeSummary)
            or nameof(SvPublisherSlotViewModel.ComtradePath))
        {
            RaiseConfigWorkspaceStateChanged();
        }
    }

    private void RaiseConfigWorkspaceStateChanged()
    {
        OnPropertyChanged(nameof(IsConfigComtradeWorkspaceVisible));
        OnPropertyChanged(nameof(IsConfigManualWorkspaceVisible));
        OnPropertyChanged(nameof(IsConfigRampWorkspaceVisible));
        OnPropertyChanged(nameof(IsConfigSequencerWorkspaceVisible));
        OnPropertyChanged(nameof(SelectedStreamHeaderText));
    }

    public SampleRatePreset? SelectedSampleRatePreset
    {
        get => _selectedSampleRatePreset;
        set
        {
            if (!SetProperty(ref _selectedSampleRatePreset, value) || value is null)
                return;

            SampleRateHz = value.SampleRateHz;
            NominalFrequencyHz = value.NominalFrequencyHz;
            if (!_isLoadingPublisherSlot && SelectedPublisherSlot is { } slot)
                slot.SampleRatePresetKey = value.Key;
        }
    }

    public SampleQualityChoice SelectedSampleQualityChoice
    {
        get => _selectedSampleQualityChoice ?? SampleQualityChoices.First(choice => choice.Key == "good");
        set
        {
            if (value is null)
                return;

            if (SetProperty(ref _selectedSampleQualityChoice, value))
            {
                OnPropertyChanged(nameof(SampleQualityHelpText));
                OnPropertyChanged(nameof(SampleQualityStatusText));
                if (!_isLoadingPublisherSlot && SelectedPublisherSlot is { } slot)
                    slot.SampleQualityKey = value.Key;
                AppendEvent($"SV quality changed to {value.Label}.");
            }
        }
    }

    public string SampleQualityHelpText => SelectedSampleQualityChoice.HelpText;

    public string SampleQualityStatusText => $"q={SelectedSampleQualityChoice.ShortLabel}";

    public PublisherScenarioPresetChoice? SelectedScenarioPresetChoice
    {
        get => _selectedScenarioPresetChoice;
        set
        {
            if (SetProperty(ref _selectedScenarioPresetChoice, value))
            {
                OnPropertyChanged(nameof(ScenarioPresetHelpText));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string ScenarioPresetHelpText => SelectedScenarioPresetChoice?.HelpText ?? "Select a publisher scenario preset, then apply it to the State Sequencer.";

    private SampleQualityChoice ResolveSampleQualityChoice(string? key)
        => SampleQualityChoices.FirstOrDefault(choice => string.Equals(choice.Key, key, StringComparison.OrdinalIgnoreCase))
           ?? SampleQualityChoices.First(choice => choice.Key == "good");

    public SvStreamChoice? SelectedStream
    {
        get => _selectedStream;
        set
        {
            if (SetProperty(ref _selectedStream, value))
            {
                ApplySelectedStream(value);
                if (!_isLoadingPublisherSlot)
                    SaveCurrentPublisherSlot();
            }
        }
    }

    public AdapterChoice? SelectedAdapter
    {
        get => _selectedAdapter;
        set
        {
            if (!SetProperty(ref _selectedAdapter, value))
                return;

            OnPropertyChanged(nameof(AdapterStatusText));

            if (value is not null && !string.IsNullOrWhiteSpace(value.MacAddress))
                SourceMac = value.MacAddress;
        }
    }

    public SignalChannelViewModel? SelectedRampChannel
    {
        get => _selectedRampChannel;
        set
        {
            if (!SetProperty(ref _selectedRampChannel, value) || value is null || SelectedRampState is null)
                return;

            if (_isSyncingRampSignalChoice)
                return;

            var choice = RampSignalChoices.FirstOrDefault(candidate => string.Equals(candidate.KeyCsv, value.Key, StringComparison.OrdinalIgnoreCase));
            if (choice is not null)
                SelectedRampSignalChoice = choice;
        }
    }

    public RampSignalChoice? SelectedRampSignalChoice
    {
        get => _selectedRampSignalChoice;
        set
        {
            if (!SetProperty(ref _selectedRampSignalChoice, value) || value is null)
                return;

            ApplyRampSignalChoiceToSelectedState(value, resetFromBase: true);
        }
    }

    public RampStateViewModel? SelectedRampState
    {
        get => _selectedRampState;
        set
        {
            if (!SetProperty(ref _selectedRampState, value))
                return;

            if (value is not null)
            {
                SyncRampSignalChoiceFromState(value);
                RampTargetMagnitude = value.To;
                RampDurationSeconds = value.TimeSeconds;
            }

            UpdateRampPreview();
        }
    }

    public SequenceStateViewModel? SelectedSequenceState
    {
        get => _selectedSequenceState;
        set
        {
            if (!SetProperty(ref _selectedSequenceState, value))
                return;

            foreach (var state in SequenceStates)
                state.IsSelected = ReferenceEquals(state, value);

            UpdateSequencePreview();
        }
    }

    public string StreamId
    {
        get => _streamId;
        set => SetProperty(ref _streamId, value);
    }

    public string StreamControlBlock
    {
        get => _streamControlBlock;
        private set => SetProperty(ref _streamControlBlock, value);
    }

    public string DataSetReference
    {
        get => _dataSetReference;
        set => SetProperty(ref _dataSetReference, value);
    }

    public string AppIdText
    {
        get => _appIdText;
        set => SetProperty(ref _appIdText, value);
    }

    public string DestinationMac
    {
        get => _destinationMac;
        set => SetProperty(ref _destinationMac, value);
    }

    public string SourceMac
    {
        get => _sourceMac;
        set => SetProperty(ref _sourceMac, value);
    }

    public bool UseVlan
    {
        get => _useVlan;
        set => SetProperty(ref _useVlan, value);
    }

    public int VlanId
    {
        get => _vlanId;
        set => SetProperty(ref _vlanId, value);
    }

    public int VlanPriority
    {
        get => _vlanPriority;
        set => SetProperty(ref _vlanPriority, value);
    }

    public double SampleRateHz
    {
        get => _sampleRateHz;
        set => SetProperty(ref _sampleRateHz, value);
    }

    public double NominalFrequencyHz
    {
        get => _nominalFrequencyHz;
        set
        {
            if (!SetProperty(ref _nominalFrequencyHz, value))
                return;

            if (LinkFrequencies)
                SetAllManualFrequencies(value);
        }
    }

    public double CurrentDlsb
    {
        get => _currentDlsb;
        set => SetProperty(ref _currentDlsb, value);
    }

    public double VoltageDlsb
    {
        get => _voltageDlsb;
        set => SetProperty(ref _voltageDlsb, value);
    }

    public double DurationSeconds
    {
        get => _durationSeconds;
        set => SetProperty(ref _durationSeconds, value);
    }

    public bool Continuous
    {
        get => _continuous;
        set => SetProperty(ref _continuous, value);
    }

    public bool LoopSequence
    {
        get => _loopSequence;
        set => SetProperty(ref _loopSequence, value);
    }

    public bool IsLiveArmed
    {
        get => _isLiveArmed;
        set => SetProperty(ref _isLiveArmed, value);
    }

    public bool AutoApplyWhileRunning
    {
        get => _autoApplyWhileRunning;
        set
        {
            if (SetProperty(ref _autoApplyWhileRunning, value))
                LiveApplyText = value ? "Auto apply ready." : "Auto apply paused. Edits remain visible but publisher keeps previous setpoints.";
        }
    }

    public bool LinkFrequencies
    {
        get => _linkFrequencies;
        set
        {
            if (SetProperty(ref _linkFrequencies, value) && value)
            {
                SetAllManualFrequencies(NominalFrequencyHz);
                AppendEvent("Frequencies linked to nominal frequency.");
            }
        }
    }

    public SvSyncPolicyMode SyncPolicyMode
    {
        get => _syncPolicyMode;
        set
        {
            var normalized = NormalizeSyncPolicyMode(value);
            if (SetProperty(ref _syncPolicyMode, normalized))
            {
                _selectedSyncPolicyChoice = ResolveSyncPolicyChoice(normalized);
                OnPropertyChanged(nameof(SelectedSyncPolicyChoice));
                OnPropertyChanged(nameof(SyncPolicyHelpText));
                OnPropertyChanged(nameof(SyncPolicyShortLabel));
                OnPropertyChanged(nameof(IsCompatibilitySyncMode));
                SmpSynchStatusText = FormatSmpSynchStatus(ResolveSampleSynchronization(null));
                AppendEvent($"smpSynch behavior changed to {_selectedSyncPolicyChoice.Label}.");
            }
        }
    }

    public SvSyncPolicyChoice SelectedSyncPolicyChoice
    {
        get => _selectedSyncPolicyChoice ?? ResolveSyncPolicyChoice(SyncPolicyMode);
        set
        {
            if (value is null)
                return;

            SyncPolicyMode = value.Mode;
        }
    }

    public string SyncPolicyHelpText => ResolveSyncPolicyChoice(SyncPolicyMode).HelpText;

    public string SyncPolicyShortLabel => ResolveSyncPolicyChoice(SyncPolicyMode).ShortLabel;

    public bool IsCompatibilitySyncMode => SyncPolicyMode is SvSyncPolicyMode.GlobalCompatibility or SvSyncPolicyMode.LocalCompatibility;

    public int ExpectedPtpDomain
    {
        get => _expectedPtpDomain;
        set => SetProperty(ref _expectedPtpDomain, Math.Clamp(value, 0, 255));
    }

    public bool PtpAllowLocalFallback
    {
        get => _ptpAllowLocalFallback;
        set => SetProperty(ref _ptpAllowLocalFallback, value);
    }

    public PtpPublisherMode PtpPublisherMode
    {
        get => _ptpPublisherMode;
        set
        {
            if (SetProperty(ref _ptpPublisherMode, value))
            {
                PtpPublisherStatusText = value == AR.Iec61850.SvPublisher.Models.PtpPublisherMode.LabPublisher ? "PTP TX: lab traffic armed" : "PTP TX: off";
                AppendEvent($"PTP traffic mode changed to {value}.");
            }
        }
    }

    public string PtpClockIdentityText
    {
        get => _ptpClockIdentityText;
        set => SetProperty(ref _ptpClockIdentityText, value);
    }

    public int PtpAnnounceIntervalMs
    {
        get => _ptpAnnounceIntervalMs;
        set => SetProperty(ref _ptpAnnounceIntervalMs, Math.Clamp(value, 100, 10000));
    }

    public int PtpSyncIntervalMs
    {
        get => _ptpSyncIntervalMs;
        set => SetProperty(ref _ptpSyncIntervalMs, Math.Clamp(value, 20, 5000));
    }

    public bool PtpRespondToPeerDelay
    {
        get => _ptpRespondToPeerDelay;
        set => SetProperty(ref _ptpRespondToPeerDelay, value);
    }

    public bool IsPublishing
    {
        get => _isPublishing;
        private set
        {
            if (SetProperty(ref _isPublishing, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    public bool IsContextValueColumn
        => string.Equals(_contextColumnHeader, "Value", StringComparison.OrdinalIgnoreCase);

    public bool IsContextAngleColumn
        => string.Equals(_contextColumnHeader, "Angle", StringComparison.OrdinalIgnoreCase);

    public bool IsContextFrequencyColumn
        => string.Equals(_contextColumnHeader, "Freq", StringComparison.OrdinalIgnoreCase);

    public bool IsContextSignalColumn
        => string.Equals(_contextColumnHeader, "Signal", StringComparison.OrdinalIgnoreCase);

    public string SignalNamingScheme
    {
        get => _signalNamingScheme;
        private set => SetProperty(ref _signalNamingScheme, value);
    }

    public InjectionMode Mode
    {
        get => _mode;
        set
        {
            if (!SetProperty(ref _mode, value))
                return;

            OnPropertyChanged(nameof(IsManualWorkspaceVisible));
            OnPropertyChanged(nameof(IsRampWorkspaceVisible));
            OnPropertyChanged(nameof(IsSequencerWorkspaceVisible));
            RaiseConfigWorkspaceStateChanged();
            OnPropertyChanged(nameof(WorkspaceTitle));
            OnPropertyChanged(nameof(WorkspaceSubtitle));
            AppendEvent($"Workspace changed to {value}.");
        }
    }

    public bool IsManualWorkspaceVisible => Mode == InjectionMode.Manual;

    public bool IsRampWorkspaceVisible => Mode == InjectionMode.Ramp;

    public bool IsSequencerWorkspaceVisible => Mode == InjectionMode.Sequencer;

    public string WorkspaceTitle => Mode switch
    {
        InjectionMode.Ramp => "Ramping",
        InjectionMode.Sequencer => "State Sequencer",
        _ => "Quick Manual"
    };

    public string WorkspaceSubtitle => Mode switch
    {
        InjectionMode.Ramp => "Step ramp profile, analog-output detail, and time-signal preview.",
        InjectionMode.Sequencer => "Horizontal state table with selected-state analog detail, phasor, and time-signal preview.",
        _ => "Fast manual SV injection with live numeric commit and unit formatting."
    };

    public string ManualSetMode
    {
        get => _manualSetMode;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            if (SetProperty(ref _manualSetMode, value))
            {
                RebuildManualRowsFromChannels();
                AppendEvent($"Manual set mode changed to {value}.");
            }
        }
    }

    public double RampTargetMagnitude
    {
        get => _rampTargetMagnitude;
        set
        {
            if (SetProperty(ref _rampTargetMagnitude, value) && SelectedRampState is not null)
            {
                SelectedRampState.To = value;
                UpdateRampPreview();
            }
        }
    }

    public double RampDurationSeconds
    {
        get => _rampDurationSeconds;
        set
        {
            var coerced = Math.Max(0.001, value);
            if (SetProperty(ref _rampDurationSeconds, coerced) && SelectedRampState is not null)
            {
                SelectedRampState.TimeSeconds = coerced;
                UpdateRampPreview();
            }
        }
    }

    public int DataSetEntryCount
    {
        get => _dataSetEntryCount;
        private set => SetProperty(ref _dataSetEntryCount, value);
    }

    public int MappedSignalCount
    {
        get => _mappedSignalCount;
        private set => SetProperty(ref _mappedSignalCount, value);
    }

    public int PayloadBytes
    {
        get => _payloadBytes;
        private set => SetProperty(ref _payloadBytes, value);
    }


    private static ObservableCollection<SignalChannelViewModel> CreatePreviewChannels()
        =>
        [
            new SignalChannelViewModel("Va", "V L1-E", "V", "V", NominalVoltageLn, 0, 50),
            new SignalChannelViewModel("Vb", "V L2-E", "V", "V", NominalVoltageLn, -120, 50),
            new SignalChannelViewModel("Vc", "V L3-E", "V", "V", NominalVoltageLn, 120, 50),
            new SignalChannelViewModel("Ia", "I L1", "I", "A", NominalCurrent, 0, 50),
            new SignalChannelViewModel("Ib", "I L2", "I", "A", NominalCurrent, -120, 50),
            new SignalChannelViewModel("Ic", "I L3", "I", "A", NominalCurrent, 120, 50)
        ];

    private void AttachRampState(RampStateViewModel state)
        => state.PropertyChanged += RampState_PropertyChanged;

    private void AttachSequenceState(SequenceStateViewModel state)
        => state.PropertyChanged += SequenceState_PropertyChanged;

    private void DetachRampState(RampStateViewModel state)
        => state.PropertyChanged -= RampState_PropertyChanged;

    private void DetachSequenceState(SequenceStateViewModel state)
        => state.PropertyChanged -= SequenceState_PropertyChanged;

    private void RampState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(RampTotalTimeSeconds));
        OnPropertyChanged(nameof(RampTotalTimeText));

        if (ReferenceEquals(sender, SelectedRampState))
        {
            if (SelectedRampState is not null)
            {
                if (string.Equals(e.PropertyName, nameof(RampStateViewModel.SignalKey), StringComparison.Ordinal))
                    SyncRampSignalChoiceFromState(SelectedRampState);

                _rampTargetMagnitude = SelectedRampState.To;
                _rampDurationSeconds = SelectedRampState.TimeSeconds;
                OnPropertyChanged(nameof(RampTargetMagnitude));
                OnPropertyChanged(nameof(RampDurationSeconds));
            }

            UpdateRampPreview();
        }
    }

    private void SequenceState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (ReferenceEquals(sender, SelectedSequenceState))
            UpdateSequencePreview();
    }

    private void SelectSequenceState(SequenceStateViewModel? state)
    {
        if (state is not null)
            SelectedSequenceState = state;
    }

    private void SyncRampSignalChoiceFromState(RampStateViewModel state)
    {
        var choice = RampSignalChoices.FirstOrDefault(candidate => string.Equals(candidate.KeyCsv, state.SignalKey, StringComparison.OrdinalIgnoreCase));
        if (choice is null)
            return;

        _isSyncingRampSignalChoice = true;
        try
        {
            _selectedRampSignalChoice = choice;
            OnPropertyChanged(nameof(SelectedRampSignalChoice));
            _selectedRampChannel = Channels.FirstOrDefault(channel => string.Equals(channel.Key, choice.Keys.FirstOrDefault(), StringComparison.OrdinalIgnoreCase));
            OnPropertyChanged(nameof(SelectedRampChannel));
        }
        finally
        {
            _isSyncingRampSignalChoice = false;
        }
    }

    private void ApplyRampSignalChoiceToSelectedState(RampSignalChoice choice, bool resetFromBase)
    {
        if (SelectedRampState is not { } state)
            return;

        state.SignalKey = choice.KeyCsv;
        state.SignalName = choice.Name;
        state.Quantity = choice.Quantity;

        _isSyncingRampSignalChoice = true;
        try
        {
            SelectedRampChannel = Channels.FirstOrDefault(channel => string.Equals(channel.Key, choice.Keys.FirstOrDefault(), StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _isSyncingRampSignalChoice = false;
        }

        if (resetFromBase && ResolveFirstRampBaseChannel(choice) is { } baseChannel)
        {
            state.From = baseChannel.Magnitude;
            if (Math.Abs(state.To - state.From) < 0.000001)
            {
                var bump = baseChannel.Kind == "I" ? 1.0 : 10.0;
                state.To = state.From + bump;
            }
        }

        RampTargetMagnitude = state.To;
        RampDurationSeconds = state.TimeSeconds;
        UpdateRampPreview();
    }

    private SignalChannelViewModel? ResolveFirstRampBaseChannel(RampSignalChoice choice)
        => choice.Keys
            .Select(key => Channels.FirstOrDefault(channel => string.Equals(channel.Key, key, StringComparison.OrdinalIgnoreCase)))
            .FirstOrDefault(channel => channel is not null);

    private void UpdateRampPreview()
    {
        if (RampPreviewChannels.Count == 0)
            return;

        CopyManualPreview(Channels, RampPreviewChannels);

        if (SelectedRampState is { } state)
        {
            foreach (var channel in RampPreviewChannels.Where(c => state.AppliesToChannel(c.Key)))
            {
                channel.Magnitude = Math.Max(0, state.To);
                channel.IsEnabled = state.To > 0;
            }
        }
    }

    private void UpdateSequencePreview()
    {
        if (SequencePreviewChannels.Count == 0)
            return;

        var state = SelectedSequenceState;
        var voltage = state is null ? NominalVoltageLn : NominalVoltageLn * Math.Max(0, state.VoltageScale);
        var current = state is null ? NominalCurrent : Math.Max(0, state.CurrentScale);
        var shift = state?.AngleShiftDegrees ?? 0;
        var frequency = state?.FrequencyHz ?? NominalFrequencyHz;

        SetPreviewChannel(SequencePreviewChannels, "Va", DisplaySignalName("Va", "V L1-E"), voltage, shift, voltage > 0, frequency);
        SetPreviewChannel(SequencePreviewChannels, "Vb", DisplaySignalName("Vb", "V L2-E"), voltage, shift - 120, voltage > 0, frequency);
        SetPreviewChannel(SequencePreviewChannels, "Vc", DisplaySignalName("Vc", "V L3-E"), voltage, shift + 120, voltage > 0, frequency);
        SetPreviewChannel(SequencePreviewChannels, "Ia", DisplaySignalName("Ia", "I L1"), current, shift, current > 0, frequency);
        SetPreviewChannel(SequencePreviewChannels, "Ib", DisplaySignalName("Ib", "I L2"), current, shift - 120, current > 0, frequency);
        SetPreviewChannel(SequencePreviewChannels, "Ic", DisplaySignalName("Ic", "I L3"), current, shift + 120, current > 0, frequency);
    }

    private static void CopyManualPreview(IEnumerable<SignalChannelViewModel> source, ObservableCollection<SignalChannelViewModel> target)
    {
        foreach (var destination in target)
        {
            var sourceChannel = source.FirstOrDefault(c => string.Equals(c.Key, destination.Key, StringComparison.OrdinalIgnoreCase));
            if (sourceChannel is null)
                continue;

            destination.Name = sourceChannel.Name;
            destination.Magnitude = sourceChannel.Magnitude;
            destination.AngleDegrees = sourceChannel.AngleDegrees;
            destination.FrequencyHz = sourceChannel.FrequencyHz;
            destination.IsEnabled = sourceChannel.IsEnabled;
        }
    }

    private static void SetPreviewChannel(
        ObservableCollection<SignalChannelViewModel> channels,
        string key,
        string name,
        double magnitude,
        double angle,
        bool enabled,
        double frequency)
    {
        var channel = channels.FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.OrdinalIgnoreCase));
        if (channel is null)
            return;

        channel.Name = name;
        channel.Magnitude = magnitude;
        channel.AngleDegrees = NormalizeDegrees(angle);
        channel.FrequencyHz = frequency;
        channel.IsEnabled = enabled;
    }

    private async Task OpenSclAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open IEC 61850 SCL",
            Filter = "SCL files (*.scd;*.cid;*.icd;*.iid;*.xml)|*.scd;*.cid;*.icd;*.iid;*.xml|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var document = await Task.Run(() => new SclParser().Load(dialog.FileName)).ConfigureAwait(true);
            SclPath = dialog.FileName;
            Streams.Clear();

            for (var i = 0; i < document.SampledValuesStreams.Count; i++)
                Streams.Add(new SvStreamChoice { Index = i + 1, Stream = document.SampledValuesStreams[i] });

            for (var i = 0; i < PublisherSlots.Count; i++)
            {
                var slot = PublisherSlots[i];
                var choice = Streams.ElementAtOrDefault(i) ?? Streams.FirstOrDefault();
                ApplyStreamMetadataToSlot(slot, choice);
            }

            SelectedStream = Streams.FirstOrDefault();
            SclSummary = $"IED={document.Ieds.Count}  DataSets={document.DataSets.Count}  SV={document.SampledValuesStreams.Count}  Warnings={document.Warnings.Count}";
            StatusText = document.SampledValuesStreams.Count == 0 ? "SCL opened, no SV streams found." : "SCL opened.";
            AppendEvent($"Opened SCL: {Path.GetFileName(dialog.FileName)}");

            foreach (var warning in document.Warnings.Take(6))
                AppendEvent($"SCL warning: {warning}");

            foreach (var conflict in document.Conflicts.Take(6))
                AppendEvent($"SCL conflict: {conflict.Description}");
        }
        catch (Exception ex)
        {
            StatusText = "Open SCL failed.";
            AppendEvent(ex.Message);
        }
    }


    private async Task ImportComtradeAsync()
    {
        if (SelectedPublisherSlot is not { } slot)
            return;

        var dialog = new OpenFileDialog
        {
            Title = $"Import COMTRADE for {slot.Header}",
            Filter = "COMTRADE configuration (*.cfg)|*.cfg|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var dataset = await Task.Run(() => new ComtradeReader().Load(dialog.FileName)).ConfigureAwait(true);
            slot.ComtradeDataset = dataset;
            slot.ComtradeChannelMap = dataset.DefaultChannelMap;
            slot.ComtradePath = dialog.FileName;
            slot.ComtradeSummary = dataset.Summary;
            slot.SignalSource = PublisherSignalSource.ComtradeReplay;
            slot.IsEnabled = true;
            slot.ComtradeLoop = false;

            var rate = dataset.NominalSampleRateHz;
            if (rate > 0)
            {
                slot.SampleRateHz = rate;
                SampleRateHz = rate;
                SelectedSampleRatePreset = SampleRatePresets.FirstOrDefault(preset => Math.Abs(preset.SampleRateHz - rate) < 0.5)
                    ?? SelectedSampleRatePreset;
            }

            if (dataset.Configuration.LineFrequencyHz > 0)
            {
                slot.NominalFrequencyHz = dataset.Configuration.LineFrequencyHz;
                NominalFrequencyHz = dataset.Configuration.LineFrequencyHz;
            }

            SaveCurrentPublisherSlot();
            LoadPublisherSlot(slot);
            StatusText = "COMTRADE imported.";
            AppendEvent($"{slot.Header}: COMTRADE loaded {Path.GetFileName(dialog.FileName)}; {dataset.Summary}; mapped {dataset.DefaultChannelMap.Count} analog channel(s).");
        }
        catch (Exception ex)
        {
            StatusText = "COMTRADE import failed.";
            AppendEvent(ex.Message);
        }
    }

    private void ClearComtrade()
    {
        if (SelectedPublisherSlot is not { } slot)
            return;

        slot.SignalSource = PublisherSignalSource.Manual;
        slot.ComtradeDataset = null;
        slot.ComtradeChannelMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        slot.ComtradePath = string.Empty;
        slot.ComtradeSummary = "No COMTRADE loaded.";
        SaveCurrentPublisherSlot();
        LoadPublisherSlot(slot);
        AppendEvent($"{slot.Header}: COMTRADE replay cleared.");
    }

    private void RefreshAdapters()
    {
        try
        {
            Adapters.Clear();
            foreach (var adapter in NpcapAdapterCatalog.ListAdapters())
            {
                var mac = adapter.MacAddress?.ToString() ?? string.Empty;
                var description = string.IsNullOrWhiteSpace(adapter.Description) ? adapter.Name : adapter.Description;
                Adapters.Add(new AdapterChoice
                {
                    Selector = adapter.Index.ToString(CultureInfo.InvariantCulture),
                    MacAddress = mac,
                    DisplayName = $"[{adapter.Index}] {(string.IsNullOrWhiteSpace(mac) ? "MAC -" : mac)}  {description}"
                });
            }

            SelectedAdapter ??= Adapters.FirstOrDefault();
            AppendEvent(Adapters.Count == 0 ? "No Npcap adapters found." : $"Adapters found: {Adapters.Count}");
        }
        catch (Exception ex)
        {
            Adapters.Clear();
            AppendEvent($"Adapter list unavailable: {ex.Message}");
        }
    }

    private void RunLivePreflight()
    {
        var report = RefreshLivePreflight();
        AppendPreflightReport(report, includeInfo: true);
        StatusText = report.HasErrors ? "Live check has fatal errors." : "Live check ready.";
    }

    private LivePreflightReport RefreshLivePreflight()
    {
        var report = BuildLivePreflightReport();
        LivePreflightDiagnostics.Clear();
        foreach (var diagnostic in report.Diagnostics)
            LivePreflightDiagnostics.Add(diagnostic);

        LivePreflightSummaryText = report.SummaryText;
        OnPropertyChanged(nameof(HasLivePreflightErrors));
        OnPropertyChanged(nameof(HasLivePreflightWarnings));
        OnPropertyChanged(nameof(LivePreflightErrorCount));
        OnPropertyChanged(nameof(LivePreflightWarningCount));
        OnPropertyChanged(nameof(LivePreflightInfoCount));
        OnPropertyChanged(nameof(LiveSafetyStatusText));
        return report;
    }

    private void AppendPreflightReport(LivePreflightReport report, bool includeInfo)
    {
        AppendEvent(report.SummaryText);
        foreach (var diagnostic in report.Diagnostics)
        {
            if (!includeInfo && diagnostic.Severity == LivePreflightSeverity.Info)
                continue;

            AppendEvent(diagnostic.ToString());
        }
    }

    private LivePreflightReport BuildLivePreflightReport()
    {
        SaveCurrentPublisherSlot();
        var diagnostics = new List<LivePreflightDiagnostic>();
        void Add(LivePreflightSeverity severity, string area, string message, string detail = "")
            => diagnostics.Add(new LivePreflightDiagnostic
            {
                Severity = severity,
                Area = area,
                Message = message,
                Detail = detail
            });

        var activeSlots = PublisherSlots.Where(slot => slot.IsEnabled).ToArray();
        if (activeSlots.Length == 0)
        {
            Add(LivePreflightSeverity.Error, "Publishers", "No enabled publisher slot.", "Enable at least one IED / MU publisher before live publishing.");
            return new LivePreflightReport(diagnostics);
        }

        Add(LivePreflightSeverity.Info, "Mode", "KM Looptest friendly preflight.", "Warnings do not block live publish. Only fatal configuration errors are blocked.");

        if (SelectedAdapter is null)
            Add(LivePreflightSeverity.Error, "Adapter", "No NIC adapter selected.", "Select the adapter connected to KM Looptest / relay point-to-point port.");
        else
        {
            Add(LivePreflightSeverity.Info, "Adapter", "Selected adapter", SelectedAdapter.DisplayName);
            if (string.IsNullOrWhiteSpace(SelectedAdapter.MacAddress))
                Add(LivePreflightSeverity.Warning, "Adapter", "Adapter MAC address is not visible.", "Live publish can still be attempted if Npcap can open the adapter.");
            if (SelectedAdapter.DisplayName.Contains("loopback", StringComparison.OrdinalIgnoreCase))
                Add(LivePreflightSeverity.Warning, "Adapter", "Adapter looks like loopback.", "For KM Looptest, normally select the physical Ethernet adapter.");
        }

        if (Adapters.Count == 0)
            Add(LivePreflightSeverity.Warning, "Adapter", "Adapter list is empty.", "Install Npcap and restart ARSVIN if no live adapter is available.");

        if (!IsRunningElevated())
            Add(LivePreflightSeverity.Warning, "Privilege", "Application may not be running as Administrator.", "Npcap live transmission may require elevated privileges on Windows.");

        var appIds = new List<(ushort AppId, SvPublisherSlotViewModel Slot)>();
        var destinations = new List<(string Mac, SvPublisherSlotViewModel Slot)>();
        var signatures = new List<(string Signature, SvPublisherSlotViewModel Slot)>();

        foreach (var slot in activeSlots)
        {
            if (slot.SelectedStream?.Stream is not { } stream)
            {
                Add(LivePreflightSeverity.Error, slot.Header, "No SV stream selected.", "Open SCL and select an SV stream. ARSVIN still needs an SV dataset layout before it can build frames.");
                continue;
            }

            if (slot.SampleRateHz <= 0)
                Add(LivePreflightSeverity.Error, slot.Header, "Sample rate must be greater than 0.", $"Current value: {slot.SampleRateHz:0.###} fps.");
            else if (slot.SampleRateHz > 4800)
                Add(LivePreflightSeverity.Warning, slot.Header, "High sample rate selected.", $"{slot.SampleRateHz:0.#} fps can increase Windows jitter. KM Looptest is easier at 4000/4800 fps.");

            if (slot.NominalFrequencyHz <= 0)
                Add(LivePreflightSeverity.Error, slot.Header, "Nominal frequency must be greater than 0.");

            if (!MacAddress.TryParse(slot.SourceMac, out var sourceMac))
                Add(LivePreflightSeverity.Error, slot.Header, "Source MAC is invalid.", slot.SourceMac);
            else
            {
                WarnForSourceMac(sourceMac, slot.Header, Add);
                if (SelectedAdapter is not null
                    && !string.IsNullOrWhiteSpace(SelectedAdapter.MacAddress)
                    && !string.Equals(slot.SourceMac.Replace('-', ':'), SelectedAdapter.MacAddress.Replace('-', ':'), StringComparison.OrdinalIgnoreCase))
                    Add(LivePreflightSeverity.Warning, slot.Header, "Source MAC differs from selected adapter MAC.", $"slot={sourceMac}, adapter={SelectedAdapter.MacAddress}");
            }

            if (!MacAddress.TryParse(slot.DestinationMac, out var destinationMac))
                Add(LivePreflightSeverity.Error, slot.Header, "Destination MAC is invalid.", slot.DestinationMac);
            else
            {
                WarnForDestinationMac(destinationMac, slot.Header, Add);
                destinations.Add((destinationMac.ToString(), slot));
            }

            ushort appId = 0;
            try
            {
                appId = ParseAppId(slot.AppIdText);
                appIds.Add((appId, slot));
                if (appId == 0)
                    Add(LivePreflightSeverity.Warning, slot.Header, "APPID is 0x0000.", "Use the APPID expected by KM Looptest / relay configuration unless this is intentional.");
                else
                    Add(LivePreflightSeverity.Info, slot.Header, "APPID accepted.", $"{slot.AppIdText} -> 0x{appId:X4}.");
            }
            catch (Exception ex)
            {
                Add(LivePreflightSeverity.Error, slot.Header, "APPID is invalid.", ex.Message);
            }

            try
            {
                _ = ResolveVlanTag(slot.UseVlan, slot.VlanId, slot.VlanPriority);
                if (!slot.UseVlan)
                    Add(LivePreflightSeverity.Warning, slot.Header, "VLAN tag is disabled.", "Allowed for KM Looptest if the peer expects untagged SV traffic.");
                else
                    Add(LivePreflightSeverity.Info, slot.Header, "VLAN", $"VID={slot.VlanId}, PCP={slot.VlanPriority}.");
            }
            catch (Exception ex)
            {
                Add(LivePreflightSeverity.Error, slot.Header, "VLAN setting is invalid.", ex.Message);
            }

            if (slot.CurrentDlsb <= 0 || slot.VoltageDlsb <= 0)
                Add(LivePreflightSeverity.Error, slot.Header, "Current and voltage dLSB must be greater than 0.");

            if (slot.SignalSource == PublisherSignalSource.ComtradeReplay && slot.ComtradeDataset is null)
                Add(LivePreflightSeverity.Error, slot.Header, "COMTRADE replay selected but no COMTRADE file is loaded.");

            var qualityChoice = ResolveSampleQualityChoice(slot.SampleQualityKey);
            if (!string.Equals(qualityChoice.Key, "good", StringComparison.OrdinalIgnoreCase))
                Add(LivePreflightSeverity.Warning, slot.Header, "Non-default SV quality selected.", $"Quality={qualityChoice.Label}. Use only for intentional relay behavior tests.");
            else
                Add(LivePreflightSeverity.Info, slot.Header, "SV quality", qualityChoice.Label);

            try
            {
                var validation = SampledValuesPublisherValidator.Validate(stream);
                foreach (var finding in validation.Findings)
                {
                    var severity = finding.Severity switch
                    {
                        SampledValuesPublisherValidationSeverity.Error => LivePreflightSeverity.Error,
                        SampledValuesPublisherValidationSeverity.Warning => LivePreflightSeverity.Warning,
                        _ => LivePreflightSeverity.Info
                    };
                    Add(severity, slot.Header, finding.Message, string.IsNullOrWhiteSpace(finding.Detail) ? finding.Code : $"{finding.Code}: {finding.Detail}");
                }

                var layout = SampledValuesPayloadLayout.FromDataSet(stream.Entries);
                if (layout.IsFullySupported && stream.Address.AppId.HasValue && stream.Address.DestinationMac.HasValue)
                {
                    var noAsdu = SampledValuesPublisherProfile.ResolveAsduPerFrame(stream);
                    var publishRate = SampledValuesPublisherProfile.ResolvePublicationRate(slot.SampleRateHz, noAsdu);
                    Add(LivePreflightSeverity.Info, slot.Header, "Frame preview.", $"nofASDU={noAsdu}, sample={slot.SampleRateHz:0.#} fps, publish={publishRate:0.#} fps, payload={layout.PayloadByteLength} B/ASDU.");
                }
            }
            catch (Exception ex)
            {
                Add(LivePreflightSeverity.Error, slot.Header, "Payload layout cannot be built.", ex.Message);
            }

            if (MacAddress.TryParse(slot.DestinationMac, out var destForSignature))
            {
                var vlanPart = slot.UseVlan ? $"vlan:{slot.VlanId}" : "untagged";
                signatures.Add(($"0x{appId:X4}|{destForSignature}|{vlanPart}", slot));
            }
        }

        foreach (var duplicate in appIds.GroupBy(item => item.AppId).Where(group => group.Count() > 1))
            Add(LivePreflightSeverity.Warning, "APPID", $"APPID 0x{duplicate.Key:X4} is used by multiple publishers.", string.Join(", ", duplicate.Select(item => item.Slot.Header)));

        foreach (var duplicate in destinations.GroupBy(item => item.Mac, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
            Add(LivePreflightSeverity.Warning, "Destination MAC", $"Destination MAC {duplicate.Key} is shared by multiple publishers.", string.Join(", ", duplicate.Select(item => item.Slot.Header)));

        foreach (var duplicate in signatures.GroupBy(item => item.Signature, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1))
            Add(LivePreflightSeverity.Warning, "Stream identity", "Multiple publishers share the same APPID, destination MAC, and VLAN identity.", $"{duplicate.Key}: {string.Join(", ", duplicate.Select(item => item.Slot.Header))}");

        AddSyncCompatibilityDiagnostics(Add);
        return new LivePreflightReport(diagnostics);
    }

    private void AddSyncCompatibilityDiagnostics(Action<LivePreflightSeverity, string, string, string> add)
    {
        switch (NormalizeSyncPolicyMode(SyncPolicyMode))
        {
            case SvSyncPolicyMode.GlobalCompatibility:
                add(LivePreflightSeverity.Warning, "smpSynch", "Global compatibility mode is selected.", "Allowed for KM Looptest / relay readability. This is not proof of real PTP accuracy.");
                break;
            case SvSyncPolicyMode.LocalCompatibility:
                add(LivePreflightSeverity.Info, "smpSynch", "Local compatibility mode is selected.", "SV will publish smpSynch=1.");
                break;
            case SvSyncPolicyMode.HonestUnsynchronized:
                add(LivePreflightSeverity.Warning, "smpSynch", "Honest unsynchronized mode is selected.", "Strict relays may reject SV traffic with smpSynch=0.");
                break;
            case SvSyncPolicyMode.ExternalPtpAuto:
                add(LivePreflightSeverity.Info, "smpSynch", "External PTP auto mode is selected.", "SV synchronization marking depends on observed external PTP health.");
                break;
        }

        if (PtpPublisherMode == AR.Iec61850.SvPublisher.Models.PtpPublisherMode.LabPublisher)
            add(LivePreflightSeverity.Warning, "PTP traffic", "Lab PTP traffic generation is enabled.", "Traffic can help compatibility testing, but it does not certify clock accuracy.");
    }

    private static void WarnForSourceMac(MacAddress mac, string area, Action<LivePreflightSeverity, string, string, string> add)
    {
        var bytes = mac.ToArray();
        if (bytes.All(value => value == 0x00))
            add(LivePreflightSeverity.Error, area, "Source MAC is all zeros.", mac.ToString());
        else if (IsBroadcastMac(bytes))
            add(LivePreflightSeverity.Error, area, "Source MAC cannot be broadcast.", mac.ToString());
        else if (IsMulticastMac(bytes))
            add(LivePreflightSeverity.Error, area, "Source MAC cannot be multicast.", mac.ToString());
    }

    private static void WarnForDestinationMac(MacAddress mac, string area, Action<LivePreflightSeverity, string, string, string> add)
    {
        var bytes = mac.ToArray();
        if (bytes.All(value => value == 0x00))
            add(LivePreflightSeverity.Error, area, "Destination MAC is all zeros.", mac.ToString());
        else if (IsBroadcastMac(bytes))
            add(LivePreflightSeverity.Error, area, "Destination MAC should not be broadcast for SV.", mac.ToString());
        else if (!IsMulticastMac(bytes))
            add(LivePreflightSeverity.Warning, area, "Destination MAC is not multicast.", mac.ToString());
        else if (!IsCommonSampledValuesMulticast(bytes))
            add(LivePreflightSeverity.Warning, area, "Destination MAC is multicast but not in the common Sampled Values multicast range.", $"{mac}; common SV range starts with 01:0C:CD:04.");
    }

    private static bool IsMulticastMac(byte[] bytes)
        => bytes.Length == 6 && (bytes[0] & 0x01) == 0x01;

    private static bool IsBroadcastMac(byte[] bytes)
        => bytes.Length == 6 && bytes.All(value => value == 0xFF);

    private static bool IsCommonSampledValuesMulticast(byte[] bytes)
        => bytes.Length == 6 && bytes[0] == 0x01 && bytes[1] == 0x0C && bytes[2] == 0xCD && bytes[3] == 0x04;

    private static bool IsRunningElevated()
    {
        try
        {
            if (!OperatingSystem.IsWindows())
                return true;

            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private async Task SaveProfileAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save ARSVIN Publish Plan",
            Filter = "SV publisher plan (*.svpub.json)|*.svpub.json|JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = "arsvin-publish-plan.svpub.json"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var snapshot = CreateSnapshot();
            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(dialog.FileName, json).ConfigureAwait(true);
            StatusText = "Plan saved.";
            AppendEvent($"Saved plan: {dialog.FileName}");
        }
        catch (Exception ex)
        {
            StatusText = "Save failed.";
            AppendEvent(ex.Message);
        }
    }


    private async Task ExportGeneratedPcapAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export Generated SV Frames to PCAP",
            Filter = "PCAP files (*.pcap)|*.pcap|All files (*.*)|*.*",
            FileName = $"arsvin-sv-generated-{DateTime.Now:yyyyMMdd-HHmmss}.pcap"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            SaveCurrentPublisherSlot();
            ValidateBeforeRun(live: false);
            var frames = await Task.Run(() => BuildGeneratedPcapFrames(maxFramesPerPublisher: 1000)).ConfigureAwait(true);
            SampledValuesPcapExporter.WriteGeneratedFrames(dialog.FileName, frames);
            StatusText = "Generated PCAP exported.";
            AppendEvent($"Exported generated SV PCAP: {dialog.FileName} ({frames.Count} frame(s)).");
        }
        catch (Exception ex)
        {
            StatusText = "PCAP export failed.";
            AppendEvent(ex.Message);
        }
    }

    private async Task ExportPublisherEvidenceReportAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export SV Publisher Evidence Report",
            Filter = "Markdown report (*.md)|*.md|Text files (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = $"arsvin-sv-evidence-{DateTime.Now:yyyyMMdd-HHmmss}.md"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            SaveCurrentPublisherSlot();
            var preflight = RefreshLivePreflight();
            var evidence = BuildPublisherEvidenceReport(preflight);
            var markdown = SampledValuesPublisherEvidenceReportWriter.ToMarkdown(evidence);
            await File.WriteAllTextAsync(dialog.FileName, markdown).ConfigureAwait(true);
            StatusText = "Publisher evidence report exported.";
            AppendEvent($"Exported publisher evidence report: {dialog.FileName}");
        }
        catch (Exception ex)
        {
            StatusText = "Evidence export failed.";
            AppendEvent(ex.Message);
        }
    }

    private SampledValuesPublisherEvidenceReport BuildPublisherEvidenceReport(LivePreflightReport preflight)
    {
        ArgumentNullException.ThrowIfNull(preflight);
        var activeSlots = PublisherSlots.Where(slot => slot.IsEnabled).ToArray();
        var streams = activeSlots.Select(slot => BuildEvidenceStream(slot, preflight)).ToArray();
        var streamAreas = new HashSet<string>(activeSlots.Select(slot => slot.Header), StringComparer.OrdinalIgnoreCase);
        var globalFindings = preflight.Diagnostics
            .Where(diagnostic => !streamAreas.Contains(diagnostic.Area))
            .Select(ToEvidenceFinding)
            .ToArray();
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "dev";

        return new SampledValuesPublisherEvidenceReport(
            ToolName: "ARSVIN",
            ToolVersion: version,
            CreatedAt: DateTimeOffset.Now,
            SclPath: SclPath,
            Adapter: SelectedAdapter?.DisplayName ?? "-",
            Mode: $"{Mode}; scenario={SelectedScenarioPresetChoice?.ShortLabel ?? "custom"}; continuous={Continuous}; duration={DurationSeconds:0.###}s",
            TxTiming: TxTimingHealthText,
            SafetyBoundary: "Lab publisher / TX-side evidence only; not an analyzer and not a certified merging unit.",
            Streams: streams,
            GlobalFindings: globalFindings);
    }

    private SampledValuesEvidenceStream BuildEvidenceStream(SvPublisherSlotViewModel slot, LivePreflightReport preflight)
    {
        var findings = preflight.Diagnostics
            .Where(diagnostic => string.Equals(diagnostic.Area, slot.Header, StringComparison.OrdinalIgnoreCase))
            .Select(ToEvidenceFinding)
            .ToArray();

        var controlBlock = slot.StreamControlBlock;
        var svId = slot.StreamId;
        var dataSet = slot.DataSetReference;
        var noAsdu = (ushort)1;
        var payloadBytes = Math.Max(0, slot.PayloadBytes);
        var estimatedBytes = 0;
        var estimatedBandwidth = 0.0;

        if (slot.SelectedStream?.Stream is { } stream)
        {
            controlBlock = string.IsNullOrWhiteSpace(controlBlock) ? stream.ControlBlockReference : controlBlock;
            svId = string.IsNullOrWhiteSpace(svId) ? stream.SvId : svId;
            dataSet = string.IsNullOrWhiteSpace(dataSet) ? stream.DataSetReference : dataSet;
            noAsdu = SampledValuesPublisherProfile.ResolveAsduPerFrame(stream);
            try
            {
                var layout = SampledValuesPayloadLayout.FromDataSet(stream.Entries);
                payloadBytes = layout.PayloadByteLength;
                if (stream.Address.AppId.HasValue && stream.Address.DestinationMac.HasValue)
                {
                    var preview = SampledValuesFramePreview.FromStream(stream, slot.SampleRateHz);
                    estimatedBytes = preview.EstimatedEthernetBytes;
                    estimatedBandwidth = preview.EstimatedBandwidthBitsPerSecond;
                }
            }
            catch
            {
                // Preflight findings carry the user-visible detail. The evidence row remains exportable.
            }
        }

        var publicationRate = SampledValuesPublisherProfile.ResolvePublicationRate(slot.SampleRateHz, noAsdu);
        var quality = ResolveSampleQualityChoice(slot.SampleQualityKey).Label;
        var vlan = slot.UseVlan ? $"VID={slot.VlanId}/PCP={slot.VlanPriority}" : "untagged";
        var status = slot.SelectedStream is null ? "needs stream" : "ready";

        return new SampledValuesEvidenceStream(
            SlotName: slot.Header,
            IsEnabled: slot.IsEnabled,
            ControlBlockReference: controlBlock,
            SvId: svId,
            DataSetReference: dataSet,
            AppId: slot.AppIdText,
            SourceMac: slot.SourceMac,
            DestinationMac: slot.DestinationMac,
            Vlan: vlan,
            SampleRateHz: slot.SampleRateHz,
            PublicationRateHz: publicationRate,
            NoAsdu: noAsdu,
            PayloadBytesPerAsdu: payloadBytes,
            EstimatedEthernetBytes: estimatedBytes,
            EstimatedBandwidthBitsPerSecond: estimatedBandwidth,
            SignalSource: slot.SignalSource == PublisherSignalSource.ComtradeReplay ? "COMTRADE replay" : "Manual phasor",
            Quality: quality,
            SyncMode: NormalizeSyncPolicyMode(SyncPolicyMode).ToString(),
            Status: status,
            Findings: findings);
    }

    private static SampledValuesEvidenceFinding ToEvidenceFinding(LivePreflightDiagnostic diagnostic)
        => new(
            diagnostic.Severity.ToString().ToUpperInvariant(),
            diagnostic.Area,
            diagnostic.Message,
            diagnostic.Detail);

    private async Task RunPublishAsync(bool live)
    {
        try
        {
            if (live)
            {
                var report = RefreshLivePreflight();
                AppendPreflightReport(report, includeInfo: false);
                if (report.HasErrors)
                    throw new InvalidOperationException("Live publish blocked by fatal preflight error(s). Warnings are allowed for KM Looptest / isolated point-to-point tests.");
            }

            ValidateBeforeRun(live);

            using var stop = new CancellationTokenSource();
            _publisherStop = stop;
            IsPublishing = true;
            var planPreview = BuildPublisherSessionPlan();
            StatusText = live ? "START PUBLISH - live NIC." : "START PUBLISH - dry run.";
            TxTimingHealthText = "TX Timing: starting";
            AutoApplyWhileRunning = true;
            LiveApplyText = planPreview.LiveApplyText;
            AppendEvent(live ? $"Start Publish: live NIC {planPreview.DisplayName}." : $"Start Publish: dry-run {planPreview.DisplayName}.");

            await Task.Run(async () => await PublishLoopAsync(live, stop.Token).ConfigureAwait(false)).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            StatusText = "STOP PUBLISH.";
            AppendEvent("Stop Publish requested by operator.");
        }
        catch (Exception ex)
        {
            StatusText = "Publisher failed.";
            AppendEvent(ex.Message);
        }
        finally
        {
            _publisherStop?.Dispose();
            _publisherStop = null;
            IsPublishing = false;
            if (!PublishText.StartsWith("Complete", StringComparison.OrdinalIgnoreCase))
            {
                PublishText = "Publisher stopped.";
                TxTimingHealthText = "TX Timing: stopped";
            }
            CommandManager.InvalidateRequerySuggested();
        }
    }


    private IReadOnlyList<(DateTimeOffset Timestamp, byte[] Frame)> BuildGeneratedPcapFrames(int maxFramesPerPublisher)
    {
        var publisherStates = BuildPublisherRuntimeStates().ToArray();
        if (publisherStates.Length == 0)
            throw new InvalidOperationException("Enable at least one IED / MU publisher slot with a selected SV stream before exporting generated PCAP.");

        var sessionPlan = BuildPublisherSessionPlan();
        var startedAt = DateTimeOffset.UtcNow;
        var frames = new List<(DateTimeOffset Timestamp, byte[] Frame)>();

        foreach (var state in publisherStates)
        {
            state.SampleCount = SampleCounterPolicy.InitialSampleCount(startedAt, state.SampleRateHz, state.SampleCounterWrap, SampleCounterMode.SecondAligned);
            var sessionLimit = sessionPlan.ResolveFrameLimit(state.PublicationRateHz);
            var frameLimit = Math.Min(maxFramesPerPublisher, checked((int)Math.Min(sessionLimit ?? maxFramesPerPublisher, maxFramesPerPublisher)));
            var baseChannels = state.FrozenChannels;

            for (var frameIndex = 0; frameIndex < frameLimit; frameIndex++)
            {
                var frameTimestamp = startedAt.AddTicks((long)Math.Round(frameIndex * TimeSpan.TicksPerSecond / state.PublicationRateHz));
                var asdus = new List<SampledValueAsdu>(state.NoAsdu);

                for (var asduIndex = 0; asduIndex < state.NoAsdu; asduIndex++)
                {
                    var sampleIndex = ((long)frameIndex * state.NoAsdu) + asduIndex;
                    var elapsedSeconds = sampleIndex / state.SampleRateHz;
                    var timestamp = startedAt.AddTicks((long)Math.Round(sampleIndex * TimeSpan.TicksPerSecond / state.SampleRateHz));
                    var sampleTime = new Iec61850UtcTime(timestamp, Quality: 0);

                    byte[] payload;
                    if (state.SignalSource == PublisherSignalSource.ComtradeReplay && state.ComtradeDataset is { } dataset)
                    {
                        var sample = dataset.GetSampleByIndex(sampleIndex, state.ComtradeLoop);
                        var instantaneousValues = ResolveComtradeInstantaneousValues(sample, state.ComtradeChannelMap);
                        payload = BuildInstantaneousSamplePayload(state.Stream, sampleTime, instantaneousValues, state.CurrentDlsb, state.VoltageDlsb, state.Quality);
                    }
                    else
                    {
                        var effectiveChannels = sessionPlan.ResolveChannels(baseChannels, elapsedSeconds);
                        var phasedChannels = ApplyOscillatorPhases(effectiveChannels, state.OscillatorStates, state.SampleRateHz);
                        payload = BuildSamplePayload(state.Stream, sampleTime, phasedChannels, state.CurrentDlsb, state.VoltageDlsb, state.Quality);
                    }

                    asdus.Add(new SampledValueAsdu
                    {
                        SvId = state.SvId,
                        DataSetReference = state.DataSetReference,
                        SampleCount = SampleCounterPolicy.Increment(state.SampleCount, state.SampleCounterWrap, asduIndex),
                        ConfigurationRevision = state.Stream.ConfigurationRevision,
                        ReferenceTime = sampleTime,
                        SampleSynchronization = (byte)ResolveSampleSynchronization(null),
                        SampleRate = ToSampleRate(state.SampleRateHz, state.NominalFrequencyHz, state.Stream.SampleMode),
                        SampleMode = MapSampleMode(state.Stream.SampleMode),
                        SamplePayload = payload
                    });
                }

                var frame = SampledValuesFrameBuilder.BuildEthernetFrame(new SampledValuesFrame
                {
                    Destination = state.Destination,
                    Source = state.Source,
                    Vlan = state.Vlan,
                    AppId = state.AppId,
                    Pdu = new SampledValuesPdu { Asdus = asdus }
                });

                frames.Add((frameTimestamp, frame));
                state.SampleCount = SampleCounterPolicy.Increment(state.SampleCount, state.SampleCounterWrap, state.NoAsdu);
                state.Sent++;
            }
        }

        return frames;
    }

    private async Task PublishLoopAsync(bool live, CancellationToken cancellationToken)
    {
        SaveCurrentPublisherSlot();
        var publisherStates = BuildPublisherRuntimeStates().ToArray();
        if (publisherStates.Length == 0)
            throw new InvalidOperationException("Enable at least one IED / MU publisher slot with a selected SV stream.");

        var sessionPlan = BuildPublisherSessionPlan();
        var primary = publisherStates[0];
        var source = primary.Source;
        var vlan = primary.Vlan;
        var frameLimitPerPublisher = publisherStates.ToDictionary(s => s.SlotIndex, s => sessionPlan.ResolveFrameLimit(s.PublicationRateHz));
        var startedTicks = Stopwatch.GetTimestamp();
        var startedAt = DateTimeOffset.UtcNow;
        foreach (var state in publisherStates)
            state.SampleCount = SampleCounterPolicy.InitialSampleCount(startedAt, state.SampleRateHz, state.SampleCounterWrap, SampleCounterMode.SecondAligned);
        var nextUiTicks = startedTicks;

        var ptpMonitor = new PtpPassiveMonitor();
        var ptpValidator = new PtpTimingHealthValidator();
        var ptpOptions = BuildPtpHealthOptions();
        PtpTimingHealthReport? latestPtpReport = null;

        IProcessBusTransport transport;
        IProcessBusFrameSource? frameSource = null;
        if (live)
        {
            var duplex = new NpcapProcessBusDuplexTransport(SelectedAdapter?.Selector ?? string.Empty);
            transport = duplex;
            frameSource = duplex;
        }
        else
        {
            transport = new InMemoryProcessBusTransport();
        }

        IDisposable? disposableTransport = transport as IDisposable;
        using var ptpCaptureStop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        PtpPublisherRuntime? labPtpPublisher = live && PtpPublisherMode == AR.Iec61850.SvPublisher.Models.PtpPublisherMode.LabPublisher
            ? new PtpPublisherRuntime(transport, BuildLabPtpOptions(source, vlan))
            : null;
        var labPtpTask = labPtpPublisher is not null
            ? Task.Run(async () => await labPtpPublisher.RunAsync(ptpCaptureStop.Token).ConfigureAwait(false), CancellationToken.None)
            : Task.CompletedTask;
        var ptpCaptureTask = live && frameSource is not null
            ? Task.Run(async () => await CapturePtpAsync(frameSource, ptpMonitor, labPtpPublisher, ptpCaptureStop.Token).ConfigureAwait(false), CancellationToken.None)
            : Task.CompletedTask;

        long totalSent = 0;
        var lastFrameBytes = 0;
        var lastPayloadBytes = 0;
        bool IsActive(PublisherRuntimeState state)
        {
            var sessionLimit = frameLimitPerPublisher[state.SlotIndex];
            long? sourceLimit = state.SignalSource == PublisherSignalSource.ComtradeReplay &&
                                state.ComtradeDataset is { } dataset &&
                                !state.ComtradeLoop
                ? Math.Max(1L, (long)Math.Ceiling(dataset.SampleCount / (double)Math.Max(1, (int)state.NoAsdu)))
                : null;

            var effectiveLimit = MinLimit(sessionLimit, sourceLimit);
            return effectiveLimit is null || state.Sent < effectiveLimit.Value;
        }

        try
        {
            Dispatch(() =>
            {
                PtpStatusText = live ? $"PTP: listening domain {ExpectedPtpDomain}" : "PTP: dry-run monitor inactive";
                UpdatePtpPublisherStatus(labPtpPublisher);
                SmpSynchStatusText = live ? $"smpSynch: waiting ({SyncPolicyShortLabel})" : FormatSmpSynchStatus(ResolveSampleSynchronization(null));
                TxTimingHealthText = $"TX Timing: target={publisherStates.Sum(s => s.PublicationRateHz):0.0}fps";
                PublishText = $"Prepared {publisherStates.Length} SV publisher(s), {sessionPlan.DisplayName}: {string.Join(", ", publisherStates.Select(s => $"P{s.SlotIndex}@{s.SampleRateHz:0.#}sps/{s.PublicationRateHz:0.#}fps nofASDU={s.NoAsdu} q={s.QualityLabel}"))}";
            });

            while (publisherStates.Any(IsActive))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var nextDueTicks = publisherStates
                    .Where(IsActive)
                    .Min(s => s.DueTicks(startedTicks));
                await DelayUntilTicksAsync(nextDueTicks, cancellationToken).ConfigureAwait(false);

                var nowTicks = Stopwatch.GetTimestamp();
                foreach (var state in publisherStates)
                {
                    if (!IsActive(state))
                        continue;

                    if (state.DueTicks(startedTicks) > nowTicks)
                        continue;

                    var smpSynch = ResolveSampleSynchronization(latestPtpReport);
                    var samplePayloads = new List<byte[]>(state.NoAsdu);
                    var asdus = new List<SampledValueAsdu>(state.NoAsdu);
                    var baseChannelsForFrame = ResolveRuntimeBaseChannels(state);

                    for (var asduIndex = 0; asduIndex < state.NoAsdu; asduIndex++)
                    {
                        var sampleIndex = (state.Sent * state.NoAsdu) + asduIndex;
                        var elapsedSeconds = sampleIndex / state.SampleRateHz;
                        var timestamp = startedAt.AddTicks((long)Math.Round(sampleIndex * TimeSpan.TicksPerSecond / state.SampleRateHz));
                        var sampleTime = new Iec61850UtcTime(timestamp, Quality: 0);
                        byte[] payload;
                        if (state.SignalSource == PublisherSignalSource.ComtradeReplay && state.ComtradeDataset is { } dataset)
                        {
                            var sample = dataset.GetSampleByIndex(sampleIndex, state.ComtradeLoop);
                            var instantaneousValues = ResolveComtradeInstantaneousValues(sample, state.ComtradeChannelMap);
                            payload = BuildInstantaneousSamplePayload(state.Stream, sampleTime, instantaneousValues, state.CurrentDlsb, state.VoltageDlsb, state.Quality);
                        }
                        else
                        {
                            var effectiveChannels = sessionPlan.ResolveChannels(baseChannelsForFrame, elapsedSeconds);
                            var phasedChannels = ApplyOscillatorPhases(effectiveChannels, state.OscillatorStates, state.SampleRateHz);
                            payload = BuildSamplePayload(state.Stream, sampleTime, phasedChannels, state.CurrentDlsb, state.VoltageDlsb, state.Quality);
                        }

                        samplePayloads.Add(payload);
                        asdus.Add(new SampledValueAsdu
                        {
                            SvId = state.SvId,
                            DataSetReference = state.DataSetReference,
                            SampleCount = SampleCounterPolicy.Increment(state.SampleCount, state.SampleCounterWrap, asduIndex),
                            ConfigurationRevision = state.Stream.ConfigurationRevision,
                            ReferenceTime = sampleTime,
                            SampleSynchronization = (byte)smpSynch,
                            SampleRate = ToSampleRate(state.SampleRateHz, state.NominalFrequencyHz, state.Stream.SampleMode),
                            SampleMode = MapSampleMode(state.Stream.SampleMode),
                            SamplePayload = payload
                        });
                    }

                    var frame = SampledValuesFrameBuilder.BuildEthernetFrame(new SampledValuesFrame
                    {
                        Destination = state.Destination,
                        Source = state.Source,
                        Vlan = state.Vlan,
                        AppId = state.AppId,
                        Pdu = new SampledValuesPdu { Asdus = asdus }
                    });

                    var scheduledTicks = state.DueTicks(startedTicks);
                    var sendStartTicks = Stopwatch.GetTimestamp();
                    await transport.SendAsync(frame, cancellationToken).ConfigureAwait(false);
                    var sendEndTicks = Stopwatch.GetTimestamp();
                    state.TimingHealth.Record(scheduledTicks, sendStartTicks, sendEndTicks);
                    state.SampleCount = SampleCounterPolicy.Increment(state.SampleCount, state.SampleCounterWrap, state.NoAsdu);
                    state.Sent++;
                    totalSent++;
                    lastFrameBytes = frame.Length;
                    lastPayloadBytes = samplePayloads.Count == 0 ? 0 : samplePayloads[0].Length;
                }

                if (nowTicks >= nextUiTicks)
                {
                    latestPtpReport = ptpValidator.Evaluate(ptpMonitor.GetSnapshot(), ptpOptions);
                    var smpSynch = ResolveSampleSynchronization(latestPtpReport);
                    var elapsed = Stopwatch.GetElapsedTime(startedTicks);
                    var effectiveRate = totalSent / Math.Max(elapsed.TotalSeconds, 0.001);
                    var totalSamples = publisherStates.Sum(s => s.Sent * s.NoAsdu);
                    var perPublisher = string.Join("  ", publisherStates.Select(s => $"P{s.SlotIndex}:{s.Sent}f/{s.Sent * s.NoAsdu}s smpCnt={s.SampleCount}"));
                    var txTimingText = FormatTxTimingHealth(publisherStates, nowTicks);
                    var message = $"{(live ? "LIVE" : "DRY")} {sessionPlan.ShortName} publishers={publisherStates.Length} frames={totalSent} samples={totalSamples} rate={effectiveRate:0.0} fps smpSynch={(byte)smpSynch} ({SyncPolicyShortLabel}) payload={lastPayloadBytes}B/asdu frame={lastFrameBytes}B q={string.Join(",", publisherStates.Select(s => s.QualityLabel).Distinct())} {txTimingText}  {perPublisher}";
                    Dispatch(() =>
                    {
                        PayloadBytes = lastPayloadBytes;
                        PublishText = message;
                        TxTimingHealthText = txTimingText;
                        UpdatePtpStatus(latestPtpReport, smpSynch, live);
                        UpdatePtpPublisherStatus(labPtpPublisher);
                    });
                    nextUiTicks = nowTicks + (long)Math.Round(0.25 * Stopwatch.Frequency);
                }
            }
        }
        finally
        {
            ptpCaptureStop.Cancel();
            try
            {
                await Task.WhenAll(ptpCaptureTask, labPtpTask).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected during publisher shutdown.
            }
            catch (Exception ex)
            {
                Dispatch(() => AppendEvent($"PTP runtime stopped: {ex.Message}"));
            }

            disposableTransport?.Dispose();
        }

        var totalElapsed = Stopwatch.GetElapsedTime(startedTicks);
        var rate = totalSent / Math.Max(totalElapsed.TotalSeconds, 0.001);
        var finalTxTimingText = FormatTxTimingHealth(publisherStates, Stopwatch.GetTimestamp());
        Dispatch(() =>
        {
            var totalSamples = publisherStates.Sum(s => s.Sent * s.NoAsdu);
            PublishText = $"Complete {sessionPlan.ShortName} publishers={publisherStates.Length} frames={totalSent} samples={totalSamples} elapsed={totalElapsed.TotalSeconds:0.000}s rate={rate:0.0} fps lastFrame={lastFrameBytes}B {finalTxTimingText}";
            TxTimingHealthText = finalTxTimingText;
            StatusText = "Publisher complete.";
            AppendEvent(PublishText);
        });
    }

    private static string FormatTxTimingHealth(IReadOnlyList<PublisherRuntimeState> publisherStates, long nowTicks)
    {
        if (publisherStates.Count == 0)
            return "TX Timing: idle";

        var snapshots = publisherStates.Select(state => state.TimingHealth.Snapshot(nowTicks)).ToArray();
        var targetFps = snapshots.Sum(snapshot => snapshot.TargetFramesPerSecond);
        var actualFps = snapshots.Sum(snapshot => snapshot.ActualFramesPerSecond);
        var totalFrames = Math.Max(1, snapshots.Sum(snapshot => snapshot.FrameCount));
        var averageJitter = snapshots.Sum(snapshot => snapshot.AverageAbsJitterMicroseconds * Math.Max(1, snapshot.FrameCount)) / totalFrames;
        var maxJitter = snapshots.Max(snapshot => snapshot.MaxAbsJitterMicroseconds);
        var lateFrames = snapshots.Sum(snapshot => snapshot.LateFrameCount);
        var missedSchedules = snapshots.Sum(snapshot => snapshot.MissedScheduleCount);
        var averageSend = snapshots.Sum(snapshot => snapshot.AverageSendDurationMicroseconds * Math.Max(1, snapshot.FrameCount)) / totalFrames;
        var maxSend = snapshots.Max(snapshot => snapshot.MaxSendDurationMicroseconds);
        var maxLateBy = snapshots.Max(snapshot => snapshot.MaxLateByMicroseconds);
        var status = ResolveWorstTxTimingStatus(snapshots);
        var label = status switch
        {
            TxTimingHealthStatus.Good => "GOOD",
            TxTimingHealthStatus.Warning => "WARN",
            TxTimingHealthStatus.Bad => "BAD",
            _ => "IDLE"
        };

        return $"TX Timing: {label} act={actualFps:0.0}/{targetFps:0.0}fps jitter={averageJitter:0}/{maxJitter:0}us late={lateFrames} missed={missedSchedules} send={averageSend:0}/{maxSend:0}us maxLate={maxLateBy:0}us";
    }

    private static TxTimingHealthStatus ResolveWorstTxTimingStatus(IEnumerable<TxTimingHealthSnapshot> snapshots)
    {
        var materialized = snapshots.ToArray();
        if (materialized.Any(snapshot => snapshot.Status == TxTimingHealthStatus.Bad))
            return TxTimingHealthStatus.Bad;
        if (materialized.Any(snapshot => snapshot.Status == TxTimingHealthStatus.Warning))
            return TxTimingHealthStatus.Warning;
        if (materialized.Any(snapshot => snapshot.Status == TxTimingHealthStatus.Good))
            return TxTimingHealthStatus.Good;
        return TxTimingHealthStatus.Idle;
    }

    private IReadOnlyList<PublisherRuntimeState> BuildPublisherRuntimeStates()
    {
        var selectedIndex = SelectedPublisherSlot?.Index ?? 1;
        var states = new List<PublisherRuntimeState>();
        foreach (var slot in PublisherSlots.Where(s => s.IsEnabled))
        {
            if (slot.SelectedStream?.Stream is not { } stream)
                continue;

            var channels = CaptureEffectiveChannelsFromSlot(slot);

            states.Add(new PublisherRuntimeState
            {
                SlotIndex = slot.Index,
                IsSelectedSlot = slot.Index == selectedIndex,
                Stream = stream,
                Source = MacAddress.Parse(slot.SourceMac),
                Destination = MacAddress.Parse(slot.DestinationMac),
                AppId = ParseAppId(slot.AppIdText),
                Vlan = ResolveVlanTag(slot.UseVlan, slot.VlanId, slot.VlanPriority),
                SampleRateHz = slot.SampleRateHz,
                NominalFrequencyHz = slot.NominalFrequencyHz,
                CurrentDlsb = slot.CurrentDlsb,
                VoltageDlsb = slot.VoltageDlsb,
                Quality = ResolveSampleQualityChoice(slot.SampleQualityKey).Quality,
                QualityLabel = ResolveSampleQualityChoice(slot.SampleQualityKey).ShortLabel,
                SvId = slot.StreamId.Trim(),
                DataSetReference = slot.DataSetReference.Trim(),
                SampleCounterWrap = ResolveSampleCounterWrap(stream, slot.SampleRateHz, slot.NominalFrequencyHz),
                NoAsdu = SampledValuesPublisherProfile.ResolveAsduPerFrame(stream),
                TimingHealth = new TxTimingHealth(SampledValuesPublisherProfile.ResolvePublicationRate(slot.SampleRateHz, SampledValuesPublisherProfile.ResolveAsduPerFrame(stream))),
                FrozenChannels = channels,
                OscillatorStates = channels.ToDictionary(
                    x => x.Key,
                    x => new OscillatorState { PhaseRadians = x.Value.AngleDegrees * Math.PI / 180.0, LastAngleDegrees = x.Value.AngleDegrees },
                    StringComparer.OrdinalIgnoreCase),
                SignalSource = slot.SignalSource,
                ComtradeDataset = slot.ComtradeDataset,
                ComtradeLoop = slot.ComtradeLoop,
                ComtradeChannelMap = slot.ComtradeChannelMap
            });
        }

        return states;
    }

    private static string ResolveChannelKind(string key)
        => key.StartsWith("I", StringComparison.OrdinalIgnoreCase) ? "I" : "V";

    private static async Task DelayUntilTicksAsync(long targetTicks, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var remainingTicks = targetTicks - Stopwatch.GetTimestamp();
            if (remainingTicks <= 0)
                return;

            var remainingMs = remainingTicks * 1000.0 / Stopwatch.Frequency;
            if (remainingMs > 2)
                await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(remainingMs - 1, 10)), cancellationToken).ConfigureAwait(false);
            else
                Thread.SpinWait(64);
        }
    }

    private sealed class PublisherRuntimeState
    {
        public int SlotIndex { get; init; }
        public bool IsSelectedSlot { get; init; }
        public required SclSampledValuesStream Stream { get; init; }
        public required MacAddress Source { get; init; }
        public required MacAddress Destination { get; init; }
        public VlanTag? Vlan { get; init; }
        public ushort AppId { get; init; }
        public double SampleRateHz { get; init; }
        public double NominalFrequencyHz { get; init; }
        public double CurrentDlsb { get; init; }
        public double VoltageDlsb { get; init; }
        public SampledValueQuality Quality { get; init; } = SampledValueQuality.Good;
        public string QualityLabel { get; init; } = "good";
        public string SvId { get; init; } = string.Empty;
        public string DataSetReference { get; init; } = string.Empty;
        public ushort? SampleCounterWrap { get; init; }
        public ushort NoAsdu { get; init; } = 1;
        public double PublicationRateHz => SampledValuesPublisherProfile.ResolvePublicationRate(SampleRateHz, NoAsdu);
        public required TxTimingHealth TimingHealth { get; init; }
        public ushort SampleCount { get; set; }
        public long Sent { get; set; }
        public required IReadOnlyDictionary<string, EffectiveChannel> FrozenChannels { get; init; }
        public required Dictionary<string, OscillatorState> OscillatorStates { get; init; }
        public PublisherSignalSource SignalSource { get; init; }
        public ComtradeDataset? ComtradeDataset { get; init; }
        public bool ComtradeLoop { get; init; }
        public IReadOnlyDictionary<string, int> ComtradeChannelMap { get; init; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public long DueTicks(long startedTicks)
            => startedTicks + (long)Math.Round(Sent * Stopwatch.Frequency / PublicationRateHz);
    }

private static async Task CapturePtpAsync(
    IProcessBusFrameSource frameSource,
    PtpPassiveMonitor monitor,
    PtpPublisherRuntime? labPtpPublisher,
    CancellationToken cancellationToken)
{
    var options = new ProcessBusCaptureOptions
    {
        Filter = PtpCaptureFilter,
        BufferCapacity = 2048,
        ReadTimeoutMilliseconds = 500
    };

    await foreach (var captured in frameSource.CaptureAsync(options, cancellationToken).ConfigureAwait(false))
    {
        var frameBytes = captured.Frame.ToArray();
        monitor.ObserveEthernetFrame(frameBytes, captured.Timestamp);

        if (labPtpPublisher is not null &&
            PtpPacketParser.TryParseEthernetFrame(frameBytes, out var ptpFrame) &&
            ptpFrame.Header.MessageType == PtpMessageType.PdelayReq)
        {
            await labPtpPublisher.RespondToPeerDelayRequestAsync(ptpFrame, cancellationToken).ConfigureAwait(false);
        }
    }
}

private PtpPublisherOptions BuildLabPtpOptions(MacAddress sourceMac, VlanTag? vlan)
    {
        var clockIdentity = ClockIdentity.TryParse(PtpClockIdentityText, out var parsedIdentity)
            ? parsedIdentity
            : DeriveClockIdentity(sourceMac);

        return new PtpPublisherOptions
        {
            DomainNumber = (byte)Math.Clamp(ExpectedPtpDomain, 0, 255),
            SourceMac = sourceMac.ToArray(),
            VlanId = vlan?.VlanId,
            VlanPriority = vlan?.PriorityCodePoint ?? (byte)Math.Clamp(VlanPriority, 0, 7),
            ClockIdentity = clockIdentity,
            PortNumber = 1,
            AnnounceInterval = TimeSpan.FromMilliseconds(PtpAnnounceIntervalMs),
            SyncInterval = TimeSpan.FromMilliseconds(PtpSyncIntervalMs),
            FollowUpDelay = TimeSpan.FromMilliseconds(2),
            RespondToPeerDelay = PtpRespondToPeerDelay,
            TwoStepClock = true
        };
    }

    private static ClockIdentity DeriveClockIdentity(MacAddress sourceMac)
    {
        var mac = sourceMac.ToArray();
        return new ClockIdentity(new byte[]
        {
            mac[0], mac[1], mac[2], 0xFF, 0xFE, mac[3], mac[4], mac[5]
        });
    }

    private PtpTimingHealthOptions BuildPtpHealthOptions()
    => new()
    {
        ExpectedDomainNumber = (byte)Math.Clamp(ExpectedPtpDomain, 0, 255),
        SourceTimeout = TimeSpan.FromSeconds(3),
        RequireAnnounce = true,
        RequireSync = true,
        RequireFollowUpForTwoStep = true,
        RequirePeerDelayActivity = true,
        MaximumSequenceAnomalies = 0
    };

private SmpSynchValue ResolveSampleSynchronization(PtpTimingHealthReport? report)
    => NormalizeSyncPolicyMode(SyncPolicyMode) switch
    {
        SvSyncPolicyMode.HonestUnsynchronized => SmpSynchValue.NotSynchronized,
        SvSyncPolicyMode.LocalCompatibility => SmpSynchValue.LocalSynchronized,
        SvSyncPolicyMode.GlobalCompatibility => SmpSynchValue.GlobalSynchronized,
        SvSyncPolicyMode.ExternalPtpAuto => report is null
            ? SmpSynchValue.NotSynchronized
            : PtpSmpSynchPolicy.Resolve(report, PtpAllowLocalFallback),
        _ => SmpSynchValue.NotSynchronized
    };

private void UpdatePtpStatus(PtpTimingHealthReport? report, SmpSynchValue smpSynch, bool live)
{
    if (!live)
    {
        PtpStatusText = "PTP RX: dry-run";
        SmpSynchStatusText = FormatSmpSynchStatus(smpSynch);
        return;
    }

    if (report is null)
    {
        PtpStatusText = "PTP RX: waiting";
        SmpSynchStatusText = FormatSmpSynchStatus(smpSynch);
        return;
    }

    var snapshot = report.Snapshot;
    var source = snapshot.Sources
        .Where(s => s.DomainNumber == Math.Clamp(ExpectedPtpDomain, 0, 255))
        .OrderByDescending(s => s.LastSeenAt)
        .FirstOrDefault()
        ?? snapshot.Sources.OrderByDescending(s => s.LastSeenAt).FirstOrDefault();

    if (source is null)
    {
        PtpStatusText = report.Severity == PtpHealthSeverity.Fail
            ? "PTP RX: not detected"
            : $"PTP RX: {report.Severity}";
    }
    else
    {
        var age = Math.Max(0, (snapshot.CapturedAt - source.LastSeenAt).TotalSeconds);
        PtpStatusText = $"PTP RX: {report.Severity} d={source.DomainNumber} src={source.SourcePortIdentity.ClockIdentity} age={age:0.0}s";
    }

    SmpSynchStatusText = FormatSmpSynchStatus(smpSynch);
}

private void UpdatePtpPublisherStatus(PtpPublisherRuntime? publisher)
{
    if (publisher is null)
    {
        PtpPublisherStatusText = "PTP TX: off";
        return;
    }

    var status = publisher.GetStatus();
    if (!string.IsNullOrWhiteSpace(status.LastError))
    {
        PtpPublisherStatusText = $"PTP TX: error {status.LastError}";
        return;
    }

    PtpPublisherStatusText = $"PTP TX: {(status.IsRunning ? "lab traffic" : "stopped")} A={status.AnnounceSent} S={status.SyncSent} FU={status.FollowUpSent} PD={status.PeerDelayResponsesSent}";
}

private string FormatSmpSynchStatus(SmpSynchValue value)
{
    var valueText = value switch
    {
        SmpSynchValue.GlobalSynchronized => "smpSynch=2",
        SmpSynchValue.LocalSynchronized => "smpSynch=1",
        _ => "smpSynch=0"
    };

    return NormalizeSyncPolicyMode(SyncPolicyMode) switch
    {
        SvSyncPolicyMode.GlobalCompatibility => $"{valueText} global compatibility",
        SvSyncPolicyMode.LocalCompatibility => $"{valueText} local compatibility",
        SvSyncPolicyMode.HonestUnsynchronized => $"{valueText} honest unsync",
        SvSyncPolicyMode.ExternalPtpAuto => $"{valueText} external PTP auto",
        _ => valueText
    };
}

private SvSyncPolicyChoice ResolveSyncPolicyChoice(SvSyncPolicyMode mode)
{
    var normalized = NormalizeSyncPolicyMode(mode);
    return SyncPolicyChoices.FirstOrDefault(choice => choice.Mode == normalized) ?? SyncPolicyChoices[0];
}

private static SvSyncPolicyMode NormalizeSyncPolicyMode(SvSyncPolicyMode mode)
    => mode switch
    {
        SvSyncPolicyMode.ExternalPtpAuto => SvSyncPolicyMode.ExternalPtpAuto,
        SvSyncPolicyMode.HonestUnsynchronized => SvSyncPolicyMode.HonestUnsynchronized,
        SvSyncPolicyMode.LocalCompatibility => SvSyncPolicyMode.LocalCompatibility,
        SvSyncPolicyMode.GlobalCompatibility => SvSyncPolicyMode.GlobalCompatibility,
        _ => SvSyncPolicyMode.GlobalCompatibility
    };


    private static IReadOnlyDictionary<string, double> ResolveComtradeInstantaneousValues(
        ComtradeSample sample,
        IReadOnlyDictionary<string, int> channelMap)
    {
        var values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in channelMap)
        {
            if (pair.Value >= 0 && pair.Value < sample.AnalogValues.Count)
                values[pair.Key] = sample.AnalogValues[pair.Value];
        }

        return values;
    }

    private byte[] BuildInstantaneousSamplePayload(
        SclSampledValuesStream stream,
        Iec61850UtcTime timestamp,
        IReadOnlyDictionary<string, double> instantaneousValues,
        double currentDlsb,
        double voltageDlsb,
        SampledValueQuality quality)
    {
        var layout = SampledValuesPayloadLayout.FromDataSet(stream.Entries);
        if (!layout.IsFullySupported)
            throw new InvalidOperationException("Unsupported SV payload layout: " + string.Join("; ", layout.UnsupportedElements.Select(x => $"{x.SignalReference} bType={x.BType}")));

        var entriesByIndex = stream.Entries.ToDictionary(x => x.Index);
        var values = new List<MmsDataValue>(layout.Elements.Count);
        foreach (var element in layout.Elements)
        {
            if (!entriesByIndex.TryGetValue(element.Index, out var entry))
                throw new InvalidOperationException($"SV payload layout entry {element.Index} has no matching DataSet entry.");

            if (element.Kind == SampledValuePayloadElementKind.Quality)
            {
                values.Add(MmsDataValue.BitString(0, quality.ToBytes(element.Width)));
                continue;
            }

            if (element.Kind == SampledValuePayloadElementKind.BitString ||
                element.Kind == SampledValuePayloadElementKind.EntryTime)
            {
                values.Add(MmsDataValue.BitString(0, new byte[element.Width]));
                continue;
            }

            if (element.Kind == SampledValuePayloadElementKind.Timestamp)
            {
                values.Add(MmsDataValue.UtcTime(timestamp));
                continue;
            }

            values.Add(BuildInstantaneousChannelValue(entry, element, instantaneousValues, currentDlsb, voltageDlsb, quality));
        }

        return SampledValuesPayloadBuilder.BuildPayload(layout, values);
    }

    private MmsDataValue BuildInstantaneousChannelValue(
        SclDataSetEntry entry,
        SampledValuePayloadElement element,
        IReadOnlyDictionary<string, double> instantaneousValues,
        double currentDlsb,
        double voltageDlsb,
        SampledValueQuality quality)
    {
        var key = ResolveSignalKey(entry);
        if (key is null || !instantaneousValues.TryGetValue(key, out var value))
            return ZeroValue(element);

        var dlsb = ResolveChannelKind(key) == "I" ? currentDlsb : voltageDlsb;
        if (dlsb <= 0)
            throw new InvalidOperationException("dLSB must be greater than 0.");

        var counts = value / dlsb;
        return element.Kind switch
        {
            SampledValuePayloadElementKind.Boolean => MmsDataValue.Boolean(Math.Abs(counts) >= 0.5),
            SampledValuePayloadElementKind.UInt8 or
            SampledValuePayloadElementKind.UInt16 or
            SampledValuePayloadElementKind.UInt24 or
            SampledValuePayloadElementKind.UInt32 or
            SampledValuePayloadElementKind.UInt64 => MmsDataValue.Unsigned((ulong)Math.Max(0, Math.Round(counts))),
            SampledValuePayloadElementKind.Float32 or
            SampledValuePayloadElementKind.Float64 => MmsDataValue.FloatingPoint((float)counts),
            _ => MmsDataValue.Integer((long)Math.Clamp(Math.Round(counts), long.MinValue, long.MaxValue))
        };
    }

    private byte[] BuildSamplePayload(
        SclSampledValuesStream stream,
        Iec61850UtcTime timestamp,
        IReadOnlyDictionary<string, EffectiveChannel> channels,
        double currentDlsb,
        double voltageDlsb,
        SampledValueQuality quality)
    {
        var layout = SampledValuesPayloadLayout.FromDataSet(stream.Entries);
        if (!layout.IsFullySupported)
            throw new InvalidOperationException("Unsupported SV payload layout: " + string.Join("; ", layout.UnsupportedElements.Select(x => $"{x.SignalReference} bType={x.BType}")));

        var entriesByIndex = stream.Entries.ToDictionary(x => x.Index);
        var values = new List<MmsDataValue>(layout.Elements.Count);
        foreach (var element in layout.Elements)
        {
            if (!entriesByIndex.TryGetValue(element.Index, out var entry))
                throw new InvalidOperationException($"SV payload layout entry {element.Index} has no matching DataSet entry.");

            if (element.Kind == SampledValuePayloadElementKind.Quality)
            {
                values.Add(MmsDataValue.BitString(0, quality.ToBytes(element.Width)));
                continue;
            }

            if (element.Kind == SampledValuePayloadElementKind.BitString ||
                element.Kind == SampledValuePayloadElementKind.EntryTime)
            {
                values.Add(MmsDataValue.BitString(0, new byte[element.Width]));
                continue;
            }

            if (element.Kind == SampledValuePayloadElementKind.Timestamp)
            {
                values.Add(MmsDataValue.UtcTime(timestamp));
                continue;
            }

            values.Add(BuildChannelValue(entry, element, channels, currentDlsb, voltageDlsb, quality));
        }

        return SampledValuesPayloadBuilder.BuildPayload(layout, values);
    }

    private MmsDataValue BuildChannelValue(
        SclDataSetEntry entry,
        SampledValuePayloadElement element,
        IReadOnlyDictionary<string, EffectiveChannel> channels,
        double currentDlsb,
        double voltageDlsb,
        SampledValueQuality quality)
    {
        var key = ResolveSignalKey(entry);
        if (key is null || !channels.TryGetValue(key, out var effective) || !effective.IsEnabled)
            return ZeroValue(element);

        var dlsb = effective.Kind == "I" ? currentDlsb : voltageDlsb;
        if (dlsb <= 0)
            throw new InvalidOperationException("dLSB must be greater than 0.");

        // Operator values are RMS phasors. IEC 61850-9-2 Sampled Values carry instantaneous samples,
        // therefore the RMS setpoint is converted to peak before dLSB scaling. P2 scenario shaping
        // adds lightweight publisher-side harmonic/DC/clipping approximations for lab stress workflows.
        var counts = effective.MagnitudeRms * Math.Sqrt(2.0) / dlsb;
        var sample = counts * Math.Sin(effective.PhaseRadians);
        if (effective.HarmonicPercent > 0)
        {
            var harmonicOrder = Math.Clamp(effective.HarmonicOrder, 2, 63);
            sample += counts * (effective.HarmonicPercent / 100.0) * Math.Sin(effective.PhaseRadians * harmonicOrder);
        }

        if (Math.Abs(effective.DcOffsetPercent) > 0)
            sample += counts * (effective.DcOffsetPercent / 100.0);

        if (effective.ClipPercent > 0 && effective.ClipPercent < 1000)
        {
            var limit = Math.Abs(counts * effective.ClipPercent / 100.0);
            if (limit > 0)
                sample = Math.Clamp(sample, -limit, limit);
        }

        return element.Kind switch
        {
            SampledValuePayloadElementKind.Boolean => MmsDataValue.Boolean(Math.Abs(sample) >= 0.5),
            SampledValuePayloadElementKind.UInt8 or
            SampledValuePayloadElementKind.UInt16 or
            SampledValuePayloadElementKind.UInt24 or
            SampledValuePayloadElementKind.UInt32 or
            SampledValuePayloadElementKind.UInt64 => MmsDataValue.Unsigned((ulong)Math.Max(0, Math.Round(sample))),
            SampledValuePayloadElementKind.Float32 or
            SampledValuePayloadElementKind.Float64 => MmsDataValue.FloatingPoint((float)sample),
            _ => MmsDataValue.Integer((long)Math.Clamp(Math.Round(sample), long.MinValue, long.MaxValue))
        };
    }

    private static MmsDataValue ZeroValue(SampledValuePayloadElement element)
        => element.Kind switch
        {
            SampledValuePayloadElementKind.Boolean => MmsDataValue.Boolean(false),
            SampledValuePayloadElementKind.UInt8 or
            SampledValuePayloadElementKind.UInt16 or
            SampledValuePayloadElementKind.UInt24 or
            SampledValuePayloadElementKind.UInt32 or
            SampledValuePayloadElementKind.UInt64 => MmsDataValue.Unsigned(0),
            SampledValuePayloadElementKind.Float32 or
            SampledValuePayloadElementKind.Float64 => MmsDataValue.FloatingPoint(0),
            _ => MmsDataValue.Integer(0)
        };

    private IReadOnlyDictionary<string, EffectiveChannel> CaptureBaseEffectiveChannels()
        => CaptureEffectiveChannelsFromSlot(SelectedPublisherSlot);

    private IReadOnlyDictionary<string, EffectiveChannel> CaptureEffectiveChannelsFromSlot(SvPublisherSlotViewModel? slot)
    {
        var snapshots = slot?.Channels is { Count: > 0 }
            ? slot.Channels
            : Channels.Select(c => c.ToSnapshot()).ToArray();

        return snapshots.ToDictionary(
            c => c.Key,
            c => new EffectiveChannel(
                ResolveChannelKind(c.Key),
                c.IsEnabled,
                c.Magnitude,
                c.AngleDegrees,
                c.FrequencyHz >= 0 ? c.FrequencyHz : (slot?.NominalFrequencyHz ?? NominalFrequencyHz),
                c.AngleDegrees * Math.PI / 180.0,
                c.DcOffsetPercent,
                c.HarmonicPercent,
                Math.Clamp(c.HarmonicOrder, 2, 63),
                c.ClipPercent),
            StringComparer.OrdinalIgnoreCase);
    }

    private IReadOnlyDictionary<string, EffectiveChannel> ResolveRuntimeBaseChannels(PublisherRuntimeState state)
    {
        if (!AutoApplyWhileRunning)
            return state.FrozenChannels;

        var slot = PublisherSlots.FirstOrDefault(item => item.Index == state.SlotIndex);
        return slot is null ? state.FrozenChannels : CaptureEffectiveChannelsFromSlot(slot);
    }

    private PublisherSessionPlan BuildPublisherSessionPlan()
    {
        return Mode switch
        {
            InjectionMode.Ramp => BuildRampSessionPlan(),
            InjectionMode.Sequencer => BuildSequencerSessionPlan(),
            _ => PublisherSessionPlan.ManualContinue()
        };
    }

    private PublisherSessionPlan BuildRampSessionPlan()
    {
        var segments = RampStates
            .Where(state => state.TimeSeconds > 0)
            .Select(state => new RampSessionSegment(
                state.Name,
                state.SignalKeys.ToArray(),
                state.From,
                state.To,
                Math.Max(0.001, state.TimeSeconds)))
            .ToArray();

        if (segments.Length == 0)
            throw new InvalidOperationException("Ramp mode needs at least one ramp state with duration greater than 0 s.");

        return PublisherSessionPlan.RampOnce(segments);
    }

    private PublisherSessionPlan BuildSequencerSessionPlan()
    {
        var segments = SequenceStates
            .Where(state => state.DurationSeconds > 0)
            .Select(state => new SequencerSessionSegment(
                state.Name,
                Math.Max(0.001, state.DurationSeconds),
                state.CurrentScale,
                NominalVoltageLn * Math.Max(0, state.VoltageScale),
                state.AngleShiftDegrees,
                state.FrequencyHz,
                state.CurrentScaleA,
                state.CurrentScaleB,
                state.CurrentScaleC,
                state.CurrentScaleN,
                state.VoltageScaleA,
                state.VoltageScaleB,
                state.VoltageScaleC,
                state.VoltageScaleN,
                state.AngleOffsetA,
                state.AngleOffsetB,
                state.AngleOffsetC,
                state.AngleOffsetN,
                state.CurrentDcOffsetPercent,
                state.VoltageDcOffsetPercent,
                state.CurrentHarmonicPercent,
                state.VoltageHarmonicPercent,
                state.HarmonicOrder,
                state.CurrentClipPercent,
                state.VoltageClipPercent,
                state.ScenarioTag))
            .ToArray();

        if (segments.Length == 0)
            throw new InvalidOperationException("Sequencer mode needs at least one state with duration greater than 0 s.");

        return LoopSequence
            ? PublisherSessionPlan.SequencerLoop(segments)
            : PublisherSessionPlan.SequencerOnce(segments);
    }

    private static long? MinLimit(long? first, long? second)
    {
        if (first is null)
            return second;

        if (second is null)
            return first;

        return Math.Min(first.Value, second.Value);
    }

    private static double PhaseOffsetForChannel(string channelKey)
    {
        if (string.Equals(channelKey, "Vb", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(channelKey, "Ib", StringComparison.OrdinalIgnoreCase))
            return -120;

        if (string.Equals(channelKey, "Vc", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(channelKey, "Ic", StringComparison.OrdinalIgnoreCase))
            return 120;

        return 0;
    }

    private void ApplyStreamMetadataToSlot(SvPublisherSlotViewModel slot, SvStreamChoice? choice)
    {
        slot.SelectedStream = choice;
        if (choice is null)
        {
            slot.StreamId = string.Empty;
            slot.StreamControlBlock = string.Empty;
            slot.DataSetReference = string.Empty;
            slot.DataSetEntryCount = 0;
            slot.MappedSignalCount = 0;
            slot.PayloadBytes = 0;
            return;
        }

        var stream = choice.Stream;
        slot.StreamControlBlock = stream.ControlBlockReference;
        slot.StreamId = stream.SvId;
        slot.DataSetReference = stream.DataSetReference;
        slot.AppIdText = stream.Address.AppId.HasValue ? $"0x{stream.Address.AppId.Value:X4}" : stream.Address.AppIdText;
        slot.DestinationMac = stream.Address.DestinationMac?.ToString() ?? stream.Address.DestinationMacText;
        slot.UseVlan = stream.Address.VlanId.HasValue;
        slot.VlanId = stream.Address.VlanId ?? 0;
        slot.VlanPriority = stream.Address.VlanPriority ?? 4;
        var sampleRate = ResolveStreamSampleRateHz(stream, slot.NominalFrequencyHz);
        if (sampleRate > 0)
        {
            slot.SampleRateHz = sampleRate;
            slot.SampleRatePresetKey = SampleRatePresets.FirstOrDefault(preset => Math.Abs(preset.SampleRateHz - sampleRate) < 0.5)?.Key ?? slot.SampleRatePresetKey;
        }
        slot.DataSetEntryCount = stream.Entries.Count;
        slot.MappedSignalCount = stream.Entries.Count(e => !e.IsQuality && !e.IsTimestamp && ResolveSignalKey(e) is not null);
        slot.PayloadBytes = EstimatePayloadBytes(stream.Entries);
    }

    private void SaveCurrentPublisherSlot()
    {
        if (_isLoadingPublisherSlot || _selectedPublisherSlot is not { } slot)
            return;

        slot.SelectedStream = SelectedStream;
        slot.StreamId = StreamId;
        slot.StreamControlBlock = StreamControlBlock;
        slot.DataSetReference = DataSetReference;
        slot.AppIdText = AppIdText;
        slot.DestinationMac = DestinationMac;
        slot.SourceMac = SourceMac;
        slot.UseVlan = UseVlan;
        slot.VlanId = VlanId;
        slot.VlanPriority = VlanPriority;
        slot.SampleRateHz = SampleRateHz;
        slot.NominalFrequencyHz = NominalFrequencyHz;
        slot.CurrentDlsb = CurrentDlsb;
        slot.VoltageDlsb = VoltageDlsb;
        slot.ManualSetMode = ManualSetMode;
        slot.SampleRatePresetKey = SelectedSampleRatePreset?.Key ?? slot.SampleRatePresetKey;
        slot.SampleQualityKey = SelectedSampleQualityChoice.Key;
        slot.DataSetEntryCount = DataSetEntryCount;
        slot.MappedSignalCount = MappedSignalCount;
        slot.PayloadBytes = PayloadBytes;
        slot.Channels = Channels.Select(c => c.ToSnapshot()).ToArray();
    }

    private void LoadPublisherSlot(SvPublisherSlotViewModel slot)
    {
        _isLoadingPublisherSlot = true;
        try
        {
            SelectedStream = slot.SelectedStream;
            StreamId = slot.StreamId;
            StreamControlBlock = slot.StreamControlBlock;
            DataSetReference = slot.DataSetReference;
            AppIdText = slot.AppIdText;
            DestinationMac = slot.DestinationMac;
            SourceMac = slot.SourceMac;
            UseVlan = slot.UseVlan;
            VlanId = slot.VlanId;
            VlanPriority = slot.VlanPriority;
            SampleRateHz = slot.SampleRateHz;
            NominalFrequencyHz = slot.NominalFrequencyHz;
            CurrentDlsb = slot.CurrentDlsb;
            VoltageDlsb = slot.VoltageDlsb;
            ManualSetMode = slot.ManualSetMode;
            SelectedSampleRatePreset = SampleRatePresets.FirstOrDefault(p => p.Key == slot.SampleRatePresetKey)
                ?? SampleRatePresets.FirstOrDefault(p => Math.Abs(p.SampleRateHz - slot.SampleRateHz) < 0.5)
                ?? SampleRatePresets.FirstOrDefault();
            SelectedSampleQualityChoice = ResolveSampleQualityChoice(slot.SampleQualityKey);

            if (slot.Channels.Count > 0)
            {
                foreach (var snapshot in slot.Channels)
                    SetChannel(snapshot.Key, snapshot.Magnitude, snapshot.AngleDegrees, snapshot.IsEnabled, snapshot.FrequencyHz, snapshot.DcOffsetPercent, snapshot.HarmonicPercent, snapshot.HarmonicOrder, snapshot.ClipPercent);
            }

            DataSetEntryCount = slot.DataSetEntryCount;
            MappedSignalCount = slot.MappedSignalCount;
            PayloadBytes = slot.PayloadBytes;
            RebuildManualRowsFromChannels();
            LiveApplyText = $"Editing {slot.Header}. {slot.SummaryText}. {slot.SourceText}";
        }
        finally
        {
            _isLoadingPublisherSlot = false;
        }
    }

    private void ValidateBeforeRun(bool live)
    {
        SaveCurrentPublisherSlot();
        var activeSlots = PublisherSlots.Where(s => s.IsEnabled).ToArray();
        if (activeSlots.Length == 0)
            throw new InvalidOperationException("Enable at least one IED / MU publisher slot.");

        foreach (var slot in activeSlots)
        {
            var selectedSlotStream = slot.SelectedStream
                ?? throw new InvalidOperationException($"{slot.Header}: select an SV stream first.");

            if (slot.SampleRateHz <= 0)
                throw new InvalidOperationException($"{slot.Header}: sample rate must be greater than 0.");

            if (!MacAddress.TryParse(slot.SourceMac, out _))
                throw new InvalidOperationException($"{slot.Header}: source MAC is invalid.");

            if (!MacAddress.TryParse(slot.DestinationMac, out _))
                throw new InvalidOperationException($"{slot.Header}: destination MAC is invalid.");

            _ = ParseAppId(slot.AppIdText);
            _ = ResolveVlanTag(slot.UseVlan, slot.VlanId, slot.VlanPriority);

            if (slot.NominalFrequencyHz <= 0)
                throw new InvalidOperationException($"{slot.Header}: nominal frequency must be greater than 0.");

            if (slot.CurrentDlsb <= 0 || slot.VoltageDlsb <= 0)
                throw new InvalidOperationException($"{slot.Header}: current and voltage dLSB must be greater than 0.");

            if (slot.SignalSource == PublisherSignalSource.ComtradeReplay && slot.ComtradeDataset is null)
                throw new InvalidOperationException($"{slot.Header}: COMTRADE replay is selected but no COMTRADE file is loaded.");

            var stream = selectedSlotStream.Stream;
            var noAsdu = SampledValuesPublisherProfile.ResolveAsduPerFrame(stream);
            if (noAsdu > SampledValuesPublisherProfile.MaxAsduPerFrame)
                throw new InvalidOperationException($"{slot.Header}: SV stream declares nofASDU={stream.NoAsdu}. This publisher supports up to {SampledValuesPublisherProfile.MaxAsduPerFrame} ASDUs per frame.");

            var layout = SampledValuesPayloadLayout.FromDataSet(stream.Entries);
            if (!layout.IsFullySupported)
                throw new InvalidOperationException($"{slot.Header}: unsupported SV payload layout: " + string.Join("; ", layout.UnsupportedElements.Select(x => $"{x.SignalReference} bType={x.BType}")));
        }

        if (!Continuous && DurationSeconds <= 0)
            throw new InvalidOperationException("Duration must be greater than 0 for finite publish.");

        if (live && SelectedAdapter is null)
            throw new InvalidOperationException("Select a NIC adapter before live publishing.");
    }

    private void ApplySelectedStream(SvStreamChoice? choice)
    {
        if (choice is null)
        {
            StreamId = string.Empty;
            StreamControlBlock = string.Empty;
            DataSetReference = string.Empty;
            DataSetEntryCount = 0;
            MappedSignalCount = 0;
            return;
        }

        var stream = choice.Stream;
        StreamControlBlock = stream.ControlBlockReference;
        StreamId = stream.SvId;
        DataSetReference = stream.DataSetReference;
        AppIdText = stream.Address.AppId.HasValue ? $"0x{stream.Address.AppId.Value:X4}" : stream.Address.AppIdText;
        DestinationMac = stream.Address.DestinationMac?.ToString() ?? stream.Address.DestinationMacText;
        UseVlan = stream.Address.VlanId.HasValue;
        VlanId = stream.Address.VlanId ?? 0;
        VlanPriority = stream.Address.VlanPriority ?? 4;
        var streamSampleRateHz = ResolveStreamSampleRateHz(stream, NominalFrequencyHz);
        if (streamSampleRateHz > 0)
        {
            SampleRateHz = streamSampleRateHz;
            SelectedSampleRatePreset = SampleRatePresets.FirstOrDefault(preset => Math.Abs(preset.SampleRateHz - streamSampleRateHz) < 0.5)
                ?? SelectedSampleRatePreset;
        }
        DataSetEntryCount = stream.Entries.Count;
        MappedSignalCount = stream.Entries.Count(e => !e.IsQuality && !e.IsTimestamp && ResolveSignalKey(e) is not null);
        PayloadBytes = EstimatePayloadBytes(stream.Entries);
        AppendEvent($"Selected SV stream #{choice.Index}: {stream.ControlBlockReference}");
        AppendEvent($"DataSet entries={DataSetEntryCount}, mapped SV signals={MappedSignalCount}, payload={PayloadBytes} bytes.");
    }

    private VlanTag? ResolveVlanTag()
        => ResolveVlanTag(UseVlan, VlanId, VlanPriority);

    private static VlanTag? ResolveVlanTag(bool useVlan, int vlanId, int vlanPriority)
    {
        if (!useVlan)
            return null;

        if (vlanId is < 0 or > 4094)
            throw new InvalidOperationException("VLAN ID must be 0..4094.");

        if (vlanPriority is < 0 or > 7)
            throw new InvalidOperationException("VLAN priority must be 0..7.");

        return new VlanTag((byte)vlanPriority, false, (ushort)vlanId);
    }

    private static ushort ParseAppId(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("APPID is required.");

        var value = text.Trim();
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            if (ushort.TryParse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
                return hex;
        }

        if (ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            return number;

        if (ushort.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var implicitHex))
            return implicitHex;

        throw new InvalidOperationException("APPID must be a 16-bit decimal value or hex value like 0x4000.");
    }

    private SignalChannelViewModel? ResolveChannel(SclDataSetEntry entry)
    {
        var key = ResolveSignalKey(entry);
        return key is null ? null : Channels.FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ResolveSignalKey(SclDataSetEntry entry)
    {
        if (!int.TryParse(entry.LnInst, NumberStyles.Integer, CultureInfo.InvariantCulture, out var instance))
            return null;

        return entry.LnClass.ToUpperInvariant() switch
        {
            "TCTR" => instance switch
            {
                1 => "Ia",
                2 => "Ib",
                3 => "Ic",
                4 => "In",
                _ => null
            },
            "TVTR" => instance switch
            {
                1 => "Va",
                2 => "Vb",
                3 => "Vc",
                4 => "Vn",
                _ => null
            },
            _ => null
        };
    }

    private static double ResolveStreamSampleRateHz(SclSampledValuesStream stream, double nominalFrequencyHz)
    {
        if (stream.SampleRate <= 0)
            return 0;

        return MapSampleMode(stream.SampleMode) switch
        {
            0 when nominalFrequencyHz > 0 => stream.SampleRate * nominalFrequencyHz,
            1 => stream.SampleRate,
            _ => stream.SampleRate
        };
    }

    private static int EstimatePayloadBytes(IEnumerable<SclDataSetEntry> entries)
        => SampledValuesPayloadLayout.FromDataSet(entries.ToArray()).PayloadByteLength;

    private static ushort? ToSampleRate(double sampleRateHz, double nominalFrequencyHz, string sampleMode)
    {
        if (sampleRateHz <= 0 || nominalFrequencyHz <= 0)
            return null;

        var mode = MapSampleMode(sampleMode);
        var value = mode switch
        {
            0 => sampleRateHz / nominalFrequencyHz,
            1 => sampleRateHz,
            _ => sampleRateHz
        };

        return value <= 0 || value > ushort.MaxValue
            ? null
            : (ushort)Math.Round(value);
    }

    private static ushort? MapSampleMode(string sampleMode)
        => sampleMode.Trim() switch
        {
            "SmpPerPeriod" => 0,
            "SmpPerSec" => 1,
            "SecPerSmp" => 2,
            _ => null
        };

    private static ushort? ResolveSampleCounterWrap(SclSampledValuesStream stream, double sampleRateHz, double nominalFrequencyHz)
    {
        var mode = MapSampleMode(stream.SampleMode);
        var samplesPerSecond = mode switch
        {
            0 when sampleRateHz > 0 => sampleRateHz,
            1 when sampleRateHz > 0 => sampleRateHz,
            _ => 0
        };

        if (samplesPerSecond <= 0 || samplesPerSecond > ushort.MaxValue)
            return null;

        return (ushort)Math.Round(samplesPerSecond);
    }

    private static ushort IncrementSampleCount(ushort current, ushort? wrap)
        => SampleCounterPolicy.Increment(current, wrap);

    private static async Task DelayUntilSampleAsync(long startedTicks, long sampleIndex, double sampleRateHz, CancellationToken cancellationToken)
    {
        var targetTicks = startedTicks + (long)Math.Round(sampleIndex * Stopwatch.Frequency / sampleRateHz);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var remainingTicks = targetTicks - Stopwatch.GetTimestamp();
            if (remainingTicks <= 0)
                return;

            var remainingMs = remainingTicks * 1000.0 / Stopwatch.Frequency;
            if (remainingMs > 2)
                await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(remainingMs - 1, 10)), cancellationToken).ConfigureAwait(false);
            else
                Thread.SpinWait(64);
        }
    }

    public void SetManualContext(ManualOutputRowViewModel? row, string columnHeader)
    {
        _contextManualRow = row;
        _contextColumnHeader = columnHeader ?? string.Empty;
        OnPropertyChanged(nameof(IsContextValueColumn));
        OnPropertyChanged(nameof(IsContextAngleColumn));
        OnPropertyChanged(nameof(IsContextFrequencyColumn));
        OnPropertyChanged(nameof(IsContextSignalColumn));
    }

    public bool CommitManualRowText(ManualOutputRowViewModel row, string propertyName, out string warning)
    {
        if (row is null)
            throw new ArgumentNullException(nameof(row));

        var committed = row.CommitText(propertyName, out warning);
        if (!committed)
        {
            LiveApplyText = warning;
            return false;
        }

        return true;
    }

    private void StopPublisher()
        => _publisherStop?.Cancel();

    private void RebuildManualRowsFromChannels()
    {
        _isUpdatingManualRows = true;
        try
        {
            ManualRows.Clear();
            if (ManualSetMode == SymmetricalSetMode)
                AddSymmetricalRowsFromChannels();
            else if (ManualSetMode == LineLineSetMode)
                AddLineLineRowsFromChannels();
            else
                AddDirectRowsFromChannels();
        }
        finally
        {
            _isUpdatingManualRows = false;
        }

        ProjectManualRowsToChannels($"Mode={ManualSetMode}");
    }

    private void AddDirectRowsFromChannels()
    {
        AddManualRow("Va", "V L1-E", "V", "V", Channel("Va"));
        AddManualRow("Vb", "V L2-E", "V", "V", Channel("Vb"));
        AddManualRow("Vc", "V L3-E", "V", "V", Channel("Vc"));
        AddManualRow("Vn", "V N", "V", "V", Channel("Vn"));
        AddManualRow("Ia", "I L1", "I", "A", Channel("Ia"));
        AddManualRow("Ib", "I L2", "I", "A", Channel("Ib"));
        AddManualRow("Ic", "I L3", "I", "A", Channel("Ic"));
        AddManualRow("In", "I N", "I", "A", Channel("In"));
    }

    private void AddSymmetricalRowsFromChannels()
    {
        var v = ToSymmetricalComponents(ChannelPhasor("Va"), ChannelPhasor("Vb"), ChannelPhasor("Vc"));
        var i = ToSymmetricalComponents(ChannelPhasor("Ia"), ChannelPhasor("Ib"), ChannelPhasor("Ic"));
        var voltageFrequency = Channel("Va")?.FrequencyHz ?? NominalFrequencyHz;
        var currentFrequency = Channel("Ia")?.FrequencyHz ?? NominalFrequencyHz;

        AddManualRow("V1", "V 1", "V", "V", v.Positive, voltageFrequency, v.Positive.Magnitude > 0.000001);
        AddManualRow("V2", "V 2", "V", "V", v.Negative, voltageFrequency, v.Negative.Magnitude > 0.000001);
        AddManualRow("V0", "V 0", "V", "V", v.Zero, voltageFrequency, v.Zero.Magnitude > 0.000001);
        AddManualRow("I1", "I 1", "I", "A", i.Positive, currentFrequency, i.Positive.Magnitude > 0.000001);
        AddManualRow("I2", "I 2", "I", "A", i.Negative, currentFrequency, i.Negative.Magnitude > 0.000001);
        AddManualRow("I0", "I 0", "I", "A", i.Zero, currentFrequency, i.Zero.Magnitude > 0.000001);
    }

    private void AddLineLineRowsFromChannels()
    {
        AddManualRow("Vab", "V L1-L2", "V", "V", ChannelPhasor("Va") - ChannelPhasor("Vb"), Channel("Va")?.FrequencyHz ?? NominalFrequencyHz, true);
        AddManualRow("Vbc", "V L2-L3", "V", "V", ChannelPhasor("Vb") - ChannelPhasor("Vc"), Channel("Vb")?.FrequencyHz ?? NominalFrequencyHz, true);
        AddManualRow("Vca", "V L3-L1", "V", "V", ChannelPhasor("Vc") - ChannelPhasor("Va"), Channel("Vc")?.FrequencyHz ?? NominalFrequencyHz, true);
        AddManualRow("Ia", "I L1", "I", "A", Channel("Ia"));
        AddManualRow("Ib", "I L2", "I", "A", Channel("Ib"));
        AddManualRow("Ic", "I L3", "I", "A", Channel("Ic"));
        AddManualRow("In", "I N", "I", "A", Channel("In"));
    }

    private void AddManualRow(string key, string name, string kind, string unit, SignalChannelViewModel? channel)
        => ManualRows.Add(new ManualOutputRowViewModel(
            key,
            DisplaySignalName(key, name),
            kind,
            unit,
            channel?.Magnitude ?? 0,
            channel?.AngleDegrees ?? 0,
            channel?.FrequencyHz ?? NominalFrequencyHz,
            channel?.IsEnabled ?? false,
            ManualRowChanged));

    private void AddManualRow(string key, string name, string kind, string unit, Complex phasor, double frequencyHz, bool isEnabled)
        => ManualRows.Add(new ManualOutputRowViewModel(
            key,
            DisplaySignalName(key, name),
            kind,
            unit,
            phasor.Magnitude,
            NormalizeDegrees(phasor.Phase * 180.0 / Math.PI),
            frequencyHz,
            isEnabled,
            ManualRowChanged));

    private void ManualRowChanged(ManualOutputRowViewModel row, string propertyName)
    {
        if (_isUpdatingManualRows)
            return;

        if (LinkFrequencies && propertyName == nameof(ManualOutputRowViewModel.FrequencyHz) && row.FrequencyHz > 0)
            NominalFrequencyHz = row.FrequencyHz;
        else
            ProjectManualRowsToChannels($"Edited {row.Name}");
    }

    private void ProjectManualRowsToChannels(string reason)
    {
        if (_isUpdatingManualRows)
            return;

        if (ManualSetMode == SymmetricalSetMode)
            ProjectSymmetricalRowsToChannels();
        else if (ManualSetMode == LineLineSetMode)
            ProjectLineLineRowsToChannels();
        else
            ProjectDirectRowsToChannels();

        UpdateRampPreview();

        LiveApplyText = IsPublishing
            ? $"RUN auto-applied: {reason}"
            : $"Ready: {reason}";

        if (!_isLoadingPublisherSlot)
            SaveCurrentPublisherSlot();
    }

    private void ProjectDirectRowsToChannels()
    {
        foreach (var row in ManualRows)
        {
            if (Channel(row.Key) is { } channel)
                SetChannel(channel.Key, row.Magnitude, row.AngleDegrees, row.IsEnabled, row.FrequencyHz);
        }
    }

    private void ProjectSymmetricalRowsToChannels()
    {
        var a = Complex.FromPolarCoordinates(1, 120.0 * Math.PI / 180.0);
        var a2 = a * a;
        var v0 = RowPhasor("V0");
        var v1 = RowPhasor("V1");
        var v2 = RowPhasor("V2");
        var i0 = RowPhasor("I0");
        var i1 = RowPhasor("I1");
        var i2 = RowPhasor("I2");
        var voltageFrequency = RowFrequency("V1", "V2", "V0");
        var currentFrequency = RowFrequency("I1", "I2", "I0");
        var voltageEnabled = IsAnyRowEnabled("V1", "V2", "V0");
        var currentEnabled = IsAnyRowEnabled("I1", "I2", "I0");

        SetChannelFromPhasor("Va", v0 + v1 + v2, voltageEnabled, voltageFrequency);
        SetChannelFromPhasor("Vb", v0 + (a2 * v1) + (a * v2), voltageEnabled, voltageFrequency);
        SetChannelFromPhasor("Vc", v0 + (a * v1) + (a2 * v2), voltageEnabled, voltageFrequency);
        SetChannel("Vn", 0, 0, false, voltageFrequency);
        SetChannelFromPhasor("Ia", i0 + i1 + i2, currentEnabled, currentFrequency);
        SetChannelFromPhasor("Ib", i0 + (a2 * i1) + (a * i2), currentEnabled, currentFrequency);
        SetChannelFromPhasor("Ic", i0 + (a * i1) + (a2 * i2), currentEnabled, currentFrequency);
        SetChannel("In", 0, 0, false, currentFrequency);
    }

    private void ProjectLineLineRowsToChannels()
    {
        var vab = RowPhasor("Vab");
        var vbc = RowPhasor("Vbc");
        var vca = RowPhasor("Vca");
        var voltageEnabled = IsAnyRowEnabled("Vab", "Vbc", "Vca");
        var voltageFrequency = RowFrequency("Vab", "Vbc", "Vca");

        SetChannelFromPhasor("Va", (vab - vca) / 3.0, voltageEnabled, voltageFrequency);
        SetChannelFromPhasor("Vb", (vbc - vab) / 3.0, voltageEnabled, voltageFrequency);
        SetChannelFromPhasor("Vc", (vca - vbc) / 3.0, voltageEnabled, voltageFrequency);
        SetChannel("Vn", 0, 0, false, voltageFrequency);

        foreach (var key in new[] { "Ia", "Ib", "Ic", "In" })
        {
            if (Row(key) is { } row)
                SetChannel(key, row.Magnitude, row.AngleDegrees, row.IsEnabled, row.FrequencyHz);
        }
    }

    private void ApplySignalNaming(string scheme)
    {
        var normalized = scheme.Trim().ToUpperInvariant() switch
        {
            "ABC" => "ABC",
            "RSTN" => "RSTN",
            "RAW" => "RAW",
            _ => "L1L2L3E"
        };

        SignalNamingScheme = normalized;
        ApplyChannelNaming();
        foreach (var row in ManualRows)
            row.Name = DisplaySignalName(row.Key, row.Name);

        LiveApplyText = $"Signal naming changed to {normalized}.";
        AppendEvent(LiveApplyText);
    }

    private void ApplyChannelNaming()
    {
        foreach (var channel in Channels)
            channel.Name = DisplaySignalName(channel.Key, channel.Name);

        UpdateRampPreview();
        UpdateSequencePreview();
    }

    private string DisplaySignalName(string key, string fallback)
        => SignalNamingScheme switch
        {
            "ABC" => key switch
            {
                "Va" => "V A-E",
                "Vb" => "V B-E",
                "Vc" => "V C-E",
                "Vn" => "V N",
                "Ia" => "I A",
                "Ib" => "I B",
                "Ic" => "I C",
                "In" => "I N",
                "Vab" => "V A-B",
                "Vbc" => "V B-C",
                "Vca" => "V C-A",
                _ => key
            },
            "RSTN" => key switch
            {
                "Va" => "V R-N",
                "Vb" => "V S-N",
                "Vc" => "V T-N",
                "Vn" => "V N",
                "Ia" => "I R",
                "Ib" => "I S",
                "Ic" => "I T",
                "In" => "I N",
                "Vab" => "V R-S",
                "Vbc" => "V S-T",
                "Vca" => "V T-R",
                _ => key
            },
            "RAW" => key,
            _ => key switch
            {
                "Va" => "V L1-E",
                "Vb" => "V L2-E",
                "Vc" => "V L3-E",
                "Vn" => "V N",
                "Ia" => "I L1",
                "Ib" => "I L2",
                "Ic" => "I L3",
                "In" => "I N",
                "Vab" => "V L1-L2",
                "Vbc" => "V L2-L3",
                "Vca" => "V L3-L1",
                _ => fallback
            }
        };

    private void NominalValueFromContext()
    {
        if (_contextManualRow is not { } row)
            return;

        SetRowsInBatch(() => row.Magnitude = ResolveNominalMagnitude(row), $"Nominal value applied to {row.Name}");
        AppendEvent($"Nominal value applied to {row.Name}.");
    }

    private void ZeroFromContext()
    {
        if (_contextManualRow is not { } row)
            return;

        SetRowsInBatch(() =>
        {
            if (IsContextAngleColumn)
                row.AngleDegrees = 0;
            else if (IsContextFrequencyColumn)
                row.FrequencyHz = 0;
            else
                row.Magnitude = 0;
        }, $"Zero applied to {row.Name}");
        AppendEvent($"Zero applied to {row.Name} {_contextColumnHeader}.");
    }

    private void EqualMagnitudesFromContext()
    {
        if (_contextManualRow is not { } anchor)
            return;

        if (!TryResolveMagnitudeGroup(anchor.Key, anchor.Kind, out var keys))
        {
            LiveApplyText = "Equal Magnitudes is available only for compatible voltage/current rows.";
            return;
        }

        var value = anchor.Magnitude;
        SetRowsInBatch(() =>
        {
            foreach (var key in keys)
            {
                if (Row(key) is { } row)
                    row.Magnitude = value;
            }
        }, $"Equal magnitudes from {anchor.Name}");
        AppendEvent($"Equal magnitudes applied using {anchor.Name}={value:0.000} {anchor.Unit} as reference.");
    }

    private void LineAngleFromContext()
    {
        if (_contextManualRow is not { } row)
            return;

        SetRowsInBatch(() => row.AngleDegrees = ResolveLineAngle(row.Key), $"Line angle applied to {row.Name}");
        AppendEvent($"Line angle applied to {row.Name}.");
    }

    private void BalanceAnglesFromContext()
    {
        if (_contextManualRow is not { } anchor)
        {
            LiveApplyText = "Right-click an angle cell first.";
            return;
        }

        if (!TryResolveBalanceGroup(anchor.Key, reverse: false, out var keys, out var offsets))
        {
            LiveApplyText = "Balance Angles is available only for three-phase voltage/current angle rows.";
            return;
        }

        var anchorIndex = Array.FindIndex(keys, key => string.Equals(key, anchor.Key, StringComparison.OrdinalIgnoreCase));
        if (anchorIndex < 0)
            return;

        var anchorAngle = anchor.AngleDegrees;
        var baseAngle = anchorAngle - offsets[anchorIndex];
        SetRowsInBatch(() =>
        {
            for (var i = 0; i < keys.Length; i++)
            {
                if (Row(keys[i]) is { } row)
                    row.AngleDegrees = baseAngle + offsets[i];
            }
        }, $"Balance angles from {anchor.Name}");

        AppendEvent($"Balance angles applied using {anchor.Name} as anchor. Anchor angle stayed {anchorAngle:0.000} deg.");
    }

    private static bool TryResolveBalanceGroup(string key, bool reverse, out string[] keys, out double[] offsets)
    {
        var isLineLine = IsAnyKey(key, "Vab", "Vbc", "Vca");
        if (isLineLine)
        {
            keys = ["Vab", "Vbc", "Vca"];
            offsets = reverse ? [-30, 90, -150] : [30, -90, 150];
            return true;
        }

        if (IsAnyKey(key, "Va", "Vb", "Vc"))
        {
            keys = ["Va", "Vb", "Vc"];
            offsets = reverse ? [0, 120, -120] : [0, -120, 120];
            return true;
        }

        if (IsAnyKey(key, "Ia", "Ib", "Ic"))
        {
            keys = ["Ia", "Ib", "Ic"];
            offsets = reverse ? [0, 120, -120] : [0, -120, 120];
            return true;
        }

        keys = [];
        offsets = [];
        return false;
    }

    private static bool TryResolveMagnitudeGroup(string key, string kind, out string[] keys)
    {
        if (IsAnyKey(key, "Va", "Vb", "Vc"))
        {
            keys = ["Va", "Vb", "Vc"];
            return true;
        }

        if (IsAnyKey(key, "Vab", "Vbc", "Vca"))
        {
            keys = ["Vab", "Vbc", "Vca"];
            return true;
        }

        if (IsAnyKey(key, "Ia", "Ib", "Ic"))
        {
            keys = ["Ia", "Ib", "Ic"];
            return true;
        }

        if (IsAnyKey(key, "V1", "V2", "V0"))
        {
            keys = ["V1", "V2", "V0"];
            return true;
        }

        if (IsAnyKey(key, "I1", "I2", "I0"))
        {
            keys = ["I1", "I2", "I0"];
            return true;
        }

        keys = string.Equals(kind, "V", StringComparison.OrdinalIgnoreCase)
            ? ["Va", "Vb", "Vc"]
            : ["Ia", "Ib", "Ic"];
        return false;
    }

    private static bool IsAnyKey(string key, params string[] candidates)
        => candidates.Any(candidate => string.Equals(candidate, key, StringComparison.OrdinalIgnoreCase));

    private static double ResolveNominalMagnitude(ManualOutputRowViewModel row)
        => row.Key switch
        {
            "Vab" or "Vbc" or "Vca" => NominalVoltageLl,
            "Va" or "Vb" or "Vc" or "V1" => NominalVoltageLn,
            "Ia" or "Ib" or "Ic" or "I1" => NominalCurrent,
            _ => 0
        };

    private static double ResolveLineAngle(string key)
        => key switch
        {
            "Vab" => 30,
            "Vbc" => -90,
            "Vca" => 150,
            "Vb" or "Ib" => -120,
            "Vc" or "Ic" => 120,
            _ => 0
        };

    private void ApplyBalancedDefaults()
    {
        ApplyNominalValues();
        AppendEvent("Balanced 3-phase defaults applied.");
    }

    private void ApplyNominalValues()
    {
        if (ManualSetMode == SymmetricalSetMode)
            ApplySymmetricalNominalValues(reverse: false);
        else if (ManualSetMode == LineLineSetMode)
            ApplyLineLineNominalValues(reverse: false, loadCurrent: NominalCurrent);
        else
            ApplyDirectNominalValues(reverse: false, loadCurrent: NominalCurrent);

        AppendEvent("Nominal value preset applied.");
    }

    private void ZeroOutputs()
    {
        SetRowsInBatch(() =>
        {
            foreach (var row in ManualRows)
            {
                row.Magnitude = 0;
                row.IsEnabled = true;
            }
        }, "Zero output applied");
        AppendEvent("Zero output: all manual setpoint magnitudes set to zero.");
    }

    private void EqualMagnitudes()
    {
        SetRowsInBatch(() =>
        {
            var voltage = ManualRows.Where(r => r.Kind == "V" && r.Magnitude > 0).Select(r => r.Magnitude).DefaultIfEmpty(NominalVoltageLn).First();
            var current = ManualRows.Where(r => r.Kind == "I" && r.Magnitude > 0).Select(r => r.Magnitude).DefaultIfEmpty(NominalCurrent).First();
            foreach (var row in ManualRows)
                row.Magnitude = row.Kind == "V" ? voltage : current;
        }, "Equal magnitudes applied");
        AppendEvent("Equal magnitudes applied per signal group.");
    }

    private void ApplyHundredPercentLoad()
    {
        if (ManualSetMode == SymmetricalSetMode)
            ApplySymmetricalNominalValues(reverse: false);
        else if (ManualSetMode == LineLineSetMode)
            ApplyLineLineNominalValues(reverse: false, loadCurrent: NominalCurrent);
        else
            ApplyDirectNominalValues(reverse: false, loadCurrent: NominalCurrent);

        AppendEvent("100% load preset applied.");
    }

    private void ApplyFiftyPercentLoad()
    {
        if (ManualSetMode == SymmetricalSetMode)
            ApplySymmetricalNominalValues(reverse: false, currentMagnitude: NominalCurrent * 0.5);
        else if (ManualSetMode == LineLineSetMode)
            ApplyLineLineNominalValues(reverse: false, loadCurrent: NominalCurrent * 0.5);
        else
            ApplyDirectNominalValues(reverse: false, loadCurrent: NominalCurrent * 0.5);

        AppendEvent("50% load preset applied.");
    }

    private void ApplyUnload()
    {
        if (ManualSetMode == SymmetricalSetMode)
            ApplySymmetricalNominalValues(reverse: false, currentMagnitude: 0);
        else if (ManualSetMode == LineLineSetMode)
            ApplyLineLineNominalValues(reverse: false, loadCurrent: 0);
        else
            ApplyDirectNominalValues(reverse: false, loadCurrent: 0);

        AppendEvent("Unload preset applied: voltage nominal, current zero.");
    }

    private void BalanceAngles()
    {
        SetRowsInBatch(() =>
        {
            if (ManualSetMode == SymmetricalSetMode)
            {
                SetRowAngle("V1", 0);
                SetRowAngle("V2", 0);
                SetRowAngle("V0", 0);
                SetRowAngle("I1", 0);
                SetRowAngle("I2", 0);
                SetRowAngle("I0", 0);
            }
            else if (ManualSetMode == LineLineSetMode)
            {
                SetRowAngle("Vab", 30);
                SetRowAngle("Vbc", -90);
                SetRowAngle("Vca", 150);
                SetRowAngle("Ia", 0);
                SetRowAngle("Ib", -120);
                SetRowAngle("Ic", 120);
            }
            else
            {
                SetRowAngle("Va", 0);
                SetRowAngle("Vb", -120);
                SetRowAngle("Vc", 120);
                SetRowAngle("Ia", 0);
                SetRowAngle("Ib", -120);
                SetRowAngle("Ic", 120);
            }
        }, "Balance angles applied");

        AppendEvent("Balance angles applied.");
    }

    private void ReverseRotationFromContext()
    {
        if (_contextManualRow is not { } anchor)
        {
            LiveApplyText = "Right-click an angle cell first.";
            return;
        }

        if (!TryResolveBalanceGroup(anchor.Key, reverse: true, out var keys, out var offsets))
        {
            LiveApplyText = "Reverse Rotation is available only for three-phase voltage/current angle rows.";
            return;
        }

        var anchorIndex = Array.FindIndex(keys, key => string.Equals(key, anchor.Key, StringComparison.OrdinalIgnoreCase));
        if (anchorIndex < 0)
            return;

        var anchorAngle = anchor.AngleDegrees;
        var baseAngle = anchorAngle - offsets[anchorIndex];
        SetRowsInBatch(() =>
        {
            for (var i = 0; i < keys.Length; i++)
            {
                if (Row(keys[i]) is { } row)
                    row.AngleDegrees = baseAngle + offsets[i];
            }
        }, $"Reverse rotation from {anchor.Name}");
        AppendEvent($"Reverse rotation applied using {anchor.Name} as anchor. Anchor angle stayed {anchorAngle:0.000} deg.");
    }

    private void NominalFrequencyFromContext()
    {
        if (_contextManualRow is not { } row)
            return;

        SetRowsInBatch(() => row.FrequencyHz = NominalFrequencyHz, $"Nominal frequency applied to {row.Name}", preserveFrequencies: true);
        AppendEvent($"Nominal frequency {NominalFrequencyHz:0.000} Hz applied to {row.Name}.");
    }

    private void DcFrequencyFromContext()
    {
        if (_contextManualRow is not { } row)
            return;

        SetRowsInBatch(() => row.FrequencyHz = 0, $"DC frequency applied to {row.Name}", preserveFrequencies: true);
        AppendEvent($"DC frequency applied to {row.Name}.");
    }

    private void EqualFrequenciesFromContext()
    {
        if (_contextManualRow is not { } anchor)
            return;

        if (!TryResolveMagnitudeGroup(anchor.Key, anchor.Kind, out var keys))
        {
            LiveApplyText = "Equal Frequencies is available only for compatible voltage/current rows.";
            return;
        }

        var value = anchor.FrequencyHz;
        SetRowsInBatch(() =>
        {
            foreach (var key in keys)
            {
                if (Row(key) is { } row)
                    row.FrequencyHz = value;
            }
        }, $"Equal frequencies from {anchor.Name}", preserveFrequencies: true);
        AppendEvent($"Equal frequencies applied using {anchor.Name}={value:0.000} Hz as reference.");
    }

    private void ReverseRotation()
    {
        if (ManualSetMode == SymmetricalSetMode)
            ApplySymmetricalNominalValues(reverse: true);
        else if (ManualSetMode == LineLineSetMode)
            ApplyLineLineNominalValues(reverse: true, loadCurrent: NominalCurrent);
        else
            ApplyDirectNominalValues(reverse: true, loadCurrent: NominalCurrent);

        AppendEvent("Reverse rotation applied.");
    }

    private void ApplyDirectNominalValues(bool reverse, double loadCurrent)
    {
        SetRowsInBatch(() =>
        {
            SetRow("Va", NominalVoltageLn, 0, true);
            SetRow("Vb", NominalVoltageLn, reverse ? 120 : -120, true);
            SetRow("Vc", NominalVoltageLn, reverse ? -120 : 120, true);
            SetRow("Vn", 0, 0, false);
            SetRow("Ia", loadCurrent, 0, loadCurrent > 0);
            SetRow("Ib", loadCurrent, reverse ? 120 : -120, loadCurrent > 0);
            SetRow("Ic", loadCurrent, reverse ? -120 : 120, loadCurrent > 0);
            SetRow("In", 0, 0, false);
        }, "Direct nominal preset applied");
    }

    private void ApplyLineLineNominalValues(bool reverse, double loadCurrent)
    {
        SetRowsInBatch(() =>
        {
            SetRow("Vab", NominalVoltageLl, reverse ? -30 : 30, true);
            SetRow("Vbc", NominalVoltageLl, reverse ? 90 : -90, true);
            SetRow("Vca", NominalVoltageLl, reverse ? -150 : 150, true);
            SetRow("Ia", loadCurrent, 0, loadCurrent > 0);
            SetRow("Ib", loadCurrent, reverse ? 120 : -120, loadCurrent > 0);
            SetRow("Ic", loadCurrent, reverse ? -120 : 120, loadCurrent > 0);
            SetRow("In", 0, 0, false);
        }, "Line-line nominal preset applied");
    }

    private void ApplySymmetricalNominalValues(bool reverse, double currentMagnitude = NominalCurrent)
    {
        SetRowsInBatch(() =>
        {
            SetRow("V1", reverse ? 0 : NominalVoltageLn, 0, !reverse);
            SetRow("V2", reverse ? NominalVoltageLn : 0, 0, reverse);
            SetRow("V0", 0, 0, false);
            SetRow("I1", reverse ? 0 : currentMagnitude, 0, !reverse && currentMagnitude > 0);
            SetRow("I2", reverse ? currentMagnitude : 0, 0, reverse && currentMagnitude > 0);
            SetRow("I0", 0, 0, false);
        }, "Symmetrical component preset applied");
    }

    private void CopyManualTable()
    {
        var builder = new StringBuilder();
        builder.AppendLine("Signal\tMagnitude\tAngleDeg\tFrequencyHz\tOn");
        foreach (var row in ManualRows)
        {
            builder.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0}\t{1}\t{2}\t{3}\t{4}",
                row.Name,
                row.Magnitude,
                row.AngleDegrees,
                row.FrequencyHz,
                row.IsEnabled));
        }

        Clipboard.SetText(builder.ToString());
        AppendEvent("Manual output table copied to clipboard.");
    }

    private void PasteManualTable()
    {
        if (!Clipboard.ContainsText())
            return;

        var lines = Clipboard.GetText().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
            return;

        var dataLines = lines.Skip(lines[0].StartsWith("Signal", StringComparison.OrdinalIgnoreCase) ? 1 : 0).ToArray();
        SetRowsInBatch(() =>
        {
            for (var i = 0; i < dataLines.Length && i < ManualRows.Count; i++)
            {
                var cells = dataLines[i].Split('\t');
                if (cells.Length < 4)
                    continue;

                var row = ManualRows[i];
                if (double.TryParse(cells[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var magnitude))
                    row.Magnitude = magnitude;
                if (double.TryParse(cells[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var angle))
                    row.AngleDegrees = angle;
                if (double.TryParse(cells[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var frequency))
                    row.FrequencyHz = frequency;
                if (cells.Length >= 5 && bool.TryParse(cells[4], out var enabled))
                    row.IsEnabled = enabled;
            }
        }, "Manual output table pasted");
        AppendEvent("Manual output table pasted from clipboard.");
    }

    private void SetRowsInBatch(Action action, string reason, bool preserveFrequencies = false)
    {
        _isUpdatingManualRows = true;
        try
        {
            action();
            if (LinkFrequencies && !preserveFrequencies)
            {
                foreach (var row in ManualRows)
                    row.FrequencyHz = NominalFrequencyHz;
            }
        }
        finally
        {
            _isUpdatingManualRows = false;
        }

        ProjectManualRowsToChannels(reason);
    }

    private void SetAllManualFrequencies(double frequencyHz)
    {
        _isUpdatingManualRows = true;
        try
        {
            foreach (var row in ManualRows)
                row.FrequencyHz = frequencyHz;
        }
        finally
        {
            _isUpdatingManualRows = false;
        }

        ProjectManualRowsToChannels("Linked frequencies updated");
    }

    private void SetRow(string key, double magnitude, double angle, bool enabled)
    {
        if (Row(key) is not { } row)
            return;

        row.Magnitude = magnitude;
        row.AngleDegrees = angle;
        row.FrequencyHz = NominalFrequencyHz;
        row.IsEnabled = enabled;
    }

    private void SetRowAngle(string key, double angle)
    {
        if (Row(key) is { } row)
            row.AngleDegrees = angle;
    }

    private void SetChannel(
        string key,
        double magnitude,
        double angle,
        bool enabled,
        double frequencyHz,
        double? dcOffsetPercent = null,
        double? harmonicPercent = null,
        int? harmonicOrder = null,
        double? clipPercent = null)
    {
        var channel = Channel(key);
        if (channel is null)
            return;

        channel.Magnitude = Math.Max(0, magnitude);
        channel.AngleDegrees = NormalizeDegrees(angle);
        channel.FrequencyHz = frequencyHz >= 0 ? frequencyHz : NominalFrequencyHz;
        channel.IsEnabled = enabled;
        if (dcOffsetPercent.HasValue)
            channel.DcOffsetPercent = dcOffsetPercent.Value;
        if (harmonicPercent.HasValue)
            channel.HarmonicPercent = harmonicPercent.Value;
        if (harmonicOrder.HasValue)
            channel.HarmonicOrder = harmonicOrder.Value;
        if (clipPercent.HasValue)
            channel.ClipPercent = clipPercent.Value;
    }

    private void SetChannelFromPhasor(string key, Complex phasor, bool enabled, double frequencyHz)
        => SetChannel(key, phasor.Magnitude, NormalizeDegrees(phasor.Phase * 180.0 / Math.PI), enabled, frequencyHz);

    private SignalChannelViewModel? Channel(string key)
        => Channels.FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.OrdinalIgnoreCase));

    private ManualOutputRowViewModel? Row(string key)
        => ManualRows.FirstOrDefault(r => string.Equals(r.Key, key, StringComparison.OrdinalIgnoreCase));

    private Complex RowPhasor(string key)
    {
        var row = Row(key);
        if (row is null || !row.IsEnabled)
            return Complex.Zero;

        return Complex.FromPolarCoordinates(row.Magnitude, row.AngleDegrees * Math.PI / 180.0);
    }

    private Complex ChannelPhasor(string key)
    {
        var channel = Channel(key);
        if (channel is null || !channel.IsEnabled)
            return Complex.Zero;

        return Complex.FromPolarCoordinates(channel.Magnitude, channel.AngleDegrees * Math.PI / 180.0);
    }

    private double RowFrequency(params string[] keys)
    {
        foreach (var key in keys)
        {
            var row = Row(key);
            if (row is not null && row.FrequencyHz >= 0)
                return row.FrequencyHz;
        }

        return NominalFrequencyHz;
    }

    private bool IsAnyRowEnabled(params string[] keys)
        => keys.Any(key => Row(key) is { IsEnabled: true });

    private static (Complex Zero, Complex Positive, Complex Negative) ToSymmetricalComponents(Complex phaseA, Complex phaseB, Complex phaseC)
    {
        var a = Complex.FromPolarCoordinates(1, 120.0 * Math.PI / 180.0);
        var a2 = a * a;
        var zero = (phaseA + phaseB + phaseC) / 3.0;
        var positive = (phaseA + (a * phaseB) + (a2 * phaseC)) / 3.0;
        var negative = (phaseA + (a2 * phaseB) + (a * phaseC)) / 3.0;
        return (zero, positive, negative);
    }

    private static double NormalizeDegrees(double degrees)
    {
        while (degrees > 180)
            degrees -= 360;
        while (degrees <= -180)
            degrees += 360;
        return Math.Round(degrees, 6);
    }

    private void AddRampState()
    {
        var choice = SelectedRampSignalChoice ?? RampSignalChoices.FirstOrDefault(choice => string.Equals(choice.KeyCsv, "Ia", StringComparison.OrdinalIgnoreCase)) ?? RampSignalChoices.First();
        var channel = ResolveFirstRampBaseChannel(choice) ?? Channels.FirstOrDefault(c => c.Key == "Ia") ?? Channels.First();
        var from = channel.Magnitude;
        var to = Math.Max(0, from + (channel.Kind == "I" ? 1.0 : 10.0));
        var state = new RampStateViewModel(
            $"Ramp {RampStates.Count + 1}",
            choice.KeyCsv,
            choice.Name,
            choice.Quantity,
            from,
            to,
            (to - from) / 20.0,
            0.100,
            21,
            Math.Max(0.001, RampDurationSeconds));

        AttachRampState(state);
        RampStates.Add(state);
        SelectedRampState = state;
        OnPropertyChanged(nameof(RampTotalTimeSeconds));
        OnPropertyChanged(nameof(RampTotalTimeText));
        CommandManager.InvalidateRequerySuggested();
    }

    private void RemoveSelectedRampState()
    {
        if (RampStates.Count == 0)
            return;

        var index = SelectedRampState is null ? RampStates.Count - 1 : RampStates.IndexOf(SelectedRampState);
        if (index < 0)
            index = RampStates.Count - 1;

        var removed = RampStates[index];
        DetachRampState(removed);
        RampStates.RemoveAt(index);
        SelectedRampState = RampStates.Count == 0 ? null : RampStates[Math.Clamp(index, 0, RampStates.Count - 1)];
        OnPropertyChanged(nameof(RampTotalTimeSeconds));
        OnPropertyChanged(nameof(RampTotalTimeText));
        CommandManager.InvalidateRequerySuggested();
    }

    private void ApplySelectedScenarioPreset()
    {
        if (SelectedScenarioPresetChoice is not { } preset || preset.States.Count == 0)
            return;

        foreach (var state in SequenceStates)
            DetachSequenceState(state);

        SequenceStates.Clear();
        foreach (var snapshot in preset.States)
        {
            var state = CreateSequenceState(snapshot);
            AttachSequenceState(state);
            SequenceStates.Add(state);
        }

        Mode = InjectionMode.Sequencer;
        AutoEnableResidualChannelsForScenario(preset);
        SelectedSequenceState = SequenceStates.FirstOrDefault();
        UpdateSequencePreview();
        StatusText = $"Scenario applied: {preset.ShortLabel}.";
        AppendEvent($"Applied publisher scenario preset: {preset.Label}. {preset.HelpText}");
        CommandManager.InvalidateRequerySuggested();
    }

    private void AutoEnableResidualChannelsForScenario(PublisherScenarioPresetChoice preset)
    {
        var needsResidual = preset.States.Any(state => state.CurrentScaleN > 0 || state.VoltageScaleN > 0);
        if (!needsResidual)
            return;

        foreach (var key in new[] { "In", "Vn" })
        {
            var channel = Channels.FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.OrdinalIgnoreCase));
            if (channel is not null)
                channel.IsEnabled = true;
        }

        SaveCurrentPublisherSlot();
    }

    private static SequenceStateViewModel CreateSequenceState(SequenceStateSnapshot snapshot)
        => new(
            string.IsNullOrWhiteSpace(snapshot.Name) ? "State" : snapshot.Name,
            snapshot.DurationSeconds,
            snapshot.CurrentScale,
            snapshot.VoltageScale,
            snapshot.AngleShiftDegrees,
            snapshot.FrequencyHz,
            snapshot.CurrentScaleA,
            snapshot.CurrentScaleB,
            snapshot.CurrentScaleC,
            snapshot.CurrentScaleN,
            snapshot.VoltageScaleA,
            snapshot.VoltageScaleB,
            snapshot.VoltageScaleC,
            snapshot.VoltageScaleN,
            snapshot.AngleOffsetA,
            snapshot.AngleOffsetB,
            snapshot.AngleOffsetC,
            snapshot.AngleOffsetN,
            snapshot.CurrentDcOffsetPercent,
            snapshot.VoltageDcOffsetPercent,
            snapshot.CurrentHarmonicPercent,
            snapshot.VoltageHarmonicPercent,
            snapshot.HarmonicOrder,
            snapshot.CurrentClipPercent,
            snapshot.VoltageClipPercent,
            snapshot.ScenarioTag);

    private void AddSequenceState()
    {
        var state = new SequenceStateViewModel($"State {SequenceStates.Count + 1}", 0.500, 1.0, 1.0, 0, NominalFrequencyHz);
        AttachSequenceState(state);
        SequenceStates.Add(state);
        SelectedSequenceState = state;
        CommandManager.InvalidateRequerySuggested();
    }

    private void RemoveLastSequenceState()
    {
        if (SequenceStates.Count == 0)
            return;

        var index = SelectedSequenceState is null ? SequenceStates.Count - 1 : SequenceStates.IndexOf(SelectedSequenceState);
        if (index < 0)
            index = SequenceStates.Count - 1;
        var removed = SequenceStates[index];
        DetachSequenceState(removed);
        SequenceStates.RemoveAt(index);
        SelectedSequenceState = SequenceStates.Count == 0 ? null : SequenceStates[Math.Clamp(index, 0, SequenceStates.Count - 1)];
        CommandManager.InvalidateRequerySuggested();
    }

    private SvPublisherConfigSnapshot CreateSnapshot()
        => new()
        {
            SclPath = SclPath,
            StreamControlBlock = StreamControlBlock,
            StreamId = StreamId,
            DataSetReference = DataSetReference,
            AppId = AppIdText,
            DestinationMac = DestinationMac,
            UseVlan = UseVlan,
            VlanId = VlanId,
            VlanPriority = VlanPriority,
            SourceMac = SourceMac,
            SampleRateHz = SampleRateHz,
            NominalFrequencyHz = NominalFrequencyHz,
            CurrentDlsb = CurrentDlsb,
            VoltageDlsb = VoltageDlsb,
            DurationSeconds = DurationSeconds,
            Continuous = Continuous,
            LoopSequence = LoopSequence,
            Mode = Mode,
            ManualSetMode = ManualSetMode,
            Publishers = PublisherSlots.Select(slot => new SvPublisherSlotConfigSnapshot
            {
                Index = slot.Index,
                IsEnabled = slot.IsEnabled,
                StreamControlBlock = slot.StreamControlBlock,
                StreamId = slot.StreamId,
                DataSetReference = slot.DataSetReference,
                AppId = slot.AppIdText,
                DestinationMac = slot.DestinationMac,
                UseVlan = slot.UseVlan,
                VlanId = slot.VlanId,
                VlanPriority = slot.VlanPriority,
                SourceMac = slot.SourceMac,
                SampleRateHz = slot.SampleRateHz,
                NominalFrequencyHz = slot.NominalFrequencyHz,
                SampleRatePresetKey = slot.SampleRatePresetKey,
                CurrentDlsb = slot.CurrentDlsb,
                VoltageDlsb = slot.VoltageDlsb,
                ManualSetMode = slot.ManualSetMode,
                SignalSource = slot.SignalSource,
                ComtradePath = slot.ComtradePath,
                ComtradeLoop = slot.ComtradeLoop,
                SampleQualityKey = slot.SampleQualityKey,
                Channels = slot.Channels
            }).ToArray(),
            AutoApplyWhileRunning = AutoApplyWhileRunning,
            LinkFrequencies = LinkFrequencies,
            SyncPolicyMode = SyncPolicyMode,
            ExpectedPtpDomain = ExpectedPtpDomain,
            PtpAllowLocalFallback = PtpAllowLocalFallback,
            PtpPublisherMode = PtpPublisherMode,
            SampleQualityKey = SelectedSampleQualityChoice.Key,
            PtpClockIdentity = PtpClockIdentityText,
            PtpAnnounceIntervalMs = PtpAnnounceIntervalMs,
            PtpSyncIntervalMs = PtpSyncIntervalMs,
            PtpRespondToPeerDelay = PtpRespondToPeerDelay,
            RampSignalKey = SelectedRampSignalChoice?.KeyCsv ?? SelectedRampState?.SignalKey ?? string.Empty,
            ScenarioPresetKey = SelectedScenarioPresetChoice?.Key ?? string.Empty,
            RampTargetMagnitude = RampTargetMagnitude,
            RampDurationSeconds = RampDurationSeconds,
            Channels = Channels.Select(c => c.ToSnapshot()).ToArray(),
            SequenceStates = SequenceStates.Select(s => s.ToSnapshot()).ToArray()
        };

    private void AppendEvent(string message)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            return;

        if (!dispatcher.CheckAccess())
        {
            Dispatch(() => AppendEvent(message));
            return;
        }

        _eventLines.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
        while (_eventLines.Count > 80)
            _eventLines.RemoveAt(_eventLines.Count - 1);

        EvidenceText = string.Join(Environment.NewLine, _eventLines);
    }

    private static void Dispatch(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            return;

        if (dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.InvokeAsync(action);
    }

    private static IReadOnlyDictionary<string, EffectiveChannel> ApplyOscillatorPhases(
        IReadOnlyDictionary<string, EffectiveChannel> channels,
        Dictionary<string, OscillatorState> states,
        double sampleRateHz)
    {
        var result = new Dictionary<string, EffectiveChannel>(channels.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in channels)
        {
            if (!states.TryGetValue(pair.Key, out var state))
            {
                state = new OscillatorState
                {
                    PhaseRadians = pair.Value.AngleDegrees * Math.PI / 180.0,
                    LastAngleDegrees = pair.Value.AngleDegrees
                };
                states[pair.Key] = state;
            }

            var angleDelta = NormalizeDegrees(pair.Value.AngleDegrees - state.LastAngleDegrees) * Math.PI / 180.0;
            state.PhaseRadians = WrapRadians(state.PhaseRadians + angleDelta);
            state.LastAngleDegrees = pair.Value.AngleDegrees;
            result[pair.Key] = pair.Value with { PhaseRadians = state.PhaseRadians };
            state.PhaseRadians = WrapRadians(state.PhaseRadians + (2.0 * Math.PI * pair.Value.FrequencyHz / sampleRateHz));
        }

        return result;
    }

    private static double WrapRadians(double radians)
    {
        const double twoPi = 2.0 * Math.PI;
        radians %= twoPi;
        return radians < -Math.PI ? radians + twoPi : radians > Math.PI ? radians - twoPi : radians;
    }

    private sealed class PublisherSessionPlan
    {
        private readonly IReadOnlyList<RampSessionSegment> _rampSegments;
        private readonly IReadOnlyList<SequencerSessionSegment> _sequencerSegments;

        private PublisherSessionPlan(
            InjectionMode mode,
            string shortName,
            string displayName,
            string liveApplyText,
            double? durationSeconds,
            bool loop,
            IReadOnlyList<RampSessionSegment>? rampSegments = null,
            IReadOnlyList<SequencerSessionSegment>? sequencerSegments = null)
        {
            Mode = mode;
            ShortName = shortName;
            DisplayName = displayName;
            LiveApplyText = liveApplyText;
            DurationSeconds = durationSeconds;
            Loop = loop;
            _rampSegments = rampSegments ?? Array.Empty<RampSessionSegment>();
            _sequencerSegments = sequencerSegments ?? Array.Empty<SequencerSessionSegment>();
        }

        public InjectionMode Mode { get; }
        public string ShortName { get; }
        public string DisplayName { get; }
        public string LiveApplyText { get; }
        public double? DurationSeconds { get; }
        public bool Loop { get; }

        public static PublisherSessionPlan ManualContinue()
            => new(
                InjectionMode.Manual,
                "manual-continuous",
                "Manual Continue session",
                "RUN: manual values are applied continuously to the next SV frames.",
                durationSeconds: null,
                loop: true);

        public static PublisherSessionPlan RampOnce(IReadOnlyList<RampSessionSegment> segments)
        {
            var duration = segments.Sum(segment => segment.DurationSeconds);
            return new PublisherSessionPlan(
                InjectionMode.Ramp,
                "ramp",
                $"Ramp session {duration:0.000}s",
                "RUN: ramp timing is locked from the configured ramp states.",
                duration,
                loop: false,
                rampSegments: segments);
        }

        public static PublisherSessionPlan SequencerOnce(IReadOnlyList<SequencerSessionSegment> segments)
        {
            var duration = segments.Sum(segment => segment.DurationSeconds);
            return new PublisherSessionPlan(
                InjectionMode.Sequencer,
                "sequencer",
                $"Sequencer session {duration:0.000}s",
                "RUN: sequencer timing is locked from the configured state durations.",
                duration,
                loop: false,
                sequencerSegments: segments);
        }

        public static PublisherSessionPlan SequencerLoop(IReadOnlyList<SequencerSessionSegment> segments)
        {
            var duration = segments.Sum(segment => segment.DurationSeconds);
            return new PublisherSessionPlan(
                InjectionMode.Sequencer,
                "sequencer-loop",
                $"Sequencer loop session cycle={duration:0.000}s",
                "RUN: sequencer cycles continuously using the configured state durations.",
                durationSeconds: null,
                loop: true,
                sequencerSegments: segments);
        }

        public long? ResolveFrameLimit(double sampleRateHz)
        {
            if (DurationSeconds is not { } duration)
                return null;

            return Math.Max(1, (long)Math.Ceiling(Math.Max(0.001, duration) * Math.Max(1, sampleRateHz)));
        }

        public IReadOnlyDictionary<string, EffectiveChannel> ResolveChannels(
            IReadOnlyDictionary<string, EffectiveChannel> baseChannels,
            double elapsedSeconds)
        {
            return Mode switch
            {
                InjectionMode.Ramp => ResolveRampChannels(baseChannels, elapsedSeconds),
                InjectionMode.Sequencer => ResolveSequencerChannels(baseChannels, elapsedSeconds),
                _ => baseChannels
            };
        }

        private IReadOnlyDictionary<string, EffectiveChannel> ResolveRampChannels(
            IReadOnlyDictionary<string, EffectiveChannel> baseChannels,
            double elapsedSeconds)
        {
            if (_rampSegments.Count == 0 || ResolveRampSegment(elapsedSeconds) is not { } active)
                return baseChannels;

            var (segment, localElapsed) = active;
            var position = Math.Clamp(localElapsed / Math.Max(0.001, segment.DurationSeconds), 0.0, 1.0);
            var magnitude = segment.From + ((segment.To - segment.From) * position);
            var result = new Dictionary<string, EffectiveChannel>(baseChannels.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var pair in baseChannels)
            {
                result[pair.Key] = segment.AppliesTo(pair.Key)
                    ? pair.Value with { MagnitudeRms = magnitude }
                    : pair.Value;
            }

            return result;
        }

        private (RampSessionSegment Segment, double LocalElapsedSeconds)? ResolveRampSegment(double elapsedSeconds)
        {
            var total = _rampSegments.Sum(segment => segment.DurationSeconds);
            if (total <= 0)
                return null;

            var cursor = Math.Min(Math.Max(0, elapsedSeconds), Math.Max(0, total - 0.000001));
            foreach (var segment in _rampSegments)
            {
                if (cursor <= segment.DurationSeconds)
                    return (segment, cursor);

                cursor -= segment.DurationSeconds;
            }

            var last = _rampSegments[^1];
            return (last, last.DurationSeconds);
        }

        private IReadOnlyDictionary<string, EffectiveChannel> ResolveSequencerChannels(
            IReadOnlyDictionary<string, EffectiveChannel> baseChannels,
            double elapsedSeconds)
        {
            if (_sequencerSegments.Count == 0 || ResolveSequencerSegment(elapsedSeconds) is not { } state)
                return baseChannels;

            var result = new Dictionary<string, EffectiveChannel>(baseChannels.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var pair in baseChannels)
            {
                var channel = pair.Value;
                var isCurrent = string.Equals(channel.Kind, "I", StringComparison.OrdinalIgnoreCase);
                var multiplier = isCurrent
                    ? state.CurrentMultiplierFor(pair.Key)
                    : state.VoltageMultiplierFor(pair.Key);
                var magnitude = (isCurrent ? state.CurrentMagnitude : state.VoltageMagnitude) * multiplier;
                var angle = state.AngleShiftDegrees + PhaseOffsetForChannel(pair.Key) + state.AngleOffsetFor(pair.Key);
                var frequency = state.FrequencyHz > 0 ? state.FrequencyHz : channel.FrequencyHz;
                result[pair.Key] = channel with
                {
                    MagnitudeRms = magnitude,
                    AngleDegrees = angle,
                    FrequencyHz = frequency,
                    PhaseRadians = angle * Math.PI / 180.0,
                    DcOffsetPercent = isCurrent ? state.CurrentDcOffsetPercent : state.VoltageDcOffsetPercent,
                    HarmonicPercent = isCurrent ? state.CurrentHarmonicPercent : state.VoltageHarmonicPercent,
                    HarmonicOrder = state.HarmonicOrder,
                    ClipPercent = isCurrent ? state.CurrentClipPercent : state.VoltageClipPercent
                };
            }

            return result;
        }

        private SequencerSessionSegment? ResolveSequencerSegment(double elapsedSeconds)
        {
            var total = _sequencerSegments.Sum(segment => segment.DurationSeconds);
            if (total <= 0)
                return null;

            var cursor = Loop
                ? elapsedSeconds % total
                : Math.Min(Math.Max(0, elapsedSeconds), Math.Max(0, total - 0.000001));

            foreach (var state in _sequencerSegments)
            {
                if (cursor <= state.DurationSeconds)
                    return state;

                cursor -= state.DurationSeconds;
            }

            return _sequencerSegments[^1];
        }
    }

    private sealed record RampSessionSegment(
        string Name,
        IReadOnlyList<string> SignalKeys,
        double From,
        double To,
        double DurationSeconds)
    {
        public bool AppliesTo(string channelKey)
            => SignalKeys.Any(key => string.Equals(key, channelKey, StringComparison.OrdinalIgnoreCase));
    }

    private sealed record SequencerSessionSegment(
        string Name,
        double DurationSeconds,
        double CurrentMagnitude,
        double VoltageMagnitude,
        double AngleShiftDegrees,
        double FrequencyHz,
        double CurrentScaleA,
        double CurrentScaleB,
        double CurrentScaleC,
        double CurrentScaleN,
        double VoltageScaleA,
        double VoltageScaleB,
        double VoltageScaleC,
        double VoltageScaleN,
        double AngleOffsetA,
        double AngleOffsetB,
        double AngleOffsetC,
        double AngleOffsetN,
        double CurrentDcOffsetPercent,
        double VoltageDcOffsetPercent,
        double CurrentHarmonicPercent,
        double VoltageHarmonicPercent,
        int HarmonicOrder,
        double CurrentClipPercent,
        double VoltageClipPercent,
        string ScenarioTag)
    {
        public double CurrentMultiplierFor(string channelKey) => PhaseSuffix(channelKey) switch
        {
            "A" => CurrentScaleA,
            "B" => CurrentScaleB,
            "C" => CurrentScaleC,
            "N" => CurrentScaleN,
            _ => 1
        };

        public double VoltageMultiplierFor(string channelKey) => PhaseSuffix(channelKey) switch
        {
            "A" => VoltageScaleA,
            "B" => VoltageScaleB,
            "C" => VoltageScaleC,
            "N" => VoltageScaleN,
            _ => 1
        };

        public double AngleOffsetFor(string channelKey) => PhaseSuffix(channelKey) switch
        {
            "A" => AngleOffsetA,
            "B" => AngleOffsetB,
            "C" => AngleOffsetC,
            "N" => AngleOffsetN,
            _ => 0
        };

        private static string PhaseSuffix(string channelKey)
        {
            if (channelKey.EndsWith("a", StringComparison.OrdinalIgnoreCase))
                return "A";
            if (channelKey.EndsWith("b", StringComparison.OrdinalIgnoreCase))
                return "B";
            if (channelKey.EndsWith("c", StringComparison.OrdinalIgnoreCase))
                return "C";
            if (channelKey.EndsWith("n", StringComparison.OrdinalIgnoreCase))
                return "N";
            return string.Empty;
        }
    }

    private sealed class OscillatorState
    {
        public double PhaseRadians { get; set; }
        public double LastAngleDegrees { get; set; }
    }

    private readonly record struct EffectiveChannel(
        string Kind,
        bool IsEnabled,
        double MagnitudeRms,
        double AngleDegrees,
        double FrequencyHz,
        double PhaseRadians,
        double DcOffsetPercent = 0,
        double HarmonicPercent = 0,
        int HarmonicOrder = 2,
        double ClipPercent = 100);
}
