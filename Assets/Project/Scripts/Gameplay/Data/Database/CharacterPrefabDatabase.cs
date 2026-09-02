using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [CreateAssetMenu(menuName = "Relic/Data/Character Prefab Database")]
    public class CharacterPrefabDatabase : ScriptableObject
    {
        [SerializeField] private List<CharacterPrefabEntry> entries = new();

        private Dictionary<string, CharacterPrefabEntry> map;

        public void Initialize()
        {
            map = new Dictionary<string, CharacterPrefabEntry>();

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.CharacterId))
                    continue;

                map[entry.CharacterId] = entry;
            }
        }

        public bool TryGetBattlePrefab(string characterId, out GameObject prefab)
        {
            prefab = null;

            if (map == null)
                Initialize();

            if (!map.TryGetValue(characterId, out var entry))
                return false;

            prefab = entry.BattlePrefab;
            return prefab != null;
        }

        public bool TryGetLobbyPrefab(string characterId, out GameObject prefab)
        {
            prefab = null;

            if (map == null)
                Initialize();

            if (!map.TryGetValue(characterId, out var entry))
                return false;

            prefab = entry.LobbyPrefab;
            return prefab != null;
        }

        public bool TryGetPreviewUIPrefab(string characterId, out GameObject prefab)
        {
            prefab = null;

            if (map == null)
                Initialize();

            if (!map.TryGetValue(characterId, out var entry))
                return false;

            prefab = entry.PreviewUIPrefab;
            return prefab != null;
        }

        public bool TryGetPreviewWorldPrefab(string characterId, out GameObject prefab)
        {
            prefab = null;

            if (map == null)
                Initialize();

            if (!map.TryGetValue(characterId, out var entry))
                return false;

            prefab = entry.PreviewWorldPrefab;
            return prefab != null;
        }

        public bool TryGetRestPrefab(string characterId, out GameObject prefab)
        {
            prefab = null;

            if (map == null)
                Initialize();

            if (!map.TryGetValue(characterId, out var entry))
                return false;

            prefab = entry.RestPrefab;
            return prefab != null;
        }

        public bool TryGetBattleEventWorldPrefab(string characterId, out GameObject prefab)
        {
            prefab = null;

            if (map == null)
                Initialize();

            if (!map.TryGetValue(characterId, out var entry))
                return false;

            prefab = entry.BattleEventWorldPrefab != null
                ? entry.BattleEventWorldPrefab
                : entry.PreviewWorldPrefab;

            return prefab != null;
        }

        // 기존 코드 호환용
        public bool TryGetPrefab(string characterId, out GameObject prefab)
        {
            return TryGetBattlePrefab(characterId, out prefab);
        }
    }

    [Serializable]
    public class CharacterPrefabEntry
    {
        public string CharacterId;

        [Header("Battle")]
        public GameObject BattlePrefab;

        [Header("Lobby")]
        public GameObject LobbyPrefab;

        [Header("Preview UI")]
        public GameObject PreviewUIPrefab;

        [Header("Preview World")]
        public GameObject PreviewWorldPrefab;

        [Header("Rest")]
        public GameObject RestPrefab;

        [Header("Battle Event World")]
        public GameObject BattleEventWorldPrefab;
    }
}