using System;
using System.Collections.Generic;
using UnityEngine;
using Relic.Gameplay.Data;

public class MapViewSpawner : MonoBehaviour
{
    [SerializeField] private MapNodeView nodePrefab;
    [SerializeField] private MapLineView linePrefab;

    [SerializeField] private RectTransform nodeRoot;
    [SerializeField] private RectTransform lineRoot;

    [Header("Node Layout")]
    [SerializeField, Min(0f)] private float nodeGapX = 200f;
    [SerializeField, Min(0f)] private float nodeGapY = 80f;

    [Header("Map Layout")]
    [SerializeField] private RectTransform mapRect;
    [SerializeField, Min(0f)] private float mapWidth = 2730f;

    private readonly Dictionary<int, MapNodeView> spawnedNodes = new();

    private bool hasCapturedMapBasePosition;
    private Vector2 mapBaseAnchoredPosition;

    private List<GeneratedMapNodeData> lastNodes;
    private Action<GeneratedMapNodeData> lastOnNodeClicked;
    private Action<GeneratedMapNodeData, Sprite> lastOnNodeHovered;
    private Action lastOnNodeHoverExited;

    public void Spawn(
        List<GeneratedMapNodeData> nodes,
        Action<GeneratedMapNodeData> onNodeClicked)
    {
        Spawn(nodes, onNodeClicked, null, null);
    }

