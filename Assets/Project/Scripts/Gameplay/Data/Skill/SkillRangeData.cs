using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class SkillRangeData
    {
        public string RangeId;
        public string RangeCategory;
        public string TargetKind;
        public int Size1;
        public int Size2;
        public int Size3;
        public bool IgnoreObstacle;
    }
}
