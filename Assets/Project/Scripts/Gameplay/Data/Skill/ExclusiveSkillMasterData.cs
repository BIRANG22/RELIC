using System;

namespace Relic.Gameplay.Data
{
    public enum ExclusiveSkillType { Passive, Unique }

    [Serializable]
    public class ExclusiveSkillMasterData
    {
        public string SkillId;
        public string Name;
        public ExclusiveSkillType Type;
        public string CharacterId;
        public bool IsDefaultProvided;
        public string UnlockCondition;
    }
}
