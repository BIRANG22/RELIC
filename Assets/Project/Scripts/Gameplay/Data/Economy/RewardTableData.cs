using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class RewardTableData
    {
        public string TableId;
        public string TableName;
        public string UseTarget;
        public int MinRewardTypeCount;
        public int MaxRewardTypeCount;
        public bool AllowDuplicate;
        public int GuaranteedRewardCount;
    }
}
