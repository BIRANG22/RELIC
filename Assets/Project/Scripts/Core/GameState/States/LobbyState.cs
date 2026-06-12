using System.Threading.Tasks;
using UnityEngine;

public class LobbyState : BaseGameState
{
    public override GameStateType StateType => GameStateType.Lobby;

    public LobbyState(SceneFlowManager sceneFlow) : base(sceneFlow) { }

    public override async Task Enter(GameStateContext context)
    {
        await sceneFlow.LoadSceneAsync(SceneName.Lobby);
        AudioManager.Instance.PlayBgm(BgmType.Lobby);
    }

    public override Task Exit()
    {
        return Task.CompletedTask;
    }
}