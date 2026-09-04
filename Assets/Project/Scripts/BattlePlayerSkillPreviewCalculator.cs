using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class BattleSkillNumberBreakdown
{
    public int FinalValue { get; }
    public string Formula { get; }

    public BattleSkillNumberBreakdown(int finalValue, string formula)
    {
        FinalValue = Mathf.Max(0, finalValue);
        Formula = string.IsNullOrWhiteSpace(formula) ? FinalValue.ToString() : formula;
    }
}

public sealed class BattlePlayerSkillPreview
{
    public PlayerReservedCommand Command { get; }
    public IReadOnlyList<int> EffectValues { get; }
    public IReadOnlyList<int> EffectCounts { get; }
    public IReadOnlyList<BattleSkillNumberBreakdown> EffectValueBreakdowns { get; }
    public IReadOnlyList<BattleSkillNumberBreakdown> EffectCountBreakdowns { get; }
    public int PayAmount { get; }

    public BattlePlayerSkillPreview(
        PlayerReservedCommand command,
        List<int> effectValues,
        List<int> effectCounts,
        int payAmount,
        List<BattleSkillNumberBreakdown> effectValueBreakdowns = null,
        List<BattleSkillNumberBreakdown> effectCountBreakdowns = null)
    {
        Command = command;
        EffectValues = effectValues ?? new List<int>();
        EffectCounts = effectCounts ?? new List<int>();
        EffectValueBreakdowns = effectValueBreakdowns ?? new List<BattleSkillNumberBreakdown>();
        EffectCountBreakdowns = effectCountBreakdowns ?? new List<BattleSkillNumberBreakdown>();
        PayAmount = Mathf.Max(0, payAmount);
    }
}

/// <summary>
/// 전투 중 스킬 UI와 타임라인이 실제 예약/실행 계산과 같은 값을 사용하도록 만드는 공통 미리보기 계산기입니다.
/// 계산 순서: GameData 기본값 + 파편 보정 -> 유물 보정(슬롯 조건 포함) -> 전투 중 상태 효과.
/// </summary>
public static class BattlePlayerSkillPreviewCalculator
{
    public static BattlePlayerSkillPreview CreatePreview(
        CharacterRuntimeData runtime,
        SkillMasterData skill,
        int slotIndex,
        BattleTimelineController timelineController)
    {
        if (skill == null)
            return new BattlePlayerSkillPreview(null, new List<int>(), new List<int>(), 0);

        PlayerReservedCommand command = new(runtime, skill);

        if (runtime != null)
        {
            if (timelineController != null && slotIndex >= 0)
            {
                timelineController.PreparePreviewCommandForReservation(slotIndex, command);
            }
            else
            {
                BattleEquipmentEffectService.ApplyReservationCostModifiers(
                    command,
                    slotIndex,
                    false,
                    slotIndex == 4,
                    true,
                    false,
                    0);
            }
        }

        return CreatePreview(command);
    }

    public static BattlePlayerSkillPreview CreatePreview(PlayerReservedCommand command)
    {
        SkillMasterData skill = command?.SkillData;
        CharacterRuntimeData runtime = command?.UserRuntime;

        if (skill == null)
            return new BattlePlayerSkillPreview(command, new List<int>(), new List<int>(), 0);

        List<SkillEffectEntry> entries = ResolveEntries(skill);
        List<int> values = new(entries.Count);
        List<int> counts = new(entries.Count);
        List<BattleSkillNumberBreakdown> valueBreakdowns = new(entries.Count);
        List<BattleSkillNumberBreakdown> countBreakdowns = new(entries.Count);

        for (int i = 0; i < entries.Count; i++)
        {
            SkillEffectEntry entry = entries[i];
            BattleSkillNumberBreakdown valueBreakdown = BuildEffectValueBreakdown(runtime, command, skill, entry);
            BattleSkillNumberBreakdown countBreakdown = BuildEffectCountBreakdown(runtime, command, entry);
            values.Add(valueBreakdown.FinalValue);
            counts.Add(countBreakdown.FinalValue);
            valueBreakdowns.Add(valueBreakdown);
            countBreakdowns.Add(countBreakdown);
        }

        return new BattlePlayerSkillPreview(
            command,
            values,
            counts,
            GetPayAmount(command),
            valueBreakdowns,
            countBreakdowns);
    }

