using System.Collections.Generic;
namespace Relic.Gameplay.Data
{
    public static class TutorialCsvLoader
    {
        public static List<TutorialData> Load(Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(workbook, "Tutorial");
            return DataRowMapper.MapList<TutorialData>(rows);
        }
    }
}
