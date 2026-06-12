using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;

public static class SkillEffectParser
{
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
            skill.CountRate,
            effectDatabase
        );
    }

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
            data.CountRate,
            effectDatabase
        );
    }

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
            data.CountRate,
            effectDatabase
        );
    }

    public static List<SkillEffectEntry> Parse(
        RelicData relic,
        EffectDatabase effectDatabase)
    {
        if (relic == null)
            return new List<SkillEffectEntry>();

        return ParseInternal(
            relic.EffectIds,
            relic.ValueCalcTypes,
            relic.ValueRate,
            relic.CountRate,
            effectDatabase
        );
    }

    private static List<SkillEffectEntry> ParseInternal(
        string effectIdsStr,
        string valueTypesStr,
        string valuesStr,
        string countsStr,
        EffectDatabase effectDatabase)
    {
        List<SkillEffectEntry> result = new();

        if (string.IsNullOrWhiteSpace(effectIdsStr))
            return result;

        string[] effectIds = Split(effectIdsStr);
        string[] valueTypes = Split(valueTypesStr);
        string[] values = Split(valuesStr);
        string[] counts = Split(countsStr);

        for (int i = 0; i < effectIds.Length; i++)
        {
            string effectId = GetString(effectIds, i, "");

            if (string.IsNullOrWhiteSpace(effectId))
                continue;

            ValueCalcType valueCalcType = ParseEnum(
                GetByIndexOrFirst(valueTypes, i, "None"),
                ValueCalcType.None
            );

            int valueAmount = ParseInt(
                GetByIndexOrFirst(values, i, "0"),
                0
            );

            int countAmount = ParseInt(
                GetByIndexOrFirst(counts, i, "1"),
                1
            );

            // None 타입이면 수치는 무조건 0으로 보정
            if (valueCalcType == ValueCalcType.None)
                valueAmount = 0;

            SkillEffectEntry entry = new SkillEffectEntry
            {
                EffectId = effectId,
                ValueCalcType = valueCalcType,
                ValueAmount = valueAmount,
                CountAmount = countAmount,
                EffectData = effectDatabase != null ? effectDatabase.Get(effectId) : null
            };

            result.Add(entry);
        }

        return result;
    }

    private static string[] Split(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? Array.Empty<string>()
            : text.Split(';');
    }

    private static string GetString(string[] arr, int index, string defaultValue)
    {
        if (arr == null || index < 0 || index >= arr.Length)
            return defaultValue;

        if (string.IsNullOrWhiteSpace(arr[index]))
            return defaultValue;

        return arr[index].Trim();
    }

    private static string GetByIndexOrFirst(string[] arr, int index, string defaultValue)
    {
        if (arr == null || arr.Length <= 0)
            return defaultValue;

        if (index >= 0 && index < arr.Length && !string.IsNullOrWhiteSpace(arr[index]))
            return arr[index].Trim();

        if (!string.IsNullOrWhiteSpace(arr[0]))
            return arr[0].Trim();

        return defaultValue;
    }

    private static ValueCalcType ParseEnum(string text, ValueCalcType defaultValue)
    {
        if (string.IsNullOrWhiteSpace(text))
            return defaultValue;

        return Enum.TryParse(text.Trim(), true, out ValueCalcType value)
            ? value
            : defaultValue;
    }

    private static int ParseInt(string text, int defaultValue)
    {
        if (string.IsNullOrWhiteSpace(text))
            return defaultValue;

        return int.TryParse(text.Trim(), out int value)
            ? value
            : defaultValue;
    }
}