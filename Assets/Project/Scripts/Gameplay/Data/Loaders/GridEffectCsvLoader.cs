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

            return DataRowMapper.MapList<GridEffectData>(rows)
                .Where(x => !string.IsNullOrWhiteSpace(x.GridEffectID))
                .ToList();
        }
    }
}
