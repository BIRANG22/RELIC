using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public static class ActiveRelicRuntimeUtility
    {
        public const int ActiveRelicSlotIndex = 0;
        public const int EquippedRelicSlotCount = 7;

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

        // Legacy character charge records are no longer used. Keep the save/API boundary.
        public static int GetMaxUses(RelicData relic) => 0;
        public static int GetRemainingUses(CharacterRuntimeData runtime, RelicData relic) => 0;
        public static bool TryConsumeUse(CharacterRuntimeData runtime, RelicData relic) => false;
        public static void ResetUses(CharacterRuntimeData runtime, RelicData relic) => NormalizeUseEntries(runtime);
        public static void NormalizeUseEntries(CharacterRuntimeData runtime)
        {
            if (runtime == null) return;
            runtime.ActiveRelicUses?.Clear();
        }
    }
}
