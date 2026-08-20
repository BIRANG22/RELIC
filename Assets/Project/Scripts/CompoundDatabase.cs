using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class CompoundDatabase
    {
        private readonly List<CompoundData> compounds = new();
        private readonly Dictionary<string, CompoundData> byId = new(StringComparer.Ordinal);
        private readonly Dictionary<string, CompoundData> byRecipe = new(StringComparer.Ordinal);

        public void Initialize(IEnumerable<CompoundData> list)
        {
            compounds.Clear();
            byId.Clear();
            byRecipe.Clear();

            if (list == null)
                return;

            foreach (CompoundData data in list)
            {
                if (data == null || string.IsNullOrWhiteSpace(data.CompoundId))
                    continue;

                data.CompoundId = data.CompoundId.Trim();
                data.FragmentId = data.CompoundId;
                compounds.Add(data);
                byId[data.CompoundId] = data;

                string recipeKey = NormalizeRecipe(data.MaterialId1, data.MaterialId2, data.MaterialId3);
                if (!string.IsNullOrEmpty(recipeKey))
                    byRecipe[recipeKey] = data;
            }
        }

        public bool TryGet(string compoundId, out CompoundData data)
        {
            data = null;
            return !string.IsNullOrWhiteSpace(compoundId) &&
                   byId.TryGetValue(compoundId.Trim(), out data);
        }

        public CompoundData Get(string compoundId)
        {
            if (TryGet(compoundId, out CompoundData data))
                return data;

            Debug.LogWarning($"[CompoundDatabase] Compound ¾øÀ½: {compoundId}");
            return null;
        }

        public IReadOnlyList<CompoundData> GetAll() => compounds;

        public bool TryGetByMaterials(string first, string second, string third, out CompoundData data)
        {
            data = null;
            string key = NormalizeRecipe(first, second, third);
            return !string.IsNullOrEmpty(key) && byRecipe.TryGetValue(key, out data);
        }

        public static string NormalizeRecipe(string first, string second, string third)
        {
            string[] ids = { NormalizeId(first), NormalizeId(second), NormalizeId(third) };
            if (Array.Exists(ids, string.IsNullOrEmpty))
                return string.Empty;

            Array.Sort(ids, StringComparer.Ordinal);
            return string.Join("|", ids);
        }

        private static string NormalizeId(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
