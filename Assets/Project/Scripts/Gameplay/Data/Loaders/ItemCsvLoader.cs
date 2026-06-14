using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public static class ItemCsvLoader
    {
        public static List<ItemData> Load(
            Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            return DataRowMapper.MapList<ItemData>(
                ExcelSheetSelector.GetSheet(workbook, "ItemData", "Item")
            );
        }
    }
}