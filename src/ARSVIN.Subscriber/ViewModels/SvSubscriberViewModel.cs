using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using AR.Iec61850.SampledValues;
using AR.Iec61850.Scl;
using AR.Iec61850.Transports;
using AR.Iec61850.Transports.Npcap;
using ARSVIN.Subscriber.Models;
using Microsoft.Win32;

namespace ARSVIN.Subscriber.ViewModels;

public sealed class SvSubscriberViewModel : ObservableObject, IDisposable
{
    private readonly ConcurrentDictionary<string, SvStreamRuntime> _runtimeStreams = new(StringComparer.OrdinalIgnoreCase);
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
    public ObservableCollection<DecodedValueRow> SelectedValues { get; } = new();

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
            ObserveFrame(frame.Timestamp, frame.Frame);
        }

        return total;
    }

    private void ObserveFrame(DateTimeOffset timestamp, ReadOnlyMemory<byte> ethernetFrame)
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
        var key = BuildStreamKey(frame, first?.SvId ?? string.Empty, first?.ConfigurationRevision);
        var runtime = _runtimeStreams.GetOrAdd(key, _ => new SvStreamRuntime(key));
        runtime.Observe(timestamp, frame, FindProfile(frame, first));
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
                ObserveFrame(captured.Timestamp, captured.Frame);
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

        return _sclProfiles.FirstOrDefault(profile =>
                   profile.AppId == frame.AppId &&
                   string.Equals(profile.Stream.SvId, asdu.SvId, StringComparison.OrdinalIgnoreCase))
               ?? _sclProfiles.FirstOrDefault(profile => profile.AppId == frame.AppId)
               ?? _sclProfiles.FirstOrDefault(profile => string.Equals(profile.Stream.SvId, asdu.SvId, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildStreamKey(SampledValuesFrame frame, string svId, uint? confRev)
    {
        var vlanText = frame.Vlan.HasValue ? frame.Vlan.Value.VlanId.ToString(CultureInfo.InvariantCulture) : "-";
        var confRevText = confRev.HasValue ? confRev.Value.ToString(CultureInfo.InvariantCulture) : "-";
        return $"SV|{frame.AppId:X4}|{frame.Source}|{frame.Destination}|{vlanText}|{svId}|{confRevText}";
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

            row.Apply(snapshot);
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
        SelectedValues.Clear();
        if (SelectedStream is null)
            return;

        foreach (var value in SelectedStream.Values)
            SelectedValues.Add(value);
    }

    private void UpdateGlobalCards(IReadOnlyList<SvStreamSnapshot> snapshots)
    {
        var raw = Interlocked.Read(ref _rawFrames);
        var sv = Interlocked.Read(ref _svFrames);
        var parse = Interlocked.Read(ref _parseErrors);
        var dropped = Interlocked.Read(ref _droppedByFilter);
        var issues = snapshots.Sum(x => x.SequenceGapCount + x.DuplicateCount + x.OutOfOrderCount + x.PayloadIssueCount + x.SclMismatchCount) + parse;
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
        else if (issues > 0)
            HealthText = snapshots.Any(x => x.Health == "BAD") || parse > 0 ? "BAD" : "WARN";
        else if (snapshots.Count == 0)
            HealthText = IsCapturing ? "LISTENING" : "IDLE";
        else
            HealthText = snapshots.All(x => x.IsBoundToScl) ? "GOOD" : "WARN";

        if (dropped > 0 && IsCapturing)
            StatusText = $"Listening. User filter dropped {dropped:N0} SV frame(s).";
    }

    private void Clear()
    {
        ClearRuntimeOnly();
        Streams.Clear();
        SelectedValues.Clear();
        _streamRows.Clear();
        SelectedStream = null;
        HealthText = "IDLE";
        StatusText = "Cleared subscriber statistics.";
        ExportReportCommand.RaiseCanExecuteChanged();
    }

    private void ClearRuntimeOnly()
    {
        _runtimeStreams.Clear();
        _streamRows.Clear();
        Streams.Clear();
        SelectedValues.Clear();
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
            Title = "Export SV subscriber verification report",
            Filter = "Markdown report (*.md)|*.md|All files (*.*)|*.*",
            FileName = $"arsvin-subscriber-report-{DateTime.Now:yyyyMMdd-HHmmss}.md"
        };

        if (dialog.ShowDialog() != true)
            return;

        var snapshots = _runtimeStreams.Values.Select(x => x.Snapshot()).OrderBy(x => x.AppId).ThenBy(x => x.SvId).ToArray();
        var lines = new List<string>
        {
            "# ARSVIN Subscriber Verification Report",
            string.Empty,
            $"Generated: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}",
            $"SCL: {(string.IsNullOrWhiteSpace(_selectedSclPath) ? "not loaded" : _selectedSclPath)}",
            $"Adapter: {SelectedAdapter?.DisplayName ?? "-"}",
            $"Filter: {(string.IsNullOrWhiteSpace(FilterText) ? "none" : FilterText)}",
            string.Empty,
            "> This report is receiver-side evidence from ARSVIN Subscriber. It is not a formal IEC 61850 conformance certificate.",
            string.Empty,
            "## Summary",
            string.Empty,
            $"- Raw frames: {Interlocked.Read(ref _rawFrames):N0}",
            $"- SV frames: {Interlocked.Read(ref _svFrames):N0}",
            $"- Streams: {snapshots.Length:N0}",
            $"- Health: {HealthText}",
            string.Empty,
            "## Streams",
            string.Empty,
            "| Health | APPID | svID | Bound | nofASDU | fps | smpCnt | Quality | Issues |",
            "|---|---:|---|---|---:|---:|---:|---|---:|"
        };

        foreach (var stream in snapshots)
        {
            var issues = stream.SequenceGapCount + stream.DuplicateCount + stream.OutOfOrderCount + stream.PayloadIssueCount + stream.SclMismatchCount;
            lines.Add($"| {stream.Health} | 0x{stream.AppId:X4} | {Escape(stream.SvId)} | {(stream.IsBoundToScl ? "yes" : "no")} | {stream.NofAsdu} | {stream.ActualFps:0.0} | {stream.LastSmpCnt?.ToString() ?? "-"} | {Escape(stream.QualitySummary)} | {issues} |");
        }

        lines.Add(string.Empty);
        lines.Add("## Phasors");
        lines.Add(string.Empty);
        foreach (var stream in snapshots)
        {
            lines.Add($"### 0x{stream.AppId:X4} — {stream.SvId}");
            lines.Add(string.Empty);
            lines.Add($"- Cursor: {stream.CursorSummary}");
            lines.Add("- Phasors:");
            if (stream.Phasors.Count == 0)
            {
                lines.Add("  - Not enough decoded waveform samples or no SCL binding.");
            }
            else
            {
                foreach (var phasor in stream.Phasors)
                    lines.Add($"  - {phasor.Channel}: RMS {phasor.Rms:0.###}, peak {phasor.Peak:0.###}, angle {phasor.AngleDegrees:0.0}°");
            }
            lines.Add(string.Empty);
        }

        lines.Add("## Diagnostics");
        lines.Add(string.Empty);
        foreach (var stream in snapshots)
        {
            lines.Add($"### 0x{stream.AppId:X4} — {stream.SvId}");
            lines.Add(string.Empty);
            if (stream.Diagnostics.Count == 0)
            {
                lines.Add("- No diagnostics.");
            }
            else
            {
                foreach (var diagnostic in stream.Diagnostics)
                    lines.Add($"- {diagnostic}");
            }
            lines.Add(string.Empty);
        }

        await File.WriteAllLinesAsync(dialog.FileName, lines).ConfigureAwait(true);
        StatusText = $"Subscriber report exported: {dialog.FileName}";
    }

    private static string Escape(string value)
        => string.IsNullOrWhiteSpace(value) ? "-" : value.Replace("|", "\\|", StringComparison.Ordinal);
}
