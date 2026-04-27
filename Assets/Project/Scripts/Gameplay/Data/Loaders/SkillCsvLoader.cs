using System.Collections.Generic;
using System.Linq;

namespace Relic.Gameplay.Data
{
    public static class SkillCsvLoader
    {
        public static List<SkillMasterData> LoadSkills(
            Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(workbook, "Skill", "SkillMaster", "SkillMasterData");

            return DataRowMapper.MapList<SkillMasterData>(rows)
                .Where(x => !string.IsNullOrWhiteSpace(x.SkillId))
                .ToList();
        }

        public static List<SkillRangeData> LoadRanges(
            Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(workbook, "SkillRange", "Range", "SkillRangeData");

            return DataRowMapper.MapList<SkillRangeData>(rows)
                .Where(x => !string.IsNullOrWhiteSpace(x.RangeId))
                .ToList();
        }
    }
}