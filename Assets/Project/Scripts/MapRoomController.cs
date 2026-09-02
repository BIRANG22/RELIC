using Relic.Gameplay.Data;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 지도 화면에 표시되는 전용 MapRoom을 갱신합니다.
/// 직전에 진행한 맵 노드의 LayerIndex에 맞는 배경을 StageBackgroundController로 표시하고,
/// 현재 파티 0~2번 캐릭터를 EventAllyPoint0~2에 배치합니다.
/// </summary>
public class MapRoomController : MonoBehaviour
{
    [Header("Map Panel")]
    [Tooltip("지도 패널입니다. 비워두면 BattleMapPanel을 자동으로 찾습니다.")]
    [SerializeField] private BattleMapPanel battleMapPanel;

    [Header("Background")]
    [Tooltip("MapRoom 전용 StageBackgroundController입니다. Background/Stage_01 쪽에 붙인 컴포넌트를 연결하세요.")]
    [SerializeField] private StageBackgroundController stageBackgroundController;
    [Tooltip("현재 노드 정보를 찾을 수 없을 때 표시할 기본 LayerIndex입니다. 0이면 첫 번째 지역입니다.")]
    [SerializeField] private int fallbackLayerIndex = 0;

    [Header("Party Allies")]
    [Tooltip("EventAllyPoint0~2가 들어 있는 AllyRoot입니다.")]
    [SerializeField] private Transform allyRoot;
    [SerializeField] private Transform[] allySpawnPoints = new Transform[3];
    [SerializeField] private float allySpawnScale = 1f;
    [SerializeField] private bool autoFindAllySpawnPoints = true;
    [Tooltip("이 Map ID에서는 AllyRoot와 맵 선택용 아군을 사용하지 않습니다.")]
    [SerializeField] private List<string> skipAllyRootMapIds = new();

    [Header("Refresh")]
    [Tooltip("지도 패널이 비활성 -> 활성으로 바뀔 때 자동으로 다시 갱신합니다.")]
    [SerializeField] private bool refreshWhenMapOpens = true;

