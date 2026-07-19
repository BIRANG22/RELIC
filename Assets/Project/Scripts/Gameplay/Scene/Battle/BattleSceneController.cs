using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using Relic.Gameplay.Data;

public class BattleSceneController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private BattleMapPanel battleMapPanel;

    [Header("Battle Scene Transition")]
    [SerializeField] private BattleDiagonalSceneTransition battleTransition;

    [Header("Battle Only Tab")]
    [Tooltip("전투방에서만 표시할 TapButton/Battle 오브젝트입니다.")]
    [SerializeField] private GameObject battleOnlyTabRoot;

    [Header("Battle Map Intro Text")]
    [SerializeField] private BattleMapIntroText battleMapIntroText;
    [SerializeField] private string mapIntroMessage = "제1구역 폐허";
    [SerializeField] private string startRoomIntroMessage = "수상한 자와 조우";
    [SerializeField] private string battleRoomIntroMessage = "전투 시작";
    [SerializeField] private string restRoomIntroMessage = "휴식 구역";
    [SerializeField] private string eventRoomIntroMessage = "낡은 보관함";
    [SerializeField] private bool playMapIntroOnStart = true;
    [SerializeField] private bool playBattleRoomIntroFromSceneController = false;

    [Header("Auto Return To Map")]
    [SerializeField] private bool autoDetectReturnToMap = true;
    [SerializeField] private Transform roomRoot;

    [Header("Runtime Default")]
    [SerializeField] private string defaultChapterId = "Chapter1";
    [SerializeField] private string defaultStage = "Stage1";

    [Header("Rooms")]
    [SerializeField] private GameObject startRoom;
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

    private void Awake()
    {
        AutoFindRoomRootIfNeeded();
        AutoFindBattleMapIntroTextIfNeeded();
        InstallMapPanelAutoReturnWatcher();
        SetBattleOnlyTabActive(false);
    }

    private void Start()
    {
        InitializeRuntime();
        CloseAllRooms();

        if (!TryOpenUnclearedCurrentNodeOnStart())
        {
            OpenMapPanelImmediate();
            PlayMapIntroTextOnStart();
        }

        lastActiveRoomLastFrame = FindActiveRoomObject();
        wasAnyRoomActiveLastFrame = lastActiveRoomLastFrame != null;
        isStarted = true;
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

    private void AutoFindBattleMapIntroTextIfNeeded()
    {
        if (battleMapIntroText != null)
            return;

        battleMapIntroText = Object.FindFirstObjectByType<BattleMapIntroText>(FindObjectsInactive.Include);
    }

    private void ShowRoomBackground(GameObject room, GeneratedMapNodeData nodeData)
    {
        StageBackgroundController controller = room != null
            ? room.GetComponentInChildren<StageBackgroundController>(true)
            : null;

        if (controller == null)
        {
            string roomName = room != null ? room.name : "null";
            Debug.LogWarning($"[BattleSceneController] StageBackgroundController is missing in {roomName}.");
            return;
        }

        controller.ShowForLayer(nodeData.LayerIndex);
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

        isOpeningMapFromController = true;
        battleMapPanel.Open(mapRuntime);
        isOpeningMapFromController = false;
    }

    private void HideMapPanelImmediate()
    {
        if (battleMapPanel == null)
            return;

        if (battleMapPanel.gameObject.activeSelf)
            battleMapPanel.gameObject.SetActive(false);
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

        Debug.Log(
            $"[BattleSceneController] Restore uncleared map node: " +
            $"{currentNode.MapId} / Node:{currentNode.NodeIndex} / {currentNode.Type}"
        );

        HideMapPanelImmediate();
        HandleSelectedMap(currentNode);
        PlayPendingRoomIntroText();
        return true;
    }

    public async void OnMapNodeSelected(GeneratedMapNodeData nodeData)
    {
        if (nodeData == null)
            return;

        if (isChangingRoom)
            return;

        mapRuntime.CurrentMapId = nodeData.MapId;
        mapRuntime.CurrentNodeIndex = nodeData.NodeIndex;

        string nodeKey = nodeData.NodeIndex.ToString();

        if (!mapRuntime.VisitedMapIds.Contains(nodeKey))
            mapRuntime.VisitedMapIds.Add(nodeKey);

        mapRuntimeStore.Set(mapRuntime);

        Debug.Log(
            $"[BattleSceneController] Map Selected: " +
            $"{nodeData.MapId} / Node:{nodeData.NodeIndex} / {nodeData.Type}"
        );

        await PlayMapToRoomTransitionAsync(() => HandleSelectedMap(nodeData));
    }

    public async void ReturnToMap()
    {
        if (isChangingRoom)
            return;

        await PlayRoomToMapTransitionAsync(() =>
        {
            CloseAllRooms();
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
            CloseAllRooms();
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
            CloseAllRooms();
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

    private void HandleSelectedMap(GeneratedMapNodeData nodeData)
    {
        switch (nodeData.Type)
        {
            case "Start":
                OpenStartEvent(nodeData);
                break;

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

    private void OpenStartEvent(GeneratedMapNodeData nodeData)
    {
        pendingRoomIntroMessage = startRoomIntroMessage;
        ShowRoomBackground(startRoom, nodeData);
        OpenRoom(startRoom, "StartRoom");
    }

    private void OpenBattleMap(GeneratedMapNodeData nodeData)
    {
        Debug.Log($"[BattleSceneController] Battle room start: {nodeData.MapId}");
        pendingRoomIntroMessage = playBattleRoomIntroFromSceneController ? battleRoomIntroMessage : null;
        ShowRoomBackground(battleRoom, nodeData);
        OpenRoom(battleRoom, "BattleRoom");
    }

    private void OpenBossBattle(GeneratedMapNodeData nodeData)
    {
        Debug.Log($"[BattleSceneController] Boss battle start: {nodeData.MapId}");
        pendingRoomIntroMessage = playBattleRoomIntroFromSceneController ? battleRoomIntroMessage : null;
        ShowRoomBackground(battleRoom, nodeData);
        OpenRoom(battleRoom, "BattleRoom");
    }

    private void OpenRestEvent(GeneratedMapNodeData nodeData)
    {
        Debug.Log($"[BattleSceneController] Rest event start: {nodeData.MapId}");
        pendingRoomIntroMessage = restRoomIntroMessage;
        ShowRoomBackground(restRoom, nodeData);
        OpenRoom(restRoom, "RestRoom");
    }

    private void OpenSpecialEvent(GeneratedMapNodeData nodeData)
    {
        Debug.Log($"[BattleSceneController] Special event start: {nodeData.MapId}");

        // 현재는 이벤트방 노드도 RestRoom 오브젝트를 함께 사용할 수 있으므로
        // Event Room Intro Message 기본값을 휴식 구역으로 둔다.
        // 나중에 실제 EventRoom을 추가하면 인스펙터에서 문구만 바꾸면 된다.
        pendingRoomIntroMessage = eventRoomIntroMessage;
        OpenRoom(eventRoom, "EventRoom");
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
        if (battleMapPanel != null)
            battleMapPanel.Close();

        CloseInventoryAndBagPanelsImmediate();
        CloseAllRooms();

        if (roomObject == null)
        {
            Debug.LogWarning($"[BattleSceneController] {roomName} is not assigned.");
            return;
        }

        roomObject.SetActive(true);

        bool isBattleRoom = roomObject == battleRoom;
        SetBattleOnlyTabActive(isBattleRoom);

        if (isBattleRoom)
            RequestBattleRoomLoadOnce();
    }

    private void RequestBattleRoomLoadOnce()
    {
        if (battleRoom == null)
            return;

        BattleRoomLoader loader = battleRoom.GetComponentInChildren<BattleRoomLoader>(true);

        if (loader == null)
        {
            Debug.LogWarning("[BattleSceneController] BattleRoomLoader is missing in BattleRoom.");
            return;
        }

        loader.LoadBattleFromSceneController();
    }

    private void CloseAllRooms()
    {
        SetBattleOnlyTabActive(false);
        CloseInventoryAndBagPanelsImmediate();

        if (roomRoot != null)
        {
            for (int i = 0; i < roomRoot.childCount; i++)
                roomRoot.GetChild(i).gameObject.SetActive(false);

            return;
        }

        SetActiveIfNotNull(startRoom, false);
        SetActiveIfNotNull(battleRoom, false);
        SetActiveIfNotNull(eventRoom, false);
        SetActiveIfNotNull(restRoom, false);
    }

    private void SetBattleOnlyTabActive(bool isActive)
    {
        if (battleOnlyTabRoot == null)
            return;

        if (battleOnlyTabRoot.activeSelf != isActive)
            battleOnlyTabRoot.SetActive(isActive);
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
                if (roomObject.activeSelf)
                    return roomObject;
            }

            return null;
        }

        if (IsActiveSelf(startRoom))
            return startRoom;

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
                if (roomRoot.GetChild(i).gameObject == target)
                    return true;
            }

            return false;
        }

        return target == startRoom ||
               target == battleRoom ||
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
}
