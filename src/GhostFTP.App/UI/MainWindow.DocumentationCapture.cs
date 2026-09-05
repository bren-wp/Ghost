using GhostFTP.Core.Models;
using System.Windows;
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
        SizeToContent = SizeToContent.WidthAndHeight;
        Left = 20;
        Top = 20;

        if (Content is FrameworkElement referenceRoot)
        {
            referenceRoot.Width = ReferenceCaptureWidth;
            referenceRoot.Height = ReferenceCaptureHeight;
        }

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

        var clientPath = Path.Combine(_captureDirectory, "ghostftp-client.png");
        if (Content is FrameworkElement captureRoot)
            CaptureReferenceRootToPng(captureRoot, clientPath);
        else
            throw new InvalidOperationException("Ghost FTP documentation capture requires a framework root element.");

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
        element.Width = ReferenceCaptureWidth;
        element.Height = ReferenceCaptureHeight;
        element.Measure(new Size(ReferenceCaptureWidth, ReferenceCaptureHeight));
        element.Arrange(new Rect(0, 0, ReferenceCaptureWidth, ReferenceCaptureHeight));
        element.UpdateLayout();

        var bitmap = new RenderTargetBitmap(
            ReferenceCaptureWidth,
            ReferenceCaptureHeight,
            96d,
            96d,
            PixelFormats.Pbgra32);
        bitmap.Render(element);
        SavePng(bitmap, path);
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
