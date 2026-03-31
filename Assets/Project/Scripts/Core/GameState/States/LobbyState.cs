using System.Threading.Tasks;
using UnityEngine;

public class LobbyState : BaseGameState
{
    public override GameStateType StateType => GameStateType.Lobby;

    public LobbyState(SceneFlowManager sceneFlow) : base(sceneFlow) { }

    public override async Task Enter(GameStateContext context)
    {
        Debug.Log("[LobbyState] Enter");
        await sceneFlow.LoadSceneAsync(SceneName.Lobby);
    }

    public override Task Exit()
    {
        Debug.Log("[LobbyState] Exit");
        return Task.CompletedTask;
    }
}