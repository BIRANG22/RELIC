using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class RelicEquipService
    {
        private readonly CharacterRuntimeStore characterStore;
        private readonly System.Collections.Generic.IList<string> ownedRelicIds;
        private readonly RelicDatabase relicDatabase;

        public RelicEquipService(
            CharacterRuntimeStore characterStore,
            BattleRuntimeData battleRuntimeData,
            RelicDatabase relicDatabase)
        {
            this.characterStore = characterStore;
            ownedRelicIds = battleRuntimeData?.OwnedRelicIds;
            this.relicDatabase = relicDatabase;
        }

        public RelicEquipService(
            CharacterRuntimeStore characterStore,
            System.Collections.Generic.IList<string> ownedRelicIds,
            RelicDatabase relicDatabase)
        {
            this.characterStore = characterStore;
            this.ownedRelicIds = ownedRelicIds;
            this.relicDatabase = relicDatabase;
        }

        public bool EquipRelic(string characterId, int slotIndex, string relicId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                return false;

            if (slotIndex < 0 || slotIndex >= ActiveRelicRuntimeUtility.EquippedRelicSlotCount)
                return false;

            if (string.IsNullOrWhiteSpace(relicId))
                return false;

            relicId = relicId.Trim();

            if (!CanEquipRelicInSlot(slotIndex, relicId))
            {
                Debug.LogWarning($"[RelicEquipService] 슬롯 타입 불일치 / Relic:{relicId} / Slot:{slotIndex + 1}");
                return false;
            }

            if (ownedRelicIds == null ||
                !HasOwnedRelic(relicId))
            {
                Debug.LogWarning($"[RelicEquipService] 보유하지 않은 유물: {relicId}");
                return false;
            }

            if (!characterStore.TryGet(characterId, out CharacterRuntimeData character))
            {
                Debug.LogWarning($"[RelicEquipService] 캐릭터 없음: {characterId}");
                return false;
            }

            EnsureRelicSlots(character);

            string previousRelicId = character.EquippedRelicIds[slotIndex];

            if (!string.IsNullOrWhiteSpace(previousRelicId))
                AddOwnedRelicIfMissing(previousRelicId);

            character.EquippedRelicIds[slotIndex] = relicId;
            RemoveAllOwnedRelic(relicId);

            if (slotIndex == ActiveRelicRuntimeUtility.ActiveRelicSlotIndex)
                ResetActiveRelicUsesForEquippedRelic(character, relicId);

            return true;
        }

        private bool CanEquipRelicInSlot(int slotIndex, string relicId)
        {
            bool isCompoundSlot = slotIndex == ActiveRelicRuntimeUtility.ActiveRelicSlotIndex;

            if (isCompoundSlot)
            {
                return global::DataManager.Instance?.CompoundDatabase != null &&
                       global::DataManager.Instance.CompoundDatabase.TryGet(relicId, out _);
            }

            return relicDatabase != null &&
                   relicDatabase.TryGet(relicId, out RelicData relic) &&
                   !ActiveRelicEffectResolver.IsActiveRelic(relic);
        }

        public bool UnequipRelic(string characterId, int slotIndex)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                return false;

            if (slotIndex < 0 || slotIndex >= 6)
                return false;

            if (!characterStore.TryGet(characterId, out CharacterRuntimeData character))
                return false;

            EnsureRelicSlots(character);

            string relicId = character.EquippedRelicIds[slotIndex];

            if (string.IsNullOrWhiteSpace(relicId))
                return false;

            character.EquippedRelicIds[slotIndex] = null;

            AddOwnedRelicIfMissing(relicId);

            return true;
        }

        private void AddOwnedRelicIfMissing(string relicId)
        {
            if (ownedRelicIds == null || string.IsNullOrWhiteSpace(relicId))
                return;

            relicId = relicId.Trim();

            if (!HasOwnedRelic(relicId))
                ownedRelicIds.Add(relicId);
        }

        private bool HasOwnedRelic(string relicId)
        {
            if (ownedRelicIds == null || string.IsNullOrWhiteSpace(relicId))
                return false;

            string targetId = relicId.Trim();

            for (int i = 0; i < ownedRelicIds.Count; i++)
            {
                if (string.Equals(ownedRelicIds[i]?.Trim(), targetId, System.StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private void RemoveAllOwnedRelic(string relicId)
        {
            if (ownedRelicIds == null || string.IsNullOrWhiteSpace(relicId))
                return;

            string targetId = relicId.Trim();

            for (int i = ownedRelicIds.Count - 1; i >= 0; i--)
            {
                if (string.Equals(ownedRelicIds[i]?.Trim(), targetId, System.StringComparison.Ordinal))
                    ownedRelicIds.RemoveAt(i);
            }
        }

        public static void EnsureRelicSlots(CharacterRuntimeData character)
        {
            ActiveRelicRuntimeUtility.EnsureRelicSlots(character);
        }

        private static void ResetActiveRelicUsesForEquippedRelic(
            CharacterRuntimeData character,
            string relicId)
        {
            if (character == null ||
                string.IsNullOrWhiteSpace(relicId) ||
                global::DataManager.Instance?.CompoundDatabase == null)
            {
                return;
            }

            if (!global::DataManager.Instance.CompoundDatabase.TryGet(relicId, out CompoundData compound))
                return;

            ActiveRelicRuntimeUtility.ResetUses(character, compound);
        }
    }
}
