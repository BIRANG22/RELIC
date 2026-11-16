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

        // Capture the entry intent before the Battle scene initializes. A new run
        // creates its first uncleared node during scene Start, which must not be
        // mistaken for a resume when choosing the scene-entry BGM.
        bool hadUnclearedCurrentNodeBeforeLoad =
            MapRuntimeProgressUtility.HasUnclearedCurrentNode(mapRuntime);

        await sceneFlow.LoadSceneAsync(mapRuntime.CurrentSceneName);

        mapRuntime = DataManager.Instance.MapRuntimeStore.Get();
        bool hasUnclearedCurrentNodeAfterLoad =
            MapRuntimeProgressUtility.HasUnclearedCurrentNode(mapRuntime);
        if (ShouldPlayDefaultBattleBgm(
            hadUnclearedCurrentNodeBeforeLoad,
            hasUnclearedCurrentNodeAfterLoad))
            AudioManager.Instance.PlayBgm(BgmState.Stage1Common);
    }

    // 신규 진입에서만 기본 BGM을 재생합니다. 이어하기 방은 BattleBgmDirector가
    // 복원된 노드에 맞는 BGM을 이미 선택하므로 여기서 덮어쓰지 않습니다.
    public static bool ShouldPlayDefaultBattleBgm(bool hasUnclearedCurrentNode) =>
        !hasUnclearedCurrentNode;

    public static bool ShouldPlayDefaultBattleBgm(
        bool hadUnclearedCurrentNodeBeforeLoad,
        bool hasUnclearedCurrentNodeAfterLoad)
    {
        // The post-load value is intentionally not used for the decision. It is
        // accepted to make the timing rule explicit and regression-testable.
        _ = hasUnclearedCurrentNodeAfterLoad;
        return !hadUnclearedCurrentNodeBeforeLoad;
    }

    public override Task Exit()
    {
        return Task.CompletedTask;
    }
}
