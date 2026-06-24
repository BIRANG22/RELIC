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
            ApplyColumnAliases(rows);

            var list = DataRowMapper.MapList<MonsterSkillData>(rows);

            return list
                .Where(x => !string.IsNullOrWhiteSpace(x.SkillId))
                .ToList();
        }

        private static void ApplyColumnAliases(IReadOnlyList<Dictionary<string, string>> rows)
        {
            if (rows == null)
                return;

            for (int i = 0; i < rows.Count; i++)
            {
                Dictionary<string, string> row = rows[i];

                if (row == null)
                    continue;

                DataColumnAliasUtility.CopyAlias(row, "ValueRandomRange", "ValueRandomRange", "\uC218\uCE58\uAC12\uBCC0\uC218");
                DataColumnAliasUtility.CopyAlias(row, "EffectDesc", "EffectDesc", "Effectdesc");
            }
        }
    }
}
