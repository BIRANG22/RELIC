using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class EventManager
    {
        public string CurrentEventId { get; private set; }

        public void StartEvent(string eventId) => CurrentEventId = eventId;
        public EventChoiceData ApplyChoice(List<EventChoiceData> choices, int order) => choices.Find(x => x.ChoiceOrder == order);
    }
}
