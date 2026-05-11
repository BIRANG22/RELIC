using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [CreateAssetMenu(menuName = "Relic/Data/Character Icon Database")]
    public class CharacterIconDatabase : ScriptableObject
    {
        [SerializeField] private List<CharacterIconEntry> entries = new();

        private Dictionary<string, Sprite> map;

        public void Initialize()
        {
            map = new Dictionary<string, Sprite>();

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.CharacterId) || entry.Icon == null)
                    continue;

                map[entry.CharacterId] = entry.Icon;
            }
        }

        public bool TryGetIcon(string characterId, out Sprite icon)
        {
            if (map == null)
                Initialize();

            return map.TryGetValue(characterId, out icon);
        }
    }

    [Serializable]
    public class CharacterIconEntry
    {
        public string CharacterId;
        public Sprite Icon;
    }
}