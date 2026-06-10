using UnityEngine;
using Relic.Gameplay.Data;

public class BattleSceneController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private BattleMapPanel battleMapPanel;

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

    private void Start()
    {
        InitializeRuntime();
        OpenMapPanel();
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

    private void OpenMapPanel()
    {
        if (battleMapPanel == null)
        {
            Debug.LogWarning("[BattleSceneController] BattleMapPanel이 연결되지 않았습니다.");
            return;
        }

        battleMapPanel.Open(mapRuntime);
    }

    public void OnMapNodeSelected(GeneratedMapNodeData nodeData)
    {
        if (nodeData == null)
            return;

        mapRuntime.CurrentMapId = nodeData.MapId;

        if (!mapRuntime.VisitedMapIds.Contains(nodeData.MapId))
            mapRuntime.VisitedMapIds.Add(nodeData.MapId);

        mapRuntimeStore.Set(mapRuntime);

        Debug.Log($"[BattleSceneController] Map Selected: {nodeData.MapId} / {nodeData.Type}");

        HandleSelectedMap(nodeData);
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
                Debug.LogWarning($"[BattleSceneController] 처리되지 않은 맵 타입: {nodeData.Type}");
                break;
        }
    }

    private void OpenStartEvent(GeneratedMapNodeData nodeData)
    {
        battleMapPanel.Close();

        if (startRoom != null)
            startRoom.SetActive(true);
    }

    private void OpenBattleMap(GeneratedMapNodeData nodeData)
    {
        Debug.Log($"[BattleSceneController] 전투 맵 시작: {nodeData.MapId}");
        battleMapPanel.Close();

        // 여기서 BattleMapId 기반으로 전투맵 프리팹/배경/몬스터 로드
    }

    private void OpenRestEvent(GeneratedMapNodeData nodeData)
    {
        Debug.Log($"[BattleSceneController] 휴식 이벤트 시작: {nodeData.MapId}");
        battleMapPanel.Close();

        // 휴식 패널 열기
    }

    private void OpenShopEvent(GeneratedMapNodeData nodeData)
    {
        Debug.Log($"[BattleSceneController] 상점 이벤트 시작: {nodeData.MapId}");
        battleMapPanel.Close();

        // 상점 패널 열기
    }

    private void OpenRewardEvent(GeneratedMapNodeData nodeData)
    {
        Debug.Log($"[BattleSceneController] 보상 이벤트 시작: {nodeData.MapId}");
        battleMapPanel.Close();

        // 보상 패널 열기
    }

    private void OpenSpecialEvent(GeneratedMapNodeData nodeData)
    {
        Debug.Log($"[BattleSceneController] 특수 이벤트 시작: {nodeData.MapId}");
        battleMapPanel.Close();

        // EventId 기반 이벤트 패널 열기
    }
}