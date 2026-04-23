using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class EventMasterData
    {
        public string EventId;
        public string EventName;
        public string EventCategory;
        public string DetailType;
        public string Title;
        public string Description;
        public string SpawnCondition;
        public string ResultRewardRef;
    }
}
