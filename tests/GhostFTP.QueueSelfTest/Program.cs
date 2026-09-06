using System.Diagnostics;
using GhostFTP.Core.Models;
using GhostFTP.Core.Protocol;
using GhostFTP.Core.Services;

namespace GhostFTP.QueueSelfTest;

public static class Program
{
    public static async Task<int> Main()
    {
        try
        {
            await TestParallelWorkersAsync();
            await TestConcurrencyClampAsync();
            await TestCancellationIsolationAsync();
            await TestPauseResumeAndSelectiveClearAsync();
            Console.WriteLine("PASS  Transfer queue runs bounded parallel jobs with isolated sessions");
            Console.WriteLine("PASS  Transfer queue concurrency limits are clamped safely");
            Console.WriteLine("PASS  Cancelling one transfer does not terminate unrelated queue work");
            Console.WriteLine("PASS  Queue pause/resume blocks new dispatch without interrupting active-transfer semantics");
            Console.WriteLine("PASS  Completed, cancelled and failed queue history can be cleared selectively");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL  " + ex.Message);
            return 1;
        }
    }

    private static async Task TestParallelWorkersAsync()
    {
        var probe = new ConcurrencyProbe();
        await using var queue = new TransferQueueService(
            _ => Task.FromResult<(IFtpSession Session, bool DisposeAfter)>((new ProbeSession(probe), true)),
            uiContext: null,
            maxAutomaticRetries: 0,
            concurrentTransferLimit: 3);

        for (var index = 0; index < 9; index++)
            queue.EnqueueUpload($"source-{index}", $"/destination-{index}", isDirectory: false);

        await WaitForSettledAsync(queue, TimeSpan.FromSeconds(10));

        if (queue.Jobs.Any(job => job.State != TransferState.Completed))
            throw new InvalidOperationException("One or more synthetic queue jobs did not complete successfully.");
        if (queue.Jobs.Any(job => job.Progress < 99.999))
            throw new InvalidOperationException("One or more completed synthetic jobs did not finish at 100% progress.");
        if (queue.Jobs.Any(job => job.StartedUtc is null || job.FinishedUtc is null))
            throw new InvalidOperationException("Completed synthetic jobs did not record transfer lifecycle timestamps.");
        if (queue.Jobs.Any(job => job.FinishedUtc < job.StartedUtc))
            throw new InvalidOperationException("A transfer finish timestamp occurred before its start timestamp.");
        if (queue.ConcurrentTransferLimit != 3)
            throw new InvalidOperationException("Configured queue concurrency limit was not retained.");
        if (probe.MaxActive < 2)
            throw new InvalidOperationException("Transfer queue did not execute jobs concurrently.");
        if (probe.MaxActive > 3)
            throw new InvalidOperationException($"Transfer queue exceeded its configured concurrency limit: {probe.MaxActive} active jobs.");
        if (probe.SessionIds.Count != 9)
            throw new InvalidOperationException("Synthetic queue jobs did not receive isolated transfer-session instances.");
    }

    private static async Task TestConcurrencyClampAsync()
    {
        await using (var minimum = new TransferQueueService(
                         _ => throw new InvalidOperationException("Factory must not run in clamp-only test."),
                         concurrentTransferLimit: 0))
        {
            if (minimum.ConcurrentTransferLimit != 1)
                throw new InvalidOperationException("Transfer queue concurrency minimum was not clamped to one worker.");
        }

        await using (var maximum = new TransferQueueService(
                         _ => throw new InvalidOperationException("Factory must not run in clamp-only test."),
                         concurrentTransferLimit: 100))
        {
            if (maximum.ConcurrentTransferLimit != 8)
                throw new InvalidOperationException("Transfer queue concurrency maximum was not clamped to eight workers.");
        }
    }

