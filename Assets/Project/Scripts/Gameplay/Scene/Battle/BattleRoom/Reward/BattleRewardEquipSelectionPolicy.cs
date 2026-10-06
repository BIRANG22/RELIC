using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;

public static class BattleRewardEquipSelectionPolicy
{
    private const int AbilityRuntimeSkillSlotIndex = 1;
    private const int FirstRewardRuntimeSkillSlotIndex = 1;
    private const int FirstFreeRuntimeSkillSlotIndex = 2;
    private const int LastRewardRuntimeSkillSlotIndex = 3;

    public static bool CanEquipRewardSkill(SkillMasterData skill, string characterId)
    {
        return SkillRarityUtility.CanEquipToFreeSlot(skill) &&
               SkillOwnershipPolicy.CanEquip(skill, characterId);
    }

    public static bool CanSelectCharacter(SkillMasterData skill, string characterId)
    {
        return CanEquipRewardSkill(skill, characterId);
    }

    public static int FindFirstCompatibleCharacterIndex(
        SkillMasterData skill,
        IReadOnlyList<string> partyCharacterIds)
    {
        if (skill == null || partyCharacterIds == null)
            return -1;

        for (int i = 0; i < partyCharacterIds.Count; i++)
        {
            if (CanSelectCharacter(skill, partyCharacterIds[i]))
                return i;
        }

        return -1;
    }

    public static bool TryFindSkillViewIndex(
        CharacterRuntimeData character,
        Func<string, SkillMasterData> resolveSkill,
        out int skillViewIndex)
    {
        skillViewIndex = -1;
        if (character == null)
            return false;

        SkillInventoryEquipService.EnsureEquippedSkillArray(character);

        // �� ������ �켱 �����մϴ�. skill1�� ��� �ִٸ� ���� ����� �� �� �ֽ��ϴ�.
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

        // ���� ���� ����(skill2/skill3)�� ��ü ���� ����� ���� �����մϴ�.
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

        // ���� ���Կ� ����� ������ skill1�� ��ü ������� ������ �� �ֽ��ϴ�.
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
