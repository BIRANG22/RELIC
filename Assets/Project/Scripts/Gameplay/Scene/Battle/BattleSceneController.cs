using System;
using System.Collections;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Localization.Components;
using TMPro;
using Relic.Gameplay.Data;

public class BattleSceneController : MonoBehaviour
{
    private const string SharedPartyPresentationRootObjectName = "AllyRoot";

    public static bool IsBattleRoomIntroPlaying { get; private set; }
    public static event Action BattleRoomIntroStarted;
    public static event Action BattleRoomIntroCompleted;
    [Header("Panels")]
    [SerializeField] private BattleMapPanel battleMapPanel;
    [SerializeField] private BattleRoomMapSelectionPresenter mapSelectionPresenter;
    [SerializeField] private GameObject erosionSelect;
    [SerializeField] private GameObject erosionPanel;
    [SerializeField] private Transform erosionSlotContent;
    [SerializeField] private GameObject erosionSlotPrefab;

    private readonly List<GameObject> battleErosionSlotInstances = new();

    [Header("Battle Scene Transition")]
    [SerializeField] private BattleDiagonalSceneTransition battleTransition;

    [Header("Battle Map Intro Text")]
    [SerializeField] private BattleMapIntroText battleMapIntroText;
    [SerializeField] private string mapIntroMessage = "제1구역 폐허";
    [SerializeField] private string battleRoomIntroMessage = "전투 시작";
    [SerializeField] private string restRoomIntroMessage = "휴식 구역";
    [SerializeField] private bool playMapIntroOnStart = true;
    [SerializeField] private bool playBattleRoomIntroFromSceneController = false;

    [Header("Stage Entry Position Panel")]
    [SerializeField] private GameObject positionPanel;
    [SerializeField] private CanvasGroup positionPanelCanvasGroup;
    [SerializeField] private TMP_Text positionStageText;
    [SerializeField] private TMP_Text positionNameText;
    [SerializeField] private float positionPanelHoldDuration = 1.2f;
    [SerializeField] private float positionPanelFadeDuration = 0.35f;

    [Header("Back2 Name")]
    [SerializeField] private TMP_Text back2NameText;

    [Header("Auto Return To Map")]
    [SerializeField] private bool autoDetectReturnToMap = true;
    [SerializeField] private Transform roomRoot;

    [Header("Shared Room Presentation")]
    [SerializeField] private GameObject sharedRoomRoot;
    [SerializeField] private StageBackgroundController sharedBackgroundController;
    [SerializeField] private MapVisualController sharedMapVisualController;
    [SerializeField] private GameObject sharedPartyPresentationRoot;
    [SerializeField] private MapRoomController sharedRoomPresentationController;

    [Header("Runtime Default")]
    [SerializeField] private string defaultChapterId = "Chapter1";
    [SerializeField] private string defaultStage = "Stage1";

    [Header("Rooms")]
    [SerializeField] private GameObject battleRoom;
    [SerializeField] private GameObject eventRoom;
    [SerializeField] private GameObject restRoom;

    [Header("Room Change Auto Close")]
    [SerializeField] private bool closeInventoryAndBagOnRoomActiveChange = true;
    [SerializeField] private string[] inventoryPanelObjectNames = { "InventoryPanel" };
    [SerializeField] private string[] bagPanelObjectNames = { "BattleBagPanel", "BagPanel", "BagPanelUI" };
    [SerializeField] private float inventoryClosedY = 1080f;
    [SerializeField] private float bagClosedX = 1100f;

    private MapRuntimeStore mapRuntimeStore;
    private MapRuntimeData mapRuntime;
    private bool isChangingRoom;
    private bool isOpeningMapFromController;
    private bool isStarted;
    private bool wasAnyRoomActiveLastFrame;
    private GameObject lastActiveRoomLastFrame;

    private bool isAutoReturningToMap;
    private bool isRestoringExternallyDisabledRoom;
    private GameObject autoReturnRoomToKeepVisible;
    private string pendingRoomIntroMessage;
    private bool hasRoomPanelAutoCloseState;
    private bool lastAnyRoomActiveForPanelAutoClose;
    private GameObject lastActiveRoomForPanelAutoClose;
    private int lastNetworkAppliedNodeIndex = int.MinValue;
    private bool lastNetworkAppliedNodeCleared;
    private bool forceNextBattleRoomLoad;
    private bool pendingBattleRoomUsesBossIntro;
    private readonly BattleRoomIntroLoadGate battleRoomIntroLoadGate = new();

    private void Awake()
    {
        HideSharedNextButtonOnSceneStart();
        AutoFindRoomRootIfNeeded();
        AutoFindSharedRoomPresentationIfNeeded();
        AutoFindBattleMapIntroTextIfNeeded();
        InstallMapPanelAutoReturnWatcher();
        AutoFindErosionSelectIfNeeded();
        AutoFindErosionPanelIfNeeded();
        AutoFindErosionSlotBindingsIfNeeded();
        AutoFindBack2NameIfNeeded();
        PrepareBack2NameForDynamicUse();
        InstallErosionSelectClickHandler();
        SetErosionPanelVisible(false);

        if (mapSelectionPresenter == null)
            mapSelectionPresenter = GetComponent<BattleRoomMapSelectionPresenter>();

        if (mapSelectionPresenter == null)
            mapSelectionPresenter = gameObject.AddComponent<BattleRoomMapSelectionPresenter>();
    }

    private void HideSharedNextButtonOnSceneStart()
    {
        HideSceneObjectOnStart("NextButton");
        HideSceneObjectOnStart("NextStageButton");
        HideSceneObjectOnStart("ReturnButton");
    }

    private static void HideSceneObjectOnStart(string objectName)
    {
        Transform target = FindSceneTransformByName(objectName);

        if (target != null)
            target.gameObject.SetActive(false);
    }

    private void Start()
    {
        SteamBattleStateSynchronizer.EnsureForBattleScene(null, null);
        InitializeRuntime();
        SetupBattleErosionGauge();
        PrimeBack2NameBeforePresentation();
        SetErosionSelectVisible(false);
        RefreshErosionScoreDisplay();
        RefreshBattleErosionSlots();
        CloseAllRooms();

        if (battleMapPanel != null)
            battleMapPanel.Prepare(mapRuntime);

        if (!TryRestoreBattleRewardOnStart() &&
            !TryOpenUnclearedCurrentNodeOnStart() && !TryOpenLayerZeroNodeOnNewRun())
        {
            OpenMapPanelImmediate();
        }

        lastActiveRoomLastFrame = FindActiveRoomObject();
        wasAnyRoomActiveLastFrame = lastActiveRoomLastFrame != null;
        isStarted = true;
    }

    private void RefreshErosionScoreDisplay()
    {
        Transform valueTransform = FindSceneTransformByName("Erosion_Value");
        if (valueTransform == null)
            return;

        TMP_Text valueText = valueTransform.GetComponent<TMP_Text>();
        if (valueText == null)
            return;

        int score = 0;
        if (DataManager.Instance != null &&
            DataManager.Instance.LobbyRuntimeStore != null &&
            DataManager.Instance.ErosionDatabase != null)
        {
            LobbyRuntimeData lobbyData = DataManager.Instance.LobbyRuntimeStore.GetOrCreate();
            List<string> selectedIds = lobbyData.SelectedErosionDifficultyIds;

            if (selectedIds != null)
            {
                for (int i = 0; i < selectedIds.Count; i++)
                {
                    string difficultyId = selectedIds[i];
                    if (string.IsNullOrWhiteSpace(difficultyId))
                        continue;

                    if (DataManager.Instance.ErosionDatabase.TryGet(difficultyId, out ErosionData erosionData) &&
                        erosionData != null)
                    {
                        score += erosionData.Score;
                    }
                }
            }
        }

        valueText.text = score.ToString();
    }

    private void AutoFindErosionSlotBindingsIfNeeded()
    {
        AutoFindErosionPanelIfNeeded();
        if (erosionPanel == null)
            return;

        if (erosionSlotContent == null)
            erosionSlotContent = FindChildRecursive(erosionPanel.transform, "Content");

        if (erosionSlotPrefab == null && erosionSlotContent != null)
        {
            Transform template = FindChildRecursive(erosionSlotContent, "ErosionSlot");
            if (template != null)
                erosionSlotPrefab = template.gameObject;
        }

        if (erosionSlotPrefab != null)
            erosionSlotPrefab.SetActive(false);
    }

