using System;

namespace Relic.Gameplay.Data
{
    public enum ActiveRelicTargetMode
    {
        None,
        Self,
        Grid,
        AllyGrid
    }

    public static class ActiveRelicEffectResolver
    {
        private const string ExcelPlaceholderEffectId = "E_Value";

        public static bool IsActiveRelic(RelicData relic)
        {
            if (relic == null)
                return false;

            return string.Equals(
                relic.Type?.Trim(),
                "Active",
                StringComparison.OrdinalIgnoreCase);
        }

        public static string ResolveEffectId(RelicData relic)
        {
            if (relic == null)
                return string.Empty;

            string explicitEffectId = GetFirstEffectId(relic.EffectIds);

            if (!string.IsNullOrWhiteSpace(explicitEffectId) &&
                !string.Equals(explicitEffectId, ExcelPlaceholderEffectId, StringComparison.Ordinal))
            {
                return explicitEffectId;
            }

            return relic.FragmentId?.Trim() switch
            {
                "Relic_11" => ActiveRelicEffectIds.DamageBoostThisTurn,
                "Relic_12" => ActiveRelicEffectIds.DamageReductionThisTurn,
                "Relic_13" => ActiveRelicEffectIds.MoveToGrid,
                "Relic_14" => ActiveRelicEffectIds.SwapAlly,
                "Relic_15" => ActiveRelicEffectIds.SpawnGridEffect,
                _ => explicitEffectId ?? string.Empty
            };
        }

        public static ActiveRelicTargetMode ResolveTargetMode(RelicData relic)
        {
            return ResolveTargetMode(ResolveEffectId(relic));
        }

        public static ActiveRelicTargetMode ResolveTargetMode(string effectId)
        {
            return effectId?.Trim() switch
            {
                ActiveRelicEffectIds.DamageBoostThisTurn => ActiveRelicTargetMode.Self,
                ActiveRelicEffectIds.DamageReductionThisTurn => ActiveRelicTargetMode.Self,
                ActiveRelicEffectIds.MoveToGrid => ActiveRelicTargetMode.Grid,
                ActiveRelicEffectIds.SwapAlly => ActiveRelicTargetMode.AllyGrid,
                ActiveRelicEffectIds.SpawnGridEffect => ActiveRelicTargetMode.Grid,
                _ => ActiveRelicTargetMode.None
            };
        }

        private static string GetFirstEffectId(string effectIds)
        {
            if (string.IsNullOrWhiteSpace(effectIds))
                return string.Empty;

            string[] tokens = effectIds.Split(new[] { ';', '|', ',' }, StringSplitOptions.RemoveEmptyEntries);
            return tokens.Length > 0 ? tokens[0].Trim() : string.Empty;
        }
    }
}
