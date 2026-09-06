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

            // 파티의 실제 슬롯 번호를 그대로 유지합니다.
            // 예: 1번 슬롯이 비어 있고 2, 3번 슬롯에 캐릭터가 있으면
            // 인벤토리에서도 첫 번째 줄은 비우고 두 번째, 세 번째 줄에 표시합니다.
            int copyCount = Math.Min(party.MaxPartyCountValue, result.Length);
            for (int partyIndex = 0; partyIndex < copyCount; partyIndex++)
            {
                string characterId = party.GetCharacterId(partyIndex);
                if (string.IsNullOrWhiteSpace(characterId))
                    continue;

                result[partyIndex] = new PartyInventoryCharacterEntry(
                    partyIndex,
                    characterId.Trim());
            }

            return result;
        }
    }
}
