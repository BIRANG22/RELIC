using System;
using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleTurnExecutor : MonoBehaviour
{
    public static event Action PlayerTurnReturned;

    public bool CanAcceptPlayerInput => !isExecuting && isMonsterPlanReady && isPlayerInputReady;

    [SerializeField] private BattleTimelineController timelineController;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private MoveGhostPreview moveGhostPreview;
    [SerializeField] private BattleRoomLoader roomLoader;
    [SerializeField] private BattleMonsterSpawner monsterSpawner;

    [Header("End Turn")]
    [SerializeField] private Button endTurnButton;

    [Header("Keyboard Input")]
    [SerializeField] private bool enableSpaceEndTurnInput = true;

    [Header("Safe Execution")]
    [SerializeField] private bool useSafeSequentialExecution = true;
    [SerializeField] private float actionRoutineTimeout = 8f;

    [Header("Intro Text")]
    [SerializeField] private string battleProgressMessage = "전투 진행";
    [SerializeField] private bool waitIntroText = false;
    [SerializeField] private float introTextTimeout = 1.5f;

    [Header("SFX")]
    [SerializeField] private bool playBattleProgressSfx = true;
    [SerializeField] private SfxType battleProgressSfxType = SfxType.BattleProgressText;
    [SerializeField, Range(0f, 1f)] private float battleProgressSfxVolume = 1f;

    private bool isMonsterPlanReady;
    private bool isPlayerInputReady;
    private bool isExecuting;
    private int playerTurnNumber = 1;

    private readonly BattleUniqueResourceService uniqueResourceService = new();
    private readonly BattlePassiveSkillService passiveSkillService = new();

    private void Start()
    {
        RefreshEndTurnButton();
        RefreshBattlePresentationState();
    }

    private void OnEnable()
    {
        BattleEffectUtility.OnPlayerDamaged -= uniqueResourceService.OnPlayerDamaged;
        BattleEffectUtility.OnPlayerDamaged += uniqueResourceService.OnPlayerDamaged;
    }

    private void OnDisable()
    {
        BattleEffectUtility.OnPlayerDamaged -= uniqueResourceService.OnPlayerDamaged;
    }

    public void SetBattleInputReady(bool ready)
    {
        isMonsterPlanReady = ready;
        isPlayerInputReady = ready;

        if (ready)
            ApplyPlayerTurnStartEquipmentEffects();

        RefreshEndTurnButton();
        RefreshBattlePresentationState();
    }

    public void ResetBattleTurnState()
    {
        playerTurnNumber = 1;
    }

    private void Update()
    {
        if (!enableSpaceEndTurnInput)
            return;

        if (!Input.GetKeyDown(KeyCode.Space))
            return;

        if (IsTypingInputFieldSelected())
            return;

        if (!CanAcceptPlayerInput)
            return;

        ExecuteTurn();
    }

    public void SetMonsterPlanReady(bool ready)
    {
        isMonsterPlanReady = ready;
        RefreshEndTurnButton();
        RefreshBattlePresentationState();
    }

    public void SetPlayerInputReady(bool ready)
    {
        isPlayerInputReady = ready;
        RefreshEndTurnButton();
        RefreshBattlePresentationState();
    }

    public void ExecuteTurn()
    {
        Debug.Log(
       $"[EndTurnCheck] isExecuting:{isExecuting} / " +
       $"MonsterReady:{isMonsterPlanReady} / " +
       $"PlayerReady:{isPlayerInputReady}"
   );

        if (isExecuting)
            return;

        if (!isMonsterPlanReady || !isPlayerInputReady)
        {
            ShowBattleWarning("아직 행동 준비가 완료되지 않았습니다.");
            return;
        }

        if (timelineController == null)
        {
            ShowBattleWarning("타임라인 컨트롤러를 찾을 수 없습니다.");
            return;
        }

        MonsterUnit.HideAllTemporaryHUDs();

        isExecuting = true;

        isMonsterPlanReady = false;
        isPlayerInputReady = false;
        RefreshEndTurnButton();
        RefreshBattlePresentationState();

        if (endTurnButton != null)
            endTurnButton.interactable = false;

        timelineController.ClearSelectedSlotSelection();
        timelineController.SetSlotSelectionLocked(true);

        StartCoroutine(ExecuteTurnRoutine());
    }

    private IEnumerator ExecuteTurnRoutine()
    {
        bool battleEnded = false;

        try
        {
            if (moveGhostPreview != null)
                moveGhostPreview.ClearAll();

            BattleActionBatchBuilder builder = new(gridManager);
            BattleActionRunner runner = new(
                gridManager,
                monsterSpawner,
                roomLoader,
                useSafeSequentialExecution,
                actionRoutineTimeout
            );
            BattleActionSimulationService simulator = new(gridManager);

            uniqueResourceService.ApplyTimelineSlotResourceGain(timelineController);

            simulator.Simulate(timelineController);

            List<BattleActionBatch> batches = builder.Build(timelineController);

            yield return ShowBattleProgressIntroTextRoutineSafe();

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

                    if (timelineController != null)
                    {
                        yield return timelineController.SlideTimelineSlotsLeftThroughSlotRoutine(
                            beforeActionSlideThroughSlotIndex
                        );
                    }

                    slidThroughSlotIndex = Mathf.Max(slidThroughSlotIndex, beforeActionSlideThroughSlotIndex);
                }

                bool keepCameraAfterBatch =
                    runner.BatchHasCrossSideHitAction(batch) &&
                    NextExecutableBatchHasCrossSideHitAction(batches, i + 1, runner);

                yield return runner.RunBatch(batch, keepCameraAfterBatch);

                if (BattleResultChecker.Instance != null &&
                    BattleResultChecker.Instance.CheckBattleEnd())
                {
                    battleEnded = true;
                    ClearTimeline();
                    yield break;
                }

                if (HasNextExecutableBatchInSameTimelineSlot(batches, i + 1, currentSlotIndex))
                    continue;

                int slideThroughSlotIndex =
                    GetSlideThroughSlotIndexAfterExecutedTimelineSlot(batches, i, currentSlotIndex);

                if (slideThroughSlotIndex > slidThroughSlotIndex)
                {
                    if (timelineController != null)
                        yield return timelineController.SlideTimelineSlotsLeftThroughSlotRoutine(slideThroughSlotIndex);

                    slidThroughSlotIndex = slideThroughSlotIndex;
                }
            }

            yield return runner.ReturnCameraDefaultIfNeeded();

            yield return runner.ApplyTurnEndEffectsRoutine();

            ClearTimeline();
            yield return null;

            if (BattleResultChecker.Instance != null &&
                BattleResultChecker.Instance.CheckBattleEnd())
            {
                battleEnded = true;
                yield break;
            }

            playerTurnNumber++;

            if (timelineController != null)
                yield return timelineController.ResetTimelineSlotsToOriginalPositionRoutine();

            if (roomLoader != null)
            {
                roomLoader.RecoverPlayerCostsToMax();

                passiveSkillService.ClearAllPlayerPassiveEffects();

                yield return roomLoader.PlanNextMonsterTurnsRoutine();

                passiveSkillService.RefreshAllPlayerPassives();

                roomLoader.RefreshBattleHUDs();
            }
        }
        finally
        {
            if (timelineController != null)
                timelineController.SetSlotSelectionLocked(false);

            isExecuting = false;

            RefreshEndTurnButton();
            RefreshBattlePresentationState();

            PlayerTurnReturned?.Invoke();
        }
    }

    private bool IsTypingInputFieldSelected()
    {
        if (EventSystem.current == null)
            return false;

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;

        if (selectedObject == null)
            return false;

        if (selectedObject.GetComponent<TMPro.TMP_InputField>() != null)
            return true;

        if (selectedObject.GetComponent<InputField>() != null)
            return true;

        return false;
    }

    private void RefreshEndTurnButton()
    {
        if (endTurnButton == null)
            return;

        endTurnButton.interactable =
            !isExecuting &&
            isMonsterPlanReady &&
            isPlayerInputReady;
    }

    private void RefreshBattlePresentationState()
    {
        bool isReservationState = CanAcceptPlayerInput;

        if (gridManager != null)
            gridManager.SetGridVisible(isReservationState);

        MonsterUnit.SetAllReservationVisualState(isReservationState);
    }

    private void ApplyPlayerTurnStartEquipmentEffects()
    {
        if (DataManager.Instance == null ||
            DataManager.Instance.PartyRuntimeStore == null ||
            DataManager.Instance.CharacterRuntimeStore == null)
        {
            return;
        }

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            string characterId = partyStore.GetCharacterId(i);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (!DataManager.Instance.CharacterRuntimeStore.TryGet(
                characterId,
                out CharacterRuntimeData runtime))
            {
                continue;
            }

            BattleEquipmentEffectService.ApplyPlayerTurnStartEffects(
                runtime,
                playerTurnNumber);
        }
    }

    private IEnumerator ShowBattleProgressIntroTextRoutineSafe()
    {
        PlaySfx(playBattleProgressSfx, battleProgressSfxType, battleProgressSfxVolume);

        IEnumerator routine = BattleMapIntroText.ShowMessageAndWait(battleProgressMessage);

        if (routine != null)
            StartCoroutine(routine);

        yield break;
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


    private bool NextExecutableBatchHasCrossSideHitAction(
        List<BattleActionBatch> batches,
        int startIndex,
        BattleActionRunner runner)
    {
        if (batches == null || runner == null)
            return false;

        int nextExecutableBatchIndex = GetNextExecutableBatchIndex(batches, startIndex);

        if (nextExecutableBatchIndex < 0)
            return false;

        return runner.BatchHasCrossSideHitAction(batches[nextExecutableBatchIndex]);
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
