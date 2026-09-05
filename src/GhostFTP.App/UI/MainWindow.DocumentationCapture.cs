using GhostFTP.Core.Models;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace GhostFTP.UI;

public sealed partial class MainWindow
{
    private async Task RunDocumentationCaptureAsync()
    {
        if (_captureDirectory is null)
            return;

        Directory.CreateDirectory(_captureDirectory);
        WindowState = WindowState.Normal;
        Width = 1600;
        Height = 960;
        Left = 20;
        Top = 20;

        var demo = _profiles.FirstOrDefault(x => x.IsDemo);
        if (demo is not null)
        {
            _profilesList.SelectedItem = demo;
            ProfileSelected();
            await ConnectAsync();
        }

        await Dispatcher.InvokeAsync(() =>
        {
            UpdateLayout();
            ResizeAllColumns();
        }, DispatcherPriority.ApplicationIdle);

        var clientPath = Path.Combine(_captureDirectory, "ghostftp-client.png");
        CaptureElementToPng(this, clientPath);
        AppendConnectionLog($"Authentic client screenshot captured to {clientPath}.", "DOCS");

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

    private static void CaptureElementToPng(FrameworkElement element, string path)
    {
        element.UpdateLayout();
        var dpi = VisualTreeHelper.GetDpi(element);
        var width = Math.Max(1, (int)Math.Ceiling(element.ActualWidth * dpi.DpiScaleX));
        var height = Math.Max(1, (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY));

        var bitmap = new RenderTargetBitmap(
            width,
            height,
            96d * dpi.DpiScaleX,
            96d * dpi.DpiScaleY,
            PixelFormats.Pbgra32);
        bitmap.Render(element);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        encoder.Save(stream);
    }
}
