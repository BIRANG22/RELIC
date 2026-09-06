using System.Collections.Generic;
using System.Linq;

namespace Relic.Gameplay.Data
{
    public class EventDatabase
    {
        private readonly LookupDatabase<EventDefinition> eventDb = new();
        private readonly List<EventDefinition> events = new();

        public void Initialize(IEnumerable<EventData> eventRows)
        {
            events.Clear();

            if (eventRows == null)
            {
                eventDb.Initialize(events, x => x.EventId);
                return;
            }

            foreach (IGrouping<string, EventData> group in eventRows
                         .Where(row => row != null)
                         .Select(NormalizeRow)
                         .Where(row => !string.IsNullOrWhiteSpace(row.EventId))
                         .GroupBy(row => row.EventId))
            {
                EventData first = group.FirstOrDefault();
                EventDefinition definition = new()
                {
                    EventId = group.Key,
                    EventName = FirstNonEmpty(group.Select(row => row.EventName)),
                    Title = FirstNonEmpty(group.Select(row => row.Title)),
                    Choices = group
                        .Where(row => !string.IsNullOrWhiteSpace(row.ChoiceName) ||
                                      !string.IsNullOrWhiteSpace(row.ChoiceDesc) ||
                                      !string.IsNullOrWhiteSpace(row.ResultType))
                        .OrderBy(row => row.ChoiceOrder)
                        .ThenBy(row => row.ChoiceName)
                        .ToList()
                };

                if (string.IsNullOrWhiteSpace(definition.EventName))
                    definition.EventName = first?.EventName;

                if (string.IsNullOrWhiteSpace(definition.Title))
                    definition.Title = first?.Title;

                events.Add(definition);
            }

            eventDb.Initialize(events, x => x.EventId);
        }

        public EventDefinition GetEvent(string id) => eventDb.Get(EventIdUtility.Normalize(id));

        public bool TryGetEvent(string id, out EventDefinition definition)
        {
            return eventDb.TryGet(EventIdUtility.Normalize(id), out definition);
        }

        public IReadOnlyList<EventDefinition> GetAll()
        {
            return events;
        }

        private static EventData NormalizeRow(EventData data)
        {
            data.EventId = EventIdUtility.Normalize(data.EventId);
            data.NextEventId = EventIdUtility.Normalize(data.NextEventId);
            data.FailNextEventId = EventIdUtility.Normalize(data.FailNextEventId);
            return data;
        }

        private static string FirstNonEmpty(IEnumerable<string> values)
        {
            if (values == null)
                return string.Empty;

            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }

            return string.Empty;
        }
    }
}
