using System.Collections.Generic;
using System.Linq;

namespace Relic.Gameplay.Data
{
    public class RewardTableDatabase
    {
        private readonly LookupDatabase<RewardTableData> tableDb = new();
        private readonly List<RewardTableEntryData> entries = new();

        public void Initialize(IEnumerable<RewardTableData> tables, IEnumerable<RewardTableEntryData> tableEntries)
        {
            tableDb.Initialize(tables, x => x.TableId);
            entries.Clear();
            entries.AddRange(tableEntries);
        }

        public RewardTableData GetTable(string tableId) => tableDb.Get(tableId);
        public List<RewardTableEntryData> GetEntries(string tableId) => entries.Where(x => x.TableId == tableId).ToList();
    }
}
