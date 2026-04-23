using System;
using System.Collections.Generic;
using System.Linq;

namespace Relic.Gameplay.Data
{
    public static class ExcelSheetSelector
    {
        public static IReadOnlyList<Dictionary<string, string>> GetSheet(Dictionary<string, List<Dictionary<string, string>>> workbook, params string[] candidates)
        {
            foreach (var candidate in candidates)
            {
                if (workbook.TryGetValue(candidate, out var rows))
                    return rows;

                var match = workbook.Keys.FirstOrDefault(k => string.Equals(k, candidate, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    return workbook[match];
            }

            return Array.Empty<Dictionary<string, string>>();
        }
    }
}
