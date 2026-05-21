using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public static class RangeCsvLoader
    {
        public static List<SkillRangeData> Load(
            Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(
                workbook,
                "SkillRange",
                "Range",
                "RangeData"
            );

            var list = DataRowMapper.MapList<SkillRangeData>(rows)
                .Where(x => !string.IsNullOrWhiteSpace(x.RangeId))
                .ToList();

            for (int rowIndex = 0; rowIndex < list.Count; rowIndex++)
            {
                SkillRangeData data = list[rowIndex];
                Dictionary<string, string> row = rows[rowIndex];

                data.RangeRaw.Clear();

                for (int i = 1; i <= 30; i++)
                {
                    string key = $"Range{i}";

                    if (!row.TryGetValue(key, out string raw))
                        continue;

                    if (string.IsNullOrWhiteSpace(raw) || raw == "0")
                        continue;

                    data.RangeRaw.Add(raw);
                }

                RangeParser.Parse(data);

                Debug.Log($"[RangeCsvLoader] RangeId:{data.RangeId}, Raw:{data.RangeRaw.Count}, Positions:{data.Positions.Count}");
            }

            return list;
        }
    }
}