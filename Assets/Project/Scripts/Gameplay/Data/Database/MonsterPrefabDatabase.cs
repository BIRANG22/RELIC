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

        [Header("Monster Info Preview")]
        [Tooltip("MonsterInfoPanel의 Preview/image에서 순서대로 재생할 Idle 스프라이트입니다.")]
        public Sprite[] idlePreviewSprites = new Sprite[6];

        [Tooltip("MonsterInfoPanel Preview 이미지의 RectTransform 크기입니다.")]
        public Vector2 previewSize = new Vector2(300f, 300f);

        [Tooltip("Idle Preview 애니메이션의 초당 프레임 수입니다.")]
        [Min(0.01f)]
        public float previewFramesPerSecond = 6f;
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<string, GameObject> prefabMap;
    private Dictionary<string, Entry> entryMap;

    public void Initialize()
    {
        prefabMap = new Dictionary<string, GameObject>();
        entryMap = new Dictionary<string, Entry>();

        foreach (var entry in entries)
        {
            if (entry == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.monsterId))
                continue;

            string monsterId = entry.monsterId.Trim();

            if (entryMap.ContainsKey(monsterId))
            {
                Debug.LogWarning($"[MonsterPrefabDatabase] 중복 MonsterId: {monsterId}");
                continue;
            }

            entryMap.Add(monsterId, entry);

            if (entry.prefab == null)
            {
                Debug.LogWarning($"[MonsterPrefabDatabase] Prefab 없음: {monsterId}");
                continue;
            }

            prefabMap.Add(monsterId, entry.prefab);
        }
    }

    public bool TryGetPrefab(string monsterId, out GameObject prefab)
    {
        if (prefabMap == null)
            Initialize();

        if (string.IsNullOrWhiteSpace(monsterId))
        {
            prefab = null;
            return false;
        }

        return prefabMap.TryGetValue(monsterId.Trim(), out prefab);
    }

    public bool TryGetEntry(string monsterId, out Entry entry)
    {
        if (entryMap == null)
            Initialize();

        if (string.IsNullOrWhiteSpace(monsterId))
        {
            entry = null;
            return false;
        }

        return entryMap.TryGetValue(monsterId.Trim(), out entry);
    }
}
