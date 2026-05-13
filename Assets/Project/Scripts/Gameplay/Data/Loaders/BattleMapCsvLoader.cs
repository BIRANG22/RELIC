using System.Collections.Generic;
using System.Linq;

namespace Relic.Gameplay.Data
{
    public static class BattleMapCsvLoader
    {
        public static List<BattleMapData> Load(Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(workbook, "BattleMap");

            var list = DataRowMapper.MapList<BattleMapData>(rows);

            return list
                .Where(x => !string.IsNullOrWhiteSpace(x.BattleMapId))
                .ToList();
        }
    }
}