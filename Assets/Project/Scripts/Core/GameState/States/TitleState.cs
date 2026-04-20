using System.Threading.Tasks;
using UnityEngine;

public class TitleState : BaseGameState
{
    public override GameStateType StateType => GameStateType.Title;

    public TitleState(SceneFlowManager sceneFlow) : base(sceneFlow) { }

    public override async Task Enter(GameStateContext context)
    {
        Debug.Log("[TitleState] Enter");

        await sceneFlow.LoadSceneAsync(SceneName.Title);
        AudioManager.Instance.PlayBgm(BgmType.Title);
    }

    public override Task Exit()
    {
        Debug.Log("[TitleState] Exit");
        return Task.CompletedTask;
    }
}