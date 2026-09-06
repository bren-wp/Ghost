using System.Text.RegularExpressions;

namespace GhostFTP.Core.Protocol;

public sealed partial class FtpSession
{
    private static void GuardTraversalDepth(int depth)
    {
        if (depth > MaxTraversalDepth)
            throw new IOException($"Remote directory depth exceeds the safety limit of {MaxTraversalDepth} levels.");
    }

    private static void ConsumeTraversalEntries(TraversalBudget budget, int count)
    {
        ArgumentNullException.ThrowIfNull(budget);
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        if ((long)budget.Entries + count > MaxTraversalEntries)
            throw new IOException($"Remote traversal exceeds the safety limit of {MaxTraversalEntries:N0} entries.");

        budget.Entries += count;
    }

    [GeneratedRegex("\\\"(?<path>(?:[^\\\"]|\\\"\\\")*)\\\"")]
    private static partial Regex PwdRegex();
}
