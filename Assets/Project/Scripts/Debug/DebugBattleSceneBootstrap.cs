using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DebugBattleSceneBootstrap
{
    private const string DebugBattleSceneName = "DebugBattle";
    private const string DebugWindowObjectName = "BattleEffectDebugWindow";
    internal const string DebugTargetMonsterId = "Mon_02";
    internal const int DebugTargetGridIndex = 23;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureDebugWindow(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureDebugWindow(scene);
    }

    private static void EnsureDebugWindow(Scene scene)
    {
        if (!scene.IsValid() || scene.name != DebugBattleSceneName)
            return;

        BattleEffectDebugWindow existingWindow = Object.FindFirstObjectByType<BattleEffectDebugWindow>(
            FindObjectsInactive.Include);

        if (existingWindow != null)
            return;

        GameObject windowObject = new(DebugWindowObjectName);
        windowObject.AddComponent<BattleEffectDebugWindow>();
        windowObject.AddComponent<BattleDebugKillAllMonsters>();
        windowObject.AddComponent<DebugBattleTargetController>();
        windowObject.AddComponent<DebugBattleSceneRunner>();

        Debug.Log("[DebugBattleSceneBootstrap] Debug battle window created.");
    }
}

public sealed class DebugBattleSceneRunner : MonoBehaviour
{
    private IEnumerator Start()
    {
        yield return null;
        yield return null;
        OpenBattleRoom();
    }

    private void OpenBattleRoom()
    {
        BattleRoomLoader loader = Object.FindFirstObjectByType<BattleRoomLoader>(
            FindObjectsInactive.Include);

        if (loader == null)
        {
            Debug.LogWarning("[DebugBattleSceneRunner] BattleRoomLoader not found.");
            return;
        }

        if (!TryPrepareDebugBattleRuntimeData())
            return;

        BattleSceneController sceneController = Object.FindFirstObjectByType<BattleSceneController>(
            FindObjectsInactive.Include);

        if (sceneController != null)
            sceneController.enabled = false;

        BattleMapPanel mapPanel = Object.FindFirstObjectByType<BattleMapPanel>(
            FindObjectsInactive.Include);

        if (mapPanel != null)
            mapPanel.Close();

        GameObject battleRoomRoot = ResolveBattleRoomRoot(loader);

        if (battleRoomRoot != null && !battleRoomRoot.activeSelf)
            battleRoomRoot.SetActive(true);

        loader.ConfigureDebugTargetMonster(
            DebugBattleSceneBootstrap.DebugTargetMonsterId,
            DebugBattleSceneBootstrap.DebugTargetGridIndex);
        loader.ResetLoadedStateForNextBattle(true);
        loader.RequestLoadBattle();
    }

    private static bool TryPrepareDebugBattleRuntimeData()
    {
        DataManager dataManager = DataManager.Instance;

        if (dataManager == null)
        {
            Debug.LogWarning("[DebugBattleSceneRunner] DataManager not found.");
            return false;
        }

        dataManager.Initialize();

        if (!DebugBattlePartySetup.TryCreateDefaultParty(dataManager))
        {
            Debug.LogError("[DebugBattleSceneRunner] Failed to create the default debug party.");
            return false;
        }

        return true;
    }

    private static GameObject ResolveBattleRoomRoot(BattleRoomLoader loader)
    {
        if (loader == null)
            return null;

        Transform current = loader.transform;

        while (current.parent != null &&
               !string.Equals(current.name, "BattleRoom", System.StringComparison.Ordinal))
        {
            current = current.parent;
        }

        return current.gameObject;
    }
}

public static class DebugBattleSkillCastUtility
{
    public const string DefaultSkillId = "S_Ability_11";
    public const int DefaultTimelineSlotIndex = 0;
    public const int DefaultDebugTargetGridIndex = 23;

    private const string GeneralSelectionRangeId = "Range_24";

