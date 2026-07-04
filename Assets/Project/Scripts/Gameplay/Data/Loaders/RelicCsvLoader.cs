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
            ApplyRarityAliases(rows, list);

            return list
                .Where(x => !string.IsNullOrWhiteSpace(x.FragmentId))
                .ToList();
        }

        private static void ApplyRarityAliases(
            IReadOnlyList<Dictionary<string, string>> rows,
            List<RelicData> list)
        {
            if (rows == null || list == null)
                return;

            int count = System.Math.Min(rows.Count, list.Count);
            for (int i = 0; i < count; i++)
            {
                RelicData data = list[i];
                if (data == null || !string.IsNullOrWhiteSpace(data.Rarity))
                    continue;

                if (TryGetCell(rows[i], out string rarity, "Rarity", "레어도", "등급", "Rare", "Grade"))
                    data.Rarity = rarity;
            }
        }

        private static bool TryGetCell(
            Dictionary<string, string> row,
            out string value,
            params string[] candidates)
        {
            value = null;

            if (row == null || candidates == null)
                return false;

            for (int i = 0; i < candidates.Length; i++)
            {
                string candidate = NormalizeKey(candidates[i]);
                foreach (KeyValuePair<string, string> pair in row)
                {
                    if (NormalizeKey(pair.Key) != candidate)
                        continue;

                    value = pair.Value?.Trim();
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
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
