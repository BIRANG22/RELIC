using System.Threading.Tasks;
using Relic.Gameplay.Data;
using UnityEngine;

public class BattleState : BaseGameState
{
    public override GameStateType StateType => GameStateType.Battle;

    public BattleState(SceneFlowManager sceneFlow) : base(sceneFlow) { }

    public override async Task Enter(GameStateContext context)
    {
        var mapRuntime = DataManager.Instance.MapRuntimeStore.Get();

        if (mapRuntime == null)
        {
            Debug.LogError("[BattleState] MapRuntime is null.");
            return;
        }

        if (string.IsNullOrWhiteSpace(mapRuntime.SelectedChapterId) ||
            string.IsNullOrWhiteSpace(mapRuntime.CurrentStage))
        {
            Debug.LogError("[BattleState] Chapter or Stage is not selected.");
            return;
        }

        if (string.IsNullOrWhiteSpace(mapRuntime.CurrentMapId))
            mapRuntime.CurrentMapId = ""; // Keep empty until a map node is selected.

        mapRuntime.CurrentSceneName = SceneName.Battle;

        DataManager.Instance.MapRuntimeStore.Set(mapRuntime);

        await sceneFlow.LoadSceneAsync(mapRuntime.CurrentSceneName);

        mapRuntime = DataManager.Instance.MapRuntimeStore.Get();
        bool hasUnclearedCurrentNode =
            MapRuntimeProgressUtility.HasUnclearedCurrentNode(mapRuntime);
        if (ShouldPlayDefaultBattleBgm(hasUnclearedCurrentNode))
            AudioManager.Instance.PlayBgm(BgmState.Stage1Common);
    }

    public static bool ShouldPlayDefaultBattleBgm(bool hasUnclearedCurrentNode) =>
        !hasUnclearedCurrentNode;

    public override Task Exit()
    {
        return Task.CompletedTask;
    }
}
