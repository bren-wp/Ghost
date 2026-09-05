using System.Windows;
using System.Windows.Media;

namespace GhostFTP.Design;

/// <summary>
/// Applies the approved workstation reference palette after the base Ghost theme.
/// The override is deliberately limited to immutable brush resources so it is safe
/// even after WPF has sealed control styles. It performs no I/O or network access.
/// </summary>
public static class GhostReferenceTheme
{
    public static void Apply(bool dark)
    {
        if (!dark)
            return;

        var resources = Application.Current.Resources;
        resources["Bg"] = Brush(GhostReferencePalette.Background);
        resources["Surface"] = Brush(GhostReferencePalette.Surface);
        resources["Surface2"] = Brush(GhostReferencePalette.Surface2);
        resources["Surface3"] = Brush(GhostReferencePalette.Surface3);
        resources["SurfaceHover"] = Brush(GhostReferencePalette.SurfaceHover);
        resources["Text"] = Brush(GhostReferencePalette.Text);
        resources["Muted"] = Brush(GhostReferencePalette.Muted);
        resources["Subtle"] = Brush(GhostReferencePalette.Subtle);
        resources["Border"] = Brush(GhostReferencePalette.Border);
        resources["BorderStrong"] = Brush(GhostReferencePalette.BorderStrong);
        resources["Accent"] = Brush(GhostReferencePalette.Accent);
        resources["AccentHover"] = Brush(GhostReferencePalette.AccentHover);
        resources["AccentPressed"] = Brush(GhostReferencePalette.AccentPressed);
        resources["AccentSoft"] = Brush(GhostReferencePalette.AccentSoft);
        resources["Success"] = Brush(GhostReferencePalette.Success);
        resources["SuccessSoft"] = Brush(GhostReferencePalette.SuccessSoft);
        resources["Danger"] = Brush(GhostReferencePalette.Danger);
        resources["DangerSoft"] = Brush(GhostReferencePalette.DangerSoft);
        resources["Warning"] = Brush(GhostReferencePalette.Warning);
        resources["WarningSoft"] = Brush(GhostReferencePalette.WarningSoft);
    }

    private static SolidColorBrush Brush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
