using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public static class BattlePartyRevivalService
    {
        public static int ReviveIncapacitatedParty(
            PartyRuntimeStore partyStore,
            CharacterRuntimeStore characterStore)
        {
            if (partyStore == null || characterStore == null)
                return 0;

            int revivedCount = 0;
            var processedCharacterIds = new HashSet<string>(System.StringComparer.Ordinal);

            for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
            {
                string characterId = partyStore.GetCharacterId(i);
                if (string.IsNullOrWhiteSpace(characterId))
                    continue;

                characterId = characterId.Trim();
                if (!processedCharacterIds.Add(characterId) ||
                    !characterStore.TryGet(characterId, out CharacterRuntimeData character) ||
                    character == null ||
                    !character.IsDead)
                {
                    continue;
                }

                character.CurrentHP = 1;
                revivedCount++;
            }

            return revivedCount;
        }
    }
}
