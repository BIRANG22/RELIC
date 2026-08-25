using System.Collections.Generic;
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

        if (effectId == "E_Heal" && passiveSkill.SkillId == "S_Passive_07")
        {
            // S_Passive_07(자애심): 조건은 패시브 보유자의 카르마 최대 여부를 확인하지만,
            // 실제 회복 대상은 최대 생명력보다 현재 생명력이 낮은 생존 아군 중
            // 현재 생명력 수치가 가장 낮은 1명입니다. 회복은 버프가 아니라 즉시 적용합니다.
            CharacterRuntimeData targetRuntime = FindLowestCurrentHpInjuredLivingPartyMember();

            if (targetRuntime == null)
                return;

            BattleCharacter targetCharacter = FindBattleCharacter(targetRuntime);

            if (targetCharacter != null)
            {
                int hpBefore = targetRuntime.CurrentHP;
                BattleEffectUtility.HealPlayer(targetCharacter, appliedValue);
                int healedValue = Mathf.Max(0, targetRuntime.CurrentHP - hpBefore);

                Debug.Log(
                    $"[Passive] Heal / Owner:{runtime.CharacterId} / Target:{targetRuntime.CharacterId} / " +
                    $"Skill:{passiveSkill.SkillId} / Heal:+{healedValue} / CurrentHP:{targetRuntime.CurrentHP}"
                );
            }
            else
            {
                int hpBefore = targetRuntime.CurrentHP;
                targetRuntime.CurrentHP = Mathf.Min(
                    targetRuntime.MaxHP,
                    targetRuntime.CurrentHP + appliedValue);

                int healedValue = Mathf.Max(0, targetRuntime.CurrentHP - hpBefore);

                Debug.Log(
                    $"[Passive] Heal / Owner:{runtime.CharacterId} / Target:{targetRuntime.CharacterId} / " +
                    $"Skill:{passiveSkill.SkillId} / Heal:+{healedValue} / CurrentHP:{targetRuntime.CurrentHP}"
                );
            }

            return;
        }

        if (effectId == "E_Armor")
        {
            CharacterRuntimeData targetRuntime = runtime;

            // S_Passive_06(결심): 조건은 패시브 보유자의 카르마 최대 여부를 확인하지만,
            // 실제 방어도는 현재 생명력 수치가 가장 낮은 살아있는 아군 1명에게 부여합니다.
            if (passiveSkill.SkillId == "S_Passive_06")
            {
                targetRuntime = FindLowestCurrentHpLivingPartyMember();
                if (targetRuntime == null)
                    return;
            }

            int finalValue =
                BattleEquipmentEffectService.ModifyPassiveEffectStack(targetRuntime, effectId, appliedValue);

            if (finalValue <= 0)
                return;

            targetRuntime.CurrentShield += finalValue;
            BattleDamageTextPopupUI.ShowArmorGain(targetRuntime.CharacterId, finalValue);

            Debug.Log(
                $"[Passive] Armor / Owner:{runtime.CharacterId} / Target:{targetRuntime.CharacterId} / " +
                $"Skill:{passiveSkill.SkillId} / Shield:+{finalValue} / CurrentShield:{targetRuntime.CurrentShield}"
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
