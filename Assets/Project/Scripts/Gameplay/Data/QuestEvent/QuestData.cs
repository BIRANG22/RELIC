using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class QuestData
    {
        public string QuestId;
        public string Name;
        public string Description;
        public string ObjectiveType;
        public string ObjectiveTarget;
        public int ObjectiveValue;
        public string RewardType;
        public int RewardValue;
    }
}
