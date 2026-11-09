using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
        toast?.Close();toast=null;
        if(!string.IsNullOrEmpty(store.State.PendingReminder)){store.State.LastReminder=store.State.PendingReminder;store.State.PendingReminder="";store.Save();}
    }
    private void ShowToast()
    {
        toast?.Close();
        var work = SystemParameters.WorkArea;
        toast = new Window { Width = 430, Height = 190, WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false, ShowActivated = false, Topmost = true, Background = Brushes.Transparent, AllowsTransparency = true,
            Left = work.Right - 450, Top = work.Bottom - 210 };
        var panel = new StackPanel { Margin = new Thickness(22) };
        panel.Children.Add(new TextBlock { Text = "МАТЧ ЗАВЕРШЁН", Foreground = new SolidColorBrush(Color.FromRgb(180,180,180)), FontSize = 10 });
        panel.Children.Add(new TextBlock { Text = "Не забудьте поставить духа", Foreground = Brushes.White, FontSize = 21, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,12,0,9) });
        var done = new Button { Content = "Готово", Padding = new Thickness(18,7,18,7), Background = new SolidColorBrush(Color.FromRgb(203,41,57)), Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Left, Cursor = System.Windows.Input.Cursors.Hand };
        done.Click += (_, _) => toast?.Close();
        panel.Children.Add(done);
        toast.Content = new Border { CornerRadius = new CornerRadius(14), BorderBrush = new SolidColorBrush(Color.FromRgb(151,42,56)), BorderThickness = new Thickness(1), Background = new SolidColorBrush(Color.FromRgb(15,15,15)), Child = panel };
        toast.Show();

    }
    public void Stop() { stopped = true; toast?.Close(); }
}

internal static class Native
{
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
}