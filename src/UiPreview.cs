using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ChestTracker;

public partial class MainWindow
{
    internal void RenderPages(string directory)
    {
        foreach(var (page,button,name) in new[] { (CounterPage,CounterNav,"counter"),(HistoryPage,HistoryNav,"history"),(RewardsPage,RewardsNav,"rewards"),(MapPage,MapNav,"map"),(RoutesPage,RoutesNav,"routes"),(RoutesPage,RoutesNav,"generated-route"),(RoutesPage,RoutesNav,"focused-route"),(RoutesPage,RoutesNav,"saved-route"),(HealthPage,HealthNav,"settings"),(CounterPage,CounterNav,"update-notice") })
        {
            ShowPage(page,button);
            UpdateNotice.Visibility=name=="update-notice"?Visibility.Visible:Visibility.Collapsed;
            UpdateNoticeTitle.Text = "Доступна версия 6.5";
            UpdateNoticeDetail.Text = "Готова к установке";
            if(page==MapPage)MapViewer.EnsureLoaded();
            if(page==RoutesPage)
            {
                LoadRoutes();
                if(name=="generated-route") GenerateRoute(RouteMap, new RoutedEventArgs());
                if(name=="focused-route") RouteStopList.SelectedIndex = 1;
                if(name=="saved-route" && displayedRoute is RoutePlan plan)
                    WriteSavedRoute(new("preview-route", "Фарм от пристани", plan.Areas.Select(area => area.ToString()).ToArray()));
            }
            UpdateLayout();
            Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
            UpdateLayout();
            File.AppendAllText(Path.Combine(directory,"layout.txt"),name+": "+string.Join(",",HistoryGrid.Columns.Select(c=>c.ActualWidth))+Environment.NewLine);
            var margin = RootContent.Margin;
            var area = new Rect(0, 0, RootContent.ActualWidth + margin.Left + margin.Right, RootContent.ActualHeight + margin.Top + margin.Bottom);
            var image = new RenderTargetBitmap((int)area.Width, (int)area.Height, 96, 96, PixelFormats.Pbgra32);
            var visual = new DrawingVisual();
            using (var drawing = visual.RenderOpen()) drawing.DrawRectangle(Background, null, area);
            image.Render(visual);
            image.Render(RootContent);
            var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(image));
            using var output=File.Create(Path.Combine(directory,name+".png"));png.Save(output);
        }
    }
    internal static int RenderPreview(string directory)
    {
        Directory.CreateDirectory(directory);
        var app=new Application();var store=new TrackerStore(Path.Combine(directory,"preview-data"));DemoData.Seed(store);store.AddManualChests(12);store.State.Animations=false;
        var window=new MainWindow(store,true){Width=1400,Height=1000,Opacity=0,ShowActivated=false};
        window.Show();window.RenderPages(directory);
        var sample=new MatchEntry{Player="Пример игрока",TimeUtc=DateTime.UtcNow,Normal=3,Rare=1,Broken=2,File="Пример записи.replay",GameVersion="Пример",Events=[
            new(){Seconds=42,Category="normal",Counted=true},new(){Seconds=130,Category="rare",Counted=true,Destroyed=true},
            new(){Seconds=184,Category="normal",Counted=true,Destroyed=true},new(){Seconds=257,Category="normal",Counted=true}]};
        var details=new MatchDetailsWindow(sample,window,false){Opacity=0,ShowActivated=false};details.Show();details.UpdateLayout();
        details.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);details.UpdateLayout();
        var area=(FrameworkElement)details.Content;var picture=new RenderTargetBitmap((int)details.ActualWidth,(int)details.ActualHeight,96,96,PixelFormats.Pbgra32);picture.Render(area);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(picture));using(var file=File.Create(Path.Combine(directory,"match-details.png")))encoder.Save(file);
        details.Close();
        var route = window.displayedRoute ?? throw new InvalidOperationException("Preview route was not loaded.");
        var mapWindow = new RouteMapWindow(window, route) { Opacity = 0, ShowActivated = false };
        mapWindow.Show();
        mapWindow.UpdateLayout();
        var viewport = mapWindow.Map;
        if (Math.Abs(viewport.ActualWidth - viewport.ActualHeight) > 1 ||
            !new Rect(viewport.RenderSize).Contains(viewport.ToScreen(new Point(39, 45))) ||
            !new Rect(viewport.RenderSize).Contains(viewport.ToScreen(new Point(230, 237))))
            throw new InvalidOperationException("The route window does not fit the full island.");
        var mapImage = new RenderTargetBitmap((int)viewport.ActualWidth, (int)viewport.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        mapImage.Render((FrameworkElement)mapWindow.Content);
        var mapPng = new PngBitmapEncoder();
        mapPng.Frames.Add(BitmapFrame.Create(mapImage));
        using (var file = File.Create(Path.Combine(directory, "route-island.png"))) mapPng.Save(file);
        mapWindow.Close();
        bool reminderClosed = RenderReminder(directory);
        window.Close();app.Shutdown();return reminderClosed ? 0 : 1;
    }

    private static bool RenderReminder(string directory)
    {
        var toast = LobbyReminder.CreateToast();
        toast.Opacity = 0;
        var frame = new DispatcherFrame();
        bool closed = false;
        toast.Closed += (_, _) => { closed = true; frame.Continue = false; };
        var watchdog = new DispatcherTimer { Interval = TimeSpan.FromSeconds(ReminderPolicy.VisibleSeconds + 2) };
        watchdog.Tick += (_, _) => frame.Continue = false;
        toast.Show();
        toast.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)toast.ActualWidth, (int)toast.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render((FrameworkElement)toast.Content);
        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(bitmap));
        using (var file = File.Create(Path.Combine(directory, "reminder.png"))) png.Save(file);
        watchdog.Start();
        Dispatcher.PushFrame(frame);
        watchdog.Stop();
        if (!closed) toast.Close();
        return closed;
    }
}

