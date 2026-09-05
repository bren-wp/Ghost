using GhostFTP.Design;
using System.Windows;
using System.Windows.Controls;

namespace GhostFTP.UI;

internal enum GhostMessageKind
{
    Information,
    Warning,
    Error
}

internal sealed class GhostMessageDialog : GhostDialog
{
    private GhostMessageDialog(
        Window? owner,
        string title,
        string message,
        string? details,
        GhostMessageKind kind,
        string primaryText,
        bool showCancel,
        bool dangerPrimary)
        : base(owner, title, 560, details is null ? 330 : 410)
    {
        ResizeMode = ResizeMode.NoResize;

        var body = new StackPanel();
        var header = new StackPanel { Orientation = Orientation.Horizontal };
        var badge = GhostTheme.Badge(Symbol(kind), ToneBackground(kind), "Text");
        badge.Margin = new Thickness(0, 0, 10, 0);
        header.Children.Add(badge);
        header.Children.Add(GhostTheme.Text(title, 22, weight: FontWeights.SemiBold));
        body.Children.Add(header);

        var messageText = GhostTheme.Text(message, 12.5);
        messageText.Margin = new Thickness(0, 16, 0, 0);
        body.Children.Add(messageText);

        if (!string.IsNullOrWhiteSpace(details))
        {
            var detailPanel = new StackPanel();
            detailPanel.Children.Add(GhostTheme.Text(GhostLocalization.T("Details"), 11.5, weight: FontWeights.SemiBold));
            var detailText = GhostTheme.Text(details.Trim(), 11, muted: true);
            detailText.Margin = new Thickness(0, 5, 0, 0);
            detailPanel.Children.Add(detailText);
            var detailSurface = GhostTheme.Surface(detailPanel, new Thickness(12), 10);
            detailSurface.Margin = new Thickness(0, 16, 0, 0);
            body.Children.Add(detailSurface);
        }

        var primary = GhostTheme.Button(primaryText, primary: !dangerPrimary, danger: dangerPrimary);
        primary.MinWidth = 96;
        primary.Click += (_, _) => DialogResult = true;

        Button? cancel = null;
        if (showCancel)
        {
            cancel = GhostTheme.Button(GhostLocalization.T("Cancel"));
            cancel.MinWidth = 88;
            cancel.Click += (_, _) => DialogResult = false;
        }

        body.Children.Add(Footer(primary, cancel));
        Content = Shell(body);
        Padding = new Thickness(16);
        Loaded += (_, _) => primary.Focus();
    }

    public static void Error(Window? owner, string message, string? details = null, string? title = null)
    {
        _ = new GhostMessageDialog(
            owner,
            title ?? GhostLocalization.T("OperationFailed"),
            message,
            details,
            GhostMessageKind.Error,
            GhostLocalization.T("Close"),
            false,
            false).ShowDialog();
    }

    public static void Information(Window? owner, string title, string message)
    {
        _ = new GhostMessageDialog(
            owner,
            title,
            message,
            null,
            GhostMessageKind.Information,
            "OK",
            false,
            false).ShowDialog();
    }

    public static bool Confirm(
        Window? owner,
        string title,
        string message,
        string? confirmText = null,
        bool danger = false,
        bool warning = false)
    {
        var kind = danger ? GhostMessageKind.Error : warning ? GhostMessageKind.Warning : GhostMessageKind.Information;
        return new GhostMessageDialog(
            owner,
            title,
            message,
            null,
            kind,
            confirmText ?? GhostLocalization.T("Continue"),
            true,
            danger).ShowDialog() == true;
    }

    private static string Symbol(GhostMessageKind kind) => kind switch
    {
        GhostMessageKind.Information => "i",
        GhostMessageKind.Warning => "!",
        GhostMessageKind.Error => "!",
        _ => "i"
    };

    private static string ToneBackground(GhostMessageKind kind) => kind switch
    {
        GhostMessageKind.Information => "AccentSoft",
        GhostMessageKind.Warning => "WarningSoft",
        GhostMessageKind.Error => "DangerSoft",
        _ => "Surface2"
    };
}
