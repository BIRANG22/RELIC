using System;
using Relic.Gameplay.Data;

public static class BattleRewardEquipSelectionPolicy
{
    private const int AbilityRuntimeSkillSlotIndex = 1;
    private const int FirstRewardRuntimeSkillSlotIndex = 1;
    private const int FirstFreeRuntimeSkillSlotIndex = 2;
    private const int LastRewardRuntimeSkillSlotIndex = 3;

    public static bool TryFindSkillViewIndex(
        CharacterRuntimeData character,
        Func<string, SkillMasterData> resolveSkill,
        out int skillViewIndex)
    {
        skillViewIndex = -1;
        if (character == null)
            return false;

        SkillInventoryEquipService.EnsureEquippedSkillArray(character);

        // 빈 슬롯을 우선 선택합니다. skill1도 비어 있다면 장착 대상이 될 수 있습니다.
        for (int runtimeIndex = FirstRewardRuntimeSkillSlotIndex;
             runtimeIndex <= LastRewardRuntimeSkillSlotIndex;
             runtimeIndex++)
        {
            if (!string.IsNullOrWhiteSpace(character.EquippedSkillIds[runtimeIndex]))
                continue;

            skillViewIndex = ToSkillViewIndex(runtimeIndex);
            return true;
        }

        if (resolveSkill == null)
            return false;

        // 기존 자유 슬롯(skill2/skill3)의 교체 가능 기억을 먼저 선택합니다.
        for (int runtimeIndex = FirstFreeRuntimeSkillSlotIndex;
             runtimeIndex <= LastRewardRuntimeSkillSlotIndex;
             runtimeIndex++)
        {
            SkillMasterData equippedSkill = resolveSkill(character.EquippedSkillIds[runtimeIndex]);
            if (!SkillRarityUtility.CanUnequip(equippedSkill))
                continue;

            skillViewIndex = ToSkillViewIndex(runtimeIndex);
            return true;
        }

        // 자유 슬롯에 대상이 없으면 skill1도 교체 대상으로 선택할 수 있습니다.
        if (!string.IsNullOrWhiteSpace(character.EquippedSkillIds[AbilityRuntimeSkillSlotIndex]))
        {
            skillViewIndex = ToSkillViewIndex(AbilityRuntimeSkillSlotIndex);
            return true;
        }

        return false;
    }

    private static int ToSkillViewIndex(int runtimeSkillSlotIndex)
    {
        return runtimeSkillSlotIndex - FirstRewardRuntimeSkillSlotIndex;
    }
}
