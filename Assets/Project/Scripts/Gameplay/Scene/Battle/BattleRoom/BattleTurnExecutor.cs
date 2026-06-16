using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleTurnExecutor : MonoBehaviour
{
    public static event Action PlayerTurnReturned;

    [SerializeField] private BattleTimelineController timelineController;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private MoveGhostPreview moveGhostPreview;
    [SerializeField] private BattleRoomLoader roomLoader;
    [SerializeField] private BattleMonsterSpawner monsterSpawner;

    [Header("Safe Execution")]
    [SerializeField] private bool useSafeSequentialExecution = true;
    [SerializeField] private float actionRoutineTimeout = 8f;

    [Header("Intro Text")]
    [SerializeField] private string battleProgressMessage = "전투 진행";

    [Header("SFX")]
    [SerializeField] private bool playBattleProgressSfx = true;
    [SerializeField] private SfxType battleProgressSfxType = SfxType.BattleProgressText;
    [SerializeField, Range(0f, 1f)] private float battleProgressSfxVolume = 1f;

    private bool isExecuting;

    private readonly BattleUniqueResourceService uniqueResourceService = new();
    private readonly BattlePassiveSkillService passiveSkillService = new();
    private void OnEnable()
    {
        BattleEffectUtility.OnPlayerDamaged -= uniqueResourceService.OnPlayerDamaged;
        BattleEffectUtility.OnPlayerDamaged += uniqueResourceService.OnPlayerDamaged;
    }

    private void OnDisable()
    {
        BattleEffectUtility.OnPlayerDamaged -= uniqueResourceService.OnPlayerDamaged;
    }

    public void ExecuteTurn()
    {
        if (isExecuting)
        {
            ShowBattleWarning("이미 턴을 실행 중입니다.");
            Debug.LogWarning("[BattleTurnExecutor] Already executing.");
            return;
        }

        if (timelineController == null)
        {
            ShowBattleWarning("타임라인 컨트롤러를 찾을 수 없습니다.");
            return;
        }

        timelineController.ClearSelectedSlotSelection();
        timelineController.SetSlotSelectionLocked(true);

        StartCoroutine(ExecuteTurnRoutine());
    }

    private IEnumerator ExecuteTurnRoutine()
    {
        isExecuting = true;

        try
        {
            if (moveGhostPreview != null)
                moveGhostPreview.ClearAll();

            BattleActionBatchBuilder builder = new(gridManager);
            BattleActionRunner runner = new(gridManager, monsterSpawner, roomLoader, useSafeSequentialExecution, actionRoutineTimeout);
            BattleActionSimulationService simulator = new(gridManager);

            uniqueResourceService.ApplyTimelineSlotResourceGain(timelineController);

            simulator.Simulate(timelineController);

            List<BattleActionBatch> batches = builder.Build(timelineController);

            if (batches == null || batches.Count <= 0)
            {
                ShowBattleWarning("실행할 행동이 없습니다.");
                yield break;
            }

            yield return ShowBattleProgressIntroTextRoutine();

            int slidThroughSlotIndex = -1;

            for (int i = 0; i < batches.Count; i++)
            {
                BattleActionBatch batch = batches[i];

                if (!BatchHasCommands(batch))
                    continue;

                int currentSlotIndex = GetBatchTimelineSlotIndex(batch, i);

                if (currentSlotIndex > slidThroughSlotIndex + 1)
                {
                    int beforeActionSlideThroughSlotIndex = currentSlotIndex - 1;
                    yield return timelineController.SlideTimelineSlotsLeftThroughSlotRoutine(beforeActionSlideThroughSlotIndex);
                    slidThroughSlotIndex = Mathf.Max(slidThroughSlotIndex, beforeActionSlideThroughSlotIndex);
                }

                yield return runner.RunBatch(batch);

                if (BattleResultChecker.Instance != null &&
                    BattleResultChecker.Instance.CheckBattleEnd())
                {
                    ClearTimeline();
                    yield break;
                }

                if (HasNextExecutableBatchInSameTimelineSlot(batches, i + 1, currentSlotIndex))
                    continue;

                int slideThroughSlotIndex = GetSlideThroughSlotIndexAfterExecutedTimelineSlot(batches, i, currentSlotIndex);

                if (slideThroughSlotIndex > slidThroughSlotIndex)
                {
                    yield return timelineController.SlideTimelineSlotsLeftThroughSlotRoutine(slideThroughSlotIndex);
                    slidThroughSlotIndex = slideThroughSlotIndex;
                }
            }

            runner.ApplyTurnEndEffects();

            ClearTimeline();
            yield return null;

            if (BattleResultChecker.Instance != null &&
                BattleResultChecker.Instance.CheckBattleEnd())
            {
                yield break;
            }

            yield return timelineController.ResetTimelineSlotsToOriginalPositionRoutine();

            if (roomLoader != null)
            {
                roomLoader.RecoverPlayerCostsToMax();

                passiveSkillService.ClearAllPlayerPassiveEffects();

                roomLoader.PlanNextMonsterTurns();

                passiveSkillService.RefreshAllPlayerPassives();

                roomLoader.RefreshBattleHUDs();
            }
        }
        finally
        {
            if (timelineController != null)
                timelineController.SetSlotSelectionLocked(false);

            isExecuting = false;
            PlayerTurnReturned?.Invoke();
        }
    }


    public void RefreshBattleHUDs()
    {
        MonsterHUDSlot[] monsterHuds = FindObjectsByType<MonsterHUDSlot>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < monsterHuds.Length; i++)
        {
            if (monsterHuds[i] != null)
                monsterHuds[i].Refresh();
        }
    }

    private IEnumerator ShowBattleProgressIntroTextRoutine()
    {
        PlaySfx(playBattleProgressSfx, battleProgressSfxType, battleProgressSfxVolume);
        yield return BattleMapIntroText.ShowMessageAndWait(battleProgressMessage);
    }

    private int GetNextExecutableBatchIndex(List<BattleActionBatch> batches, int startIndex)
    {
        if (batches == null)
            return -1;

        for (int i = Mathf.Max(0, startIndex); i < batches.Count; i++)
        {
            if (BatchHasCommands(batches[i]))
                return i;
        }

        return -1;
    }

    private int GetBatchTimelineSlotIndex(BattleActionBatch batch, int fallbackIndex)
    {
        int slotCount = timelineController != null ? timelineController.SlotCount : 0;

        if (batch != null && batch.TimelineSlotIndex >= 0)
            return slotCount > 0
                ? Mathf.Clamp(batch.TimelineSlotIndex, 0, slotCount - 1)
                : batch.TimelineSlotIndex;

        return slotCount > 0
            ? Mathf.Clamp(fallbackIndex, 0, slotCount - 1)
            : fallbackIndex;
    }

    private bool HasNextExecutableBatchInSameTimelineSlot(
        List<BattleActionBatch> batches,
        int startIndex,
        int timelineSlotIndex)
    {
        int nextExecutableBatchIndex = GetNextExecutableBatchIndex(batches, startIndex);

        if (nextExecutableBatchIndex < 0)
            return false;

        int nextTimelineSlotIndex = GetBatchTimelineSlotIndex(
            batches[nextExecutableBatchIndex],
            nextExecutableBatchIndex
        );

        return nextTimelineSlotIndex == timelineSlotIndex;
    }

    private int GetSlideThroughSlotIndexAfterExecutedTimelineSlot(
        List<BattleActionBatch> batches,
        int executedBatchIndex,
        int executedTimelineSlotIndex)
    {
        int nextExecutableBatchIndex = GetNextExecutableBatchIndex(batches, executedBatchIndex + 1);

        if (nextExecutableBatchIndex >= 0)
        {
            int nextTimelineSlotIndex = GetBatchTimelineSlotIndex(
                batches[nextExecutableBatchIndex],
                nextExecutableBatchIndex
            );

            return Mathf.Max(executedTimelineSlotIndex, nextTimelineSlotIndex - 1);
        }

        if (timelineController != null && timelineController.SlotCount > 0)
            return timelineController.SlotCount - 1;

        return executedTimelineSlotIndex;
    }

    private bool BatchHasCommands(BattleActionBatch batch)
    {
        if (batch == null)
            return false;

        bool hasPlayerCommand =
            batch.PlayerCommands != null &&
            batch.PlayerCommands.Count > 0;

        bool hasMonsterCommand =
            batch.MonsterCommands != null &&
            batch.MonsterCommands.Count > 0;

        return hasPlayerCommand || hasMonsterCommand;
    }

    private void PlaySfx(bool play, SfxType sfxType, float volume)
    {
        if (!play)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(sfxType, volume);
    }

    private void ShowBattleWarning(string message)
    {
        BattleWarningUI.ShowMessage(message);
    }

    private void ClearTimeline()
    {
        if (timelineController != null)
            timelineController.ClearAllReservations();
    }
}