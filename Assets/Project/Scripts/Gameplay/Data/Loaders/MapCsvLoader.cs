using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public static class MapCsvLoader
    {
        public static List<MapData> Load(Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(workbook, "MapData", "Map");
            return DataRowMapper.MapList<MapData>(rows);
        }
    }
}