    private bool wasMapOpen;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        RefreshNow();
        wasMapOpen = IsMapOpen();
    }

    private void Update()
    {
        if (!refreshWhenMapOpens)
            return;

        bool isMapOpen = IsMapOpen();

        if (isMapOpen && !wasMapOpen)
            RefreshNow();

        wasMapOpen = isMapOpen;
    }

    public void RefreshNow()
    {
        RefreshForMap(ResolveCurrentMapId());
    }

    public void RefreshForMap(string mapId)
    {
        ResolveReferences();
        RefreshBackground(mapId);
        SpawnPartyAllies(mapId);
    }

    private void RefreshBackground(string mapId)
    {
        if (stageBackgroundController == null)
            return;

        int layerIndex = ResolveCurrentLayerIndex();
        stageBackgroundController.ShowForMap(mapId, layerIndex);
    }

    private int ResolveCurrentLayerIndex()
    {
        if (DataManager.Instance == null || DataManager.Instance.MapRuntimeStore == null)
            return Mathf.Max(0, fallbackLayerIndex);

        MapRuntimeData runtime = DataManager.Instance.MapRuntimeStore.Get();
        if (runtime == null)
            return Mathf.Max(0, fallbackLayerIndex);

        GeneratedMapNodeData currentNode = MapRuntimeProgressUtility.FindCurrentNode(runtime);
        if (currentNode == null)
            return Mathf.Max(0, fallbackLayerIndex);

        return Mathf.Max(0, currentNode.LayerIndex);
    }

    private string ResolveCurrentMapId()
    {
        if (DataManager.Instance == null || DataManager.Instance.MapRuntimeStore == null)
            return string.Empty;

        MapRuntimeData runtime = DataManager.Instance.MapRuntimeStore.Get();
        GeneratedMapNodeData currentNode = MapRuntimeProgressUtility.FindCurrentNode(runtime);
        return currentNode != null ? currentNode.MapId : string.Empty;
    }

    private void SpawnPartyAllies(string mapId)
    {
        ResolveAllySpawnPoints();
        ClearAllSpawnPoints();

        bool useAllyRoot = !ShouldSkipAllyRoot(mapId);
        SetAllyRootActive(useAllyRoot);

        if (!useAllyRoot)
            return;

        if (DataManager.Instance == null)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        CharacterPrefabDatabase prefabDatabase = DataManager.Instance.CharacterPrefabDatabase;

        if (partyStore == null || prefabDatabase == null)
            return;

        int pointCount = allySpawnPoints != null ? allySpawnPoints.Length : 0;
        int partyCount = Mathf.Min(3, pointCount);

        for (int i = 0; i < partyCount; i++)
        {
            Transform point = allySpawnPoints[i];
            if (point == null)
                continue;

            string characterId = partyStore.GetCharacterId(i);
            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (!prefabDatabase.TryGetBattleEventWorldPrefab(characterId, out GameObject prefab) || prefab == null)
            {
                Debug.LogWarning($"[MapRoomController] Battle event world prefab not found: {characterId}", this);
                continue;
            }

            GameObject ally = Instantiate(prefab, point, false);
            ally.name = $"MapAlly_{i}_{characterId}";
            ally.transform.localPosition = Vector3.zero;
            ally.transform.localRotation = Quaternion.identity;
            ally.transform.localScale = Vector3.one * Mathf.Max(0f, allySpawnScale);

            if (ally.GetComponent<BattleMapSelectionCharacterMarker>() == null)
                ally.AddComponent<BattleMapSelectionCharacterMarker>();
        }
    }

    private bool ShouldSkipAllyRoot(string mapId)
    {
        if (string.IsNullOrWhiteSpace(mapId) || skipAllyRootMapIds == null)
            return false;

        for (int i = 0; i < skipAllyRootMapIds.Count; i++)
        {
            string skipMapId = skipAllyRootMapIds[i];

            if (!string.IsNullOrWhiteSpace(skipMapId) &&
                string.Equals(skipMapId.Trim(), mapId.Trim(), System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private void SetAllyRootActive(bool active)
    {
        if (allyRoot != null && allyRoot.gameObject.activeSelf != active)
            allyRoot.gameObject.SetActive(active);
    }

    private void ClearAllSpawnPoints()
    {
        if (allySpawnPoints == null)
            return;

        for (int i = 0; i < allySpawnPoints.Length; i++)
            ClearPoint(allySpawnPoints[i]);
    }

    private static void ClearPoint(Transform point)
    {
        if (point == null)
            return;

        for (int i = point.childCount - 1; i >= 0; i--)
        {
            GameObject child = point.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }

    private void ResolveReferences()
    {
        if (battleMapPanel == null)
        {
            battleMapPanel = Object.FindFirstObjectByType<BattleMapPanel>(FindObjectsInactive.Include);
        }

        if (stageBackgroundController == null)
        {
            stageBackgroundController = GetComponentInChildren<StageBackgroundController>(true);
        }

        if (allyRoot == null)
        {
            Transform found = FindChildRecursive(transform, "AllyRoot");
            if (found != null)
                allyRoot = found;
        }

        ResolveAllySpawnPoints();
    }

    private void ResolveAllySpawnPoints()
    {
        if (!autoFindAllySpawnPoints)
            return;

        if (allySpawnPoints == null || allySpawnPoints.Length != 3)
            allySpawnPoints = new Transform[3];

        Transform searchRoot = allyRoot != null ? allyRoot : transform;

        for (int i = 0; i < 3; i++)
        {
            if (allySpawnPoints[i] != null)
                continue;

            allySpawnPoints[i] = FindChildRecursive(searchRoot, $"EventAllyPoint{i}");
        }
    }

    private bool IsMapOpen()
    {
        return battleMapPanel != null && battleMapPanel.gameObject.activeInHierarchy;
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
