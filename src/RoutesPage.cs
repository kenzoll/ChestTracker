using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ChestTracker;

public partial class MainWindow
{
    private sealed record StartChoice(RouteArea Area, string Name);
    private sealed record GoalChoice(int Count)
    {
        public string Label => $"{Count} сундуков";
    }
    private sealed record RouteStopChoice(int Index, string Heading, string Instruction);
    private RoutePlan? displayedRoute;
    private SavedRoute? displayedSaved;
    private ChestRoutes? routePlanner;

    private void ShowRoutes(object sender, RoutedEventArgs e)
    {
        ShowPage(RoutesPage, RoutesNav);
        LoadRoutes();
    }

    private void LoadRoutes()
    {
        RouteMap.EnsureLoaded();
        if (routePlanner != null) return;
        routePlanner = new ChestRoutes(RouteMap.MapPins);
        var starts = ChestRoutes.Starts.Select(start => new StartChoice(start.Area, start.Name)).OrderBy(start => start.Name).ToArray();
        RouteStartChoice.ItemsSource = starts;
        RouteStartChoice.SelectedItem = starts.First(start => start.Area == RouteArea.Lantern);
        RouteLengthChoice.ItemsSource = new[] { 60, 100, 150, 200, 250 }.Select(count => new GoalChoice(count)).ToArray();
        RouteLengthChoice.SelectedIndex = 1;
        RouteGuideList.ItemsSource = ChestRoutes.Guides;
        RouteMap.RouteStopClicked += index => RouteStopList.SelectedIndex = index;
        RefreshSavedRoutes();
        SetRouteLibrary(saved: false);
        RouteGuideList.SelectedIndex = 0;
    }

    private void SelectRouteGuide(object sender, SelectionChangedEventArgs e)
    {
        if (RouteGuideList.SelectedItem is RoutePreset guide && routePlanner != null)
            DisplayRoute(routePlanner.Plan(guide));
    }

    private void GenerateRoute(object sender, RoutedEventArgs e)
    {
        if (routePlanner == null || RouteStartChoice.SelectedItem is not StartChoice start ||
            RouteLengthChoice.SelectedItem is not GoalChoice goal) return;
        RouteGuideList.SelectedIndex = -1;
        SavedRouteList.SelectedIndex = -1;
        DisplayRoute(routePlanner.Generate(start.Area, goal.Count, AvoidRouteWater.IsChecked == true), canSave: true);
    }

