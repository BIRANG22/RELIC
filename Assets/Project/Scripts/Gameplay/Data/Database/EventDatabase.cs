using System.Collections.Generic;
using System.Linq;

namespace Relic.Gameplay.Data
{
    public class EventDatabase
    {
        private readonly LookupDatabase<EventMasterData> eventDb = new();
        private readonly List<EventChoiceData> allChoices = new();

        public void Initialize(IEnumerable<EventMasterData> events, IEnumerable<EventChoiceData> choices)
        {
            eventDb.Initialize(events, x => x.EventId);
            allChoices.Clear();
            allChoices.AddRange(choices);
        }

        public EventMasterData GetEvent(string id) => eventDb.Get(id);
        public List<EventChoiceData> GetChoices(string eventId) => allChoices.Where(x => x.EventId == eventId).OrderBy(x => x.ChoiceOrder).ToList();
    }
}
