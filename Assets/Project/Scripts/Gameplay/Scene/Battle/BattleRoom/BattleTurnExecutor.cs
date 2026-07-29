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

    [Header("Turn Text")]
    [SerializeField] private TMP_Text turnNumberText;
    [SerializeField] private bool autoFindTurnNumberText = true;
    [SerializeField] private string turnNumberTextObjectName = "TURN_TEXT2";

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
    private bool networkExecutionLocked;
    private bool battleExecutionUiSuppressed;
    private Coroutine executeTurnCoroutine;
    private int playerTurnNumber = 1;

    private readonly BattleUniqueResourceService uniqueResourceService = new();
    private readonly BattlePassiveSkillService passiveSkillService = new();

    private void Start()
    {
        AutoFindTurnNumberTextIfNeeded();
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

        ApplyPlayerEndTurnTriggeredEquipmentEffects();
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
            BattleActionRunner runner = new(
                gridManager,
                monsterSpawner,
                roomLoader,
                useSafeSequentialExecution,
                actionRoutineTimeout,
                uniqueResourceService.OnPlayerCommandExecuted
            );
            BattleActionSimulationService simulator = new(gridManager);

            uniqueResourceService.BeginTurnExecution();

            simulator.Simulate(timelineController);

            List<BattleActionBatch> batches = builder.Build(timelineController);
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
                    runner.BatchHasCrossSideHitAction(batch) &&
                    NextExecutableBatchHasCrossSideHitAction(batches, i + 1, runner);

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

            yield return runner.ApplyTurnEndEffectsRoutine();

            AdvanceGridEffectDurations();
            ClearActiveRelicTurnScopedStatuses();
            ClearAllShield();
            RefreshBattleHUDs();

            if (timelineController != null)
            {
                timelineController.ApplyBlockedMoveCostRefunds();
                RefreshBattleHUDs();
            }

            ClearTimeline();
            yield return null;

            if (BattleResultChecker.Instance != null &&
                BattleResultChecker.Instance.CheckBattleEnd())
            {
                yield return ReturnCameraDefaultRoutine();
                yield break;
            }

            playerTurnNumber++;
            RefreshTurnNumberText();

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
            executeTurnCoroutine = null;

            if (timelineController != null)
                timelineController.SetSlotSelectionLocked(false);

            isExecuting = false;

            RefreshEndTurnButton();
            RefreshBattlePresentationState();
            RefreshBattleExecutionUiVisibility();

            if (CanAcceptPlayerInput && timelineController != null)
            {
                timelineController.SelectDefaultSlotWhenInputReady();
                timelineController.SetSelectedCharacterScaleFeedbackActive(true);
                timelineController.RefocusCurrentSelectedCharacterWhenInputReady();
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

            BattleActionRunner runner = new(
                gridManager,
                monsterSpawner,
                roomLoader,
                useSafeSequentialExecution,
                actionRoutineTimeout,
                uniqueResourceService != null ? uniqueResourceService.OnPlayerCommandExecuted : null
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
                    runner.BatchHasCrossSideHitAction(batch) &&
                    NextExecutableBatchHasCrossSideHitAction(batches, i + 1, runner);

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

            if (CanAcceptPlayerInput && timelineController != null)
            {
                timelineController.SelectDefaultSlotWhenInputReady();
                timelineController.SetSelectedCharacterScaleFeedbackActive(true);
                timelineController.RefocusCurrentSelectedCharacterWhenInputReady();
            }

            EnsureSkillListPanel();
            if (CanAcceptPlayerInput && skillListPanel != null)
                skillListPanel.ReopenAfterBattleExecution();
        }
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

    private void AutoFindTurnNumberTextIfNeeded()
    {
        if (!autoFindTurnNumberText)
            return;

        if (turnNumberText != null)
            return;

        if (string.IsNullOrWhiteSpace(turnNumberTextObjectName))
            return;

        GameObject found = GameObject.Find(turnNumberTextObjectName);

        if (found == null)
            return;

        turnNumberText = found.GetComponent<TMP_Text>();
    }

    private void RefreshTurnNumberText()
    {
        AutoFindTurnNumberTextIfNeeded();

        if (turnNumberText == null)
            return;

        int displayTurnNumber = Mathf.Max(1, playerTurnNumber);
        turnNumberText.text = displayTurnNumber.ToString();
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

        SetRootActive(playerHudRoot, visible);
        SetRootActive(menuRoot, visible);

        if (!autoFindBattleExecutionUiRoots)
            return;

        SetNamedBattleExecutionUiRootsVisible(playerHudRootObjectName, visible, playerHudRoot);
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
        PlaySfx(playBattleProgressSfx, battleProgressSfxType, battleProgressSfxVolume);

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

            monsters[i].RuntimeData.CurrentShield = 0;
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
