using System.Threading.Tasks;
using UnityEngine;

public class TitleState : BaseGameState
{
    public override GameStateType StateType => GameStateType.Title;

    public TitleState(SceneFlowManager sceneFlow) : base(sceneFlow) { }

    public override async Task Enter(GameStateContext context)
    {
        await sceneFlow.LoadSceneAsync(SceneName.Title);
        AudioManager.Instance.PlayBgmDelayed(BgmType.Title);
    }

    public override Task Exit()
    {
        return Task.CompletedTask;
    }
}