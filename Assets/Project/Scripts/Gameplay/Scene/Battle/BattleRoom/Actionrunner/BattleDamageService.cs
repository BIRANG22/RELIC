using System.Collections.Generic;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleDamageService
{
    private const string NocturnGrabSkillId = "S_Monster_19";
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
        {
            value += GetStatusStack(attacker.RuntimeData.StatusEffects, "E_Boost");

            if (command.SkillData.SkillType == SkillType.Attack &&
                GetStatusStack(attacker.RuntimeData.StatusEffects, "E_Smite") > 0)
            {
                value = Mathf.CeilToInt(value * 1.5f);
            }
        }

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

        return BattleRandom.Range(minDamage, maxDamage + 1);
    }

    public static string GetMonsterDamageText(MonsterReservedCommand command)
    {
        if (!TryGetMonsterDamageDisplayValues(command, out int baseDamage, out int additiveDamage))
            return "";

        // 팝업 설명에는 기본 피해와 확정 정수 추가 피해를 분리해 표시합니다.
        // 취약, 약화, 기습처럼 실행 결과에 따라 달라지는 배율형 효과는 포함하지 않습니다.
        if (additiveDamage > 0)
            return $"{baseDamage}(+{additiveDamage})";

        return baseDamage.ToString();
    }

    public static string GetMonsterDamageTotalText(MonsterReservedCommand command)
    {
        if (!TryGetMonsterDamageDisplayValues(command, out int baseDamage, out int additiveDamage))
            return "";

        // 타임라인 슬롯의 짧은 수치 표시는 확정된 정수 피해를 합산해 표시합니다.
        return (baseDamage + additiveDamage).ToString();
    }

    public static int CalculateFinalMonsterDamageToPlayer(
        MonsterReservedCommand command,
        MonsterUnit caster,
        BattleCharacter target,
        int baseDamage)
    {
        BattleEffectContext context = new()
        {
            MonsterCaster = caster,
            PlayerTarget = target,
            MonsterSkillData = command?.SkillData
        };

        return BattleDamageModifierUtility.CalculateFinalDamageToPlayer(context, baseDamage);
    }

    public static int CalculateFinalMonsterDamageToMonster(
        MonsterReservedCommand command,
        MonsterUnit caster,
        MonsterUnit target,
        int baseDamage)
    {
        BattleEffectContext context = new()
        {
            MonsterCaster = caster,
            MonsterTarget = target,
            MonsterSkillData = command?.SkillData
        };

        return BattleDamageModifierUtility.CalculateFinalDamageToMonster(context, baseDamage);
    }

    public static bool ShouldReserveMonsterDamage(MonsterSkillData skillData)
    {
        if (skillData == null)
            return false;

        if (string.Equals(skillData.SkillId, NocturnGrabSkillId, System.StringComparison.Ordinal))
            return false;

        if (HasMonsterDamageHitEffect(skillData))
            return true;

        return skillData.TimelineNotation == TimelineActionType.Attack;
    }

    public static bool HasMonsterDamageHitEffect(MonsterSkillData skillData)
    {
        if (skillData == null || string.IsNullOrWhiteSpace(skillData.EffectIds))
            return false;

        if (string.Equals(skillData.SkillId, NocturnGrabSkillId, System.StringComparison.Ordinal))
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

    private static bool TryGetMonsterDamageDisplayValues(
        MonsterReservedCommand command,
        out int baseDamage,
        out int additiveDamage)
    {
        baseDamage = 0;
        additiveDamage = 0;

        if (command == null || !ShouldReserveMonsterDamage(command.SkillData))
            return false;

        baseDamage = command.EnsureReservedDamage();

        if (baseDamage <= 0)
            return false;

        MonsterUnit caster = FindMonsterUnit(command.RuntimeId);
        additiveDamage = caster != null && caster.RuntimeData != null
            ? GetStatusStackValue(caster.RuntimeData.StatusEffects, "E_Grudge")
            : 0;

        additiveDamage = Mathf.Max(0, additiveDamage);
        return true;
    }

    private static void AddPlayerTargetAdditiveValues(
        MonsterReservedCommand command,
        int attackerAddition,
        List<int> additions)
    {
        if (command == null || additions == null)
            return;

        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null || character.RuntimeData.IsDead)
                continue;

            if (command.TargetGridIndices == null ||
                !command.TargetGridIndices.Contains(character.CurrentGridIndex))
            {
                continue;
            }

            int targetAddition = GetStatusStackValue(
                character.RuntimeData.StatusEffects,
                "E_Corrosion");

            additions.Add(attackerAddition + targetAddition);
        }
    }

    private static void AddMonsterTargetAdditiveValues(
        MonsterReservedCommand command,
        MonsterUnit caster,
        int attackerAddition,
        List<int> additions)
    {
        if (command == null || additions == null)
            return;

        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null || monster == caster ||
                monster.RuntimeData == null || monster.RuntimeData.IsDead)
            {
                continue;
            }

            if (!OccupiesAnyTargetGrid(command, monster))
                continue;

            int targetAddition = GetStatusStackValue(
                monster.RuntimeData.StatusEffects,
                "E_Corrosion");

            additions.Add(attackerAddition + targetAddition);
        }
    }

    private static int GetStatusStackValue(
        List<StatusEffectRuntimeData> statusEffects,
        string effectId)
    {
        if (statusEffects == null || string.IsNullOrWhiteSpace(effectId))
            return 0;

        for (int i = 0; i < statusEffects.Count; i++)
        {
            StatusEffectRuntimeData status = statusEffects[i];

            if (status != null && status.EffectId == effectId)
                return Mathf.Max(0, status.Stack);
        }

        return 0;
    }

    private static void AddPlayerTargetDamageValues(
        MonsterReservedCommand command,
        MonsterUnit caster,
        int baseDamage,
        List<int> damageValues)
    {
        if (command == null || damageValues == null)
            return;

        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < characters.Length; i++)
        {
            BattleCharacter character = characters[i];

            if (character == null || character.RuntimeData == null || character.RuntimeData.IsDead)
                continue;

            if (command.TargetGridIndices == null ||
                !command.TargetGridIndices.Contains(character.CurrentGridIndex))
            {
                continue;
            }

            damageValues.Add(CalculateFinalMonsterDamageToPlayer(command, caster, character, baseDamage));
        }
    }

    private static void AddMonsterTargetDamageValues(
        MonsterReservedCommand command,
        MonsterUnit caster,
        int baseDamage,
        List<int> damageValues)
    {
        if (command == null || damageValues == null)
            return;

        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null || monster.RuntimeData == null || monster.RuntimeData.IsDead)
                continue;

            if (!OccupiesAnyTargetGrid(command, monster))
                continue;

            damageValues.Add(CalculateFinalMonsterDamageToMonster(command, caster, monster, baseDamage));
        }
    }

    private static bool OccupiesAnyTargetGrid(MonsterReservedCommand command, MonsterUnit monster)
    {
        if (command?.TargetGridIndices == null || monster == null)
            return false;

        for (int i = 0; i < monster.OccupiedGridIndices.Count; i++)
        {
            if (command.TargetGridIndices.Contains(monster.OccupiedGridIndices[i]))
                return true;
        }

        return false;
    }

    private static MonsterUnit FindMonsterUnit(string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId))
            return null;

        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null || monster.RuntimeData == null)
                continue;

            if (monster.RuntimeData.RuntimeId == runtimeId)
                return monster;
        }

        return null;
    }

    private static string FormatDamageValues(List<int> damageValues)
    {
        if (damageValues == null || damageValues.Count <= 0)
            return "";

        int minDamage = damageValues[0];
        int maxDamage = damageValues[0];

        for (int i = 1; i < damageValues.Count; i++)
        {
            minDamage = Mathf.Min(minDamage, damageValues[i]);
            maxDamage = Mathf.Max(maxDamage, damageValues[i]);
        }

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
