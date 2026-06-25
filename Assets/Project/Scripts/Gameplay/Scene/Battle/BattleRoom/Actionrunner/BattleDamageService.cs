using Relic.Gameplay.Data;
using UnityEngine;

public class BattleDamageService
{
    private readonly BattleUnitFinder unitFinder;

    public BattleDamageService(BattleUnitFinder unitFinder)
    {
        this.unitFinder = unitFinder;
    }

    public int GetPlayerDamage(PlayerReservedCommand command)
    {
        if (command == null || command.SkillData == null)
            return 1;

        int value = ParseFirstInt(command.SkillData.ValueRate);

        BattleCharacter attacker = unitFinder.FindBattleCharacter(command.CharacterId);

        if (attacker != null && attacker.RuntimeData != null)
            value += GetStatusStack(attacker.RuntimeData.StatusEffects, "E_Power");

        return Mathf.Max(1, value);
    }

    public int GetMonsterDamage(MonsterReservedCommand command)
    {
        if (command == null)
            return 1;

        int reservedDamage = command.EnsureReservedDamage();

        if (reservedDamage > 0)
            return reservedDamage;

        return RollMonsterDamage(command.SkillData);
    }

    public bool TryGetMonsterDamageRange(MonsterReservedCommand command, out int minDamage, out int maxDamage)
    {
        minDamage = 0;
        maxDamage = 0;

        if (command == null)
            return false;

        return TryGetMonsterDamageRange(command.SkillData, out minDamage, out maxDamage);
    }

    public static bool TryGetMonsterDamageRange(MonsterSkillData skillData, out int minDamage, out int maxDamage)
    {
        minDamage = 0;
        maxDamage = 0;

        if (skillData == null)
            return false;

        int baseDamage = ParseFirstIntValue(skillData.ValueRate);
        int randomRange = Mathf.Max(0, skillData.ValueRandomRange);

        minDamage = Mathf.Max(1, baseDamage - randomRange);
        maxDamage = Mathf.Max(minDamage, baseDamage + randomRange);
        return true;
    }

    public static int RollMonsterDamage(MonsterSkillData skillData)
    {
        if (!TryGetMonsterDamageRange(skillData, out int minDamage, out int maxDamage))
            return 1;

        return Random.Range(minDamage, maxDamage + 1);
    }

    public static string GetMonsterDamageText(MonsterReservedCommand command)
    {
        if (command == null || !ShouldReserveMonsterDamage(command.SkillData))
            return "";

        int damage = command.EnsureReservedDamage();

        return damage > 0
            ? damage.ToString()
            : "";
    }

    public static bool ShouldReserveMonsterDamage(MonsterSkillData skillData)
    {
        if (skillData == null)
            return false;

        if (HasMonsterDamageHitEffect(skillData))
            return true;

        return skillData.TimelineNotation == TimelineActionType.Attack;
    }

    public static bool HasMonsterDamageHitEffect(MonsterSkillData skillData)
    {
        if (skillData == null || string.IsNullOrWhiteSpace(skillData.EffectIds))
            return false;

        string[] effectIds = skillData.EffectIds.Split(';');

        for (int i = 0; i < effectIds.Length; i++)
        {
            if (IsMonsterDamageHitEffect(effectIds[i]))
                return true;
        }

        return false;
    }

    public static bool IsMonsterDamageHitEffect(string effectId)
    {
        if (string.IsNullOrWhiteSpace(effectId))
            return false;

        string trimmedEffectId = effectId.Trim();
        return trimmedEffectId == "E_Strike" || trimmedEffectId == "E_Pierce";
    }

    public static string GetMonsterDamageRangeText(MonsterSkillData skillData)
    {
        if (!TryGetMonsterDamageRange(skillData, out int minDamage, out int maxDamage))
            return "";

        if (minDamage == maxDamage)
            return minDamage.ToString();

        return $"{minDamage}-{maxDamage}";
    }

    public int GetStatusStack(
        System.Collections.Generic.List<StatusEffectRuntimeData> statusEffects,
        string effectId)
    {
        if (statusEffects == null)
            return 0;

        for (int i = 0; i < statusEffects.Count; i++)
        {
            if (statusEffects[i] == null)
                continue;

            if (statusEffects[i].EffectId == effectId)
                return statusEffects[i].Stack;
        }

        return 0;
    }

    public int ParseFirstInt(string text)
    {
        return ParseFirstIntValue(text);
    }

    private static int ParseFirstIntValue(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 1;

        string number = "";

        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsDigit(text[i]) || text[i] == '-')
                number += text[i];
            else if (!string.IsNullOrEmpty(number))
                break;
        }

        if (int.TryParse(number, out int result))
            return result;

        return 1;
    }
}
