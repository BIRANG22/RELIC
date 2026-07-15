using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [CreateAssetMenu(menuName = "Relic/Data/Rune Icon Database")]
    public class RuneIconDatabase : ScriptableObject
    {
        [SerializeField] private List<RuneIconEntry> entries = new();

        private Dictionary<string, Sprite> map;

        public void Initialize()
        {
            map = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.RuneId) || entry.Icon == null)
                    continue;

                RegisterAliases(entry.RuneId, entry.Icon);
            }
        }

        public bool TryGetIcon(string runeId, out Sprite icon)
        {
            icon = null;

            if (map == null)
                Initialize();

            if (string.IsNullOrWhiteSpace(runeId))
                return false;

            foreach (string candidate in GetIdCandidates(runeId))
            {
                if (map.TryGetValue(candidate, out icon) && icon != null)
                    return true;
            }

            return false;
        }

        private void RegisterAliases(string runeId, Sprite icon)
        {
            foreach (string candidate in GetIdCandidates(runeId))
                map[candidate] = icon;
        }

        private IEnumerable<string> GetIdCandidates(string runeId)
        {
            string trimmed = runeId.Trim();
            yield return trimmed;

            int runeNumber = GetTrailingNumber(trimmed);
            if (runeNumber < 0)
                yield break;

            yield return runeNumber.ToString();
            yield return $"Rune_{runeNumber}";
            yield return $"Rune_{runeNumber:00}";
            yield return $"RUNE_{runeNumber}";
            yield return $"RUNE_{runeNumber:00}";
        }

        private int GetTrailingNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return -1;

            int end = value.Length - 1;
            while (end >= 0 && char.IsWhiteSpace(value[end]))
                end--;

            if (end < 0 || !char.IsDigit(value[end]))
                return -1;

            int start = end;
            while (start >= 0 && char.IsDigit(value[start]))
                start--;

            string numberText = value.Substring(start + 1, end - start);
            return int.TryParse(numberText, out int number) ? number : -1;
        }
    }

    [Serializable]
    public class RuneIconEntry
    {
        public string RuneId;
        public Sprite Icon;
    }
}
