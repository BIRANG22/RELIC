namespace Relic.Gameplay.Data
{
    public static class SkillRuntimeValueBonusResolver
    {
        public static int GetTotalValueBonus(
            SkillRuntimeStore store,
            string characterId,
            string skillId)
        {
            if (store == null)
                return 0;

            return store.GetPermanentValueBonus(characterId, skillId) +
                   store.GetBattleValueBonus(characterId, skillId);
        }
    }
}
