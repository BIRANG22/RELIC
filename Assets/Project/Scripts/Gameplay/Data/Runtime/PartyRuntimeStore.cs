using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class PartyRuntimeStore
    {
        private const int MaxPartyCount = 3;

        private readonly string[] characterIds = new string[MaxPartyCount];

        public string GetCharacterId(int slotIndex)
        {
            if (!IsValidSlot(slotIndex))
                return null;

            return characterIds[slotIndex];
        }

        public bool SetCharacter(int slotIndex, string characterId)
        {
            if (!IsValidSlot(slotIndex))
            {
                Debug.LogWarning($"[PartyRuntimeStore] Invalid slot index: {slotIndex}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(characterId))
            {
                Debug.LogWarning("[PartyRuntimeStore] CharacterId is empty.");
                return false;
            }

            characterIds[slotIndex] = characterId;

            Debug.Log($"[PartyRuntimeStore] Slot {slotIndex} saved: {characterId}");
            LogParty();

            return true;
        }

        private bool IsValidSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < MaxPartyCount;
        }

        private void LogParty()
        {
            Debug.Log(
                $"[PartyRuntimeStore]\n" +
                $"Slot 0: {characterIds[0] ?? "Empty"}\n" +
                $"Slot 1: {characterIds[1] ?? "Empty"}\n" +
                $"Slot 2: {characterIds[2] ?? "Empty"}"
            );
        }
    }
}