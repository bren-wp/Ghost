using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GhostFTP.Core.Models;

public enum TransferDirection
{
    Upload,
    Download
}

public enum TransferState
{
    Queued,
    Running,
    Retrying,
    Completed,
    Cancelled,
    Failed
}

public sealed class TransferJob : INotifyPropertyChanged
{
    private TransferState _state = TransferState.Queued;
    private double _progress;
    private long _bytesTransferred;
    private long? _totalBytes;
    private string? _error;
    private double _speedBytesPerSecond;
    private int _retryCount;
    private DateTimeOffset? _startedUtc;
    private DateTimeOffset? _finishedUtc;

    public Guid Id { get; } = Guid.NewGuid();
    public TransferDirection Direction { get; init; }
    public string Source { get; init; } = string.Empty;
    public string Destination { get; init; } = string.Empty;
    public bool IsDirectory { get; init; }
    public DateTimeOffset CreatedUtc { get; } = DateTimeOffset.UtcNow;

    public TransferState State
    {
        get => _state;
        set
        {
            if (_state == value) return;
            _state = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(EtaText));
        }
    }

    public double Progress
    {
        get => _progress;
        set
        {
            if (Math.Abs(_progress - value) < 0.001) return;
            _progress = Math.Clamp(value, 0, 100);
            OnPropertyChanged();
            OnPropertyChanged(nameof(ProgressText));
        }
    }

    public long BytesTransferred
    {
        get => _bytesTransferred;
        set
        {
            value = Math.Max(0, value);
            if (_bytesTransferred == value) return;
            _bytesTransferred = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TransferredText));
            OnPropertyChanged(nameof(EtaText));
        }
    }

    public long? TotalBytes
    {
        get => _totalBytes;
        set
        {
            var normalized = value is > 0 ? value : null;
            if (_totalBytes == normalized) return;
            _totalBytes = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TransferredText));
            OnPropertyChanged(nameof(EtaText));
        }
    }

    public double SpeedBytesPerSecond
    {
        get => _speedBytesPerSecond;
        set
        {
            if (Math.Abs(_speedBytesPerSecond - value) < 1) return;
            _speedBytesPerSecond = Math.Max(0, value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(SpeedText));
            OnPropertyChanged(nameof(EtaText));
        }
    }

    public int RetryCount
    {
        get => _retryCount;
        set
        {
            value = Math.Max(0, value);
            if (_retryCount == value) return;
            _retryCount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RetryText));
        }
    }

    public DateTimeOffset? StartedUtc
    {
        get => _startedUtc;
        set => Set(ref _startedUtc, value);
    }

    public DateTimeOffset? FinishedUtc
    {
        get => _finishedUtc;
        set => Set(ref _finishedUtc, value);
    }

    public string? Error
    {
        get => _error;
        set => Set(ref _error, value);
    }

    public string DisplayName
    {
        get
        {
            var value = Source.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, '/', '\\');
            var name = Path.GetFileName(value);
            return string.IsNullOrWhiteSpace(name) ? Source : name;
        }
    }

    public string ProgressText => $"{Progress:0}%";
    public string SpeedText => SpeedBytesPerSecond > 0 ? FormatBytes(SpeedBytesPerSecond) + "/s" : "—";
    public string RetryText => RetryCount == 0 ? string.Empty : RetryCount.ToString();
    public string TransferredText => TotalBytes is > 0
        ? $"{FormatBytes(BytesTransferred)} / {FormatBytes(TotalBytes.Value)}"
        : BytesTransferred > 0 ? FormatBytes(BytesTransferred) : "—";

    public string EtaText
    {
        get
        {
            if (State == TransferState.Completed)
                return "Done";
            if (State is TransferState.Failed or TransferState.Cancelled)
                return "—";
            if (TotalBytes is not > 0 || SpeedBytesPerSecond <= 0 || BytesTransferred >= TotalBytes.Value)
                return "—";

            var seconds = (TotalBytes.Value - BytesTransferred) / SpeedBytesPerSecond;
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
                return "—";

            return FormatDuration(TimeSpan.FromSeconds(Math.Min(seconds, TimeSpan.FromDays(99).TotalSeconds)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static string FormatBytes(double value)
    {
        value = Math.Max(0, value);
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var index = 0;
        while (value >= 1024 && index < units.Length - 1)
        {
            value /= 1024;
            index++;
        }
        return $"{value:0.#} {units[index]}";
    }

    private static string FormatDuration(TimeSpan value)
    {
        if (value.TotalHours >= 24)
            return $"{(int)value.TotalDays}d {value.Hours}h";
        if (value.TotalHours >= 1)
            return $"{(int)value.TotalHours}h {value.Minutes}m";
        if (value.TotalMinutes >= 1)
            return $"{(int)value.TotalMinutes}m {value.Seconds}s";
        return $"{Math.Max(1, (int)Math.Ceiling(value.TotalSeconds))}s";
    }
}
