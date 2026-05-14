using System.Threading.Tasks;
using UnityEngine;

public class BattleState : BaseGameState
{
    public override GameStateType StateType => GameStateType.Battle;

    public BattleState(SceneFlowManager sceneFlow) : base(sceneFlow) { }

    public override async Task Enter(GameStateContext context)
    {
        Debug.Log("[BattleState] Enter");

        var mapRuntime = DataManager.Instance.MapRuntimeStore.Get();

        if (mapRuntime == null || !mapRuntime.IsRunInitialized)
        {
            Debug.LogError("[BattleState] MapRuntime is not initialized.");
            return;
        }

        if (string.IsNullOrEmpty(mapRuntime.CurrentSceneName))
        {
            var mapData = DataManager.Instance.MapDatabase.GetStartMap(
                mapRuntime.SelectedChapterId,
                mapRuntime.CurrentStage
            );

            if (mapData == null)
            {
                Debug.LogError(
                    $"[BattleState] No start map found. Chapter: {mapRuntime.SelectedChapterId}, Stage: {mapRuntime.CurrentStage}"
                );
                return;
            }

            mapRuntime.CurrentMapId = mapData.MapId;
            mapRuntime.CurrentSceneName = mapData.Name.Trim();

            DataManager.Instance.MapRuntimeStore.Set(mapRuntime);
        }

        await sceneFlow.LoadSceneAsync(mapRuntime.CurrentSceneName);
    }

    public override Task Exit()
    {
        Debug.Log("[BattleState] Exit");
        return Task.CompletedTask;
    }
}