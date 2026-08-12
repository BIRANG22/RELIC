using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [CreateAssetMenu(menuName = "Relic/Data/Map Visual Database")]
    public class MapVisualDatabase : ScriptableObject
    {
        [SerializeField] private List<MapVisualEntry> entries = new();

        private Dictionary<string, MapVisualEntry> map;

        public void Initialize()
        {
            map = new Dictionary<string, MapVisualEntry>();

            foreach (MapVisualEntry entry in entries)
            {
                if (entry == null)
                    continue;

                string mapId = NormalizeId(entry.MapId);
                if (string.IsNullOrWhiteSpace(mapId))
                    continue;

                if (map.ContainsKey(mapId))
                {
                    Debug.LogWarning($"[MapVisualDatabase] Duplicate MapId: {mapId}", this);
                    continue;
                }

                map.Add(mapId, entry);
            }
        }

        public bool TryGetEntry(string mapId, out MapVisualEntry entry)
        {
            entry = null;
            mapId = NormalizeId(mapId);

            if (string.IsNullOrWhiteSpace(mapId))
                return false;

            if (map == null)
                Initialize();

            return map.TryGetValue(mapId, out entry) && entry != null;
        }

        private static string NormalizeId(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
        }
    }

    [Serializable]
    public class MapVisualEntry
    {
        public string MapId;
        public List<MapVisualSpawnEntry> Spawns = new();
    }

    [Serializable]
    public class MapVisualSpawnEntry
    {
        public GameObject Prefab;
        public string VisualObjectId;
        public string AnchorName;
        public Vector3 LocalPosition;
        public Vector3 LocalEulerAngles;
        public Vector3 LocalScale = Vector3.one;
        public bool Active = true;
    }
}
