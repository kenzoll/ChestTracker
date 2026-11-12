using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace ChestTracker;

public sealed partial class ChestMap
{
    private RoutePlan? routePlan;
    private int activeStop = -1;
    internal IReadOnlyList<ChestPin> MapPins => pins ?? throw new InvalidOperationException("Load the map first.");
    internal event Action<int>? RouteStopClicked;
    private static readonly Brush RouteShade = FrozenBrush(8, 12, 19);
    private static readonly Lazy<Dictionary<string, Brush>> RoutePinColors = new(() => AvailableLayers.ToDictionary(
        layer => layer.Id, layer => FrozenBrush(layer.Color.R, layer.Color.G, layer.Color.B)));

    internal void SetRoute(RoutePlan plan)
    {
        routePlan = plan;
        FitRoute();
    }

    internal void FitRoute()
    {
        activeStop = -1;
        if (routePlan == null || ActualWidth < 1 || ActualHeight < 1)
        {
            Fit();
            return;
        }

        var points = routePlan.Stages.Select(stage => stage.District.Center)
            .Concat(routePlan.Stages.SelectMany(stage => stage.Pins.Select(pin => pin.World)))
            .Concat(routePlan.Crossings.SelectMany(crossing => crossing.Via));
        FitPoints(points, 0.8, 5.5);
    }

    internal void FocusRouteStop(int index)
    {
        if (routePlan == null || index < 0 || index >= routePlan.Stages.Length) return;
        activeStop = index;
        var stage = routePlan.Stages[index];
        FitPoints(stage.Pins.Select(pin => pin.World).Append(stage.District.Center), 3.5, 17);
    }

    private void FitPoints(IEnumerable<Point> points, double minimumZoom, double maximumZoom)
    {
        var bounds = Rect.Empty;
        foreach (var point in points) bounds.Union(point);
        double baseScale = Math.Max(0.01, Math.Min(ActualWidth, ActualHeight) / 194);
        double desired = Math.Min((ActualWidth - 82) / Math.Max(7, bounds.Width), (ActualHeight - 82) / Math.Max(7, bounds.Height));
        zoom = Math.Clamp(desired / baseScale, minimumZoom, maximumZoom);
        pan = new Vector((141 - (bounds.Left + bounds.Right) / 2) * Scale, (141 - (bounds.Top + bounds.Bottom) / 2) * Scale);
        InvalidateVisual();
    }

    private void ClickRouteStop(Point cursor)
    {
        if (routePlan == null) return;
        var stages = routePlan.Stages;
        int closest = Enumerable.Range(0, stages.Length).MinBy(i => (cursor - ToScreen(stages[i].District.Center)).LengthSquared);
        if ((cursor - ToScreen(stages[closest].District.Center)).Length <= 17) RouteStopClicked?.Invoke(closest);
    }

    private void DrawRoute(DrawingContext drawing)
    {
        var plan = routePlan!;
        drawing.DrawRectangle(new SolidColorBrush(Color.FromArgb(63, 5, 7, 13)), null, new Rect(RenderSize));
        var trail = new Pen(Red, 3.6) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        var trailOutline = new Pen(RouteShade, 7);
        var sweep = new Pen(Brushes.White, 1.5) { DashStyle = DashStyles.Dash };
        var sweepOutline = new Pen(RouteShade, 3.5);
        var pinOutline = new Pen(Dark, 1);
        var markerOutline = new Pen(Brushes.White, 1.5);

        for (int i = 0; i < plan.Crossings.Length; i++)
        {
            var from = plan.Stages[i];
            var to = plan.Stages[i + 1];
            var exit = activeStop == i ? from.Pins.LastOrDefault()?.World ?? from.District.Center : from.District.Center;
            var enter = activeStop == i + 1 ? to.Pins.FirstOrDefault()?.World ?? to.District.Center : to.District.Center;
            var points = plan.Crossings[i].Via.Prepend(exit).Append(enter).Select(ToScreen);
            DrawTrail(drawing, points, trail, trailOutline, 26);
        }

        for (int i = 0; i < plan.Stages.Length; i++)
        {
            var stage = plan.Stages[i];
            bool focus = i == activeStop;
            if (focus)
                DrawTrail(drawing, stage.Pins.Select(pin => ToScreen(pin.World)), sweep, sweepOutline, 35);
            foreach (var pin in stage.Pins)
                drawing.DrawEllipse(RoutePinColors.Value[pin.Layer], pinOutline, ToScreen(pin.World), focus ? 4 : 2.6, focus ? 4 : 2.6);
        }

        for (int i = 0; i < plan.Stages.Length; i++)
        {
            var stop = ToScreen(plan.Stages[i].District.Center);
            bool focus = i == activeStop;
            drawing.DrawEllipse(focus ? Red : Dark, markerOutline, stop, 11, 11);
            DrawRouteLabel(drawing, (i + 1).ToString(CultureInfo.InvariantCulture), stop, 11);
            if (focus) DrawRouteLabel(drawing, plan.Stages[i].District.Name, stop + new Vector(0, -26), 12, backdrop: true);
        }
    }

    private void DrawRouteLabel(DrawingContext drawing, string value, Point at, int size, bool backdrop = false)
    {
        var label = new FormattedText(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI Semibold"), size, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        var position = new Point(at.X - label.Width / 2, at.Y - label.Height / 2);
        if (backdrop)
            drawing.DrawRoundedRectangle(RouteShade, null, new Rect(position.X - 5, position.Y - 3, label.Width + 10, label.Height + 6), 4, 4);
        drawing.DrawText(label, position);
    }

    private static void DrawTrail(DrawingContext drawing, IEnumerable<Point> points, Pen line, Pen outline, double arrowInterval)
    {
        using var route = points.GetEnumerator();
        if (!route.MoveNext()) return;
        Point previous = route.Current;
        while (route.MoveNext())
        {
            Point next = route.Current;
            drawing.DrawLine(outline, previous, next);
            drawing.DrawLine(line, previous, next);
            var direction = next - previous;
            if (direction.Length >= arrowInterval)
            {
                var tip = previous + direction * 0.6;
                direction.Normalize();
                var side = new Vector(-direction.Y, direction.X);
                var arrow = new StreamGeometry();
                using (var geometry = arrow.Open())
                {
                    geometry.BeginFigure(tip, true, true);
                    geometry.LineTo(tip - direction * 9 + side * 4.5, true, false);
                    geometry.LineTo(tip - direction * 9 - side * 4.5, true, false);
                }
                drawing.DrawGeometry(Brushes.White, null, arrow);
            }
            previous = next;
        }
    }
}
