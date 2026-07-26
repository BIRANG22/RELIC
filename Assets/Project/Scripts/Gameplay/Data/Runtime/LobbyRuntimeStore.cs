using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    public sealed class LobbyRuntimeStore
    {
        public const int StartingBlueDustium = 0;

        private LobbyRuntimeData data;

        public LobbyRuntimeData Get()
        {
            return data;
        }

        public LobbyRuntimeData GetOrCreate()
        {
            data ??= new LobbyRuntimeData();
            Normalize(data);
            return data;
        }

        public void Set(LobbyRuntimeData value)
        {
            data = value;
            if (data != null)
                Normalize(data);
        }

        private static void Normalize(LobbyRuntimeData value)
        {
            value.OwnedRelicIds ??= new List<string>();
            value.SkillInventoryIds ??= new List<string>();
            value.BagItemIds ??= new List<string>();
            value.CharacterLoadouts ??= new List<LobbyCharacterLoadoutData>();
            value.CharacterSkillUpgrades ??= new List<LobbySkillUpgradeRecordData>();
            value.RelicOfferIds ??= new List<string>();
            CultureTankResearchService.Normalize(value);

            if (!value.HasPendingResearchResult || value.PendingResearchResult == null)
            {
                value.HasPendingResearchResult = false;
                value.PendingResearchResult = null;
            }
        }
    }
}
