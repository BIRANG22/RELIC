using Relic.Gameplay.Data;
using UnityEngine;

public static class RangeParser
{
    public static void Parse(SkillRangeData data)
    {
        data.Positions.Clear();

        foreach (var raw in data.RangeRaw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var split = raw.Split(';');

            if (split.Length != 2)
                continue;

            if (int.TryParse(split[0], out int x) &&
                int.TryParse(split[1], out int y))
            {
                data.Positions.Add(new Vector2Int(x, y));
            }
        }
    }
}