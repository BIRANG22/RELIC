using System;
using System.Collections.Generic;
using UnityEngine;
using Relic.Gameplay.Data;

public class MapViewSpawner : MonoBehaviour
{
    [SerializeField] private MapNodeView nodePrefab;
    [SerializeField] private MapLineView linePrefab;

    [SerializeField] private Transform nodeRoot;
    [SerializeField] private Transform lineRoot;

    private readonly Dictionary<int, MapNodeView> spawnedNodes = new();

    public void Spawn(
        List<GeneratedMapNodeData> nodes,
        Action<GeneratedMapNodeData> onNodeClicked)
    {
        Debug.Log($"[MapViewSpawner] Spawn »£√‚µ  / Count: {nodes?.Count}");

        Clear();

        MapNodeIconDatabase iconDatabase = DataManager.Instance.MapNodeIconDatabase;

        for (int i = 0; i < nodes.Count; i++)
        {
            GeneratedMapNodeData data = nodes[i];

            MapNodeView node = Instantiate(nodePrefab, nodeRoot);

            RectTransform rect = node.GetComponent<RectTransform>();
            rect.anchoredPosition = data.Position;

            node.Setup(data, iconDatabase, onNodeClicked);

            spawnedNodes.Add(data.NodeIndex, node);
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            GeneratedMapNodeData from = nodes[i];

            for (int j = 0; j < from.NextNodeIndices.Count; j++)
            {
                int toIndex = from.NextNodeIndices[j];

                if (!spawnedNodes.ContainsKey(toIndex))
                    continue;

                CreateLine(
                    from.Position,
                    GetNodeData(nodes, toIndex).Position
                );
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
}