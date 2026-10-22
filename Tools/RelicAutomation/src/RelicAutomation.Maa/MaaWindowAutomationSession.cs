using MaaFramework.Binding;
using RelicAutomation.Core;

namespace RelicAutomation.Maa;

public sealed class MaaWindowAutomationSession : IWindowAutomationSession
{
    private readonly object gate = new();
    private MaaTasker? tasker;

    public Task ConnectAsync(
        WindowSnapshot window,
        string resourceDirectory,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var controller = new MaaWin32Controller(
                window.Handle,
                Win32ScreencapMethods.Background,
                Win32InputMethod.SendMessage,
                Win32InputMethod.SendMessage,
                LinkOption.Start,
                CheckStatusOption.ThrowIfNotSucceeded);
            var resource = new MaaResource(
                CheckStatusOption.ThrowIfNotSucceeded,
                new[] { resourceDirectory });
            var createdTasker = new MaaTasker(controller, resource, DisposeOptions.All, true);

            if (!createdTasker.IsInitialized)
            {
                createdTasker.Dispose();
                throw new InvalidOperationException("MaaFramework Tasker 초기화에 실패했습니다.");
            }

            lock (gate)
            {
                tasker?.Dispose();
                tasker = createdTasker;
            }
        }, cancellationToken);
    }

    public async Task<SessionRunResult> RunOnceAsync(
        string entryNode,
        string targetNode,
        CancellationToken cancellationToken)
    {
        MaaTasker current;
        lock (gate)
        {
            current = tasker ?? throw new InvalidOperationException("먼저 대상 창에 연결해야 합니다.");
        }

        try
        {
            using var registration = cancellationToken.Register(() => current.Stop());
            var status = await Task.Run(
                () => current.AppendTask(entryNode, "{}").Wait(),
                CancellationToken.None);

            if (cancellationToken.IsCancellationRequested)
            {
                return SessionRunResult.Failed("사용자가 실행을 중지했습니다.");
            }

            if (status != MaaJobStatus.Succeeded)
            {
                return SessionRunResult.Failed(
                    $"MaaFramework 작업이 실패했습니다. entry={entryNode}, target={targetNode}, status={status}");
            }

            return current.GetLatestNode(targetNode, out _)
                ? SessionRunResult.Succeeded()
                : SessionRunResult.Failed($"목표 노드가 실행되지 않았습니다: {targetNode}");
        }
        catch (Exception exception)
        {
            return SessionRunResult.Failed(exception.Message);
        }
    }

    public Task StopAsync()
    {
        lock (gate)
        {
            tasker?.Stop();
        }

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        lock (gate)
        {
            tasker?.Dispose();
            tasker = null;
        }

        return ValueTask.CompletedTask;
    }
}
