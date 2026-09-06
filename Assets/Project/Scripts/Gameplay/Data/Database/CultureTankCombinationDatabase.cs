using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    [CreateAssetMenu(menuName = "Relic/Data/Culture Tank Combination Database")]
    public sealed class CultureTankCombinationDatabase : ScriptableObject
    {
        [SerializeField] private List<CultureTankCombinationEntry> entries = new();

        private Dictionary<string, CultureTankCombinationEntry> byKey;
        private Dictionary<string, CultureTankCombinationEntry> byId;

        public static CultureTankCombinationDatabase CreateRuntime(IEnumerable<CultureTankCombinationEntry> source)
        {
            CultureTankCombinationDatabase database = CreateInstance<CultureTankCombinationDatabase>();
            if (source != null)
                database.entries.AddRange(source);
            database.Initialize();
            return database;
        }

        public void Initialize()
        {
            byKey = new Dictionary<string, CultureTankCombinationEntry>(StringComparer.Ordinal);
            byId = new Dictionary<string, CultureTankCombinationEntry>(StringComparer.Ordinal);

            foreach (CultureTankCombinationEntry entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.CombinationId))
                    continue;

                entry.CombinationId = entry.CombinationId.Trim();
                entry.ValueRate = Mathf.Max(0, entry.ValueRate);
                entry.CountRate = Mathf.Max(0, entry.CountRate);
                entry.RemainingBattleStarts = Mathf.Max(1, entry.RemainingBattleStarts);
                string key = NormalizeKey(entry.TypeA, entry.TypeB, entry.TypeC);
                if (key.Length != 3)
                    continue;

                byKey[key] = entry;
                byId[entry.CombinationId] = entry;
            }
        }

        public bool TryGetByTypes(string first, string second, string third, out CultureTankCombinationEntry entry)
        {
            EnsureInitialized();
            return byKey.TryGetValue(NormalizeKey(first, second, third), out entry);
        }

        public bool TryGetById(string combinationId, out CultureTankCombinationEntry entry)
        {
            EnsureInitialized();
            string id = string.IsNullOrWhiteSpace(combinationId) ? string.Empty : combinationId.Trim();
            return byId.TryGetValue(id, out entry);
        }

        public static string NormalizeKey(string first, string second, string third)
        {
            string[] values = { NormalizeType(first), NormalizeType(second), NormalizeType(third) };
            if (Array.Exists(values, string.IsNullOrEmpty))
                return string.Empty;
            Array.Sort(values, StringComparer.Ordinal);
            return string.Concat(values);
        }

        private void EnsureInitialized()
        {
            if (byKey == null || byId == null)
                Initialize();
        }

        private static string NormalizeType(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            string normalized = value.Trim().ToUpperInvariant();
            return normalized is "A" or "B" or "C" ? normalized : string.Empty;
        }
    }

    [Serializable]
    public sealed class CultureTankCombinationEntry
    {
        public string CombinationId;
        public string TypeA;
        public string TypeB;
        public string TypeC;
        public Sprite ResultIcon;
        public string EffectId;
        public int ValueRate = 1;
        public int CountRate = 1;
        public int RemainingBattleStarts = CultureTankResearchService.DefaultBattleStartEffectUses;
    }
}
