using System.Collections.Generic;
using System.Linq;

namespace Relic.Gameplay.Data
{
    public static class EffectCsvLoader
    {
        public static List<EffectMasterData> Load(
            Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(
                workbook,
                "EffectMasterData",
                "EffectMaster",
                "Effect"
            );

            var list = DataRowMapper.MapList<EffectMasterData>(rows);

            return list
                .Where(x => !string.IsNullOrWhiteSpace(x.EffectId))
                .ToList();
        }
    }
}