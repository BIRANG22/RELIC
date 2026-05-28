using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class MapRuntimeStore
    {
        private MapRuntimeData currentRun;

        public void Set(MapRuntimeData data)
        {
            currentRun = data;
        }

        public MapRuntimeData Get()
        {
            return currentRun;
        }

        public bool HasRun()
        {
            return currentRun != null && currentRun.IsRunInitialized;
        }

        public void Clear()
        {
            currentRun = null;
            Debug.Log("[MapRuntimeStore] Cleared current map runtime.");
        }

        private void LogMapRuntime(MapRuntimeData data)
        {
        }
    }
}