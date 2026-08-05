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

    private readonly Dictionary<int, MapNodeView> spawnedNodes = new();

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
                rect.anchoredPosition = data.Position;
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
            }

            node.Setup(data, iconDatabase, null, false);

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

                CreateLine(from.Position, to.Position);
            }
        }
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

    private void CreateLine(Vector2 from, Vector2 to)
    {
        MapLineView line = Instantiate(linePrefab, lineRoot);
        line.Setup(from, to);
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
