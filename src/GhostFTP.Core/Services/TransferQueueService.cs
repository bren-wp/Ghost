using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Channels;
using GhostFTP.Core.Models;
using GhostFTP.Core.Protocol;

namespace GhostFTP.Core.Services;

public sealed class TransferQueueService : IAsyncDisposable
{
    private sealed record Queued(TransferJob Job, CancellationTokenSource Cancellation);

    private readonly Func<CancellationToken, Task<(IFtpSession Session, bool DisposeAfter)>> _sessionFactory;
    private const int MaxQueuedTransfers = 4096;
    private readonly Channel<Queued> _channel = Channel.CreateBounded<Queued>(new BoundedChannelOptions(MaxQueuedTransfers)
    {
        SingleReader = true,
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.Wait
    });
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Dictionary<Guid, CancellationTokenSource> _cancellations = new();
    private readonly object _sync = new();
    private readonly Task _worker;
    private readonly SynchronizationContext? _uiContext;
    private readonly int _maxAutomaticRetries;

    public ObservableCollection<TransferJob> Jobs { get; } = [];
    public event EventHandler<TransferJob>? JobUpdated;

    public TransferQueueService(
        Func<CancellationToken, Task<(IFtpSession Session, bool DisposeAfter)>> sessionFactory,
        SynchronizationContext? uiContext = null,
        int maxAutomaticRetries = 2)
    {
        _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        _uiContext = uiContext;
        _maxAutomaticRetries = Math.Clamp(maxAutomaticRetries, 0, 5);
        _worker = Task.Run(WorkerAsync);
    }

    public TransferJob EnqueueUpload(string source, string destination, bool isDirectory)
    {
        var job = new TransferJob { Direction = TransferDirection.Upload, Source = source, Destination = destination, IsDirectory = isDirectory };
        Enqueue(job);
        return job;
    }

    public TransferJob EnqueueDownload(string source, string destination, bool isDirectory, long? totalBytes = null)
    {
        var job = new TransferJob { Direction = TransferDirection.Download, Source = source, Destination = destination, IsDirectory = isDirectory, TotalBytes = totalBytes };
        Enqueue(job);
        return job;
    }

    public void Cancel(Guid jobId)
    {
        lock (_sync)
        {
            if (_cancellations.TryGetValue(jobId, out var cts))
                cts.Cancel();
        }
    }

    public void ClearFinished()
    {
        var finished = Jobs.Where(x => x.State is TransferState.Completed or TransferState.Cancelled or TransferState.Failed).ToArray();
        foreach (var job in finished)
            Jobs.Remove(job);
    }

    private void Enqueue(TransferJob job)
    {
        if (_shutdown.IsCancellationRequested)
        {
            job.Error = "Transfer queue is shutting down.";
            job.State = TransferState.Failed;
            Jobs.Add(job);
            JobUpdated?.Invoke(this, job);
            return;
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token);
        lock (_sync) _cancellations[job.Id] = cts;
        Jobs.Add(job);
        if (!_channel.Writer.TryWrite(new Queued(job, cts)))
        {
            lock (_sync) _cancellations.Remove(job.Id);
            cts.Dispose();
            job.Error = $"Transfer queue is full. Maximum queued transfers: {MaxQueuedTransfers:N0}.";
            job.State = TransferState.Failed;
            JobUpdated?.Invoke(this, job);
            return;
        }
        JobUpdated?.Invoke(this, job);
    }

