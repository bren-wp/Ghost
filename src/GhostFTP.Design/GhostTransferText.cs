namespace GhostFTP.Design;

/// <summary>
/// Shared transfer-management copy used by the Windows and Linux renderers.
/// English is authoritative and is the fallback for every installed language so new queue controls
/// never become blank or depend on an online translation service.
/// </summary>
public static class GhostTransferText
{
    private static readonly Dictionary<string, string> English = new(StringComparer.Ordinal)
    {
        ["PauseQueue"] = "Pause queue",
        ["ResumeQueue"] = "Resume queue",
        ["RetryFailed"] = "Retry failed",
        ["ClearCompleted"] = "Clear completed",
        ["ClearFailed"] = "Clear failed",
        ["ClearCancelled"] = "Clear cancelled",
        ["QueuePaused"] = "Queue paused",
        ["QueueActive"] = "Queue active",
        ["RunningContinue"] = "Running transfers continue; only new dispatch is paused.",
        ["NoFailedTransfers"] = "There are no failed transfers to retry."
    };

    private static readonly Dictionary<string, string> Croatian = new(StringComparer.Ordinal)
    {
        ["PauseQueue"] = "Pauziraj red",
        ["ResumeQueue"] = "Nastavi red",
        ["RetryFailed"] = "Ponovi neuspjele",
        ["ClearCompleted"] = "Očisti dovršene",
        ["ClearFailed"] = "Očisti neuspjele",
        ["ClearCancelled"] = "Očisti otkazane",
        ["QueuePaused"] = "Red je pauziran",
        ["QueueActive"] = "Red je aktivan",
        ["RunningContinue"] = "Aktivni prijenosi se nastavljaju; pauzira se samo pokretanje novih.",
        ["NoFailedTransfers"] = "Nema neuspjelih prijenosa za ponovno pokretanje."
    };

    public static string T(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (string.Equals(GhostLocalization.CurrentLanguageCode, "hr", StringComparison.OrdinalIgnoreCase)
            && Croatian.TryGetValue(key, out var croatian))
        {
            return croatian;
        }

        return English.TryGetValue(key, out var english) ? english : key;
    }
}
