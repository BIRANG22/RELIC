using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    /// <summary>
    /// Erosion 난이도 데이터를 DifficultyId와 UI SlotName으로 조회합니다.
    /// </summary>
    public class ErosionDatabase
    {
        private readonly LookupDatabase<ErosionData> byDifficultyId = new();
        private readonly Dictionary<string, ErosionData> bySlotName =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly List<ErosionData> all = new();

        public void Initialize(IEnumerable<ErosionData> entries)
        {
            all.Clear();
            bySlotName.Clear();

            if (entries != null)
            {
                foreach (ErosionData entry in entries)
                {
                    if (entry == null)
                        continue;

                    all.Add(entry);

                    if (!string.IsNullOrWhiteSpace(entry.SlotName))
                        bySlotName[entry.SlotName.Trim()] = entry;
                }
            }

            byDifficultyId.Initialize(all, x => x.DifficultyId);
        }

        public ErosionData Get(string difficultyId)
        {
            return byDifficultyId.Get(difficultyId);
        }

        public bool TryGet(string difficultyId, out ErosionData data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(difficultyId))
                return false;

            return byDifficultyId.TryGet(difficultyId.Trim(), out data) && data != null;
        }

        public bool TryGetBySlotName(string slotName, out ErosionData data)
        {
            data = null;
            return !string.IsNullOrWhiteSpace(slotName) &&
                   bySlotName.TryGetValue(slotName.Trim(), out data) &&
                   data != null;
        }

        public IReadOnlyList<ErosionData> GetAll()
        {
            return all;
        }
    }
}
