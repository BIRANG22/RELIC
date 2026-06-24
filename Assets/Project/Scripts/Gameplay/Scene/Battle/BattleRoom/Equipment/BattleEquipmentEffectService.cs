using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public static class BattleEquipmentEffectService
{
    private const int LastTimelineSlotIndex = 4;
    private const int MoveSkillLevelTwoThreshold = 50;
    private const string MoveSkillLevelOneId = "S_Move_1";
    private const string MoveSkillLevelTwoId = "S_Move_2";
    private const string Relic06Turn2ArmorAppliedId = "Relic_06_Turn2Armor";

    public static void ApplyBattleStartEffects(
        CharacterRuntimeData runtime,
        CharacterMasterData masterData)
    {
        if (runtime == null)
            return;

        ResetBattleOnlyEffectState(runtime);

        int baseMaxHP = masterData != null
            ? Mathf.Max(1, masterData.MaxHP)
            : Mathf.Max(1, runtime.MaxHP);

        int previousMaxHP = runtime.MaxHP > 0
            ? runtime.MaxHP
            : baseMaxHP;

        bool shouldFillHP =
            runtime.CurrentHP <= 0 ||
            runtime.CurrentHP >= previousMaxHP;

        runtime.MaxHP = Mathf.Max(1, baseMaxHP + GetMaxHPBonus(runtime));
        runtime.CurrentHP = shouldFillHP
            ? runtime.MaxHP
            : Mathf.Clamp(runtime.CurrentHP, 1, runtime.MaxHP);

        int baseMaxCost = masterData != null
            ? Mathf.Max(0, masterData.MaxCost)
            : Mathf.Max(0, runtime.MaxCost);

        runtime.MaxCost = Mathf.Max(0, baseMaxCost + GetMaxCostBonus(runtime));
        runtime.CurrentCost = Mathf.Max(0, runtime.MaxCost + GetBattleStartCostBonus(runtime));

        runtime.CurrentResource = GetBattleStartUniqueResource(runtime, masterData);
        runtime.CurrentMoveLevel = Mathf.Max(0, GetEffectiveMoveValue(runtime, masterData));
        SyncMoveSkillForMoveValue(runtime);
        runtime.ClearReservedCosts();
    }

    public static void SyncMoveSkillForMoveValue(CharacterRuntimeData runtime)
    {
        if (runtime == null)
            return;

        runtime.MoveSkillId = runtime.CurrentMoveLevel >= MoveSkillLevelTwoThreshold
            ? MoveSkillLevelTwoId
            : MoveSkillLevelOneId;
    }

    public static void ResetBattleOnlyEffectState(CharacterRuntimeData runtime)
    {
        if (runtime == null)
            return;

        if (runtime.AppliedBattleEquipmentEffectIds == null)
            runtime.AppliedBattleEquipmentEffectIds = new List<string>();
        else
            runtime.AppliedBattleEquipmentEffectIds.Clear();
    }

    public static void ApplyPlayerTurnStartEffects(
        CharacterRuntimeData runtime,
        int playerTurnNumber)
    {
        if (runtime == null)
            return;

        if (playerTurnNumber == 2 &&
            HasRelic(runtime, "Relic_06") &&
            TryMarkBattleEffectApplied(runtime, Relic06Turn2ArmorAppliedId))
        {
            runtime.CurrentShield += ModifyArmorGain(runtime, 10);
        }
    }

    public static int GetEffectiveMoveValue(
        CharacterRuntimeData runtime,
        CharacterMasterData masterData)
    {
        int baseMoveValue = masterData != null
            ? Mathf.Max(0, masterData.MoveValue)
            : runtime != null ? Mathf.Max(0, runtime.CurrentMoveLevel) : 0;

        return Mathf.Max(0, baseMoveValue + GetMoveValueBonus(runtime));
    }

    public static int ModifyUniqueResourceGain(
        CharacterRuntimeData runtime,
        int amount)
    {
        int safeAmount = Mathf.Max(0, amount);

        if (runtime == null || safeAmount <= 0)
            return safeAmount;

        if (IsCharacter(runtime, "Char_01") &&
            HasRune(runtime, "Rune_03") &&
            runtime.CurrentResource <= 0)
        {
            safeAmount += 1;
        }

        return safeAmount;
    }

    public static int GetAllCurrentMinimumCost(
        CharacterRuntimeData runtime,
        SkillMasterData skillData,
        int defaultMinimum)
    {
        int minimum = Mathf.Max(1, defaultMinimum);

        if (runtime == null || skillData == null)
            return minimum;

        if (skillData.ReferenceResource != ReferenceResource.UniqueResource ||
            skillData.ResourceCostType != ResourceCostType.AllCurrent)
        {
            return minimum;
        }

        if (IsCharacter(runtime, "Char_01") && HasRune(runtime, "Rune_05"))
            return 1;

        if (IsCharacter(runtime, "Char_02") && HasRune(runtime, "Rune_10"))
            return 1;

        if (IsCharacter(runtime, "Char_03") && HasRune(runtime, "Rune_15"))
            return 1;

        return minimum;
    }

    public static void ApplyReservationCostModifiers(
        PlayerReservedCommand command,
        int slotIndex,
        bool isFirstMoveCommand,
        bool isLastTimelineSlot,
        int duplicateSkillReservationCountInSlot = 0)
    {
        if (command == null)
            return;

        command.ResetCostsToBase();
        command.SetTimelineSlotIndex(slotIndex);

        if (command.IsMoveContinuationCommand)
        {
            command.SetCosts(0, 0, 0, 0, 0);
            command.MarkReservationCostModifiersApplied();
            return;
        }

        int hpCost = command.HPCost;
        int cost = command.Cost;
        int resourceCost = command.ResourceCost;
        int moveCost = command.MoveCost;
        int shieldCost = command.ShieldCost;

        if (HasRune(command.UserRuntime, "Rune_24") &&
            isFirstMoveCommand &&
            IsMoveCommand(command))
        {
            ReduceFirstPositiveCost(
                ref hpCost,
                ref cost,
                ref resourceCost,
                ref moveCost,
                ref shieldCost);
        }

        if (HasRelic(command.UserRuntime, "Relic_05") && slotIndex == 0)
        {
            ReduceFirstPositiveCost(
                ref hpCost,
                ref cost,
                ref resourceCost,
                ref moveCost,
                ref shieldCost);
        }

        cost += Mathf.Max(0, duplicateSkillReservationCountInSlot);

        command.SetCosts(
            hpCost,
            cost,
            resourceCost,
            moveCost,
            shieldCost);
        command.MarkReservationCostModifiersApplied();
    }

    public static int ModifyPassiveEffectStack(
        CharacterRuntimeData runtime,
        string effectId,
        int baseStack)
    {
        int stack = Mathf.Max(0, baseStack);

        if (effectId == "E_Armor")
            return ModifyArmorGain(runtime, stack);

        return stack;
    }

    public static void ApplyPassiveExtras(CharacterRuntimeData runtime)
    {
        if (runtime == null)
            return;

        int resource = Mathf.Max(0, runtime.CurrentResource);

        if (IsCharacter(runtime, "Char_01") &&
            HasRune(runtime, "Rune_02") &&
            resource >= 3)
        {
            AddPassiveStatus(runtime, "E_Power", 1, "Rune_02");
        }

        if (IsCharacter(runtime, "Char_02") &&
            HasRune(runtime, "Rune_08") &&
            resource >= 5)
        {
            AddPassiveStatus(runtime, "E_Power", 1, "Rune_08");
        }

        if (IsCharacter(runtime, "Char_03") &&
            HasRune(runtime, "Rune_12") &&
            resource >= 3)
        {
            runtime.CurrentShield += ModifyArmorGain(runtime, 2);
        }

        if (IsCharacter(runtime, "Char_03") &&
            HasRune(runtime, "Rune_13") &&
            resource >= 3)
        {
            AddPassiveStatus(runtime, "E_Power", 1, "Rune_13");
        }
    }

    public static int ModifyPlayerEffectValue(
        CharacterRuntimeData runtime,
        PlayerReservedCommand command,
        SkillEffectEntry entry,
        int baseValue)
    {
        int value = Mathf.Max(0, baseValue);

        if (runtime == null || command == null || entry == null)
            return value;

        if (entry.EffectId == "E_Armor")
            value = ModifyArmorGain(runtime, value);

        if (IsDamageEffect(entry.EffectId) &&
            IsCharacter(runtime, "Char_02") &&
            HasRune(runtime, "Rune_09"))
        {
            value += 2;
        }

        if (IsLastTimelineSlot(command) &&
            HasRelic(runtime, "Relic_03") &&
            command.SkillData != null &&
            command.SkillData.SkillType == SkillType.Attack)
        {
            value += 1;
        }

        if (command.TimelineSlotIndex == 0 &&
            HasRelic(runtime, "Relic_04") &&
            command.SkillData != null &&
            command.SkillData.SkillType == SkillType.Skill)
        {
            value += 1;
        }

        return value;
    }

    public static int ModifyPlayerEffectCount(
        CharacterRuntimeData runtime,
        PlayerReservedCommand command,
        SkillEffectEntry entry,
        int baseCount)
    {
        int count = Mathf.Max(0, baseCount);

        if (runtime == null || command == null || entry == null)
            return count;

        if (!IsDamageEffect(entry.EffectId))
            return count;

        if (command.SkillData == null || command.SkillData.SkillType != SkillType.Attack)
            return count;

        if (HasRelic(runtime, "Relic_07") &&
            IsRelic07SelectedAttackSkill(runtime, command.SkillId))
        {
            count += 1;
        }

        return count;
    }

    public static string GetEffectiveRangeId(
        CharacterRuntimeData runtime,
        SkillMasterData skillData)
    {
        if (skillData == null)
            return string.Empty;

        if (IsCharacter(runtime, "Char_03") &&
            HasRune(runtime, "Rune_14") &&
            skillData.RangeId == "Range_21")
        {
            return "Range_18";
        }

        return skillData.RangeId;
    }

    public static bool HasRune(CharacterRuntimeData runtime, string runeId)
    {
        if (runtime == null || runtime.EquippedRuneIds == null || string.IsNullOrWhiteSpace(runeId))
            return false;

        for (int i = 0; i < runtime.EquippedRuneIds.Length; i++)
        {
            if (runtime.EquippedRuneIds[i] == runeId)
                return true;
        }

        return false;
    }

    public static bool HasRelic(CharacterRuntimeData runtime, string relicId)
    {
        if (runtime == null || runtime.EquippedRelicIds == null || string.IsNullOrWhiteSpace(relicId))
            return false;

        for (int i = 0; i < runtime.EquippedRelicIds.Length; i++)
        {
            if (runtime.EquippedRelicIds[i] == relicId)
                return true;
        }

        return false;
    }

    public static bool IsMoveCommand(PlayerReservedCommand command)
    {
        if (command == null)
            return false;

        if (command.ReservedMoveGridIndex >= 0)
            return true;

        SkillMasterData skillData = command.SkillData;

        if (skillData == null)
            return false;

        return skillData.Category == Category.Move;
    }

    private static int GetMaxHPBonus(CharacterRuntimeData runtime)
    {
        int bonus = 0;

        if (HasRune(runtime, "Rune_16"))
            bonus += 3;

        if (HasRune(runtime, "Rune_18"))
            bonus += 5;

        if (HasRune(runtime, "Rune_22"))
            bonus += 7;

        if (HasRune(runtime, "Rune_25"))
            bonus += 3;

        if (HasRelic(runtime, "Relic_09"))
            bonus += 5;

        return bonus;
    }

    private static int GetMaxCostBonus(CharacterRuntimeData runtime)
    {
        int bonus = 0;

        if (HasRune(runtime, "Rune_20"))
            bonus += 1;

        if (HasRune(runtime, "Rune_21"))
            bonus += 2;

        if (HasRune(runtime, "Rune_25"))
            bonus += 1;

        if (HasRelic(runtime, "Relic_08"))
            bonus += 1;

        return bonus;
    }

    private static int GetMoveValueBonus(CharacterRuntimeData runtime)
    {
        int bonus = 0;

        if (HasRune(runtime, "Rune_17"))
            bonus += 3;

        if (HasRune(runtime, "Rune_19"))
            bonus += 5;

        if (HasRune(runtime, "Rune_23"))
            bonus += 7;

        if (HasRune(runtime, "Rune_25"))
            bonus += 3;

        if (HasRelic(runtime, "Relic_10"))
            bonus += 8;

        return bonus;
    }

    private static int GetBattleStartCostBonus(CharacterRuntimeData runtime)
    {
        return HasRelic(runtime, "Relic_01") ? 2 : 0;
    }

    private static int GetBattleStartUniqueResource(
        CharacterRuntimeData runtime,
        CharacterMasterData masterData)
    {
        int resource = 0;

        if (IsCharacter(runtime, "Char_01") && HasRune(runtime, "Rune_01"))
            resource += 3;

        if (IsCharacter(runtime, "Char_02") && HasRune(runtime, "Rune_06"))
            resource += 3;

        if (IsCharacter(runtime, "Char_03") && HasRune(runtime, "Rune_11"))
            resource += 3;

        int maxResource = masterData != null
            ? Mathf.Max(0, masterData.MaxResource)
            : Mathf.Max(0, resource);

        return Mathf.Clamp(resource, 0, maxResource);
    }

    private static int ModifyArmorGain(CharacterRuntimeData runtime, int baseValue)
    {
        int value = Mathf.Max(0, baseValue);

        if (IsCharacter(runtime, "Char_01") && HasRune(runtime, "Rune_04"))
            value += 1;

        return value;
    }

    private static void AddPassiveStatus(
        CharacterRuntimeData runtime,
        string effectId,
        int stack,
        string sourceId)
    {
        if (runtime == null || string.IsNullOrWhiteSpace(effectId) || stack <= 0)
            return;

        if (runtime.StatusEffects == null)
            runtime.StatusEffects = new List<StatusEffectRuntimeData>();

        runtime.StatusEffects.Add(new StatusEffectRuntimeData
        {
            EffectId = effectId,
            Stack = stack,
            TurnCount = 1,
            IsPassive = true,
            SourceSkillId = sourceId
        });
    }

    private static bool IsRelic07SelectedAttackSkill(
        CharacterRuntimeData runtime,
        string skillId)
    {
        if (runtime == null || string.IsNullOrWhiteSpace(skillId))
            return false;

        List<string> attackSkillIds = GetEquippedAttackSkillIds(runtime);

        if (attackSkillIds.Count <= 0)
            return true;

        attackSkillIds.Sort(System.StringComparer.Ordinal);

        int selectedIndex = GetStableHash(runtime.CharacterId) % attackSkillIds.Count;
        return attackSkillIds[selectedIndex] == skillId;
    }

    private static List<string> GetEquippedAttackSkillIds(CharacterRuntimeData runtime)
    {
        List<string> result = new();

        AddAttackSkillId(result, runtime.UniqueSkillId);
        AddAttackSkillId(result, runtime.AbilitySkillId);

        if (runtime.EquippedSkillIds != null)
        {
            for (int i = 0; i < runtime.EquippedSkillIds.Length; i++)
                AddAttackSkillId(result, runtime.EquippedSkillIds[i]);
        }

        return result;
    }

    private static void AddAttackSkillId(List<string> result, string skillId)
    {
        if (result == null || string.IsNullOrWhiteSpace(skillId))
            return;

        if (result.Contains(skillId))
            return;

        if (DataManager.Instance == null || DataManager.Instance.SkillDatabase == null)
        {
            result.Add(skillId);
            return;
        }

        SkillMasterData skillData = DataManager.Instance.SkillDatabase.Get(skillId);

        if (skillData != null && skillData.SkillType == SkillType.Attack)
            result.Add(skillId);
    }

    private static int GetStableHash(string text)
    {
        unchecked
        {
            int hash = 23;

            if (!string.IsNullOrEmpty(text))
            {
                for (int i = 0; i < text.Length; i++)
                    hash = hash * 31 + text[i];
            }

            return Mathf.Abs(hash);
        }
    }

    private static bool IsDamageEffect(string effectId)
    {
        return effectId == "E_Strike" || effectId == "E_Pierce";
    }

    private static bool IsLastTimelineSlot(PlayerReservedCommand command)
    {
        return command != null && command.TimelineSlotIndex == LastTimelineSlotIndex;
    }

    private static bool IsCharacter(CharacterRuntimeData runtime, string characterId)
    {
        return runtime != null && runtime.CharacterId == characterId;
    }

    private static bool TryMarkBattleEffectApplied(
        CharacterRuntimeData runtime,
        string effectId)
    {
        if (runtime == null || string.IsNullOrWhiteSpace(effectId))
            return false;

        if (runtime.AppliedBattleEquipmentEffectIds == null)
            runtime.AppliedBattleEquipmentEffectIds = new List<string>();

        if (runtime.AppliedBattleEquipmentEffectIds.Contains(effectId))
            return false;

        runtime.AppliedBattleEquipmentEffectIds.Add(effectId);
        return true;
    }

    private static void ReduceFirstPositiveCost(
        ref int hpCost,
        ref int cost,
        ref int resourceCost,
        ref int moveCost,
        ref int shieldCost)
    {
        if (cost > 0)
        {
            cost -= 1;
            return;
        }

        if (resourceCost > 0)
        {
            resourceCost -= 1;
            return;
        }

        if (moveCost > 0)
        {
            moveCost -= 1;
            return;
        }

        if (shieldCost > 0)
        {
            shieldCost -= 1;
            return;
        }

        if (hpCost > 0)
            hpCost -= 1;
    }
}
