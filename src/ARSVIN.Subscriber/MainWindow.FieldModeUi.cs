using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace ARSVIN.Subscriber;

public partial class MainWindow
{
    private bool _fieldModeUiAttached;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        AttachFieldHealthStrip();
    }

    private void AttachFieldHealthStrip()
    {
        if (_fieldModeUiAttached || ScopeHost.Child is not UIElement existingScope)
            return;

        _fieldModeUiAttached = true;
        ScopeHost.Child = null;

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var strip = new Border
        {
            Margin = new Thickness(0, 0, 0, 7),
            Padding = new Thickness(6, 5, 6, 5),
            CornerRadius = new CornerRadius(7),
            BorderThickness = new Thickness(1),
            Background = ResolveBrush("ShellBg", Color.FromRgb(248, 251, 255)),
            BorderBrush = ResolveBrush("PanelBorder", Color.FromRgb(220, 230, 243)),
            ToolTip = "Operational health is CAPTURE + PROTOCOL + STREAM. CONFIGURATION and MEASUREMENT are independent review axes."
        };

        var axes = new UniformGrid { Columns = 5, Rows = 1 };
        axes.Children.Add(CreateAxisCard("CAPTURE", "SelectedStream.CaptureFieldState"));
        axes.Children.Add(CreateAxisCard("PROTOCOL", "SelectedStream.ProtocolFieldState"));
        axes.Children.Add(CreateAxisCard("STREAM", "SelectedStream.StreamFieldState"));
        axes.Children.Add(CreateAxisCard("CONFIG", "SelectedStream.ConfigurationFieldState"));
        axes.Children.Add(CreateAxisCard("MEASUREMENT", "SelectedStream.MeasurementFieldState", "SelectedStream.MeasurementFieldDetail"));
        strip.Child = axes;

        Grid.SetRow(strip, 0);
        Grid.SetRow(existingScope, 1);
        root.Children.Add(strip);
        root.Children.Add(existingScope);
        ScopeHost.Child = root;
    }

    private Border CreateAxisCard(string label, string statePath, string? toolTipPath = null)
    {
        var card = new Border
        {
            Margin = new Thickness(3, 0, 3, 0),
            Padding = new Thickness(8, 3, 8, 3),
            CornerRadius = new CornerRadius(6),
            Background = Brushes.White,
            BorderBrush = ResolveBrush("PanelBorder", Color.FromRgb(220, 230, 243)),
            BorderThickness = new Thickness(1)
        };

        if (!string.IsNullOrWhiteSpace(toolTipPath))
            BindingOperations.SetBinding(card, ToolTipProperty, new Binding(toolTipPath));

        var panel = new Grid();
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        panel.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 9.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = ResolveBrush("Dim", Color.FromRgb(100, 116, 139)),
            VerticalAlignment = VerticalAlignment.Center
        });

        var state = new TextBlock
        {
            FontSize = 10.5,
            FontWeight = FontWeights.SemiBold,
            FontFamily = new FontFamily("Cascadia Mono, Consolas"),
            VerticalAlignment = VerticalAlignment.Center,
            Style = CreateFieldStateStyle()
        };
        Grid.SetColumn(state, 1);
        BindingOperations.SetBinding(state, TextBlock.TextProperty, new Binding(statePath) { FallbackValue = "UNKNOWN" });
        panel.Children.Add(state);

        card.Child = panel;
        return card;
    }

    private Style CreateFieldStateStyle()
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.ForegroundProperty, ResolveBrush("Muted", Color.FromRgb(100, 116, 139))));
        AddStateTrigger(style, "GOOD", ResolveBrush("Green", Color.FromRgb(22, 163, 74)));
        AddStateTrigger(style, "QUIET", ResolveBrush("Blue", Color.FromRgb(37, 99, 235)));
        AddStateTrigger(style, "WARN", ResolveBrush("Amber", Color.FromRgb(217, 119, 6)));
        AddStateTrigger(style, "BAD", ResolveBrush("Red", Color.FromRgb(220, 38, 38)));
        AddStateTrigger(style, "ERROR", ResolveBrush("Red", Color.FromRgb(220, 38, 38)));
        return style;
    }

    private static void AddStateTrigger(Style style, string value, Brush brush)
    {
        var trigger = new DataTrigger
        {
            Binding = new Binding("Text") { RelativeSource = new RelativeSource(RelativeSourceMode.Self) },
            Value = value
        };
        trigger.Setters.Add(new Setter(TextBlock.ForegroundProperty, brush));
        style.Triggers.Add(trigger);
    }

    private Brush ResolveBrush(string resourceKey, Color fallback)
        => TryFindResource(resourceKey) as Brush ?? new SolidColorBrush(fallback);
}
