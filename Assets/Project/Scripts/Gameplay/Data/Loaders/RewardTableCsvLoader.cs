using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public static class RewardTableCsvLoader
    {
        public static List<RewardTableData> LoadTables(Dictionary<string, List<Dictionary<string, string>>> workbook)
            => DataRowMapper.MapList<RewardTableData>(ExcelSheetSelector.GetSheet(workbook, "RewardTableData", "RewardTable"));

        public static List<RewardTableEntryData> LoadEntries(Dictionary<string, List<Dictionary<string, string>>> workbook)
            => DataRowMapper.MapList<RewardTableEntryData>(ExcelSheetSelector.GetSheet(workbook, "RewardTableEntryData", "RewardTableEntry"));
    }
}
