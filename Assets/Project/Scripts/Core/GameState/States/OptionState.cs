using System.Threading.Tasks;
using UnityEngine;

public class OptionState : BaseGameState
{
    public override GameStateType StateType => GameStateType.Option;

    public OptionState(SceneFlowManager sceneFlow) : base(sceneFlow) { }

    public override Task Enter(GameStateContext context)
    {
        Debug.Log("[OptionState] Enter");
        UIManager.Instance.ShowOption();
        return Task.CompletedTask;
    }

    public override Task Exit()
    {
        Debug.Log("[OptionState] Exit");
        UIManager.Instance.HideOption();
        return Task.CompletedTask;
    }
}