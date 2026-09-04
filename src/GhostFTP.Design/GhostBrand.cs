using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace GhostFTP.Design;

public static class GhostBrand
{
    public const string DisplayName = "Ghost FTP";
    public const string ProductName = "GhostFTP";
    public const string Website = "https://ghostftp.com";
    public const string Repository = "https://github.com/bren-wp/Ghost";
    public const string PrivacyTagline = "Private FTP / FTPS workspace for Windows";

    private static readonly Lazy<ImageSource> Icon = new(CreateIconSource, LazyThreadSafetyMode.ExecutionAndPublication);

    public static ImageSource IconSource => Icon.Value;

    private static ImageSource CreateIconSource()
    {
        const double size = 256;
        var drawing = new DrawingGroup();

        var background = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops =
            {
                new GradientStop(Color.FromRgb(8, 26, 67), 0),
                new GradientStop(Color.FromRgb(22, 52, 145), 0.5),
                new GradientStop(Color.FromRgb(74, 42, 169), 1)
            }
        };
        drawing.Children.Add(new GeometryDrawing(background, new Pen(new SolidColorBrush(Color.FromRgb(69, 203, 255)), 5),
            new RectangleGeometry(new Rect(8, 8, 240, 240), 52, 52)));

        var folderBrush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops =
            {
                new GradientStop(Color.FromRgb(49, 220, 255), 0),
                new GradientStop(Color.FromRgb(67, 106, 255), 0.55),
                new GradientStop(Color.FromRgb(139, 74, 255), 1)
            }
        };

        var folderTab = new StreamGeometry();
        using (var ctx = folderTab.Open())
        {
            ctx.BeginFigure(new Point(46, 62), true, true);
            ctx.LineTo(new Point(108, 62), true, false);
            ctx.LineTo(new Point(132, 84), true, false);
            ctx.LineTo(new Point(210, 84), true, false);
            ctx.LineTo(new Point(210, 112), true, false);
            ctx.LineTo(new Point(46, 112), true, false);
        }
        folderTab.Freeze();
        drawing.Children.Add(new GeometryDrawing(folderBrush, null, folderTab));
        drawing.Children.Add(new GeometryDrawing(folderBrush, new Pen(new SolidColorBrush(Color.FromArgb(115, 255, 255, 255)), 2),
            new RectangleGeometry(new Rect(36, 92, 184, 120), 26, 26)));

        var typeface = new Typeface(new FontFamily("Segoe UI Variable Display, Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        var glyph = new FormattedText(
            "G",
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            typeface,
            112,
            Brushes.White,
            1.0);
        var glyphGeometry = glyph.BuildGeometry(new Point(49, 91));
        drawing.Children.Add(new GeometryDrawing(Brushes.White, null, glyphGeometry));

        var cyan = new SolidColorBrush(Color.FromRgb(70, 226, 255));
        var violet = new SolidColorBrush(Color.FromRgb(184, 104, 255));
        drawing.Children.Add(new GeometryDrawing(cyan, null, ArrowGeometry(181, 112, up: true)));
        drawing.Children.Add(new GeometryDrawing(violet, null, ArrowGeometry(181, 176, up: false)));

        drawing.ClipGeometry = new RectangleGeometry(new Rect(0, 0, size, size), 52, 52);
        drawing.Freeze();
        var image = new DrawingImage(drawing);
        image.Freeze();
        return image;
    }

    private static Geometry ArrowGeometry(double centerX, double centerY, bool up)
    {
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        if (up)
        {
            ctx.BeginFigure(new Point(centerX, centerY - 28), true, true);
            ctx.LineTo(new Point(centerX - 20, centerY - 6), true, false);
            ctx.LineTo(new Point(centerX - 8, centerY - 6), true, false);
            ctx.LineTo(new Point(centerX - 8, centerY + 22), true, false);
            ctx.LineTo(new Point(centerX + 8, centerY + 22), true, false);
            ctx.LineTo(new Point(centerX + 8, centerY - 6), true, false);
            ctx.LineTo(new Point(centerX + 20, centerY - 6), true, false);
        }
        else
        {
            ctx.BeginFigure(new Point(centerX, centerY + 28), true, true);
            ctx.LineTo(new Point(centerX - 20, centerY + 6), true, false);
            ctx.LineTo(new Point(centerX - 8, centerY + 6), true, false);
            ctx.LineTo(new Point(centerX - 8, centerY - 22), true, false);
            ctx.LineTo(new Point(centerX + 8, centerY - 22), true, false);
            ctx.LineTo(new Point(centerX + 8, centerY + 6), true, false);
            ctx.LineTo(new Point(centerX + 20, centerY + 6), true, false);
        }
        geometry.Freeze();
        return geometry;
    }
}
