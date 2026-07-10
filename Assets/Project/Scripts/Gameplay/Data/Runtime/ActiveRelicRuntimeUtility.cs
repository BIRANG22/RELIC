using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public static class ActiveRelicRuntimeUtility
    {
        public const int ActiveRelicSlotIndex = 0;
        public const int EquippedRelicSlotCount = 5;

        public static string GetActiveRelicId(CharacterRuntimeData runtime)
        {
            EnsureRelicSlots(runtime);

            if (runtime?.EquippedRelicIds == null ||
                runtime.EquippedRelicIds.Length <= ActiveRelicSlotIndex)
            {
                return string.Empty;
            }

            return runtime.EquippedRelicIds[ActiveRelicSlotIndex]?.Trim();
        }

        public static void EnsureRelicSlots(CharacterRuntimeData runtime)
        {
            if (runtime == null)
                return;

            if (runtime.EquippedRelicIds != null &&
                runtime.EquippedRelicIds.Length == EquippedRelicSlotCount)
            {
                return;
            }

            string[] normalized = new string[EquippedRelicSlotCount];

            if (runtime.EquippedRelicIds != null)
            {
                int count = Mathf.Min(runtime.EquippedRelicIds.Length, normalized.Length);

                for (int i = 0; i < count; i++)
                    normalized[i] = runtime.EquippedRelicIds[i];
            }

            runtime.EquippedRelicIds = normalized;
        }

        public static int GetMaxUses(RelicData relic)
        {
            return Mathf.Max(0, relic?.Durability ?? 0);
        }

        public static int GetRemainingUses(CharacterRuntimeData runtime, RelicData relic)
        {
            ActiveRelicUseRuntimeData entry = GetOrCreateUseEntry(runtime, relic);
            return entry != null ? entry.RemainingUses : 0;
        }

        public static bool TryConsumeUse(CharacterRuntimeData runtime, RelicData relic)
        {
            ActiveRelicUseRuntimeData entry = GetOrCreateUseEntry(runtime, relic);

            if (entry == null || entry.RemainingUses <= 0)
                return false;

            entry.RemainingUses--;
            return true;
        }

        public static void ResetUses(CharacterRuntimeData runtime, RelicData relic)
        {
            ActiveRelicUseRuntimeData entry = GetOrCreateUseEntry(runtime, relic);

            if (entry == null)
                return;

            entry.RemainingUses = GetMaxUses(relic);
        }

        public static void NormalizeUseEntries(CharacterRuntimeData runtime)
        {
            if (runtime == null)
                return;

            runtime.ActiveRelicUses ??= new List<ActiveRelicUseRuntimeData>();

            for (int i = runtime.ActiveRelicUses.Count - 1; i >= 0; i--)
            {
                ActiveRelicUseRuntimeData entry = runtime.ActiveRelicUses[i];

                if (entry == null || string.IsNullOrWhiteSpace(entry.RelicId))
                {
                    runtime.ActiveRelicUses.RemoveAt(i);
                    continue;
                }

                entry.RelicId = entry.RelicId.Trim();
                entry.RemainingUses = Mathf.Max(0, entry.RemainingUses);
            }
        }

        public static bool TryAddTurnScopedStatus(CharacterRuntimeData runtime, string effectId)
        {
            if (runtime == null || string.IsNullOrWhiteSpace(effectId))
                return false;

            runtime.StatusEffects ??= new List<StatusEffectRuntimeData>();

            string normalizedEffectId = effectId.Trim();

            for (int i = 0; i < runtime.StatusEffects.Count; i++)
            {
                StatusEffectRuntimeData status = runtime.StatusEffects[i];

                if (status == null)
                    continue;

                if (status.EffectId == normalizedEffectId)
                    return false;
            }

            runtime.StatusEffects.Add(new StatusEffectRuntimeData
            {
                EffectId = normalizedEffectId,
                Stack = 1,
                TurnCount = 1
            });

            return true;
        }

        public static void RemoveTurnScopedStatuses(CharacterRuntimeData runtime)
        {
            if (runtime?.StatusEffects == null)
                return;

            for (int i = runtime.StatusEffects.Count - 1; i >= 0; i--)
            {
                StatusEffectRuntimeData status = runtime.StatusEffects[i];

                if (status == null)
                    continue;

                if (IsTurnScopedActiveRelicStatus(status.EffectId))
                    runtime.StatusEffects.RemoveAt(i);
            }
        }

        public static bool IsTurnScopedActiveRelicStatus(string effectId)
        {
            return effectId == ActiveRelicEffectIds.DamageBoostThisTurn ||
                   effectId == ActiveRelicEffectIds.DamageReductionThisTurn;
        }

        private static ActiveRelicUseRuntimeData GetOrCreateUseEntry(
            CharacterRuntimeData runtime,
            RelicData relic)
        {
            if (runtime == null || relic == null || string.IsNullOrWhiteSpace(relic.FragmentId))
                return null;

            runtime.ActiveRelicUses ??= new List<ActiveRelicUseRuntimeData>();

            string relicId = relic.FragmentId.Trim();

            for (int i = 0; i < runtime.ActiveRelicUses.Count; i++)
            {
                ActiveRelicUseRuntimeData entry = runtime.ActiveRelicUses[i];

                if (entry == null)
                    continue;

                if (entry.RelicId != relicId)
                    continue;

                entry.RemainingUses = Mathf.Clamp(entry.RemainingUses, 0, GetMaxUses(relic));
                return entry;
            }

            ActiveRelicUseRuntimeData created = new()
            {
                RelicId = relicId,
                RemainingUses = GetMaxUses(relic)
            };
            runtime.ActiveRelicUses.Add(created);
            return created;
        }
    }
}
