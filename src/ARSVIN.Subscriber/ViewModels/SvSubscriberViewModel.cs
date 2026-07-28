using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using AR.Iec61850.Capture;
using AR.Iec61850.SampledValues;
using AR.Iec61850.SampledValues.Field;
using AR.Iec61850.SampledValues.Measurements;
using AR.Iec61850.SampledValues.Profiles;
using AR.Iec61850.SampledValues.Reporting;
using AR.Iec61850.Scl;
using AR.Iec61850.Transports;
using AR.Iec61850.Transports.Npcap;
using ARSVIN.Subscriber.Models;
using ARSVIN.Subscriber.Reporting;
using Microsoft.Win32;

namespace ARSVIN.Subscriber.ViewModels;

public sealed class SvSubscriberViewModel : ObservableObject, IDisposable
{
    private readonly ConcurrentDictionary<string, SvStreamRuntime> _runtimeStreams = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SvStreamObservationSnapshot> _latestObservations = new(StringComparer.Ordinal);
    private readonly SvStreamObservationManager _observationManager = new();
    private readonly Dictionary<string, SvStreamViewModel> _streamRows = new(StringComparer.OrdinalIgnoreCase);
    private readonly DispatcherTimer _uiTimer;
    private CancellationTokenSource? _captureCts;
    private Task? _captureTask;
    private IReadOnlyList<SampledValuesPublisherProfile> _sclProfiles = Array.Empty<SampledValuesPublisherProfile>();
    private string _selectedSclPath = string.Empty;
    private string _captureSourcePath = string.Empty;
    private string _statusText = "Ready. Select adapter, optionally import SCL, then start listening.";
    private string _healthText = "IDLE";
    private string _captureButtonText = "Start";
    private string _filterText = string.Empty;
    private AdapterChoice? _selectedAdapter;
    private SvStreamViewModel? _selectedStream;
    private bool _isCapturing;
    private long _rawFrames;
    private long _svFrames;
    private long _parseErrors;
    private long _droppedByFilter;
    private DateTimeOffset? _captureStarted;
    private string _totalFramesText = "0";
    private string _svFramesText = "0";
    private string _streamsText = "0";
    private string _issuesText = "0";
    private string _sclText = "No SCL loaded";
    private string _captureDurationText = "00:00:00";
    private string _globalFpsText = "0.0 fps";

    public SvSubscriberViewModel()
    {
        RefreshAdaptersCommand = new RelayCommand(RefreshAdapters, () => !IsCapturing);
        OpenSclCommand = new AsyncRelayCommand(OpenSclAsync, () => !IsCapturing);
        OpenCaptureFileCommand = new AsyncRelayCommand(OpenCaptureFileAsync, () => !IsCapturing);
        ToggleCaptureCommand = new RelayCommand(ToggleCapture);
        ClearCommand = new RelayCommand(Clear, () => !IsCapturing);
        ExportReportCommand = new AsyncRelayCommand(ExportReportAsync, () => Streams.Count > 0);

        _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _uiTimer.Tick += (_, _) => RefreshUiSnapshots();
        _uiTimer.Start();
        RefreshAdapters();
    }

    public ObservableCollection<AdapterChoice> Adapters { get; } = new();
    public ObservableCollection<SvStreamViewModel> Streams { get; } = new();
    public BulkObservableCollection<DecodedValueRow> SelectedValues { get; } = new();
    public RelayCommand RefreshAdaptersCommand { get; }
    public AsyncRelayCommand OpenSclCommand { get; }
    public AsyncRelayCommand OpenCaptureFileCommand { get; }
    public RelayCommand ToggleCaptureCommand { get; }
    public RelayCommand ClearCommand { get; }
    public AsyncRelayCommand ExportReportCommand { get; }

