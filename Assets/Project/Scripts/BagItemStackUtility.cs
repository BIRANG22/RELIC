using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public readonly struct BagItemStack
    {
        public BagItemStack(string itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }

        public string ItemId { get; }
        public int Count { get; }
    }

    public static class BagItemStackUtility
    {
        public static List<BagItemStack> BuildStacks(IReadOnlyList<string> itemIds)
        {
            List<BagItemStack> result = new List<BagItemStack>();

            if (itemIds == null || itemIds.Count == 0)
                return result;

            Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.Ordinal);
            List<string> order = new List<string>();

            for (int i = 0; i < itemIds.Count; i++)
            {
                string rawId = itemIds[i];

                if (string.IsNullOrWhiteSpace(rawId))
                    continue;

                string itemId = rawId.Trim();

                if (counts.TryGetValue(itemId, out int currentCount))
                {
                    counts[itemId] = currentCount + 1;
                    continue;
                }

                counts[itemId] = 1;
                order.Add(itemId);
            }

            for (int i = 0; i < order.Count; i++)
            {
                string itemId = order[i];
                result.Add(new BagItemStack(itemId, counts[itemId]));
            }

            return result;
        }

        public static int CountDistinct(IReadOnlyList<string> itemIds)
        {
            return BuildStacks(itemIds).Count;
        }

        public static bool CanAddItem(IReadOnlyList<string> itemIds, string itemId, int maxDistinctItemCount)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            string normalizedItemId = itemId.Trim();

            if (itemIds != null)
            {
                for (int i = 0; i < itemIds.Count; i++)
                {
                    if (string.Equals(itemIds[i]?.Trim(), normalizedItemId, StringComparison.Ordinal))
                        return true;
                }
            }

            return CountDistinct(itemIds) < maxDistinctItemCount;
        }

        public static bool RemoveOne(List<string> itemIds, string itemId)
        {
            if (itemIds == null || string.IsNullOrWhiteSpace(itemId))
                return false;

            string normalizedItemId = itemId.Trim();

            for (int i = 0; i < itemIds.Count; i++)
            {
                if (!string.Equals(itemIds[i]?.Trim(), normalizedItemId, StringComparison.Ordinal))
                    continue;

                itemIds.RemoveAt(i);
                return true;
            }

            return false;
        }
    }
}
