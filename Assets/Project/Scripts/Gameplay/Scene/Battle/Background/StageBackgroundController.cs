using System;
using System.Collections.Generic;
using UnityEngine;

public class StageBackgroundController : MonoBehaviour
{
    [Serializable]
    public sealed class BackgroundRange
    {
        [Min(1)] [SerializeField] private int minRow = 1;
        [Min(1)] [SerializeField] private int maxRow = 1;
        [SerializeField] private GameObject prefab;

        public int MinRow => minRow;
        public int MaxRow => maxRow;
        public GameObject Prefab => prefab;

        public BackgroundRange(int minRow, int maxRow, GameObject prefab)
        {
            this.minRow = minRow;
            this.maxRow = maxRow;
            this.prefab = prefab;
        }

        public bool Contains(int row)
        {
            return minRow <= maxRow && row >= minRow && row <= maxRow;
        }
    }

    [SerializeField] private Transform spawnRoot;
    [SerializeField] private List<BackgroundRange> backgroundRanges = new();

    private GameObject currentPrefab;
    private GameObject currentInstance;

    public void ShowForLayer(int layerIndex)
    {
        int row = layerIndex + 1;
        BackgroundRange range = FindRange(row);

        if (range == null || range.Prefab == null)
        {
            ClearCurrentBackground();
            Debug.LogWarning($"[StageBackgroundController] No background is configured for row {row}.", this);
            return;
        }

        if (currentPrefab == range.Prefab && currentInstance != null)
            return;

        ClearCurrentBackground();

        Transform parent = spawnRoot != null ? spawnRoot : transform;
        currentInstance = Instantiate(range.Prefab, parent, false);
        currentInstance.name = range.Prefab.name;
        currentPrefab = range.Prefab;
    }

    private BackgroundRange FindRange(int row)
    {
        if (backgroundRanges == null)
            return null;

        for (int i = 0; i < backgroundRanges.Count; i++)
        {
            BackgroundRange range = backgroundRanges[i];
            if (range != null && range.Contains(row))
                return range;
        }

        return null;
    }

    private void ClearCurrentBackground()
    {
        if (currentInstance != null)
        {
            if (Application.isPlaying)
                Destroy(currentInstance);
            else
                DestroyImmediate(currentInstance);
        }

        currentInstance = null;
        currentPrefab = null;
    }
}
