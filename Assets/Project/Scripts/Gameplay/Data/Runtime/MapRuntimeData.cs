using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [System.Serializable]
    public class MapRuntimeData
    {
        public string SelectedChapterId;

        public string CurrentStage;
        public string CurrentMapId;
        public string CurrentSceneName;

        public List<string> ClearedMapIds = new();
        public List<string> VisitedMapIds = new();

        public bool IsBossUnlocked;
        public bool IsRunInitialized;

        public List<GeneratedMapNodeData> GeneratedNodes = new();
    }
}