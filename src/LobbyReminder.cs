using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ChestTracker;

public sealed class LobbyReminder(TrackerStore store)
{
    private bool busy;
    private bool stopped;
    private Window? toast;
    public string Status { get; private set; } = "Напомнить поставить духа после завершения записи матча.";
    public Task CheckAsync()
    {
        if(stopped||busy||!store.State.SpiritReminder||string.IsNullOrEmpty(store.State.PendingReminder)||store.State.LastReminder==store.State.PendingReminder)return Task.CompletedTask;
        if(store.State.PendingReminderNotBeforeUtc==default){store.State.PendingReminderNotBeforeUtc=DateTime.UtcNow.AddSeconds(ReminderPolicy.DelaySeconds);store.Save();}
        if(!ReminderPolicy.Ready(store.State,DateTime.UtcNow))return Task.CompletedTask;
        busy=true;
        try
        {
            Native.GetWindowThreadProcessId(Native.GetForegroundWindow(),out uint pid);
            using var process=Process.GetProcessById((int)pid);
            if(!process.ProcessName.StartsWith("FortniteClient",StringComparison.OrdinalIgnoreCase))return Task.CompletedTask;
            ShowToast();
            store.State.LastReminder=store.State.PendingReminder;store.State.PendingReminder="";
            store.Log("Показано напоминание о духе после матча");store.Save();Status="Напоминание показано";
        }
        catch(Exception e){Status=e.Message;}
        finally{busy=false;}
        return Task.CompletedTask;
    }
    public void DismissForMatchStart()
    {
        toast?.Close();toast=null;store.State.PendingReminderNotBeforeUtc=default;
        if(!string.IsNullOrEmpty(store.State.PendingReminder)){store.State.LastReminder=store.State.PendingReminder;store.State.PendingReminder="";store.Save();}
    }
    private void ShowToast()
    {
        toast?.Close();
        toast = CreateToast();
        toast.Closed += (_, _) => toast = null;
        toast.Show();
    }

    internal static Window CreateToast()
    {
        var work = SystemParameters.WorkArea;
        var window = new Window { Icon=Brand.Logo, Width = 420, SizeToContent = SizeToContent.Height, WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false, ShowActivated = false, Topmost = true, Background = Brushes.Transparent, AllowsTransparency = true,
            Left = work.Right - 440, Top = work.Bottom - 156 };
        var panel = new StackPanel { Margin = new Thickness(22) };
        var heading=new StackPanel{Orientation=Orientation.Horizontal};heading.Children.Add(new Image{Source=Brand.Logo,Width=26,Height=26,Margin=new Thickness(0,0,10,0)});
        heading.Children.Add(new TextBlock { VerticalAlignment=VerticalAlignment.Center, Text = "МАТЧ ЗАВЕРШЁН", Foreground = new SolidColorBrush(Color.FromRgb(180,180,180)), FontSize = 10 });panel.Children.Add(heading);
        panel.Children.Add(new TextBlock { Text = "Не забудьте поставить духа", Foreground = Brushes.White, FontSize = 21, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,12,0,9) });
        var remaining = new ScaleTransform(1, 1);
        var fill = new Border
        {
            Height = 4, Background = new SolidColorBrush(Color.FromRgb(238, 67, 84)),
            CornerRadius = new CornerRadius(2), RenderTransform = remaining
        };
        panel.Children.Add(new Border
        {
            Margin = new Thickness(0, 8, 0, 0), Background = new SolidColorBrush(Color.FromRgb(65, 34, 39)),
            CornerRadius = new CornerRadius(2), Child = fill
        });
        window.Content = new Border { CornerRadius = new CornerRadius(14), BorderBrush = new SolidColorBrush(Color.FromRgb(151,42,56)), BorderThickness = new Thickness(1), Background = new SolidColorBrush(Color.FromRgb(15,15,15)), Child = panel };

        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(50) };
        var elapsed = new Stopwatch();
        timer.Tick += (_, _) =>
        {
            double fraction = 1 - elapsed.Elapsed.TotalSeconds / ReminderPolicy.VisibleSeconds;
            if (fraction <= 0) window.Close();
            else remaining.ScaleX = fraction;
        };
        window.Loaded += (_, _) => { elapsed.Start(); timer.Start(); };
        window.Closed += (_, _) => timer.Stop();
        return window;
    }
    public void Stop() { stopped = true; toast?.Close(); }
}

internal static class Native
{
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
}