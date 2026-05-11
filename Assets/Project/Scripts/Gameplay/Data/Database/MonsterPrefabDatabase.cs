using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "MonsterPrefabDatabase",
    menuName = "Relic/Data/Monster Prefab Database"
)]
public class MonsterPrefabDatabase : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string monsterId;
        public GameObject prefab;
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<string, GameObject> prefabMap;

    public void Initialize()
    {
        prefabMap = new Dictionary<string, GameObject>();

        foreach (var entry in entries)
        {
            if (entry == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.monsterId))
                continue;

            if (entry.prefab == null)
            {
                Debug.LogWarning($"[MonsterPrefabDatabase] Prefab 없음: {entry.monsterId}");
                continue;
            }

            if (prefabMap.ContainsKey(entry.monsterId))
            {
                Debug.LogWarning($"[MonsterPrefabDatabase] 중복 MonsterId: {entry.monsterId}");
                continue;
            }

            prefabMap.Add(entry.monsterId, entry.prefab);
        }
    }

    public bool TryGetPrefab(string monsterId, out GameObject prefab)
    {
        if (prefabMap == null)
            Initialize();

        return prefabMap.TryGetValue(monsterId, out prefab);
    }
}