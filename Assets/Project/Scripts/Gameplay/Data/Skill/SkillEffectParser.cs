using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;

public static class SkillEffectParser
{
    public static List<SkillEffectEntry> Parse(
        SkillMasterData skill,
        EffectDatabase effectDatabase)
    {
        List<SkillEffectEntry> result = new();

        if (skill == null || string.IsNullOrEmpty(skill.EffectIds))
            return result;

        string[] effectIds = Split(skill.EffectIds);
        string[] valueTypes = Split(skill.ValueCalcTypes);
        string[] values = Split(skill.ValueRate);
        string[] countTypes = Split(skill.CountCalcTypes);
        string[] counts = Split(skill.CountRate);

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