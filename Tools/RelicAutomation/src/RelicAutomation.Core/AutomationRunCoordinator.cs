namespace RelicAutomation.Core;

public sealed class AutomationRunCoordinator
{
    private readonly IWindowAutomationSession session;
    private readonly Func<WindowSnapshot?> readWindow;

    public AutomationRunCoordinator(IWindowAutomationSession session, Func<WindowSnapshot?> readWindow)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.readWindow = readWindow ?? throw new ArgumentNullException(nameof(readWindow));
    }

    public async Task<AutomationRunResult> RunAsync(
        AutomationProfile profile,
        WindowSnapshot selectedWindow,
        CancellationToken cancellationToken,
        string resourceDirectory = "")
    {
        int completed = 0;

        if (profile.StopWhenWindowSizeChanges
            && (selectedWindow.ClientWidth != profile.ExpectedClientWidth
                || selectedWindow.ClientHeight != profile.ExpectedClientHeight))
        {
            return await StopAsync(
                AutomationStopReason.WindowChanged,
                completed,
                $"The selected window size must be {profile.ExpectedClientWidth}x{profile.ExpectedClientHeight}.");
        }

        for (int iteration = 0; iteration < profile.RepeatCount; iteration++)
        {
            if (cancellationToken.IsCancellationRequested)
                return await StopAsync(AutomationStopReason.Cancelled, completed, string.Empty);

            WindowSnapshot? current = readWindow();
            if (current is null || !selectedWindow.HasSameIdentity(current))
                return await StopAsync(AutomationStopReason.WindowLost, completed, "The selected window is no longer available.");

            if (profile.StopWhenWindowSizeChanges
                && (current.ClientWidth != selectedWindow.ClientWidth || current.ClientHeight != selectedWindow.ClientHeight))
            {
                return await StopAsync(AutomationStopReason.WindowChanged, completed, "The selected window size changed.");
            }

            if (iteration == 0)
                await session.ConnectAsync(selectedWindow, resourceDirectory, cancellationToken);

            SessionRunResult run = await session.RunOnceAsync(profile.EntryNode, profile.TargetNode, cancellationToken);
            if (!run.IsSuccess)
                return await StopAsync(AutomationStopReason.SessionFailed, completed, run.Error);

            completed++;
        }

        return new AutomationRunResult(AutomationStopReason.Completed, completed, string.Empty);
    }

    private async Task<AutomationRunResult> StopAsync(AutomationStopReason reason, int completed, string error)
    {
        await session.StopAsync();
        return new AutomationRunResult(reason, completed, error);
    }
}
