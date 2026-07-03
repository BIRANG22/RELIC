using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "GridEffectSpriteDatabase",
    menuName = "Relic/Data/Grid Effect Prefab Database"
)]
public class GridEffectSpriteDatabase : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string gridEffectId;
        public GameObject prefab;
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<string, GameObject> prefabMap;

    public void Initialize()
    {
        prefabMap = new Dictionary<string, GameObject>();

        foreach (Entry entry in entries)
        {
            if (entry == null)
                continue;

            string gridEffectId = entry.gridEffectId?.Trim();

            if (string.IsNullOrWhiteSpace(gridEffectId))
                continue;

            if (entry.prefab == null)
            {
                Debug.LogWarning($"[GridEffectSpriteDatabase] Prefab 없음: {gridEffectId}");
                continue;
            }

            if (prefabMap.ContainsKey(gridEffectId))
            {
                Debug.LogWarning($"[GridEffectSpriteDatabase] 중복 GridEffectID: {gridEffectId}");
                continue;
            }

            prefabMap.Add(gridEffectId, entry.prefab);
        }
    }

    public bool TryGetPrefab(string gridEffectId, out GameObject prefab)
    {
        prefab = null;

        if (string.IsNullOrWhiteSpace(gridEffectId))
            return false;

        if (prefabMap == null)
            Initialize();

        return prefabMap.TryGetValue(gridEffectId.Trim(), out prefab);
    }
}
