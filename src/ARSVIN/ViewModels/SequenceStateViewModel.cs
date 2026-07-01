using System.Globalization;
using AR.Iec61850.SvPublisher.Models;

namespace AR.Iec61850.SvPublisher.ViewModels;

public sealed class SequenceStateViewModel : ObservableObject
{
    public const string DurationSecondsPropertyName = nameof(DurationSeconds);
    public const string CurrentMagnitudePropertyName = "CurrentMagnitude";
    public const string VoltageMagnitudePropertyName = "VoltageMagnitude";
    public const string PhaseAAnglePropertyName = "PhaseAAngle";
    public const string PhaseBAnglePropertyName = "PhaseBAngle";
    public const string PhaseCAnglePropertyName = "PhaseCAngle";
    public const string FrequencyHzPropertyName = nameof(FrequencyHz);

    private const string NumericFormat = "0.000";
    private const double NominalVoltageLn = 57.735;

    private string _name;
    private double _durationSeconds;
    private double _currentScale;
    private double _voltageScale;
    private double _angleShiftDegrees;
    private double _frequencyHz;
    private double _currentScaleA = 1;
    private double _currentScaleB = 1;
    private double _currentScaleC = 1;
    private double _currentScaleN;
    private double _voltageScaleA = 1;
    private double _voltageScaleB = 1;
    private double _voltageScaleC = 1;
    private double _voltageScaleN;
    private double _angleOffsetA;
    private double _angleOffsetB;
    private double _angleOffsetC;
    private double _angleOffsetN;
    private double _currentDcOffsetPercent;
    private double _voltageDcOffsetPercent;
    private double _currentHarmonicPercent;
    private double _voltageHarmonicPercent;
    private int _harmonicOrder = 2;
    private double _currentClipPercent = 100;
    private double _voltageClipPercent = 100;
    private string _scenarioTag = string.Empty;
    private bool _isSelected;
    private string _durationText = string.Empty;
    private string _currentText = string.Empty;
    private string _voltageMagnitudeText = string.Empty;
    private string _angleText = string.Empty;
    private string _phaseAAngleText = string.Empty;
    private string _phaseBAngleText = string.Empty;
    private string _phaseCAngleText = string.Empty;
    private string _frequencyText = string.Empty;

