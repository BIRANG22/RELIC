using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class RelicDatabase
    {
        private readonly List<RelicData> relics = new();
        private readonly Dictionary<string, RelicData> byId = new();

        public void Initialize(IEnumerable<RelicData> list)
        {
            relics.Clear();
            byId.Clear();

            if (list == null)
                return;

            foreach (var data in list)
            {
                if (data == null || string.IsNullOrWhiteSpace(data.FragmentId))
                    continue;

                string id = data.FragmentId.Trim();

                relics.Add(data);
                byId[id] = data;
            }
        }

        public bool TryGet(string relicId, out RelicData data)
        {
            data = null;

            if (string.IsNullOrWhiteSpace(relicId))
                return false;

            return byId.TryGetValue(relicId.Trim(), out data);
        }

        public RelicData Get(string relicId)
        {
            if (TryGet(relicId, out RelicData data))
                return data;

            Debug.LogWarning($"[RelicDatabase] Relic ¾øÀ½: {relicId}");
            return null;
        }

        public IReadOnlyList<RelicData> GetAll()
        {
            return relics;
        }
    }
}