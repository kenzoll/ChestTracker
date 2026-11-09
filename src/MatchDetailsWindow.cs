using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ChestTracker;

public sealed class MatchDetailsWindow : Window
{
    internal sealed record EventRow(string Time,string Kind,string Action);
    internal IReadOnlyList<EventRow> EventRows { get; }
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
    private static readonly Brush Muted=new SolidColorBrush(Color.FromRgb(174,174,184));
    private static TextBlock Label(string text,double size=13,Brush? color=null)=>new(){Text=text,FontSize=size,Foreground=color??Brushes.White,TextWrapping=TextWrapping.Wrap};
    public MatchDetailsWindow(MatchEntry match,Window owner,bool animations)
    {
        Owner=owner;Resources=owner.Resources;Title="Подробности матча";Width=880;Height=760;MinWidth=720;MinHeight=580;
        WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=new SolidColorBrush(Color.FromRgb(8,8,10));Foreground=Brushes.White;FontFamily=new FontFamily("Segoe UI");
        SourceInitialized+=(_,_)=>{int dark=1;DwmSetWindowAttribute(new System.Windows.Interop.WindowInteropHelper(this).Handle,20,ref dark,sizeof(int));};
        EventRows=match.Events.Where(e=>e.Counted&&(e.Category=="normal"||e.Category=="rare")).OrderBy(e=>e.Seconds)
            .Select(e=>new EventRow(EventTime(e.Seconds),e.Category=="rare"?"Редкий":"Обычный пиратский",e.Destroyed?"Разрушен":"Открыт")).ToArray();
        var root=new Grid{Margin=new Thickness(28)};
        root.RowDefinitions.Add(new(){Height=GridLength.Auto});root.RowDefinitions.Add(new(){Height=GridLength.Auto});
        root.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});root.RowDefinitions.Add(new(){Height=GridLength.Auto});
        var heading=new StackPanel{Margin=new Thickness(0,0,0,22)};
        var title=Label("Подробности матча",28);title.FontWeight=FontWeights.SemiBold;heading.Children.Add(title);
        var subtitle=Label(match.DateLabel+(string.IsNullOrWhiteSpace(match.Player)?"":"  ·  "+match.Player),13,Muted);subtitle.Margin=new Thickness(0,8,0,0);heading.Children.Add(subtitle);root.Children.Add(heading);
        var summaries=new Grid{Margin=new Thickness(0,0,0,22)};
        for(int i=0;i<3;i++)summaries.ColumnDefinitions.Add(new());
        var values=new[]{("ВСЕГО СУНДУКОВ",match.Total),("ОБЫЧНЫЕ",match.Normal),("РЕДКИЕ",match.Rare)};
        for(int i=0;i<values.Length;i++)
        {
            var panel=new StackPanel();panel.Children.Add(Label(values[i].Item1,10,Muted));
            var number=Label(values[i].Item2.ToString(),38);number.FontWeight=FontWeights.SemiBold;number.Margin=new Thickness(0,9,0,0);panel.Children.Add(number);
            var card=new Border{Child=panel,Padding=new Thickness(20,16,20,16),Margin=new Thickness(0,0,i<2?12:0,0),CornerRadius=new CornerRadius(13),Background=new SolidColorBrush(Color.FromRgb(18,18,23)),BorderBrush=new SolidColorBrush(Color.FromRgb(i==0?(byte)107:(byte)44,35,43)),BorderThickness=new Thickness(1)};
            Grid.SetColumn(card,i);summaries.Children.Add(card);
        }
        Grid.SetRow(summaries,1);root.Children.Add(summaries);
        var body=new Grid();body.RowDefinitions.Add(new(){Height=GridLength.Auto});body.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});
        var caption=new Grid{Margin=new Thickness(0,0,0,12)};caption.ColumnDefinitions.Add(new());caption.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        var eventsTitle=Label("Сундуки в матче",17);eventsTitle.FontWeight=FontWeights.SemiBold;caption.Children.Add(eventsTitle);
        var breakdown=Label($"Открыто: {Math.Max(0,match.Total-match.Broken)}   ·   Разрушено: {match.Broken}",12,Muted);breakdown.VerticalAlignment=VerticalAlignment.Center;Grid.SetColumn(breakdown,1);caption.Children.Add(breakdown);body.Children.Add(caption);
        if(EventRows.Count>0)
        {
            var table=new DataGrid{ItemsSource=EventRows,EnableRowVirtualization=true,EnableColumnVirtualization=true,CanUserReorderColumns=false};
            table.Columns.Add(new DataGridTextColumn{Header="Время в матче",Binding=new System.Windows.Data.Binding("Time"),Width=new DataGridLength(1,DataGridLengthUnitType.Star)});
            table.Columns.Add(new DataGridTextColumn{Header="Сундук",Binding=new System.Windows.Data.Binding("Kind"),Width=new DataGridLength(2,DataGridLengthUnitType.Star)});
            table.Columns.Add(new DataGridTextColumn{Header="Действие",Binding=new System.Windows.Data.Binding("Action"),Width=new DataGridLength(1,DataGridLengthUnitType.Star)});
            var border=new Border{Child=table,Padding=new Thickness(6),CornerRadius=new CornerRadius(13),Background=new SolidColorBrush(Color.FromRgb(16,16,16)),BorderBrush=new SolidColorBrush(Color.FromRgb(43,43,49)),BorderThickness=new Thickness(1)};
            Grid.SetRow(border,1);body.Children.Add(border);
        }
        else
        {
            var empty=new StackPanel{VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(24)};
            var emptyTitle=Label("Список событий недоступен",18);emptyTitle.FontWeight=FontWeights.SemiBold;empty.Children.Add(emptyTitle);
            var explanation=Label("В этой записи сохранены только итоги матча.",13,Muted);explanation.Margin=new Thickness(0,10,0,0);empty.Children.Add(explanation);
            var border=new Border{Child=empty,CornerRadius=new CornerRadius(13),Background=new SolidColorBrush(Color.FromRgb(16,16,20))};Grid.SetRow(border,1);body.Children.Add(border);
        }
        Grid.SetRow(body,2);root.Children.Add(body);
        var footer=new Grid{Margin=new Thickness(0,18,0,0)};footer.ColumnDefinitions.Add(new());footer.ColumnDefinitions.Add(new(){Width=GridLength.Auto});
        var details=new StackPanel{Margin=new Thickness(0,12,20,0)};
        foreach(var (name,value) in new[]{("Файл",match.File),("Режим",match.Mode),("Версия игры",match.GameVersion)})
            if(!string.IsNullOrWhiteSpace(value))details.Children.Add(Label(name+": "+value,11,Muted));
        var more=new Expander{Header="Информация о записи",Foreground=Muted,Content=details,VerticalAlignment=VerticalAlignment.Center,Visibility=details.Children.Count>0?Visibility.Visible:Visibility.Collapsed};footer.Children.Add(more);
        var close=new Button{Content="Закрыть",MinWidth=110,Style=(Style)Resources["Primary"],VerticalAlignment=VerticalAlignment.Bottom,IsCancel=true};close.Click+=(_,_)=>Close();Grid.SetColumn(close,1);footer.Children.Add(close);Grid.SetRow(footer,3);root.Children.Add(footer);Content=root;
        if(animations)Loaded+=(_,_)=>root.BeginAnimation(OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(180)));
    }
    internal static string EventTime(double seconds)
    {
        var t=TimeSpan.FromSeconds(double.IsFinite(seconds)?Math.Clamp(seconds,0,604800):0);
        return t.TotalHours>=1?$"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}":$"{t.Minutes:00}:{t.Seconds:00}";
    }
}
