using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public class SkillInventoryEquipService
{
    private const int FirstFreeSkillSlotIndex = 2;
    private const int LastFreeSkillSlotIndex = 3;

    private readonly CharacterRuntimeStore characterStore;
    private readonly BattleRuntimeData battleRuntimeData;
    private readonly Func<string, SkillMasterData> skillResolver;

    public SkillInventoryEquipService(
        CharacterRuntimeStore characterStore,
        BattleRuntimeData battleRuntimeData,
        Func<string, SkillMasterData> skillResolver)
    {
        this.characterStore = characterStore;
        this.battleRuntimeData = battleRuntimeData;
        this.skillResolver = skillResolver;
    }

    public bool EquipInventorySkillToSlot(
        string characterId,
        int equippedSkillIndex,
        string skillId)
    {
        if (!TryGetCharacter(characterId, out CharacterRuntimeData character))
            return false;

        if (!IsFreeSkillSlotIndex(equippedSkillIndex))
            return false;

        if (string.IsNullOrWhiteSpace(skillId))
            return false;

        skillId = skillId.Trim();
        EnsureInventory();
        EnsureEquippedSkillArray(character);

        if (!HasInventorySkill(skillId))
        {
            Debug.LogWarning($"[SkillInventoryEquipService] 보유하지 않은 스킬: {skillId}");
            return false;
        }

        SkillMasterData nextSkill = ResolveSkill(skillId);

        if (!SkillRarityUtility.CanEquipToFreeSlot(nextSkill))
            return false;

        string previousSkillId = character.EquippedSkillIds[equippedSkillIndex];

        if (!string.IsNullOrWhiteSpace(previousSkillId) &&
            !string.Equals(previousSkillId.Trim(), skillId, StringComparison.Ordinal))
        {
            SkillMasterData previousSkill = ResolveSkill(previousSkillId);

            if (!SkillRarityUtility.CanUnequip(previousSkill))
                return false;

            AddInventorySkillIfMissing(previousSkillId);
        }

        character.EquippedSkillIds[equippedSkillIndex] = skillId;
        RemoveOneInventorySkill(skillId);
        characterStore.AddOrUpdate(character);
        return true;
    }

    public bool UnequipSkillFromSlot(string characterId, int equippedSkillIndex)
    {
        if (!TryGetCharacter(characterId, out CharacterRuntimeData character))
            return false;

        if (!IsFreeSkillSlotIndex(equippedSkillIndex))
            return false;

        EnsureInventory();
        EnsureEquippedSkillArray(character);

        string skillId = character.EquippedSkillIds[equippedSkillIndex];

        if (string.IsNullOrWhiteSpace(skillId))
            return false;

        SkillMasterData skill = ResolveSkill(skillId);

        if (!SkillRarityUtility.CanUnequip(skill))
            return false;

        character.EquippedSkillIds[equippedSkillIndex] = string.Empty;
        AddInventorySkillIfMissing(skillId);
        characterStore.AddOrUpdate(character);
        return true;
    }

    public static bool IsFreeSkillSlotIndex(int equippedSkillIndex)
    {
        return equippedSkillIndex >= FirstFreeSkillSlotIndex &&
               equippedSkillIndex <= LastFreeSkillSlotIndex;
    }

    public static void EnsureEquippedSkillArray(CharacterRuntimeData character)
    {
        if (character == null)
            return;

        if (character.EquippedSkillIds != null &&
            character.EquippedSkillIds.Length == 4)
            return;

        string[] newSlots = new string[4];

        if (character.EquippedSkillIds != null)
        {
            int count = Mathf.Min(character.EquippedSkillIds.Length, newSlots.Length);

            for (int i = 0; i < count; i++)
                newSlots[i] = character.EquippedSkillIds[i];
        }

        character.EquippedSkillIds = newSlots;
    }

    private bool TryGetCharacter(string characterId, out CharacterRuntimeData character)
    {
        character = null;

        if (string.IsNullOrWhiteSpace(characterId) || characterStore == null)
            return false;

        return characterStore.TryGet(characterId, out character) && character != null;
    }

    private SkillMasterData ResolveSkill(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId) || skillResolver == null)
            return null;

        return skillResolver(skillId.Trim());
    }

    private void EnsureInventory()
    {
        if (battleRuntimeData == null)
            return;

        battleRuntimeData.SkillInventoryIds ??= new List<string>();
    }

    private bool HasInventorySkill(string skillId)
    {
        if (battleRuntimeData?.SkillInventoryIds == null || string.IsNullOrWhiteSpace(skillId))
            return false;

        string targetId = skillId.Trim();

        for (int i = 0; i < battleRuntimeData.SkillInventoryIds.Count; i++)
        {
            if (string.Equals(
                    battleRuntimeData.SkillInventoryIds[i]?.Trim(),
                    targetId,
                    StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private void AddInventorySkillIfMissing(string skillId)
    {
        if (battleRuntimeData == null || string.IsNullOrWhiteSpace(skillId))
            return;

        EnsureInventory();

        skillId = skillId.Trim();

        if (!HasInventorySkill(skillId))
            battleRuntimeData.SkillInventoryIds.Add(skillId);
    }

    private void RemoveOneInventorySkill(string skillId)
    {
        if (battleRuntimeData?.SkillInventoryIds == null || string.IsNullOrWhiteSpace(skillId))
            return;

        string targetId = skillId.Trim();

        for (int i = 0; i < battleRuntimeData.SkillInventoryIds.Count; i++)
        {
            if (!string.Equals(
                    battleRuntimeData.SkillInventoryIds[i]?.Trim(),
                    targetId,
                    StringComparison.Ordinal))
            {
                continue;
            }

            battleRuntimeData.SkillInventoryIds.RemoveAt(i);
            return;
        }
    }
}
