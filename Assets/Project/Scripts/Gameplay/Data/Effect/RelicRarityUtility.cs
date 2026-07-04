using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public enum RelicRarity
    {
        None = 0,
        Common = 1,
        Uncommon = 2,
        Rare = 3,
        Unique = 4
    }

    public static class RelicRarityUtility
    {
        private static readonly Dictionary<string, RelicRarity> RarityMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["common"] = RelicRarity.Common,
            ["커먼"] = RelicRarity.Common,
            ["uncommon"] = RelicRarity.Uncommon,
            ["언커먼"] = RelicRarity.Uncommon,
            ["rare"] = RelicRarity.Rare,
            ["레어"] = RelicRarity.Rare,
            ["unique"] = RelicRarity.Unique,
            ["유니크"] = RelicRarity.Unique
        };

        public static bool TryParseChestRarity(string raw, out RelicRarity rarity)
        {
            rarity = RelicRarity.None;

            string normalized = Normalize(raw);
            if (string.IsNullOrWhiteSpace(normalized))
                return false;

            return RarityMap.TryGetValue(normalized, out rarity);
        }

        public static int GetRevealRank(RelicRarity rarity)
        {
            return rarity is >= RelicRarity.Common and <= RelicRarity.Unique
                ? (int)rarity
                : 0;
        }

        public static bool IsChestRarity(RelicRarity rarity)
        {
            return GetRevealRank(rarity) > 0;
        }

        private static string Normalize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            return raw
                .Replace("\uFEFF", "")
                .Replace("_", "")
                .Replace("-", "")
                .Replace(" ", "")
                .Trim()
                .ToLowerInvariant();
        }
    }
}
