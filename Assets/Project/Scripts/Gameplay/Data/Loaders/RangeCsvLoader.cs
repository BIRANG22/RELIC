using System.Collections.Generic;
using System.Linq;

namespace Relic.Gameplay.Data
{
    public static class RangeCsvLoader
    {
        public static List<SkillRangeData> Load(
            Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(
                workbook,
                "SkillRange",
                "Range",
                "RangeData"
            );

            var list = DataRowMapper.MapList<SkillRangeData>(rows);

            return list
                .Where(x => !string.IsNullOrWhiteSpace(x.RangeId))
                .ToList();
        }
    }
}