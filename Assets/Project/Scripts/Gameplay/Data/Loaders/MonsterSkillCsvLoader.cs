using System.Collections.Generic;
using System.Linq;

namespace Relic.Gameplay.Data
{
    public static class MonsterSkillCsvLoader
    {
        public static List<MonsterSkillData> Load(
            Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(
                workbook,
                "MonsterSkillData",
                "MonsterSkill",
                "MonsterSkills"
            );

            var list = DataRowMapper.MapList<MonsterSkillData>(rows);

            return list
                .Where(x => !string.IsNullOrWhiteSpace(x.SkillId))
                .ToList();
        }
    }
}