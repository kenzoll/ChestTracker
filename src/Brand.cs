using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ChestTracker;

public static class Brand
{
    public const string ChestPath="M3,10 L3,20 L21,20 L21,10 M3,10 C3,2 21,2 21,10 Z M8,5 L8,20 M16,5 L16,20 M10,12 L14,12 L14,16 L10,16 Z";
    public static DrawingImage Logo { get; }=Create();
    private static DrawingImage Create()
    {
        var group=new DrawingGroup();
        using(var drawing=group.Open())
        {
            drawing.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(36,17,20)),new Pen(new SolidColorBrush(Color.FromRgb(116,37,48)),1.2),new Rect(.6,.6,62.8,62.8),17,17);
            drawing.PushTransform(new TranslateTransform(13,13));drawing.PushTransform(new ScaleTransform(38d/24,38d/24));
            drawing.DrawGeometry(null,new Pen(Brushes.White,1.5){LineJoin=PenLineJoin.Round},Geometry.Parse(ChestPath));drawing.Pop();drawing.Pop();
        }
        var image=new DrawingImage(group);image.Freeze();return image;
    }
    public static void WriteAssets(string directory)
    {
        Directory.CreateDirectory(directory);
        var visual=new DrawingVisual();using(var dc=visual.RenderOpen())dc.DrawImage(Logo,new Rect(0,0,256,256));
        var bitmap=new RenderTargetBitmap(256,256,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var png=new MemoryStream();encoder.Save(png);byte[] pixels=png.ToArray();
        File.WriteAllBytes(Path.Combine(directory,"logo.png"),pixels);
        using(var writer=new BinaryWriter(File.Create(Path.Combine(directory,"app.ico"))))
        {writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)1);writer.Write(new byte[]{0,0,0,0});writer.Write((ushort)1);writer.Write((ushort)32);writer.Write(pixels.Length);writer.Write(22);writer.Write(pixels);}
        string svg=$"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"256\" height=\"256\" viewBox=\"0 0 64 64\"><rect x=\".6\" y=\".6\" width=\"62.8\" height=\"62.8\" rx=\"17\" fill=\"#241114\" stroke=\"#742530\" stroke-width=\"1.2\"/><g transform=\"translate(13 13) scale({(38d/24).ToString(System.Globalization.CultureInfo.InvariantCulture)})\"><path d=\"{ChestPath}\" fill=\"none\" stroke=\"white\" stroke-width=\"1.5\" stroke-linejoin=\"round\"/></g></svg>";
        File.WriteAllText(Path.Combine(directory,"logo.svg"),svg);
    }
}