    private async Task WorkerAsync()
    {
        try
        {
            await foreach (var queued in _channel.Reader.ReadAllAsync(_shutdown.Token).ConfigureAwait(false))
                await ProcessQueuedAsync(queued).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
        {
        }
    }

    private async Task ProcessQueuedAsync(Queued queued)
    {
        var job = queued.Job;
        var ct = queued.Cancellation.Token;

        try
        {
            for (var attempt = 0; ; attempt++)
            {
                IFtpSession? transferSession = null;
                var disposeAfter = false;
                try
                {
                    ct.ThrowIfCancellationRequested();
                    var lease = await _sessionFactory(ct).ConfigureAwait(false);
                    transferSession = lease.Session;
                    disposeAfter = lease.DisposeAfter;
                    if (!transferSession.IsConnected)
                        throw new InvalidOperationException("Transfer session is not connected.");

                    Ui(() =>
                    {
                        job.State = TransferState.Running;
                        if (attempt == 0) job.Error = null;
                    }, job);

                    await ExecuteTransferAsync(job, transferSession, ct).ConfigureAwait(false);
                    Ui(() =>
                    {
                        job.Progress = 100;
                        job.Error = null;
                        job.State = TransferState.Completed;
                    }, job);
                    return;
                }
                catch (OperationCanceledException)
                {
                    Ui(() => job.State = TransferState.Cancelled, job);
                    return;
                }
                catch (Exception ex) when (attempt < _maxAutomaticRetries && IsTransient(ex))
                {
                    var retryNumber = attempt + 1;
                    Ui(() =>
                    {
                        job.RetryCount = retryNumber;
                        job.State = TransferState.Retrying;
                        job.Error = $"Transient transfer failure. Retry {retryNumber} of {_maxAutomaticRetries}: {ex.Message}";
                        job.SpeedBytesPerSecond = 0;
                    }, job);

                    await DisposeLeaseAsync(transferSession, disposeAfter).ConfigureAwait(false);
                    transferSession = null;
                    disposeAfter = false;

                    var delay = TimeSpan.FromMilliseconds(Math.Min(5000, 700 * Math.Pow(2, attempt)));
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                    continue;
                }
                catch (Exception ex)
                {
                    Ui(() =>
                    {
                        job.Error = ex.Message;
                        job.State = TransferState.Failed;
                        job.SpeedBytesPerSecond = 0;
                    }, job);
                    return;
                }
                finally
                {
                    await DisposeLeaseAsync(transferSession, disposeAfter).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            Ui(() => { }, job);
            lock (_sync) _cancellations.Remove(job.Id);
            queued.Cancellation.Dispose();
        }
    }

    private async Task ExecuteTransferAsync(TransferJob job, IFtpSession transferSession, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        long lastBytes = 0;
        var lastTime = TimeSpan.Zero;
        var progress = new Progress<(long transferred, long? total)>(p =>
        {
            var total = p.total ?? job.TotalBytes;
            var elapsed = stopwatch.Elapsed;
            var deltaSeconds = (elapsed - lastTime).TotalSeconds;
            var speed = deltaSeconds >= 0.5 ? Math.Max(0, p.transferred - lastBytes) / deltaSeconds : -1;
            if (deltaSeconds >= 0.5)
            {
                lastBytes = p.transferred;
                lastTime = elapsed;
            }
            Ui(() =>
            {
                job.BytesTransferred = p.transferred;
                if (total is > 0) job.Progress = p.transferred * 100d / total.Value;
                if (speed >= 0) job.SpeedBytesPerSecond = speed;
            }, job);
        });

        if (job.Direction == TransferDirection.Upload)
        {
            if (job.IsDirectory) await transferSession.UploadDirectoryAsync(job.Source, job.Destination, progress, ct).ConfigureAwait(false);
            else await transferSession.UploadFileAsync(job.Source, job.Destination, progress, ct).ConfigureAwait(false);
        }
        else
        {
            if (job.IsDirectory) await transferSession.DownloadDirectoryAsync(job.Source, job.Destination, progress, ct).ConfigureAwait(false);
            else await transferSession.DownloadFileAsync(job.Source, job.Destination, progress, ct).ConfigureAwait(false);
        }
    }

    private static bool IsTransient(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is OperationCanceledException or UnauthorizedAccessException or System.Security.Authentication.AuthenticationException)
                return false;

            if (current is FtpException ftp)
            {
                if (ftp.ReplyCode is >= 400 and <= 499)
                    return true;
                if (ftp.ReplyCode is >= 500 and <= 599)
                    return false;
                if (ftp.ReplyCode is null)
                    return true;
            }

            if (current is TimeoutException or SocketException)
                return true;
        }

        return false;
    }

    private static async Task DisposeLeaseAsync(IFtpSession? session, bool disposeAfter)
    {
        if (!disposeAfter || session is null)
            return;
        try { await session.DisposeAsync().ConfigureAwait(false); }
        catch { }
    }

    private void Ui(Action update, TransferJob job)
    {
        if (_uiContext is null)
        {
            update();
            JobUpdated?.Invoke(this, job);
            return;
        }
        _uiContext.Post(_ =>
        {
            update();
            JobUpdated?.Invoke(this, job);
        }, null);
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        _shutdown.Cancel();
        lock (_sync)
        {
            foreach (var cts in _cancellations.Values) cts.Cancel();
        }
        try { await _worker.ConfigureAwait(false); } catch (OperationCanceledException) { }
        lock (_sync)
        {
            foreach (var cts in _cancellations.Values) cts.Dispose();
            _cancellations.Clear();
        }
        _shutdown.Dispose();
    }
}
