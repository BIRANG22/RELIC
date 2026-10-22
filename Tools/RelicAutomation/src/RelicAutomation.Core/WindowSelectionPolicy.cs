namespace RelicAutomation.Core;

public static class WindowSelectionPolicy
{
    public static IReadOnlyList<WindowSnapshot> SelectCandidates(
        IEnumerable<WindowSnapshot> windows,
        nint ownHandle)
    {
        ArgumentNullException.ThrowIfNull(windows);

        return windows
            .Where(window => window.Handle != nint.Zero)
            .Where(window => window.Handle != ownHandle)
            .Where(window => !string.IsNullOrWhiteSpace(window.Title))
            .Where(window => window.ClientWidth > 0 && window.ClientHeight > 0)
            .OrderBy(window => window.Title, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(window => window.ProcessName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }
}
