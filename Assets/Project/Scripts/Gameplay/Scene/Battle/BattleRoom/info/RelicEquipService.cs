using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class RelicEquipService
    {
        private readonly CharacterRuntimeStore characterStore;
        private readonly BattleRuntimeData battleRuntimeData;

        public RelicEquipService(
            CharacterRuntimeStore characterStore,
            BattleRuntimeData battleRuntimeData)
        {
            this.characterStore = characterStore;
            this.battleRuntimeData = battleRuntimeData;
        }

        public bool EquipRelic(string characterId, int slotIndex, string relicId)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                return false;

            if (slotIndex < 0 || slotIndex >= 5)
                return false;

            if (string.IsNullOrWhiteSpace(relicId))
                return false;

            relicId = relicId.Trim();

            if (battleRuntimeData == null ||
                battleRuntimeData.OwnedRelicIds == null ||
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

            return true;
        }

        public bool UnequipRelic(string characterId, int slotIndex)
        {
            if (string.IsNullOrWhiteSpace(characterId))
                return false;

            if (slotIndex < 0 || slotIndex >= 5)
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
            if (battleRuntimeData == null || string.IsNullOrWhiteSpace(relicId))
                return;

            relicId = relicId.Trim();
            battleRuntimeData.OwnedRelicIds ??= new System.Collections.Generic.List<string>();

            if (!HasOwnedRelic(relicId))
                battleRuntimeData.OwnedRelicIds.Add(relicId);
        }

        private bool HasOwnedRelic(string relicId)
        {
            if (battleRuntimeData == null || battleRuntimeData.OwnedRelicIds == null || string.IsNullOrWhiteSpace(relicId))
                return false;

            string targetId = relicId.Trim();

            for (int i = 0; i < battleRuntimeData.OwnedRelicIds.Count; i++)
            {
                if (string.Equals(battleRuntimeData.OwnedRelicIds[i]?.Trim(), targetId, System.StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private void RemoveAllOwnedRelic(string relicId)
        {
            if (battleRuntimeData == null || battleRuntimeData.OwnedRelicIds == null || string.IsNullOrWhiteSpace(relicId))
                return;

            string targetId = relicId.Trim();

            for (int i = battleRuntimeData.OwnedRelicIds.Count - 1; i >= 0; i--)
            {
                if (string.Equals(battleRuntimeData.OwnedRelicIds[i]?.Trim(), targetId, System.StringComparison.Ordinal))
                    battleRuntimeData.OwnedRelicIds.RemoveAt(i);
            }
        }

        public static void EnsureRelicSlots(CharacterRuntimeData character)
        {
            if (character == null)
                return;

            if (character.EquippedRelicIds != null &&
                character.EquippedRelicIds.Length == 5)
                return;

            string[] newSlots = new string[5];

            if (character.EquippedRelicIds != null)
            {
                int count = Mathf.Min(character.EquippedRelicIds.Length, newSlots.Length);

                for (int i = 0; i < count; i++)
                    newSlots[i] = character.EquippedRelicIds[i];
            }

            character.EquippedRelicIds = newSlots;
        }
    }
}
