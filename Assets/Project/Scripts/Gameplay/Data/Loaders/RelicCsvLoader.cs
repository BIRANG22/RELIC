using System.Collections.Generic;
using System.Linq;

namespace Relic.Gameplay.Data
{
    public static class RelicCsvLoader
    {
        public static List<RelicData> Load(
            Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(workbook, "Relic");

            var list = DataRowMapper.MapList<RelicData>(rows);

            return list
                .Where(x => !string.IsNullOrWhiteSpace(x.FragmentId))
                .ToList();
        }
    }
}