using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace ChestTracker;

public sealed partial class ChestMap
{
    private RoutePlan? routePlan;
    private int activeStop = -1;
    internal IReadOnlyList<ChestPin> MapPins => pins ?? throw new InvalidOperationException("Load the map first.");
    internal IReadOnlyList<MapPlace> Places => places;

    internal void SetRoute(RoutePlan plan)
    {
        routePlan = plan;
        activeStop = -1;
        FitRoute();
    }

    internal void FitRoute()
    {
        if (routePlan == null || routePlan.Route.Stops.Length == 0 || ActualWidth < 1 || ActualHeight < 1)
        {
            Fit();
            return;
        }
        var points = routePlan.Route.Stops.Select(stop => stop.Location)
            .Concat(routePlan.Pins.SelectMany(group => group.Select(pin => pin.World))).ToArray();
        double left = points.Min(p => p.X), right = points.Max(p => p.X);
        double top = points.Min(p => p.Y), bottom = points.Max(p => p.Y);
        double baseScale = Math.Min(ActualWidth, ActualHeight) / 194;
        double desired = Math.Min((ActualWidth - 76) / Math.Max(10, right - left), (ActualHeight - 76) / Math.Max(10, bottom - top));
        CenterAt(new((left + right) / 2, (top + bottom) / 2), Math.Clamp(desired / baseScale, 0.8, 16));
    }

    internal void FocusRouteStop(int index)
    {
        if (routePlan == null || index < 0 || index >= routePlan.Route.Stops.Length) return;
        activeStop = index;
        CenterAt(routePlan.Route.Stops[index].Location, routePlan.Route.Stops[index].Radius > 0 ? 6 : Math.Max(zoom, 10));
    }

    private void CenterAt(Point point, double newZoom)
    {
        zoom = newZoom;
        pan = new Vector((141 - point.X) * Scale, (141 - point.Y) * Scale);
        InvalidateVisual();
    }

    private void DrawRoute(DrawingContext drawing)
    {
        var plan = routePlan!;
        drawing.DrawRectangle(new SolidColorBrush(Color.FromArgb(63, 5, 7, 13)), null, new Rect(RenderSize));
        var stops = plan.Route.Stops.Select(stop => ToScreen(stop.Location)).ToArray();
        var path = new Pen(Red, 3.5) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        var outline = new Pen(Dark, 7) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        for (int i = 1; i < stops.Length; i++)
        {
            var intermediate = plan.Route.Via?[i - 1] ?? [];
            Point from = stops[i - 1];
            foreach (Point to in intermediate.Select(ToScreen).Append(stops[i]))
            {
                DrawRouteLeg(drawing, path, outline, from, to);
                from = to;
            }
        }

        for (int i = 0; i < stops.Length; i++)
        {
            foreach (var pin in plan.Pins[i])
            {
                var fill = new SolidColorBrush(AvailableLayers.First(layer => layer.Id == pin.Layer).Color);
                drawing.DrawEllipse(fill, new Pen(Dark, 1), ToScreen(pin.World), 3.3, 3.3);
            }
        }
        for (int i = 0; i < stops.Length; i++)
        {
            var stop = stops[i];
            bool numbered = i == 0 || i == stops.Length - 1 || i == activeStop || plan.Route.Source != null ||
                stops.Where((_, other) => other != i).All(other => (other - stop).Length > 23);
            var fill = i == activeStop ? Red : plan.Route.Source == null
                ? new SolidColorBrush(AvailableLayers.First(layer => layer.Id == plan.Pins[i][0].Layer).Color)
                : Dark;
            drawing.DrawEllipse(numbered && i != activeStop ? Dark : fill, new Pen(Brushes.White, 1.4), stop, numbered ? 10 : 4, numbered ? 10 : 4);
            if (!numbered) continue;
            var label = new FormattedText((i + 1).ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI Semibold"), 10, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            drawing.DrawText(label, new Point(stop.X - label.Width / 2, stop.Y - label.Height / 2));
        }
    }

    private static void DrawRouteLeg(DrawingContext drawing, Pen path, Pen outline, Point from, Point to)
    {
        var direction = to - from;
        double distance = direction.Length;
        if (distance < 4) return;
        direction.Normalize();
        var side = new Vector(-direction.Y, direction.X);
        drawing.DrawLine(outline, from, to);
        drawing.DrawLine(path, from, to);
        if (distance < 30) return;
        Point tip = from + direction * (distance * 0.58);
        var arrow = new StreamGeometry();
        using (var geometry = arrow.Open())
        {
            geometry.BeginFigure(tip, true, true);
            geometry.LineTo(tip - direction * 11 + side * 5.5, true, false);
            geometry.LineTo(tip - direction * 11 - side * 5.5, true, false);
        }
        drawing.DrawGeometry(Brushes.White, null, arrow);
    }
}
