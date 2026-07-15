using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using AR.Iec61850.SampledValues;
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

    public AdapterChoice? SelectedAdapter
    {
        get => _selectedAdapter;
        set => SetProperty(ref _selectedAdapter, value);
    }

    public SvStreamViewModel? SelectedStream
    {
        get => _selectedStream;
        set
        {
            if (SetProperty(ref _selectedStream, value))
                RefreshSelectedValues();
        }
    }

    public string FilterText
    {
        get => _filterText;
        set => SetProperty(ref _filterText, value);
    }

    public bool IsCapturing
    {
        get => _isCapturing;
        private set
        {
            if (!SetProperty(ref _isCapturing, value))
                return;

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
        catch (Exception ex)
        {
            StatusText = $"Adapter refresh failed: {ex.Message}";
        }
    }

    private async Task OpenSclAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open SCL/SCD file for SV verification",
            Filter = "IEC 61850 SCL (*.scd;*.cid;*.icd;*.iid)|*.scd;*.cid;*.icd;*.iid|XML files (*.xml)|*.xml|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var document = await Task.Run(() => new SclParser().Load(dialog.FileName)).ConfigureAwait(true);
            _sclProfiles = SampledValuesPublisherProfile.CreateMany(document);
            _selectedSclPath = dialog.FileName;
            SclText = $"{Path.GetFileName(dialog.FileName)} • {_sclProfiles.Count} SV stream(s)";
            StatusText = $"SCL loaded: {_sclProfiles.Count} SampledValueControl stream(s) available for binding.";
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
            Title = "Open PCAP file with IEC 61850 Sampled Values",
            Filter = "PCAP capture (*.pcap)|*.pcap|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            ClearRuntimeOnly();
            _captureStarted = DateTimeOffset.Now;
            var count = await Task.Run(() => ReplayPcapFile(dialog.FileName)).ConfigureAwait(true);
            StatusText = $"Processed {count:N0} frame(s) from {Path.GetFileName(dialog.FileName)}.";
            RefreshUiSnapshots();
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException)
        {
            StatusText = $"PCAP import failed: {ex.Message}";
        }
    }

    private int ReplayPcapFile(string path)
    {
        var total = 0;
        foreach (var frame in PcapFrames.Read(path))
        {
            total++;
            ObserveFrame(frame.Timestamp, frame.Frame, SvObservationInputKind.PcapReplay);
        }

        return total;
    }

    private void ObserveFrame(
        DateTimeOffset timestamp,
        ReadOnlyMemory<byte> ethernetFrame,
        SvObservationInputKind inputKind)
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
        runtime.Observe(
            timestamp,
            frame,
            observation.IsBoundToScl ? profile : null,
            observation);
    }

    private void ToggleCapture()
    {
        if (IsCapturing)
        {
            StopCapture();
            return;
        }

        StartCapture();
    }

    private void StartCapture()
    {
        if (SelectedAdapter is null)
        {
            StatusText = "Select an Npcap adapter before starting.";
            return;
        }

        ClearRuntimeOnly();
        IsCapturing = true;
        _captureStarted = DateTimeOffset.Now;
        HealthText = "LISTENING";
        StatusText = "Listening for IEC 61850 Sampled Values frames...";
        _captureCts = new CancellationTokenSource();
        var selector = SelectedAdapter.Selector;
        _captureTask = Task.Run(() => CaptureLoopAsync(selector, _captureCts.Token));
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
            {
                ObserveFrame(captured.Timestamp, captured.Frame, SvObservationInputKind.LiveCapture);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on stop.
        }
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
        if (string.IsNullOrWhiteSpace(filter))
            return true;

        var first = frame.Pdu.Asdus.FirstOrDefault();
        if (ushort.TryParse(filter.Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase), System.Globalization.NumberStyles.HexNumber, null, out var appId) && frame.AppId == appId)
            return true;

        return (first?.SvId?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false) ||
               frame.Source.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase) ||
               frame.Destination.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private SampledValuesPublisherProfile? FindProfile(SampledValuesFrame frame, SampledValueAsdu? asdu)
    {
        if (asdu is null || _sclProfiles.Count == 0)
            return null;

        var addressCandidates = _sclProfiles.Where(profile =>
                profile.AppId == frame.AppId &&
                string.Equals(profile.Destination.ToString(), frame.Destination.ToString(), StringComparison.OrdinalIgnoreCase) &&
                profile.Vlan?.VlanId == frame.Vlan?.VlanId)
            .ToArray();
        if (addressCandidates.Length == 0)
            return null;

        var exact = addressCandidates.Where(profile =>
                string.Equals(profile.Stream.SvId, asdu.SvId, StringComparison.Ordinal) &&
                string.Equals(profile.Stream.DataSetReference, asdu.DataSetReference, StringComparison.Ordinal))
            .ToArray();
        if (exact.Length == 1)
            return exact[0];

        var svIdMatches = addressCandidates.Where(profile =>
                string.Equals(profile.Stream.SvId, asdu.SvId, StringComparison.Ordinal))
            .ToArray();
        if (svIdMatches.Length == 1)
            return svIdMatches[0];

        var dataSetMatches = addressCandidates.Where(profile =>
                string.Equals(profile.Stream.DataSetReference, asdu.DataSetReference, StringComparison.Ordinal))
            .ToArray();
        if (dataSetMatches.Length == 1)
            return dataSetMatches[0];

        return null;
    }

    private void RefreshUiSnapshots()
    {
        var snapshots = _runtimeStreams.Values.Select(x => x.Snapshot()).OrderBy(x => x.AppId).ThenBy(x => x.SvId).ToArray();
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

        var keys = snapshots.Select(x => x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var stale in _streamRows.Keys.Where(k => !keys.Contains(k)).ToArray())
        {
            var row = _streamRows[stale];
            _streamRows.Remove(stale);
            Streams.Remove(row);
        }

        RefreshSelectedValues();
        UpdateGlobalCards(snapshots);
        ExportReportCommand.RaiseCanExecuteChanged();
    }

    private void RefreshSelectedValues()
    {
        SelectedValues.ReplaceAll(SelectedStream?.Values ?? Array.Empty<DecodedValueRow>());
    }

    private void UpdateGlobalCards(IReadOnlyList<SvStreamSnapshot> snapshots)
    {
        var raw = Interlocked.Read(ref _rawFrames);
        var sv = Interlocked.Read(ref _svFrames);
        var parse = Interlocked.Read(ref _parseErrors);
        var dropped = Interlocked.Read(ref _droppedByFilter);
        var runtimeIssues = snapshots.Sum(x => x.SequenceGapCount + x.DuplicateCount + x.OutOfOrderCount + x.PayloadIssueCount);
        var configurationIssues = snapshots.Sum(snapshot =>
            _latestObservations.TryGetValue(snapshot.Key, out var observation)
                ? observation.ConfigurationComparison?.Findings.Count ?? 0
                : 0);
        var issues = runtimeIssues + configurationIssues + parse;
        var duration = _captureStarted.HasValue ? DateTimeOffset.Now - _captureStarted.Value : TimeSpan.Zero;
        var fps = duration.TotalSeconds > 0.001 ? sv / duration.TotalSeconds : 0;

        TotalFramesText = raw.ToString("N0");
        SvFramesText = sv.ToString("N0");
        StreamsText = snapshots.Count.ToString("N0");
        IssuesText = issues.ToString("N0");
        CaptureDurationText = duration.ToString(@"hh\:mm\:ss");
        GlobalFpsText = $"{fps:0.0} fps";

        if (!IsCapturing && snapshots.Count == 0)
            HealthText = "IDLE";
        else if (_streamRows.Values.Any(row => row.Health == "BAD") || parse > 0)
            HealthText = "BAD";
        else if (_streamRows.Values.Any(row => row.Health == "WARN"))
            HealthText = "WARN";
        else if (snapshots.Count == 0)
            HealthText = IsCapturing ? "LISTENING" : "IDLE";
        else
            HealthText = "GOOD";

        if (dropped > 0 && IsCapturing)
            StatusText = $"Listening. User filter dropped {dropped:N0} SV frame(s).";
    }

    private void Clear()
    {
        ClearRuntimeOnly();
        Streams.Clear();
        SelectedValues.ReplaceAll(Array.Empty<DecodedValueRow>());
        _streamRows.Clear();
        SelectedStream = null;
        HealthText = "IDLE";
        StatusText = "Cleared subscriber statistics.";
        ExportReportCommand.RaiseCanExecuteChanged();
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
        UpdateGlobalCards(Array.Empty<SvStreamSnapshot>());
        ExportReportCommand.RaiseCanExecuteChanged();
    }

    private async Task ExportReportAsync()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export SV subscriber evidence bundle",
            Filter = "ARSVIN evidence bundle (*.md)|*.md|Markdown report (*.md)|*.md|JSON evidence (*.json)|*.json",
            DefaultExt = ".md",
            AddExtension = true,
            FileName = $"arsvin-subscriber-evidence-{DateTime.Now:yyyyMMdd-HHmmss}.md"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var generatedAt = DateTimeOffset.Now;
            var snapshots = _runtimeStreams.Values
                .Select(runtime => runtime.Snapshot())
                .OrderBy(snapshot => snapshot.AppId)
                .ThenBy(snapshot => snapshot.SvId)
                .ToArray();
            var observations = _latestObservations.ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.Ordinal);
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

            var markdownPath = Path.ChangeExtension(dialog.FileName, ".md");
            var jsonPath = Path.ChangeExtension(dialog.FileName, ".json");
            var markdown = SvSubscriberEvidenceReportSerializer.ToMarkdown(report);
            var json = SvSubscriberEvidenceReportSerializer.ToJson(report);
            await Task.WhenAll(
                File.WriteAllTextAsync(markdownPath, markdown),
                File.WriteAllTextAsync(jsonPath, json)).ConfigureAwait(true);

            StatusText = $"Evidence bundle exported: {Path.GetFileName(markdownPath)} + {Path.GetFileName(jsonPath)}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            StatusText = $"Evidence export failed: {ex.Message}";
        }
    }
}
