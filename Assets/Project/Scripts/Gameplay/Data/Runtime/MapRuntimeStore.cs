using UnityEngine;

namespace Relic.Gameplay.Data
{
    public class MapRuntimeStore
    {
        private MapRuntimeData currentRun;

        public void Set(MapRuntimeData data)
        {
            currentRun = data;

            Debug.Log("[MapRuntimeStore] Saved current map runtime.");
            LogMapRuntime(data);
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
            Debug.Log(
                $"[MapRuntime]\n" +
                $"Theme: {data.SelectedThemeId}\n" +
                $"Stage: {data.CurrentStage}\n" +
                $"MapId: {data.CurrentMapId}\n" +
                $"Scene: {data.CurrentSceneName}\n" +
                $"BossUnlocked: {data.IsBossUnlocked}\n" +
                $"Initialized: {data.IsRunInitialized}\n" +
                $"Cleared: {string.Join(", ", data.ClearedMapIds)}\n" +
                $"Visited: {string.Join(", ", data.VisitedMapIds)}"
            );
        }
    }
}