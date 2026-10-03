using System.Threading.Tasks;

public sealed class DemoTitleState : BaseGameState
{
    public override GameStateType StateType => GameStateType.DemoTitle;

    public DemoTitleState(SceneFlowManager sceneFlow) : base(sceneFlow) { }

    public override async Task Enter(GameStateContext context)
    {
        await sceneFlow.LoadSceneAsync(SceneName.DemoTitle);
        AudioManager.Instance.PlayBgm(BgmState.TitleMain);
    }
}