    public static string GetTimelineValueText(BattlePlayerSkillPreview preview)
    {
        if (preview?.Command?.SkillData == null)
            return string.Empty;

        SkillMasterData skill = preview.Command.SkillData;
        List<SkillEffectEntry> entries = ResolveEntries(skill);
        int index = FindPreferredDisplayEntry(entries, skill.SkillType);

        if (index < 0 || index >= preview.EffectValues.Count)
            return string.Empty;

        int value = preview.EffectValues[index];
        if (value <= 0)
            return string.Empty;

        int count = index < preview.EffectCounts.Count
            ? Mathf.Max(1, preview.EffectCounts[index])
            : 1;

        return count > 1 ? $"{value}x{count}" : value.ToString();
    }

    public static string FormatDescription(
        SkillMasterData skill,
        string localizedDescription,
        BattlePlayerSkillPreview preview,
        bool detailedMode = false,
        string hoveredLinkId = null)
    {
        if (skill == null || string.IsNullOrWhiteSpace(localizedDescription))
            return string.Empty;

        if (preview == null)
            return SkillDescriptionFormatter.Format(localizedDescription, skill.ValueRate, skill.CountRate);

        List<SkillEffectEntry> entries = ResolveEntries(skill);
        string description = localizedDescription;
        string[] valueTexts = new string[preview.EffectValues.Count];
        string[] countTexts = new string[preview.EffectCounts.Count];

        for (int i = 0; i < valueTexts.Length; i++)
        {
            int value = preview.EffectValues[i];
            BattleSkillNumberBreakdown breakdown = i < preview.EffectValueBreakdowns.Count
                ? preview.EffectValueBreakdowns[i]
                : null;
            valueTexts[i] = BuildNumericLink(
                $"value_{i}",
                value,
                breakdown?.Formula,
                detailedMode,
                hoveredLinkId);
        }

        for (int i = 0; i < countTexts.Length; i++)
        {
            int count = Mathf.Max(1, preview.EffectCounts[i]);
            BattleSkillNumberBreakdown breakdown = i < preview.EffectCountBreakdowns.Count
                ? preview.EffectCountBreakdowns[i]
                : null;
            countTexts[i] = BuildNumericLink(
                $"count_{i}",
                count,
                breakdown?.Formula,
                detailedMode,
                hoveredLinkId);
        }

        // 원문에 횟수 토큰이 없는 스킬에서 횟수가 2회 이상으로 증가하면
        // ValueRate 위치에 8X2 같은 형태로 표시합니다. 상세 모드에서는 각 수치의 계산식도 함께 표시합니다.
        for (int i = 0; i < entries.Count && i < preview.EffectValues.Count; i++)
        {
            SkillEffectEntry entry = entries[i];
            if (entry == null || !ShouldShowValue(entry.EffectId, skill.SkillType))
                continue;

            int count = i < preview.EffectCounts.Count ? Mathf.Max(1, preview.EffectCounts[i]) : 1;
            if (count <= 1)
                continue;

            string indexedCountToken = $"{{CountRate{i + 1}}}";
            bool hasCountToken = description.Contains(indexedCountToken) ||
                                 (i == 0 && description.Contains("{CountRate}"));
            if (hasCountToken)
                continue;

            string indexedValueToken = $"{{ValueRate{i + 1}}}";
            string repeated = $"{valueTexts[i]}X{countTexts[i]}";

            if (description.Contains(indexedValueToken))
                description = description.Replace(indexedValueToken, repeated);
            else if (i == 0 && description.Contains("{ValueRate}"))
                description = description.Replace("{ValueRate}", repeated);
        }

        return SkillDescriptionFormatter.Format(
            description,
            string.Join(";", valueTexts),
            string.Join(";", countTexts));
    }

