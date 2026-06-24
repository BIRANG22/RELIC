using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public static class DataColumnAliasUtility
    {
        public static void CopyAlias(
            Dictionary<string, string> row,
            string targetKey,
            params string[] aliases)
        {
            if (row == null || HasNonBlankKey(row, targetKey))
                return;

            for (int i = 0; i < aliases.Length; i++)
            {
                string alias = aliases[i];

                foreach (var pair in row)
                {
                    if (NormalizeKey(pair.Key) != NormalizeKey(alias))
                        continue;

                    if (string.IsNullOrWhiteSpace(pair.Value))
                        continue;

                    row[targetKey] = pair.Value;
                    return;
                }
            }
        }

        private static bool HasNonBlankKey(Dictionary<string, string> row, string key)
        {
            foreach (var pair in row)
            {
                if (NormalizeKey(pair.Key) == NormalizeKey(key) &&
                    !string.IsNullOrWhiteSpace(pair.Value))
                {
                    return true;
                }
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