    public AdapterChoice? SelectedAdapter { get => _selectedAdapter; set => SetProperty(ref _selectedAdapter, value); }
    public SvStreamViewModel? SelectedStream
    {
        get => _selectedStream;
        set { if (SetProperty(ref _selectedStream, value)) RefreshSelectedValues(); }
    }
    public string FilterText { get => _filterText; set => SetProperty(ref _filterText, value); }
    public bool IsCapturing
    {
        get => _isCapturing;
        private set
        {
            if (!SetProperty(ref _isCapturing, value)) return;
            CaptureButtonText = value ? "Stop" : "Start";
            RefreshAdaptersCommand.RaiseCanExecuteChanged();
            OpenSclCommand.RaiseCanExecuteChanged();
            OpenCaptureFileCommand.RaiseCanExecuteChanged();
            ClearCommand.RaiseCanExecuteChanged();
            ToggleCaptureCommand.RaiseCanExecuteChanged();
        }
    }

    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }
    public string HealthText { get => _healthText; private set => SetProperty(ref _healthText, value); }
    public string CaptureButtonText { get => _captureButtonText; private set => SetProperty(ref _captureButtonText, value); }
    public string TotalFramesText { get => _totalFramesText; private set => SetProperty(ref _totalFramesText, value); }
    public string SvFramesText { get => _svFramesText; private set => SetProperty(ref _svFramesText, value); }
    public string StreamsText { get => _streamsText; private set => SetProperty(ref _streamsText, value); }
    public string IssuesText { get => _issuesText; private set => SetProperty(ref _issuesText, value); }
    public string SclText { get => _sclText; private set => SetProperty(ref _sclText, value); }
    public string CaptureDurationText { get => _captureDurationText; private set => SetProperty(ref _captureDurationText, value); }
    public string GlobalFpsText { get => _globalFpsText; private set => SetProperty(ref _globalFpsText, value); }

    public void Dispose()
    {
        _uiTimer.Stop();
        _captureCts?.Cancel();
        _captureCts?.Dispose();
    }

    private void RefreshAdapters()
    {
        try
        {
            Adapters.Clear();
            foreach (var adapter in NpcapAdapterCatalog.ListAdapters())
            {
                Adapters.Add(new AdapterChoice
                {
                    Index = adapter.Index,
                    Name = adapter.Name,
                    Description = string.IsNullOrWhiteSpace(adapter.Description) ? adapter.Name : adapter.Description,
                    MacAddress = adapter.MacAddress?.ToString() ?? string.Empty
                });
            }
            SelectedAdapter ??= Adapters.FirstOrDefault();
            StatusText = Adapters.Count == 0
                ? "No Npcap adapter found. Install Npcap and run as Administrator if needed."
                : $"Detected {Adapters.Count} adapter(s).";
        }
        catch (Exception ex) { StatusText = $"Adapter refresh failed: {ex.Message}"; }
    }

    private async Task OpenSclAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open SCL/SCD file for SV verification",
            Filter = "IEC 61850 SCL (*.scd;*.cid;*.icd;*.iid)|*.scd;*.cid;*.icd;*.iid|XML files (*.xml)|*.xml|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var document = await Task.Run(() => new SclParser().Load(dialog.FileName)).ConfigureAwait(true);
            _sclProfiles = SampledValuesPublisherProfile.CreateMany(document);
            _selectedSclPath = dialog.FileName;
            SclText = $"{Path.GetFileName(dialog.FileName)} • {_sclProfiles.Count} SV stream(s)";
            StatusText = $"SCL loaded: {_sclProfiles.Count} SampledValueControl stream(s) available for evidence-scored binding.";
        }
        catch (Exception ex)
        {
            _sclProfiles = Array.Empty<SampledValuesPublisherProfile>();
            _selectedSclPath = string.Empty;
            SclText = "No SCL loaded";
            StatusText = $"SCL load failed: {ex.Message}";
        }
    }

    private async Task OpenCaptureFileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open PCAP or PCAPNG with IEC 61850 Sampled Values",
            Filter = "Process-bus capture (*.pcap;*.pcapng)|*.pcap;*.pcapng|Classic PCAP (*.pcap)|*.pcap|PCAPNG (*.pcapng)|*.pcapng|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            ClearRuntimeOnly();
            _captureStarted = DateTimeOffset.Now;
            _captureSourcePath = dialog.FileName;
            var count = await Task.Run(() => ReplayPcapFile(dialog.FileName)).ConfigureAwait(true);
            StatusText = $"Processed {count:N0} frame(s) from {Path.GetFileName(dialog.FileName)}.";
            RefreshUiSnapshots();
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException or OverflowException)
        {
            StatusText = $"Capture import failed: {ex.Message}";
        }
    }

    private int ReplayPcapFile(string path)
    {
        var total = 0;
        foreach (var packet in ProcessBusCaptureFileReader.Read(path))
        {
            total++;
            ObserveFrame(packet.Timestamp, packet.Frame, SvObservationInputKind.PcapReplay);
        }
        return total;
    }

    private void ObserveFrame(DateTimeOffset timestamp, ReadOnlyMemory<byte> ethernetFrame, SvObservationInputKind inputKind)
    {
        Interlocked.Increment(ref _rawFrames);
        if (!SampledValuesFrameParser.TryParseEthernetFrame(ethernetFrame, out var frame))
        {
            Interlocked.Increment(ref _parseErrors);
            return;
        }
        if (!PassesUserFilter(frame))
        {
            Interlocked.Increment(ref _droppedByFilter);
            return;
        }

        Interlocked.Increment(ref _svFrames);
        var first = frame.Pdu.Asdus.FirstOrDefault();
        var profile = FindProfile(frame, first);
        if (!_observationManager.TryObserve(timestamp, frame, inputKind, profile, out var observation))
        {
            Interlocked.Increment(ref _parseErrors);
            return;
        }

        var key = observation.Key.Id;
        _latestObservations[key] = observation;
        var runtime = _runtimeStreams.GetOrAdd(key, _ => new SvStreamRuntime(key));
        runtime.Observe(timestamp, frame, observation.IsBoundToScl ? profile : null, observation);
    }

    private void ToggleCapture() { if (IsCapturing) StopCapture(); else StartCapture(); }

    private void StartCapture()
    {
        if (SelectedAdapter is null) { StatusText = "Select an Npcap adapter before starting."; return; }
        ClearRuntimeOnly();
        _captureSourcePath = $"live://{SelectedAdapter.DisplayName}";
        IsCapturing = true;
        _captureStarted = DateTimeOffset.Now;
        HealthText = "LISTENING";
        StatusText = "Listening for IEC 61850 Sampled Values frames...";
        _captureCts = new CancellationTokenSource();
        _captureTask = Task.Run(() => CaptureLoopAsync(SelectedAdapter.Selector, _captureCts.Token));
    }

    private void StopCapture()
    {
        _captureCts?.Cancel();
        IsCapturing = false;
        HealthText = Streams.Count == 0 ? "IDLE" : HealthText;
        StatusText = "Capture stopped.";
    }

    private async Task CaptureLoopAsync(string adapterSelector, CancellationToken cancellationToken)
    {
        try
        {
            using var source = new NpcapProcessBusFrameSource(adapterSelector);
            var options = new ProcessBusCaptureOptions
            {
                Filter = "(ether proto 0x88ba) or (vlan and ether proto 0x88ba)",
                BufferCapacity = 8192,
                ReadTimeoutMilliseconds = 250
            };
            await foreach (var captured in source.CaptureAsync(options, cancellationToken).ConfigureAwait(false))
                ObserveFrame(captured.Timestamp, captured.Frame, SvObservationInputKind.LiveCapture);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                IsCapturing = false;
                HealthText = "ERROR";
                StatusText = $"Capture failed: {ex.Message}";
            });
        }
    }

    private bool PassesUserFilter(SampledValuesFrame frame)
    {
        var filter = FilterText?.Trim();
        if (string.IsNullOrWhiteSpace(filter)) return true;
        var first = frame.Pdu.Asdus.FirstOrDefault();
        if (ushort.TryParse(filter.Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase), System.Globalization.NumberStyles.HexNumber, null, out var appId) && frame.AppId == appId)
            return true;
        return (first?.SvId?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
               frame.Source.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase) ||
               frame.Destination.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private SampledValuesPublisherProfile? FindProfile(SampledValuesFrame frame, SampledValueAsdu? asdu)
    {
        if (asdu is null || _sclProfiles.Count == 0) return null;

        var observation = new SvSclBindingObservation
        {
            AppId = frame.AppId,
            DestinationMac = frame.Destination.ToString(),
            VlanId = frame.Vlan?.VlanId,
            SvId = asdu.SvId,
            DataSetReference = asdu.DataSetReference,
            ConfigurationRevision = asdu.ConfigurationRevision,
            AsduPerFrame = frame.Pdu.Asdus.Count,
            PayloadBytesPerAsdu = asdu.SamplePayload.Length
        };
        var ranked = _sclProfiles.Select(profile => new
            {
                Profile = profile,
                Result = SvSclBindingScorer.Score(new SvSclBindingCandidate
                {
                    CandidateId = profile.Stream.ControlBlockReference,
                    ExpectedAppId = profile.AppId,
                    ExpectedDestinationMac = profile.Destination.ToString(),
                    ExpectedVlanId = profile.Vlan?.VlanId,
                    ExpectedSvId = profile.Stream.SvId,
                    ExpectedDataSetReference = profile.Stream.DataSetReference,
                    ExpectedConfigurationRevision = profile.Stream.ConfigurationRevision,
                    ExpectedAsduPerFrame = profile.AsduPerFrame,
                    ExpectedPayloadBytesPerAsdu = profile.PayloadLayout.PayloadByteLength
                }, observation)
            })
            .Where(item => item.Result.Confidence is SvSclBindingConfidence.Likely or SvSclBindingConfidence.Confirmed)
            .OrderByDescending(item => item.Result.Confidence)
            .ThenByDescending(item => item.Result.Score)
            .ToArray();
        if (ranked.Length == 0) return null;
        if (ranked.Length > 1 && ranked[0].Result.Confidence == ranked[1].Result.Confidence && ranked[0].Result.Score == ranked[1].Result.Score)
            return null;
        return ranked[0].Profile;
    }

    private void RefreshUiSnapshots()
    {
        var snapshots = _runtimeStreams.Values.Select(runtime => runtime.Snapshot()).OrderBy(snapshot => snapshot.AppId).ThenBy(snapshot => snapshot.SvId).ToArray();
        foreach (var snapshot in snapshots)
        {
            if (!_streamRows.TryGetValue(snapshot.Key, out var row))
            {
                row = new SvStreamViewModel { Key = snapshot.Key };
                _streamRows[snapshot.Key] = row;
                Streams.Add(row);
                SelectedStream ??= row;
            }
            _latestObservations.TryGetValue(snapshot.Key, out var observation);
            row.Apply(snapshot, observation);
        }

        var keys = snapshots.Select(snapshot => snapshot.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var stale in _streamRows.Keys.Where(key => !keys.Contains(key)).ToArray())
        {
            var row = _streamRows[stale];
            _streamRows.Remove(stale);
            Streams.Remove(row);
        }
        RefreshSelectedValues();
        UpdateGlobalCards(snapshots);
        ExportReportCommand.RaiseCanExecuteChanged();
    }

    private void RefreshSelectedValues() => SelectedValues.ReplaceAll(SelectedStream?.Values ?? Array.Empty<DecodedValueRow>());

    private void UpdateGlobalCards(IReadOnlyList<SvStreamSnapshot> snapshots)
    {
        var raw = Interlocked.Read(ref _rawFrames);
        var sv = Interlocked.Read(ref _svFrames);
        var parse = Interlocked.Read(ref _parseErrors);
        var dropped = Interlocked.Read(ref _droppedByFilter);
        var runtimeIssues = snapshots.Sum(snapshot => snapshot.SequenceGapCount + snapshot.DuplicateCount + snapshot.OutOfOrderCount + snapshot.PayloadIssueCount);
        var configurationIssues = snapshots.Sum(snapshot => _latestObservations.TryGetValue(snapshot.Key, out var observation) ? observation.ConfigurationComparison?.Findings.Count ?? 0 : 0);
        var issues = runtimeIssues + configurationIssues + parse;
        var duration = _captureStarted.HasValue ? DateTimeOffset.Now - _captureStarted.Value : TimeSpan.Zero;
        var fps = duration.TotalSeconds > 0.001 ? sv / duration.TotalSeconds : 0;

        TotalFramesText = raw.ToString("N0");
        SvFramesText = sv.ToString("N0");
        StreamsText = snapshots.Count.ToString("N0");
        IssuesText = issues.ToString("N0");
        CaptureDurationText = duration.ToString(@"hh\:mm\:ss");
        GlobalFpsText = $"{fps:0.0} fps";

        if (!IsCapturing && snapshots.Count == 0) HealthText = "IDLE";
        else if (_streamRows.Values.Any(row => row.CaptureFieldState == "BAD" || row.ProtocolFieldState == "BAD" || row.StreamFieldState == "BAD") || (parse > 0 && sv == 0)) HealthText = "BAD";
        else if (_streamRows.Values.Any(row => row.CaptureFieldState == "WARN" || row.ProtocolFieldState == "WARN" || row.StreamFieldState == "WARN" || row.ConfigurationFieldState is "WARN" or "BAD" || row.MeasurementFieldState == "WARN") || parse > 0) HealthText = "WARN";
        else if (snapshots.Count == 0) HealthText = IsCapturing ? "LISTENING" : "IDLE";
        else HealthText = "GOOD";

        if (dropped > 0 && IsCapturing) StatusText = $"Listening. User filter dropped {dropped:N0} SV frame(s).";
    }

    private void Clear()
    {
        ClearRuntimeOnly();
        HealthText = "IDLE";
        StatusText = "Cleared subscriber statistics.";
    }

    private void ClearRuntimeOnly()
    {
        _runtimeStreams.Clear();
        _latestObservations.Clear();
        _observationManager.Clear();
        _streamRows.Clear();
        Streams.Clear();
        SelectedValues.ReplaceAll(Array.Empty<DecodedValueRow>());
        SelectedStream = null;
        Interlocked.Exchange(ref _rawFrames, 0);
        Interlocked.Exchange(ref _svFrames, 0);
        Interlocked.Exchange(ref _parseErrors, 0);
        Interlocked.Exchange(ref _droppedByFilter, 0);
        _captureStarted = null;
        _captureSourcePath = string.Empty;
        UpdateGlobalCards(Array.Empty<SvStreamSnapshot>());
        ExportReportCommand.RaiseCanExecuteChanged();
    }

    private async Task ExportReportAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export ArSubsv field support bundle",
            Filter = "ArSubsv support bundle (*.zip)|*.zip",
            DefaultExt = ".zip",
            AddExtension = true,
            FileName = $"ArSubsv-Support-{DateTime.Now:yyyyMMdd-HHmmss}.zip"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var generatedAt = DateTimeOffset.Now;
            var snapshots = _runtimeStreams.Values.Select(runtime => runtime.Snapshot()).OrderBy(snapshot => snapshot.AppId).ThenBy(snapshot => snapshot.SvId).ToArray();
            var observations = _latestObservations.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            var report = SvSubscriberReportBuilder.Build(new SvSubscriberReportContext
            {
                GeneratedAt = generatedAt,
                CaptureStartedAt = _captureStarted,
                Health = HealthText,
                SclPath = _selectedSclPath,
                Adapter = SelectedAdapter?.DisplayName ?? string.Empty,
                Filter = string.IsNullOrWhiteSpace(FilterText) ? string.Empty : FilterText,
                RawFrames = Interlocked.Read(ref _rawFrames),
                SvFrames = Interlocked.Read(ref _svFrames),
                ParseErrors = Interlocked.Read(ref _parseErrors),
                DroppedByFilter = Interlocked.Read(ref _droppedByFilter),
                Streams = snapshots,
                Observations = observations
            });

            var markdown = SvSubscriberEvidenceReportSerializer.ToMarkdown(report);
            var json = SvSubscriberEvidenceReportSerializer.ToJson(report);
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            var contents = new List<SvSupportBundleContent>
            {
                SvSupportBundleWriter.Text("subscriber-evidence.md", markdown, "Human-readable receiver evidence"),
                SvSupportBundleWriter.Text("subscriber-evidence.json", json, "Machine-readable receiver evidence"),
                SvSupportBundleWriter.Text("field-summary.json", JsonSerializer.Serialize(BuildFieldSummary(), jsonOptions), "Five-axis field health and selected stream state"),
                SvSupportBundleWriter.Text("diagnostics.md", BuildDiagnosticsMarkdown(), "Selected stream diagnostics and provenance")
            };
            if (SelectedStream?.ActiveMeasurementContext is { } context)
            {
                var contextDocument = new SvMeasurementContextDocument { Streams = [context] };
                contents.Add(SvSupportBundleWriter.Text("measurement-context.json", SvMeasurementContextSerializer.ToJson(contextDocument), "Explicit CT/VT and display-domain context"));
            }

            var appAssembly = typeof(SvSubscriberViewModel).Assembly;
            var engineAssembly = typeof(SampledValuesFrame).Assembly;
            var manifest = new SvSupportBundleManifest
            {
                GeneratedAt = generatedAt,
                Application = "ArSubsv",
                ApplicationVersion = appAssembly.GetName().Version?.ToString() ?? string.Empty,
                ApplicationCommit = ResolveRevision(appAssembly),
                EngineCommit = ResolveRevision(engineAssembly),
                CaptureSource = string.IsNullOrWhiteSpace(_captureSourcePath) ? "live or unspecified" : Path.GetFileName(_captureSourcePath),
                SclSha256 = ComputeFileSha256(_selectedSclPath),
                PrivacyMode = SvSupportBundlePrivacyMode.MetadataOnly
            };

            await Task.Run(() => SvSupportBundleWriter.Write(dialog.FileName, manifest, contents)).ConfigureAwait(true);
            StatusText = $"Support bundle exported: {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or CryptographicException)
        {
            StatusText = $"Support bundle export failed: {ex.Message}";
        }
    }

    private object BuildFieldSummary()
        => new
        {
            health = HealthText,
            captureSource = string.IsNullOrWhiteSpace(_captureSourcePath) ? "unspecified" : Path.GetFileName(_captureSourcePath),
            scl = string.IsNullOrWhiteSpace(_selectedSclPath) ? "not loaded" : Path.GetFileName(_selectedSclPath),
            selectedStream = SelectedStream is null ? null : new
            {
                SelectedStream.Key,
                SelectedStream.AppId,
                SelectedStream.SvId,
                capture = SelectedStream.CaptureFieldState,
                protocol = SelectedStream.ProtocolFieldState,
                stream = SelectedStream.StreamFieldState,
                configuration = SelectedStream.ConfigurationFieldState,
                measurement = SelectedStream.MeasurementFieldState,
                signal = SelectedStream.SignalState,
                SelectedStream.FieldSummary,
                SelectedStream.MeasurementFieldDetail,
                SelectedStream.Scaling,
                SelectedStream.Timebase,
                SelectedStream.MeasurementContext
            }
        };

    private string BuildDiagnosticsMarkdown()
    {
        var builder = new StringBuilder("# ArSubsv Field Diagnostics\n\n");
        builder.AppendLine($"- Capture source: {(string.IsNullOrWhiteSpace(_captureSourcePath) ? "unspecified" : Path.GetFileName(_captureSourcePath))}");
        builder.AppendLine($"- SCL: {(string.IsNullOrWhiteSpace(_selectedSclPath) ? "not loaded" : Path.GetFileName(_selectedSclPath))}");
        builder.AppendLine($"- Global health: {HealthText}");
        if (SelectedStream is null) return builder.ToString();
        builder.AppendLine($"- Selected stream: {SelectedStream.SvId} / {SelectedStream.AppId}");
        builder.AppendLine($"- {SelectedStream.FieldSummary}");
        builder.AppendLine();
        builder.AppendLine("## Evidence");
        foreach (var line in SelectedStream.EvidenceDetails) builder.Append("- ").AppendLine(line);
        return builder.ToString();
    }

    private static string ResolveRevision(Assembly assembly)
    {
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;
        var plus = informational.LastIndexOf('+');
        var candidate = plus >= 0 ? informational[(plus + 1)..] : informational;
        return candidate.Length >= 40 && candidate.Take(40).All(Uri.IsHexDigit) ? candidate[..40].ToLowerInvariant() : informational;
    }

    private static string ComputeFileSha256(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return string.Empty;
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
