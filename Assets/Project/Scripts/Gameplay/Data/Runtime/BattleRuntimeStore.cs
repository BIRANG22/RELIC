using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class BattleRuntimeStore
    {
        public const int StartingRemnant = 100;

        private BattleRuntimeData currentRun;

        public void Set(BattleRuntimeData data)
        {
            currentRun = data;
        }

        public BattleRuntimeData Get()
        {
            return currentRun;
        }

        public bool HasRun()
        {
            return currentRun != null && currentRun.IsBattleRunInitialized;
        }

        public BattleRuntimeData GetOrCreate()
        {
            if (currentRun == null)
            {
                currentRun = new BattleRuntimeData
                {
                    Remnant = StartingRemnant,
                    IsBattleRunInitialized = true
                };
            }

            currentRun.OwnedRelicIds ??= new List<string>();
            currentRun.BagItemIds ??= new List<string>();
            currentRun.SkillInventoryIds ??= new List<string>();
            currentRun.StartingSkillInventoryIds ??= new List<string>();
            currentRun.AcquiredSkillIds ??= new List<string>();
            currentRun.CharacterStatistics ??= new List<BattleRunCharacterStatisticsData>();
            currentRun.LobbyLoadoutSnapshots ??= new List<BattleLobbyLoadoutSnapshotData>();
            CultureTankBattleStartEffectService.Normalize(currentRun);

            return currentRun;
        }

        public void Clear()
        {
            currentRun = null;
            Debug.Log("[BattleRuntimeStore] Cleared battle runtime.");
        }
    }
}
