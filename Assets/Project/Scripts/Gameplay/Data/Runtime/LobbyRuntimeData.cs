using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public sealed class LobbyRuntimeData
    {
        public int BlueDustium = LobbyRuntimeStore.StartingBlueDustium;
        public List<string> OwnedRelicIds = new();
        public List<string> SkillInventoryIds = new();
        public List<string> BagItemIds = new();
        public List<LobbyCharacterLoadoutData> CharacterLoadouts = new();
        public int RelicOfferSeed;
        public List<string> RelicOfferIds = new();
    }

    [Serializable]
    public sealed class LobbyCharacterLoadoutData
    {
        public string CharacterId;
        public string[] EquippedRelicIds = new string[5];
        public string[] EquippedSkillIds = new string[4];
    }
}
