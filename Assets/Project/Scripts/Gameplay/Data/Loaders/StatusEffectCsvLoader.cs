using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public static class StatusEffectCsvLoader
    {
        public static List<StatusEffectMasterData> Load(Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(workbook, "StatusEffectMasterData", "StatusEffect");
            return DataRowMapper.MapList<StatusEffectMasterData>(rows);
        }
    }
}
