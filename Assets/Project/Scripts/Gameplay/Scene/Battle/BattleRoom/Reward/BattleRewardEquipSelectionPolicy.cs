using System;
using Relic.Gameplay.Data;

public static class BattleRewardEquipSelectionPolicy
{
    private const int FirstFreeRuntimeSkillSlotIndex = 2;
    private const int LastFreeRuntimeSkillSlotIndex = 3;
    private const int FirstFreeSkillViewIndex = 1;

    public static bool TryFindSkillViewIndex(
        CharacterRuntimeData character,
        Func<string, SkillMasterData> resolveSkill,
        out int skillViewIndex)
    {
        skillViewIndex = -1;
        if (character == null)
            return false;

        SkillInventoryEquipService.EnsureEquippedSkillArray(character);

        for (int runtimeIndex = FirstFreeRuntimeSkillSlotIndex;
             runtimeIndex <= LastFreeRuntimeSkillSlotIndex;
             runtimeIndex++)
        {
            if (string.IsNullOrWhiteSpace(character.EquippedSkillIds[runtimeIndex]))
            {
                skillViewIndex = ToSkillViewIndex(runtimeIndex);
                return true;
            }
        }

        if (resolveSkill == null)
            return false;

        for (int runtimeIndex = FirstFreeRuntimeSkillSlotIndex;
             runtimeIndex <= LastFreeRuntimeSkillSlotIndex;
             runtimeIndex++)
        {
            SkillMasterData equippedSkill = resolveSkill(character.EquippedSkillIds[runtimeIndex]);
            if (!SkillRarityUtility.CanUnequip(equippedSkill))
                continue;

            skillViewIndex = ToSkillViewIndex(runtimeIndex);
            return true;
        }

        return false;
    }

    private static int ToSkillViewIndex(int runtimeSkillSlotIndex)
    {
        return runtimeSkillSlotIndex - FirstFreeRuntimeSkillSlotIndex + FirstFreeSkillViewIndex;
    }
}