    private void DisplayRoute(RoutePlan plan, bool canSave = false, SavedRoute? saved = null)
    {
        displayedRoute = plan;
        displayedSaved = saved;
        RouteHeading.Text = plan.Name;
        RouteTotal.Text = $"До {plan.Count} сундуков";
        int water = plan.Crossings.Count(edge => edge.Water);
        RouteOverview.Text = $"Районов · {plan.Stages.Length}     Переправ · {water}" +
            (string.IsNullOrWhiteSpace(plan.Summary) ? "" : "\n" + plan.Summary);
        RouteLegend.Children.Clear();
        foreach (var layer in ChestMap.AvailableLayers.Take(3))
        {
            int count = plan.Stages.Sum(stage => stage.Pins.Count(pin => pin.Layer == layer.Id));
            if (count == 0) continue;
            RouteLegend.Children.Add(new TextBlock
            {
                Text = $"●  {layer.Label}  {count}", Foreground = new SolidColorBrush(layer.Color), FontSize = 12,
                Margin = new Thickness(0, 0, 14, 0)
            });
        }
        RouteActions.Visibility = canSave || saved != null ? Visibility.Visible : Visibility.Collapsed;
        SaveRouteButton.Visibility = canSave ? Visibility.Visible : Visibility.Collapsed;
        RenameRouteButton.Visibility = DeleteRouteButton.Visibility = saved != null ? Visibility.Visible : Visibility.Collapsed;
        RouteStopList.ItemsSource = plan.Stages.Select((stage, i) => new RouteStopChoice(i,
            $"{i + 1:00}  {stage.District.Name} · до {stage.Pins.Length}",
            stage.Instruction + (i < plan.Crossings.Length ? "\n" + plan.Crossings[i].Note : ""))).ToArray();
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

    private void FitRouteMap(object sender, RoutedEventArgs e)
    {
        RouteStopList.SelectedIndex = -1;
        RouteMap.FitRoute();
    }

    private void ShowReadyRoutes(object sender, RoutedEventArgs e) => SetRouteLibrary(saved: false);
    private void ShowSavedRoutes(object sender, RoutedEventArgs e) => SetRouteLibrary(saved: true);

    private void SetRouteLibrary(bool saved)
    {
        ReadyRoutesButton.Background = saved ? Brushes.Transparent : new SolidColorBrush(Color.FromRgb(68, 23, 32));
        SavedRoutesButton.Background = saved ? new SolidColorBrush(Color.FromRgb(68, 23, 32)) : Brushes.Transparent;
        RouteGuideList.Visibility = saved ? Visibility.Collapsed : Visibility.Visible;
        SavedRouteList.Visibility = saved ? Visibility.Visible : Visibility.Collapsed;
        NoSavedRoutes.Visibility = saved && store.State.SavedRoutes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshSavedRoutes(string? selectedId = null)
    {
        SavedRouteList.ItemsSource = store.State.SavedRoutes.ToArray();
        SavedRouteList.SelectedItem = store.State.SavedRoutes.FirstOrDefault(route => route.Id == selectedId);
    }

    private void SelectSavedRoute(object sender, SelectionChangedEventArgs e)
    {
        if (SavedRouteList.SelectedItem is not SavedRoute saved || routePlanner == null) return;
        var plan = routePlanner.Open(saved);
        if (plan == null)
        {
            MessageBox.Show(this, "Этот маршрут недоступен на текущей карте.", "Мои маршруты");
            return;
        }
        RouteGuideList.SelectedIndex = -1;
        DisplayRoute(plan, saved: saved);
    }

    private void SaveCurrentRoute(object sender, RoutedEventArgs e)
    {
        if (displayedRoute is not RoutePlan plan) return;
        string? name = AskRouteName("Сохранить маршрут", plan.Name);
        if (name == null) return;
        WriteSavedRoute(new(Guid.NewGuid().ToString("N"), name, plan.Areas.Select(area => area.ToString()).ToArray()));
    }

    private void RenameSavedRoute(object sender, RoutedEventArgs e)
    {
        if (displayedSaved is not SavedRoute saved) return;
        string? name = AskRouteName("Переименовать маршрут", saved.Name);
        if (name != null) WriteSavedRoute(saved with { Name = name });
    }

    private void WriteSavedRoute(SavedRoute route)
    {
        try
        {
            store.StoreRoute(route);
            RefreshSavedRoutes(route.Id);
            SetRouteLibrary(saved: true);
        }
        catch (Exception error) { MessageBox.Show(this, error.Message, "Не удалось сохранить маршрут"); }
    }

    private void DeleteSavedRoute(object sender, RoutedEventArgs e)
    {
        if (displayedSaved is not SavedRoute saved) return;
        if (MessageBox.Show(this, $"Удалить маршрут «{saved.Name}»?", "Мои маршруты",
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        try
        {
            store.RemoveRoute(saved);
            RefreshSavedRoutes();
            SetRouteLibrary(saved: true);
            if (SavedRouteList.Items.Count > 0) SavedRouteList.SelectedIndex = 0;
            else
            {
                SetRouteLibrary(saved: false);
                RouteGuideList.SelectedIndex = 0;
            }
        }
        catch (Exception error) { MessageBox.Show(this, error.Message, "Не удалось удалить маршрут"); }
    }

    private string? AskRouteName(string title, string current)
    {
        var dialog = new Window
        {
            Owner = this, Title = title, Icon = Brand.Logo, Width = 420, SizeToContent = SizeToContent.Height,
            ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.FromRgb(16, 16, 16)), Foreground = Brushes.White,
            FontFamily = FontFamily, FontSize = 14
        };
        var panel = new StackPanel { Margin = new Thickness(22) };
        panel.Children.Add(new TextBlock { Text = "Название маршрута", Margin = new Thickness(0, 0, 0, 8), Foreground = Brushes.White });
        var entry = new TextBox
        {
            Text = current, MaxLength = 80, Padding = new Thickness(10), Foreground = Brushes.White, CaretBrush = Brushes.White,
            Background = new SolidColorBrush(Color.FromRgb(31, 31, 31))
        };
        panel.Children.Add(entry);
        var save = new Button { Content = "Сохранить", Style = (Style)Resources["Primary"], IsDefault = true, Margin = new Thickness(0, 14, 0, 0) };
        save.IsEnabled = !string.IsNullOrWhiteSpace(entry.Text);
        entry.TextChanged += (_, _) => save.IsEnabled = !string.IsNullOrWhiteSpace(entry.Text);
        save.Click += (_, _) => dialog.DialogResult = true;
        panel.Children.Add(save);
        dialog.Content = panel;
        dialog.Loaded += (_, _) => { entry.Focus(); entry.SelectAll(); };
        return dialog.ShowDialog() == true ? entry.Text.Trim() : null;
    }
}
