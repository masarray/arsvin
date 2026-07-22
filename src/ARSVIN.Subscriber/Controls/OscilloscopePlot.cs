using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ARSVIN.Subscriber.Models;

namespace ARSVIN.Subscriber.Controls;

public sealed class OscilloscopePlot : FrameworkElement
{
    private double _cursorFraction = 0.72;
    private double _voltageScale = 1.0;
    private double _currentScale = 1.0;
    private string _voltageUnit = "count";
    private string _currentUnit = "count";
    private bool _dragging;
    private INotifyCollectionChanged? _pointsNotifier;

    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register(
        nameof(Points), typeof(IEnumerable), typeof(OscilloscopePlot),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnPointsChanged));

    public IEnumerable? Points
    {
        get => (IEnumerable?)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    public OscilloscopePlot()
    {
        Focusable = true;
        Cursor = Cursors.Cross;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 840 : availableSize.Width;
        var height = double.IsInfinity(availableSize.Height) ? 560 : availableSize.Height;
        return new Size(Math.Max(520, width), Math.Max(360, height));
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        Focus();
        CaptureMouse();
        _dragging = true;
        UpdateCursor(e.GetPosition(this).X);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragging)
            UpdateCursor(e.GetPosition(this).X);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        _dragging = false;
        ReleaseMouseCapture();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var bounds = new Rect(0, 0, ActualWidth, ActualHeight);
        if (bounds.Width < 40 || bounds.Height < 40)
            return;

        dc.DrawRoundedRectangle(Brush("#FBFDFF"), Pen("#D8E2EF", 1), bounds, 10, 10);
        var points = Points?.OfType<WaveformPoint>().ToArray() ?? Array.Empty<WaveformPoint>();
        if (points.Length < 2)
        {
            DrawCenteredText(dc, bounds, "Waiting for decoded SV samples", 13, "#64748B");
            return;
        }

        var latest = points[^1];
        ResetScaleWhenUnitChanges(latest.VoltageUnit, latest.CurrentUnit);

        const double pad = 10;
        const double gap = 12;
        var laneHeight = Math.Max(130, (bounds.Height - (pad * 2) - gap) / 2.0);
        var voltageRect = new Rect(pad, pad, bounds.Width - pad * 2, laneHeight);
        var currentRect = new Rect(pad, pad + laneHeight + gap, bounds.Width - pad * 2, laneHeight);

        DrawLane(dc, voltageRect, "Voltage", latest.VoltageUnit, points,
            new[]
            {
                new Trace("Va", p => p.Va, "#EF4444"),
                new Trace("Vb", p => p.Vb, "#D97706"),
                new Trace("Vc", p => p.Vc, "#2563EB"),
                new Trace("Vn", p => p.Vn, "#94A3B8")
            }, ref _voltageScale);

        DrawLane(dc, currentRect, "Current", latest.CurrentUnit, points,
            new[]
            {
                new Trace("Ia", p => p.Ia, "#EF4444"),
                new Trace("Ib", p => p.Ib, "#D97706"),
                new Trace("Ic", p => p.Ic, "#2563EB"),
                new Trace("In", p => p.In, "#94A3B8")
            }, ref _currentScale);
    }

    private void DrawLane(
        DrawingContext dc,
        Rect lane,
        string title,
        string unit,
        IReadOnlyList<WaveformPoint> points,
        IReadOnlyList<Trace> traces,
        ref double retainedScale)
    {
        dc.DrawRoundedRectangle(Brush("#FFFFFF"), Pen("#D8E2EF", 1), lane, 8, 8);
        DrawGrid(dc, lane);

        var plot = new Rect(lane.Left + 58, lane.Top + 18, Math.Max(60, lane.Width - 74), Math.Max(40, lane.Height - 36));
        var absoluteValues = traces
            .SelectMany(trace => points.Select(trace.Selector))
            .Where(value => value.HasValue)
            .Select(value => Math.Abs(value!.Value))
            .ToArray();
        var observedMax = absoluteValues.DefaultIfEmpty(0).Max();
        var floor = ResolveScaleFloor(title, unit);
        var targetScale = Math.Max(floor, observedMax * 1.15);
        retainedScale = targetScale >= retainedScale
            ? targetScale
            : Math.Max(targetScale, retainedScale * 0.92);

        var nearZero = observedMax <= floor * 0.2;
        DrawText(dc, title, new Point(lane.Left + 12, lane.Top + 10), 12.5, "#2563EB", FontWeights.SemiBold);
        DrawText(dc, unit, new Point(lane.Left + 12, lane.Bottom - 28), 11, "#64748B", FontWeights.Normal);
        DrawText(dc, $"±{retainedScale:0.###}", new Point(lane.Left + 12, lane.Top + 31), 10.5, "#94A3B8", FontWeights.Normal);
        if (nearZero)
            DrawText(dc, "near zero", new Point(lane.Left + 12, lane.Top + 48), 9.8, "#94A3B8", FontWeights.Normal);

        foreach (var trace in traces)
            DrawTrace(dc, plot, points, trace, retainedScale);

        var cursorX = plot.Left + plot.Width * _cursorFraction;
        var cursorPen = Pen("#0EA5E9", 1.2, 4, 5);
        dc.DrawLine(cursorPen, new Point(cursorX, plot.Top), new Point(cursorX, plot.Bottom));
        dc.DrawEllipse(Brush("#2563EB"), null, new Point(cursorX, plot.Top), 3.5, 3.5);

        var x = plot.Left + 10;
        foreach (var trace in traces)
        {
            dc.DrawRoundedRectangle(Brush(trace.Color), null, new Rect(x, lane.Bottom - 20, 8, 8), 3, 3);
            DrawText(dc, trace.Name, new Point(x + 12, lane.Bottom - 24), 10.8, "#64748B", FontWeights.SemiBold);
            x += 44;
        }
    }

    private void ResetScaleWhenUnitChanges(string voltageUnit, string currentUnit)
    {
        if (!string.Equals(_voltageUnit, voltageUnit, StringComparison.Ordinal))
        {
            _voltageUnit = voltageUnit;
            _voltageScale = ResolveScaleFloor("Voltage", voltageUnit);
        }

        if (!string.Equals(_currentUnit, currentUnit, StringComparison.Ordinal))
        {
            _currentUnit = currentUnit;
            _currentScale = ResolveScaleFloor("Current", currentUnit);
        }
    }

    private static double ResolveScaleFloor(string title, string unit)
    {
        if (unit.Equals("V", StringComparison.OrdinalIgnoreCase))
            return 0.5;
        if (unit.Equals("A", StringComparison.OrdinalIgnoreCase))
            return 0.02;
        return title.Equals("Voltage", StringComparison.OrdinalIgnoreCase) ? 100.0 : 10.0;
    }

    private static void DrawGrid(DrawingContext dc, Rect lane)
    {
        var plot = new Rect(lane.Left + 58, lane.Top + 18, Math.Max(60, lane.Width - 74), Math.Max(40, lane.Height - 36));
        var minor = Pen("#E8EEF7", 0.85, 2, 6);
        var major = Pen("#D6E0EC", 0.95, 4, 6);
        for (var i = 0; i <= 8; i++)
        {
            var x = plot.Left + plot.Width * i / 8.0;
            dc.DrawLine(i % 2 == 0 ? major : minor, new Point(x, plot.Top), new Point(x, plot.Bottom));
        }

        for (var i = 0; i <= 4; i++)
        {
            var y = plot.Top + plot.Height * i / 4.0;
            dc.DrawLine(i == 2 ? Pen("#B6C6D8", 1.05) : minor, new Point(plot.Left, y), new Point(plot.Right, y));
        }
    }

    private static void DrawTrace(
        DrawingContext dc,
        Rect plot,
        IReadOnlyList<WaveformPoint> points,
        Trace trace,
        double scale)
    {
        var usable = points
            .Select((point, index) => (value: trace.Selector(point), index))
            .Where(item => item.value.HasValue)
            .ToArray();
        if (usable.Length < 2 || scale <= 0)
            return;

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            for (var i = 0; i < usable.Length; i++)
            {
                var sourceIndex = usable[i].index;
                var x = plot.Left + plot.Width * sourceIndex / Math.Max(1, points.Count - 1);
                var y = plot.Top + plot.Height / 2.0 - (usable[i].value!.Value / scale) * plot.Height * 0.46;
                y = Math.Clamp(y, plot.Top + 1, plot.Bottom - 1);
                if (i == 0)
                    context.BeginFigure(new Point(x, y), false, false);
                else
                    context.LineTo(new Point(x, y), true, false);
            }
        }

        geometry.Freeze();
        dc.DrawGeometry(null, Pen(trace.Color, 1.8), geometry);
    }

    private void UpdateCursor(double x)
    {
        _cursorFraction = Math.Clamp(x / Math.Max(1, ActualWidth), 0.03, 0.97);
        InvalidateVisual();
    }

    private static void OnPointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OscilloscopePlot plot)
            plot.AttachNotifier(e.OldValue as INotifyCollectionChanged, e.NewValue as INotifyCollectionChanged);
    }

    private void AttachNotifier(INotifyCollectionChanged? oldValue, INotifyCollectionChanged? newValue)
    {
        if (oldValue is not null)
            oldValue.CollectionChanged -= OnPointsCollectionChanged;
        if (_pointsNotifier is not null &&
            !ReferenceEquals(_pointsNotifier, oldValue) &&
            !ReferenceEquals(_pointsNotifier, newValue))
            _pointsNotifier.CollectionChanged -= OnPointsCollectionChanged;

        _pointsNotifier = newValue;
        if (_pointsNotifier is not null)
            _pointsNotifier.CollectionChanged += OnPointsCollectionChanged;

        InvalidateVisual();
    }

    private void OnPointsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => Dispatcher.InvokeAsync(InvalidateVisual);

    private static void DrawCenteredText(DrawingContext dc, Rect rect, string text, double size, string color)
    {
        var formatted = MakeText(text, size, color, FontWeights.SemiBold);
        dc.DrawText(formatted, new Point(rect.Left + (rect.Width - formatted.Width) / 2, rect.Top + (rect.Height - formatted.Height) / 2));
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

    private sealed record Trace(string Name, Func<WaveformPoint, double?> Selector, string Color);
}
