using System.Collections.Generic;
using System.Linq;

namespace Relic.Gameplay.Data
{
    public static class SkillEnhanceCsvLoader
    {
        public static List<SkillEnhanceData> Load(
            Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(
                workbook,
                "SkillEnhanceData",
                "SkillEnhance",
                "SkillUpgrade",
                "SkillUpgradeData"
            );

            var list = DataRowMapper.MapList<SkillEnhanceData>(rows);

            return list
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .ToList();
        }
    }
}