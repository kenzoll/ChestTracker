using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ChestTracker;

public partial class MainWindow
{
    private sealed record RouteStopChoice(int Index, string Heading, string Instruction);
    private RoutePlan? displayedRoute;
    private SavedRoute? displayedSaved;
    private ChestRoutes? routePlanner;
    private readonly GeneratedRouteHistory generatedRoutes = new();

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
        if (routePlanner == null) return;
        generatedRoutes.Add(routePlanner.Generate());
        ShowGeneratedRoute();
    }

    private void PreviousGeneratedRoute(object sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(displayedRoute, generatedRoutes.Current)) generatedRoutes.Move(-1);
        ShowGeneratedRoute();
    }

    private void NextGeneratedRoute(object sender, RoutedEventArgs e)
    {
        generatedRoutes.Move(1);
        ShowGeneratedRoute();
    }

    private void ShowGeneratedRoute()
    {
        if (generatedRoutes.Current is not RoutePlan plan) return;
        RouteGuideList.SelectedIndex = -1;
        SavedRouteList.SelectedIndex = -1;
        DisplayRoute(plan, canSave: true);
    }

    private void DisplayRoute(RoutePlan plan, bool canSave = false, SavedRoute? saved = null)
    {
        displayedRoute = plan;
        PreviousRouteButton.IsEnabled = generatedRoutes.HasPrevious || (!canSave && generatedRoutes.Current != null);
        NextRouteButton.IsEnabled = generatedRoutes.HasNext;
        displayedSaved = saved;
        RouteHeading.Text = plan.Name;
        RouteTotal.Text = $"До {plan.Count} сундуков";
        RouteActions.Visibility = canSave || saved != null ? Visibility.Visible : Visibility.Collapsed;
        SaveRouteButton.Visibility = canSave ? Visibility.Visible : Visibility.Collapsed;
        RenameRouteButton.Visibility = DeleteRouteButton.Visibility = saved != null ? Visibility.Visible : Visibility.Collapsed;
        RouteStopList.ItemsSource = plan.Stages.Select((stage, i) => new RouteStopChoice(i,
            $"{i + 1:00}  {stage.District.Name} · до {stage.Pins.Length}",
            stage.Instruction + (i < plan.Crossings.Length && plan.Crossings[i].Note.Length > 0 ? "\n" + plan.Crossings[i].Note : ""))).ToArray();
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
