using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class RewardManager
    {
        private readonly Random random = new();

        public List<RewardTableEntryData> RollRewards(RewardTableData table, List<RewardTableEntryData> entries)
        {
            var results = new List<RewardTableEntryData>();
            foreach (var entry in entries)
            {
                if (entry.IsGuaranteedDrop || random.NextDouble() <= entry.Probability)
                    results.Add(entry);
            }
            return results;
        }
    }
}
