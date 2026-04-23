using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public static class AssetCsvLoader
    {
        public static List<AssetData> Load(Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(workbook, "AssetData", "Asset");
            return DataRowMapper.MapList<AssetData>(rows);
        }
    }
}
