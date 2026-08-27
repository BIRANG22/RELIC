using System;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
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

    [Header("Battle Scene Transition")]
    [SerializeField] private BattleDiagonalSceneTransition battleTransition;

    [Header("Battle Map Intro Text")]
    [SerializeField] private BattleMapIntroText battleMapIntroText;
    [SerializeField] private string mapIntroMessage = "제1구역 폐허";
    [SerializeField] private string battleRoomIntroMessage = "전투 시작";
    [SerializeField] private string restRoomIntroMessage = "휴식 구역";
    [SerializeField] private bool playMapIntroOnStart = true;
    [SerializeField] private bool playBattleRoomIntroFromSceneController = false;

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
        AutoFindRoomRootIfNeeded();
        AutoFindSharedRoomPresentationIfNeeded();
        AutoFindBattleMapIntroTextIfNeeded();
        InstallMapPanelAutoReturnWatcher();

        if (mapSelectionPresenter == null)
            mapSelectionPresenter = GetComponent<BattleRoomMapSelectionPresenter>();

        if (mapSelectionPresenter == null)
            mapSelectionPresenter = gameObject.AddComponent<BattleRoomMapSelectionPresenter>();
    }

    private void Start()
    {
        SteamBattleStateSynchronizer.EnsureForBattleScene(null, null);
        InitializeRuntime();
        CloseAllRooms();

        if (battleMapPanel != null)
            battleMapPanel.Prepare(mapRuntime);

        if (!TryOpenUnclearedCurrentNodeOnStart() && !TryOpenLayerZeroNodeOnNewRun())
        {
            OpenMapPanelImmediate();
            PlayMapIntroTextOnStart();
        }

        lastActiveRoomLastFrame = FindActiveRoomObject();
        wasAnyRoomActiveLastFrame = lastActiveRoomLastFrame != null;
        isStarted = true;
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
            sharedBackgroundController.ShowForLayer(nodeData.LayerIndex, playBossReveal);
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

        controller.ShowForLayer(nodeData.LayerIndex, playBossReveal);
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

        isOpeningMapFromController = true;
        battleMapPanel.Open(mapRuntime);
        isOpeningMapFromController = false;
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

        HideMapPanelImmediate();
        HandleSelectedMap(currentNode);
        PlayPendingRoomIntroText();
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
        HandleSelectedMap(entryNode);
        PlayPendingRoomIntroText();
        return true;
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

    public async void ReturnToMap()
    {
        if (isChangingRoom)
            return;

        GameObject roomToKeepVisible = FindActiveRoomObject();

        await PlayRoomToMapTransitionAsync(() =>
        {
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
        SaveSystem.Instance?.CaptureBattleRoomEntryCheckpoint();
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
        OpenRoom(battleRoom, "BattleRoom");
        ApplyRoomVisual(battleRoom, nodeData);
    }

    private void OpenBossBattle(GeneratedMapNodeData nodeData)
    {
        Debug.Log($"[BattleSceneController] Boss battle start: {nodeData.MapId}");
        pendingBattleRoomUsesBossIntro = true;
        pendingRoomIntroMessage = playBattleRoomIntroFromSceneController ? battleRoomIntroMessage : null;
        ShowRoomBackground(battleRoom, nodeData, true);
        OpenRoom(battleRoom, "BattleRoom");
        ApplyRoomVisual(battleRoom, nodeData);
    }

    private void OpenRestEvent(GeneratedMapNodeData nodeData)
    {
        Debug.Log($"[BattleSceneController] Rest event start: {nodeData.MapId}");
        pendingBattleRoomUsesBossIntro = false;
        pendingRoomIntroMessage = restRoomIntroMessage;
        ShowRoomBackground(restRoom, nodeData);
        OpenRoom(restRoom, "RestRoom");
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
        sharedRoomPresentationController?.RefreshNow();
    }

    public bool TryPlaySharedMapVisualAction(string visualObjectId, string actionId)
    {
        AutoFindSharedRoomPresentationIfNeeded();
        return sharedMapVisualController != null &&
               sharedMapVisualController.TryPlayAction(visualObjectId, actionId);
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
            battleMapIntroText.Play(pendingRoomIntroMessage);

        pendingRoomIntroMessage = null;
    }

    private void OpenRoom(GameObject roomObject, string roomName)
    {
        mapSelectionPresenter?.Hide();

        if (battleMapPanel != null)
            battleMapPanel.Close();

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