    private static async Task TestCancellationIsolationAsync()
    {
        var probe = new ConcurrencyProbe(delayMilliseconds: 220);
        await using var queue = new TransferQueueService(
            _ => Task.FromResult<(IFtpSession Session, bool DisposeAfter)>((new ProbeSession(probe), true)),
            uiContext: null,
            maxAutomaticRetries: 0,
            concurrentTransferLimit: 2);

        var cancelled = queue.EnqueueUpload("cancel-me", "/cancel-me", isDirectory: false);
        var survivorA = queue.EnqueueUpload("survivor-a", "/survivor-a", isDirectory: false);
        var survivorB = queue.EnqueueUpload("survivor-b", "/survivor-b", isDirectory: false);
        var survivorC = queue.EnqueueUpload("survivor-c", "/survivor-c", isDirectory: false);

        queue.Cancel(cancelled.Id);
        await WaitForSettledAsync(queue, TimeSpan.FromSeconds(10));

        if (cancelled.State != TransferState.Cancelled)
            throw new InvalidOperationException($"Cancelled transfer ended in unexpected state {cancelled.State}.");

        foreach (var survivor in new[] { survivorA, survivorB, survivorC })
        {
            if (survivor.State != TransferState.Completed)
                throw new InvalidOperationException("Cancelling one transfer prevented an unrelated transfer from completing.");
            if (survivor.Progress < 99.999)
                throw new InvalidOperationException("An unrelated survivor transfer did not reach 100% progress.");
        }
    }

    private static async Task TestPauseResumeAndSelectiveClearAsync()
    {
        var probe = new ConcurrencyProbe(delayMilliseconds: 80);
        await using var queue = new TransferQueueService(
            _ => Task.FromResult<(IFtpSession Session, bool DisposeAfter)>((new ProbeSession(probe), true)),
            uiContext: null,
            maxAutomaticRetries: 0,
            concurrentTransferLimit: 2);

        var stateChanges = 0;
        queue.QueueStateChanged += (_, _) => Interlocked.Increment(ref stateChanges);
        queue.PauseQueue();
        if (!queue.IsQueuePaused)
            throw new InvalidOperationException("PauseQueue did not expose the paused queue state.");

        var first = queue.EnqueueUpload("paused-a", "/paused-a", isDirectory: false);
        var second = queue.EnqueueUpload("paused-b", "/paused-b", isDirectory: false);
        await Task.Delay(160);

        if (first.State != TransferState.Queued || second.State != TransferState.Queued)
            throw new InvalidOperationException("A paused queue started a new transfer before ResumeQueue.");
        if (probe.SessionIds.Count != 0)
            throw new InvalidOperationException("A paused queue created a transfer session before dispatch resumed.");

        queue.ResumeQueue();
        if (queue.IsQueuePaused)
            throw new InvalidOperationException("ResumeQueue left the queue marked as paused.");
        await WaitForSettledAsync(queue, TimeSpan.FromSeconds(10));

        if (first.State != TransferState.Completed || second.State != TransferState.Completed)
            throw new InvalidOperationException("Paused transfers did not complete after the queue resumed.");
        if (Volatile.Read(ref stateChanges) < 2)
            throw new InvalidOperationException("Queue pause/resume did not raise state-change notifications.");
        if (queue.ClearCompleted() != 2 || queue.Jobs.Count != 0)
            throw new InvalidOperationException("Selective completed-transfer cleanup did not remove only completed history.");

        queue.PauseQueue();
        var cancelled = queue.EnqueueUpload("paused-cancel", "/paused-cancel", isDirectory: false);
        queue.Cancel(cancelled.Id);
        queue.ResumeQueue();
        await WaitForSettledAsync(queue, TimeSpan.FromSeconds(10));
        if (cancelled.State != TransferState.Cancelled)
            throw new InvalidOperationException("A transfer cancelled while queue dispatch was paused did not become Cancelled.");
        if (queue.ClearCancelled() != 1 || queue.Jobs.Count != 0)
            throw new InvalidOperationException("Selective cancelled-transfer cleanup did not remove cancelled history.");

        await using var failingQueue = new TransferQueueService(
            _ => throw new InvalidOperationException("synthetic permanent failure"),
            uiContext: null,
            maxAutomaticRetries: 0,
            concurrentTransferLimit: 1);
        var failed = failingQueue.EnqueueUpload("fail", "/fail", isDirectory: false);
        await WaitForSettledAsync(failingQueue, TimeSpan.FromSeconds(10));
        if (failed.State != TransferState.Failed)
            throw new InvalidOperationException("Synthetic permanent transfer failure did not produce Failed state.");
        if (failingQueue.ClearFailed() != 1 || failingQueue.Jobs.Count != 0)
            throw new InvalidOperationException("Selective failed-transfer cleanup did not remove failed history.");
    }

