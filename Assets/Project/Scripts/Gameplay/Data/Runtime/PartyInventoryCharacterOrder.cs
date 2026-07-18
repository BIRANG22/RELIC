using System;

namespace Relic.Gameplay.Data
{
    public readonly struct PartyInventoryCharacterEntry
    {
        public PartyInventoryCharacterEntry(int partySlotIndex, string characterId)
        {
            PartySlotIndex = partySlotIndex;
            CharacterId = characterId;
        }

        public int PartySlotIndex { get; }
        public string CharacterId { get; }
    }

    public static class PartyInventoryCharacterOrder
    {
        public static PartyInventoryCharacterEntry[] Build(PartyRuntimeStore party, int displaySlotCount)
        {
            var result = new PartyInventoryCharacterEntry[Math.Max(0, displaySlotCount)];
            for (int i = 0; i < result.Length; i++)
                result[i] = new PartyInventoryCharacterEntry(-1, null);

            if (party == null)
                return result;

            int displayIndex = 0;
            for (int partyIndex = 0;
                 partyIndex < party.MaxPartyCountValue && displayIndex < result.Length;
                 partyIndex++)
            {
                string characterId = party.GetCharacterId(partyIndex);
                if (string.IsNullOrWhiteSpace(characterId))
                    continue;

                result[displayIndex++] = new PartyInventoryCharacterEntry(
                    partyIndex,
                    characterId.Trim());
            }

            return result;
        }
    }
}
