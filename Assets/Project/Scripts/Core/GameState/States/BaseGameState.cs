using System.Threading.Tasks;

public abstract class BaseGameState : IGameState
{
    protected readonly SceneFlowManager sceneFlow;

    public abstract GameStateType StateType { get; }

    protected BaseGameState(SceneFlowManager sceneFlow)
    {
        this.sceneFlow = sceneFlow;
    }

    public virtual Task Enter(GameStateContext context)
    {
        return Task.CompletedTask;
    }

    public virtual Task Exit()
    {
        return Task.CompletedTask;
    }
}