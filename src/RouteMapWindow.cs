using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ChestTracker;

internal sealed class RouteMapWindow : Window
{
    internal ChestMap Map { get; } = new();

    public RouteMapWindow(Window owner, RoutePlan plan)
    {
        Owner = owner;
        Title = plan.Name;
        Icon = Brand.Logo;
        FontFamily = owner.FontFamily;
        FontSize = 13;
        Foreground = Brushes.White;
        Background = new SolidColorBrush(Color.FromRgb(15, 15, 17));
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        var area = SystemParameters.WorkArea;
        double side = Math.Min(850, Math.Min(area.Width - 44, area.Height - 84));

        var surface = new Grid { Width = side, Height = side, ClipToBounds = true };
        surface.Children.Add(Map);
        Content = surface;

        var toolbar = new WrapPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(10)
        };
        surface.Children.Add(toolbar);
        AddButton("−", "Отдалить", () => Zoom(1 / 1.35));
        AddButton("+", "Приблизить", () => Zoom(1.35));
        AddButton("Весь остров", "Показать весь остров", Map.FitIsland);
        AddButton("К маршруту", "Показать весь маршрут", Map.FitRoute);

        Map.EnsureLoaded();
        Map.SetRoute(plan);
        Map.RouteStopClicked += Map.FocusRouteStop;
        Loaded += (_, _) => Map.FitIsland();
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Escape) return;
            e.Handled = true;
            Close();
        };

        void Zoom(double factor) => Map.ZoomAt(new Point(Map.ActualWidth / 2, Map.ActualHeight / 2), factor);
        void AddButton(string text, string label, Action action)
        {
            var button = new Button
            {
                Content = text, ToolTip = label, Style = (Style)owner.FindResource(typeof(Button)),
                Padding = new Thickness(10, 7, 10, 7), Margin = new Thickness(3, 0, 0, 0), MinWidth = 32
            };
            System.Windows.Automation.AutomationProperties.SetName(button, label);
            button.Click += (_, _) => action();
            toolbar.Children.Add(button);
        }
    }
}
