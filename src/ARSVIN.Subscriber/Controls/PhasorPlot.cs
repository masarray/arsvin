using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ARSVIN.Subscriber.Models;

namespace ARSVIN.Subscriber.Controls;

public sealed class PhasorPlot : FrameworkElement
{
    private INotifyCollectionChanged? _vectorsNotifier;

    public static readonly DependencyProperty VectorsProperty = DependencyProperty.Register(
        nameof(Vectors), typeof(IEnumerable), typeof(PhasorPlot),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnVectorsChanged));

    public static readonly DependencyProperty EmptyMessageProperty = DependencyProperty.Register(
        nameof(EmptyMessage), typeof(string), typeof(PhasorPlot),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    public IEnumerable? Vectors
    {
        get => (IEnumerable?)GetValue(VectorsProperty);
        set => SetValue(VectorsProperty, value);
    }

    public string EmptyMessage
    {
        get => (string)GetValue(EmptyMessageProperty);
        set => SetValue(EmptyMessageProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 420 : availableSize.Width;
        var height = double.IsInfinity(availableSize.Height) ? 420 : availableSize.Height;
        return new Size(Math.Max(280, width), Math.Max(280, height));
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var rect = new Rect(0, 0, ActualWidth, ActualHeight);
        if (rect.Width < 40 || rect.Height < 40)
            return;

        dc.DrawRoundedRectangle(Brush("#FBFDFF"), Pen("#D8E2EF", 1), rect, 10, 10);
        var vectors = Vectors?.OfType<PhasorVector>()
            .Where(vector => vector.IsValid && vector.Rms > 0)
            .ToArray() ?? Array.Empty<PhasorVector>();
        var plot = new Rect(rect.Left + 12, rect.Top + 12, rect.Width - 24, rect.Height - 24);
        var center = new Point(plot.Left + plot.Width / 2.0, plot.Top + plot.Height / 2.0 + 6);
        var radius = Math.Max(40, Math.Min(plot.Width, plot.Height) / 2.0 - 34);

        DrawGrid(dc, center, radius);
        DrawText(dc, "RMS / Phase", new Point(rect.Left + 14, rect.Top + 12), 13, "#2563EB", FontWeights.SemiBold);

        if (vectors.Length == 0)
        {
            var message = string.IsNullOrWhiteSpace(EmptyMessage)
                ? "Waiting for a trusted contiguous cycle"
                : EmptyMessage;
            var color = message.Contains("withheld", StringComparison.OrdinalIgnoreCase)
                ? "#B45309"
                : "#64748B";
            DrawCenteredWrappedText(dc, rect, message, 11.8, color);
            return;
        }

        var voltageMax = vectors.Where(vector => vector.Kind.Equals("Voltage", StringComparison.OrdinalIgnoreCase)).Select(vector => vector.Rms).DefaultIfEmpty(0).Max();
        var currentMax = vectors.Where(vector => vector.Kind.Equals("Current", StringComparison.OrdinalIgnoreCase)).Select(vector => vector.Rms).DefaultIfEmpty(0).Max();
        if (voltageMax <= 0) voltageMax = vectors.Select(vector => vector.Rms).DefaultIfEmpty(1).Max();
        if (currentMax <= 0) currentMax = vectors.Select(vector => vector.Rms).DefaultIfEmpty(1).Max();

        foreach (var vector in vectors.OrderBy(vector => vector.Channel.StartsWith('V') ? 0 : 1).ThenBy(vector => vector.Channel))
            DrawVector(dc, center, radius, vector, voltageMax, currentMax);
    }

    private static void DrawGrid(DrawingContext dc, Point center, double radius)
    {
        dc.DrawEllipse(null, Pen("#E4EDF8", 4), center, radius + 1, radius + 1);
        foreach (var fraction in new[] { 0.25, 0.5, 0.75, 1.0 })
            dc.DrawEllipse(null, fraction >= 1 ? Pen("#2563EB", 1.4) : Pen("#DCE5F0", 0.9, 4, 6), center, radius * fraction, radius * fraction);

        dc.DrawLine(Pen("#B7C6D8", 1.1), new Point(center.X - radius, center.Y), new Point(center.X + radius, center.Y));
        dc.DrawLine(Pen("#B7C6D8", 1.1), new Point(center.X, center.Y - radius), new Point(center.X, center.Y + radius));
        var diagonal = radius / Math.Sqrt(2.0);
        var diagonalPen = Pen("#E2E8F0", 0.9, 3, 5);
        dc.DrawLine(diagonalPen, new Point(center.X - diagonal, center.Y - diagonal), new Point(center.X + diagonal, center.Y + diagonal));
        dc.DrawLine(diagonalPen, new Point(center.X + diagonal, center.Y - diagonal), new Point(center.X - diagonal, center.Y + diagonal));
        dc.DrawEllipse(Brush("#2563EB"), null, center, 4.5, 4.5);
        DrawText(dc, "0°", new Point(center.X + radius + 6, center.Y - 10), 10, "#64748B", FontWeights.Normal);
        DrawText(dc, "+90°", new Point(center.X - 16, center.Y - radius - 20), 10, "#64748B", FontWeights.Normal);
        DrawText(dc, "180°", new Point(center.X - radius - 38, center.Y - 10), 10, "#64748B", FontWeights.Normal);
        DrawText(dc, "-90°", new Point(center.X - 16, center.Y + radius + 4), 10, "#64748B", FontWeights.Normal);
    }

    private static void DrawVector(DrawingContext dc, Point center, double radius, PhasorVector vector, double voltageMax, double currentMax)
    {
        var isVoltage = vector.Kind.Equals("Voltage", StringComparison.OrdinalIgnoreCase) || vector.Channel.StartsWith('V');
        var max = isVoltage ? voltageMax : currentMax;
        if (max <= 0)
            return;

        var normalized = Math.Clamp(vector.Rms / max, 0.0, 1.0);
        var visualRadius = radius * (isVoltage ? 0.96 : 0.78) * normalized;
        var radians = vector.AngleDegrees * Math.PI / 180.0;
        var end = new Point(center.X + visualRadius * Math.Cos(radians), center.Y - visualRadius * Math.Sin(radians));
        var color = ResolveColor(vector.Channel);
        var pen = new Pen(Brush(color), isVoltage ? 2.1 : 2.3) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        pen.Freeze();
        dc.DrawLine(pen, center, end);
        DrawArrowHead(dc, center, end, color);
        DrawText(dc, vector.Channel, new Point(end.X + 6, end.Y - 9), 11.5, color, FontWeights.SemiBold);
    }

    private static void DrawArrowHead(DrawingContext dc, Point start, Point end, string color)
    {
        var vector = start - end;
        if (vector.Length < 1)
            return;

        vector.Normalize();
        var normal = new Vector(-vector.Y, vector.X);
        var first = end + (vector * 10) + (normal * 4);
        var second = end + (vector * 10) - (normal * 4);
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(end, true, true);
            context.LineTo(first, true, false);
            context.LineTo(second, true, false);
        }
        geometry.Freeze();
        dc.DrawGeometry(Brush(color), null, geometry);
    }

    private static string ResolveColor(string channel) => channel switch
    {
        "Ia" => "#EF4444",
        "Ib" => "#D97706",
        "Ic" => "#2563EB",
        "In" => "#94A3B8",
        "Va" => "#EF4444",
        "Vb" => "#D97706",
        "Vc" => "#2563EB",
        "Vn" => "#94A3B8",
        _ => "#64748B"
    };

    private static void OnVectorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PhasorPlot plot)
            plot.AttachNotifier(e.OldValue as INotifyCollectionChanged, e.NewValue as INotifyCollectionChanged);
    }

    private void AttachNotifier(INotifyCollectionChanged? oldValue, INotifyCollectionChanged? newValue)
    {
        if (oldValue is not null)
            oldValue.CollectionChanged -= OnVectorsCollectionChanged;
        if (_vectorsNotifier is not null && !ReferenceEquals(_vectorsNotifier, oldValue) && !ReferenceEquals(_vectorsNotifier, newValue))
            _vectorsNotifier.CollectionChanged -= OnVectorsCollectionChanged;

        _vectorsNotifier = newValue;
        if (_vectorsNotifier is not null)
            _vectorsNotifier.CollectionChanged += OnVectorsCollectionChanged;
        InvalidateVisual();
    }

    private void OnVectorsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => Dispatcher.InvokeAsync(InvalidateVisual);

    private static void DrawCenteredWrappedText(DrawingContext dc, Rect rect, string text, double size, string color)
    {
        var formatted = MakeText(text, size, color, FontWeights.SemiBold);
        formatted.MaxTextWidth = Math.Max(120, rect.Width - 64);
        formatted.TextAlignment = TextAlignment.Center;
        dc.DrawText(formatted, new Point(rect.Left + 32, rect.Top + (rect.Height - formatted.Height) / 2));
    }

    private static void DrawText(DrawingContext dc, string text, Point point, double size, string color, FontWeight weight)
        => dc.DrawText(MakeText(text, size, color, weight), point);

    private static FormattedText MakeText(string text, double size, string color, FontWeight weight)
        => new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
            size, Brush(color), 1.0);

    private static Brush Brush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }

    private static Pen Pen(string color, double width, double dash = 0, double gap = 0)
    {
        var pen = new Pen(Brush(color), width);
        if (dash > 0 && gap > 0)
            pen.DashStyle = new DashStyle(new[] { dash, gap }, 0);
        pen.Freeze();
        return pen;
    }
}