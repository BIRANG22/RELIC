using System.Collections.Generic;
using System.Linq;

namespace Relic.Gameplay.Data
{
    public static class RuneCsvLoader
    {
        public static List<RuneData> Load(
            Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(
                workbook,
                "RuneData",
                "Rune",
                "Runes"
            );

            var list = DataRowMapper.MapList<RuneData>(rows);

            return list
                .Where(x => !string.IsNullOrWhiteSpace(x.RuneId))
                .ToList();
        }
    }
}