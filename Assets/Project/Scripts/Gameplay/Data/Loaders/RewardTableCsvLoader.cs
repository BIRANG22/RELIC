using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public static class RewardTableCsvLoader
    {
        public static List<RewardTableData> Load(
            Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            return DataRowMapper.MapList<RewardTableData>(
                ExcelSheetSelector.GetSheet(workbook, "RewardTableData", "RewardTable")
            );
        }
    }
}