using System.IO;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;

namespace ChestTracker;

public partial class MainWindow : Window
{
    private readonly TrackerStore store;
    private readonly ReplayWatcher watcher;
    private readonly LobbyReminder reminder;
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly bool demo;
    private readonly DispatcherTimer reminderTimer = new() { Interval=TimeSpan.FromSeconds(1) };
    private AppUpdates? updates;
    private bool closing;
    private bool restartForUpdate;
    private bool updateNoticeDismissed;
    private DateTime nextUpdateCheck=DateTime.MinValue;
    private readonly DispatcherTimer updateTimer=new(){Interval=TimeSpan.FromMinutes(1)};
    private bool refreshing;
    private string previousTotal = "";
    private static readonly Brush MutedBrush = new SolidColorBrush(Color.FromRgb(181,181,181));
    private sealed record PlayerChoice(string Key, string Name);
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

    public MainWindow(TrackerStore store, bool demo = false)
    {
        this.store = store;
        this.demo = demo;
        InitializeComponent();
        Icon=Brand.Logo;
        InitializeSeasonPages();
        watcher = new(store);
        reminder = new(store);
        watcher.Updated += changed => Refresh(changed);
        watcher.MatchFinished += () => { _ = CheckReminder(); };
        watcher.MatchStarted += () => reminder.DismissForMatchStart();
        reminderTimer.Tick += async (_,_) => await CheckReminder();
        timer.Tick += async (_, _) => await watcher.ScanAsync();
        SourceInitialized += (_, _) => { int dark = 1; DwmSetWindowAttribute(new System.Windows.Interop.WindowInteropHelper(this).Handle, 20, ref dark, sizeof(int)); };
        StateChanged += (_, _) => ApplyAnimations();
        ShowPage(CounterPage, CounterNav);
        Refresh(true);
        UpdatePanel.Visibility=AppUpdates.Configured?Visibility.Visible:Visibility.Collapsed;
        if(!demo&&AppUpdates.Configured)
        {
            updates=new AppUpdates(store.Folder);updates.Changed+=RefreshUpdateNotice;
            updateTimer.Tick+=async (_,_)=>await CheckUpdatesWhenIdle();
        }
    }
    private async void WindowLoaded(object sender, RoutedEventArgs e)
    {
        BindButtonMotion(this);
        ApplyAnimations();
        if (demo) { StatusText.Text = "Пример интерфейса · наблюдение выключено"; StatusDetail.Text = "Показаны демонстрационные матчи. Ваша история не используется.";  ObserveButton.Content = "Демонстрация"; ObserveButton.IsEnabled = false; return; }
        store.Save();
        Microsoft.Win32.SystemEvents.TimeChanged += WindowsTimeChanged;
        Microsoft.Win32.SystemEvents.UserPreferenceChanged += WindowsPreferenceChanged;
        timer.Start(); reminderTimer.Start();
        _ = BeginUpdates();
        await watcher.ScanAsync();
        await CheckReminder();
    }
    private void WindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        closing=true;updateTimer.Stop();
        timer.Stop(); reminderTimer.Stop(); watcher.Stop(); reminder.Stop();
        Microsoft.Win32.SystemEvents.TimeChanged -= WindowsTimeChanged;
        Microsoft.Win32.SystemEvents.UserPreferenceChanged -= WindowsPreferenceChanged;
        try { store.Save(); } catch (Exception ex) { MessageBox.Show(this, "Не удалось сохранить историю:\n" + ex.Message); }
        try{updates?.InstallOnExit(restartForUpdate);}catch(Exception ex){store.Log("Не удалось начать установку обновления: "+ex.Message);}
        updates?.Dispose();
    }
    private async Task BeginUpdates()
    {
        if(updates==null)return;
        await Task.Delay(15000);if(closing)return;
        updateTimer.Start();await CheckUpdatesWhenIdle();
    }
    private async Task CheckUpdatesWhenIdle()
    {
        if(closing||updates==null||watcher.Busy||watcher.Phase=="live"||DateTime.UtcNow<nextUpdateCheck)return;
        nextUpdateCheck=DateTime.UtcNow.AddHours(6);await updates.CheckAsync();
    }
    private async void CheckUpdates(object sender,RoutedEventArgs e)
    {
        if(updates?.IsReady==true){InstallUpdateNow(sender,e);return;}
        if(updates!=null)await updates.CheckAsync();
    }
    private void RefreshUpdateNotice()
    {
        if(updates==null)return;
        UpdateStatus.Text=updates.Status;
        UpdateCheckButton.Content=updates.IsReady?"Обновить":"Проверить";
        UpdateNotice.Visibility=updates.IsReady&&!updateNoticeDismissed&&!watcher.Busy&&watcher.Phase!="live"?Visibility.Visible:Visibility.Collapsed;
    }
    private void DismissUpdate(object sender,RoutedEventArgs e){updateNoticeDismissed=true;UpdateNotice.Visibility=Visibility.Collapsed;}
    private void InstallUpdateNow(object sender,RoutedEventArgs e)
    {
        if(updates?.IsReady!=true)return;
        if(watcher.Busy||watcher.Phase=="live"){UpdateStatus.Text="Обновление готово. Установим после закрытия приложения или окончания матча.";return;}
        restartForUpdate=true;Close();
    }
    private async Task CheckReminder()
    {
        if (demo || !watcher.Running) return;
        await reminder.CheckAsync();

    }
    private void Refresh(bool history)
    {
        StatusText.Text = watcher.Status;
        StatusDetail.Text = watcher.Detail;
        ObserveButton.Content = watcher.Running ? "Ⅱ  Пауза" : "▶  Продолжить";
        AnalysisProgress.Visibility = watcher.Phase is "analyzing" or "recounting" ? Visibility.Visible : Visibility.Collapsed;

        PulseDot.Fill = watcher.Running ? Brushes.White : new SolidColorBrush(Color.FromRgb(105,105,115));

        RefreshUpdateNotice();
        RefreshMatchButtons();
        HealthSummary.Text = watcher.FileCount > 0 ? $"Доступно повторов: {watcher.FileCount}. Новые матчи учитываются автоматически." : "Пока нет повторов. Включите запись и завершите матч.";
        refreshing = true;
        AnimationToggle.IsChecked = store.State.Animations;
        ReminderToggle.IsChecked = store.State.SpiritReminder;
        refreshing = false;
        if (!history) return;
        var matches = store.Current.ToList();
        string total = store.Total.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("ru-RU"));
        TotalNumber.Text = total;
        if (total != previousTotal && store.State.Animations)
            TotalNumber.BeginAnimation(OpacityProperty, new DoubleAnimation(0.35, 1, TimeSpan.FromMilliseconds(350)));
        previousTotal = total;
        ChestEntryButton.Content = store.HasInitialBalance ? "Добавить сундуки" : "Указать начальное количество";
        TotalCaption.Text = $"Ранее: {store.Baseline:N0}  +  по матчам: {matches.Sum(m => (long)m.Total):N0}";
        if (store.ManualTotal > 0) TotalCaption.Text += $"  +  вручную: {store.ManualTotal:N0}";
        var last = matches.FirstOrDefault();
        LastNumber.Text = last?.Total.ToString() ?? "—";
        LastCaption.Text = last == null ? "Появится после завершения матча" : last.DateLabel;
        LastNormal.Text = last?.Normal.ToString() ?? "—";
        LastRare.Text = last?.Rare.ToString() ?? "—";
        RecentGrid.ItemsSource = matches.Take(4).ToList();
        RefreshHistoryDays();
        EmptyRecent.Visibility = matches.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        HistoryCaption.Text = $"Матчей: {matches.Count}   ·   По истории: {matches.Sum(m => (long)m.Total):N0} сундуков";
        int recounted=matches.Count(m=>m.RecountStatus=="Пересчитано в 5.0");
        int notRecounted=matches.Count(m=>m.CounterVersion<CounterSchema.Version&&!string.IsNullOrEmpty(m.RecountStatus));
        if(recounted>0)HistoryCaption.Text+=$"   ·   Пересчитано: {recounted}";
        if(notRecounted>0)HistoryCaption.Text+=$"   ·   Не пересчитано: {notRecounted}";
        PlayerLabel.Text = last?.Player ?? "";

        refreshing = true;
        var profiles = store.State.Matches.GroupBy(m => m.Profile).Select(g => new PlayerChoice(g.Key, g.OrderByDescending(m => m.TimeUtc).First().Player)).ToList();
        ProfilePanel.Visibility = profiles.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        ProfileCombo.ItemsSource = profiles;
        ProfileCombo.SelectedItem = profiles.FirstOrDefault(p => p.Key == store.State.ActiveProfile);
        refreshing = false;
        DrawChart();
        if(RewardsPage.Visibility==Visibility.Visible) RenderRewardWeek(selectedWeek);
    }
    private void ShowPage(FrameworkElement page, Button selected)
    {
        foreach (var p in new FrameworkElement[] { CounterPage, HistoryPage, RewardsPage, MapPage, RoutesPage, HealthPage }) p.Visibility = p == page ? Visibility.Visible : Visibility.Collapsed;
        foreach (var button in new[] { CounterNav, HistoryNav, RewardsNav, MapNav, RoutesNav, HealthNav })
        {
            button.Foreground = button == selected ? Brushes.White : MutedBrush;
            button.Background = button == selected ? new SolidColorBrush(Color.FromRgb(65,20,27)) : Brushes.Transparent;
        }
        if(page!=MapPage) MapViewer.ReleaseImage();
        if(page!=RoutesPage) RouteMap.ReleaseImage();
        if (store.State.Animations)
        {
            page.RenderTransform = new TranslateTransform();
            var ease=new CubicEase { EasingMode=EasingMode.EaseOut };
            page.BeginAnimation(OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(270)) { EasingFunction=ease });
            page.RenderTransform.BeginAnimation(TranslateTransform.YProperty,new DoubleAnimation(10,0,TimeSpan.FromMilliseconds(270)) { EasingFunction=ease });
        }
    }
    private void ShowCounter(object sender, RoutedEventArgs e) => ShowPage(CounterPage, CounterNav);
    private void ShowHistory(object sender, RoutedEventArgs e) { ShowPage(HistoryPage, HistoryNav); DrawChart(); }
    private void ShowHealth(object sender, RoutedEventArgs e) => ShowPage(HealthPage, HealthNav);
    private async void ToggleObservation(object sender, RoutedEventArgs e)
    {
        if (demo) return;
        watcher.Toggle(); ApplyAnimations();
        if (watcher.Running) await watcher.ScanAsync();
    }
    private void ApplyAnimations()
    {
        PulseDot.BeginAnimation(OpacityProperty, null);
        PulseDot.Opacity = 1;
        PulseDot.Fill = watcher.Running ? Brushes.White : new SolidColorBrush(Color.FromRgb(105,105,115));
        if (store.State.Animations && watcher.Running && WindowState != WindowState.Minimized)
            PulseDot.BeginAnimation(OpacityProperty, new DoubleAnimation(0.65, 1, TimeSpan.FromMilliseconds(1600)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
    }
    private void ChangeBaseline(object sender, RoutedEventArgs e)
    {
        bool initial = !store.HasInitialBalance;
        var result = initial
            ? NumberDialog("Начальное количество", "Введите число сундуков, открытых до начала учёта. Если их не было, сохраните 0.", [("Сундуки до начала учёта", 0L)])
            : NumberDialog("Добавить сундуки", "Введите только новые сундуки, которые приложение ещё не учло.", [("Прибавить к общему количеству", 1L)], 1);
        if (result == null) return;
        try
        {
            if (initial) store.SetInitialBalance(result[0]);
            else store.AddManualChests(result[0]);
            Refresh(true);
        }
        catch (Exception error) { MessageBox.Show(this, error.Message, "Не удалось сохранить количество"); }
    }
    private long[]? NumberDialog(string title, string hint, (string Label, long Value)[] fields, long minimum = 0)
    {
        var window = new Window { Icon=Brand.Logo, Title = title, Width = 450, SizeToContent = SizeToContent.Height, ResizeMode = ResizeMode.NoResize, Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = new SolidColorBrush(Color.FromRgb(16,16,16)), Foreground = Brushes.White, FontFamily = FontFamily, FontSize = 14 };
        var panel = new StackPanel { Margin = new Thickness(25) };
        window.Content = panel;
        panel.Children.Add(new TextBlock { Text = title, FontSize = 22, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.White });
        panel.Children.Add(new TextBlock { Text = hint, TextWrapping = TextWrapping.Wrap, Foreground = MutedBrush, Margin = new Thickness(0,12,0,15) });
        var entries = new List<TextBox>();
        foreach (var f in fields)
        {
            panel.Children.Add(new TextBlock { Text = f.Label, Foreground = MutedBrush, Margin = new Thickness(0,10,0,7) });
            var box = new TextBox { Text = f.Value.ToString(), Background = new SolidColorBrush(Color.FromRgb(30,30,30)), Foreground = Brushes.White, CaretBrush = Brushes.White, Padding = new Thickness(12), FontSize = 18 };
            panel.Children.Add(box); entries.Add(box);
        }
        long[]? result = null;
        var save = new Button { Content = "Сохранить", Style = (Style)Resources["Primary"], Margin = new Thickness(0,22,0,0), IsDefault = true };
        save.Click += (_, _) =>
        {
            var values = new List<long>();
            foreach (var b in entries)
            {
                if (!long.TryParse(b.Text.Replace(" ", "").Replace("\u00a0", ""), out long n) || n < minimum || n > 1_000_000_000_000)
                { MessageBox.Show(window, $"Введите целое число от {minimum:N0} до 1 000 000 000 000."); return; }
                values.Add(n);
            }
            result = values.ToArray(); window.DialogResult = true;
        };
        panel.Children.Add(save); window.ShowDialog(); return result;
    }
    private void ShowRecentDetails(object sender, MouseButtonEventArgs e) { if (RecentGrid.SelectedItem is MatchEntry m) ShowDetails(m); }
    private void ShowHistoryDetails(object sender, MouseButtonEventArgs e) => HistoryDetails(sender, e);
    private void HistoryDetails(object sender, RoutedEventArgs e) { if (HistoryGrid.SelectedItem is MatchEntry m) ShowDetails(m); }
    private void ShowDetails(MatchEntry m) => new MatchDetailsWindow(m,this,store.State.Animations).ShowDialog();
    private void ShowText(string title, string text)
    {
        new Window { Icon=Brand.Logo, Title = title, Width = 710, Height = 550, Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new TextBox { Text = text, IsReadOnly = true, Background = new SolidColorBrush(Color.FromRgb(15,15,15)), Foreground = Brushes.White,
                FontFamily = new FontFamily("Consolas"), FontSize = 13, Padding = new Thickness(22), TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto } }.ShowDialog();
    }
    private void EditSelectedMatch(object sender, RoutedEventArgs e)
    {
        if (HistoryGrid.SelectedItem is not MatchEntry m) return;
        var r = NumberDialog("Исправить результат матча", "Ручная коррекция сохранит исходные события в подробностях. Число разбитых не прибавляется к итогу ещё раз.", new[] { ("Обычные пиратские", (long)m.Normal), ("Синие редкие", (long)m.Rare), ("Из них разбито", (long)m.Broken) });
        if (r == null) return;
        if (r.Any(v => v > 1_000_000) || r[2] > r[0] + r[1]) { MessageBox.Show(this, "Проверьте числа: разбитых не может быть больше всех сундуков. Лимит на один матч — 1 000 000."); return; }
        m.Normal = (int)r[0]; m.Rare = (int)r[1]; m.Broken = (int)r[2]; m.Source = "Исправлено";
        store.Log($"Исправлен матч {m.DateLabel}"); store.Save(); Refresh(true);
    }
    private void DeleteSelectedMatch(object sender, RoutedEventArgs e)
    {
        if (HistoryGrid.SelectedItem is not MatchEntry m) return;
        if (MessageBox.Show(this, $"Удалить матч {m.DateLabel}? Из общей суммы будет вычтено {m.Total} сундуков.", "Удалить запись", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        store.State.Matches.Remove(m); store.Log($"Удалён матч {m.DateLabel}"); store.Save(); Refresh(true);
    }
    private void ExportHistory(object sender, RoutedEventArgs e)
    {
        var save = new SaveFileDialog { FileName = "История сундуков.csv", Filter = "CSV для Excel|*.csv", DefaultExt = ".csv" };
        if (save.ShowDialog(this) != true) return;
        System.IO.File.WriteAllText(save.FileName, store.ExportCsv(store.Current), new UTF8Encoding(true));
        MessageBox.Show(this, "История сохранена.", "Экспорт");
    }
    private async void CheckNow(object sender, RoutedEventArgs e) { if (!demo) { await watcher.ScanAsync(); await CheckReminder(); } }
    private void OpenReplayFolder(object sender, RoutedEventArgs e)
    {
        if (!Directory.Exists(store.State.ReplayFolder)) { MessageBox.Show(this, "Папка ещё не создана. Включите повторы в Fortnite и сыграйте матч."); return; }
        Process.Start(new ProcessStartInfo(store.State.ReplayFolder) { UseShellExecute = true });
    }
    private void OpenDataFolder(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo(store.Folder) { UseShellExecute = true });
    private async void ChangeReplayFolder(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Папка с повторами Fortnite", InitialDirectory = Directory.Exists(store.State.ReplayFolder) ? store.State.ReplayFolder : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) };
        if (dialog.ShowDialog(this) != true) return;
        if (watcher.Busy) { MessageBox.Show(this, "Дождитесь окончания анализа или поставьте наблюдение на паузу."); return; }
        store.State.ReplayFolder = dialog.FolderName; store.Save(); Refresh(true); if (!demo) await watcher.ScanAsync();
    }
    private async void ImportOlder(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, "Добавить имеющиеся старые повторы? Если эти сундуки уже входят в начальное число, уменьшите его, чтобы не посчитать их дважды.", "Старые матчи", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        watcher.ImportOld(); if (!demo) await watcher.ScanAsync();
    }
    private async void RetryErrors(object sender, RoutedEventArgs e) { watcher.RetryErrors(); if (!demo) await watcher.ScanAsync(); }
    private void ImportV1(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Файл data/state.json первой версии", Filter = "История версии 1|state.json|JSON|*.json" };
        if (dialog.ShowDialog(this) != true) return;
        int count = store.ImportLegacy(dialog.FileName); Refresh(true);
        MessageBox.Show(this, $"Перенесено матчей: {count}. Начальная сумма перенесена. Исходный файл не изменён.");
    }
    private void ToggleAnimations(object sender, RoutedEventArgs e) { store.State.Animations = !store.State.Animations; store.Save(); ApplyAnimations(); StopButtonMotion(); Refresh(false); }
    private void ToggleReminder(object sender, RoutedEventArgs e)
    {
        store.State.SpiritReminder = !store.State.SpiritReminder;
        if (!store.State.SpiritReminder) store.State.PendingReminder = "";
        store.Save(); Refresh(false);
    }
    private void ChangeProfile(object sender, SelectionChangedEventArgs e)
    {
        if (refreshing || ProfileCombo.SelectedItem is not PlayerChoice choice) return;
        store.State.ActiveProfile = choice.Key; store.Save(); Refresh(true);
    }
    private void ChartSizeChanged(object sender, SizeChangedEventArgs e) => DrawChart();
    private void DrawChart()
    {
        ActivityChart.Children.Clear();
        double width = ActivityChart.ActualWidth;
        if (width < 50) return;
        var list = store.Current.Take(10).Reverse().ToList();
        if (list.Count == 0) { ActivityChart.Children.Add(new TextBlock { Text = "График появится после первого матча", Foreground = MutedBrush, FontSize = 12 }); return; }
        double maximum = Math.Max(1, list.Max(m => m.Total));
        double step = width / 10;
        for (int i = 0; i < list.Count; i++)
        {
            double height = Math.Max(2, list[i].Total / maximum * 65);
            var rect = new Rectangle { Width = Math.Max(12, step - 16), Height = height, Fill = i == list.Count-1 ? new SolidColorBrush(Color.FromRgb(238,67,84)) : new SolidColorBrush(Color.FromRgb(110,110,110)), RadiusX = 3, RadiusY = 3,
                ToolTip = $"{list[i].DateLabel}: {list[i].Total}" };
            Canvas.SetLeft(rect, i * step); Canvas.SetTop(rect, 76 - height); ActivityChart.Children.Add(rect);
            var label = new TextBlock { Text = list[i].Total.ToString(), Foreground = MutedBrush, FontSize = 11 };
            Canvas.SetLeft(label, i * step + 4); Canvas.SetTop(label, 82); ActivityChart.Children.Add(label);
        }
    }
}
