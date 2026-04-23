using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class MonsterSkillLoadoutData
    {
        public string MonsterId;
        public string[] SkillIds = new string[10];
    }
}
