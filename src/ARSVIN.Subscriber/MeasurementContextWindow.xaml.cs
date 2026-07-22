using System.Globalization;
using System.Windows;
using AR.Iec61850.SampledValues.Measurements;

namespace ARSVIN.Subscriber;

public partial class MeasurementContextWindow : Window
{
    private readonly string _streamKey;
    private readonly string _svId;

    public MeasurementContextWindow(
        string streamKey,
        string svId,
        SvStreamMeasurementContext? existing)
    {
        InitializeComponent();
        _streamKey = streamKey;
        _svId = svId;

        var domains = new[]
        {
            SvMeasurementValueDomain.PrimaryEngineering,
            SvMeasurementValueDomain.SecondaryEquivalent
        };
        WireDomainBox.ItemsSource = domains;
        DisplayDomainBox.ItemsSource = domains;

        var ratioSources = Enum.GetValues<SvRatioSource>()
            .Where(value => value != SvRatioSource.Unknown)
            .ToArray();
        CurrentSourceBox.ItemsSource = ratioSources;
        VoltageSourceBox.ItemsSource = ratioSources;

        StreamText.Text = $"{svId}\n{streamKey}";
        WireDomainBox.SelectedItem = existing?.WireDomain ?? SvMeasurementValueDomain.PrimaryEngineering;
        DisplayDomainBox.SelectedItem = existing?.DisplayDomain ?? SvMeasurementValueDomain.PrimaryEngineering;
        CurrentSourceBox.SelectedItem = existing?.CurrentRatio?.Source ?? SvRatioSource.Manual;
        VoltageSourceBox.SelectedItem = existing?.VoltageRatio?.Source ?? SvRatioSource.Manual;
        CurrentPrimaryText.Text = Format(existing?.CurrentRatio?.PrimaryNominal);
        CurrentSecondaryText.Text = Format(existing?.CurrentRatio?.SecondaryNominal);
        CurrentReferenceText.Text = existing?.CurrentRatio?.Reference ?? string.Empty;
        VoltagePrimaryText.Text = Format(existing?.VoltageRatio?.PrimaryNominal);
        VoltageSecondaryText.Text = Format(existing?.VoltageRatio?.SecondaryNominal);
        VoltageReferenceText.Text = existing?.VoltageRatio?.Reference ?? string.Empty;
        NotesText.Text = existing?.Notes ?? string.Empty;
        RemoveButton.IsEnabled = existing is not null;
    }

    public SvStreamMeasurementContext? ResultContext { get; private set; }
    public bool RemoveRequested { get; private set; }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var context = new SvStreamMeasurementContext
            {
                StreamKey = _streamKey,
                SvId = _svId,
                WireDomain = (SvMeasurementValueDomain)(WireDomainBox.SelectedItem
                    ?? SvMeasurementValueDomain.PrimaryEngineering),
                DisplayDomain = (SvMeasurementValueDomain)(DisplayDomainBox.SelectedItem
                    ?? SvMeasurementValueDomain.PrimaryEngineering),
                CurrentRatio = BuildRatio(
                    CurrentPrimaryText.Text,
                    CurrentSecondaryText.Text,
                    "A",
                    (SvRatioSource)(CurrentSourceBox.SelectedItem ?? SvRatioSource.Manual),
                    CurrentReferenceText.Text,
                    "current"),
                VoltageRatio = BuildRatio(
                    VoltagePrimaryText.Text,
                    VoltageSecondaryText.Text,
                    "V",
                    (SvRatioSource)(VoltageSourceBox.SelectedItem ?? SvRatioSource.Manual),
                    VoltageReferenceText.Text,
                    "voltage"),
                Notes = NotesText.Text.Trim(),
                UpdatedAt = DateTimeOffset.UtcNow
            };

            var errors = context.Validate();
            if (errors.Count > 0)
                throw new InvalidDataException(string.Join("\n", errors));

            ResultContext = context;
            DialogResult = true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidDataException or OverflowException)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Invalid measurement context",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        RemoveRequested = true;
        ResultContext = null;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;

    private static SvMeasurementRatio? BuildRatio(
        string primaryText,
        string secondaryText,
        string unit,
        SvRatioSource source,
        string reference,
        string label)
    {
        var primaryBlank = string.IsNullOrWhiteSpace(primaryText);
        var secondaryBlank = string.IsNullOrWhiteSpace(secondaryText);
        if (primaryBlank && secondaryBlank)
            return null;
        if (primaryBlank || secondaryBlank)
            throw new FormatException($"Both primary and secondary nominal values are required for the {label} ratio.");

        var primary = ParsePositive(primaryText, $"{label} primary nominal");
        var secondary = ParsePositive(secondaryText, $"{label} secondary nominal");
        return new SvMeasurementRatio
        {
            PrimaryNominal = primary,
            SecondaryNominal = secondary,
            Unit = unit,
            Source = source,
            Reference = reference.Trim()
        };
    }

    private static double ParsePositive(string text, string label)
    {
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out var value) &&
            !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            throw new FormatException($"{label} is not a valid number.");
        if (!double.IsFinite(value) || value <= 0)
            throw new FormatException($"{label} must be a positive finite number.");
        return value;
    }

    private static string Format(double? value)
        => value.HasValue ? value.Value.ToString("0.########", CultureInfo.CurrentCulture) : string.Empty;
}
