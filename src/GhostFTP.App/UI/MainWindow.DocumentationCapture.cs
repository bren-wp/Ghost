using GhostFTP.Core.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace GhostFTP.UI;

public sealed partial class MainWindow
{
    private const int ReferenceCaptureWidth = 1914;
    private const int ReferenceCaptureHeight = 907;

    private async Task RunDocumentationCaptureAsync()
    {
        if (_captureDirectory is null)
            return;

        Directory.CreateDirectory(_captureDirectory);
        WindowState = WindowState.Normal;
        Left = 20;
        Top = 20;

        var demo = _profiles.FirstOrDefault(x => x.IsDemo);
        if (demo is not null)
        {
            _profilesList.SelectedItem = demo;
            if (_referenceSitesList is not null)
                _referenceSitesList.SelectedItem = demo;
            ProfileSelected();
            await ConnectAsync();
        }

        PrepareDeterministicDocumentationState();

        await Dispatcher.InvokeAsync(() =>
        {
            UpdateLayout();
            ResizeAllColumns();
        }, DispatcherPriority.ApplicationIdle);

        // Capture owned dialogs while the real MainWindow visual tree is still attached.
        // This avoids reparenting a large reference-sized tree back into the smaller virtual
        // desktop used by GitHub-hosted runners.
        if (_profileStore is not null)
        {
            var manager = new SiteManagerDialog(
                this,
                _profiles,
                profile => profile.IsDemo ? string.Empty : _profileStore.GetPassword(profile));
            manager.Show();
            await Dispatcher.InvokeAsync(manager.UpdateLayout, DispatcherPriority.ApplicationIdle);
            var managerPath = Path.Combine(_captureDirectory, "ghostftp-site-manager.png");
            CaptureElementToPng(manager, managerPath);
            manager.Close();
        }

        var clientPath = Path.Combine(_captureDirectory, "ghostftp-client.png");
        if (Content is FrameworkElement captureRoot)
        {
            // Hosted Windows runners expose a desktop smaller than the canonical 1914x907
            // workstation viewport. A live Window is therefore constrained to the runner's
            // work area. Detach the exact compiled production visual tree, allow WPF to finish
            // the detach, then place that same tree in a non-window staging Grid whose layout
            // size is the canonical viewport. This performs a real WPF measure/arrange pass at
            // 1914x907; it does not scale a smaller screenshot and does not create a mock UI.
            Content = null;
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            CaptureReferenceRootToPng(captureRoot, clientPath);
        }
        else
        {
            throw new InvalidOperationException("Ghost FTP documentation capture requires a framework root element.");
        }

        if (_queue is not null)
        {
            _queue.JobUpdated -= QueueJobUpdated;
            await _queue.DisposeAsync();
            _queue = null;
        }

        if (_session is not null)
            await DisconnectCoreAsync();

        _allowClose = true;
        Application.Current.Shutdown(0);
    }

    private void PrepareDeterministicDocumentationState()
    {
        // Canonical repository screenshots must not change merely because a CI runner started
        // at a different wall-clock time. These rows describe the real Demo state that was
        // established above, but use stable documentation timestamps and contain no secrets.
        _connectionLog.Clear();
        _connectionLog.Add("09:41:00  [INFO]  Ghost FTP documentation workspace ready.");
        _connectionLog.Add("09:41:01  [DEMO]  Built-in Ghost FTP Demo session opened locally.");
        _connectionLog.Add("09:41:01  [INFO]  No network connection is used by Demo mode.");
        _connectionLog.Add($"09:41:02  [LIST]  Directory listing completed: {_remoteAll.Count} item(s) in {_remotePath}.");
        _connectionLog.Add("09:41:02  [OK]  Local/Remote workstation ready for documentation capture.");
        _connectionLogList.SelectedIndex = -1;
        if (_connectionLog.Count > 0)
            _connectionLogList.ScrollIntoView(_connectionLog[^1]);
    }

    private static void CaptureReferenceRootToPng(FrameworkElement element, string path)
    {
        if (VisualTreeHelper.GetParent(element) is not null)
            throw new InvalidOperationException("Reference visual must be detached before canonical capture.");

        // Remove only viewport constraints inherited from the previous Window layout. The
        // production child controls, styles, bindings, commands and data remain untouched.
        element.Width = double.NaN;
        element.Height = double.NaN;
        element.MinWidth = 0;
        element.MinHeight = 0;
        element.MaxWidth = double.PositiveInfinity;
        element.MaxHeight = double.PositiveInfinity;
        element.HorizontalAlignment = HorizontalAlignment.Stretch;
        element.VerticalAlignment = VerticalAlignment.Stretch;

        var stagingRoot = new Grid
        {
            Width = ReferenceCaptureWidth,
            Height = ReferenceCaptureHeight,
            ClipToBounds = true
        };
        stagingRoot.Children.Add(element);

        stagingRoot.Measure(new Size(ReferenceCaptureWidth, ReferenceCaptureHeight));
        stagingRoot.Arrange(new Rect(0, 0, ReferenceCaptureWidth, ReferenceCaptureHeight));
        stagingRoot.UpdateLayout();

        if (Math.Abs(stagingRoot.ActualWidth - ReferenceCaptureWidth) > 0.5
            || Math.Abs(stagingRoot.ActualHeight - ReferenceCaptureHeight) > 0.5
            || Math.Abs(element.ActualWidth - ReferenceCaptureWidth) > 0.5
            || Math.Abs(element.ActualHeight - ReferenceCaptureHeight) > 0.5)
        {
            throw new InvalidOperationException(
                $"Reference visual did not arrange to {ReferenceCaptureWidth}x{ReferenceCaptureHeight}; " +
                $"host {stagingRoot.ActualWidth:0.#}x{stagingRoot.ActualHeight:0.#}, " +
                $"content {element.ActualWidth:0.#}x{element.ActualHeight:0.#}.");
        }

        var bitmap = new RenderTargetBitmap(
            ReferenceCaptureWidth,
            ReferenceCaptureHeight,
            96d,
            96d,
            PixelFormats.Pbgra32);
        bitmap.Render(stagingRoot);
        SavePng(bitmap, path);

        stagingRoot.Children.Remove(element);
    }

    private static void CaptureElementToPng(FrameworkElement element, string path)
    {
        element.UpdateLayout();
        var width = Math.Max(1, (int)Math.Ceiling(element.ActualWidth));
        var height = Math.Max(1, (int)Math.Ceiling(element.ActualHeight));
        var bitmap = new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Pbgra32);
        bitmap.Render(element);
        SavePng(bitmap, path);
    }

    private static void SavePng(BitmapSource bitmap, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        encoder.Save(stream);
    }
}
