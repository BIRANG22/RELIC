using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public sealed class LobbyRuntimeData
    {
        public int BlueDustium = LobbyRuntimeStore.StartingBlueDustium;
        public int LobbySkillUpgradeCount;
        public List<string> OwnedRelicIds = new();
        public List<string> SkillInventoryIds = new();
        public List<string> BagItemIds = new();
        public List<LobbyCharacterLoadoutData> CharacterLoadouts = new();
        public List<LobbySkillUpgradeRecordData> CharacterSkillUpgrades = new();
        public int RelicOfferSeed;
        public int RelicRefreshCount;
        public List<string> RelicOfferIds = new();
        public List<CultureTankResearchRuntimeData> CultureTankResearches = new();
        public List<CultureTankBattleStartEffectRuntimeData> PendingCultureTankBattleStartEffects = new();
        public bool HasPendingResearchResult;
        public PendingResearchResultData PendingResearchResult;
    }

    [Serializable]
    public sealed class LobbyCharacterLoadoutData
    {
        public string CharacterId;
        public string[] EquippedRelicIds = new string[5];
        public string[] EquippedSkillIds = new string[4];
    }

    [Serializable]
    public sealed class LobbySkillUpgradeRecordData
    {
        public string CharacterId;
        public int SlotType;
        public int SlotIndex;
        public string SkillId;
    }
}
