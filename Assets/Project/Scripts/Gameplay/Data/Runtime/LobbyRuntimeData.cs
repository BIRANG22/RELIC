using System;
using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public enum LobbyTutorialProgress
    {
        NotStarted = 0,
        WaitingForSetup = 1,
        FirstExpeditionAssigned = 2,
        Completed = 3
    }

    [Serializable]
    public sealed class LobbyRuntimeData
    {
        public int BlueDustium = LobbyRuntimeStore.StartingBlueDustium;
        public LobbyTutorialProgress TutorialProgress = LobbyTutorialProgress.NotStarted;
        public int LobbySkillUpgradeCount;
        public List<string> OwnedRelicIds = new();
        public List<string> SkillInventoryIds = new();
        public List<string> BagItemIds = new();
        public List<string> StoredCompoundIds = new();
        public List<LobbyCharacterLoadoutData> CharacterLoadouts = new();
        public List<LobbySkillUpgradeRecordData> CharacterSkillUpgrades = new();
        public int RelicOfferSeed;
        public int RelicRefreshCount;
        public List<string> RelicOfferIds = new();
        public int CultureTankCombinationSchemaVersion;
        public List<CultureTankResearchRuntimeData> CultureTankResearches = new();
        public string CompletedCultureTankCombinationId;
        public List<CultureTankBattleStartEffectRuntimeData> PendingCultureTankBattleStartEffects = new();
        public bool HasPendingResearchResult;
        public PendingResearchResultData PendingResearchResult;
    }

    [Serializable]
    public sealed class LobbyCharacterLoadoutData
    {
        public string CharacterId;
        public string[] EquippedRelicIds = new string[7];
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
