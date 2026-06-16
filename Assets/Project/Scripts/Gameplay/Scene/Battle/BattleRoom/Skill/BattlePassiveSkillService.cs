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

        CharacterRuntimeData runtime = character.RuntimeData;

        ClearPassiveStatusEffects(runtime);

        SkillMasterData passiveSkill = GetPassiveSkill(runtime);

        if (passiveSkill == null)
            return;

        int stack = CalculatePassiveStack(runtime, passiveSkill);

        if (stack <= 0)
            return;

        if (string.IsNullOrWhiteSpace(passiveSkill.EffectIds))
            return;

        string[] effectIds = passiveSkill.EffectIds.Split(';');

        for (int i = 0; i < effectIds.Length; i++)
        {
            string effectId = effectIds[i].Trim();

            if (string.IsNullOrWhiteSpace(effectId))
                continue;

            ApplyPassiveEffect(runtime, passiveSkill, effectId, stack);
        }
    }

    private void ApplyPassiveEffect(
        CharacterRuntimeData runtime,
        SkillMasterData passiveSkill,
        string effectId,
        int stack)
    {
        if (effectId == "E_Armor")
        {
            runtime.CurrentShield += stack;

            Debug.Log(
                $"[Passive] Armor / Character:{runtime.CharacterId} / " +
                $"Skill:{passiveSkill.SkillId} / Shield:+{stack} / CurrentShield:{runtime.CurrentShield}"
            );

            return;
        }

        StatusEffectRuntimeData status = new StatusEffectRuntimeData
        {
            EffectId = effectId,
            Stack = stack,
            TurnCount = 1,
            IsPassive = true,
            SourceSkillId = passiveSkill.SkillId
        };

        runtime.StatusEffects.Add(status);

        Debug.Log(
            $"[Passive] Status / Character:{runtime.CharacterId} / " +
            $"Skill:{passiveSkill.SkillId} / Effect:{effectId} / Stack:{stack}"
        );
    }

    private SkillMasterData GetPassiveSkill(CharacterRuntimeData runtime)
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

    private int CalculatePassiveStack(
        CharacterRuntimeData runtime,
        SkillMasterData passiveSkill)
    {
        if (runtime == null || passiveSkill == null)
            return 0;

        int resource = Mathf.Max(0, runtime.CurrentResource);

        switch (passiveSkill.SkillId)
        {
            case "S_Passive_01":
                return resource;

            case "S_Passive_02":
                return resource >= 2 ? 2 : 0;

            case "S_Passive_03":
                return resource >= 3 ? 1 : 0;

            case "S_Passive_04":
                return resource >= 5 ? 1 : 0;

            case "S_Passive_05":
                return resource >= 2 ? 1 : 0;

            case "S_Passive_06":
                return resource >= 3 ? 1 : 0;

            default:
                return 0;
        }
    }

    private void ClearPassiveStatusEffects(CharacterRuntimeData runtime)
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