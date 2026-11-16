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
        // A completed boss is restored directly to its result screen; the
        // victory event does not fire again on load. Restore its music first.
        if (BattleBgmDirector.IsRestoredBossVictory(mapRuntime))
        {
            BattleBgmDirector victoryDirector = Object.FindFirstObjectByType<BattleBgmDirector>(
                FindObjectsInactive.Include);
            if (victoryDirector != null)
                victoryDirector.RestoreBossVictoryBgm();
            else
            {
                AudioManager audio = AudioManager.Instance;
                if (audio != null && audio.IsBgmConfigured(BgmState.Stage1BossVictory))
                    audio.PlayBgm(BgmState.Stage1BossVictory);
                else
                    audio?.FadeOutBgm(0.5f);
            }
            return;
        }

        if (ShouldPlayDefaultBattleBgm(
            hadUnclearedCurrentNodeBeforeLoad,
            hasUnclearedCurrentNodeAfterLoad))
        {
            PlayEntryBgmWhileRoomSelectionIsPending(mapRuntime);
        }
        else
        {
            // A continued expedition skips the default battle BGM. Re-apply
            // the restored room's BGM after the scene finishes loading so the
            // previous TitleMain state cannot remain active.
            BattleBgmDirector director = Object.FindFirstObjectByType<BattleBgmDirector>(
                FindObjectsInactive.Exclude);
            if (director != null)
                director.EnsureRoomBgmAfterSceneLoad();

            // The first Start/Event room may be opened later, after the position
            // panel presentation. Until BattleRoomSelected fires, do not leave
            // the persistent TitleMain BGM playing in the Battle scene.
            if (director == null || !director.HasSelectedRoom)
                PlayEntryBgmWhileRoomSelectionIsPending(mapRuntime);
        }
    }


    private static void PlayEntryBgmWhileRoomSelectionIsPending(MapRuntimeData runtime)
    {
        AudioManager audio = AudioManager.Instance;
        if (audio == null)
            return;

        GeneratedMapNodeData node = MapRuntimeProgressUtility.FindCurrentNode(runtime)
            ?? MapRuntimeProgressUtility.FindStartNode(runtime);
        BgmState desired = BgmState.Stage1Common;
        if (node != null)
        {
            switch (node.Type?.Trim().ToLowerInvariant())
            {
                case "start": desired = BgmState.Stage1Start; break;
                case "event":
                case "special": desired = BgmState.Stage1Special; break;
                case "shop": desired = BgmState.Stage1Shop; break;
                case "rest": desired = BgmState.Stage1Rest; break;
                case "elite": desired = BgmState.Stage1Elite; break;
                case "boss": desired = BgmState.Stage1Boss; break;
            }
        }

        // A missing start-room track must not keep the title track alive.
        if (!audio.IsBgmConfigured(desired))
            desired = audio.IsBgmConfigured(BgmState.Stage1Common)
                ? BgmState.Stage1Common : desired;
        if (audio.IsBgmConfigured(desired))
            audio.PlayBgm(desired);
        else
            audio.FadeOutBgm(0.5f);
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
