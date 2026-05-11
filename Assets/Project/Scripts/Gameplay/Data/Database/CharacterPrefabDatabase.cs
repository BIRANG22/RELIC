using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [CreateAssetMenu(menuName = "Relic/Data/Character Prefab Database")]
    public class CharacterPrefabDatabase : ScriptableObject
    {
        [SerializeField] private List<CharacterPrefabEntry> entries = new();

        private Dictionary<string, GameObject> map;

        public void Initialize()
        {
            map = new Dictionary<string, GameObject>();

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.CharacterId) || entry.BattlePrefab == null)
                    continue;

                map[entry.CharacterId] = entry.BattlePrefab;
            }
        }

        public bool TryGetPrefab(string characterId, out GameObject prefab)
        {
            if (map == null)
                Initialize();

            return map.TryGetValue(characterId, out prefab);
        }
    }

    [Serializable]
    public class CharacterPrefabEntry
    {
        public string CharacterId;
        public GameObject BattlePrefab;
    }
}