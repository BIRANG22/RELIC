using System;
using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleTurnExecutor : MonoBehaviour
{
    public static event Action BattleExecutionStarted;
    public static event Action PlayerTurnReturned;

    public bool CanAcceptPlayerInput => !networkExecutionLocked && !isExecuting && isMonsterPlanReady && isPlayerInputReady;
    public bool IsExecuting => isExecuting;
    public Button EndTurnButton => endTurnButton;

    [SerializeField] private BattleTimelineController timelineController;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private MoveGhostPreview moveGhostPreview;
    [SerializeField] private BattleRoomLoader roomLoader;
    [SerializeField] private BattleMonsterSpawner monsterSpawner;
    [SerializeField] private SkillListPanel skillListPanel;

    [Header("Battle Execution UI Roots")]
    [SerializeField] private GameObject playerHudRoot;
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private bool autoFindBattleExecutionUiRoots = true;
    [SerializeField] private string playerHudRootObjectName = "PlayerHUD_Root";
    [SerializeField] private string menuRootObjectName = "MenuRoot";

    [Header("End Turn")]
    [SerializeField] private Button endTurnButton;

    [Header("End Turn Visual Feedback")]
    [SerializeField] private Image endTurnLineImage;
    [SerializeField] private GameObject endTurnBackground2;
    [SerializeField] private Color endTurnLineHoverColor = new Color32(0x4E, 0x66, 0xDF, 0xFF);
    [SerializeField, Min(0f)] private float endTurnClickFeedbackDuration = 0.15f;

    [Header("Turn Text")]
    [SerializeField] private TMP_Text turnNumberText;
    [SerializeField] private bool autoFindTurnNumberText = true;
    [SerializeField] private string turnNumberTextRootObjectName = "TurnText";
    [SerializeField] private string turnNumberTextValueObjectName = "Value";
    [SerializeField] private string turnNumberTextObjectName = "TURN_TEXT2";

    [Header("Keyboard Input")]
    [SerializeField] private bool enableSpaceEndTurnInput = true;

    [Header("Safe Execution")]
    [SerializeField] private bool useSafeSequentialExecution = true;
    [SerializeField] private float actionRoutineTimeout = 8f;

    [Header("Consecutive Action Presentation")]
    [SerializeField, Min(1f)] private float consecutiveActionSpeedMultiplier = 1.2f;

    [Header("Multi Hit Presentation")]
    [SerializeField, Min(0f)] private float multiHitActionInterval = 0.12f;

    [Header("Intro Text")]
    [SerializeField] private string battleProgressMessage = "전투 진행";
    [SerializeField] private bool waitIntroText = false;
    [SerializeField] private float introTextTimeout = 1.5f;

    [Header("SFX")]
    [SerializeField] private bool playBattleProgressSfx = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string battleProgressSfxId = AudioIds.Sfx.BattleProgressText;
    [SerializeField, Range(0f, 1f)] private float battleProgressSfxVolume = 1f;

    private bool isMonsterPlanReady;
    private bool isPlayerInputReady;
    private bool isExecuting;
    private bool networkExecutionLocked;
    private bool battleExecutionUiSuppressed;
    private Coroutine executeTurnCoroutine;
    private Coroutine endTurnClickFeedbackCoroutine;
    private int playerTurnNumber = 1;
    private Color endTurnLineDefaultColor = Color.white;
    private bool endTurnVisualFeedbackInitialized;
    private CharacterRuntimeData selectedCharacterBeforeExecution;

    private readonly BattleUniqueResourceService uniqueResourceService = new();
    private readonly BattlePassiveSkillService passiveSkillService = new();
    private readonly Dictionary<string, int> pendingNextTurnSwiftByCharacterId = new();

    private void Start()
    {
        AutoFindTurnNumberTextIfNeeded();
        InitializeEndTurnVisualFeedback();
        RefreshTurnNumberText();
        RefreshEndTurnButton();
        RefreshBattlePresentationState();
        SteamBattleStateSynchronizer.EnsureForBattleScene(this, timelineController);
    }


    private void EnsureSkillListPanel()
    {
        if (skillListPanel != null)
            return;

        skillListPanel = FindFirstObjectByType<SkillListPanel>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        BattleEffectUtility.OnPlayerHit -= uniqueResourceService.OnAnyPlayerDamaged;
        BattleEffectUtility.OnPlayerHit += uniqueResourceService.OnAnyPlayerDamaged;
        BattleEffectUtility.OnPlayerBuffApplied -= uniqueResourceService.OnPlayerBuffApplied;
        BattleEffectUtility.OnPlayerBuffApplied += uniqueResourceService.OnPlayerBuffApplied;
        BattleEffectUtility.OnPlayerDamagedEnemy -= uniqueResourceService.OnPlayerDamagedEnemy;
        BattleEffectUtility.OnPlayerDamagedEnemy += uniqueResourceService.OnPlayerDamagedEnemy;
    }

    private void OnDisable()
    {
        BattleEffectUtility.OnPlayerHit -= uniqueResourceService.OnAnyPlayerDamaged;
        BattleEffectUtility.OnPlayerBuffApplied -= uniqueResourceService.OnPlayerBuffApplied;
        BattleEffectUtility.OnPlayerDamagedEnemy -= uniqueResourceService.OnPlayerDamagedEnemy;

        if (endTurnClickFeedbackCoroutine != null)
        {
            StopCoroutine(endTurnClickFeedbackCoroutine);
            endTurnClickFeedbackCoroutine = null;
        }

        RestoreEndTurnLineColor();
        SetEndTurnBackground2Visible(false);
    }

    public void QueueNextTurnSwift(BattleCharacter target, int value, int count)
    {
        if (target == null || target.RuntimeData == null || target.RuntimeData.IsDead)
            return;

        string characterId = target.RuntimeData.CharacterId;

        if (string.IsNullOrWhiteSpace(characterId))
            return;

        int stack = BattleEffectUtility.GetRepeatedValue(value, count);

        if (stack <= 0)
            return;

        pendingNextTurnSwiftByCharacterId.TryGetValue(characterId, out int queuedStack);
        pendingNextTurnSwiftByCharacterId[characterId] = queuedStack + stack;
    }

    private void ApplyQueuedNextTurnSwift()
    {
        if (pendingNextTurnSwiftByCharacterId.Count <= 0)
            return;

        BattleCharacter[] characters = FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null || character.RuntimeData.IsDead)
                continue;

            string characterId = character.RuntimeData.CharacterId;

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (!pendingNextTurnSwiftByCharacterId.TryGetValue(characterId, out int stack))
                continue;

            if (stack <= 0)
                continue;

            BattleEffectUtility.AddStatusToPlayer(character, "E_Swift", stack, 1);
        }

        pendingNextTurnSwiftByCharacterId.Clear();
    }

    public void SetBattleInputReady(bool ready)
    {
        isMonsterPlanReady = ready;
        isPlayerInputReady = ready;

        if (ready)
            ApplyPlayerTurnStartEquipmentEffects();

        RefreshEndTurnButton();
        RefreshBattlePresentationState();
        RefreshBattleExecutionUiVisibility();

        if (ready && timelineController != null)
            timelineController.SelectDefaultSlotWhenInputReady();
    }

    public void ResetBattleTurnState()
    {
        playerTurnNumber = 1;
        pendingNextTurnSwiftByCharacterId.Clear();
        RefreshTurnNumberText();
    }

    public void ForceStopBattleExecutionForRoomEnd()
    {
        if (executeTurnCoroutine != null)
        {
            StopCoroutine(executeTurnCoroutine);
            executeTurnCoroutine = null;
        }

        isExecuting = false;
        isMonsterPlanReady = false;
        isPlayerInputReady = false;
        networkExecutionLocked = false;
        pendingNextTurnSwiftByCharacterId.Clear();

        EnsureSkillListPanel();
        if (skillListPanel != null)
            skillListPanel.CloseForBattleExecution();

        if (moveGhostPreview != null)
            moveGhostPreview.ClearAll();

        if (timelineController != null)
        {
            timelineController.SetSlotSelectionLocked(false);
            timelineController.SetSelectedCharacterScaleFeedbackActive(false);
            timelineController.ClearAllReservations();
            timelineController.ResetTimelineBarsForNewBattleRoom();
        }

        ResetBattleEffectPlanesForRoomEnd();

        RefreshEndTurnButton();
        RefreshBattlePresentationState();
        RefreshBattleExecutionUiVisibility();
    }

    public void RestoreBattleExecutionUiAfterRoomEnd()
    {
        if (!battleExecutionUiSuppressed)
            return;

        battleExecutionUiSuppressed = false;
        SetBattleExecutionUiVisible(true);
    }

    public void SuppressBattleExecutionUiUntilPlayerInputReady()
    {
        HideBattleExecutionUiUntilPlayerTurn();
    }

    private void ResetBattleEffectPlanesForRoomEnd()
    {
        BattleEffectPlaneSlideController[] planeControllers = UnityEngine.Object.FindObjectsByType<BattleEffectPlaneSlideController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < planeControllers.Length; i++)
        {
            if (planeControllers[i] == null)
                continue;

            planeControllers[i].ForceResetToReservePositionInstant();
        }
    }

    private void Update()
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

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

    public void SetNetworkExecutionLocked(bool locked)
    {
        if (networkExecutionLocked == locked)
            return;

        networkExecutionLocked = locked;
        RefreshEndTurnButton();
        RefreshBattlePresentationState();
        RefreshBattleExecutionUiVisibility();
    }

    public void SetMonsterPlanReady(bool ready)
    {
        isMonsterPlanReady = ready;
        RefreshEndTurnButton();
        RefreshBattlePresentationState();
        RefreshBattleExecutionUiVisibility();
    }

    public void SetPlayerInputReady(bool ready)
    {
        isPlayerInputReady = ready;
        RefreshEndTurnButton();
        RefreshBattlePresentationState();
        RefreshBattleExecutionUiVisibility();
    }

    public void ExecuteTurn()
    {
        if (SteamBattleStateSynchronizer.TryHandleExecuteTurnRequest(this))
            return;

        ExecuteTurnInternal();
    }

    public void ExecuteTurnFromNetworkHost()
    {
        ExecuteTurnInternal();
    }

    public void PlayNetworkExecutionFromHost(List<BattleActionBatch> batches)
    {
        if (batches == null || batches.Count <= 0 || executeTurnCoroutine != null)
            return;

        CaptureSelectedCharacterBeforeExecution();

        MonsterUnit.ClearMonsterInfoSelection();
        MonsterUnit.HideAllTemporaryHUDs();

        EnsureSkillListPanel();
        if (skillListPanel != null)
            skillListPanel.CloseForBattleExecution();

        HideBattleExecutionUiUntilPlayerTurn();

        isExecuting = true;
        isMonsterPlanReady = false;
        isPlayerInputReady = false;
        RefreshEndTurnButton();
        RefreshBattlePresentationState();

        if (endTurnButton != null)
            endTurnButton.interactable = false;

        if (timelineController != null)
        {
            timelineController.ClearSelectedSlotSelection();
            timelineController.SetSelectedCharacterScaleFeedbackActive(false);
            timelineController.SetSlotSelectionLocked(true);
        }

        BattleExecutionStarted?.Invoke();

        executeTurnCoroutine = StartCoroutine(PlayNetworkExecutionRoutine(batches));
    }

    private void ExecuteTurnInternal()
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

        CaptureSelectedCharacterBeforeExecution();

        MonsterUnit.ClearMonsterInfoSelection();
        MonsterUnit.HideAllTemporaryHUDs();

        EnsureSkillListPanel();
        if (skillListPanel != null)
            skillListPanel.CloseForBattleExecution();

        HideBattleExecutionUiUntilPlayerTurn();

        isExecuting = true;

        isMonsterPlanReady = false;
        isPlayerInputReady = false;
        RefreshEndTurnButton();
        RefreshBattlePresentationState();

        if (endTurnButton != null)
            endTurnButton.interactable = false;

        timelineController.ClearSelectedSlotSelection();
        timelineController.SetSelectedCharacterScaleFeedbackActive(false);
        timelineController.SetSlotSelectionLocked(true);

        ApplyPlayerReservationTurnStartEquipmentEffects();
        RefreshBattleHUDs();

        BattleExecutionStarted?.Invoke();

        executeTurnCoroutine = StartCoroutine(ExecuteTurnRoutine());
    }

    private IEnumerator ExecuteTurnRoutine()
    {
        try
        {
            if (moveGhostPreview != null)
                moveGhostPreview.ClearAll();

            yield return ReturnCameraDefaultRoutine();

            BattleActionBatchBuilder builder = new(gridManager);
            BattleActionSimulationService simulator = new(gridManager);

            uniqueResourceService.BeginTurnExecution();

            simulator.Simulate(timelineController);

            List<BattleActionBatch> batches = builder.Build(timelineController);
            BattleConsecutiveActionPlan consecutiveActionPlan =
                BattleConsecutiveActionPlan.Build(
                    batches,
                    consecutiveActionSpeedMultiplier);
            BattleActionRunner runner = new(
                gridManager,
                monsterSpawner,
                roomLoader,
                useSafeSequentialExecution,
                actionRoutineTimeout,
                uniqueResourceService.OnPlayerCommandExecuted,
                consecutiveActionPlan,
                multiHitActionInterval
            );
            SteamBattleStateSynchronizer.TryBroadcastBattleExecution(batches);

            yield return ShowBattleProgressIntroTextRoutineSafe();

            int slidThroughSlotIndex = -1;
            Dictionary<int, int> nextTimelineOrderAnimationIndexBySlot = new Dictionary<int, int>();

            for (int i = 0; i < batches.Count; i++)
            {
                BattleActionBatch batch = batches[i];

                if (!BatchHasCommands(batch))
                    continue;

                int currentSlotIndex = GetBatchTimelineSlotIndex(batch, i);

                if (currentSlotIndex > slidThroughSlotIndex)
                {
                    // 해당 슬롯의 행동이 시작되기 전에, 비어 있는 슬롯과 현재 슬롯의 TurnMark가 먼저 갈리면서 전체 타임라인 라인이 왼쪽으로 이동합니다.
                    if (timelineController != null)
                    {
                        yield return timelineController.SlideTimelineSlotsLeftThroughSlotRoutine(
                            currentSlotIndex
                        );
                    }

                    slidThroughSlotIndex = Mathf.Max(slidThroughSlotIndex, currentSlotIndex);
                }

                int nextOrderAnimationIndex = 0;
                if (nextTimelineOrderAnimationIndexBySlot.TryGetValue(currentSlotIndex, out int savedOrderIndex))
                    nextOrderAnimationIndex = savedOrderIndex;

                int batchCommandCount = GetBatchCommandCount(batch);

                bool keepCameraAfterBatch =
                    ShouldKeepCameraAcrossBatchBoundary(
                        batch,
                        batches,
                        i + 1,
                        runner,
                        consecutiveActionPlan);

                yield return runner.RunBatch(batch, keepCameraAfterBatch);

                bool hasNextBatchInSameTimelineSlot = HasNextExecutableBatchInSameTimelineSlot(batches, i + 1, currentSlotIndex);

                if (timelineController != null && batchCommandCount > 0)
                {
                    yield return timelineController.PlayTimelineActionAnimationsRoutine(
                        currentSlotIndex,
                        nextOrderAnimationIndex,
                        batchCommandCount,
                        !hasNextBatchInSameTimelineSlot
                    );
                }

                nextTimelineOrderAnimationIndexBySlot[currentSlotIndex] = nextOrderAnimationIndex + batchCommandCount;

                if (BattleResultChecker.Instance != null &&
                    BattleResultChecker.Instance.CheckBattleEnd())
                {
                    uniqueResourceService.FlushPendingUniqueResourceGains();
                    RefreshBattleHUDs();
                    yield return ReturnCameraDefaultRoutine();
                    ClearTimeline();
                    yield break;
                }

                if (hasNextBatchInSameTimelineSlot)
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

            // 실행할 행동이 없는 빈 슬롯도 한 턴의 라인 길이를 모두 지나가야 하므로,
            // 마지막 행동 슬롯 이후부터 5번 슬롯까지 TurnMark 갈림과 TimelineBar 이동을 진행합니다.
            if (timelineController != null && slidThroughSlotIndex < 4)
            {
                yield return timelineController.SlideTimelineSlotsLeftThroughSlotRoutine(4);
            }

            // 5번 슬롯의 Use_skill 애니메이션까지 모두 끝난 뒤에만
            // 진행 중인 TimelineBar를 -1420 완료 위치로 보정합니다.
            if (timelineController != null)
                yield return timelineController.MoveTimelineBarsToCompletedTurnPositionRoutine();

            yield return runner.ReturnCameraDefaultIfNeeded();

            // 턴 종료 회복 팝업은 모두 모은 뒤 카르마 -> 마나 -> 체력 순으로 차례대로 표시합니다.
            BattleDamageTextPopupUI.BeginRecoveryPopupSequence();

            yield return runner.ApplyTurnEndEffectsRoutine();

            ResolveTurnEndGridEffects();
            AdvanceGridEffectDurations();
            ClearActiveRelicTurnScopedStatuses();
            ClearAllShield();
            RefreshBattleHUDs();

            if (timelineController != null)
            {
                timelineController.ApplyBlockedMoveCostRefunds();
                RefreshBattleHUDs();
            }

            ApplyPlayerEndTurnTriggeredEquipmentEffects();
            RefreshBattleHUDs();

            // 전투 실행 중 획득한 카르마는 행동마다 즉시 반영하지 않고
            // 모든 행동과 턴 종료 효과가 끝난 뒤 캐릭터별 합계로 한 번만 반영합니다.
            uniqueResourceService.FlushPendingUniqueResourceGains();
            RefreshBattleHUDs();

            ClearTimeline();
            yield return null;

            if (BattleResultChecker.Instance != null &&
                BattleResultChecker.Instance.CheckBattleEnd())
            {
                BattleDamageTextPopupUI.EndRecoveryPopupSequence();
                yield return ReturnCameraDefaultRoutine();
                yield break;
            }

            playerTurnNumber++;
            ApplyQueuedNextTurnSwift();
            RefreshTurnNumberText();

            if (timelineController != null)
                yield return timelineController.ResetTimelineSlotsToOriginalPositionRoutine();

            if (roomLoader != null)
            {
                roomLoader.RecoverPlayerCostsToMax();

                // 카르마/마나/체력 회복값이 모두 확정된 뒤 순차 팝업 재생을 시작합니다.
                BattleDamageTextPopupUI.EndRecoveryPopupSequence();

                passiveSkillService.ClearAllPlayerPassiveEffects();

                yield return roomLoader.PlanNextMonsterTurnsRoutine();

                passiveSkillService.RefreshAllPlayerPassives();

                roomLoader.RefreshBattleHUDs();
            }
            else
            {
                BattleDamageTextPopupUI.EndRecoveryPopupSequence();
            }

            // 몬스터 계획과 턴 전환 정리가 모두 끝난 뒤, 실제 다음 플레이어 턴이 시작되는 시점에
            // 현재 잔여물 위에 서 있는 캐릭터에게 피해를 다시 적용합니다.
            ApplyStandingResidueAtTurnStart();

            if (BattleResultChecker.Instance != null &&
                BattleResultChecker.Instance.CheckBattleEnd())
            {
                yield return ReturnCameraDefaultRoutine();
                yield break;
            }

            // 이전 턴의 캐릭터 선택 상태를 먼저 비웁니다.
            // 여기서는 카메라를 움직이지 않고 선택 상태만 해제하여,
            // Y 1.5 복귀가 끝난 뒤 실제 캐릭터 선택 이벤트가 새로 발생하도록 합니다.
            if (timelineController != null)
                timelineController.SelectCharacter(null);

            // 다음 예약 턴으로 UI가 올라오기 직전까지만 Panel Down 카메라를 사용합니다.
            // PlayerTurnReturned가 발생하기 전이므로 BattleCharacterPanel/BattleSlot은 아직 내려가 있습니다.
            yield return ReturnCameraPanelDownRoutine();
        }
        finally
        {
            // 중간 종료/예외가 발생해도 회복 팝업 대기 상태가 남지 않도록 해제합니다.
            BattleDamageTextPopupUI.EndRecoveryPopupSequence();
            executeTurnCoroutine = null;

            if (timelineController != null)
                timelineController.SetSlotSelectionLocked(false);

            isExecuting = false;

            RefreshEndTurnButton();
            RefreshBattlePresentationState();
            RefreshBattleExecutionUiVisibility();

            if (CanAcceptPlayerInput)
            {
                RestoreSelectedCharacterForReservationTurn();

                if (timelineController != null)
                {
                    timelineController.SelectDefaultSlotWhenInputReady();
                    timelineController.SetSelectedCharacterScaleFeedbackActive(true);
                }
            }

            EnsureSkillListPanel();
            if (CanAcceptPlayerInput && skillListPanel != null)
                skillListPanel.ReopenAfterBattleExecution();

            PlayerTurnReturned?.Invoke();
        }
    }

    private IEnumerator PlayNetworkExecutionRoutine(List<BattleActionBatch> batches)
    {
        try
        {
            if (moveGhostPreview != null)
                moveGhostPreview.ClearAll();

            yield return ReturnCameraDefaultRoutine();

            BattleConsecutiveActionPlan consecutiveActionPlan =
                BattleConsecutiveActionPlan.Build(
                    batches,
                    consecutiveActionSpeedMultiplier);
            BattleActionRunner runner = new(
                gridManager,
                monsterSpawner,
                roomLoader,
                useSafeSequentialExecution,
                actionRoutineTimeout,
                uniqueResourceService != null ? uniqueResourceService.OnPlayerCommandExecuted : null,
                consecutiveActionPlan,
                multiHitActionInterval
            );

            yield return ShowBattleProgressIntroTextRoutineSafe();

            int slidThroughSlotIndex = -1;
            Dictionary<int, int> nextTimelineOrderAnimationIndexBySlot = new Dictionary<int, int>();

            for (int i = 0; i < batches.Count; i++)
            {
                BattleActionBatch batch = batches[i];

                if (!BatchHasCommands(batch))
                    continue;

                int currentSlotIndex = GetBatchTimelineSlotIndex(batch, i);

                if (currentSlotIndex > slidThroughSlotIndex)
                {
                    if (timelineController != null)
                    {
                        yield return timelineController.SlideTimelineSlotsLeftThroughSlotRoutine(
                            currentSlotIndex);
                    }

                    slidThroughSlotIndex = Mathf.Max(slidThroughSlotIndex, currentSlotIndex);
                }

                int nextOrderAnimationIndex = 0;
                if (nextTimelineOrderAnimationIndexBySlot.TryGetValue(
                        currentSlotIndex,
                        out int savedOrderIndex))
                {
                    nextOrderAnimationIndex = savedOrderIndex;
                }

                int batchCommandCount = GetBatchCommandCount(batch);

                bool keepCameraAfterBatch =
                    ShouldKeepCameraAcrossBatchBoundary(
                        batch,
                        batches,
                        i + 1,
                        runner,
                        consecutiveActionPlan);

                yield return runner.RunBatch(batch, keepCameraAfterBatch);

                bool hasNextBatchInSameTimelineSlot =
                    HasNextExecutableBatchInSameTimelineSlot(batches, i + 1, currentSlotIndex);

                if (timelineController != null && batchCommandCount > 0)
                {
                    yield return timelineController.PlayTimelineActionAnimationsRoutine(
                        currentSlotIndex,
                        nextOrderAnimationIndex,
                        batchCommandCount,
                        !hasNextBatchInSameTimelineSlot);
                }

                nextTimelineOrderAnimationIndexBySlot[currentSlotIndex] =
                    nextOrderAnimationIndex + batchCommandCount;

                if (hasNextBatchInSameTimelineSlot)
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

            if (timelineController != null && slidThroughSlotIndex < 4)
                yield return timelineController.SlideTimelineSlotsLeftThroughSlotRoutine(4);

            if (timelineController != null)
                yield return timelineController.MoveTimelineBarsToCompletedTurnPositionRoutine();

            yield return runner.ReturnCameraDefaultIfNeeded();

            yield return RestoreNetworkReservationStateAfterExecutionRoutine();

            // 네트워크 실행에서도 이전 선택을 비운 뒤 Y 1.5 복귀를 완료하고,
            // finally에서 실제 살아있는 캐릭터를 새로 선택합니다.
            if (timelineController != null)
                timelineController.SelectCharacter(null);

            // 네트워크 실행도 예약 UI가 다시 올라오기 직전에만 Y 1.5로 복귀합니다.
            yield return ReturnCameraPanelDownRoutine();
        }
        finally
        {
            executeTurnCoroutine = null;

            if (timelineController != null)
                timelineController.SetSlotSelectionLocked(false);

            isExecuting = false;
            networkExecutionLocked = false;
            isMonsterPlanReady = true;
            isPlayerInputReady = true;

            RefreshEndTurnButton();
            RefreshBattlePresentationState();
            RefreshBattleExecutionUiVisibility();

            if (CanAcceptPlayerInput)
            {
                RestoreSelectedCharacterForReservationTurn();

                if (timelineController != null)
                {
                    timelineController.SelectDefaultSlotWhenInputReady(false);
                    timelineController.SetSelectedCharacterScaleFeedbackActive(true);
                    timelineController.StopTimelineMotionEffects();
                }
            }

            EnsureSkillListPanel();
            if (CanAcceptPlayerInput && skillListPanel != null)
                skillListPanel.ReopenAfterBattleExecution();

            if (CanAcceptPlayerInput && timelineController != null)
                timelineController.StopTimelineMotionEffects();

            PlayerTurnReturned?.Invoke();
        }
    }

    private void CaptureSelectedCharacterBeforeExecution()
    {
        selectedCharacterBeforeExecution = timelineController != null
            ? timelineController.SelectedCharacter
            : null;
    }

    private void RestoreSelectedCharacterForReservationTurn()
    {
        if (selectedCharacterBeforeExecution != null && !selectedCharacterBeforeExecution.IsDead)
        {
            if (roomLoader != null)
                roomLoader.OnPlayerCharacterClicked(selectedCharacterBeforeExecution);
            else if (timelineController != null)
                timelineController.SelectCharacter(selectedCharacterBeforeExecution);
        }
        else if (roomLoader != null)
        {
            roomLoader.SelectFirstAlivePlayerCharacterIfNeeded();
        }

        selectedCharacterBeforeExecution = null;
    }

    private IEnumerator RestoreNetworkReservationStateAfterExecutionRoutine()
    {
        ClearTimeline();

        if (moveGhostPreview != null)
            moveGhostPreview.ClearAll();

        yield return null;

        playerTurnNumber++;
        ApplyQueuedNextTurnSwift();
        RefreshTurnNumberText();

        if (timelineController != null)
        {
            yield return timelineController.ResetTimelineSlotsToOriginalPositionRoutine();
            timelineController.StopTimelineMotionEffects();
        }

        SteamBattleStateSynchronizer.TryRefreshIdleSnapshotAfterNetworkExecution();
        RefreshBattleHUDs();
    }

    private static void ApplyStandingResidueAtTurnStart()
    {
        BattleGridEffectController controller =
            FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

        if (controller != null)
            controller.ApplyStandingResidueToPlayers();
    }

    private static void ResolveTurnEndGridEffects()
    {
        BattleGridEffectController controller =
            FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

        if (controller != null)
            controller.ResolveTurnEndGridEffects();
    }

    private static void AdvanceGridEffectDurations()
    {
        BattleGridEffectController controller =
            FindFirstObjectByType<BattleGridEffectController>(FindObjectsInactive.Include);

        if (controller != null)
            controller.AdvanceTurnDurations();
    }

    private IEnumerator ReturnCameraDefaultRoutine()
    {
        BattleCameraController cameraController = BattleCameraController.Instance;

        if (cameraController == null)
            yield break;

        yield return cameraController.ReturnDefault();
    }

    private IEnumerator ReturnCameraPanelDownRoutine()
    {
        BattleCameraController cameraController = BattleCameraController.Instance;

        if (cameraController == null)
            yield break;

        // 모든 행동/턴 전환 처리가 끝났고 패널은 아직 내려가 있는 구간에서만
        // 카메라를 Panel Down 기준 Y(기본 1.5)로 옮깁니다.
        yield return cameraController.ReturnPanelDown();
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

    private void InitializeEndTurnVisualFeedback()
    {
        if (endTurnVisualFeedbackInitialized || endTurnButton == null)
            return;

        if (endTurnLineImage == null)
        {
            Transform lineTransform = FindChildRecursive(endTurnButton.transform, "Line");
            if (lineTransform != null)
                endTurnLineImage = lineTransform.GetComponent<Image>();
        }

        if (endTurnBackground2 == null)
        {
            Transform background2Transform = FindChildRecursive(endTurnButton.transform, "Background2");
            if (background2Transform != null)
                endTurnBackground2 = background2Transform.gameObject;
        }

        if (endTurnLineImage != null)
            endTurnLineDefaultColor = endTurnLineImage.color;

        SetEndTurnBackground2Visible(false);

        EventTrigger trigger = endTurnButton.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = endTurnButton.gameObject.AddComponent<EventTrigger>();

        if (trigger.triggers == null)
            trigger.triggers = new List<EventTrigger.Entry>();

        EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ => SetEndTurnLineHovered(true));
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ => SetEndTurnLineHovered(false));
        trigger.triggers.Add(exitEntry);

        endTurnButton.onClick.AddListener(PlayEndTurnClickFeedback);
        endTurnVisualFeedbackInitialized = true;
    }

    private void SetEndTurnLineHovered(bool hovered)
    {
        if (endTurnLineImage == null)
            return;

        endTurnLineImage.color = hovered ? endTurnLineHoverColor : endTurnLineDefaultColor;
    }

    private void RestoreEndTurnLineColor()
    {
        if (endTurnLineImage != null)
            endTurnLineImage.color = endTurnLineDefaultColor;
    }

    private void PlayEndTurnClickFeedback()
    {
        InitializeEndTurnVisualFeedback();

        if (endTurnClickFeedbackCoroutine != null)
            StopCoroutine(endTurnClickFeedbackCoroutine);

        SetEndTurnBackground2Visible(true);
        endTurnClickFeedbackCoroutine = StartCoroutine(EndTurnClickFeedbackRoutine());
    }

    private IEnumerator EndTurnClickFeedbackRoutine()
    {
        float duration = Mathf.Max(0f, endTurnClickFeedbackDuration);

        if (duration > 0f)
            yield return new WaitForSecondsRealtime(duration);

        SetEndTurnBackground2Visible(false);
        endTurnClickFeedbackCoroutine = null;
    }

    private void SetEndTurnBackground2Visible(bool visible)
    {
        if (endTurnBackground2 != null && endTurnBackground2.activeSelf != visible)
            endTurnBackground2.SetActive(visible);
    }

    private static Transform FindChildRecursive(Transform root, string objectName)
    {
        if (root == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == objectName)
                return child;

            Transform found = FindChildRecursive(child, objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private void AutoFindTurnNumberTextIfNeeded()
    {
        if (!autoFindTurnNumberText)
            return;

        if (turnNumberText != null)
            return;

        if (!string.IsNullOrWhiteSpace(turnNumberTextRootObjectName) &&
            !string.IsNullOrWhiteSpace(turnNumberTextValueObjectName))
        {
            Transform[] transforms = FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < transforms.Length; i++)
            {
                Transform root = transforms[i];
                if (root == null || root.name != turnNumberTextRootObjectName)
                    continue;

                Transform valueTransform = root.Find(turnNumberTextValueObjectName);
                if (valueTransform == null)
                    continue;

                turnNumberText = valueTransform.GetComponent<TMP_Text>();
                if (turnNumberText != null)
                    return;
            }
        }

        if (string.IsNullOrWhiteSpace(turnNumberTextObjectName))
            return;

        GameObject found = GameObject.Find(turnNumberTextObjectName);
        if (found != null)
            turnNumberText = found.GetComponent<TMP_Text>();
    }

    private void RefreshTurnNumberText()
    {
        AutoFindTurnNumberTextIfNeeded();

        if (turnNumberText == null)
            return;

        int displayTurnNumber = Mathf.Max(1, playerTurnNumber);
        turnNumberText.text = displayTurnNumber.ToString("D2");
    }

    private void RefreshEndTurnButton()
    {
        if (endTurnButton == null)
            return;

        endTurnButton.interactable =
            !networkExecutionLocked &&
            !isExecuting &&
            isMonsterPlanReady &&
            isPlayerInputReady;
    }

    private void RefreshBattlePresentationState()
    {
        bool isReservationState = CanAcceptPlayerInput;

        if (gridManager != null)
            gridManager.SetGridVisible(isReservationState);

        MonsterUnit.SetAllReservationVisualState(false);
    }

    private void HideBattleExecutionUiUntilPlayerTurn()
    {
        battleExecutionUiSuppressed = true;
        SetBattleExecutionUiVisible(false);
    }

    private void RefreshBattleExecutionUiVisibility()
    {
        if (!battleExecutionUiSuppressed)
            return;

        if (!CanAcceptPlayerInput)
        {
            SetBattleExecutionUiVisible(false);
            return;
        }

        battleExecutionUiSuppressed = false;
        SetBattleExecutionUiVisible(true);
    }

    private void SetBattleExecutionUiVisible(bool visible)
    {
        EnsureBattleExecutionUiRoots();

        SetRootActive(playerHudRoot, true);
        SetRootActive(menuRoot, visible);

        if (!autoFindBattleExecutionUiRoots)
            return;

        SetNamedBattleExecutionUiRootsVisible(playerHudRootObjectName, true, playerHudRoot);
        SetNamedBattleExecutionUiRootsVisible(menuRootObjectName, visible, menuRoot);
    }

    private void EnsureBattleExecutionUiRoots()
    {
        if (!autoFindBattleExecutionUiRoots)
            return;

        if (playerHudRoot == null && !string.IsNullOrWhiteSpace(playerHudRootObjectName))
            playerHudRoot = FindFirstBattleExecutionUiRoot(playerHudRootObjectName);

        if (menuRoot == null && !string.IsNullOrWhiteSpace(menuRootObjectName))
            menuRoot = FindFirstBattleExecutionUiRoot(menuRootObjectName);
    }

    private static GameObject FindFirstBattleExecutionUiRoot(string objectName)
    {
        Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform root = transforms[i];

            if (root != null && root.gameObject.name == objectName)
                return root.gameObject;
        }

        return null;
    }

    private static void SetNamedBattleExecutionUiRootsVisible(
        string objectName,
        bool visible,
        GameObject configuredRoot)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return;

        Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform root = transforms[i];

            if (root == null || root.gameObject.name != objectName)
                continue;

            if (configuredRoot != null && root.gameObject == configuredRoot)
                continue;

            SetRootActive(root.gameObject, visible);
        }
    }

    private static void SetRootActive(GameObject root, bool active)
    {
        if (root != null && root.activeSelf != active)
            root.SetActive(active);
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

    private void ApplyPlayerEndTurnTriggeredEquipmentEffects()
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

            BattleEquipmentEffectService.ApplyEndTurnTriggeredEffects(runtime);
        }
    }

    private void ApplyPlayerReservationTurnStartEquipmentEffects()
    {
        if (timelineController == null ||
            DataManager.Instance == null ||
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

            BattleEquipmentEffectService.ApplyReservationTurnStartEffects(
                runtime,
                playerTurnNumber,
                timelineController.CountPlayerOccupiedSlots(characterId),
                timelineController.CountPlayerEmptySlots(characterId),
                timelineController.GetPlayerEmptySlotMask(characterId),
                timelineController.CountPlayerAttackSkillCommands(characterId));
        }
    }

    private IEnumerator ShowBattleProgressIntroTextRoutineSafe()
    {
        PlaySfx(playBattleProgressSfx, battleProgressSfxId, battleProgressSfxVolume);

        IEnumerator routine = BattleMapIntroText.ShowMessageAndWait(battleProgressMessage);

        if (routine == null)
            yield break;

        if (!waitIntroText)
        {
            StartCoroutine(routine);
            yield break;
        }

        bool isIntroTextFinished = false;
        StartCoroutine(RunBattleProgressIntroTextRoutine(routine, () => isIntroTextFinished = true));

        float elapsed = 0f;
        float timeout = Mathf.Max(0f, introTextTimeout);

        while (!isIntroTextFinished && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator RunBattleProgressIntroTextRoutine(IEnumerator routine, Action onFinished)
    {
        if (routine != null)
            yield return routine;

        onFinished?.Invoke();
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


    private int GetBatchCommandCount(BattleActionBatch batch)
    {
        if (batch == null)
            return 0;

        int count = 0;

        if (batch.PlayerCommands != null)
            count += batch.PlayerCommands.Count;

        if (batch.MonsterCommands != null)
            count += batch.MonsterCommands.Count;

        return count;
    }

    private bool ShouldKeepCameraAcrossBatchBoundary(
        BattleActionBatch currentBatch,
        List<BattleActionBatch> batches,
        int nextStartIndex,
        BattleActionRunner runner,
        BattleConsecutiveActionPlan consecutiveActionPlan)
    {
        if (currentBatch == null || batches == null || runner == null)
            return false;

        int nextExecutableBatchIndex = GetNextExecutableBatchIndex(batches, nextStartIndex);
        if (nextExecutableBatchIndex < 0)
            return false;

        BattleActionBatch nextBatch = batches[nextExecutableBatchIndex];

        if (consecutiveActionPlan != null &&
            consecutiveActionPlan.ContinuesAcrossBoundary(currentBatch, nextBatch))
        {
            return true;
        }

        if (!runner.BatchHasCrossSideHitAction(currentBatch))
            return false;

        if (nextBatch == null || !runner.BatchHasCrossSideHitAction(nextBatch))
            return false;

        string currentMonsterRuntimeId = GetSingleMonsterRuntimeId(currentBatch);
        string nextMonsterRuntimeId = GetSingleMonsterRuntimeId(nextBatch);

        // 서로 다른 몬스터의 행동 사이에는 이전 몬스터의 전투 줌을 유지하지 않는다.
        // A 몬스터 연출이 끝나면 기본 전투 카메라(Y=1.5)로 복귀한 뒤
        // B 몬스터의 카메라 연출이 새로 시작되어야 한다.
        if (!string.IsNullOrEmpty(currentMonsterRuntimeId) &&
            !string.IsNullOrEmpty(nextMonsterRuntimeId) &&
            !string.Equals(currentMonsterRuntimeId, nextMonsterRuntimeId, StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    private string GetSingleMonsterRuntimeId(BattleActionBatch batch)
    {
        if (batch == null || batch.MonsterCommands == null || batch.MonsterCommands.Count != 1)
            return string.Empty;

        MonsterReservedCommand command = batch.MonsterCommands[0];
        return command != null ? command.RuntimeId : string.Empty;
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

    private void PlaySfx(bool play, string sfxId, float volume)
    {
        if (!play)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(sfxId, volume);
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

    private void ClearAllShield()
    {
        BattleCharacter[] characters = FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == null || characters[i].RuntimeData == null)
                continue;

            characters[i].RuntimeData.CurrentShield = 0;
        }

        MonsterUnit[] monsters = FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < monsters.Length; i++)
        {
            if (monsters[i] == null || monsters[i].RuntimeData == null)
                continue;

            monsters[i].RuntimeData.ClearTemporaryShield();
        }
    }

    private void ClearActiveRelicTurnScopedStatuses()
    {
        BattleCharacter[] characters = FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == null || characters[i].RuntimeData == null)
                continue;

            ActiveRelicRuntimeUtility.RemoveTurnScopedStatuses(characters[i].RuntimeData);
        }
    }

    private void RefreshBattleHUDs()
    {
        if (roomLoader != null)
        {
            roomLoader.RefreshBattleHUDs();
            return;
        }

        PlayerHUDSlot[] playerHUDs = FindObjectsByType<PlayerHUDSlot>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < playerHUDs.Length; i++)
        {
            if (playerHUDs[i] != null)
                playerHUDs[i].Refresh();
        }

        MonsterUnit.HideAllTemporaryHUDs();
    }
}
