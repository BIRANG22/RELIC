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

            List<SkillMasterData> skills = DataRowMapper.MapList<SkillMasterData>(rows);
            for (int i = 0; i < skills.Count && i < rows.Count; i++)
                skills[i].ResourceCostFormula = GetResourceCostFormula(rows[i]);

            return skills.Where(x => !string.IsNullOrWhiteSpace(x.SkillId)).ToList();
        }

        public static List<SkillRangeData> LoadRanges(
            Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(workbook, "SkillRange", "Range", "SkillRangeData");

            return DataRowMapper.MapList<SkillRangeData>(rows)
                .Where(x => !string.IsNullOrWhiteSpace(x.RangeId))
                .ToList();
        }

        private static string GetResourceCostFormula(Dictionary<string, string> row)
        {
            if (row == null)
                return string.Empty;

            foreach (KeyValuePair<string, string> pair in row)
            {
                string key = pair.Key?.Replace("\uFEFF", "").Replace("_", "").Replace(" ", "");
                if (string.Equals(key, "ResourceCostValue", System.StringComparison.OrdinalIgnoreCase))
                    return pair.Value?.Trim() ?? string.Empty;
            }

            return string.Empty;
        }
    }
}
