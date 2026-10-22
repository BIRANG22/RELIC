namespace RelicAutomation.Core;

public sealed record WindowSnapshot(
    nint Handle,
    int ProcessId,
    string Title,
    string ProcessName,
    int ClientWidth,
    int ClientHeight)
{
    public bool HasSameIdentity(WindowSnapshot other) =>
        Handle == other.Handle
        && ProcessId == other.ProcessId
        && string.Equals(Title, other.Title, StringComparison.Ordinal)
        && string.Equals(ProcessName, other.ProcessName, StringComparison.OrdinalIgnoreCase);
}

public sealed record SessionRunResult(bool IsSuccess, string Error)
{
    public static SessionRunResult Succeeded() => new(true, string.Empty);
    public static SessionRunResult Failed(string error) => new(false, error ?? string.Empty);
}

public interface IWindowAutomationSession : IAsyncDisposable
{
    Task ConnectAsync(WindowSnapshot window, string resourceDirectory, CancellationToken cancellationToken);
    Task<SessionRunResult> RunOnceAsync(string entryNode, string targetNode, CancellationToken cancellationToken);
    Task StopAsync();
}

public enum AutomationStopReason
{
    Completed,
    Cancelled,
    WindowLost,
    WindowChanged,
    SessionFailed
}

public sealed record AutomationRunResult(
    AutomationStopReason StopReason,
    int CompletedIterations,
    string Error);
