using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public static class CultureTankResearchService
    {
        public const int DefaultResearchDurationSeconds = 150;
        public const int DefaultBattleStartEffectUses = 3;

        public static bool TryStartResearch(
            LobbyRuntimeData lobby,
            string tankId,
            string itemId,
            long startedAtUtcTicks,
            out string error)
        {
            error = string.Empty;

            if (lobby == null)
            {
                error = "Lobby runtime is missing.";
                return false;
            }

            string normalizedTankId = NormalizeId(tankId);
            string normalizedItemId = NormalizeId(itemId);

            if (string.IsNullOrEmpty(normalizedTankId))
            {
                error = "TankId is required.";
                return false;
            }

            if (string.IsNullOrEmpty(normalizedItemId))
            {
                error = "ItemId is required.";
                return false;
            }

            Normalize(lobby);

            if (TryGetTank(lobby, normalizedTankId, out _))
            {
                error = "Culture tank is already occupied.";
                return false;
            }

            int bagIndex = lobby.BagItemIds.FindIndex(value =>
                string.Equals(NormalizeId(value), normalizedItemId, StringComparison.Ordinal));

            if (bagIndex < 0)
            {
                error = "Selected item does not exist in lobby bag.";
                return false;
            }

            lobby.BagItemIds.RemoveAt(bagIndex);
            lobby.CultureTankResearches.Add(new CultureTankResearchRuntimeData
            {
                TankId = normalizedTankId,
                ItemId = normalizedItemId,
                StartedAtUtcTicks = Math.Max(0L, startedAtUtcTicks),
                DurationSeconds = DefaultResearchDurationSeconds,
                IsCompleted = false
            });

            return true;
        }

        public static bool TryGetTank(
            LobbyRuntimeData lobby,
            string tankId,
            out CultureTankResearchRuntimeData tank)
        {
            tank = null;

            if (lobby == null)
                return false;

            Normalize(lobby);
            string normalizedTankId = NormalizeId(tankId);
            if (string.IsNullOrEmpty(normalizedTankId))
                return false;

            for (int i = 0; i < lobby.CultureTankResearches.Count; i++)
            {
                CultureTankResearchRuntimeData candidate = lobby.CultureTankResearches[i];
                if (candidate == null)
                    continue;

                if (!string.Equals(NormalizeId(candidate.TankId), normalizedTankId, StringComparison.Ordinal))
                    continue;

                tank = candidate;
                return true;
            }

            return false;
        }

        public static bool RefreshCompletion(
            CultureTankResearchRuntimeData tank,
            long nowUtcTicks)
        {
            if (tank == null || tank.IsCompleted)
                return false;

            if (GetRemainingSeconds(tank, nowUtcTicks) > 0)
                return false;

            tank.IsCompleted = true;
            return true;
        }

        public static int GetRemainingSeconds(
            CultureTankResearchRuntimeData tank,
            long nowUtcTicks)
        {
            if (tank == null)
                return 0;

            int durationSeconds = Mathf.Max(0, tank.DurationSeconds);
            long elapsedTicks = Math.Max(0L, nowUtcTicks - Math.Max(0L, tank.StartedAtUtcTicks));
            double elapsedSeconds = TimeSpan.FromTicks(elapsedTicks).TotalSeconds;
            return Mathf.Max(0, Mathf.CeilToInt(durationSeconds - (float)elapsedSeconds));
        }

        public static float GetProgress01(
            CultureTankResearchRuntimeData tank,
            long nowUtcTicks)
        {
            if (tank == null)
                return 0f;

            int durationSeconds = Mathf.Max(1, tank.DurationSeconds);
            long elapsedTicks = Math.Max(0L, nowUtcTicks - Math.Max(0L, tank.StartedAtUtcTicks));
            float elapsedSeconds = (float)TimeSpan.FromTicks(elapsedTicks).TotalSeconds;
            return Mathf.Clamp01(elapsedSeconds / durationSeconds);
        }

        public static bool TryClaimCompletedResearch(
            LobbyRuntimeData lobby,
            ItemData item,
            string tankId,
            long nowUtcTicks,
            out CultureTankBattleStartEffectRuntimeData effect,
            out string error)
        {
            effect = null;
            error = string.Empty;

            if (lobby == null)
            {
                error = "Lobby runtime is missing.";
                return false;
            }

            Normalize(lobby);

            if (!TryGetTank(lobby, tankId, out CultureTankResearchRuntimeData tank))
            {
                error = "Culture tank is empty.";
                return false;
            }

            RefreshCompletion(tank, nowUtcTicks);

            if (!tank.IsCompleted)
            {
                error = "Research is not completed.";
                return false;
            }

            if (item == null || !string.Equals(NormalizeId(item.ItemId), NormalizeId(tank.ItemId), StringComparison.Ordinal))
            {
                error = "Completed item data is missing.";
                return false;
            }

            effect = BuildBattleStartEffect(item);
            if (effect == null)
            {
                error = "Completed item has no battle start effect.";
                return false;
            }

            lobby.PendingCultureTankBattleStartEffects.Add(effect);
            RemoveTank(lobby, tank.TankId);
            return true;
        }

        public static CultureTankBattleStartEffectRuntimeData BuildBattleStartEffect(ItemData item)
        {
            if (item == null)
                return null;

            string itemId = NormalizeId(item.ItemId);
            string effectId = NormalizeId(item.EffectId);

            if (string.IsNullOrEmpty(itemId) || string.IsNullOrEmpty(effectId))
                return null;

            int value = ParsePositiveInt(item.ValueRate, 1);
            int count = ParsePositiveInt(item.CountRate, 1);

            return new CultureTankBattleStartEffectRuntimeData
            {
                SourceItemId = itemId,
                EffectId = effectId,
                Value = value,
                Count = count,
                RemainingBattleStarts = DefaultBattleStartEffectUses
            };
        }

        public static List<CultureTankBattleStartEffectRuntimeData> CopyPendingBattleStartEffects(
            LobbyRuntimeData lobby)
        {
            var result = new List<CultureTankBattleStartEffectRuntimeData>();

            if (lobby == null)
                return result;

            Normalize(lobby);

            for (int i = 0; i < lobby.PendingCultureTankBattleStartEffects.Count; i++)
            {
                CultureTankBattleStartEffectRuntimeData source = lobby.PendingCultureTankBattleStartEffects[i];
                CultureTankBattleStartEffectRuntimeData copy = CopyBattleStartEffect(source);

                if (copy != null)
                    result.Add(copy);
            }

            return result;
        }

        public static CultureTankBattleStartEffectRuntimeData CopyBattleStartEffect(
            CultureTankBattleStartEffectRuntimeData source)
        {
            if (source == null ||
                string.IsNullOrWhiteSpace(source.EffectId) ||
                source.RemainingBattleStarts <= 0)
            {
                return null;
            }

            return new CultureTankBattleStartEffectRuntimeData
            {
                SourceItemId = NormalizeId(source.SourceItemId),
                EffectId = NormalizeId(source.EffectId),
                Value = Mathf.Max(0, source.Value),
                Count = Mathf.Max(0, source.Count),
                RemainingBattleStarts = Mathf.Max(0, source.RemainingBattleStarts)
            };
        }

        public static void Normalize(LobbyRuntimeData lobby)
        {
            if (lobby == null)
                return;

            lobby.CultureTankResearches ??= new List<CultureTankResearchRuntimeData>();
            lobby.PendingCultureTankBattleStartEffects ??= new List<CultureTankBattleStartEffectRuntimeData>();

            for (int i = lobby.CultureTankResearches.Count - 1; i >= 0; i--)
            {
                CultureTankResearchRuntimeData tank = lobby.CultureTankResearches[i];
                if (tank == null ||
                    string.IsNullOrWhiteSpace(tank.TankId) ||
                    string.IsNullOrWhiteSpace(tank.ItemId))
                {
                    lobby.CultureTankResearches.RemoveAt(i);
                    continue;
                }

                tank.TankId = NormalizeId(tank.TankId);
                tank.ItemId = NormalizeId(tank.ItemId);
                tank.StartedAtUtcTicks = Math.Max(0L, tank.StartedAtUtcTicks);

                if (tank.DurationSeconds <= 0)
                    tank.DurationSeconds = DefaultResearchDurationSeconds;
            }

            for (int i = lobby.PendingCultureTankBattleStartEffects.Count - 1; i >= 0; i--)
            {
                CultureTankBattleStartEffectRuntimeData copy =
                    CopyBattleStartEffect(lobby.PendingCultureTankBattleStartEffects[i]);

                if (copy == null)
                    lobby.PendingCultureTankBattleStartEffects.RemoveAt(i);
                else
                    lobby.PendingCultureTankBattleStartEffects[i] = copy;
            }
        }

        private static void RemoveTank(LobbyRuntimeData lobby, string tankId)
        {
            if (lobby == null || lobby.CultureTankResearches == null)
                return;

            string normalizedTankId = NormalizeId(tankId);

            for (int i = lobby.CultureTankResearches.Count - 1; i >= 0; i--)
            {
                CultureTankResearchRuntimeData tank = lobby.CultureTankResearches[i];

                if (tank != null &&
                    string.Equals(NormalizeId(tank.TankId), normalizedTankId, StringComparison.Ordinal))
                {
                    lobby.CultureTankResearches.RemoveAt(i);
                }
            }
        }

        private static string NormalizeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static int ParsePositiveInt(string value, int fallback)
        {
            if (int.TryParse(value, out int parsed))
                return Mathf.Max(0, parsed);

            return Mathf.Max(0, fallback);
        }
    }
}