    public SequenceStateViewModel(
        string name,
        double durationSeconds,
        double currentScale,
        double voltageScale,
        double angleShiftDegrees,
        double frequencyHz,
        double currentScaleA = 1,
        double currentScaleB = 1,
        double currentScaleC = 1,
        double currentScaleN = 0,
        double voltageScaleA = 1,
        double voltageScaleB = 1,
        double voltageScaleC = 1,
        double voltageScaleN = 0,
        double angleOffsetA = 0,
        double angleOffsetB = 0,
        double angleOffsetC = 0,
        double angleOffsetN = 0,
        double currentDcOffsetPercent = 0,
        double voltageDcOffsetPercent = 0,
        double currentHarmonicPercent = 0,
        double voltageHarmonicPercent = 0,
        int harmonicOrder = 2,
        double currentClipPercent = 100,
        double voltageClipPercent = 100,
        string scenarioTag = "")
    {
        _name = name;
        _durationSeconds = CoerceSeconds(durationSeconds);
        _currentScale = CoerceMagnitude(currentScale);
        _voltageScale = CoerceMagnitude(voltageScale);
        _angleShiftDegrees = NormalizeDegrees(angleShiftDegrees);
        _frequencyHz = CoerceFrequency(frequencyHz);
        _currentScaleA = CoerceMultiplier(currentScaleA, 1);
        _currentScaleB = CoerceMultiplier(currentScaleB, 1);
        _currentScaleC = CoerceMultiplier(currentScaleC, 1);
        _currentScaleN = CoerceMultiplier(currentScaleN, 0);
        _voltageScaleA = CoerceMultiplier(voltageScaleA, 1);
        _voltageScaleB = CoerceMultiplier(voltageScaleB, 1);
        _voltageScaleC = CoerceMultiplier(voltageScaleC, 1);
        _voltageScaleN = CoerceMultiplier(voltageScaleN, 0);
        _angleOffsetA = NormalizeDegrees(angleOffsetA);
        _angleOffsetB = NormalizeDegrees(angleOffsetB);
        _angleOffsetC = NormalizeDegrees(angleOffsetC);
        _angleOffsetN = NormalizeDegrees(angleOffsetN);
        _currentDcOffsetPercent = CoercePercent(currentDcOffsetPercent, -300, 300);
        _voltageDcOffsetPercent = CoercePercent(voltageDcOffsetPercent, -300, 300);
        _currentHarmonicPercent = CoercePercent(currentHarmonicPercent, 0, 300);
        _voltageHarmonicPercent = CoercePercent(voltageHarmonicPercent, 0, 300);
        _harmonicOrder = Math.Clamp(harmonicOrder, 2, 63);
        _currentClipPercent = CoercePercent(currentClipPercent, 1, 1000);
        _voltageClipPercent = CoercePercent(voltageClipPercent, 1, 1000);
        _scenarioTag = scenarioTag ?? string.Empty;
        RefreshTexts();
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public double DurationSeconds
    {
        get => _durationSeconds;
        set
        {
            var coerced = CoerceSeconds(value);
            if (SetProperty(ref _durationSeconds, coerced))
                DurationText = FormatSeconds(coerced);
            else
                DurationText = FormatSeconds(coerced);
        }
    }

    /// <summary>
    /// Absolute RMS current magnitude for the state. The property name is retained for compatibility with
    /// saved snapshots, but the operator-facing workflow treats it as an ampere value, not a UI scale field.
    /// </summary>
    public double CurrentScale
    {
        get => _currentScale;
        set
        {
            var coerced = CoerceMagnitude(value);
            if (SetProperty(ref _currentScale, coerced))
                CurrentText = FormatAmpere(coerced);
            else
                CurrentText = FormatAmpere(coerced);
        }
    }

    /// <summary>
    /// Voltage is stored as per-unit of 57.735 V L-N for snapshot compatibility. The UI exposes the
    /// resulting absolute RMS voltage with a V unit so the state table behaves like a test-set output table.
    /// </summary>
    public double VoltageScale
    {
        get => _voltageScale;
        set
        {
            var coerced = CoerceMagnitude(value);
            if (SetProperty(ref _voltageScale, coerced))
            {
                VoltageMagnitudeText = FormatVolt(VoltageMagnitude);
                OnPropertyChanged(nameof(VoltageText));
                OnPropertyChanged(nameof(VoltageMagnitude));
            }
            else
            {
                VoltageMagnitudeText = FormatVolt(VoltageMagnitude);
            }
        }
    }

    public double VoltageMagnitude => NominalVoltageLn * Math.Max(0, VoltageScale);

    public double AngleShiftDegrees
    {
        get => _angleShiftDegrees;
        set
        {
            var coerced = NormalizeDegrees(value);
            if (SetProperty(ref _angleShiftDegrees, coerced))
                RefreshAngleTexts();
            else
                RefreshAngleTexts();
        }
    }

    public double FrequencyHz
    {
        get => _frequencyHz;
        set
        {
            var coerced = CoerceFrequency(value);
            if (SetProperty(ref _frequencyHz, coerced))
                FrequencyText = FormatHertz(coerced);
            else
                FrequencyText = FormatHertz(coerced);
        }
    }

    public double CurrentScaleA { get => _currentScaleA; set => SetProperty(ref _currentScaleA, CoerceMultiplier(value, 1)); }
    public double CurrentScaleB { get => _currentScaleB; set => SetProperty(ref _currentScaleB, CoerceMultiplier(value, 1)); }
    public double CurrentScaleC { get => _currentScaleC; set => SetProperty(ref _currentScaleC, CoerceMultiplier(value, 1)); }
    public double CurrentScaleN { get => _currentScaleN; set => SetProperty(ref _currentScaleN, CoerceMultiplier(value, 0)); }
    public double VoltageScaleA { get => _voltageScaleA; set => SetProperty(ref _voltageScaleA, CoerceMultiplier(value, 1)); }
    public double VoltageScaleB { get => _voltageScaleB; set => SetProperty(ref _voltageScaleB, CoerceMultiplier(value, 1)); }
    public double VoltageScaleC { get => _voltageScaleC; set => SetProperty(ref _voltageScaleC, CoerceMultiplier(value, 1)); }
    public double VoltageScaleN { get => _voltageScaleN; set => SetProperty(ref _voltageScaleN, CoerceMultiplier(value, 0)); }
    public double AngleOffsetA { get => _angleOffsetA; set => SetProperty(ref _angleOffsetA, NormalizeDegrees(value)); }
    public double AngleOffsetB { get => _angleOffsetB; set => SetProperty(ref _angleOffsetB, NormalizeDegrees(value)); }
    public double AngleOffsetC { get => _angleOffsetC; set => SetProperty(ref _angleOffsetC, NormalizeDegrees(value)); }
    public double AngleOffsetN { get => _angleOffsetN; set => SetProperty(ref _angleOffsetN, NormalizeDegrees(value)); }
    public double CurrentDcOffsetPercent { get => _currentDcOffsetPercent; set => SetProperty(ref _currentDcOffsetPercent, CoercePercent(value, -300, 300)); }
    public double VoltageDcOffsetPercent { get => _voltageDcOffsetPercent; set => SetProperty(ref _voltageDcOffsetPercent, CoercePercent(value, -300, 300)); }
    public double CurrentHarmonicPercent { get => _currentHarmonicPercent; set => SetProperty(ref _currentHarmonicPercent, CoercePercent(value, 0, 300)); }
    public double VoltageHarmonicPercent { get => _voltageHarmonicPercent; set => SetProperty(ref _voltageHarmonicPercent, CoercePercent(value, 0, 300)); }
    public int HarmonicOrder { get => _harmonicOrder; set => SetProperty(ref _harmonicOrder, Math.Clamp(value, 2, 63)); }
    public double CurrentClipPercent { get => _currentClipPercent; set => SetProperty(ref _currentClipPercent, CoercePercent(value, 1, 1000)); }
    public double VoltageClipPercent { get => _voltageClipPercent; set => SetProperty(ref _voltageClipPercent, CoercePercent(value, 1, 1000)); }
    public string ScenarioTag { get => _scenarioTag; set => SetProperty(ref _scenarioTag, value ?? string.Empty); }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string DurationText
    {
        get => _durationText;
        set => SetProperty(ref _durationText, value);
    }

    public string CurrentText
    {
        get => _currentText;
        set => SetProperty(ref _currentText, value);
    }

    public string VoltageText => $"{VoltageScale:0.000} pu";

    public string VoltageMagnitudeText
    {
        get => _voltageMagnitudeText;
        set => SetProperty(ref _voltageMagnitudeText, value);
    }

    public string AngleText
    {
        get => _angleText;
        set => SetProperty(ref _angleText, value);
    }

    public string PhaseAAngleText
    {
        get => _phaseAAngleText;
        set => SetProperty(ref _phaseAAngleText, value);
    }

    public string PhaseBAngleText
    {
        get => _phaseBAngleText;
        set => SetProperty(ref _phaseBAngleText, value);
    }

    public string PhaseCAngleText
    {
        get => _phaseCAngleText;
        set => SetProperty(ref _phaseCAngleText, value);
    }

    public string FrequencyText
    {
        get => _frequencyText;
        set => SetProperty(ref _frequencyText, value);
    }

    public bool CommitText(string propertyName, out string warning)
    {
        warning = string.Empty;
        return propertyName switch
        {
            DurationSecondsPropertyName => CommitSeconds(out warning),
            CurrentMagnitudePropertyName => CommitCurrent(out warning),
            VoltageMagnitudePropertyName => CommitVoltage(out warning),
            PhaseAAnglePropertyName => CommitAngle(PhaseAAngleText, 0, out warning),
            PhaseBAnglePropertyName => CommitAngle(PhaseBAngleText, 120, out warning),
            PhaseCAnglePropertyName => CommitAngle(PhaseCAngleText, -120, out warning),
            FrequencyHzPropertyName => CommitFrequency(out warning),
            _ => true
        };
    }

    public void RejectText(string propertyName)
    {
        switch (propertyName)
        {
            case DurationSecondsPropertyName:
                DurationText = FormatSeconds(DurationSeconds);
                break;
            case CurrentMagnitudePropertyName:
                CurrentText = FormatAmpere(CurrentScale);
                break;
            case VoltageMagnitudePropertyName:
                VoltageMagnitudeText = FormatVolt(VoltageMagnitude);
                break;
            case PhaseAAnglePropertyName:
            case PhaseBAnglePropertyName:
            case PhaseCAnglePropertyName:
                RefreshAngleTexts();
                break;
            case FrequencyHzPropertyName:
                FrequencyText = FormatHertz(FrequencyHz);
                break;
        }
    }

    public void RefreshTexts()
    {
        DurationText = FormatSeconds(DurationSeconds);
        CurrentText = FormatAmpere(CurrentScale);
        VoltageMagnitudeText = FormatVolt(VoltageMagnitude);
        FrequencyText = FormatHertz(FrequencyHz);
        RefreshAngleTexts();
        OnPropertyChanged(nameof(VoltageText));
        OnPropertyChanged(nameof(VoltageMagnitude));
    }

    private bool CommitSeconds(out string warning)
    {
        warning = string.Empty;
        if (!TryParseOperatorNumber(DurationText, "s", out var value) || !IsFinite(value) || value <= 0 || value > 86_400)
        {
            warning = $"Invalid duration for {Name}. Duration must be greater than 0 and not more than 86,400 s.";
            RejectText(DurationSecondsPropertyName);
            return false;
        }

        DurationSeconds = value;
        return true;
    }

    private bool CommitCurrent(out string warning)
    {
        warning = string.Empty;
        if (!TryParseOperatorNumber(CurrentText, "A", out var value) || !IsFinite(value) || value < 0 || value > 1_000_000_000)
        {
            warning = $"Invalid current for {Name}. Current must be a numeric value from 0 to 1,000,000,000 A.";
            RejectText(CurrentMagnitudePropertyName);
            return false;
        }

        CurrentScale = value;
        return true;
    }

    private bool CommitVoltage(out string warning)
    {
        warning = string.Empty;
        if (!TryParseOperatorNumber(VoltageMagnitudeText, "V", out var value) || !IsFinite(value) || value < 0 || value > 1_000_000_000)
        {
            warning = $"Invalid voltage for {Name}. Voltage must be a numeric value from 0 to 1,000,000,000 V.";
            RejectText(VoltageMagnitudePropertyName);
            return false;
        }

        VoltageScale = value / NominalVoltageLn;
        OnPropertyChanged(nameof(VoltageText));
        OnPropertyChanged(nameof(VoltageMagnitude));
        return true;
    }

    private bool CommitAngle(string text, double anchorCompensationDegrees, out string warning)
    {
        warning = string.Empty;
        if (!TryParseOperatorNumber(text, "°", out var value) || !IsFinite(value) || Math.Abs(value) > 360_000)
        {
            warning = $"Invalid angle for {Name}. Angle must be a numeric value within ±360,000 degrees.";
            RejectText(PhaseAAnglePropertyName);
            return false;
        }

        AngleShiftDegrees = NormalizeDegrees(value + anchorCompensationDegrees);
        return true;
    }

    private bool CommitFrequency(out string warning)
    {
        warning = string.Empty;
        if (!TryParseOperatorNumber(FrequencyText, "Hz", out var value) || !IsFinite(value) || value < 0 || value > 5_000)
        {
            warning = $"Invalid frequency for {Name}. Frequency must be a numeric value from 0 to 5000 Hz. Use 0 Hz for DC.";
            RejectText(FrequencyHzPropertyName);
            return false;
        }

        FrequencyHz = value;
        return true;
    }

    private void RefreshAngleTexts()
    {
        AngleText = FormatDegrees(AngleShiftDegrees);
        PhaseAAngleText = FormatDegrees(NormalizeDegrees(AngleShiftDegrees));
        PhaseBAngleText = FormatDegrees(NormalizeDegrees(AngleShiftDegrees - 120));
        PhaseCAngleText = FormatDegrees(NormalizeDegrees(AngleShiftDegrees + 120));
    }

    private static bool TryParseOperatorNumber(string text, string unit, out double value)
    {
        var input = NormalizeOperatorInput(text, unit);
        if (double.TryParse(input, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
            return true;

        return double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static string NormalizeOperatorInput(string text, string unit)
    {
        var input = (text ?? string.Empty).Trim();
        input = input.Replace(" ", string.Empty, StringComparison.Ordinal);
        input = input.Replace("deg", string.Empty, StringComparison.OrdinalIgnoreCase);
        input = input.Replace("degree", string.Empty, StringComparison.OrdinalIgnoreCase);
        input = input.Replace("degrees", string.Empty, StringComparison.OrdinalIgnoreCase);
        input = input.Replace("seconds", string.Empty, StringComparison.OrdinalIgnoreCase);
        input = input.Replace("second", string.Empty, StringComparison.OrdinalIgnoreCase);
        input = input.Replace("sec", string.Empty, StringComparison.OrdinalIgnoreCase);
        input = input.Replace("°", string.Empty, StringComparison.Ordinal);
        input = input.Replace("Hz", string.Empty, StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(unit))
            input = input.Replace(unit, string.Empty, StringComparison.OrdinalIgnoreCase);

        return input.Trim();
    }

    private static double CoerceMagnitude(double value)
        => IsFinite(value) ? Math.Clamp(value, 0, 1_000_000_000) : 0;

    private static double CoerceMultiplier(double value, double fallback)
        => IsFinite(value) ? Math.Clamp(value, 0, 1_000_000) : fallback;

    private static double CoercePercent(double value, double minimum, double maximum)
        => IsFinite(value) ? Math.Clamp(value, minimum, maximum) : 0;

    private static double CoerceFrequency(double value)
        => IsFinite(value) && value >= 0 ? Math.Min(value, 5_000) : 50;

    private static double CoerceSeconds(double value)
        => IsFinite(value) && value > 0 ? Math.Min(value, 86_400) : 0.001;

    private static bool IsFinite(double value)
        => !double.IsNaN(value) && !double.IsInfinity(value);

    private static double NormalizeDegrees(double degrees)
    {
        if (!IsFinite(degrees))
            return 0;

        while (degrees > 180)
            degrees -= 360;
        while (degrees <= -180)
            degrees += 360;
        return Math.Round(degrees, 6);
    }

    private static string FormatNumber(double value)
        => value.ToString(NumericFormat, CultureInfo.InvariantCulture);

    private static string FormatSeconds(double value)
        => $"{FormatNumber(value)} s";

    private static string FormatAmpere(double value)
        => $"{FormatNumber(value)} A";

    private static string FormatVolt(double value)
        => $"{FormatNumber(value)} V";

    private static string FormatHertz(double value)
        => $"{FormatNumber(value)} Hz";

    private static string FormatDegrees(double value)
        => $"{FormatNumber(value)} °";

    public SequenceStateSnapshot ToSnapshot()
        => new()
        {
            Name = Name,
            DurationSeconds = DurationSeconds,
            CurrentScale = CurrentScale,
            VoltageScale = VoltageScale,
            AngleShiftDegrees = AngleShiftDegrees,
            FrequencyHz = FrequencyHz,
            CurrentScaleA = CurrentScaleA,
            CurrentScaleB = CurrentScaleB,
            CurrentScaleC = CurrentScaleC,
            CurrentScaleN = CurrentScaleN,
            VoltageScaleA = VoltageScaleA,
            VoltageScaleB = VoltageScaleB,
            VoltageScaleC = VoltageScaleC,
            VoltageScaleN = VoltageScaleN,
            AngleOffsetA = AngleOffsetA,
            AngleOffsetB = AngleOffsetB,
            AngleOffsetC = AngleOffsetC,
            AngleOffsetN = AngleOffsetN,
            CurrentDcOffsetPercent = CurrentDcOffsetPercent,
            VoltageDcOffsetPercent = VoltageDcOffsetPercent,
            CurrentHarmonicPercent = CurrentHarmonicPercent,
            VoltageHarmonicPercent = VoltageHarmonicPercent,
            HarmonicOrder = HarmonicOrder,
            CurrentClipPercent = CurrentClipPercent,
            VoltageClipPercent = VoltageClipPercent,
            ScenarioTag = ScenarioTag
        };
}