    private void RefreshBattleErosionSlots()
    {
        AutoFindErosionSlotBindingsIfNeeded();
        ClearBattleErosionSlots();

        if (erosionSlotContent == null || erosionSlotPrefab == null)
        {
            Debug.LogWarning("[BattleSceneController] ErosionPanel의 Content 또는 ErosionSlot 프리팹을 찾을 수 없습니다.");
            return;
        }

        if (DataManager.Instance == null ||
            DataManager.Instance.LobbyRuntimeStore == null ||
            DataManager.Instance.ErosionDatabase == null)
        {
            return;
        }

        LobbyRuntimeData lobbyData = DataManager.Instance.LobbyRuntimeStore.GetOrCreate();
        List<string> selectedIds = lobbyData.SelectedErosionDifficultyIds;
        if (selectedIds == null || selectedIds.Count == 0)
            return;

        for (int i = 0; i < selectedIds.Count; i++)
        {
            string difficultyId = selectedIds[i];
            if (string.IsNullOrWhiteSpace(difficultyId))
                continue;

            if (!DataManager.Instance.ErosionDatabase.TryGet(difficultyId, out ErosionData erosionData) ||
                erosionData == null)
            {
                continue;
            }

            GameObject slotInstance = Instantiate(erosionSlotPrefab, erosionSlotContent);
            slotInstance.name = $"ErosionSlot_{erosionData.DifficultyId}";

            // 패널이 화면에 표시되기 전에 고정 LocalizedTMPText를 끄고
            // 최종 번역 문자열을 먼저 적용합니다.
            // ErosionPanel을 켠 뒤 기본 문자열 -> 번역 문자열로 바뀌는 모습이 보이지 않게 합니다.
            PrepareBattleErosionSlotForImmediateDisplay(slotInstance.transform);
            BindBattleErosionSlot(slotInstance.transform, erosionData);
            slotInstance.SetActive(true);

            battleErosionSlotInstances.Add(slotInstance);
            StartCoroutine(RebindBattleErosionSlotNextFrame(slotInstance, erosionData));
        }
    }

    private IEnumerator RebindBattleErosionSlotNextFrame(GameObject slotInstance, ErosionData erosionData)
    {
        yield return null;

        if (slotInstance != null && erosionData != null)
            BindBattleErosionSlot(slotInstance.transform, erosionData);
    }

    private void ClearBattleErosionSlots()
    {
        for (int i = 0; i < battleErosionSlotInstances.Count; i++)
        {
            GameObject slot = battleErosionSlotInstances[i];
            if (slot != null)
                Destroy(slot);
        }

        battleErosionSlotInstances.Clear();
    }

    private static void PrepareBattleErosionSlotForImmediateDisplay(Transform slotRoot)
    {
        if (slotRoot == null)
            return;

        // ErosionSlot은 ErosionData를 기준으로 직접 번역 문자열을 바인딩합니다.
        // 프리팹에 남아 있는 LocalizedTMPText가 패널 활성화 시 텍스트를 다시 덮어쓰면
        // 한 프레임 동안 기본 문자열이 보일 수 있으므로 전투 슬롯에서는 비활성화합니다.
        LocalizedTMPText[] localizers = slotRoot.GetComponentsInChildren<LocalizedTMPText>(true);
        for (int i = 0; i < localizers.Length; i++)
        {
            if (localizers[i] != null)
                localizers[i].enabled = false;
        }
    }

    private static void BindBattleErosionSlot(Transform slotRoot, ErosionData erosionData)
    {
        if (slotRoot == null || erosionData == null)
            return;

        // 배틀에서는 로비 Erosion_Catalog에 실제로 표시됐던 최종 Sprite를 그대로 사용합니다.
        // 이렇게 하면 로비에서는 보이지만 DataManager의 ErosionIconDatabase가 null이라
        // 배틀에서만 아이콘이 사라지는 문제를 피할 수 있습니다.
        Sprite icon = null;
        ErosionDifficultyCatalogUI.TryGetDisplayedErosionIcon(erosionData.DifficultyId, out icon);

        // 로비 카탈로그 캐시가 없는 특수 진입 경로에서만 기존 DB 조회를 보조 fallback으로 사용합니다.
        if (icon == null)
            icon = ErosionDifficultyCatalogUI.ResolveErosionIcon(erosionData);

        ErosionDifficultyCatalogUI.BindErosionSlotView(slotRoot, erosionData, icon);
    }

    private bool TryRestoreBattleRewardOnStart()
    {
        if (SaveSystem.Instance == null ||
            !SaveSystem.Instance.TryGetPendingResumeData(out ResumeData resume) ||
            resume.Phase != ResumePhase.BattleReward)
        {
            return false;
        }

        BattleRewardPanelUI rewardPanel = Object.FindFirstObjectByType<BattleRewardPanelUI>(
            FindObjectsInactive.Include);
        if (rewardPanel == null)
        {
            Debug.LogWarning("[BattleSceneController] Saved battle reward panel was not found.");
            return false;
        }

        PrepareBattleRewardResumePresentation(rewardPanel);
        rewardPanel.OpenSavedRewards(resume, RestoreBattleRewardCompletionPresentation);
        SaveSystem.Instance.ClearPendingResumeData();
        SaveSystem.Instance.CompleteCheckpointAutosaveRestore();
        return true;
    }

    private void PrepareBattleRewardResumePresentation(BattleRewardPanelUI rewardPanel)
    {
        CloseAllRooms();
        battleMapPanel?.Close();
        SetErosionSelectVisible(false);
        mapSelectionPresenter?.Hide();
        CloseInventoryAndBagPanelsImmediate();
        rewardPanel?.PrepareForResumePresentation();
    }

    private void RestoreBattleRewardCompletionPresentation()
    {
        BattleResultChecker resultChecker = Object.FindFirstObjectByType<BattleResultChecker>(
            FindObjectsInactive.Include);
        if (resultChecker != null)
        {
            resultChecker.RestoreBattleRewardCompletionPresentation();
            return;
        }

        Debug.LogWarning("[BattleSceneController] BattleResultChecker가 없어 보상 완료 Next UI를 복원할 수 없습니다.");
    }

    private void OnDisable()
    {
        CancelPendingBattleRoomIntro();
        SetBattleRoomIntroPlaying(false);
    }

    private void OnEnable()
    {
        if (isStarted && battleRoom != null && battleRoom.activeInHierarchy)
            RequestBattleRoomLoadOnce();
    }

    private void LateUpdate()
    {
        KeepExternalReturnRoomVisibleIfNeeded();
        UpdateRoomPanelAutoCloseState();

        if (!autoDetectReturnToMap)
        {
            UpdateLastActiveRoomState();
            return;
        }

        if (!isStarted)
            return;

        if (isChangingRoom || isOpeningMapFromController || isAutoReturningToMap)
        {
            UpdateLastActiveRoomState();
            return;
        }

        GameObject activeRoomObject = FindActiveRoomObject();
        bool anyRoomActive = activeRoomObject != null;
        bool mapPanelActive = IsMapPanelActive();

        if (activeRoomObject != null)
            lastActiveRoomLastFrame = activeRoomObject;

        if (wasAnyRoomActiveLastFrame && !anyRoomActive && mapPanelActive)
        {
            GameObject roomToKeepVisible = autoReturnRoomToKeepVisible != null
                ? autoReturnRoomToKeepVisible
                : lastActiveRoomLastFrame;

            if (roomToKeepVisible != null)
            {
                HideMapPanelImmediate();
                RestoreRoomObjectImmediate(roomToKeepVisible);
                _ = PlayAutoRoomToMapTransitionFromCurrentRoomAsync(roomToKeepVisible);
            }
            else
            {
                _ = PlayAutoRoomToMapAlreadyCoveredTransitionAsync();
            }

            wasAnyRoomActiveLastFrame = false;
            return;
        }

        wasAnyRoomActiveLastFrame = anyRoomActive;
    }

    private void InitializeRuntime()
    {
        mapRuntimeStore = DataManager.Instance.MapRuntimeStore;
        mapRuntime = mapRuntimeStore.Get();

        if (mapRuntime != null)
            return;

        mapRuntime = new MapRuntimeData
        {
            SelectedChapterId = defaultChapterId,
            CurrentStage = defaultStage,
            IsRunInitialized = false
        };

        mapRuntimeStore.Set(mapRuntime);
    }