    private static async Task WaitForSettledAsync(TransferQueueService queue, TimeSpan timeoutValue)
    {
        var timeout = Stopwatch.StartNew();
        while (queue.Jobs.Any(job => job.State is TransferState.Queued or TransferState.Running or TransferState.Retrying))
        {
            if (timeout.Elapsed > timeoutValue)
                throw new InvalidOperationException("Parallel transfer queue did not settle within the test timeout.");
            await Task.Delay(20);
        }
    }

    private sealed class ConcurrencyProbe
    {
        private int _active;
        private int _maxActive;
        private readonly int _delayMilliseconds;
        private readonly object _sync = new();
        private readonly HashSet<Guid> _sessionIds = [];

        public ConcurrencyProbe(int delayMilliseconds = 140)
        {
            _delayMilliseconds = delayMilliseconds;
        }

        public int MaxActive => Volatile.Read(ref _maxActive);

        public IReadOnlyCollection<Guid> SessionIds
        {
            get
            {
                lock (_sync)
                    return _sessionIds.ToArray();
            }
        }

        public void Register(Guid sessionId)
        {
            lock (_sync)
                _sessionIds.Add(sessionId);
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var active = Interlocked.Increment(ref _active);
            while (true)
            {
                var observed = Volatile.Read(ref _maxActive);
                if (active <= observed || Interlocked.CompareExchange(ref _maxActive, active, observed) == observed)
                    break;
            }

            try
            {
                await Task.Delay(_delayMilliseconds, cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref _active);
            }
        }
    }

    private sealed class ProbeSession : IFtpSession
    {
        private readonly ConcurrencyProbe _probe;
        private readonly Guid _id = Guid.NewGuid();
        private bool _disposed;

        public ProbeSession(ConcurrencyProbe probe)
        {
            _probe = probe;
            _probe.Register(_id);
        }

        public bool IsConnected => !_disposed;
        public bool IsEncrypted => true;
        public string Host => "queue-self-test.local";
        public string WorkingDirectory => "/";

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(_disposed, this);
            return Task.CompletedTask;
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<FtpEntry>> ListAsync(string remotePath, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FtpEntry>>([]);

        public Task<string> GetWorkingDirectoryAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult("/");

        public Task ChangeDirectoryAsync(string remotePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task CreateDirectoryAsync(string remotePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RenameAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteFileAsync(string remotePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteDirectoryAsync(string remotePath, bool recursive, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public async Task DownloadFileAsync(
            string remotePath,
            string localPath,
            IProgress<(long transferred, long? total)>? progress = null,
            CancellationToken cancellationToken = default)
        {
            await _probe.RunAsync(cancellationToken);
            progress?.Report((1, 1));
        }

        public async Task UploadFileAsync(
            string localPath,
            string remotePath,
            IProgress<(long transferred, long? total)>? progress = null,
            CancellationToken cancellationToken = default)
        {
            await _probe.RunAsync(cancellationToken);
            progress?.Report((1, 1));
        }

        public Task DownloadDirectoryAsync(
            string remotePath,
            string localDirectory,
            IProgress<(long transferred, long? total)>? progress = null,
            CancellationToken cancellationToken = default) =>
            DownloadFileAsync(remotePath, localDirectory, progress, cancellationToken);

        public Task UploadDirectoryAsync(
            string localDirectory,
            string remotePath,
            IProgress<(long transferred, long? total)>? progress = null,
            CancellationToken cancellationToken = default) =>
            UploadFileAsync(localDirectory, remotePath, progress, cancellationToken);

        public ValueTask DisposeAsync()
        {
            _disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
