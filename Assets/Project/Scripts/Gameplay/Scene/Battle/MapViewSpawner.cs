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

    public void Spawn(
        List<GeneratedMapNodeData> nodes,
        Action<GeneratedMapNodeData> onNodeClicked)
    {
        lastNodes = nodes;
        lastOnNodeClicked = onNodeClicked;

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

        MapRuntimeData runtime = DataManager.Instance.MapRuntimeStore.Get();
        MapNodeIconDatabase iconDatabase = DataManager.Instance.MapNodeIconDatabase;

        for (int i = 0; i < nodes.Count; i++)
        {
            GeneratedMapNodeData data = nodes[i];

            MapNodeView node = Instantiate(nodePrefab, nodeRoot);

            RectTransform rect = node.GetComponent<RectTransform>();

            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = data.Position;
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.identity;
            }

            bool canClick = IsNodeClickable(data, nodes, runtime);

            node.Setup(data, iconDatabase, onNodeClicked, canClick);

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

    private bool IsNodeClickable(
    GeneratedMapNodeData node,
    List<GeneratedMapNodeData> nodes,
    MapRuntimeData runtime)
    {
        if (node == null || runtime == null)
            return false;

        if (runtime.CurrentNodeIndex < 0)
            return node.Type == "Start";

        GeneratedMapNodeData currentNode = GetNodeData(
            nodes,
            runtime.CurrentNodeIndex
        );

        if (currentNode == null)
        {
            Debug.LogWarning(
                $"[MapViewSpawner] CurrentNode not found / CurrentNodeIndex:{runtime.CurrentNodeIndex}"
            );
            return false;
        }

        bool canClick = currentNode.NextNodeIndices.Contains(node.NodeIndex);

        Debug.Log(
            $"[MapViewSpawner] ClickCheck / Current:{currentNode.NodeIndex} -> Target:{node.NodeIndex} / Can:{canClick}"
        );

        return canClick;
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

    public void Refresh()
    {
        Spawn(lastNodes, lastOnNodeClicked);
    }
}