namespace Relic.Gameplay.Data
{
    public static class CharacterStartingRelicUtility
    {
        public static string[] CreateStartingRelicSlots(CharacterMasterData master)
        {
            string[] slots = new string[ActiveRelicRuntimeUtility.EquippedRelicSlotCount];

            string relicId = master?.Relic?.Trim();
            if (!string.IsNullOrWhiteSpace(relicId))
                slots[ActiveRelicRuntimeUtility.ActiveRelicSlotIndex] = relicId;

            return slots;
        }

        public static void ApplyStartingRelic(
            CharacterRuntimeData runtime,
            CharacterMasterData master,
            RelicDatabase relicDatabase)
        {
            if (runtime == null)
                return;

            runtime.EquippedRelicIds = CreateStartingRelicSlots(master);
            InitializeActiveRelicUses(runtime, relicDatabase);
        }

        public static bool EnsureStartingRelicEquippedIfEmpty(
            CharacterRuntimeData runtime,
            CharacterMasterData master,
            RelicDatabase relicDatabase)
        {
            if (runtime == null || master == null)
                return false;

            ActiveRelicRuntimeUtility.EnsureRelicSlots(runtime);

            string currentRelicId =
                runtime.EquippedRelicIds[ActiveRelicRuntimeUtility.ActiveRelicSlotIndex];

            if (!string.IsNullOrWhiteSpace(currentRelicId))
                return false;

            string startingRelicId = master.Relic?.Trim();

            if (string.IsNullOrWhiteSpace(startingRelicId))
                return false;

            runtime.EquippedRelicIds[ActiveRelicRuntimeUtility.ActiveRelicSlotIndex] =
                startingRelicId;

            InitializeActiveRelicUses(runtime, relicDatabase);
            return true;
        }

        public static int EnsureAllStartingRelicsEquippedIfEmpty(
            CharacterRuntimeStore runtimeStore,
            CharacterDatabase characterDatabase,
            RelicDatabase relicDatabase)
        {
            if (runtimeStore == null || characterDatabase == null)
                return 0;

            int changedCount = 0;

            foreach (CharacterRuntimeData runtime in runtimeStore.GetAll().Values)
            {
                if (runtime == null ||
                    string.IsNullOrWhiteSpace(runtime.CharacterId) ||
                    !characterDatabase.TryGet(runtime.CharacterId, out CharacterMasterData master))
                {
                    continue;
                }

                if (EnsureStartingRelicEquippedIfEmpty(runtime, master, relicDatabase))
                    changedCount++;
            }

            return changedCount;
        }

        public static void InitializeActiveRelicUses(
            CharacterRuntimeData runtime,
            RelicDatabase relicDatabase)
        {
            if (runtime == null || relicDatabase == null)
                return;

            string relicId = ActiveRelicRuntimeUtility.GetActiveRelicId(runtime);
            if (string.IsNullOrWhiteSpace(relicId))
                return;

            if (!relicDatabase.TryGet(relicId, out RelicData relic) ||
                !ActiveRelicEffectResolver.IsActiveRelic(relic))
            {
                return;
            }

            ActiveRelicRuntimeUtility.ResetUses(runtime, relic);
        }
    }
}
