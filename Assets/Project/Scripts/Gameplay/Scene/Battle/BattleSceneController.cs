using System.Threading.Tasks;
using UnityEngine;
using Relic.Gameplay.Data;

public class BattleSceneController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private BattleMapPanel battleMapPanel;

    [Header("Battle Scene Transition")]
    [SerializeField] private BattleDiagonalSceneTransition battleTransition;

    [Header("Auto Return To Map")]
    [SerializeField] private bool autoDetectReturnToMap = true;
    [SerializeField] private Transform roomRoot;

    [Header("Runtime Default")]
    [SerializeField] private string defaultChapterId = "Chapter1";
    [SerializeField] private string defaultStage = "Stage1";

    [Header("Rooms")]
    [SerializeField] private GameObject startRoom;
    [SerializeField] private GameObject battleRoom;
    [SerializeField] private GameObject chestRoom;
    [SerializeField] private GameObject eventRoom;
    [SerializeField] private GameObject restRoom;
    [SerializeField] private GameObject shopRoom;

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

    private void Awake()
    {
        AutoFindRoomRootIfNeeded();
        InstallMapPanelAutoReturnWatcher();
    }

    private void Start()
    {
        InitializeRuntime();
        CloseAllRooms();
        OpenMapPanelImmediate();

        lastActiveRoomLastFrame = FindActiveRoomObject();
        wasAnyRoomActiveLastFrame = lastActiveRoomLastFrame != null;
        isStarted = true;
    }

    private void LateUpdate()
    {
        KeepExternalReturnRoomVisibleIfNeeded();

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

    public async void OnMapNodeSelected(GeneratedMapNodeData nodeData)
    {
        if (nodeData == null)
            return;

        if (isChangingRoom)
            return;

        mapRuntime.CurrentMapId = nodeData.MapId;

        if (!mapRuntime.VisitedMapIds.Contains(nodeData.MapId))
            mapRuntime.VisitedMapIds.Add(nodeData.MapId);

        mapRuntimeStore.Set(mapRuntime);

        Debug.Log($"[BattleSceneController] Map Selected: {nodeData.MapId} / {nodeData.Type}");

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
            isChangingRoom = false;
            UpdateLastActiveRoomState();
            return;
        }

        await battleTransition.PlayMapToRoomAsync(onCovered);
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
            case "Boss":
                OpenBattleMap(nodeData);
                break;

            case "Rest":
                OpenRestEvent(nodeData);
                break;

            case "Shop":
                OpenShopEvent(nodeData);
                break;

            case "Chest":
                OpenRewardEvent(nodeData);
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
        OpenRoom(startRoom, "StartRoom");
    }

    private void OpenBattleMap(GeneratedMapNodeData nodeData)
    {
        Debug.Log($"[BattleSceneController] Battle room start: {nodeData.MapId}");
        OpenRoom(battleRoom, "BattleRoom");
    }

    private void OpenRestEvent(GeneratedMapNodeData nodeData)
    {
        Debug.Log($"[BattleSceneController] Rest event start: {nodeData.MapId}");
        OpenRoom(restRoom, "RestRoom");
    }

    private void OpenShopEvent(GeneratedMapNodeData nodeData)
    {
        Debug.Log($"[BattleSceneController] Shop event start: {nodeData.MapId}");
        OpenRoom(shopRoom, "ShopRoom");
    }

    private void OpenRewardEvent(GeneratedMapNodeData nodeData)
    {
        Debug.Log($"[BattleSceneController] Chest reward start: {nodeData.MapId}");
        OpenRoom(chestRoom, "ChestRoom");
    }

    private void OpenSpecialEvent(GeneratedMapNodeData nodeData)
    {
        Debug.Log($"[BattleSceneController] Special event start: {nodeData.MapId}");
        OpenRoom(eventRoom, "EventRoom");
    }

    private void OpenRoom(GameObject roomObject, string roomName)
    {
        if (battleMapPanel != null)
            battleMapPanel.Close();

        CloseAllRooms();

        if (roomObject == null)
        {
            Debug.LogWarning($"[BattleSceneController] {roomName} is not assigned.");
            return;
        }

        roomObject.SetActive(true);
    }

    private void CloseAllRooms()
    {
        if (roomRoot != null)
        {
            for (int i = 0; i < roomRoot.childCount; i++)
                roomRoot.GetChild(i).gameObject.SetActive(false);

            return;
        }

        SetActiveIfNotNull(startRoom, false);
        SetActiveIfNotNull(battleRoom, false);
        SetActiveIfNotNull(chestRoom, false);
        SetActiveIfNotNull(eventRoom, false);
        SetActiveIfNotNull(restRoom, false);
        SetActiveIfNotNull(shopRoom, false);
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

        if (IsActiveSelf(chestRoom))
            return chestRoom;

        if (IsActiveSelf(eventRoom))
            return eventRoom;

        if (IsActiveSelf(restRoom))
            return restRoom;

        if (IsActiveSelf(shopRoom))
            return shopRoom;

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
               target == chestRoom ||
               target == eventRoom ||
               target == restRoom ||
               target == shopRoom;
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
