using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public static class QuestCsvLoader
    {
        public static List<QuestData> Load(Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(workbook, "QuestData", "Quest");
            return DataRowMapper.MapList<QuestData>(rows);
        }
    }
}
