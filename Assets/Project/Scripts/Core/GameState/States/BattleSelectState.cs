using System.Threading.Tasks;
using UnityEngine;

public class BattleSelectState : BaseGameState
{
    public override GameStateType StateType => GameStateType.BattleSelect;

    public BattleSelectState(SceneFlowManager sceneFlow) : base(sceneFlow) { }

    public override async Task Enter(GameStateContext context)
    {
        Debug.Log("[BattleSelectState] Enter");
        await sceneFlow.LoadSceneAsync(SceneName.BattleSelect);
    }

    public override Task Exit()
    {
        Debug.Log("[BattleSelectState] Exit");
        return Task.CompletedTask;
    }
}