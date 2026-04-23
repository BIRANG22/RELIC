using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public static class EventCsvLoader
    {
        public static List<EventMasterData> LoadMaster(Dictionary<string, List<Dictionary<string, string>>> workbook)
            => DataRowMapper.MapList<EventMasterData>(ExcelSheetSelector.GetSheet(workbook, "EventMasterData", "EventMaster"));

        public static List<EventChoiceData> LoadChoices(Dictionary<string, List<Dictionary<string, string>>> workbook)
            => DataRowMapper.MapList<EventChoiceData>(ExcelSheetSelector.GetSheet(workbook, "EventChoiceData", "EventChoice"));
    }
}