    private void AutoFindRoomRootIfNeeded()
    {
        if (roomRoot != null)
            return;

        GameObject foundRoomRoot = GameObject.Find("RoomRoot");
        if (foundRoomRoot != null)
            roomRoot = foundRoomRoot.transform;
    }

    private void AutoFindSharedRoomPresentationIfNeeded()
    {
        if (sharedRoomRoot == null && roomRoot != null)
        {
            Transform sharedTransform = roomRoot.Find("SharedRoomRoot");
            if (sharedTransform != null)
                sharedRoomRoot = sharedTransform.gameObject;
        }

        if (sharedBackgroundController == null && sharedRoomRoot != null)
            sharedBackgroundController = sharedRoomRoot.GetComponentInChildren<StageBackgroundController>(true);

        if (sharedMapVisualController == null && sharedRoomRoot != null)
            sharedMapVisualController = sharedRoomRoot.GetComponent<MapVisualController>();

        if (sharedRoomPresentationController == null && sharedRoomRoot != null)
            sharedRoomPresentationController = sharedRoomRoot.GetComponent<MapRoomController>();

        if (sharedPartyPresentationRoot == null && sharedRoomRoot != null)
        {
            Transform sharedPartyTransform =
                FindChildRecursive(sharedRoomRoot.transform, SharedPartyPresentationRootObjectName);

            if (sharedPartyTransform != null)
                sharedPartyPresentationRoot = sharedPartyTransform.gameObject;
        }

        if (sharedRoomRoot != null && !sharedRoomRoot.activeSelf)
            sharedRoomRoot.SetActive(true);
    }

    private void AutoFindBattleMapIntroTextIfNeeded()
    {
        if (battleMapIntroText != null)
            return;

        battleMapIntroText = Object.FindFirstObjectByType<BattleMapIntroText>(FindObjectsInactive.Include);
    }

    private void ShowRoomBackground(GameObject room, GeneratedMapNodeData nodeData, bool playBossReveal = false)
    {
        AutoFindSharedRoomPresentationIfNeeded();
        if (sharedBackgroundController != null)
        {
            sharedBackgroundController.ShowForMap(nodeData.MapId, nodeData.LayerIndex, playBossReveal);
            return;
        }

        StageBackgroundController controller = room != null
            ? room.GetComponentInChildren<StageBackgroundController>(true)
            : null;

        if (controller == null)
        {
            string roomName = room != null ? room.name : "null";
            Debug.LogWarning($"[BattleSceneController] StageBackgroundController is missing in {roomName}.");
            return;
        }

        controller.ShowForMap(nodeData.MapId, nodeData.LayerIndex, playBossReveal);
    }

    private void ApplyRoomVisual(GameObject room, GeneratedMapNodeData nodeData)
    {
        AutoFindSharedRoomPresentationIfNeeded();
        if (sharedMapVisualController != null)
        {
            sharedMapVisualController.ApplyMapVisual(nodeData?.MapId);
            return;
        }

        MapVisualController controller = room != null
            ? room.GetComponentInChildren<MapVisualController>(true)
            : null;

        controller?.ApplyMapVisual(nodeData?.MapId);
    }

    private void InstallMapPanelAutoReturnWatcher()
    {
        if (battleMapPanel == null)
            return;

        BattleMapPanelAutoReturnWatcher watcher = battleMapPanel.GetComponent<BattleMapPanelAutoReturnWatcher>();
        if (watcher == null)
            watcher = battleMapPanel.gameObject.AddComponent<BattleMapPanelAutoReturnWatcher>();

        watcher.Initialize(this);
    }

    private void OpenMapPanelImmediate()
    {
        SaveSystem.Instance?.CompleteCheckpointAutosaveRestore();
        if (battleMapPanel == null)
        {
            Debug.LogWarning("[BattleSceneController] BattleMapPanel is not assigned.");
            return;
        }

        BattleTurnExecutor turnExecutor =
            Object.FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);
        turnExecutor?.RestoreBattleExecutionUiAfterRoomEnd();

        ActivateMapRoomForMap();
        ResetCameraForMap();
        RefreshBack2LocationName(MapRuntimeProgressUtility.FindCurrentNode(mapRuntime));

