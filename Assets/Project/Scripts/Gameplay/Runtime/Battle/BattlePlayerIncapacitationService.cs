using Relic.Gameplay.Data;

namespace Relic.Gameplay.Battle
{
    public static class BattlePlayerIncapacitationService
    {
        public static bool ResolveState(CharacterRuntimeData runtimeData, int hpBefore)
        {
            if (runtimeData == null || hpBefore <= 0 || runtimeData.CurrentHP > 0)
                return false;

            runtimeData.HandleDeath();
            return true;
        }

        public static bool Resolve(CharacterRuntimeData runtimeData, int hpBefore)
        {
            if (!ResolveState(runtimeData, hpBefore))
                return false;

            BattleErosionRuntimeService.CountCharacterIncapacitated();
            return true;
        }
    }
}
