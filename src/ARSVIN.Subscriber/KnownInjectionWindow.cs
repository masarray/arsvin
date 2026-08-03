using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using AR.Iec61850.SampledValues.Field;
using ARSVIN.Subscriber.Models;

namespace ARSVIN.Subscriber;

public sealed class KnownInjectionWindow : Window
{
    private readonly ComboBox _channel = new();
    private readonly TextBox _expectedRms = new();
    private readonly TextBox _amplitudeTolerance = new();
    private readonly TextBox _expectedAngle = new();
    private readonly TextBox _angleTolerance = new();
    private readonly TextBox _expectedFrequency = new();
    private readonly TextBox _frequencyTolerance = new();
    private readonly TextBlock _measured = new();
    private readonly IReadOnlyList<PhasorVector> _phasors;

    public KnownInjectionWindow(IReadOnlyList<PhasorVector> phasors)
    {
        _phasors = phasors?.Where(item => item.IsValid).ToArray()
            ?? throw new ArgumentNullException(nameof(phasors));

        Title = "Validate Known Injection";
        Width = 520;
        Height = 500;
        MinWidth = 480;
        MinHeight = 460;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.CanResizeWithGrip;
        Content = BuildContent();

        _channel.ItemsSource = _phasors;
        _channel.DisplayMemberPath = nameof(PhasorVector.RmsText);
        _channel.SelectedIndex = _phasors.Count > 0 ? 0 : -1;
        _channel.SelectionChanged += (_, _) => RefreshMeasured();
        RefreshMeasured();
    }

    public SvKnownInjectionExpectation? Expectation { get; private set; }
    public SvKnownInjectionMeasurement? Measurement { get; private set; }
    public SvKnownInjectionComparison? Comparison { get; private set; }

    private UIElement BuildContent()
    {
        var root = new Grid { Margin = new Thickness(18) };
        for (var index = 0; index < 9; index++)
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(185) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        AddRow(root, 0, "Channel / measured phasor", _channel);
        AddRow(root, 1, "Measured", _measured);
        AddRow(root, 2, "Expected RMS", _expectedRms);
        AddRow(root, 3, "Amplitude tolerance (%)", _amplitudeTolerance);
        AddRow(root, 4, "Expected angle (deg)", _expectedAngle);
        AddRow(root, 5, "Angle tolerance (deg)", _angleTolerance);
        AddRow(root, 6, "Expected frequency (Hz)", _expectedFrequency);
        AddRow(root, 7, "Frequency tolerance (Hz)", _frequencyTolerance);

        var note = new TextBlock
        {
            Text = "A tolerance is optional. Without tolerance, ArSubsv reports REVIEW with numerical error rather than inventing PASS/FAIL. Values use the currently displayed engineering domain and provenance.",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 12, 0, 12),
            Foreground = System.Windows.Media.Brushes.DimGray
        };
        Grid.SetRow(note, 8);
        Grid.SetColumnSpan(note, 2);
        root.Children.Add(note);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        };
        var validate = new Button { Content = "Validate", MinWidth = 96, Padding = new Thickness(12, 6, 12, 6), IsDefault = true };
        validate.Click += Validate_Click;
        var cancel = new Button { Content = "Cancel", MinWidth = 88, Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(8, 0, 0, 0), IsCancel = true };
        buttons.Children.Add(validate);
        buttons.Children.Add(cancel);
        Grid.SetRow(buttons, 10);
        Grid.SetColumnSpan(buttons, 2);
        root.Children.Add(buttons);

        return root;
    }

    private static void AddRow(Grid grid, int row, string label, UIElement editor)
    {
        var labelBlock = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 5, 12, 5)
        };
        Grid.SetRow(labelBlock, row);
        Grid.SetColumn(labelBlock, 0);
        grid.Children.Add(labelBlock);

        if (editor is FrameworkElement element)
            element.Margin = new Thickness(0, 5, 0, 5);
        Grid.SetRow(editor, row);
        Grid.SetColumn(editor, 1);
        grid.Children.Add(editor);
    }

    private void RefreshMeasured()
    {
        _measured.Text = _channel.SelectedItem is PhasorVector selected
            ? $"{selected.Rms:0.######} {selected.Unit} RMS · {selected.AngleDegrees:0.###}° · {selected.Kind}"
            : "No validated phasor is available for this stream.";
    }

    private void Validate_Click(object sender, RoutedEventArgs e)
    {
        if (_channel.SelectedItem is not PhasorVector selected)
        {
            MessageBox.Show(this, "No valid phasor is available. Resolve SCL mapping, timebase, and waveform confidence first.", Title, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!TryParseRequired(_expectedRms.Text, out var expectedRms) || expectedRms < 0)
        {
            MessageBox.Show(this, "Expected RMS must be a finite non-negative number.", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseOptional(_amplitudeTolerance.Text, out var amplitudeTolerance) ||
            !TryParseOptional(_expectedAngle.Text, out var expectedAngle) ||
            !TryParseOptional(_angleTolerance.Text, out var angleTolerance) ||
            !TryParseOptional(_expectedFrequency.Text, out var expectedFrequency) ||
            !TryParseOptional(_frequencyTolerance.Text, out var frequencyTolerance))
        {
            MessageBox.Show(this, "One or more optional values are not valid numbers.", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Expectation = new SvKnownInjectionExpectation
        {
            Channel = selected.Channel,
            ExpectedRms = expectedRms,
            Unit = selected.Unit,
            Domain = "display",
            ExpectedAngleDegrees = expectedAngle,
            ExpectedFrequencyHz = expectedFrequency,
            AmplitudeTolerancePercent = amplitudeTolerance,
            AngleToleranceDegrees = angleTolerance,
            FrequencyToleranceHz = frequencyTolerance
        };
        Measurement = new SvKnownInjectionMeasurement
        {
            MeasuredRms = selected.Rms,
            MeasuredAngleDegrees = selected.AngleDegrees,
            MeasuredFrequencyHz = null
        };
        Comparison = SvKnownInjectionComparator.Compare(Expectation, Measurement);
        DialogResult = true;
    }

    private static bool TryParseRequired(string text, out double value)
    {
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
            double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            return double.IsFinite(value);
        value = 0;
        return false;
    }

    private static bool TryParseOptional(string text, out double? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text))
            return true;
        if (!TryParseRequired(text, out var parsed))
            return false;
        value = parsed;
        return true;
    }
}
