using RelicAutomation.Core;
using Xunit;

namespace RelicAutomation.Core.Tests;

public sealed class AutomationRunCoordinatorTests
{
    [Fact]
    public async Task RunAsync_AllIterationsSucceed_CompletesRequestedCount()
    {
        FakeSession session = new();
        AutomationRunCoordinator coordinator = new(session, () => ValidWindow());

        AutomationRunResult result = await coordinator.RunAsync(Profile(repeatCount: 3), ValidWindow(), CancellationToken.None);

        Assert.Equal(AutomationStopReason.Completed, result.StopReason);
        Assert.Equal(3, result.CompletedIterations);
        Assert.Equal(3, session.RunCount);
    }

    [Fact]
    public async Task RunAsync_WindowDisappears_StopsBeforeAnotherIteration()
    {
        int reads = 0;
        FakeSession session = new();
        AutomationRunCoordinator coordinator = new(session, () => ++reads < 2 ? ValidWindow() : null);

        AutomationRunResult result = await coordinator.RunAsync(Profile(repeatCount: 3), ValidWindow(), CancellationToken.None);

        Assert.Equal(AutomationStopReason.WindowLost, result.StopReason);
        Assert.Equal(1, session.RunCount);
    }

    [Fact]
    public async Task RunAsync_WindowSizeChanges_StopsBeforeInput()
    {
        FakeSession session = new();
        AutomationRunCoordinator coordinator = new(session, () => ValidWindow() with { ClientWidth = 1024 });

        AutomationRunResult result = await coordinator.RunAsync(Profile(repeatCount: 2), ValidWindow(), CancellationToken.None);

        Assert.Equal(AutomationStopReason.WindowChanged, result.StopReason);
        Assert.Equal(0, session.RunCount);
    }

    [Fact]
    public async Task RunAsync_SelectedWindowDoesNotMatchProfileSize_StopsBeforeInput()
    {
        WindowSnapshot selected = ValidWindow() with { ClientWidth = 1600, ClientHeight = 900 };
        FakeSession session = new();
        AutomationRunCoordinator coordinator = new(session, () => selected);

        AutomationRunResult result = await coordinator.RunAsync(Profile(repeatCount: 2), selected, CancellationToken.None);

        Assert.Equal(AutomationStopReason.WindowChanged, result.StopReason);
        Assert.Equal(0, session.RunCount);
    }

    [Fact]
    public async Task RunAsync_SessionFails_StopsWithSessionFailure()
    {
        FakeSession session = new() { Result = SessionRunResult.Failed("pipeline failed") };
        AutomationRunCoordinator coordinator = new(session, () => ValidWindow());

        AutomationRunResult result = await coordinator.RunAsync(Profile(repeatCount: 2), ValidWindow(), CancellationToken.None);

        Assert.Equal(AutomationStopReason.SessionFailed, result.StopReason);
        Assert.Equal(0, result.CompletedIterations);
    }

    [Fact]
    public async Task RunAsync_CancellationRequested_DoesNotStartAnotherIteration()
    {
        using CancellationTokenSource source = new();
        FakeSession session = new() { AfterRun = source.Cancel };
        AutomationRunCoordinator coordinator = new(session, () => ValidWindow());

        AutomationRunResult result = await coordinator.RunAsync(Profile(repeatCount: 3), ValidWindow(), source.Token);

        Assert.Equal(AutomationStopReason.Cancelled, result.StopReason);
        Assert.Equal(1, session.RunCount);
    }

    private static WindowSnapshot ValidWindow() => new((nint)123, 10, "Sample", "sample.exe", 1280, 720);

    private static AutomationProfile Profile(int repeatCount) => new(
        "Sample", "Start", "TargetReached", repeatCount, 1280, 720, true, "resource");

    private sealed class FakeSession : IWindowAutomationSession
    {
        public SessionRunResult Result { get; set; } = SessionRunResult.Succeeded();
        public Action? AfterRun { get; set; }
        public int RunCount { get; private set; }

        public Task ConnectAsync(WindowSnapshot window, string resourceDirectory, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<SessionRunResult> RunOnceAsync(string entryNode, string targetNode, CancellationToken cancellationToken)
        {
            RunCount++;
            AfterRun?.Invoke();
            return Task.FromResult(Result);
        }

        public Task StopAsync() => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
