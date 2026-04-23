using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public static class FragmentCsvLoader
    {
        public static List<FragmentData> Load(Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(workbook, "FragmentData", "Fragment");
            return DataRowMapper.MapList<FragmentData>(rows);
        }
    }
}
