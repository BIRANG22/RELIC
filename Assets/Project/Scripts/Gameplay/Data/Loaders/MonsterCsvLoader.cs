using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public static class MonsterCsvLoader
    {
        public static List<MonsterMasterData> Load(Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(workbook, "MonsterMasterData", "MonsterMaster", "Monster");
            return DataRowMapper.MapList<MonsterMasterData>(rows);
        }
    }
}
