using AR.Iec61850.SvPublisher.Models;

namespace AR.Iec61850.SvPublisher.ViewModels;

public sealed class SignalChannelViewModel : ObservableObject
{
    private bool _isEnabled = true;
    private string _name;
    private double _magnitude;
    private double _angleDegrees;
    private double _frequencyHz;
    private double _dcOffsetPercent;
    private double _harmonicPercent;
    private int _harmonicOrder = 2;
    private double _clipPercent = 100;

    public SignalChannelViewModel(string key, string name, string kind, string unit, double magnitude, double angleDegrees, double frequencyHz = 50)
    {
        Key = key;
        _name = name;
        Kind = kind;
        Unit = unit;
        _magnitude = magnitude;
        _angleDegrees = angleDegrees;
        _frequencyHz = frequencyHz;
    }

    public string Key { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
                OnPropertyChanged(nameof(DisplayName));
        }
    }

    public string Kind { get; }
    public string Unit { get; }

    public string DisplayName => Name;

    public string MagnitudeText => $"{Magnitude:0.000} {Unit}";

    public string AngleDegreesText => $"{AngleDegrees:0.000} °";

    public string FrequencyHzText => $"{FrequencyHz:0.000} Hz";

    public string WaveformShapeSummary
    {
        get
        {
            var parts = new List<string>();
            if (Math.Abs(DcOffsetPercent) > 0.0001)
                parts.Add($"DC {DcOffsetPercent:0.###}%");
            if (HarmonicPercent > 0.0001)
                parts.Add($"H{HarmonicOrder} {HarmonicPercent:0.###}%");
            if (Math.Abs(ClipPercent - 100.0) > 0.0001)
                parts.Add($"clip {ClipPercent:0.###}%");
            return parts.Count == 0 ? "clean" : string.Join(", ", parts);
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    /// <summary>
    /// RMS phasor magnitude shown to the operator. The SV payload builder converts this value
    /// to instantaneous peak counts before encoding samples.
    /// </summary>
    public double Magnitude
    {
        get => _magnitude;
        set
        {
            if (SetProperty(ref _magnitude, value))
                OnPropertyChanged(nameof(MagnitudeText));
        }
    }

    public double AngleDegrees
    {
        get => _angleDegrees;
        set
        {
            if (SetProperty(ref _angleDegrees, value))
                OnPropertyChanged(nameof(AngleDegreesText));
        }
    }

    public double FrequencyHz
    {
        get => _frequencyHz;
        set
        {
            if (SetProperty(ref _frequencyHz, value))
                OnPropertyChanged(nameof(FrequencyHzText));
        }
    }

    public double DcOffsetPercent
    {
        get => _dcOffsetPercent;
        set
        {
            if (SetProperty(ref _dcOffsetPercent, CoercePercent(value, -300, 300)))
                OnPropertyChanged(nameof(WaveformShapeSummary));
        }
    }

    public double HarmonicPercent
    {
        get => _harmonicPercent;
        set
        {
            if (SetProperty(ref _harmonicPercent, CoercePercent(value, 0, 300)))
                OnPropertyChanged(nameof(WaveformShapeSummary));
        }
    }

    public int HarmonicOrder
    {
        get => _harmonicOrder;
        set
        {
            if (SetProperty(ref _harmonicOrder, Math.Clamp(value, 2, 63)))
                OnPropertyChanged(nameof(WaveformShapeSummary));
        }
    }

    public double ClipPercent
    {
        get => _clipPercent;
        set
        {
            if (SetProperty(ref _clipPercent, CoercePercent(value, 1, 1000)))
                OnPropertyChanged(nameof(WaveformShapeSummary));
        }
    }

    public void ResetWaveformShape()
    {
        DcOffsetPercent = 0;
        HarmonicPercent = 0;
        HarmonicOrder = 2;
        ClipPercent = 100;
    }

    public SignalChannelSnapshot ToSnapshot()
        => new()
        {
            Key = Key,
            IsEnabled = IsEnabled,
            Magnitude = Magnitude,
            AngleDegrees = AngleDegrees,
            FrequencyHz = FrequencyHz,
            DcOffsetPercent = DcOffsetPercent,
            HarmonicPercent = HarmonicPercent,
            HarmonicOrder = HarmonicOrder,
            ClipPercent = ClipPercent
        };

    private static double CoercePercent(double value, double min, double max)
        => double.IsFinite(value) ? Math.Clamp(value, min, max) : 0;
}
