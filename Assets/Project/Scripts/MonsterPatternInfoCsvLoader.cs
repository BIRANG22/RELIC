using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public static class MonsterPatternInfoCsvLoader
    {
        public static List<MonsterPatternInfoData> Load(Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(workbook, "MonsterPatternInfo", "MonsterPatternInfoData", "MonsterPattern", "MonsterPatternData");
            ApplyColumnAliases(rows);
            return DataRowMapper.MapList<MonsterPatternInfoData>(rows);
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

                DataColumnAliasUtility.CopyAlias(row, "PatternId", "PatternId", "PatternID", "Id", "ID", "패턴ID");
                DataColumnAliasUtility.CopyAlias(row, "MonsterId", "MonsterId", "MonsterID", "몬스터ID");
                DataColumnAliasUtility.CopyAlias(row, "Order", "Order", "SortOrder", "순서");
                DataColumnAliasUtility.CopyAlias(row, "SkillId", "SkillId", "SkillID", "MonsterSkillId", "MonsterSkillID", "스킬ID", "몬스터스킬ID", "스킬Id");
                DataColumnAliasUtility.CopyAlias(row, "Description", "Description", "Desc", "PatternDescription", "내용");
            }
        }
    }
}