    private static string BuildNumericLink(
        string linkId,
        int finalValue,
        string formula,
        bool detailedMode,
        string hoveredLinkId)
    {
        string display = finalValue.ToString();
        if (detailedMode && !string.IsNullOrWhiteSpace(formula) && formula != display)
            display = $"{display}({formula})";

        if (linkId == hoveredLinkId || hoveredLinkId == SkillDetailNumericLinkHandler.AllLinksHoverId)
            display = $"<size={SkillDetailNumericLinkHandler.HoverScalePercent}%>{display}</size>"; // Skill_Details Hover 시 숫자 1.2배

        return $"<link=\"{linkId}\"><color=#FFEB04>{display}</color></link>";
    }

    private static BattleSkillNumberBreakdown BuildEffectValueBreakdown(
        CharacterRuntimeData runtime,
        PlayerReservedCommand command,
        SkillMasterData skill,
        SkillEffectEntry entry)
    {
        if (entry == null)
            return new BattleSkillNumberBreakdown(0, "0");

        int baseValue = Mathf.Max(0, entry.ValueAmount);
        int equipmentValue;

        if (entry.EffectId == "E_Knockback")
        {
            equipmentValue = BattleEquipmentEffectService.ModifyPlayerKnockbackValue(
                runtime, command, entry, entry.ValueAmount);
        }
        else if (entry.EffectId == "E_Move" || entry.EffectId == "E_Grab")
        {
            equipmentValue = entry.ValueAmount;
        }
        else
        {
            equipmentValue = BattleEquipmentEffectService.ModifyPlayerEffectValue(
                runtime, command, entry, entry.ValueAmount);
        }

        List<string> formulaParts = new() { baseValue.ToString() };
        AppendDelta(formulaParts, equipmentValue - baseValue);

        int finalValue = Mathf.Max(0, equipmentValue);
        if (IsDamageEffect(entry.EffectId))
        {
            int smiteAttacksAlreadyReserved = command != null
                ? command.EarlierAttackReservationCount
                : 0;

            AppendDamageStatusFormulaParts(
                formulaParts,
                runtime,
                skill != null && skill.SkillType == SkillType.Attack,
                smiteAttacksAlreadyReserved);

            finalValue = BattleDamageModifierUtility.ApplyPlayerAttackerModifiersInStatusOrder(
                finalValue,
                runtime,
                skill != null && skill.SkillType == SkillType.Attack,
                smiteAttacksAlreadyReserved);
        }

        return new BattleSkillNumberBreakdown(finalValue, string.Join(string.Empty, formulaParts));
    }

    private static BattleSkillNumberBreakdown BuildEffectCountBreakdown(
        CharacterRuntimeData runtime,
        PlayerReservedCommand command,
        SkillEffectEntry entry)
    {
        if (entry == null)
            return new BattleSkillNumberBreakdown(1, "1");

        int baseCount = Mathf.Max(1, entry.CountAmount);
        int finalCount = CalculateEffectCount(runtime, command, entry);
        List<string> formulaParts = new() { baseCount.ToString() };
        AppendDelta(formulaParts, finalCount - baseCount);
        return new BattleSkillNumberBreakdown(finalCount, string.Join(string.Empty, formulaParts));
    }

    private static void AppendDelta(List<string> formulaParts, int delta)
    {
        if (delta > 0)
            formulaParts.Add($"+{delta}");
        else if (delta < 0)
            formulaParts.Add(delta.ToString());
    }

    private static void AppendDamageStatusFormulaParts(
        List<string> formulaParts,
        CharacterRuntimeData runtime,
        bool isAttackSkill,
        int smiteAttacksAlreadyReserved)
    {
        if (runtime?.StatusEffects == null)
            return;

        for (int i = 0; i < runtime.StatusEffects.Count; i++)
        {
            StatusEffectRuntimeData status = runtime.StatusEffects[i];
            if (status == null || status.Stack <= 0)
                continue;

            switch (status.EffectId)
            {
                case "E_Boost":
                    formulaParts.Add($"+{status.Stack}");
                    break;
                case "E_Smite":
                    if (isAttackSkill && smiteAttacksAlreadyReserved < status.Stack)
                        formulaParts.Add("+50%"); // 예: 13(6+3+50%)
                    break;
                case "E_Weaken":
                    formulaParts.Add("-15%");
                    break;
                case ActiveRelicEffectIds.DamageBoostThisTurn:
                    formulaParts.Add("+100%");
                    break;
                case ActiveRelicEffectIds.TargetOutgoingDamageReductionThisTurn:
                    formulaParts.Add("-50%");
                    break;
                case "E_Move_First_Attack_Power":
                    if (BattleEquipmentEffectService.IsMoveFirstAttackPowerReady(runtime))
                        formulaParts.Add("+20%");
                    break;
                case "E_Low_HP_Power":
                    if (runtime.MaxHP > 0)
                    {
                        float missingRatio = 1f - Mathf.Clamp01(runtime.CurrentHP / (float)runtime.MaxHP);
                        float percent = missingRatio * status.Stack;
                        if (percent > 0f)
                            formulaParts.Add($"+{percent:0.#}%");
                    }
                    break;
                case "E_Grudge":
                    formulaParts.Add($"+{status.Stack}");
                    break;
            }
        }
    }

