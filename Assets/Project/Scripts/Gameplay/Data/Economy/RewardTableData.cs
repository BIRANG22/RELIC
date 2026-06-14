using System;
namespace Relic.Gameplay.Data
{ 
    [Serializable] 
    public class RewardTableData
    { 
        public string DropTableId;
        public string DropType;
        public string DropId;
        public int MinAmount;
        public int MaxAmount;
        public int Chance;
    } 
}