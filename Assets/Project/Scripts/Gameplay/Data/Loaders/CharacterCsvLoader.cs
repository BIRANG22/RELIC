using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public static class CharacterCsvLoader
    {
        public static List<CharacterMasterData> Load(Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(workbook, "CharacterMasterData", "CharacterMaster", "Character");
            return DataRowMapper.MapList<CharacterMasterData>(rows);
        }
    }
}
