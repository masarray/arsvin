using System.Collections.ObjectModel;
using ARSVIN.Subscriber.Models;

namespace ARSVIN.Subscriber.ViewModels;

public sealed class SvStreamViewModel : ObservableObject
{
    private string _key = string.Empty;
    private string _health = "IDLE";
    private string _healthDetail = string.Empty;
    private string _appId = string.Empty;
    private string _svId = string.Empty;
    private string _source = string.Empty;
    private string _destination = string.Empty;
    private string _vlan = string.Empty;
    private string _confRev = string.Empty;
    private string _nofAsdu = string.Empty;
    private string _sampleRate = string.Empty;
    private string _smpCnt = string.Empty;
    private string _smpSynch = string.Empty;
    private string _packets = string.Empty;
    private string _fps = string.Empty;
    private string _gap = string.Empty;
    private string _issues = string.Empty;
    private string _bound = string.Empty;
    private string _lastSeen = string.Empty;
    private string _summary = string.Empty;
    private string _dataSet = string.Empty;
    private string _sourceDestination = string.Empty;
    private string _cursorSummary = string.Empty;
    private string _qualitySummary = string.Empty;

    public ObservableCollection<DecodedValueRow> Values { get; } = new();
    public ObservableCollection<WaveformPoint> WaveformPoints { get; } = new();
    public ObservableCollection<PhasorVector> Phasors { get; } = new();

    public string Key { get => _key; set => SetProperty(ref _key, value); }
    public string Health { get => _health; set => SetProperty(ref _health, value); }
    public string HealthDetail { get => _healthDetail; set => SetProperty(ref _healthDetail, value); }
    public string AppId { get => _appId; set => SetProperty(ref _appId, value); }
    public string SvId { get => _svId; set => SetProperty(ref _svId, value); }
    public string Source { get => _source; set => SetProperty(ref _source, value); }
    public string Destination { get => _destination; set => SetProperty(ref _destination, value); }
    public string Vlan { get => _vlan; set => SetProperty(ref _vlan, value); }
    public string ConfRev { get => _confRev; set => SetProperty(ref _confRev, value); }
    public string NofAsdu { get => _nofAsdu; set => SetProperty(ref _nofAsdu, value); }
    public string SampleRate { get => _sampleRate; set => SetProperty(ref _sampleRate, value); }
    public string SmpCnt { get => _smpCnt; set => SetProperty(ref _smpCnt, value); }
    public string SmpSynch { get => _smpSynch; set => SetProperty(ref _smpSynch, value); }
    public string Packets { get => _packets; set => SetProperty(ref _packets, value); }
    public string Fps { get => _fps; set => SetProperty(ref _fps, value); }
    public string Gap { get => _gap; set => SetProperty(ref _gap, value); }
    public string Issues { get => _issues; set => SetProperty(ref _issues, value); }
    public string Bound { get => _bound; set => SetProperty(ref _bound, value); }
    public string LastSeen { get => _lastSeen; set => SetProperty(ref _lastSeen, value); }
    public string Summary { get => _summary; set => SetProperty(ref _summary, value); }
    public string DataSet { get => _dataSet; set => SetProperty(ref _dataSet, value); }
    public string SourceDestination { get => _sourceDestination; set => SetProperty(ref _sourceDestination, value); }
    public string CursorSummary { get => _cursorSummary; set => SetProperty(ref _cursorSummary, value); }
    public string QualitySummary { get => _qualitySummary; set => SetProperty(ref _qualitySummary, value); }

    public void Apply(SvStreamSnapshot snapshot)
    {
        Key = snapshot.Key;
        Health = snapshot.Health;
        HealthDetail = snapshot.HealthDetail;
        AppId = $"0x{snapshot.AppId:X4}";
        SvId = string.IsNullOrWhiteSpace(snapshot.SvId) ? "-" : snapshot.SvId;
        Source = snapshot.Source;
        Destination = snapshot.Destination;
        SourceDestination = string.IsNullOrWhiteSpace(snapshot.Source) ? "-" : $"{snapshot.Source} → {snapshot.Destination}";
        Vlan = snapshot.VlanId.HasValue ? $"{snapshot.VlanId} / p{snapshot.VlanPriority ?? 0}" : "untagged";
        ConfRev = snapshot.ConfRev?.ToString() ?? "-";
        NofAsdu = snapshot.NofAsdu <= 0 ? "-" : snapshot.NofAsdu.ToString();
        SampleRate = snapshot.SampleRate?.ToString() ?? "-";
        SmpCnt = snapshot.LastSmpCnt?.ToString() ?? "-";
        SmpSynch = snapshot.SmpSynch?.ToString() ?? "-";
        Packets = snapshot.FrameCount.ToString("N0");
        Fps = $"{snapshot.ActualFps:0.0}";
        Gap = $"avg {snapshot.AverageFrameGapMilliseconds:0.###} ms / max {snapshot.MaxFrameGapMilliseconds:0.###} ms";
        DataSet = string.IsNullOrWhiteSpace(snapshot.DataSet) ? "-" : snapshot.DataSet;
        CursorSummary = snapshot.CursorSummary;
        QualitySummary = snapshot.QualitySummary;
        var issueTotal = snapshot.SequenceGapCount + snapshot.DuplicateCount + snapshot.OutOfOrderCount + snapshot.PayloadIssueCount + snapshot.SclMismatchCount;
        Issues = issueTotal == 0
            ? "0"
            : $"{issueTotal} (gap {snapshot.SequenceGapCount}, dup {snapshot.DuplicateCount}, order {snapshot.OutOfOrderCount}, payload {snapshot.PayloadIssueCount}, SCL {snapshot.SclMismatchCount})";
        Bound = snapshot.IsBoundToScl
            ? $"SCL: {snapshot.ControlBlockReference}"
            : string.IsNullOrWhiteSpace(snapshot.LayoutBinding) ? "Unbound" : snapshot.LayoutBinding;
        LastSeen = snapshot.LastSeen;
        Summary = string.Join("  •  ", snapshot.Diagnostics.Take(3));

        Replace(Values, snapshot.Values.Take(64));
        Replace(WaveformPoints, snapshot.WaveformPoints);
        Replace(Phasors, snapshot.Phasors);
    }

    private static void Replace<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
            collection.Add(item);
    }
}
