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
        button.Click += (_, _) => action();
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
                ShowOperationError("The operation failed.", ex);
            }
        };
        return button;
    }

    private static GridView CreateFileGrid(bool local)
    {
        var grid = new GridView { AllowsColumnReorder = true };
        grid.Columns.Add(Column("Name", "Name", local ? 230 : 205));
        grid.Columns.Add(Column("Type", "Type", 70));
        grid.Columns.Add(Column("Size", "SizeText", 78));
        grid.Columns.Add(Column("Modified", "ModifiedText", 124));
        return grid;
    }

    private static GridView CreateQueueGrid()
    {
        var grid = new GridView { AllowsColumnReorder = true };
        grid.Columns.Add(Column("Item", "DisplayName", 160));
        grid.Columns.Add(Column("Direction", "Direction", 76));
        grid.Columns.Add(Column("State", "State", 72));
        grid.Columns.Add(Column("Progress", "ProgressText", 70));
        grid.Columns.Add(Column("Speed", "SpeedText", 82));
        grid.Columns.Add(Column("Source", "Source", 220));
        grid.Columns.Add(Column("Destination", "Destination", 220));
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
        if (_queueList.View is not GridView grid || grid.Columns.Count != 7 || _queueList.ActualWidth <= 0) return;

        var available = Math.Max(760, _queueList.ActualWidth - 22);
        var item = Math.Clamp(available * 0.16, 130, 200);
        var direction = 76d;
        var state = 72d;
        var progress = 70d;
        var speed = 82d;
        var remaining = Math.Max(260, available - item - direction - state - progress - speed);
        var source = remaining / 2;

        grid.Columns[0].Width = item;
        grid.Columns[1].Width = direction;
        grid.Columns[2].Width = state;
        grid.Columns[3].Width = progress;
        grid.Columns[4].Width = speed;
        grid.Columns[5].Width = source;
        grid.Columns[6].Width = source;
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
        MessageBox.Show(this, message + "\n\n" + ex.Message, "GhostFTP", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void UpdatePaneSummaries()
    {
        _localSummary.Text = SummaryText(_localItems.Count, _localList.SelectedItems.Count);
        _remoteSummary.Text = IsConnected
            ? SummaryText(_remoteItems.Count, _remoteList.SelectedItems.Count)
            : "Not connected";
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
        _remotePath = "/";
        try
        {
            await _session!.ChangeDirectoryAsync(_remotePath);
            _remotePath = await _session.GetWorkingDirectoryAsync();
        }
        catch
        {
            _remotePath = "/";
        }
        await RefreshRemoteAsync();
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