    public static bool TryCastSkillNow(
        CharacterRuntimeData runtime,
        string skillId,
        int timelineSlotIndex,
        int preferredTargetGridIndex,
        bool forcePreferredTargetGrid,
        bool clearExistingReservations,
        bool refillResourcesBeforeCast,
        out string message)
    {
        message = string.Empty;

        if (runtime == null)
        {
            message = "스킬을 시전할 캐릭터가 없습니다.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(skillId))
        {
            message = "SkillId가 비어 있습니다.";
            return false;
        }

        DataManager dataManager = DataManager.Instance;
        if (dataManager == null || dataManager.SkillDatabase == null)
        {
            message = "DataManager 또는 SkillDatabase가 없습니다.";
            return false;
        }

        if (!dataManager.SkillDatabase.TryGet(skillId.Trim(), out SkillMasterData skillData) ||
            skillData == null)
        {
            message = $"SkillData를 찾을 수 없습니다: {skillId}";
            return false;
        }

        BattleTimelineController timelineController =
            Object.FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);
        BattleTurnExecutor turnExecutor =
            Object.FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);
        GridManager gridManager =
            Object.FindFirstObjectByType<GridManager>(FindObjectsInactive.Include);

        if (timelineController == null)
        {
            message = "BattleTimelineController가 없습니다.";
            return false;
        }

        if (turnExecutor == null)
        {
            message = "BattleTurnExecutor가 없습니다.";
            return false;
        }

        if (gridManager == null)
        {
            message = "GridManager가 없습니다.";
            return false;
        }

        if (!turnExecutor.CanAcceptPlayerInput)
        {
            message = "전투가 아직 입력 가능한 상태가 아닙니다.";
            return false;
        }

        if (timelineController.SlotCount <= 0)
        {
            message = "사용 가능한 타임라인 슬롯이 없습니다.";
            return false;
        }

        int safeSlotIndex = Mathf.Clamp(
            timelineSlotIndex,
            0,
            timelineController.SlotCount - 1);

        if (clearExistingReservations)
            timelineController.ClearAllReservations();

        if (refillResourcesBeforeCast)
        {
            BattleEffectDebugTool.SetFullResources(runtime);
            BattleEffectDebugTool.RefreshBattle();
        }

        int casterGridIndex = timelineController.GetPreviewGridIndexAtSlotEnd(runtime, safeSlotIndex);
        BattleDirection casterDirection = timelineController.GetPreviewDirection(runtime, safeSlotIndex);

        if (!TryCreatePlayerSkillCommand(
                runtime,
                skillData,
                gridManager,
                dataManager.RangeDatabase,
                casterGridIndex,
                casterDirection,
                preferredTargetGridIndex,
                forcePreferredTargetGrid,
                out PlayerReservedCommand command,
                out message))
        {
            return false;
        }

        timelineController.SelectCharacter(runtime);
        timelineController.SelectTimelineSlotFromNetwork(safeSlotIndex, false, false);

        if (!timelineController.ConfirmPlayerCommand(safeSlotIndex, command))
        {
            message = $"스킬 예약에 실패했습니다: {skillData.SkillId}";
            return false;
        }

