using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public static class EventCsvLoader
    {
        public static List<EventData> Load(Dictionary<string, List<Dictionary<string, string>>> workbook)
        {
            List<EventData> events = DataRowMapper.MapList<EventData>(
                ExcelSheetSelector.GetSheet(workbook, "EventData", "Event"));

            for (int i = 0; i < events.Count; i++)
            {
                EventData data = events[i];
                if (data == null)
                    continue;

                data.EventId = EventIdUtility.Normalize(data.EventId);
                data.NextEventId = EventIdUtility.Normalize(data.NextEventId);
                data.FailNextEventId = EventIdUtility.Normalize(data.FailNextEventId);
            }

            return events;
        }
    }
}
