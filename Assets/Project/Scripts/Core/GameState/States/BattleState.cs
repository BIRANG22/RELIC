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

    // 신규 진입에서만 기본 BGM을 재생합니다. 이어하기 방은 BattleBgmDirector가
    // 복원된 노드에 맞는 BGM을 이미 선택하므로 여기서 덮어쓰지 않습니다.
    public static bool ShouldPlayDefaultBattleBgm(bool hasUnclearedCurrentNode) =>
        !hasUnclearedCurrentNode;

    public override Task Exit()
    {
        return Task.CompletedTask;
    }
}
