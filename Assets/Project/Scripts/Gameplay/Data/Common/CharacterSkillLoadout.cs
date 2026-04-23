using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public class CharacterSkillLoadout
    {
        public string PassiveId;
        public string UniqueSkillId;
        public string[] CommonSkillIds = new string[3];
        public SkillFragmentSlotData[] FragmentSlots = new SkillFragmentSlotData[3]
        {
            new SkillFragmentSlotData(), new SkillFragmentSlotData(), new SkillFragmentSlotData()
        };
    }
}
