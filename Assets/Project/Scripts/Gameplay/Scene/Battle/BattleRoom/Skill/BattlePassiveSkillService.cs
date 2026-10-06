using System.Collections.Generic;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
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

        if (!ShouldApplyPassive(passiveSkill, runtime))
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

    public static bool ShouldApplyPassive(
        SkillMasterData passiveSkill,
        CharacterRuntimeData runtime)
    {
        return passiveSkill != null && runtime != null;
    }

    private static void ApplyPassiveEffect(
        CharacterRuntimeData runtime,
        SkillMasterData passiveSkill,
        string effectId,
        int value,
        int count)
    {
        int appliedValue = BattleEffectUtility.GetRepeatedValue(value, count);
        BattlePassiveTargetGroup targetGroup = BattlePassiveTargetPolicy.Resolve(passiveSkill.Target);

        if (targetGroup == BattlePassiveTargetGroup.Monsters)
        {
            ApplyStatusToAllLivingMonsters(runtime, passiveSkill, effectId, appliedValue);
            return;
        }

        if (targetGroup == BattlePassiveTargetGroup.Players)
        {
            ApplyEffectToAllLivingPlayers(runtime, passiveSkill, effectId, appliedValue);
            return;
        }

        if (effectId == "E_Armor")
        {
            ApplyArmorToPlayer(runtime, runtime, passiveSkill, appliedValue);
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

    private static void ApplyStatusToAllLivingMonsters(
        CharacterRuntimeData ownerRuntime,
        SkillMasterData passiveSkill,
        string effectId,
        int appliedValue)
    {
        int finalStack = BattleEquipmentEffectService.ModifyPassiveEffectStack(
            ownerRuntime,
            effectId,
            appliedValue);

        if (finalStack <= 0)
            return;

        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];
            if (monster == null || monster.RuntimeData == null || monster.RuntimeData.IsDead)
                continue;

            BattleEffectUtility.AddStatusToMonster(monster, effectId, finalStack, 1);

            Debug.Log(
                $"[Passive] EnemyParty / Owner:{ownerRuntime.CharacterId} / " +
                $"Target:{monster.RuntimeData.RuntimeId} / Skill:{passiveSkill.SkillId} / " +
                $"Effect:{effectId} / Stack:{finalStack}");
        }
    }

    private static void ApplyEffectToAllLivingPlayers(
        CharacterRuntimeData ownerRuntime,
        SkillMasterData passiveSkill,
        string effectId,
        int appliedValue)
    {
        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];
            CharacterRuntimeData targetRuntime = character != null ? character.RuntimeData : null;

            if (targetRuntime == null || targetRuntime.IsDead)
                continue;

            if (effectId == "E_Armor")
            {
                ApplyArmorToPlayer(ownerRuntime, targetRuntime, passiveSkill, appliedValue);
                continue;
            }

            int finalStack = BattleEquipmentEffectService.ModifyPassiveEffectStack(
                targetRuntime,
                effectId,
                appliedValue);

            if (finalStack <= 0)
                continue;

            BattleEffectUtility.AddStatusToPlayer(character, effectId, finalStack, 1);
        }
    }

    private static void ApplyArmorToPlayer(
        CharacterRuntimeData ownerRuntime,
        CharacterRuntimeData targetRuntime,
        SkillMasterData passiveSkill,
        int appliedValue)
    {
        if (targetRuntime == null || targetRuntime.IsDead)
            return;

        int finalValue = BattleEquipmentEffectService.ModifyPassiveEffectStack(
            targetRuntime,
            "E_Armor",
            appliedValue);

        if (finalValue <= 0)
            return;

        targetRuntime.CurrentShield += finalValue;
        BattleDamageTextPopupUI.ShowArmorGain(targetRuntime.CharacterId, finalValue);

        Debug.Log(
            $"[Passive] Armor / Owner:{ownerRuntime.CharacterId} / Target:{targetRuntime.CharacterId} / " +
            $"Skill:{passiveSkill.SkillId} / Shield:+{finalValue} / CurrentShield:{targetRuntime.CurrentShield}");
    }


    private static CharacterRuntimeData FindLowestCurrentHpInjuredLivingPartyMember()
    {
        if (DataManager.Instance == null ||
            DataManager.Instance.CharacterRuntimeStore == null)
        {
            return null;
        }

        CharacterRuntimeData best = null;
        int bestCurrentHp = int.MaxValue;
        HashSet<string> addedIds = new(System.StringComparer.Ordinal);
        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        if (partyStore != null)
        {
            for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
            {
                string characterId = partyStore.GetCharacterId(i);

                if (string.IsNullOrWhiteSpace(characterId))
                    continue;

                characterId = characterId.Trim();

                if (!addedIds.Add(characterId))
                    continue;

                if (!DataManager.Instance.CharacterRuntimeStore.TryGet(
                        characterId,
                        out CharacterRuntimeData candidate) ||
                    candidate == null ||
                    candidate.IsDead ||
                    candidate.MaxHP <= 0 ||
                    candidate.CurrentHP >= candidate.MaxHP)
                {
                    continue;
                }

                if (best == null || candidate.CurrentHP < bestCurrentHp)
                {
                    best = candidate;
                    bestCurrentHp = candidate.CurrentHP;
                }
            }
        }

        if (best != null)
            return best;

        IReadOnlyDictionary<string, CharacterRuntimeData> allCharacters =
            DataManager.Instance.CharacterRuntimeStore.GetAll();

        if (allCharacters == null)
            return null;

        foreach (KeyValuePair<string, CharacterRuntimeData> pair in allCharacters)
        {
            CharacterRuntimeData candidate = pair.Value;

            if (candidate == null ||
                candidate.IsDead ||
                candidate.MaxHP <= 0 ||
                candidate.CurrentHP >= candidate.MaxHP)
            {
                continue;
            }

            if (best == null || candidate.CurrentHP < bestCurrentHp)
            {
                best = candidate;
                bestCurrentHp = candidate.CurrentHP;
            }
        }

        return best;
    }

    private static BattleCharacter FindBattleCharacter(CharacterRuntimeData runtime)
    {
        if (runtime == null)
            return null;

        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null)
                continue;

            if (ReferenceEquals(character.RuntimeData, runtime) ||
                character.RuntimeData.CharacterId == runtime.CharacterId)
            {
                return character;
            }
        }

        return null;
    }

    private static CharacterRuntimeData FindLowestCurrentHpLivingPartyMember()
    {
        if (DataManager.Instance == null ||
            DataManager.Instance.CharacterRuntimeStore == null)
        {
            return null;
        }

        CharacterRuntimeData best = null;
        int bestCurrentHp = int.MaxValue;
        HashSet<string> addedIds = new(System.StringComparer.Ordinal);
        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        if (partyStore != null)
        {
            for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
            {
                string characterId = partyStore.GetCharacterId(i);

                if (string.IsNullOrWhiteSpace(characterId))
                    continue;

                characterId = characterId.Trim();

                if (!addedIds.Add(characterId))
                    continue;

                if (!DataManager.Instance.CharacterRuntimeStore.TryGet(
                        characterId,
                        out CharacterRuntimeData candidate) ||
                    candidate == null ||
                    candidate.IsDead ||
                    candidate.MaxHP <= 0)
                {
                    continue;
                }

                if (best == null || candidate.CurrentHP < bestCurrentHp)
                {
                    best = candidate;
                    bestCurrentHp = candidate.CurrentHP;
                }
            }
        }

        if (best != null)
            return best;

        IReadOnlyDictionary<string, CharacterRuntimeData> allCharacters =
            DataManager.Instance.CharacterRuntimeStore.GetAll();

        if (allCharacters == null)
            return null;

        foreach (KeyValuePair<string, CharacterRuntimeData> pair in allCharacters)
        {
            CharacterRuntimeData candidate = pair.Value;

            if (candidate == null || candidate.IsDead || candidate.MaxHP <= 0)
                continue;

            if (best == null || candidate.CurrentHP < bestCurrentHp)
            {
                best = candidate;
                bestCurrentHp = candidate.CurrentHP;
            }
        }

        return best;
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
