using System;
using System.Collections.Generic;
using System.Linq;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public sealed class TutorialGuideData
    {
        public string IndexId;
        public int Page;
        public string IndexNameKey;
        public string IndexNameKO;
        public string PageNameKey;
        public string PageNameKO;
        public string ExplanationKey;
        public string ExplanationKO;
    }

    public sealed class TutorialGuideDatabase
    {
        private readonly Dictionary<string, List<TutorialGuideData>> pages = new(StringComparer.OrdinalIgnoreCase);
        public void Initialize(IEnumerable<TutorialGuideData> source)
        {
            pages.Clear();
            if (source == null) return;
            foreach (var entry in source)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.IndexId) || entry.Page < 1) continue;
                string key = entry.IndexId.Trim();
                if (!pages.TryGetValue(key, out var list)) pages[key] = list = new List<TutorialGuideData>();
                list.RemoveAll(x => x.Page == entry.Page);
                list.Add(entry);
            }
            foreach (var pair in pages) pair.Value.Sort((a, b) => a.Page.CompareTo(b.Page));
        }
        public IReadOnlyList<string> IndexIds => pages.Keys.OrderBy(x => ParseIndexNumber(x)).ThenBy(x => x).ToArray();
        public IReadOnlyList<TutorialGuideData> GetPages(string indexId) =>
            indexId != null && pages.TryGetValue(indexId, out var list) ? list : Array.Empty<TutorialGuideData>();
        public TutorialGuideData GetIndexInfo(string indexId)
        {
            var entries = GetPages(indexId);
            return entries.Count > 0 ? entries[0] : null;
        }
        public static int ParseIndexNumber(string indexId)
        {
            if (string.IsNullOrEmpty(indexId)) return 0;
            string digits = new string(indexId.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out int result) ? result : 0;
        }
    }
}
