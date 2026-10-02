using System.Drawing;
using System.Drawing.Drawing2D;
using Microsoft.Win32;

namespace BluetoothScheduler.App.UI;

public static class Theme
{
    private static bool _isDark;

    public static bool IsDark => _isDark;

    // Colores Claro (Material Light)
    public static readonly Color LightBg = Color.FromArgb(255, 255, 255);
    public static readonly Color LightSurface = Color.FromArgb(241, 243, 244);
    public static readonly Color LightBorder = Color.FromArgb(218, 220, 224);
    public static readonly Color LightTextPrimary = Color.FromArgb(32, 33, 36);
    public static readonly Color LightTextSecondary = Color.FromArgb(95, 99, 104);
    public static readonly Color LightTextDisabled = Color.FromArgb(154, 160, 166);
    public static readonly Color LightAccent = Color.FromArgb(26, 115, 232); // #1A73E8
    public static readonly Color LightAccentHover = Color.FromArgb(23, 101, 204); // #1765CC
    public static readonly Color LightAccentTonal = Color.FromArgb(232, 240, 254); // #E8F0FE
    public static readonly Color LightError = Color.FromArgb(217, 48, 37); // #D93025

    // Colores Oscuro (Material Dark)
    public static readonly Color DarkBg = Color.FromArgb(32, 33, 36);
    public static readonly Color DarkSurface = Color.FromArgb(48, 49, 52);
    public static readonly Color DarkBorder = Color.FromArgb(95, 99, 104);
    public static readonly Color DarkTextPrimary = Color.FromArgb(232, 234, 237);
    public static readonly Color DarkTextSecondary = Color.FromArgb(154, 160, 166);
    public static readonly Color DarkTextDisabled = Color.FromArgb(95, 99, 104);
    public static readonly Color DarkAccent = Color.FromArgb(138, 180, 248); // #8AB4F8
    public static readonly Color DarkAccentHover = Color.FromArgb(102, 157, 246);
    public static readonly Color DarkAccentTonal = Color.FromArgb(41, 62, 94);
    public static readonly Color DarkError = Color.FromArgb(242, 139, 130);

    // Propiedades dinámicas según tema
    public static Color Background => _isDark ? DarkBg : LightBg;
    public static Color Surface => _isDark ? DarkSurface : LightSurface;
    public static Color Border => _isDark ? DarkBorder : LightBorder;
    public static Color TextPrimary => _isDark ? DarkTextPrimary : LightTextPrimary;
    public static Color TextSecondary => _isDark ? DarkTextSecondary : LightTextSecondary;
    public static Color TextDisabled => _isDark ? DarkTextDisabled : LightTextDisabled;
    public static Color Accent => _isDark ? DarkAccent : LightAccent;
    public static Color AccentHover => _isDark ? DarkAccentHover : LightAccentHover;
    public static Color AccentTonal => _isDark ? DarkAccentTonal : LightAccentTonal;
    public static Color Error => _isDark ? DarkError : LightError;

    // Fuentes cacheadas
    public static readonly Font TitleFont;
    public static readonly Font SubtitleFont;
    public static readonly Font SectionHeaderFont;
    public static readonly Font BodyFont;
    public static readonly Font BodyBoldFont;
    public static readonly Font CaptionFont;
    public static readonly Font TimeDisplayFont;

    // Pens y Brushes cacheados estáticos comunes
    public static Pen BorderPen { get; private set; } = null!;
    public static Pen AccentPen { get; private set; } = null!;
    public static Pen ErrorPen { get; private set; } = null!;
    public static SolidBrush BackgroundBrush { get; private set; } = null!;
    public static SolidBrush SurfaceBrush { get; private set; } = null!;
    public static SolidBrush TextPrimaryBrush { get; private set; } = null!;
    public static SolidBrush TextSecondaryBrush { get; private set; } = null!;
    public static SolidBrush AccentBrush { get; private set; } = null!;
    public static SolidBrush AccentTonalBrush { get; private set; } = null!;
    public static SolidBrush WhiteBrush { get; }

    static Theme()
    {
        string fontFamily = GetSystemFontFamily();
        
        TitleFont = new Font(fontFamily, 18f, FontStyle.Bold, GraphicsUnit.Point);
        SubtitleFont = new Font(fontFamily, 10.5f, FontStyle.Regular, GraphicsUnit.Point);
        SectionHeaderFont = new Font(fontFamily, 8.5f, FontStyle.Bold, GraphicsUnit.Point);
        BodyFont = new Font(fontFamily, 9.5f, FontStyle.Regular, GraphicsUnit.Point);
        BodyBoldFont = new Font(fontFamily, 9.5f, FontStyle.Bold, GraphicsUnit.Point);
        CaptionFont = new Font(fontFamily, 8.5f, FontStyle.Regular, GraphicsUnit.Point);
        TimeDisplayFont = new Font(fontFamily, 14f, FontStyle.Regular, GraphicsUnit.Point);

        WhiteBrush = new SolidBrush(Color.White);

        DetectSystemTheme();
        RecreateGdiObjects();
    }

    private static string GetSystemFontFamily()
    {
        try
        {
            using var test = new FontFamily("Segoe UI Variable Text");
            return "Segoe UI Variable Text";
        }
        catch
        {
            return "Segoe UI";
        }
    }

    public static void DetectSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key != null)
            {
                var val = key.GetValue("AppsUseLightTheme");
                if (val is int lightVal)
                {
                    _isDark = (lightVal == 0);
                    return;
                }
            }
        }
        catch
        {
        }
        _isDark = false;
    }

    public static void SetDark(bool dark)
    {
        if (_isDark != dark)
        {
            _isDark = dark;
            RecreateGdiObjects();
        }
    }

    private static void RecreateGdiObjects()
    {
        BorderPen?.Dispose();
        AccentPen?.Dispose();
        ErrorPen?.Dispose();
        BackgroundBrush?.Dispose();
        SurfaceBrush?.Dispose();
        TextPrimaryBrush?.Dispose();
        TextSecondaryBrush?.Dispose();
        AccentBrush?.Dispose();
        AccentTonalBrush?.Dispose();

        BorderPen = new Pen(Border, 1f);
        AccentPen = new Pen(Accent, 1.5f);
        ErrorPen = new Pen(Error, 1.2f);
        BackgroundBrush = new SolidBrush(Background);
        SurfaceBrush = new SolidBrush(Surface);
        TextPrimaryBrush = new SolidBrush(TextPrimary);
        TextSecondaryBrush = new SolidBrush(TextSecondary);
        AccentBrush = new SolidBrush(Accent);
        AccentTonalBrush = new SolidBrush(AccentTonal);
    }

    public static GraphicsPath CreateRoundedRectangle(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        float diameter = radius * 2;
        var size = new SizeF(diameter, diameter);
        var arc = new RectangleF(rect.Location, size);

        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
        return path;
    }
}
