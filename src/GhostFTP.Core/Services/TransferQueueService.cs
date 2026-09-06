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

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        private readonly Action<T> _report = report ?? throw new ArgumentNullException(nameof(report));
        public void Report(T value) => _report(value);
    }

    private readonly Func<CancellationToken, Task<(IFtpSession Session, bool DisposeAfter)>> _sessionFactory;
    private const int MaxQueuedTransfers = 4096;
    private const int MaxParallelTransfers = 8;
    private static readonly TimeSpan MinimumProgressUiInterval = TimeSpan.FromMilliseconds(100);
    private readonly Channel<Queued> _channel = Channel.CreateBounded<Queued>(new BoundedChannelOptions(MaxQueuedTransfers)
    {
        SingleReader = false,
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.Wait
    });
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Dictionary<Guid, CancellationTokenSource> _cancellations = new();
    private readonly object _sync = new();
    private readonly Task[] _workers;
    private readonly SynchronizationContext? _uiContext;
    private readonly int _maxAutomaticRetries;
    private readonly TaskCompletionSource<bool> _disposeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private TaskCompletionSource<bool> _resumeSignal = CreateCompletedSignal();
    private bool _isQueuePaused;
    private int _disposeState;

    public ObservableCollection<TransferJob> Jobs { get; } = [];
    public event EventHandler<TransferJob>? JobUpdated;
    public event EventHandler? QueueStateChanged;
    public int ConcurrentTransferLimit { get; }

    /// <summary>
    /// True when dispatch of queued/retrying transfers is paused. Transfers that were already running
    /// continue to completion so Ghost FTP never pretends that an FTP data stream can be safely paused
    /// when the remote server has not negotiated resumable transfer semantics.
    /// </summary>
    public bool IsQueuePaused
    {
        get
        {
            lock (_sync)
                return _isQueuePaused;
        }
    }

    public TransferQueueService(
        Func<CancellationToken, Task<(IFtpSession Session, bool DisposeAfter)>> sessionFactory,
        SynchronizationContext? uiContext = null,
        int maxAutomaticRetries = 2,
        int concurrentTransferLimit = 3)
    {
        _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        _uiContext = uiContext;
        _maxAutomaticRetries = Math.Clamp(maxAutomaticRetries, 0, 5);
        ConcurrentTransferLimit = Math.Clamp(concurrentTransferLimit, 1, MaxParallelTransfers);
        _workers = Enumerable.Range(0, ConcurrentTransferLimit)
            .Select(_ => Task.Run(WorkerAsync))
            .ToArray();
    }

    public TransferJob EnqueueUpload(string source, string destination, bool isDirectory)
    {
        var job = new TransferJob
        {
            Direction = TransferDirection.Upload,
            Source = source,
            Destination = destination,
            IsDirectory = isDirectory,
            TotalBytes = isDirectory ? null : TryGetLocalFileLength(source)
        };
        Enqueue(job);
        return job;
    }

    public TransferJob EnqueueDownload(string source, string destination, bool isDirectory, long? totalBytes = null)
    {
        var job = new TransferJob
        {
            Direction = TransferDirection.Download,
            Source = source,
            Destination = destination,
            IsDirectory = isDirectory,
            TotalBytes = totalBytes
        };
        Enqueue(job);
        return job;
    }

    /// <summary>
    /// Stops workers from starting additional queued/retrying transfers. Running transfers are not
    /// interrupted; callers can cancel those explicitly when that is the intended action.
    /// </summary>
    public void PauseQueue()
    {
        var changed = false;
        lock (_sync)
        {
            if (!_isQueuePaused && Volatile.Read(ref _disposeState) == 0 && !_shutdown.IsCancellationRequested)
            {
                _isQueuePaused = true;
                _resumeSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                changed = true;
            }
        }

        if (changed)
            RaiseQueueStateChanged();
    }

    public void ResumeQueue()
    {
        TaskCompletionSource<bool>? signal = null;
        lock (_sync)
        {
            if (_isQueuePaused)
            {
                _isQueuePaused = false;
                signal = _resumeSignal;
            }
        }

        if (signal is null)
            return;

        signal.TrySetResult(true);
        RaiseQueueStateChanged();
    }

    public void Cancel(Guid jobId)
    {
        lock (_sync)
        {
            if (_cancellations.TryGetValue(jobId, out var cts))
                cts.Cancel();
        }
    }

    public void ClearFinished() =>
        ClearStates(TransferState.Completed, TransferState.Cancelled, TransferState.Failed);

    public int ClearCompleted() => ClearStates(TransferState.Completed);

    public int ClearFailed() => ClearStates(TransferState.Failed);

    public int ClearCancelled() => ClearStates(TransferState.Cancelled);

    private void Enqueue(TransferJob job)
    {
        CancellationTokenSource? cts = null;
        lock (_sync)
        {
            if (Volatile.Read(ref _disposeState) == 0 && !_shutdown.IsCancellationRequested)
            {
                cts = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token);
                _cancellations[job.Id] = cts;
            }
        }

        Jobs.Add(job);
        if (cts is null)
        {
            FailBeforeDispatch(job, "Transfer queue is shutting down.");
            return;
        }

        if (!_channel.Writer.TryWrite(new Queued(job, cts)))
        {
            lock (_sync) _cancellations.Remove(job.Id);
            cts.Dispose();
            FailBeforeDispatch(job, $"Transfer queue is full. Maximum queued transfers: {MaxQueuedTransfers:N0}.");
            return;
        }
        JobUpdated?.Invoke(this, job);
    }

    private void FailBeforeDispatch(TransferJob job, string error)
    {
        job.Error = error;
        job.State = TransferState.Failed;
        job.FinishedUtc = DateTimeOffset.UtcNow;
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
                    await WaitForDispatchAsync(ct).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();

                    var lease = await _sessionFactory(ct).ConfigureAwait(false);
                    transferSession = lease.Session;
                    disposeAfter = lease.DisposeAfter;
                    if (!transferSession.IsConnected)
                        throw new InvalidOperationException("Transfer session is not connected.");

                    Ui(() =>
                    {
                        job.StartedUtc ??= DateTimeOffset.UtcNow;
                        job.FinishedUtc = null;
                        job.State = TransferState.Running;
                        if (attempt == 0) job.Error = null;
                    }, job);

                    await ExecuteTransferAsync(job, transferSession, ct).ConfigureAwait(false);
                    Ui(() =>
                    {
                        job.Progress = 100;
                        job.Error = null;
                        job.SpeedBytesPerSecond = 0;
                        job.State = TransferState.Completed;
                        job.FinishedUtc = DateTimeOffset.UtcNow;
                    }, job);
                    return;
                }
                catch (OperationCanceledException)
                {
                    Ui(() =>
                    {
                        job.SpeedBytesPerSecond = 0;
                        job.State = TransferState.Cancelled;
                        job.FinishedUtc = DateTimeOffset.UtcNow;
                    }, job);
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
                    try
                    {
                        await Task.Delay(delay, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        Ui(() =>
                        {
                            job.SpeedBytesPerSecond = 0;
                            job.State = TransferState.Cancelled;
                            job.FinishedUtc = DateTimeOffset.UtcNow;
                        }, job);
                        return;
                    }
                    continue;
                }
                catch (Exception ex)
                {
                    Ui(() =>
                    {
                        job.Error = ex.Message;
                        job.SpeedBytesPerSecond = 0;
                        job.State = TransferState.Failed;
                        job.FinishedUtc = DateTimeOffset.UtcNow;
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

    private Task WaitForDispatchAsync(CancellationToken cancellationToken)
    {
        Task signalTask;
        lock (_sync)
        {
            if (!_isQueuePaused)
                return Task.CompletedTask;
            signalTask = _resumeSignal.Task;
        }
        return signalTask.WaitAsync(cancellationToken);
    }

    private async Task ExecuteTransferAsync(TransferJob job, IFtpSession transferSession, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        long lastBytes = 0;
        var lastSpeedTime = TimeSpan.Zero;
        var lastUiTime = TimeSpan.Zero;
        var hasSpeedBaseline = false;
        var hasUiReport = false;

        // FtpSession reports progress synchronously from its transfer loop. Using an inline progress
        // adapter avoids Progress<T>'s extra ThreadPool hop; Ui() remains the single deliberate
        // marshaling boundary for renderers that supplied a SynchronizationContext.
        var progress = new InlineProgress<(long transferred, long? total)>(p =>
        {
            var transferred = Math.Max(0, p.transferred);
            var total = p.total ?? job.TotalBytes;
            var elapsed = stopwatch.Elapsed;
            var speed = -1d;

            if (!hasSpeedBaseline)
            {
                lastBytes = transferred;
                lastSpeedTime = elapsed;
                hasSpeedBaseline = true;
            }
            else
            {
                var deltaSeconds = (elapsed - lastSpeedTime).TotalSeconds;
                if (deltaSeconds >= 0.5)
                {
                    speed = Math.Max(0, transferred - lastBytes) / deltaSeconds;
                    lastBytes = transferred;
                    lastSpeedTime = elapsed;
                }
            }

            var finalProgress = total is > 0 && transferred >= total.Value;
            if (hasUiReport && !finalProgress && elapsed - lastUiTime < MinimumProgressUiInterval)
                return;

            hasUiReport = true;
            lastUiTime = elapsed;
            Ui(() =>
            {
                job.BytesTransferred = transferred;
                if (total is > 0)
                {
                    job.TotalBytes = total;
                    job.Progress = Math.Clamp(transferred * 100d / total.Value, 0d, 100d);
                }
                if (speed >= 0)
                    job.SpeedBytesPerSecond = speed;
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

    private int ClearStates(params TransferState[] states)
    {
        var stateSet = states.ToHashSet();
        var matches = Jobs.Where(job => stateSet.Contains(job.State)).ToArray();
        foreach (var job in matches)
            Jobs.Remove(job);
        return matches.Length;
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

    private static long? TryGetLocalFileLength(string source)
    {
        try
        {
            var info = new FileInfo(source);
            return info.Exists ? Math.Max(0, info.Length) : null;
        }
        catch
        {
            return null;
        }
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

    private void RaiseQueueStateChanged()
    {
        if (_uiContext is null)
        {
            QueueStateChanged?.Invoke(this, EventArgs.Empty);
            return;
        }
        _uiContext.Post(_ => QueueStateChanged?.Invoke(this, EventArgs.Empty), null);
    }

    private static TaskCompletionSource<bool> CreateCompletedSignal()
    {
        var signal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        signal.TrySetResult(true);
        return signal;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _disposeState, 1, 0) != 0)
        {
            await _disposeCompletion.Task.ConfigureAwait(false);
            return;
        }

        try
        {
            TaskCompletionSource<bool> resumeSignal;
            lock (_sync)
            {
                _isQueuePaused = false;
                resumeSignal = _resumeSignal;
            }
            resumeSignal.TrySetResult(true);

            _channel.Writer.TryComplete();
            _shutdown.Cancel();
            lock (_sync)
            {
                foreach (var cts in _cancellations.Values) cts.Cancel();
            }

            try
            {
                await Task.WhenAll(_workers).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
            {
            }

            lock (_sync)
            {
                foreach (var cts in _cancellations.Values) cts.Dispose();
                _cancellations.Clear();
            }
            _shutdown.Dispose();
        }
        finally
        {
            Volatile.Write(ref _disposeState, 2);
            _disposeCompletion.TrySetResult(true);
        }
    }
}
