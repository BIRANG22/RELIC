using System.Threading.Tasks;
using UnityEngine;

public class BattleState : BaseGameState
{
    public override GameStateType StateType => GameStateType.Battle;

    public BattleState(SceneFlowManager sceneFlow) : base(sceneFlow) { }

    public override async Task Enter(GameStateContext context)
    {
        Debug.Log("[BattleState] Enter");

        if (!context.PendingBattle.isInitialized)
        {
            Debug.LogWarning("[BattleState] PendingBattle is not initialized.");
        }

        await sceneFlow.LoadSceneAsync(SceneName.Battle);
    }

    public override Task Exit()
    {
        Debug.Log("[BattleState] Exit");
        return Task.CompletedTask;
    }
}