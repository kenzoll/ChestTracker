using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ChestTracker;

public sealed record ChestPin(string Id, Point World, bool Rare, string Layer="chests");
public sealed record MapLayer(string Id,string Label,Color Color,bool DefaultOn);
public sealed record MapPlace(string Name,Point Position);
public sealed partial class ChestMap : FrameworkElement
{
    private BitmapImage? image;
    private List<ChestPin>? pins;
    private readonly List<Cluster> hits = [];
    private bool dragging;
    private readonly HashSet<string> enabled=AvailableLayers.Where(layer=>layer.DefaultOn).Select(layer=>layer.Id).ToHashSet();
    private readonly List<MapPlace> places=[];
    public static readonly MapLayer[] AvailableLayers=[
        new("chests","Обычные",Color.FromRgb(239,194,85),true),
        new("buried_chests","Закопанные",Color.FromRgb(224,158,93),true),
        new("rare_chests","Редкие",Color.FromRgb(114,183,255),true),
        new("sprite_chests","С духами",Color.FromRgb(198,154,255),false),
        new("ammo_boxes","Патроны",Color.FromRgb(151,203,159),false),
        new("key_vaults","Хранилища",Color.FromRgb(227,234,244),false),
        new("loot_cannons","Пушки",Color.FromRgb(251,152,160),false),
        new("launchpads","Батуты",Color.FromRgb(119,220,214),false)
    ];
    public bool IsLayerEnabled(string id)=>enabled.Contains(id);
    public void SetLayer(string id,bool show){if(!AvailableLayers.Any(l=>l.Id==id))throw new ArgumentException("Unknown layer");if(show)enabled.Add(id);else enabled.Remove(id);InvalidateVisual();}
    private Point pressed, last;
    private Vector pan;
    private double zoom = 1;
    private ChestPin? selected;
    private sealed record Cluster(Point Screen, List<ChestPin> Pins);
    public event Action<string>? SelectionChanged;
    public double Zoom => zoom;
    public int NormalCount => pins?.Count(p=>p.Layer=="chests") ?? 0;
    public int RareCount => pins?.Count(p=>p.Layer=="rare_chests") ?? 0;
    public int FilteredCount => pins?.Count(p=>enabled.Contains(p.Layer)) ?? 0;
    public bool IsImageLoaded => image!=null;
    private double Scale => Math.Max(0.01, Math.Min(ActualWidth / 194, ActualHeight / 194)) * zoom;
    private static readonly Brush Red = FrozenBrush(238,67,84), Dark = FrozenBrush(16,16,20);
    private static Brush FrozenBrush(byte r,byte g,byte b) { var brush = new SolidColorBrush(Color.FromRgb(r,g,b)); brush.Freeze(); return brush; }
    public ChestMap()
    {
        ClipToBounds = true; Focusable = true; Cursor = Cursors.Hand;
        SizeChanged += (_,_) => InvalidateVisual();
    }
    public void EnsureLoaded()
    {
        if (pins == null)
        {
            using var data=StoragePaths.Asset("layers.json");using var doc=JsonDocument.Parse(data);
            if(doc.RootElement.GetProperty("season").GetInt32()!=43)throw new InvalidDataException("Карта другого сезона");pins=[];
            foreach(var layer in AvailableLayers)
            {
                int index=0;
                foreach(var pair in doc.RootElement.GetProperty("layers").GetProperty(layer.Id).EnumerateArray())
                {
                    double lat=pair[0].GetDouble(),lon=pair[1].GetDouble();
                    if(lat>0||lat< -256||lon<0||lon>256)throw new InvalidDataException("Invalid map point");
                    pins.Add(new(layer.Id+"-"+index++,new Point(lon,-lat),layer.Id=="rare_chests",layer.Id));
                }
            }
            foreach(var label in doc.RootElement.GetProperty("labels").EnumerateArray())
                places.Add(new(label.GetProperty("name").GetString()!,new Point(label.GetProperty("lon").GetDouble(),-label.GetProperty("lat").GetDouble())));
        }

        if(image == null)
        {
            using var stream = StoragePaths.Asset("map.jpg");
            image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = 2048; image.StreamSource = stream; image.EndInit(); image.Freeze();
        }
        InvalidateVisual();
    }
    public void ReleaseImage() { image=null; InvalidateVisual(); }
    public void Filters(bool showNormal,bool showRare){enabled.Clear();if(showNormal)enabled.Add("chests");if(showRare)enabled.Add("rare_chests");InvalidateVisual();}
    public void Fit() { zoom=1; pan=new(); selected=null; InvalidateVisual(); }
    public Point ToScreen(Point p) => new((p.X-141)*Scale+ActualWidth/2+pan.X,(p.Y-141)*Scale+ActualHeight/2+pan.Y);
    public Point ToWorld(Point p) => new((p.X-ActualWidth/2-pan.X)/Scale+141,(p.Y-ActualHeight/2-pan.Y)/Scale+141);
    public void ZoomAt(Point cursor, double factor)
    {
        var world=ToWorld(cursor); zoom=Math.Clamp(zoom*factor,0.8,28);
        pan=new Vector(cursor.X-ActualWidth/2-(world.X-141)*Scale,cursor.Y-ActualHeight/2-(world.Y-141)*Scale);
        InvalidateVisual();
    }
    protected override void OnRender(DrawingContext drawing)
    {
        base.OnRender(drawing);
        drawing.DrawRectangle(Dark,null,new Rect(RenderSize));
        hits.Clear();
        if(image == null || pins == null) return;
        var origin=ToScreen(new Point(0,0));
        drawing.DrawImage(image,new Rect(origin,new Size(256*Scale,256*Scale)));
        if (routePlan != null)
        {
            DrawRoute(drawing);
            return;
        }
        foreach(var place in places)
        {
            var at=ToScreen(place.Position);if(at.X<0||at.X>ActualWidth||at.Y<0||at.Y>ActualHeight)continue;
            var label=new FormattedText(ChestRoutes.Label(place.Name),System.Globalization.CultureInfo.InvariantCulture,FlowDirection.LeftToRight,
                new Typeface("Segoe UI Semibold"),zoom>2?11:9,Brushes.White,VisualTreeHelper.GetDpi(this).PixelsPerDip);
            var pos=new Point(at.X-label.Width/2,at.Y-19);
            drawing.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(165,10,13,19)),null,new Rect(pos.X-4,pos.Y-2,label.Width+8,label.Height+4),3,3);drawing.DrawText(label,pos);
        }
        var bins=new Dictionary<(int,int),List<ChestPin>>();
        double cell=zoom<2?52:zoom<4?36:25;
        foreach(var pin in pins)
        {
            if(!enabled.Contains(pin.Layer))continue;
            var p=ToScreen(pin.World);
            if(p.X < -30 || p.X>ActualWidth+30 || p.Y < -30 || p.Y>ActualHeight+30) continue;
            var key=((int)Math.Floor(p.X/cell),(int)Math.Floor(p.Y/cell));
            if(!bins.TryGetValue(key,out var list)) bins[key]=list=[];
            list.Add(pin);
        }
        var groups=bins.Values.ToList();
        // Merge close neighboring bins so overview badges do not cover each other.
        if(zoom<4)
        {
            for(int i=0;i<groups.Count;i++)
                for(int j=i+1;j<groups.Count;j++)
                {
                    Point Center(List<ChestPin> g)=>new(g.Average(p=>ToScreen(p.World).X),g.Average(p=>ToScreen(p.World).Y));
                    if((Center(groups[i])-Center(groups[j])).Length>=29)continue;
                    groups[i].AddRange(groups[j]);groups.RemoveAt(j--);
                }
        }
        foreach(var group in groups)
        {
            var p=new Point(group.Average(g=>ToScreen(g.World).X),group.Average(g=>ToScreen(g.World).Y));
            var cluster=new Cluster(p,group); hits.Add(cluster);
            string category=group.GroupBy(g=>g.Layer).OrderByDescending(g=>g.Count()).First().Key;
            var color=AvailableLayers.First(l=>l.Id==category).Color;var accent=new SolidColorBrush(color);accent.Freeze();
            double radius=group.Count>1?12:5;
            drawing.DrawEllipse(Dark,new Pen(Brushes.Black,2.5),p,radius+1,radius+1);
            if(group.Count>1)drawing.DrawEllipse(Dark,new Pen(accent,2),p,radius,radius);
            else
            {
                var box=new Rect(p.X-5.5,p.Y-4.5,11,9);drawing.DrawRoundedRectangle(Dark,new Pen(accent,1.8),box,2,2);
                drawing.DrawLine(new Pen(accent,1.4),new Point(p.X-5,p.Y),new Point(p.X+5,p.Y));
                drawing.DrawRectangle(accent,null,new Rect(p.X-1,p.Y-1,2,3));
            }
            if(group.Count>1)
            {
                var text = new FormattedText(group.Count.ToString(),System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,new Typeface(new FontFamily("Segoe UI"),FontStyles.Normal,FontWeights.SemiBold,FontStretches.Normal),10,Brushes.White,VisualTreeHelper.GetDpi(this).PixelsPerDip);
                drawing.DrawText(text,new Point(p.X-text.Width/2,p.Y-text.Height/2));
            }
        }
        if(selected != null)
        {
            var p=ToScreen(selected.World); drawing.DrawEllipse(null,new Pen(Brushes.White,2),p,13,13);
        }
    }
    protected override void OnMouseWheel(MouseWheelEventArgs e) { ZoomAt(e.GetPosition(this),e.Delta>0?1.24:1/1.24); e.Handled=true; }
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e); Focus(); pressed=last=e.GetPosition(this); dragging=false; CaptureMouse(); e.Handled=true;
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e); var p=e.GetPosition(this);
        if(IsMouseCaptured && e.LeftButton==MouseButtonState.Pressed)
        {
            if((p-pressed).Length>3) dragging=true;
            if(dragging) { pan+=p-last; InvalidateVisual(); } last=p; e.Handled=true;
        }
        else
        {
            var h=hits.LastOrDefault(h=>(h.Screen-p).Length<17);
            ToolTip=h==null?null:string.Join(" · ",h.Pins.GroupBy(g=>g.Layer).Select(g=>AvailableLayers.First(l=>l.Id==g.Key).Label+": "+g.Count()));
        }
    }
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e); var p=e.GetPosition(this); ReleaseMouseCapture();
        if (!dragging && routePlan != null)
        {
            ClickRouteStop(p);
        }
        else if(!dragging)
        {
            var h=hits.LastOrDefault(h=>(h.Screen-p).Length<18);
            if(h!=null && h.Pins.Count>1) ZoomAt(h.Screen,1.9);
            else if(h!=null)
            {
                selected=h.Pins[0]; SelectionChanged?.Invoke(selected.Rare?"Синий редкий сундук · точка возможного появления":"Обычный пиратский сундук · точка возможного появления"); InvalidateVisual();
            }
        }
        dragging=false; e.Handled=true;
    }
}
