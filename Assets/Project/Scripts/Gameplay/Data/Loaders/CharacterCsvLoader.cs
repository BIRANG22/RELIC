using System.Collections.Generic;
using System.Linq;

namespace Relic.Gameplay.Data
{
    public static class CharacterCsvLoader
    {
        public static List<CharacterMasterData> Load(Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            var rows = ExcelSheetSelector.GetSheet(workbook, "CharacterMasterData", "CharacterMaster", "Character");

            var list = DataRowMapper.MapList<CharacterMasterData>(rows);

            return list
                .Where(x => !string.IsNullOrWhiteSpace(x.CharacterId))
                .ToList();
        }
    }
}