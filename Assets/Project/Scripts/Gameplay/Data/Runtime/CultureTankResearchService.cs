using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public static class CultureTankResearchService
    {
        public const int DefaultBattleStartEffectUses = 3;
        public const int CurrentSchemaVersion = 1;

        public static bool TryPlaceIngredient(LobbyRuntimeData lobby, string tankId, string itemId, out string error)
        {
            error = string.Empty;
            if (!ValidateLobbyAndIds(lobby, tankId, itemId, out string slotId, out string ingredientId, out error))
                return false;

            Normalize(lobby);
            if (!string.IsNullOrEmpty(lobby.CompletedCultureTankCombinationId))
            {
                error = "Claim the completed combination first.";
                return false;
            }
            if (TryGetTank(lobby, slotId, out _))
            {
                error = "Culture tank slot is already occupied.";
                return false;
            }

            int bagIndex = lobby.BagItemIds.FindIndex(value => NormalizeId(value) == ingredientId);
            if (bagIndex < 0)
            {
                error = "Selected item does not exist in lobby bag.";
                return false;
            }

            lobby.BagItemIds.RemoveAt(bagIndex);
            lobby.CultureTankResearches.Add(new CultureTankResearchRuntimeData { TankId = slotId, ItemId = ingredientId });
            return true;
        }

        public static bool TryRemoveIngredient(LobbyRuntimeData lobby, string tankId, out string error)
        {
            error = string.Empty;
            if (lobby == null) { error = "Lobby runtime is missing."; return false; }
            Normalize(lobby);
            if (!TryGetTank(lobby, tankId, out CultureTankResearchRuntimeData slot))
            {
                error = "Culture tank slot is empty.";
                return false;
            }
            lobby.BagItemIds.Add(slot.ItemId);
            lobby.CultureTankResearches.Remove(slot);
            return true;
        }

        public static bool TryCombine(
            LobbyRuntimeData lobby,
            ItemDatabase itemDatabase,
            CultureTankCombinationDatabase combinationDatabase,
            out string combinationId,
            out string error)
        {
            combinationId = string.Empty;
            error = string.Empty;
            if (lobby == null || itemDatabase == null || combinationDatabase == null)
            {
                error = "Culture tank data is missing.";
                return false;
            }
            Normalize(lobby);
            if (!string.IsNullOrEmpty(lobby.CompletedCultureTankCombinationId))
            {
                error = "Claim the completed combination first.";
                return false;
            }
            if (lobby.CultureTankResearches.Count != 3)
            {
                error = "Three ingredients are required.";
                return false;
            }

            string[] types = new string[3];
            for (int i = 0; i < 3; i++)
            {
                ItemData item = itemDatabase.Get(lobby.CultureTankResearches[i].ItemId);
                if (item == null || string.IsNullOrWhiteSpace(item.CultureType))
                {
                    error = "Ingredient type data is missing.";
                    return false;
                }
                types[i] = item.CultureType;
            }

            if (!combinationDatabase.TryGetByTypes(types[0], types[1], types[2], out CultureTankCombinationEntry recipe))
            {
                error = "No culture tank recipe matches these ingredients.";
                return false;
            }

            combinationId = recipe.CombinationId;
            lobby.CultureTankResearches.Clear();
            lobby.CompletedCultureTankCombinationId = combinationId;
            return true;
        }

        public static bool TryClaimCompletedCombination(
            LobbyRuntimeData lobby,
            CultureTankCombinationDatabase combinationDatabase,
            out CultureTankBattleStartEffectRuntimeData effect,
            out string error)
        {
            effect = null;
            error = string.Empty;
            if (lobby == null || combinationDatabase == null)
            {
                error = "Culture tank data is missing.";
                return false;
            }
            Normalize(lobby);
            if (!combinationDatabase.TryGetById(lobby.CompletedCultureTankCombinationId, out CultureTankCombinationEntry recipe) ||
                string.IsNullOrWhiteSpace(recipe.EffectId))
            {
                error = "Completed combination data is missing.";
                return false;
            }

            effect = new CultureTankBattleStartEffectRuntimeData
            {
                SourceItemId = recipe.CombinationId,
                EffectId = recipe.EffectId.Trim(),
                Value = Mathf.Max(0, recipe.ValueRate),
                Count = Mathf.Max(0, recipe.CountRate),
                RemainingBattleStarts = Mathf.Max(1, recipe.RemainingBattleStarts)
            };
            lobby.PendingCultureTankBattleStartEffects.Add(effect);
            lobby.CompletedCultureTankCombinationId = string.Empty;
            return true;
        }

        public static bool TryGetTank(LobbyRuntimeData lobby, string tankId, out CultureTankResearchRuntimeData tank)
        {
            tank = null;
            if (lobby == null) return false;
            Normalize(lobby);
            string id = NormalizeId(tankId);
            tank = lobby.CultureTankResearches.Find(value => value != null && NormalizeId(value.TankId) == id);
            return tank != null;
        }

        public static List<CultureTankBattleStartEffectRuntimeData> CopyPendingBattleStartEffects(LobbyRuntimeData lobby)
        {
            var result = new List<CultureTankBattleStartEffectRuntimeData>();
            if (lobby == null) return result;
            Normalize(lobby);
            foreach (CultureTankBattleStartEffectRuntimeData source in lobby.PendingCultureTankBattleStartEffects)
            {
                CultureTankBattleStartEffectRuntimeData copy = CopyBattleStartEffect(source);
                if (copy != null) result.Add(copy);
            }
            return result;
        }

        public static CultureTankBattleStartEffectRuntimeData CopyBattleStartEffect(CultureTankBattleStartEffectRuntimeData source)
        {
            if (source == null || string.IsNullOrWhiteSpace(source.EffectId) || source.RemainingBattleStarts <= 0) return null;
            return new CultureTankBattleStartEffectRuntimeData
            {
                SourceItemId = NormalizeId(source.SourceItemId), EffectId = NormalizeId(source.EffectId),
                Value = Mathf.Max(0, source.Value), Count = Mathf.Max(0, source.Count),
                RemainingBattleStarts = Mathf.Max(0, source.RemainingBattleStarts)
            };
        }

        public static void Normalize(LobbyRuntimeData lobby)
        {
            if (lobby == null) return;
            lobby.BagItemIds ??= new List<string>();
            lobby.CultureTankResearches ??= new List<CultureTankResearchRuntimeData>();
            lobby.PendingCultureTankBattleStartEffects ??= new List<CultureTankBattleStartEffectRuntimeData>();
            lobby.CompletedCultureTankCombinationId = NormalizeId(lobby.CompletedCultureTankCombinationId);

            if (lobby.CultureTankCombinationSchemaVersion < CurrentSchemaVersion)
            {
                foreach (CultureTankResearchRuntimeData legacy in lobby.CultureTankResearches)
                    if (legacy != null && !string.IsNullOrWhiteSpace(legacy.ItemId))
                        lobby.BagItemIds.Add(legacy.ItemId.Trim());
                lobby.CultureTankResearches.Clear();
                lobby.CompletedCultureTankCombinationId = string.Empty;
                lobby.CultureTankCombinationSchemaVersion = CurrentSchemaVersion;
            }

            var usedSlots = new HashSet<string>(StringComparer.Ordinal);
            for (int i = lobby.CultureTankResearches.Count - 1; i >= 0; i--)
            {
                CultureTankResearchRuntimeData slot = lobby.CultureTankResearches[i];
                if (slot == null || string.IsNullOrWhiteSpace(slot.TankId) || string.IsNullOrWhiteSpace(slot.ItemId))
                { lobby.CultureTankResearches.RemoveAt(i); continue; }
                slot.TankId = NormalizeId(slot.TankId);
                slot.ItemId = NormalizeId(slot.ItemId);
                if (!usedSlots.Add(slot.TankId))
                { lobby.BagItemIds.Add(slot.ItemId); lobby.CultureTankResearches.RemoveAt(i); }
            }
            for (int i = lobby.PendingCultureTankBattleStartEffects.Count - 1; i >= 0; i--)
            {
                CultureTankBattleStartEffectRuntimeData copy = CopyBattleStartEffect(lobby.PendingCultureTankBattleStartEffects[i]);
                if (copy == null) lobby.PendingCultureTankBattleStartEffects.RemoveAt(i);
                else lobby.PendingCultureTankBattleStartEffects[i] = copy;
            }
        }

        [Obsolete("Use TryPlaceIngredient.")]
        public static bool TryStartResearch(LobbyRuntimeData lobby, string tankId, string itemId, long _, out string error) =>
            TryPlaceIngredient(lobby, tankId, itemId, out error);

        private static bool ValidateLobbyAndIds(LobbyRuntimeData lobby, string tankId, string itemId,
            out string slotId, out string ingredientId, out string error)
        {
            slotId = NormalizeId(tankId); ingredientId = NormalizeId(itemId); error = string.Empty;
            if (lobby == null) error = "Lobby runtime is missing.";
            else if (string.IsNullOrEmpty(slotId)) error = "TankId is required.";
            else if (string.IsNullOrEmpty(ingredientId)) error = "ItemId is required.";
            return string.IsNullOrEmpty(error);
        }

        private static string NormalizeId(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
