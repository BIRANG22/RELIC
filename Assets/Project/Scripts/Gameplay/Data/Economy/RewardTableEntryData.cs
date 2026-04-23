using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class RewardTableEntryData
    {
        public string EntryId;
        public string TableId;
        public string RewardType;
        public string RewardId;
        public float Probability;
        public int MinQuantity;
        public int MaxQuantity;
        public bool IsGuaranteedDrop;
        public bool IsNoDuplicate;
        public bool HasCondition;
    }
}
