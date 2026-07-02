using System.Collections.Generic;
using System.Linq;

namespace Relic.Gameplay.Data
{
    public static class GridEffectCsvLoader
    {
        public static List<GridEffectData> Load(
            Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(
                workbook,
                "GridEffect",
                "GridEffectData",
                "GridEffects"
            );

            ApplyColumnAliases(rows);

            return DataRowMapper.MapList<GridEffectData>(rows)
                .Where(x => !string.IsNullOrWhiteSpace(x.GridEffectID))
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

                DataColumnAliasUtility.CopyAlias(row, "Passed", "Passed", "Passable", "CanPass", "\uD1B5\uACFC\uC720\uBB34");
                DataColumnAliasUtility.CopyAlias(row, "Consumable", "Consumable", "IsConsumable", "Expendable", "expendable", "\uC18C\uBAA8\uC131");
            }
        }
    }
}
