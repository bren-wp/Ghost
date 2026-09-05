using GhostFTP.Core.Protocol;
using GhostFTP.Design;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GhostFTP.UI;

public sealed partial class MainWindow
{
    private Button ToolButton(string text, Action action, bool primary = false, bool danger = false)
    {
        var button = GhostTheme.Button(text, primary: primary, danger: danger);
        button.Padding = new Thickness(10, 5, 10, 5);
        button.MinHeight = 30;
        button.Margin = new Thickness(0, 0, 6, 5);
        button.Click += (_, _) =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                ShowOperationError(L("OperationFailed"), ex);
            }
        };
        return button;
    }

    private Button ToolButton(string text, Func<Task> action, bool primary = false, bool danger = false)
    {
        var button = GhostTheme.Button(text, primary: primary, danger: danger);
        button.Padding = new Thickness(10, 5, 10, 5);
        button.MinHeight = 30;
        button.Margin = new Thickness(0, 0, 6, 5);
        button.Click += async (_, _) =>
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                ShowOperationError(L("OperationFailed"), ex);
            }
        };
        return button;
    }

    private static GridView CreateFileGrid(bool local)
    {
        var grid = new GridView { AllowsColumnReorder = true };
        grid.Columns.Add(Column(L("Name"), "Name", local ? 230 : 205));
        grid.Columns.Add(Column(L("Type"), "Type", 70));
        grid.Columns.Add(Column(L("Size"), "SizeText", 78));
        grid.Columns.Add(Column(L("Modified"), "ModifiedText", 124));
        return grid;
    }

    private static GridView CreateQueueGrid()
    {
        var grid = new GridView { AllowsColumnReorder = true };
        grid.Columns.Add(Column(L("Item"), "DisplayName", 150));
        grid.Columns.Add(Column(L("Direction"), "Direction", 76));
        grid.Columns.Add(Column(L("State"), "State", 76));
        grid.Columns.Add(Column(L("Progress"), "ProgressText", 70));
        grid.Columns.Add(Column("Retry", "RetryText", 48));
        grid.Columns.Add(Column(L("Speed"), "SpeedText", 82));
        grid.Columns.Add(Column(L("Source"), "Source", 210));
        grid.Columns.Add(Column(L("Destination"), "Destination", 210));
        return grid;
    }

    private static GridViewColumn Column(string title, string binding, double width) => new()
    {
        Header = title,
        Width = width,
        DisplayMemberBinding = new System.Windows.Data.Binding(binding)
    };

    private void ResizeFileColumns(ListView list)
    {
        if (list.View is not GridView grid || grid.Columns.Count != 4 || list.ActualWidth <= 0) return;

        var available = Math.Max(360, list.ActualWidth - 22);
        var type = 68d;
        var size = 76d;
        var modified = 120d;
        var name = Math.Max(120d, available - type - size - modified);

        grid.Columns[0].Width = name;
        grid.Columns[1].Width = type;
        grid.Columns[2].Width = size;
        grid.Columns[3].Width = modified;
    }

    private void ResizeQueueColumns()
    {
        if (_queueList.View is not GridView grid || grid.Columns.Count != 8 || _queueList.ActualWidth <= 0) return;

        var available = Math.Max(820, _queueList.ActualWidth - 22);
        var item = Math.Clamp(available * 0.15, 125, 190);
        var direction = 76d;
        var state = 76d;
        var progress = 70d;
        var retry = 48d;
        var speed = 82d;
        var remaining = Math.Max(260, available - item - direction - state - progress - retry - speed);
        var source = remaining / 2;

        grid.Columns[0].Width = item;
        grid.Columns[1].Width = direction;
        grid.Columns[2].Width = state;
        grid.Columns[3].Width = progress;
        grid.Columns[4].Width = retry;
        grid.Columns[5].Width = speed;
        grid.Columns[6].Width = source;
        grid.Columns[7].Width = source;
    }

    private static ContextMenu CreateContextMenu(params (string text, RoutedEventHandler handler)[] items)
    {
        var menu = new ContextMenu();
        foreach (var item in items)
        {
            var menuItem = new MenuItem { Header = item.text };
            menuItem.Click += item.handler;
            menu.Items.Add(menuItem);
        }
        return menu;
    }

    private void ShowOperationError(string message, Exception ex)
    {
        GhostMessageDialog.Error(this, message, ex.Message);
    }

    private void UpdatePaneSummaries()
    {
        _localSummary.Text = SummaryText(_localItems.Count, _localList.SelectedItems.Count);
        _remoteSummary.Text = IsConnected
            ? SummaryText(_remoteItems.Count, _remoteList.SelectedItems.Count)
            : L("NotConnected");
    }

    private static string SummaryText(int count, int selected)
    {
        var items = count == 1 ? "1 item" : $"{count} items";
        return selected > 0 ? $"{items} · {selected} selected" : items;
    }

    private void NavigateLocalHome()
    {
        NavigateLocalQuick(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    }

    private void NavigateLocalQuick(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;
        _localPath = path;
        RefreshLocal();
    }

    private async Task NavigateRemoteHomeAsync()
    {
        if (!IsConnected) return;
        await ChangeRemoteDirectoryAsync("/");
    }

    private void RevealLocalSelected()
    {
        if (_localList.SelectedItem is not LocalItem item) return;
        try
        {
            if (item.IsDirectory)
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{item.FullPath}\"") { UseShellExecute = true });
            }
            else
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{item.FullPath}\"") { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            ShowOperationError("Could not open File Explorer.", ex);
        }
    }

    private void CopyLocalPath()
    {
        if (_localList.SelectedItem is LocalItem item) CopyText(item.FullPath);
    }

    private void CopyRemotePath()
    {
        if (_remoteList.SelectedItem is RemoteItem item) CopyText(item.FullPath);
    }

    private void CopyText(string text)
    {
        try
        {
            Clipboard.SetText(text);
        }
        catch (Exception ex)
        {
            ShowOperationError("Could not copy the path to the clipboard.", ex);
        }
    }

    private async Task HandleShortcutAsync(KeyEventArgs e)
    {
        var typing = Keyboard.FocusedElement is TextBox or PasswordBox or ComboBox;
        var remoteActive = _remoteList.IsKeyboardFocusWithin || _remotePathBox.IsKeyboardFocusWithin || _remoteFilter.IsKeyboardFocusWithin;

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
        {
            (remoteActive ? _remoteFilter : _localFilter).Focus();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.L)
        {
            var path = remoteActive ? _remotePathBox : _localPathBox;
            path.Focus();
            path.SelectAll();
            e.Handled = true;
            return;
        }

        if (typing) return;

        if (e.Key == Key.F5)
        {
            if (remoteActive && IsConnected) await RefreshRemoteAsync();
            else RefreshLocal();
            e.Handled = true;
        }
        else if (e.Key == Key.F2)
        {
            if (remoteActive) await RenameRemoteSelectedAsync();
            else RenameLocalSelected();
            e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            if (remoteActive) await DeleteRemoteSelectedAsync();
            else DeleteLocalSelected();
            e.Handled = true;
        }
    }

    private static string FormatBytes(long value)
    {
        double number = Math.Max(0, value);
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var index = 0;
        while (number >= 1024 && index < units.Length - 1)
        {
            number /= 1024;
            index++;
        }
        return $"{number:0.#} {units[index]}";
    }
}
