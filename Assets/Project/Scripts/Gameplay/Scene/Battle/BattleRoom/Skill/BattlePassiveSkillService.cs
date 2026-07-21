using Relic.Gameplay.Data;
using UnityEngine;

public class BattlePassiveSkillService
{
    public void RefreshAllPlayerPassives()
    {
        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
            RefreshPassiveEffects(characters[i]);
    }

    public void RefreshPassiveEffects(BattleCharacter character)
    {
        if (character == null || character.RuntimeData == null)
            return;

        RefreshRuntimePassiveEffects(character.RuntimeData);
    }

    public static void RefreshRuntimePassiveEffects(CharacterRuntimeData runtime)
    {
        if (runtime == null)
            return;

        ClearPassiveStatusEffects(runtime);

        SkillMasterData passiveSkill = GetPassiveSkill(runtime);

        if (passiveSkill == null || !IsPassiveConditionMet(runtime))
        {
            BattleEquipmentEffectService.ApplyPassiveExtras(runtime);
            return;
        }

        if (passiveSkill.EffectEntries == null || passiveSkill.EffectEntries.Count == 0)
        {
            BattleEquipmentEffectService.ApplyPassiveExtras(runtime);
            return;
        }

        for (int i = 0; i < passiveSkill.EffectEntries.Count; i++)
        {
            SkillEffectEntry entry = passiveSkill.EffectEntries[i];

            if (entry == null || string.IsNullOrWhiteSpace(entry.EffectId))
                continue;

            int value = Mathf.Max(0, entry.ValueAmount);
            int count = Mathf.Max(1, entry.CountAmount);

            ApplyPassiveEffect(runtime, passiveSkill, entry.EffectId, value, count);
        }

        BattleEquipmentEffectService.ApplyPassiveExtras(runtime);
    }

    private static void ApplyPassiveEffect(
        CharacterRuntimeData runtime,
        SkillMasterData passiveSkill,
        string effectId,
        int value,
        int count)
    {
        int appliedValue = BattleEffectUtility.GetRepeatedValue(value, count);

        if (effectId == "E_Armor")
        {
            int finalValue =
                BattleEquipmentEffectService.ModifyPassiveEffectStack(runtime, effectId, appliedValue);

            if (finalValue <= 0)
                return;

            runtime.CurrentShield += finalValue;
            BattleDamageTextPopupUI.ShowArmorGain(runtime.CharacterId, finalValue);

            Debug.Log(
                $"[Passive] Armor / Character:{runtime.CharacterId} / " +
                $"Skill:{passiveSkill.SkillId} / Shield:+{finalValue} / CurrentShield:{runtime.CurrentShield}"
            );

            return;
        }

        int finalStack =
            BattleEquipmentEffectService.ModifyPassiveEffectStack(runtime, effectId, appliedValue);

        if (finalStack <= 0)
            return;

        StatusEffectRuntimeData status = new StatusEffectRuntimeData
        {
            EffectId = effectId,
            Stack = finalStack,
            TurnCount = 1,
            IsPassive = true,
            SourceSkillId = passiveSkill.SkillId
        };

        if (runtime.StatusEffects == null)
            runtime.StatusEffects = new System.Collections.Generic.List<StatusEffectRuntimeData>();

        runtime.StatusEffects.Add(status);

        Debug.Log(
            $"[Passive] Status / Character:{runtime.CharacterId} / " +
            $"Skill:{passiveSkill.SkillId} / Effect:{effectId} / " +
            $"Stack:{finalStack} / Turn:1"
        );
    }

    private static bool IsPassiveConditionMet(CharacterRuntimeData runtime)
    {
        if (runtime == null || DataManager.Instance == null)
            return false;

        CharacterMasterData characterData =
            DataManager.Instance.CharacterDatabase.Get(runtime.CharacterId);

        if (characterData == null)
            return false;

        int maxResource = Mathf.Max(0, characterData.MaxResource);

        return maxResource > 0 && runtime.CurrentResource >= maxResource;
    }

    private static SkillMasterData GetPassiveSkill(CharacterRuntimeData runtime)
    {
        if (runtime == null || DataManager.Instance == null)
            return null;

        string passiveSkillId = runtime.PassiveSkillId;

        if (string.IsNullOrWhiteSpace(passiveSkillId))
        {
            if (runtime.EquippedSkillIds != null && runtime.EquippedSkillIds.Length > 0)
                passiveSkillId = runtime.EquippedSkillIds[0];
        }

        if (string.IsNullOrWhiteSpace(passiveSkillId))
            return null;

        SkillMasterData skillData =
            DataManager.Instance.SkillDatabase.Get(passiveSkillId);

        if (skillData == null || skillData.Category != Category.Passive)
            return null;

        return skillData;
    }

    private static void ClearPassiveStatusEffects(CharacterRuntimeData runtime)
    {
        if (runtime == null || runtime.StatusEffects == null)
            return;

        for (int i = runtime.StatusEffects.Count - 1; i >= 0; i--)
        {
            StatusEffectRuntimeData status = runtime.StatusEffects[i];

            if (status == null)
                continue;

            if (status.IsPassive)
                runtime.StatusEffects.RemoveAt(i);
        }
    }

    public void ClearAllPlayerPassiveEffects()
    {
        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null)
                continue;

            ClearPassiveStatusEffects(character.RuntimeData);
        }
    }
}
