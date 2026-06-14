using System.Collections.Generic;
using System.Linq;

namespace Relic.Gameplay.Data
{
    public class RewardTableDatabase
    {
        private readonly List<RewardTableData> entries = new();

        public void Initialize(IEnumerable<RewardTableData> rewardEntries)
        {
            entries.Clear();

            if (rewardEntries != null)
                entries.AddRange(rewardEntries);
        }

        public List<RewardTableData> GetEntries(string dropTableId)
        {
            return entries
                .Where(x => x != null && x.DropTableId == dropTableId)
                .ToList();
        }
    }
}