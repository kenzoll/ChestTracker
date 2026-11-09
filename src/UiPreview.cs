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
        foreach(var (page,button,name) in new[] { (CounterPage,CounterNav,"counter"),(HistoryPage,HistoryNav,"history"),(RewardsPage,RewardsNav,"rewards"),(MapPage,MapNav,"map"),(HealthPage,HealthNav,"settings"),(CounterPage,CounterNav,"update-notice") })
        {
            ShowPage(page,button);
            UpdateNotice.Visibility=name=="update-notice"?Visibility.Visible:Visibility.Collapsed;
            if(page==MapPage)MapViewer.EnsureLoaded();
            UpdateLayout();
            Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);
            UpdateLayout();
            File.AppendAllText(Path.Combine(directory,"layout.txt"),name+": "+string.Join(",",HistoryGrid.Columns.Select(c=>c.ActualWidth))+Environment.NewLine);
            var image=new RenderTargetBitmap((int)RootContent.ActualWidth,(int)RootContent.ActualHeight,96,96,PixelFormats.Pbgra32);
            var visual=new DrawingVisual();using(var dc=visual.RenderOpen()){dc.DrawRectangle(Background,null,new Rect(0,0,RootContent.ActualWidth,RootContent.ActualHeight));dc.DrawRectangle(new VisualBrush(RootContent){ViewboxUnits=BrushMappingMode.Absolute,Viewbox=new Rect(0,0,RootContent.ActualWidth,RootContent.ActualHeight)},null,new Rect(0,0,RootContent.ActualWidth,RootContent.ActualHeight));}
            image.Render(visual);
            var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(image));
            using var output=File.Create(Path.Combine(directory,name+".png"));png.Save(output);
        }
    }
    internal static int RenderPreview(string directory)
    {
        Directory.CreateDirectory(directory);
        var app=new Application();var store=new TrackerStore(Path.Combine(directory,"preview-data"));DemoData.Seed(store);store.State.Animations=false;
        var window=new MainWindow(store,true){Width=1400,Height=1000,Opacity=0,ShowActivated=false};
        window.Show();window.RenderPages(directory);
        var sample=new MatchEntry{Player="Пример игрока",TimeUtc=DateTime.UtcNow,Normal=3,Rare=1,Broken=2,File="Пример записи.replay",GameVersion="Пример",Events=[
            new(){Seconds=42,Category="normal",Counted=true},new(){Seconds=130,Category="rare",Counted=true,Destroyed=true},
            new(){Seconds=184,Category="normal",Counted=true,Destroyed=true},new(){Seconds=257,Category="normal",Counted=true}]};
        var details=new MatchDetailsWindow(sample,window,false){Opacity=0,ShowActivated=false};details.Show();details.UpdateLayout();
        details.Dispatcher.Invoke(()=>{},DispatcherPriority.ApplicationIdle);details.UpdateLayout();
        var area=(FrameworkElement)details.Content;var picture=new RenderTargetBitmap((int)details.ActualWidth,(int)details.ActualHeight,96,96,PixelFormats.Pbgra32);picture.Render(area);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(picture));using(var file=File.Create(Path.Combine(directory,"match-details.png")))encoder.Save(file);
        details.Close();window.Close();app.Shutdown();return 0;
    }
}

