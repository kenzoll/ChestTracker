using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ChestTracker;

public partial class MainWindow
{
    private sealed record RouteLayerChoiceItem(string Label, string? Layer);
    private sealed record RouteStopChoice(int Index, string Heading, string Instruction);
    private RoutePlan? displayedRoute;

    private void ShowRoutes(object sender, RoutedEventArgs e)
    {
        ShowPage(RoutesPage, RoutesNav);
        LoadRoutes();
    }

    private void LoadRoutes()
    {
        RouteMap.EnsureLoaded();
        if (RouteGuideList.ItemsSource != null) return;
        var english = CultureInfo.GetCultureInfo("en-US").TextInfo;
        RouteStartChoice.ItemsSource = RouteMap.Places.Select(place => place with
        {
            Name = place.Name == "THE $.$. ROCKY" ? "The $.$. Rocky" : english.ToTitleCase(place.Name.ToLowerInvariant())
        }).ToArray();
        RouteStartChoice.SelectedItem = ((MapPlace[])RouteStartChoice.ItemsSource).First(place => place.Name == "Lantern Landing");
        RouteLayerChoice.ItemsSource = new[] { new RouteLayerChoiceItem("Все сундуки", null) }
            .Concat(ChestMap.AvailableLayers.Take(3).Select(layer => new RouteLayerChoiceItem(layer.Label, layer.Id))).ToArray();
        RouteLayerChoice.SelectedIndex = 0;
        RouteLengthChoice.ItemsSource = new[] { 8, 15, 25 };
        RouteLengthChoice.SelectedIndex = 1;
        RouteGuideList.ItemsSource = ChestRoutes.Guides;
        RouteGuideList.SelectedIndex = 0;
    }

    private void SelectRouteGuide(object sender, SelectionChangedEventArgs e)
    {
        if (RouteGuideList.SelectedItem is ChestRoute guide)
            DisplayRoute(ChestRoutes.PlanGuide(guide, RouteMap.MapPins));
    }

    private void GenerateRoute(object sender, RoutedEventArgs e)
    {
        if (RouteStartChoice.SelectedItem is not MapPlace start ||
            RouteLayerChoice.SelectedItem is not RouteLayerChoiceItem type ||
            RouteLengthChoice.SelectedItem is not int length) return;
        RouteGuideList.SelectedIndex = -1;
        DisplayRoute(ChestRoutes.Generate(start, length, type.Layer, RouteMap.MapPins));
    }

    private void DisplayRoute(RoutePlan plan)
    {
        displayedRoute = plan;
        RouteHeading.Text = plan.Route.Name;
        RouteOverview.Text = plan.Route.Summary;
        RouteTotal.Text = $"Точек появления · {plan.Count}";
        RouteSourceButton.Visibility = plan.Route.Source == null ? Visibility.Collapsed : Visibility.Visible;
        RouteLegend.Children.Clear();
        foreach (var layer in ChestMap.AvailableLayers.Take(3))
        {
            int count = plan.Pins.Sum(group => group.Count(pin => pin.Layer == layer.Id));
            RouteLegend.Children.Add(new TextBlock
            {
                Text = $"●  {layer.Label}  {count}", Foreground = new SolidColorBrush(layer.Color), FontSize = 12,
                Margin = new Thickness(0, 0, 14, 0)
            });
        }
        RouteStopList.ItemsSource = plan.Route.Stops.Select((stop, i) => new RouteStopChoice(i,
            $"{i + 1:00}  {stop.Name}" + (plan.Route.Source == null ? "" : $"  ·  {plan.Pins[i].Count}"), stop.Instruction)).ToArray();
        RouteMap.SetRoute(plan);
    }

    private void RouteMapSized(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 1 && e.PreviousSize.Width < 1) RouteMap.FitRoute();
    }

    private void SelectRouteStop(object sender, SelectionChangedEventArgs e)
    {
        if (RouteStopList.SelectedItem is RouteStopChoice stop) RouteMap.FocusRouteStop(stop.Index);
    }

    private void FitRouteMap(object sender, RoutedEventArgs e) => RouteMap.FitRoute();

    private void OpenRouteSource(object sender, RoutedEventArgs e)
    {
        if (displayedRoute?.Route.Source is string url) Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
