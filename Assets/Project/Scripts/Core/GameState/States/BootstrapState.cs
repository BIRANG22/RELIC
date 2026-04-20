using System.Threading.Tasks;
using UnityEngine;

public class BootstrapState : BaseGameState
{
    public override GameStateType StateType => GameStateType.Bootstrap;

    public BootstrapState(SceneFlowManager sceneFlow) : base(sceneFlow) { }

    public override async Task Enter(GameStateContext context)
    {
        Debug.Log("[BootstrapState] Enter");

        await sceneFlow.LoadSceneAsync(SceneName.Bootstrap);

        context.ClearRunData();

        // 여기서 나중에
        // - 세이브 로드
        // - Addressables 초기화
        // - 글로벌 시스템 초기화
        // 등을 추가하면 됨.

        //await GameManager.Instance.StateMachine.ChangeState(GameStateType.Lobby);
    }

    public override Task Exit()
    {
        Debug.Log("[BootstrapState] Exit");
        return Task.CompletedTask;
    }
}