using System.Collections.Generic;
using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class BattleRuntimeStore
    {
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
                    IsBattleRunInitialized = true
                };
            }

            currentRun.OwnedRelicIds ??= new List<string>();
            currentRun.BagItemIds ??= new List<string>();
            currentRun.SkillInventoryIds ??= new List<string>();

            return currentRun;
        }

        public void Clear()
        {
            currentRun = null;
            Debug.Log("[BattleRuntimeStore] Cleared battle runtime.");
        }
    }
}