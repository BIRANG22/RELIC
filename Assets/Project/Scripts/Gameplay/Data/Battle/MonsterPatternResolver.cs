using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public class MonsterPatternResolver
    {
        public string ResolveNextSkill(MonsterPatternData pattern, MonsterSkillLoadoutData loadout, int currentHp)
        {
            if (loadout == null || loadout.SkillIds == null || loadout.SkillIds.Length == 0)
                return null;

            if (pattern != null && pattern.Condition == "HP_LESS_THAN" && currentHp <= pattern.ConditionValue)
                return loadout.SkillIds[0];

            for (var i = 0; i < loadout.SkillIds.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(loadout.SkillIds[i]))
                    return loadout.SkillIds[i];
            }

            return null;
        }
    }
}