    public void Spawn(List<GeneratedMapNodeData> nodes,
        Action<GeneratedMapNodeData> onNodeClicked,
        Action<GeneratedMapNodeData, Sprite> onNodeHovered,
        Action onNodeHoverExited)
    {
        lastNodes = nodes;
        lastOnNodeClicked = onNodeClicked;
        lastOnNodeHovered = onNodeHovered;
        lastOnNodeHoverExited = onNodeHoverExited;

        ApplyMapWidth();

        MapRuntimeData runtime = DataManager.Instance?.MapRuntimeStore?.Get();
        ApplyMapProgressOffset(nodes, runtime);

        // 방 클리어 후 맵으로 돌아왔을 때 이전 카테고리 버튼 색상이 남지 않도록 초기화합니다.
        MapCategoryHighlightController categoryHighlightController =
            FindFirstObjectByType<MapCategoryHighlightController>();

        if (categoryHighlightController != null)
            categoryHighlightController.ResetHighlightForMapRefresh();

        Clear();

        if (nodes == null || nodes.Count == 0)
            return;

        if (nodePrefab == null || linePrefab == null)
        {
            Debug.LogWarning("[MapViewSpawner] NodePrefab 또는 LinePrefab이 연결되지 않았습니다.");
            return;
        }

        if (nodeRoot == null || lineRoot == null)
        {
            Debug.LogWarning("[MapViewSpawner] NodeRoot 또는 LineRoot가 연결되지 않았습니다.");
            return;
        }

        EnsureLineRootBehindNodeRoot();

        MapNodeIconDatabase iconDatabase = DataManager.Instance.MapNodeIconDatabase;

        for (int i = 0; i < nodes.Count; i++)
        {
            GeneratedMapNodeData data = nodes[i];

            MapNodeView node = Instantiate(nodePrefab, nodeRoot);

            RectTransform rect = node.GetComponent<RectTransform>();

            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = GetDisplayPosition(data.Position);
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
            }

            node.Setup(data, iconDatabase, null, false);
            node.SetProgressVisual(
                IsCurrentlyAvailable(runtime, data),
                IsVisitedOrCleared(runtime, data));

            Sprite nodeIcon = null;
            iconDatabase?.TryGetIcon(data.Type, out nodeIcon);
            MapNodeHoverRelay hoverRelay = node.GetComponentInChildren<MapNodeHoverRelay>(true);
            hoverRelay?.Configure(data, nodeIcon, onNodeHovered, onNodeHoverExited);

            spawnedNodes[data.NodeIndex] = node;
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            GeneratedMapNodeData from = nodes[i];

            for (int j = 0; j < from.NextNodeIndices.Count; j++)
            {
                int toIndex = from.NextNodeIndices[j];

                GeneratedMapNodeData to = GetNodeData(nodes, toIndex);

                if (to == null)
                    continue;

                CreateLine(
                    GetDisplayPosition(from.Position),
                    GetDisplayPosition(to.Position),
                    IsCurrentOutgoingLineAvailable(runtime, from, to),
                    IsTraversedLine(runtime, from, to));
            }
        }
    }


    private void ApplyMapWidth()
    {
        if (mapRect == null)
            return;

        mapRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, mapWidth);
    }

    private void ApplyMapProgressOffset(List<GeneratedMapNodeData> nodes, MapRuntimeData runtime)
    {
        if (mapRect == null || nodes == null || nodes.Count == 0)
            return;

        if (!hasCapturedMapBasePosition)
        {
            mapBaseAnchoredPosition = mapRect.anchoredPosition;
            hasCapturedMapBasePosition = true;
        }

        float targetX = mapBaseAnchoredPosition.x;

        GeneratedMapNodeData currentNode = GetNodeData(nodes, runtime != null ? runtime.CurrentNodeIndex : -1);
        if (currentNode != null && IsCleared(runtime, currentNode))
        {
            float sourceGapX = Mathf.Max(0.0001f, BattleMapLayoutUtility.LayerGap);
            float clearedLayer = Mathf.Max(0f, currentNode.Position.x / sourceGapX);
            float shiftLayer = Mathf.Max(0f, clearedLayer - 1f);
            targetX -= shiftLayer * nodeGapX;
        }

        Vector2 position = mapRect.anchoredPosition;
        position.x = targetX;
        mapRect.anchoredPosition = position;
    }


    private void EnsureLineRootBehindNodeRoot()
    {
        if (lineRoot == null || nodeRoot == null)
            return;

        // 같은 부모 아래에 있다면 LineRoot가 NodeRoot보다 먼저 그려지도록 배치합니다.
        // Unity UI는 같은 Canvas 안에서 뒤쪽 sibling이 위에 그려지므로,
        // 라인이 항상 노드 이미지 뒤에 가려지게 됩니다.
        if (lineRoot.parent == nodeRoot.parent && lineRoot.GetSiblingIndex() > nodeRoot.GetSiblingIndex())
            lineRoot.SetSiblingIndex(nodeRoot.GetSiblingIndex());
    }

    private Vector2 GetDisplayPosition(Vector2 sourcePosition)
    {
        float sourceGapX = Mathf.Max(0.0001f, BattleMapLayoutUtility.LayerGap);
        float sourceGapY = Mathf.Max(0.0001f, BattleMapLayoutUtility.RowGap);

        return new Vector2(
            sourcePosition.x / sourceGapX * nodeGapX,
            sourcePosition.y / sourceGapY * nodeGapY
        );
    }

    private GeneratedMapNodeData GetNodeData(List<GeneratedMapNodeData> nodes, int nodeIndex)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].NodeIndex == nodeIndex)
                return nodes[i];
        }

        return null;
    }

    private static bool IsCurrentlyAvailable(MapRuntimeData runtime, GeneratedMapNodeData node)
    {
        if (runtime == null || node == null)
            return false;

        return MapRuntimeProgressUtility.IsNodeClickableFromCurrentProgress(runtime, node);
    }


    private static bool IsCurrentOutgoingLineAvailable(
        MapRuntimeData runtime,
        GeneratedMapNodeData from,
        GeneratedMapNodeData to)
    {
        if (runtime == null || from == null || to == null)
            return false;

        return from.NodeIndex == runtime.CurrentNodeIndex &&
               IsCurrentlyAvailable(runtime, to);
    }

    private static bool IsCleared(MapRuntimeData runtime, GeneratedMapNodeData node)
    {
        if (runtime == null || node == null)
            return false;

        string nodeKey = node.NodeIndex.ToString();
        return runtime.ClearedMapIds != null && runtime.ClearedMapIds.Contains(nodeKey);
    }

    private static bool IsVisitedOrCleared(MapRuntimeData runtime, GeneratedMapNodeData node)
    {
        if (runtime == null || node == null)
            return false;

        string nodeKey = node.NodeIndex.ToString();

        bool visited = runtime.VisitedMapIds != null && runtime.VisitedMapIds.Contains(nodeKey);
        bool cleared = runtime.ClearedMapIds != null && runtime.ClearedMapIds.Contains(nodeKey);
        return visited || cleared;
    }

    private static bool IsTraversedLine(
        MapRuntimeData runtime,
        GeneratedMapNodeData from,
        GeneratedMapNodeData to)
    {
        return IsVisitedOrCleared(runtime, from) && IsVisitedOrCleared(runtime, to);
    }

    private void CreateLine(Vector2 from, Vector2 to, bool available, bool traversed)
    {
        MapLineView line = Instantiate(linePrefab, lineRoot);
        line.Setup(from, to);
        line.SetProgressVisual(available, traversed);
    }

    private void Clear()
    {
        spawnedNodes.Clear();

        if (nodeRoot != null)
        {
            for (int i = nodeRoot.childCount - 1; i >= 0; i--)
                Destroy(nodeRoot.GetChild(i).gameObject);
        }

        if (lineRoot != null)
        {
            for (int i = lineRoot.childCount - 1; i >= 0; i--)
                Destroy(lineRoot.GetChild(i).gameObject);
        }
    }

    public void HighlightCategory(string nodeType)
    {
        if (lastNodes == null)
            return;

        foreach (KeyValuePair<int, MapNodeView> pair in spawnedNodes)
        {
            MapNodeView nodeView = pair.Value;

            if (nodeView == null)
                continue;

            GeneratedMapNodeData data = GetNodeData(lastNodes, pair.Key);
            bool isMatch = data != null &&
                           string.Equals(data.Type, nodeType, StringComparison.OrdinalIgnoreCase);

            nodeView.SetCategoryHighlighted(isMatch);
        }
    }

    public void ClearCategoryHighlight()
    {
        foreach (MapNodeView nodeView in spawnedNodes.Values)
        {
            if (nodeView != null)
                nodeView.SetCategoryHighlighted(false);
        }
    }

    public void Refresh()
    {
        Spawn(lastNodes, lastOnNodeClicked, lastOnNodeHovered, lastOnNodeHoverExited);
    }
}