        isOpeningMapFromController = true;
        battleMapPanel.Open(mapRuntime);
        SetErosionSelectVisible(true);
        isOpeningMapFromController = false;
        sharedRoomPresentationController?.RefreshForMapSelection(
            MapRuntimeProgressUtility.FindCurrentNode(mapRuntime)?.MapId);
    }

    private static void ResetCameraForMap()
    {
        BattleCameraController cameraController = BattleCameraController.Instance;
        if (cameraController != null)
        {
            cameraController.ForceReturnMapImmediate();
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        mainCamera.transform.position = new Vector3(0f, 0f, -20f);
        mainCamera.transform.rotation = Quaternion.identity;
    }

    private void HideMapPanelImmediate()
    {
        if (battleMapPanel != null && battleMapPanel.gameObject.activeSelf)
            battleMapPanel.gameObject.SetActive(false);

        SetErosionSelectVisible(false);
    }

    private void AutoFindErosionSelectIfNeeded()
    {
        if (erosionSelect != null)
            return;

        Transform found = FindSceneTransformByName("ErosionSelect");
        if (found != null)
            erosionSelect = found.gameObject;
    }

    private void SetErosionSelectVisible(bool visible)
    {
        AutoFindErosionSelectIfNeeded();

        if (erosionSelect != null && erosionSelect.activeSelf != visible)
            erosionSelect.SetActive(visible);

        if (!visible)
            SetErosionPanelVisible(false);
    }

    private void AutoFindErosionPanelIfNeeded()
    {
        if (erosionPanel != null)
            return;

        Transform found = FindSceneTransformByName("ErosionPanel");
        if (found != null)
            erosionPanel = found.gameObject;
    }

    private void InstallErosionSelectClickHandler()
    {
        AutoFindErosionSelectIfNeeded();
        if (erosionSelect == null)
            return;

        BattleErosionSelectClickHandler clickHandler =
            erosionSelect.GetComponent<BattleErosionSelectClickHandler>();
        if (clickHandler == null)
            clickHandler = erosionSelect.AddComponent<BattleErosionSelectClickHandler>();

        clickHandler.Initialize(ToggleErosionPanel);
    }

    private void ToggleErosionPanel()
    {
        if (erosionSelect == null || !erosionSelect.activeInHierarchy)
            return;

        AutoFindErosionPanelIfNeeded();
        if (erosionPanel == null)
        {
            Debug.LogWarning("[BattleSceneController] ErosionPanel을 찾을 수 없습니다.");
            return;
        }

        bool shouldOpen = !erosionPanel.activeSelf;

        if (shouldOpen)
        {
            // 패널을 먼저 보여준 뒤 데이터를 바꾸면 기본 텍스트에서 번역 텍스트로
            // 바뀌는 과정이 한 프레임 노출될 수 있습니다.
            // 패널이 꺼진 상태에서 슬롯을 완성한 뒤 마지막에 표시합니다.
            RefreshBattleErosionSlots();
            SetErosionPanelVisible(true);
        }
        else
        {
            SetErosionPanelVisible(false);
        }
    }

    private void SetErosionPanelVisible(bool visible)
    {
        AutoFindErosionPanelIfNeeded();

        if (erosionPanel != null && erosionPanel.activeSelf != visible)
            erosionPanel.SetActive(visible);
    }

    private void ActivateMapRoomForMap()
    {
        // 지도에서는 직전에 사용한 Battle/Event/Rest/Shop 등의 룸을 남겨두지 않는다.
        // RoomRoot 아래에서는 MapRoom만 활성 상태로 유지한다.
        if (roomRoot != null)
        {
            for (int i = 0; i < roomRoot.childCount; i++)
            {
                GameObject roomObject = roomRoot.GetChild(i).gameObject;
                if (roomObject == sharedRoomRoot)
                    continue;

                if (roomObject.activeSelf)
                    roomObject.SetActive(false);
            }
        }
        else
        {
            SetActiveIfNotNull(battleRoom, false);
            SetActiveIfNotNull(eventRoom, false);
            SetActiveIfNotNull(restRoom, false);
        }

        SetBattleRoomIntroPlaying(false);

        AutoFindSharedRoomPresentationIfNeeded();
        sharedMapVisualController?.ClearVisuals();
        SetSharedPartyPresentationVisible(true);
        sharedRoomPresentationController?.RefreshNow();
    }

    private bool TryOpenUnclearedCurrentNodeOnStart()
    {
        if (!MapRuntimeProgressUtility.HasUnclearedCurrentNode(mapRuntime))
            return false;

        GeneratedMapNodeData currentNode = MapRuntimeProgressUtility.FindCurrentNode(mapRuntime);
        if (currentNode == null)
            return false;

        mapRuntime.CurrentMapId = currentNode.MapId;
        mapRuntime.CurrentNodeIndex = currentNode.NodeIndex;

        string nodeKey = currentNode.NodeIndex.ToString();
        mapRuntime.VisitedMapIds ??= new List<string>();
        if (!mapRuntime.VisitedMapIds.Contains(nodeKey))
            mapRuntime.VisitedMapIds.Add(nodeKey);

        mapRuntimeStore.Set(mapRuntime);
        CaptureRoomEntrySaveCheckpoint();

        Debug.Log(
            $"[BattleSceneController] Restore uncleared map node: " +
            $"{currentNode.MapId} / Node:{currentNode.NodeIndex} / {currentNode.Type}"
        );

        // Continue가 지도 대신 방으로 직행하는 경우에도, Runtime 적용은 이미 끝났다.
        // 이 시점 이후 Event/Battle의 확정 결과 checkpoint는 저장 가능해야 한다.
        SaveSystem.Instance?.CompleteCheckpointAutosaveRestore();
        HideMapPanelImmediate();
        HandleSelectedMap(currentNode);
        PlayPendingRoomIntroText();
        PlayEventRoomEntranceAnimationIfNeeded();
        return true;
    }

    private bool TryOpenLayerZeroNodeOnNewRun()
    {
        if (mapRuntime == null || mapRuntime.CurrentNodeIndex >= 0)
            return false;

        GeneratedMapNodeData entryNode = MapRuntimeProgressUtility.FindStartNode(mapRuntime);
        if (entryNode == null)
        {
            Debug.LogWarning("[BattleSceneController] Generated map has no Layer 0 entry node.");
            return false;
        }

        mapRuntime.CurrentMapId = entryNode.MapId;
        mapRuntime.CurrentNodeIndex = entryNode.NodeIndex;
        mapRuntime.VisitedMapIds ??= new List<string>();

        string nodeKey = entryNode.NodeIndex.ToString();
        if (!mapRuntime.VisitedMapIds.Contains(nodeKey))
            mapRuntime.VisitedMapIds.Add(nodeKey);

        mapRuntimeStore.Set(mapRuntime);
        CaptureRoomEntrySaveCheckpoint();
        HideMapPanelImmediate();

        PrepareStageEntryPositionPanel();
        // Position_Panel is the stage-entry information panel. While it covers the map UI,
        // replace the editor default Back2 name with the actual first location.
        RefreshBack2LocationName(entryNode);
        StartCoroutine(PlayStageEntryThenOpenNodeRoutine(entryNode));
        return true;
    }

    private void PrepareStageEntryPositionPanel()
    {
        AutoFindStageEntryPositionPanelIfNeeded();

        if (positionPanel == null)
            return;

        ResolveStageEntryTexts(
            mapRuntime != null ? mapRuntime.CurrentStage : defaultStage,
            out string stageLabel,
            out string stageName);

        if (positionStageText != null)
            positionStageText.text = stageLabel;

        if (positionNameText != null)
            positionNameText.text = stageName;

        positionPanel.SetActive(true);

        if (positionPanelCanvasGroup == null)
            positionPanelCanvasGroup = positionPanel.GetComponent<CanvasGroup>();

        if (positionPanelCanvasGroup == null)
            positionPanelCanvasGroup = positionPanel.AddComponent<CanvasGroup>();

        positionPanelCanvasGroup.alpha = 1f;
        positionPanelCanvasGroup.interactable = false;
        positionPanelCanvasGroup.blocksRaycasts = false;
    }

    private IEnumerator PlayStageEntryThenOpenNodeRoutine(GeneratedMapNodeData entryNode)
    {
        CanvasMaterialSceneTransition sceneTransition = CanvasMaterialSceneTransition.Instance;
        SceneFlowManager sceneFlow = SceneFlowManager.Instance;

        // 씬 전환이 화면을 가리고 있는 동안 Position_Panel은 이미 활성화되어 있다.
        // BattleScene의 Start가 호출된 직후에는 PlayOpenAsync가 아직 시작되지 않았을 수 있으므로
        // 최소 한 프레임 기다린 뒤, 씬 로드와 열림 전환이 모두 끝난 시점부터 표시 시간을 계산한다.
        yield return null;

        while (sceneFlow != null && sceneFlow.IsLoading)
            yield return null;

        while (sceneTransition != null && sceneTransition.IsPlaying)
            yield return null;

        if (positionPanel != null && positionPanel.activeSelf)
        {
            float holdDuration = Mathf.Max(0f, positionPanelHoldDuration);
            float holdElapsed = 0f;

            while (holdElapsed < holdDuration)
            {
                holdElapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            float fadeDuration = Mathf.Max(0f, positionPanelFadeDuration);

            if (positionPanelCanvasGroup != null && fadeDuration > 0f)
            {
                float fadeElapsed = 0f;

                while (fadeElapsed < fadeDuration)
                {
                    fadeElapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(fadeElapsed / fadeDuration);
                    positionPanelCanvasGroup.alpha = 1f - t;
                    yield return null;
                }
            }

            if (positionPanelCanvasGroup != null)
                positionPanelCanvasGroup.alpha = 0f;

            positionPanel.SetActive(false);
        }

        HandleSelectedMap(entryNode);
        PlayPendingRoomIntroText();
        PlayEventRoomEntranceAnimationIfNeeded();
    }


    private void AutoFindBack2NameIfNeeded()
    {
        if (back2NameText != null)
            return;

        Transform back2 = FindSceneTransformByName("Back2");
        if (back2 == null)
            return;

        Transform nameTransform = FindChildRecursive(back2, "Name");
        if (nameTransform != null)
            back2NameText = nameTransform.GetComponent<TMP_Text>();
    }

    private void PrepareBack2NameForDynamicUse()
    {
        AutoFindBack2NameIfNeeded();
        if (back2NameText == null)
            return;

        // Back2 > Name은 지역명/턴을 런타임에서 직접 바꾸는 동적 텍스트입니다.
        // 씬에 남아 있는 정적 로컬라이저가 OnEnable/Start에서 기본 문구를 다시 덮어쓰지 않도록 합니다.
        LocalizedTMPText localizedTmp = back2NameText.GetComponent<LocalizedTMPText>();
        if (localizedTmp != null)
            localizedTmp.enabled = false;

        LocalizeStringEvent legacyLocalizer = back2NameText.GetComponent<LocalizeStringEvent>();
        if (legacyLocalizer != null)
            legacyLocalizer.enabled = false;
    }

    private void PrimeBack2NameBeforePresentation()
    {
        PrepareBack2NameForDynamicUse();

        if (mapRuntime == null)
            return;

        GeneratedMapNodeData currentNode = MapRuntimeProgressUtility.FindCurrentNode(mapRuntime);
        if (currentNode == null)
            currentNode = MapRuntimeProgressUtility.FindStartNode(mapRuntime);

        if (currentNode == null)
            return;

        bool isUnclearedCurrentBattle =
            currentNode.NodeIndex == mapRuntime.CurrentNodeIndex &&
            MapRuntimeProgressUtility.HasUnclearedCurrentNode(mapRuntime) &&
            IsBattleNodeType(currentNode.Type);

        if (isUnclearedCurrentBattle)
            SetBack2TurnNumber(1);
        else
            RefreshBack2LocationName(currentNode);
    }

    public void SetBack2TurnNumber(int turnNumber)
    {
        AutoFindBack2NameIfNeeded();
        if (back2NameText == null)
            return;

        back2NameText.text = $"턴 {Mathf.Max(1, turnNumber):D2}";
    }

    private void RefreshBack2LocationName(GeneratedMapNodeData nodeData)
    {
        if (nodeData == null)
            return;

        AutoFindBack2NameIfNeeded();
        if (back2NameText == null)
            return;

        string locationName = ResolveBack2LocationName(nodeData);
        if (string.IsNullOrWhiteSpace(locationName))
            return;

        int progressNumber = Mathf.Max(1, nodeData.LayerIndex + 1);
        back2NameText.text = $"{progressNumber:D2} {locationName}";
    }

    private string ResolveBack2LocationName(GeneratedMapNodeData nodeData)
    {
        AutoFindSharedRoomPresentationIfNeeded();

        string backgroundName = sharedBackgroundController != null
            ? sharedBackgroundController.ResolveBackgroundPrefabName(nodeData.MapId, nodeData.LayerIndex)
            : string.Empty;

        if (string.IsNullOrWhiteSpace(backgroundName))
        {
            StageBackgroundController fallbackController =
                Object.FindFirstObjectByType<StageBackgroundController>(FindObjectsInactive.Include);

            if (fallbackController != null)
                backgroundName = fallbackController.ResolveBackgroundPrefabName(nodeData.MapId, nodeData.LayerIndex);
        }

        switch (backgroundName)
        {
            case "St1_00":
                return "폐허 외곽";
            case "St1_01":
                return "성채 연결로";
            case "St1_02":
                return "내부 광장";
            case "Share_Restroom":
                return "휴식";
        }

        if (string.Equals(nodeData.Type, "Rest", StringComparison.OrdinalIgnoreCase))
            return "휴식";

        return backgroundName;
    }

    private void AutoFindStageEntryPositionPanelIfNeeded()
    {
        if (positionPanel == null)
        {
            Transform found = FindSceneTransformByName("Position_Panel")
                              ?? FindSceneTransformByName("PositionPanel");
            if (found != null)
                positionPanel = found.gameObject;
        }

        if (positionPanel == null)
            return;

        if (positionPanelCanvasGroup == null)
            positionPanelCanvasGroup = positionPanel.GetComponent<CanvasGroup>();

        if (positionStageText == null)
        {
            Transform stageTransform = FindChildRecursive(positionPanel.transform, "Stage_Text");
            if (stageTransform != null)
                positionStageText = stageTransform.GetComponent<TMP_Text>();
        }

        if (positionNameText == null)
        {
            Transform nameTransform = FindChildRecursive(positionPanel.transform, "Name_Text");
            if (nameTransform != null)
                positionNameText = nameTransform.GetComponent<TMP_Text>();
        }
    }

    private static Transform FindSceneTransformByName(string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
            return null;

        UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindChildRecursive(roots[i].transform, targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static void ResolveStageEntryTexts(string stageId, out string stageLabel, out string stageName)
    {
        switch (stageId?.Trim())
        {
            case "Stage2":
                stageLabel = "2구역";
                stageName = "모르덴 지하수로";
                break;

            case "Stage3":
                stageLabel = "3구역";
                stageName = "아우렐 묘지";
                break;

            case "Stage1":
            default:
                stageLabel = "1구역";
                stageName = "로데른 폐허";
                break;
        }
    }

    public async void OnMapNodeSelected(GeneratedMapNodeData nodeData)
    {
        if (nodeData == null)
            return;

        if (isChangingRoom)
            return;

        if (!MapRuntimeProgressUtility.IsNodeClickableFromCurrentProgress(mapRuntime, nodeData))
        {
            Debug.LogWarning(
                $"[BattleSceneController] Selectable next node validation failed: {nodeData.NodeIndex}");
            return;
        }

        // 방 내용은 실제로 이 노드를 선택한 순간 확정합니다.
        // 지도에 생성만 되고 지나가지 않은 방은 중복 방지 이력에 포함되지 않습니다.
        List<MapData> mapPool = DataManager.Instance.MapDatabase.GetAll();
        MapData resolvedMap = MapRoomSelectionResolver.ResolveForVisit(nodeData, mapRuntime, mapPool);
        MapRoomSelectionResolver.ApplyToNode(nodeData, resolvedMap);

        mapRuntime.CurrentMapId = nodeData.MapId;
        mapRuntime.CurrentNodeIndex = nodeData.NodeIndex;

        string nodeKey = nodeData.NodeIndex.ToString();

        if (!mapRuntime.VisitedMapIds.Contains(nodeKey))
            mapRuntime.VisitedMapIds.Add(nodeKey);

        mapRuntimeStore.Set(mapRuntime);
        CaptureRoomEntrySaveCheckpoint();

        Debug.Log(
            $"[BattleSceneController] Map Selected: " +
            $"{nodeData.MapId} / Node:{nodeData.NodeIndex} / {nodeData.Type}"
        );

        await PlayMapToRoomTransitionAsync(() =>
        {
            CleanupCompletedBattleRoom();
            HandleSelectedMap(nodeData);
        });

        PlayEventRoomEntranceAnimationIfNeeded();
    }

    public void OnMapNodeSelectedByIndex(int nodeIndex)
    {
        if (mapRuntime?.GeneratedNodes == null)
            return;

        for (int i = 0; i < mapRuntime.GeneratedNodes.Count; i++)
        {
            GeneratedMapNodeData node = mapRuntime.GeneratedNodes[i];
            if (node == null || node.NodeIndex != nodeIndex)
                continue;

            OnMapNodeSelected(node);
            return;
        }

        Debug.LogWarning($"[BattleSceneController] Map node not found: {nodeIndex}");
    }

    public void ReturnToMap()
    {
        ReturnToMap(null);
    }

    public async void ReturnToMap(System.Action onCovered)
    {
        if (isChangingRoom)
            return;

        GameObject roomToKeepVisible = FindActiveRoomObject();

        await PlayRoomToMapTransitionAsync(() =>
        {
            // 전환 화면이 방을 완전히 가린 시점에만 외부 UI 정리를 허용합니다.
            onCovered?.Invoke();
            PrepareRoomForMapSelection(roomToKeepVisible);
            OpenMapPanelImmediate();
        });

        UpdateLastActiveRoomState();
    }

    public void ReturnToMapPanel()
    {
        ReturnToMap();
    }

    public void OnBattleMapPanelEnabledExternally(GameObject enabledMapPanelObject)
    {
        if (!autoDetectReturnToMap)
            return;

        if (!isStarted)
            return;

        if (isOpeningMapFromController)
            return;

        if (battleMapPanel == null)
            return;

        if (enabledMapPanelObject != battleMapPanel.gameObject)
            return;

        if (isAutoReturningToMap)
        {
            HideMapPanelImmediate();
            return;
        }

        if (isChangingRoom)
            return;

        GameObject activeRoomObject = FindActiveRoomObject();
        if (activeRoomObject != null)
            lastActiveRoomLastFrame = activeRoomObject;

        GameObject roomToKeepVisible = autoReturnRoomToKeepVisible != null
            ? autoReturnRoomToKeepVisible
            : activeRoomObject != null
                ? activeRoomObject
                : lastActiveRoomLastFrame;

        if (roomToKeepVisible != null)
        {
            HideMapPanelImmediate();
            _ = PlayAutoRoomToMapTransitionFromCurrentRoomAsync(roomToKeepVisible);
            return;
        }

        if (!wasAnyRoomActiveLastFrame && !IsAnyRoomActive())
            return;

        _ = PlayAutoRoomToMapAlreadyCoveredTransitionAsync();
    }

    public void OnBattleRoomDisabledExternally(GameObject disabledRoomObject)
    {
        if (!autoDetectReturnToMap)
            return;

        if (!isStarted)
            return;

        if (disabledRoomObject == null)
            return;

        if (isChangingRoom || isOpeningMapFromController || isRestoringExternallyDisabledRoom)
            return;

        if (isAutoReturningToMap)
            return;

        if (!IsRoomObject(disabledRoomObject))
            return;

        autoReturnRoomToKeepVisible = disabledRoomObject;
        RestoreRoomObjectImmediate(disabledRoomObject);
        HideMapPanelImmediate();

        _ = PlayAutoRoomToMapTransitionFromCurrentRoomAsync(disabledRoomObject);
    }

    private async Task PlayAutoRoomToMapTransitionFromCurrentRoomAsync(GameObject roomToKeepVisible)
    {
        if (isChangingRoom)
            return;

        if (roomToKeepVisible == null)
            return;

        isAutoReturningToMap = true;
        autoReturnRoomToKeepVisible = roomToKeepVisible;
        RestoreRoomObjectImmediate(roomToKeepVisible);
        HideMapPanelImmediate();

        await PlayRoomToMapTransitionAsync(() =>
        {
            isAutoReturningToMap = false;
            autoReturnRoomToKeepVisible = null;
            PrepareRoomForMapSelection(roomToKeepVisible);
            OpenMapPanelImmediate();
        });

        isAutoReturningToMap = false;
        autoReturnRoomToKeepVisible = null;
        UpdateLastActiveRoomState();
    }

    private async Task PlayAutoRoomToMapTransitionAsync()
    {
        if (isChangingRoom)
            return;

        GameObject roomToKeepVisible = FindActiveRoomObject();
        if (roomToKeepVisible == null)
        {
            await PlayAutoRoomToMapAlreadyCoveredTransitionAsync();
            return;
        }

        await PlayAutoRoomToMapTransitionFromCurrentRoomAsync(roomToKeepVisible);
    }

    private async Task PlayAutoRoomToMapAlreadyCoveredTransitionAsync()
    {
        if (isChangingRoom)
            return;

        await PlayRoomToMapAlreadyCoveredTransitionAsync(() =>
        {
            GameObject roomToKeepVisible = FindActiveRoomObject() ?? lastActiveRoomLastFrame;
            PrepareRoomForMapSelection(roomToKeepVisible);
            OpenMapPanelImmediate();
        });

        UpdateLastActiveRoomState();
    }

    private async Task PlayMapToRoomTransitionAsync(System.Action onCovered)
    {
        isChangingRoom = true;

        if (battleTransition == null)
        {
            onCovered?.Invoke();
            PlayPendingRoomIntroText();
            isChangingRoom = false;
            UpdateLastActiveRoomState();
            return;
        }

        await battleTransition.PlayMapToRoomAsync(onCovered);
        PlayPendingRoomIntroText();
        isChangingRoom = false;
        UpdateLastActiveRoomState();
    }

    private async Task PlayRoomToMapTransitionAsync(System.Action onCovered)
    {
        isChangingRoom = true;

        if (battleTransition == null)
        {
            onCovered?.Invoke();
            isChangingRoom = false;
            UpdateLastActiveRoomState();
            return;
        }

        await battleTransition.PlayRoomToMapAsync(onCovered);
        isChangingRoom = false;
        UpdateLastActiveRoomState();
    }

    private async Task PlayRoomToMapAlreadyCoveredTransitionAsync(System.Action onCovered)
    {
        isChangingRoom = true;

        if (battleTransition == null)
        {
            onCovered?.Invoke();
            isChangingRoom = false;
            UpdateLastActiveRoomState();
            return;
        }

        await battleTransition.PlayRoomToMapAlreadyCoveredAsync(onCovered);
        isChangingRoom = false;
        UpdateLastActiveRoomState();
    }

    public void ApplyNetworkMapRuntime(MapRuntimeData runtime)
    {
        if (runtime == null || DataManager.Instance == null)
            return;

        if (mapRuntimeStore == null)
            mapRuntimeStore = DataManager.Instance.MapRuntimeStore;

        mapRuntime = runtime;
        bool isCurrentNodeCleared = MapRuntimeProgressUtility.IsCurrentNodeCleared(mapRuntime);

        if (lastNetworkAppliedNodeIndex == mapRuntime.CurrentNodeIndex &&
            lastNetworkAppliedNodeCleared == isCurrentNodeCleared)
        {
            return;
        }

        lastNetworkAppliedNodeIndex = mapRuntime.CurrentNodeIndex;
        lastNetworkAppliedNodeCleared = isCurrentNodeCleared;

        if (mapRuntime.CurrentNodeIndex >= 0 && !isCurrentNodeCleared)
        {
            GeneratedMapNodeData currentNode = MapRuntimeProgressUtility.FindCurrentNode(mapRuntime);
            if (currentNode != null)
            {
                forceNextBattleRoomLoad = IsBattleNodeType(currentNode.Type);
                CaptureRoomEntrySaveCheckpoint();
                HideMapPanelImmediate();
                HandleSelectedMap(currentNode);
                PlayPendingRoomIntroText();
                PlayEventRoomEntranceAnimationIfNeeded();
                UpdateLastActiveRoomState();
                return;
            }
        }

        CloseAllRooms();
        OpenMapPanelImmediate();
        UpdateLastActiveRoomState();
    }

    private void CaptureRoomEntrySaveCheckpoint()
    {
        GeneratedMapNodeData currentNode = MapRuntimeProgressUtility.FindCurrentNode(mapRuntime);
        BattleErosionRuntimeService.CountRoomEntry(currentNode);
        SaveSystem.Instance?.CaptureBattleRoomEntryCheckpoint();
    }

    private void SetupBattleErosionGauge()
    {
        Transform menuRoot = FindSceneTransformByName("MenuRoot");
        if (menuRoot == null)
            return;

        Transform erosionRoot = null;
        for (int i = 0; i < menuRoot.childCount; i++)
        {
            Transform child = menuRoot.GetChild(i);
            if (child != null && child.name == "Erosion")
            {
                erosionRoot = child;
                break;
            }
        }

        if (erosionRoot == null)
            return;

        BattleErosionGaugeUI gauge = erosionRoot.GetComponent<BattleErosionGaugeUI>();
        if (gauge == null)
        {
            Debug.LogWarning(
                "[BattleSceneController] MenuRoot/Erosion에 BattleErosionGaugeUI가 없습니다. " +
                "Erosion 오브젝트에 컴포넌트를 직접 추가하고 Fill/Value를 연결해 주세요.");
            return;
        }

        gauge.Initialize();
    }

    private void HandleSelectedMap(GeneratedMapNodeData nodeData)
    {
        switch (nodeData.Type)
        {
            case "Common":
            case "Elite":
                OpenBattleMap(nodeData);
                break;

            case "Boss":
                OpenBossBattle(nodeData);
                break;

            case "Rest":
                OpenRestEvent(nodeData);
                break;

            case "Shop":
                OpenSpecialEvent(nodeData);
                break;

            case "Start":
                OpenSpecialEvent(nodeData);
                break;

            case "Special":
                OpenSpecialEvent(nodeData);
                break;

            default:
                Debug.LogWarning($"[BattleSceneController] Unhandled map node type: {nodeData.Type}");
                break;
        }
    }

    private void OpenBattleMap(GeneratedMapNodeData nodeData)
    {
        Debug.Log($"[BattleSceneController] Battle room start: {nodeData.MapId}");
        pendingBattleRoomUsesBossIntro = false;
        pendingRoomIntroMessage = playBattleRoomIntroFromSceneController ? battleRoomIntroMessage : null;
        ShowRoomBackground(battleRoom, nodeData);
        SetBack2TurnNumber(1);
        OpenRoom(battleRoom, "BattleRoom");
        ApplyRoomVisual(battleRoom, nodeData);
    }

    private void OpenBossBattle(GeneratedMapNodeData nodeData)
    {
        Debug.Log($"[BattleSceneController] Boss battle start: {nodeData.MapId}");
        pendingBattleRoomUsesBossIntro = true;
        pendingRoomIntroMessage = playBattleRoomIntroFromSceneController ? battleRoomIntroMessage : null;
        ShowRoomBackground(battleRoom, nodeData, true);
        SetBack2TurnNumber(1);
        OpenRoom(battleRoom, "BattleRoom");
        ApplyRoomVisual(battleRoom, nodeData);
    }

    private void OpenRestEvent(GeneratedMapNodeData nodeData)
    {
        Debug.Log($"[BattleSceneController] Rest event start: {nodeData.MapId}");
        pendingBattleRoomUsesBossIntro = false;
        pendingRoomIntroMessage = restRoomIntroMessage;
        ShowRoomBackground(restRoom, nodeData);
        RefreshBack2LocationName(nodeData);
        OpenRoom(restRoom, "RestRoom");
        sharedRoomPresentationController?.RefreshForMap(nodeData.MapId);
        ApplyRoomVisual(restRoom, nodeData);
    }

    private void OpenSpecialEvent(GeneratedMapNodeData nodeData)
    {
        Debug.Log($"[BattleSceneController] Special event start: {nodeData.MapId} / Event:{nodeData.EventId}");
        pendingBattleRoomUsesBossIntro = false;

        pendingRoomIntroMessage = ResolveEventRoomIntroMessage(nodeData);
        EventRoomController eventController =
            eventRoom != null
                ? eventRoom.GetComponentInChildren<EventRoomController>(true)
                : null;

        if (eventController != null)
            eventController.SetEventId(nodeData.EventId);

        // EventRoom의 OnEnable에서 이벤트 선택지/연출이 즉시 실행될 수 있으므로
        // MapVisual을 먼저 생성한 뒤 EventRoom을 활성화한다.
        RefreshBack2LocationName(nodeData);
        ApplyRoomVisual(eventRoom, nodeData);
        OpenRoom(eventRoom, "EventRoom");
    }

    private static string ResolveEventRoomIntroMessage(GeneratedMapNodeData nodeData)
    {
        if (nodeData == null)
            return string.Empty;

        string eventId = EventIdUtility.Normalize(nodeData.EventId);
        if (string.IsNullOrWhiteSpace(eventId))
            return string.Empty;

        if (DataManager.Instance?.EventDatabase != null &&
            DataManager.Instance.EventDatabase.TryGetEvent(eventId, out EventDefinition definition) &&
            definition != null)
        {
            if (!string.IsNullOrWhiteSpace(definition.EventName))
                return definition.EventName.Trim();
        }

        Debug.LogWarning($"[BattleSceneController] EventName을 찾을 수 없습니다: {eventId}");
        return eventId;
    }

    private void PrepareRoomForMapSelection(GameObject completedRoom)
    {
        BattleRoomCleaner cleaner =
            Object.FindFirstObjectByType<BattleRoomCleaner>(FindObjectsInactive.Include);
        cleaner?.PrepareForMapSelection();

        if (completedRoom == eventRoom && eventRoom != null)
            eventRoom.SetActive(false);

        AutoFindSharedRoomPresentationIfNeeded();
        sharedRoomPresentationController?.RefreshForMapSelection(
            MapRuntimeProgressUtility.FindCurrentNode(mapRuntime)?.MapId);
    }

    public bool TryPlaySharedMapVisualAction(string visualObjectId, string actionId)
    {
        AutoFindSharedRoomPresentationIfNeeded();
        return sharedMapVisualController != null &&
               sharedMapVisualController.TryPlayAction(visualObjectId, actionId);
    }

    public bool TryReverseSharedMapVisualAction(string visualObjectId, string actionId)
    {
        AutoFindSharedRoomPresentationIfNeeded();
        return sharedMapVisualController != null &&
               sharedMapVisualController.TryReverseAction(visualObjectId, actionId);
    }

    private void PlayMapIntroTextOnStart()
    {
        if (!playMapIntroOnStart)
            return;

        if (string.IsNullOrEmpty(mapIntroMessage))
            return;

        if (battleMapIntroText == null)
            AutoFindBattleMapIntroTextIfNeeded();

        if (battleMapIntroText != null)
            battleMapIntroText.Play(mapIntroMessage);
    }

    private void PlayPendingRoomIntroText()
    {
        if (string.IsNullOrEmpty(pendingRoomIntroMessage))
            return;

        if (battleMapIntroText == null)
            AutoFindBattleMapIntroTextIfNeeded();

        if (battleMapIntroText != null)
        {
            if (eventRoom != null && eventRoom.activeInHierarchy)
                BattleMapIntroText.PlayRoomIntroSfx();

            battleMapIntroText.Play(pendingRoomIntroMessage);
        }

        pendingRoomIntroMessage = null;
    }

    private void PlayEventRoomEntranceAnimationIfNeeded()
    {
        if (eventRoom == null || !eventRoom.activeInHierarchy)
            return;

        EventRoomController eventController =
            eventRoom.GetComponentInChildren<EventRoomController>(true);
        eventController?.PlayEventChoiceEntranceAnimation();
    }

    private void OpenRoom(GameObject roomObject, string roomName)
    {
        mapSelectionPresenter?.Hide();

        if (battleMapPanel != null)
            battleMapPanel.Close();

        SetErosionSelectVisible(false);
        CloseInventoryAndBagPanelsImmediate();
        CloseAllRooms();

        if (roomObject == null)
        {
            Debug.LogWarning($"[BattleSceneController] {roomName} is not assigned.");
            return;
        }

        bool isBattleRoom = roomObject == battleRoom;
        ResetCameraForNonBattleRoom(roomObject, isBattleRoom);
        SetSharedPartyPresentationVisible(!isBattleRoom);

        if (isBattleRoom)
            SetBattleRoomIntroPlaying(true);

        roomObject.SetActive(true);

        if (isBattleRoom)
            RequestBattleRoomLoadOnce();
    }

    private static void ResetCameraForNonBattleRoom(GameObject roomObject, bool isBattleRoom)
    {
        if (roomObject == null || isBattleRoom)
            return;

        ResetCameraForMap();
    }

    private void CleanupCompletedBattleRoom()
    {
        BattleRoomCleaner cleaner =
            Object.FindFirstObjectByType<BattleRoomCleaner>(FindObjectsInactive.Include);
        cleaner?.Clean();
    }

    private void RequestBattleRoomLoadOnce()
    {
        if (battleRoom == null)
            return;

        CancelPendingBattleRoomIntro();

        IBattleRoomIntroSequence introSequence = ResolveBattleRoomIntroSequence();

        if (introSequence == null || introSequence.IsCompleted)
        {
            SetBattleRoomIntroPlaying(false);
        }
        else
        {
            SetBattleRoomIntroPlaying(true);
            SuppressBattleRoomExecutionUiUntilPlayerInputReady();
        }

        battleRoomIntroLoadGate.Request(
            introSequence,
            HandleBattleRoomIntroCompletedAndLoad
        );
    }

    private IBattleRoomIntroSequence ResolveBattleRoomIntroSequence()
    {
        IBattleRoomIntroSequence introSequence =
            BattleRoomIntroSequenceUtility.FindFirst(battleRoom);

        if (introSequence != null)
            return introSequence;

        if (!pendingBattleRoomUsesBossIntro)
            return null;

        AutoFindSharedRoomPresentationIfNeeded();

        introSequence = sharedBackgroundController != null
            ? sharedBackgroundController.CurrentBattleRoomIntroSequence
            : null;

        if (introSequence != null)
            return introSequence;

        return BattleRoomIntroSequenceUtility.FindFirst(sharedRoomRoot);
    }

    private void SuppressBattleRoomExecutionUiUntilPlayerInputReady()
    {
        BattleTurnExecutor[] executors = battleRoom != null
            ? battleRoom.GetComponentsInChildren<BattleTurnExecutor>(true)
            : null;

        if (executors == null || executors.Length == 0)
        {
            executors = Object.FindObjectsByType<BattleTurnExecutor>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
        }

        for (int i = 0; i < executors.Length; i++)
            executors[i]?.SuppressBattleExecutionUiUntilPlayerInputReady();
    }

    private void CancelPendingBattleRoomIntro()
    {
        battleRoomIntroLoadGate.Cancel();
    }

    private void HandleBattleRoomIntroCompletedAndLoad()
    {
        LoadBattleRoomNow();
        SetBattleRoomIntroPlaying(false);
    }

    private static void SetBattleRoomIntroPlaying(bool isPlaying)
    {
        if (IsBattleRoomIntroPlaying == isPlaying)
            return;

        IsBattleRoomIntroPlaying = isPlaying;

        if (isPlaying)
            BattleRoomIntroStarted?.Invoke();
        else
            BattleRoomIntroCompleted?.Invoke();
    }

    private void LoadBattleRoomNow()
    {
        if (battleRoom == null || !battleRoom.activeInHierarchy)
            return;

        BattleRoomLoader loader = battleRoom.GetComponentInChildren<BattleRoomLoader>(true);

        if (loader == null)
        {
            Debug.LogWarning("[BattleSceneController] BattleRoomLoader is missing in BattleRoom.");
            return;
        }

        bool forceReload = forceNextBattleRoomLoad;
        forceNextBattleRoomLoad = false;
        loader.LoadBattleFromSceneController(forceReload);
    }

    private static bool IsBattleNodeType(string nodeType)
    {
        return nodeType == "Common" ||
               nodeType == "Elite" ||
               nodeType == "Boss";
    }

    private void CloseAllRooms()
    {
        CancelPendingBattleRoomIntro();
        SetBattleRoomIntroPlaying(false);
        CloseInventoryAndBagPanelsImmediate();

        if (roomRoot != null)
        {
            for (int i = 0; i < roomRoot.childCount; i++)
            {
                GameObject roomObject = roomRoot.GetChild(i).gameObject;
                if (roomObject == sharedRoomRoot)
                    continue;

                roomObject.SetActive(false);
            }

            if (sharedRoomRoot != null && !sharedRoomRoot.activeSelf)
                sharedRoomRoot.SetActive(true);

            return;
        }

        SetActiveIfNotNull(battleRoom, false);
        SetActiveIfNotNull(eventRoom, false);
        SetActiveIfNotNull(restRoom, false);
    }

    private void UpdateRoomPanelAutoCloseState()
    {
        if (!closeInventoryAndBagOnRoomActiveChange)
            return;

        GameObject activeRoomObject = FindActiveRoomObject();
        bool anyRoomActive = activeRoomObject != null;

        if (!hasRoomPanelAutoCloseState)
        {
            hasRoomPanelAutoCloseState = true;
            lastAnyRoomActiveForPanelAutoClose = anyRoomActive;
            lastActiveRoomForPanelAutoClose = activeRoomObject;
            return;
        }

        bool roomStateChanged = anyRoomActive != lastAnyRoomActiveForPanelAutoClose ||
                                activeRoomObject != lastActiveRoomForPanelAutoClose;

        if (roomStateChanged)
            CloseInventoryAndBagPanelsImmediate();

        lastAnyRoomActiveForPanelAutoClose = anyRoomActive;
        lastActiveRoomForPanelAutoClose = activeRoomObject;
    }

    private void CloseInventoryAndBagPanelsImmediate()
    {
        if (!closeInventoryAndBagOnRoomActiveChange)
            return;

        InventoryPanelSelectionResetter.ResetAllSelectionsExcept(null);
        CloseInventoryPanelsImmediate();
        CloseBagPanelsImmediate();
    }

    private void CloseInventoryPanelsImmediate()
    {
        GameObject[] inventoryPanels = FindObjectsByNames(inventoryPanelObjectNames);

        for (int i = 0; i < inventoryPanels.Length; i++)
        {
            GameObject inventoryPanel = inventoryPanels[i];

            if (inventoryPanel == null)
                continue;

            RectTransform rect = inventoryPanel.GetComponent<RectTransform>();
            if (rect != null)
                rect.anchoredPosition = new Vector2(0f, inventoryClosedY);

            ClearSelectedObjectIfChildOf(inventoryPanel);
        }
    }

    private void CloseBagPanelsImmediate()
    {
        GameObject[] namedBagPanels = FindObjectsByNames(bagPanelObjectNames);

        for (int i = 0; i < namedBagPanels.Length; i++)
            CloseBagPanelImmediate(namedBagPanels[i]);

        BattleBagPanelUI[] bagPanels = Object.FindObjectsByType<BattleBagPanelUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < bagPanels.Length; i++)
        {
            if (bagPanels[i] == null)
                continue;

            CloseBagPanelImmediate(bagPanels[i].gameObject);
        }
    }

    private void CloseBagPanelImmediate(GameObject panelObject)
    {
        if (panelObject == null)
            return;

        ClearSelectedObjectIfChildOf(panelObject);

        if (!panelObject.activeSelf)
            panelObject.SetActive(true);

        RectTransform rect = panelObject.GetComponent<RectTransform>();
        if (rect != null)
            rect.anchoredPosition = new Vector2(bagClosedX, rect.anchoredPosition.y);
    }

    private void ClosePanelGameObjectImmediate(GameObject panelObject)
    {
        if (panelObject == null)
            return;

        ClearSelectedObjectIfChildOf(panelObject);
        UIPanelButton.ClearCurrentOpenedPanelIfPanel(panelObject);

        if (panelObject.activeSelf)
            panelObject.SetActive(false);
    }

    private GameObject[] FindObjectsByNames(string[] names)
    {
        if (names == null || names.Length == 0)
            return System.Array.Empty<GameObject>();

        GameObject[] objects = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        System.Collections.Generic.List<GameObject> results = new();

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject candidate = objects[i];

            if (candidate == null)
                continue;

            if (IsNameInList(candidate.name, names))
                results.Add(candidate);
        }

        return results.ToArray();
    }

    private bool IsNameInList(string objectName, string[] names)
    {
        if (string.IsNullOrWhiteSpace(objectName) || names == null)
            return false;

        string normalizedObjectName = NormalizeObjectName(objectName);

        for (int i = 0; i < names.Length; i++)
        {
            string name = names[i];

            if (string.IsNullOrWhiteSpace(name))
                continue;

            if (normalizedObjectName == NormalizeObjectName(name))
                return true;
        }

        return false;
    }

    private string NormalizeObjectName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return string.Empty;

        return objectName.Replace("(Clone)", string.Empty).Trim();
    }

    private void ClearSelectedObjectIfChildOf(GameObject root)
    {
        EventSystem eventSystem = EventSystem.current;

        if (eventSystem == null || eventSystem.currentSelectedGameObject == null || root == null)
            return;

        if (eventSystem.currentSelectedGameObject.transform.IsChildOf(root.transform))
            eventSystem.SetSelectedGameObject(null);
    }

    private void KeepExternalReturnRoomVisibleIfNeeded()
    {
        if (!isAutoReturningToMap)
            return;

        if (autoReturnRoomToKeepVisible != null && !autoReturnRoomToKeepVisible.activeSelf)
            RestoreRoomObjectImmediate(autoReturnRoomToKeepVisible);

        HideMapPanelImmediate();
    }

    private void RestoreRoomObjectImmediate(GameObject roomObject)
    {
        if (roomObject == null)
            return;

        if (roomObject.activeSelf)
            return;

        isRestoringExternallyDisabledRoom = true;
        roomObject.SetActive(true);
        isRestoringExternallyDisabledRoom = false;
    }

    private void UpdateLastActiveRoomState()
    {
        GameObject activeRoomObject = FindActiveRoomObject();
        if (activeRoomObject != null)
            lastActiveRoomLastFrame = activeRoomObject;

        wasAnyRoomActiveLastFrame = activeRoomObject != null;
    }

    private bool IsAnyRoomActive()
    {
        return FindActiveRoomObject() != null;
    }

    private GameObject FindActiveRoomObject()
    {
        if (roomRoot != null)
        {
            for (int i = 0; i < roomRoot.childCount; i++)
            {
                GameObject roomObject = roomRoot.GetChild(i).gameObject;
                if (roomObject == sharedRoomRoot)
                    continue;

                if (roomObject.activeSelf)
                    return roomObject;
            }

            return null;
        }

        if (IsActiveSelf(battleRoom))
            return battleRoom;
        if (IsActiveSelf(eventRoom))
            return eventRoom;

        if (IsActiveSelf(restRoom))
            return restRoom;

        return null;
    }

    private bool IsRoomObject(GameObject target)
    {
        if (target == null)
            return false;

        if (roomRoot != null)
        {
            for (int i = 0; i < roomRoot.childCount; i++)
            {
                GameObject roomObject = roomRoot.GetChild(i).gameObject;
                if (roomObject == sharedRoomRoot)
                    continue;

                if (roomObject == target)
                    return true;
            }

            return false;
        }

        return target == battleRoom ||
               target == eventRoom ||
               target == restRoom;
    }

    private bool IsMapPanelActive()
    {
        return battleMapPanel != null && battleMapPanel.gameObject.activeSelf;
    }

    private bool IsActiveSelf(GameObject target)
    {
        return target != null && target.activeSelf;
    }

    private void SetActiveIfNotNull(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    private void SetSharedPartyPresentationVisible(bool visible)
    {
        AutoFindSharedRoomPresentationIfNeeded();

        if (sharedPartyPresentationRoot != null &&
            sharedPartyPresentationRoot.activeSelf != visible)
        {
            sharedPartyPresentationRoot.SetActive(visible);
        }
    }

    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
            return null;

        if (root.name == targetName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }

        return null;
    }
}

public sealed class BattleErosionSelectClickHandler : MonoBehaviour, IPointerClickHandler
{
    private Action onClick;

    public void Initialize(Action clickAction)
    {
        onClick = clickAction;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;

        onClick?.Invoke();
    }
}
