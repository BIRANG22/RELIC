using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;

public static class SkillEffectParser
{
    // Skill용
    public static List<SkillEffectEntry> Parse(
        SkillMasterData skill,
        EffectDatabase effectDatabase)
    {
        if (skill == null)
            return new List<SkillEffectEntry>();

        return ParseInternal(
            skill.EffectIds,
            skill.ValueCalcTypes,
            skill.ValueRate,
            skill.CountCalcTypes,
            skill.CountRate,
            effectDatabase
        );
    }

    // Fragment용
    public static List<SkillEffectEntry> Parse(
        FragmentData fragment,
        EffectDatabase effectDatabase)
    {
        if (fragment == null)
            return new List<SkillEffectEntry>();

        return ParseInternal(
            fragment.EffectIds,
            fragment.ValueCalcTypes,
            fragment.ValueRate,
            fragment.CountCalcTypes,
            fragment.CountRate,
            effectDatabase
        );
    }

    // 강화 스킬
    public static List<SkillEffectEntry> Parse(
    SkillEnhanceData data,
    EffectDatabase effectDatabase)
    {
        if (data == null)
            return new List<SkillEffectEntry>();

        return ParseInternal(
            data.EffectIds,
            data.ValueCalcTypes,
            data.ValueRate,
            data.CountCalcTypes,
            data.CountRate,
            effectDatabase
        );
    }

    // 몬스터 스킬
    public static List<SkillEffectEntry> Parse(
        MonsterSkillData data,
        EffectDatabase effectDatabase)
    {
        if (data == null)
            return new List<SkillEffectEntry>();

        return ParseInternal(
            data.EffectIds,
            data.ValueCalcTypes,
            data.ValueRate,
            data.CountCalcTypes,
            data.CountRate,
            effectDatabase
        );
    }


    // 룬
    public static List<SkillEffectEntry> Parse(
        RuneData data,
        EffectDatabase effectDatabase)
    {
        if (data == null)
            return new List<SkillEffectEntry>();

        return ParseInternal(
            data.EffectIds,
            data.ValueCalcTypes,
            data.ValueRate,
            data.CountCalcTypes,
            data.CountRate,
            effectDatabase
        );
    }

    // 공통 로직
    private static List<SkillEffectEntry> ParseInternal(
        string effectIdsStr,
        string valueTypesStr,
        string valuesStr,
        string countTypesStr,
        string countsStr,
        EffectDatabase effectDatabase)
    {
        List<SkillEffectEntry> result = new();

        if (string.IsNullOrEmpty(effectIdsStr))
            return result;

        string[] effectIds = Split(effectIdsStr);
        string[] valueTypes = Split(valueTypesStr);
        string[] values = Split(valuesStr);
        string[] countTypes = Split(countTypesStr);
        string[] counts = Split(countsStr);

        // 길이 검증
        if (effectIds.Length != valueTypes.Length ||
            effectIds.Length != values.Length ||
            effectIds.Length != countTypes.Length ||
            effectIds.Length != counts.Length)
        {
            UnityEngine.Debug.LogError(
                $"[SkillEffectParser] 길이 불일치\n" +
                $"EffectIds:{effectIds.Length}, ValueTypes:{valueTypes.Length}, Values:{values.Length}, CountTypes:{countTypes.Length}, Counts:{counts.Length}"
            );
        }

        for (int i = 0; i < effectIds.Length; i++)
        {
            var entry = new SkillEffectEntry
            {
                EffectId = effectIds[i],
                ValueCalcType = ParseEnum<ValueCalcType>(valueTypes, i),
                ValueAmount = ParseInt(values, i),
                CountCalcType = ParseEnum<ValueCalcType>(countTypes, i),
                CountAmount = ParseInt(counts, i),
                EffectData = effectDatabase.Get(effectIds[i])
            };

            result.Add(entry);
        }

        return result;
    }

    private static string[] Split(string text)
    {
        return string.IsNullOrEmpty(text)
            ? Array.Empty<string>()
            : text.Split(';');
    }

    private static T ParseEnum<T>(string[] arr, int index) where T : struct
    {
        if (index >= arr.Length)
            return default;

        return Enum.TryParse(arr[index], true, out T value)
            ? value
            : default;
    }

    private static int ParseInt(string[] arr, int index)
    {
        if (index >= arr.Length)
            return 0;

        return int.TryParse(arr[index], out int value)
            ? value
            : 0;
    }
}