        turnExecutor.ExecuteTurn();
        message = $"스킬 시전 요청 완료: {runtime.CharacterId} / {skillData.SkillId}";
        return true;
    }

    public static bool TryCreatePlayerSkillCommand(
        CharacterRuntimeData runtime,
        SkillMasterData skillData,
        GridManager gridManager,
        RangeDatabase rangeDatabase,
        int casterGridIndex,
        BattleDirection casterDirection,
        int preferredTargetGridIndex,
        bool forcePreferredTargetGrid,
        out PlayerReservedCommand command,
        out string error)
    {
        command = null;
        error = string.Empty;

        if (runtime == null)
        {
            error = "캐릭터 런타임 데이터가 없습니다.";
            return false;
        }

        if (skillData == null)
        {
            error = "스킬 데이터가 없습니다.";
            return false;
        }

        if (gridManager == null)
        {
            error = "GridManager가 없습니다.";
            return false;
        }

        if (IsMoveSkill(skillData))
        {
            error = "디버그 스킬 시전은 이동 스킬을 지원하지 않습니다.";
            return false;
        }

        if (!IsValidGridIndex(gridManager, casterGridIndex))
        {
            error = $"시전자 그리드가 올바르지 않습니다: {casterGridIndex}";
            return false;
        }

        command = new PlayerReservedCommand(runtime, skillData);

        switch (skillData.RangeType)
        {
            case RangeType.None:
                return true;

            case RangeType.Direction:
                List<int> directionRange = BuildDirectionRange(
                    runtime,
                    skillData,
                    gridManager,
                    rangeDatabase,
                    casterGridIndex,
                    casterDirection);
                command.SetDirectionResult(casterDirection, directionRange, directionRange);
                return true;

            case RangeType.Selection:
                return TryConfigureSelectionCommand(
                    command,
                    runtime,
                    skillData,
                    gridManager,
                    rangeDatabase,
                    casterGridIndex,
                    casterDirection,
                    preferredTargetGridIndex,
                    forcePreferredTargetGrid,
                    out error);
        }

        error = $"지원하지 않는 RangeType입니다: {skillData.RangeType}";
        command = null;
        return false;
    }

    private static bool TryConfigureSelectionCommand(
        PlayerReservedCommand command,
        CharacterRuntimeData runtime,
        SkillMasterData skillData,
        GridManager gridManager,
        RangeDatabase rangeDatabase,
        int casterGridIndex,
        BattleDirection casterDirection,
        int preferredTargetGridIndex,
        bool forcePreferredTargetGrid,
        out string error)
    {
        error = string.Empty;

        string rangeId = BattleEquipmentEffectService.GetEffectiveRangeId(runtime, skillData);
        List<int> rangeGridIndices;
        int selectedGridIndex;

        if (BattleRangeCalculator.IsAllRangeId(rangeId))
        {
            selectedGridIndex = casterGridIndex;
            rangeGridIndices = BattleRangeCalculator.GetAllGridIndices(gridManager);
        }
        else
        {
            if (!TryChooseSelectionGrid(
                    gridManager,
                    rangeDatabase,
                    casterGridIndex,
                    preferredTargetGridIndex,
                    forcePreferredTargetGrid,
                    out selectedGridIndex))
            {
                error = "선택 가능한 스킬 타겟 그리드를 찾을 수 없습니다.";
                return false;
            }

            rangeGridIndices = BattleRangeCalculator.GetSelectionRangeIndices(
                selectedGridIndex,
                rangeId,
                rangeDatabase,
                gridManager);
        }

        if (rangeGridIndices.Count <= 0)
        {
            error = $"스킬 범위를 계산할 수 없습니다: {skillData.RangeId}";
            return false;
        }

        command.SetSelectionAreaResult(
            casterDirection,
            selectedGridIndex,
            rangeGridIndices);
        return true;
    }

    private static bool TryChooseSelectionGrid(
        GridManager gridManager,
        RangeDatabase rangeDatabase,
        int casterGridIndex,
        int preferredTargetGridIndex,
        bool forcePreferredTargetGrid,
        out int selectedGridIndex)
    {
        selectedGridIndex = -1;

        if (forcePreferredTargetGrid && IsValidGridIndex(gridManager, preferredTargetGridIndex))
        {
            selectedGridIndex = preferredTargetGridIndex;
            return true;
        }

        List<int> selectableGridIndices = BattleRangeCalculator.GetSelectionRangeIndices(
            casterGridIndex,
            GeneralSelectionRangeId,
            rangeDatabase,
            gridManager);

        if (selectableGridIndices.Count <= 0)
            return false;

        if (selectableGridIndices.Contains(preferredTargetGridIndex))
        {
            selectedGridIndex = preferredTargetGridIndex;
            return true;
        }

        selectedGridIndex = selectableGridIndices[0];
        return true;
    }

    private static List<int> BuildDirectionRange(
        CharacterRuntimeData runtime,
        SkillMasterData skillData,
        GridManager gridManager,
        RangeDatabase rangeDatabase,
        int casterGridIndex,
        BattleDirection casterDirection)
    {
        string rangeId = BattleEquipmentEffectService.GetEffectiveRangeId(runtime, skillData);
        return BattleRangeCalculator.GetDirectionRangeIndices(
            casterGridIndex,
            rangeId,
            casterDirection,
            rangeDatabase,
            gridManager);
    }

    private static bool IsMoveSkill(SkillMasterData skillData)
    {
        if (skillData == null)
            return false;

        return skillData.Category == Category.Move ||
               skillData.TimelineNotation == TimelineActionType.Move ||
               skillData.SkillId == "S_Move_1" ||
               skillData.SkillId == "S_Move_2";
    }

    private static bool IsValidGridIndex(GridManager gridManager, int gridIndex)
    {
        if (gridManager == null || gridIndex < 0)
            return false;

        int cellCount = gridManager.Width * gridManager.Height;
        if (gridIndex >= cellCount)
            return false;

        return gridManager.IsValidCoord(gridManager.IndexToCoord(gridIndex));
    }
}
