using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Diagnostics;

namespace ChestTracker;

public partial class MainWindow
{
    private RewardWeek selectedWeek = RewardCatalog.Active(DateTime.UtcNow);
    private readonly Dictionary<int,Button> weekButtons=[];
    private readonly HashSet<Button> motionButtons=[];
    private static readonly Dictionary<string,BitmapImage> rewardImages=[];
    private void InitializeSeasonPages()
    {
        foreach(var week in RewardCatalog.Weeks)
        {
            var label=new StackPanel();
            label.Children.Add(new TextBlock{Text=$"НЕДЕЛЯ {week.Number}",FontSize=11,Foreground=Brushes.White,FontWeight=FontWeights.SemiBold});
            label.Children.Add(new TextBlock{Text=GameClock.Local(week.StartUtc).ToString("d MMMM",System.Globalization.CultureInfo.GetCultureInfo("ru-RU")),FontSize=11,Foreground=MutedBrush,Margin=new Thickness(0,5,0,0)});
            var button=new Button{Content=label,Width=158,Margin=new Thickness(0,0,10,8)};
            button.Click+=(_,_)=>RenderRewardWeek(week);weekButtons[week.Number]=button;WeekTabs.Children.Add(button);
        }
        foreach(var layer in ChestMap.AvailableLayers)
        {
            var choice=new CheckBox{Style=(Style)Resources["RewardCheck"],Content=layer.Label,Foreground=Brushes.White,FontSize=12,Margin=new Thickness(0,5,18,5),IsChecked=layer.DefaultOn,Tag=layer.Id};
            choice.Checked+=(_,_)=>MapViewer.SetLayer(layer.Id,true);
            choice.Unchecked+=(_,_)=>MapViewer.SetLayer(layer.Id,false);
            MapLayerChoices.Children.Add(choice);
        }
        RenderRewardWeek(selectedWeek);
        SizeChanged+=(_,_)=>MapViewer.Height=Math.Clamp(ActualHeight-310,380,850);
    }
    private void ShowRewards(object sender,RoutedEventArgs e){RenderRewardWeek(selectedWeek);ShowPage(RewardsPage,RewardsNav);}
    private void ShowMap(object sender,RoutedEventArgs e){ShowPage(MapPage,MapNav);MapViewer.EnsureLoaded();}
    private static BitmapImage RewardImage(string name)
    {
        if(rewardImages.TryGetValue(name,out var value))return value;
        using var stream=StoragePaths.Asset(name);var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;
        image.DecodePixelWidth=420;image.StreamSource=stream;image.EndInit();image.Freeze();rewardImages[name]=image;return image;
    }
    private void RenderRewardWeek(RewardWeek week)
    {
        selectedWeek=week;
        foreach(var pair in weekButtons)
        {
            pair.Value.Background=pair.Key==week.Number?new SolidColorBrush(Color.FromRgb(65,20,27)):new SolidColorBrush(Color.FromRgb(24,24,28));
            pair.Value.BorderBrush=pair.Key==week.Number?new SolidColorBrush(Color.FromRgb(203,41,57)):new SolidColorBrush(Color.FromRgb(48,48,55));
        }
        WeekHeading.Text=$"Неделя {week.Number} — {week.Title}";WeekStatus.Text=week.Status(DateTime.UtcNow);
        WeekDates.Text=$"{GameClock.Local(week.StartUtc):dd.MM} — {GameClock.Local(week.EndUtc):dd.MM}";
        RewardOddsNote.Text=$"Шанс получить награду хотя бы раз за {store.Total:N0} учтённых сундуков";
        RewardOddsNote.ToolTip="Шанс хотя бы одного выпадения за указанное число подходящих сундуков при обычных шансах сезона. Для недельных наград это условный расчёт: общий счётчик включает и другие недели. Шанс следующего сундука от прошлых открытий не растёт. Бонусные часы здесь не учтены.";
        RewardOddsNote.Visibility=week.Items.Length>0?Visibility.Visible:Visibility.Collapsed;
        RewardCards.Children.Clear();UnknownRewardsPanel.Visibility=week.Items.Length==0?Visibility.Visible:Visibility.Collapsed;
        var items=week.Items.Length>0?week.Items.Append(RewardCatalog.Token):Enumerable.Empty<RewardItem>();
        foreach(var reward in items)
        {
            string key=store.State.ActiveProfile+"|"+reward.Id;
            bool owned=store.State.FoundRewards.Contains(key);
            var card=new Border{Width=292,Height=424,Margin=new Thickness(0,0,16,16),CornerRadius=new CornerRadius(15),Background=new SolidColorBrush(Color.FromRgb(17,17,22)),BorderBrush=new SolidColorBrush(Color.FromRgb(47,47,56)),BorderThickness=new Thickness(1)};
            var panel=new Grid();panel.RowDefinitions.Add(new(){Height=new GridLength(200)});panel.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});panel.RowDefinitions.Add(new(){Height=GridLength.Auto});
            var art=new Border{Margin=new Thickness(10),CornerRadius=new CornerRadius(10),Background=new SolidColorBrush(Color.FromRgb(23,23,30))};
            art.Child=new Image{Source=RewardImage(reward.ImageName),Stretch=Stretch.Uniform,Margin=new Thickness(8)};panel.Children.Add(art);
            var text=new StackPanel{Margin=new Thickness(20,8,20,0)};
            text.Children.Add(new TextBlock{Text=reward.Kind.ToUpperInvariant(),Foreground=MutedBrush,FontSize=10});
            text.Children.Add(new TextBlock{Text=reward.Name,Foreground=Brushes.White,FontSize=18,FontWeight=FontWeights.SemiBold,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,0)});Grid.SetRow(text,1);panel.Children.Add(text);
            int denominator=RewardOdds.Denominator(reward.Kind);
            var odds=new StackPanel{Margin=new Thickness(0,12,0,0),Visibility=!owned&&denominator>0?Visibility.Visible:Visibility.Collapsed};
            odds.Children.Add(new TextBlock{Text=$"За все сундуки · {RewardOdds.Percent(RewardOdds.AtLeastOne(store.Total,denominator))}",FontSize=13,Foreground=Brushes.White});
            odds.Children.Add(new TextBlock{Text=$"За один сундук · 1 из {denominator:N0}",FontSize=11,Foreground=MutedBrush,Margin=new Thickness(0,5,0,0)});text.Children.Add(odds);
            var check=new CheckBox{Content="Получено",Foreground=Brushes.White,Style=(Style)Resources["RewardCheck"],IsChecked=owned,FontSize=12,Margin=new Thickness(20,12,20,20)};
            void Update(bool yes){if(yes)store.State.FoundRewards.Add(key);else store.State.FoundRewards.Remove(key);store.Save();odds.Visibility=!yes&&denominator>0?Visibility.Visible:Visibility.Collapsed;UpdateRewardProgress();}
            card.Cursor=System.Windows.Input.Cursors.Hand;
            card.MouseLeftButtonUp+=(_,e)=>
            {
                var source=e.OriginalSource as DependencyObject;
                while(source!=null&&source!=card){if(source==check)return;source=VisualTreeHelper.GetParent(source);}
                check.IsChecked=check.IsChecked!=true;e.Handled=true;
            };
            check.Checked+=(_,_)=>Update(true);check.Unchecked+=(_,_)=>Update(false);Grid.SetRow(check,2);panel.Children.Add(check);card.Child=panel;RewardCards.Children.Add(card);
        }
        UpdateRewardProgress();
    }
    private void UpdateRewardProgress()
    {
        var items=selectedWeek.Items.Length>0?selectedWeek.Items.Append(RewardCatalog.Token).ToArray():[];
        int found=items.Count(r=>store.State.FoundRewards.Contains(store.State.ActiveProfile+"|"+r.Id));
        RewardProgress.Text=items.Length==0?"":$"Получено: {found} из {items.Length}";
    }
    private void AnimationSettingChanged(object sender,RoutedEventArgs e)
    {
        if(refreshing)return;store.State.Animations=AnimationToggle.IsChecked==true;store.Save();ApplyAnimations();StopButtonMotion();
    }
    private void ReminderSettingChanged(object sender,RoutedEventArgs e)
    {
        if(refreshing)return;store.State.SpiritReminder=ReminderToggle.IsChecked==true;
        if(!store.State.SpiritReminder)reminder.DismissForMatchStart();store.Save();
    }
    private void OpenRewardSource(object sender,RoutedEventArgs e)=>Process.Start(new ProcessStartInfo(RewardCatalog.Source){UseShellExecute=true});
    private void OpenMapSource(object sender,RoutedEventArgs e)=>Process.Start(new ProcessStartInfo("https://fortnite.gg/"){UseShellExecute=true});
    private void MapZoomIn(object sender,RoutedEventArgs e)=>MapViewer.ZoomAt(new Point(MapViewer.ActualWidth/2,MapViewer.ActualHeight/2),1.5);
    private void MapZoomOut(object sender,RoutedEventArgs e)=>MapViewer.ZoomAt(new Point(MapViewer.ActualWidth/2,MapViewer.ActualHeight/2),1/1.5);
    private void MapFit(object sender,RoutedEventArgs e)=>MapViewer.Fit();
    private void ShowAbout(object sender,RoutedEventArgs e){using var r=new StreamReader(StoragePaths.Asset("Notices.txt"));ShowText("Chest Tracker 6.4",r.ReadToEnd());}
    private void WindowsTimeChanged(object? sender,EventArgs e)=>Dispatcher.BeginInvoke(()=>{TimeZoneInfo.ClearCachedData();Refresh(true);RenderRewardWeek(selectedWeek);});
    private void WindowsPreferenceChanged(object sender,Microsoft.Win32.UserPreferenceChangedEventArgs e){if(e.Category is Microsoft.Win32.UserPreferenceCategory.Locale or Microsoft.Win32.UserPreferenceCategory.General)WindowsTimeChanged(sender,EventArgs.Empty);}
    private void BindButtonMotion(DependencyObject root)
    {
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)
        {
            var child=VisualTreeHelper.GetChild(root,i);
            if(child is Button b&&motionButtons.Add(b)){b.RenderTransformOrigin=new Point(.5,.5);b.RenderTransform=new ScaleTransform();b.MouseEnter+=(_,_)=>AnimateButton(b,1.025);b.MouseLeave+=(_,_)=>AnimateButton(b,1);}
            BindButtonMotion(child);
        }
    }
    private void AnimateButton(Button button,double target)
    {
        if(!store.State.Animations||WindowState==WindowState.Minimized||button.RenderTransform is not ScaleTransform scale)return;
        var a=new DoubleAnimation(target,TimeSpan.FromMilliseconds(140)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}};
        scale.BeginAnimation(ScaleTransform.ScaleXProperty,a);scale.BeginAnimation(ScaleTransform.ScaleYProperty,a);
    }
    private void StopButtonMotion(){foreach(var b in motionButtons){if(b.RenderTransform is not ScaleTransform s)continue;s.BeginAnimation(ScaleTransform.ScaleXProperty,null);s.BeginAnimation(ScaleTransform.ScaleYProperty,null);s.ScaleX=s.ScaleY=1;}}
}
