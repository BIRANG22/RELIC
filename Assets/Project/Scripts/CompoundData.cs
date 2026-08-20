using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class CompoundData : RelicData
    {
        public string CompoundId;
        public string TargetType;
        public int Durability;
        public string MaterialId1;
        public string MaterialId2;
        public string MaterialId3;
    }
}
