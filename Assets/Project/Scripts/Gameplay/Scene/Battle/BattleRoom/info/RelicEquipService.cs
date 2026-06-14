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

            if (battleRuntimeData == null ||
                battleRuntimeData.OwnedRelicIds == null ||
                !battleRuntimeData.OwnedRelicIds.Contains(relicId))
            {
                Debug.LogWarning($"[RelicEquipService] 보유하지 않은 렐릭: {relicId}");
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
                battleRuntimeData.OwnedRelicIds.Add(previousRelicId);

            character.EquippedRelicIds[slotIndex] = relicId;
            battleRuntimeData.OwnedRelicIds.Remove(relicId);

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

            battleRuntimeData.OwnedRelicIds ??= new System.Collections.Generic.List<string>();
            battleRuntimeData.OwnedRelicIds.Add(relicId);

            return true;
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