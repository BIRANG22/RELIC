using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public static class MonsterCsvLoader
    {
        public static List<MonsterMasterData> Load(Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(workbook, "MonsterMasterData", "MonsterMaster", "Monster");
            ApplyRewardColumnAliases(rows);
            return DataRowMapper.MapList<MonsterMasterData>(rows);
        }

        private static void ApplyRewardColumnAliases(IReadOnlyList<Dictionary<string, string>> rows)
        {
            if (rows == null)
                return;

            for (int i = 0; i < rows.Count; i++)
            {
                Dictionary<string, string> row = rows[i];

                if (row == null)
                    continue;

                CopyAlias(row, "MinRemnant", "MinRemnant", "RemnantMin", "MinimumRemnant", "최소렘넌트", "렘넌트최소");
                CopyAlias(row, "MaxRemnant", "MaxRemnant", "RemnantMax", "MaximumRemnant", "최대렘넌트", "렘넌트최대");
                CopyAlias(row, "UniqueItemId", "UniqueItemId", "UniqueItemID", "UniqueItem", "FixedItemId", "FixedItem", "고유아이템", "고유아이템ID", "고유아이템Id");
                CopyAlias(row, "UniqueItemChance", "UniqueItemChance", "UniqueItemRate", "UniqueItemProbability", "ItemChance", "ItemRate", "고유아이템확률", "아이템확률");
                CopyAlias(row, "RelicChance", "RelicChance", "RelicRate", "RelicProbability", "유물확률");
            }
        }

        private static void CopyAlias(Dictionary<string, string> row, string targetKey, params string[] aliases)
        {
            if (HasKey(row, targetKey))
                return;

            for (int i = 0; i < aliases.Length; i++)
            {
                string alias = aliases[i];

                foreach (var pair in row)
                {
                    if (NormalizeKey(pair.Key) != NormalizeKey(alias))
                        continue;

                    row[targetKey] = pair.Value;
                    return;
                }
            }
        }

        private static bool HasKey(Dictionary<string, string> row, string key)
        {
            foreach (var pair in row)
            {
                if (NormalizeKey(pair.Key) == NormalizeKey(key))
                    return true;
            }

            return false;
        }

        private static string NormalizeKey(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Replace("\uFEFF", "")
                .Replace("_", "")
                .Replace(" ", "")
                .Trim()
                .ToLowerInvariant();
        }
    }
}