    private static int CalculateEffectValue(
        CharacterRuntimeData runtime,
        PlayerReservedCommand command,
        SkillMasterData skill,
        SkillEffectEntry entry)
    {
        if (entry == null)
            return 0;

        if (entry.EffectId == "E_Knockback")
        {
            return BattleEquipmentEffectService.ModifyPlayerKnockbackValue(
                runtime,
                command,
                entry,
                entry.ValueAmount);
        }

        if (entry.EffectId == "E_Move" || entry.EffectId == "E_Grab")
            return entry.ValueAmount;

        int value = BattleEquipmentEffectService.ModifyPlayerEffectValue(
            runtime,
            command,
            entry,
            entry.ValueAmount);

        if (IsDamageEffect(entry.EffectId))
        {
            int smiteAttacksAlreadyReserved = command != null
                ? command.EarlierAttackReservationCount
                : 0;

            value = BattleDamageModifierUtility.ApplyPlayerAttackerModifiersInStatusOrder(
                value,
                runtime,
                skill != null && skill.SkillType == SkillType.Attack,
                smiteAttacksAlreadyReserved);
        }

        return Mathf.Max(0, value);
    }

    private static int CalculateEffectCount(
        CharacterRuntimeData runtime,
        PlayerReservedCommand command,
        SkillEffectEntry entry)
    {
        if (entry == null)
            return 1;

        return Mathf.Max(
            1,
            BattleEquipmentEffectService.ModifyPlayerEffectCount(
                runtime,
                command,
                entry,
                entry.CountAmount));
    }

    private static int GetPayAmount(PlayerReservedCommand command)
    {
        if (command?.SkillData == null)
            return 0;

        return command.SkillData.ReferenceResource switch
        {
            ReferenceResource.HP => command.HPCost,
            ReferenceResource.Cost => command.Cost,
            ReferenceResource.MovePoint => command.Cost,
            ReferenceResource.UniqueResource => command.ResourceCost,
            _ => 0
        };
    }

    private static List<SkillEffectEntry> ResolveEntries(SkillMasterData skill)
    {
        if (skill == null)
            return new List<SkillEffectEntry>();

        if (skill.EffectEntries != null && skill.EffectEntries.Count > 0)
            return skill.EffectEntries;

        return SkillEffectParser.Parse(
            skill,
            DataManager.Instance != null ? DataManager.Instance.EffectDatabase : null);
    }

    private static int FindPreferredDisplayEntry(List<SkillEffectEntry> entries, SkillType skillType)
    {
        if (entries == null)
            return -1;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && IsDamageEffect(entries[i].EffectId))
                return i;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            SkillEffectEntry entry = entries[i];
            if (entry != null && ShouldShowValue(entry.EffectId, skillType))
                return i;
        }

        return -1;
    }

    private static bool IsDamageEffect(string effectId)
    {
        return effectId == "E_Strike" || effectId == "E_Pierce";
    }

    private static bool ShouldShowValue(string effectId, SkillType skillType)
    {
        switch (effectId)
        {
            case "E_Heal":
            case "E_Armor":
            case "E_Boost":
            case "E_Charge":
            case "E_Focus":
            case "E_Ward":
            case "E_Swift":
            case "E_Smite":
            case "E_Lifesteal":
            case "E_Poison":
            case "E_Bleed":
            case "E_Weaken":
            case "E_Vulnerable":
                return true;
            default:
                return false;
        }
    }

}
