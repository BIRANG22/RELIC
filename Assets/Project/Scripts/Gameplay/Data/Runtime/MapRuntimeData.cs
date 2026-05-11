using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [System.Serializable]
    public class MapRuntimeData
    {
        public string SelectedThemeId;

        public int CurrentStage;
        public string CurrentMapId;
        public string CurrentSceneName;

        public List<string> ClearedMapIds = new();
        public List<string> VisitedMapIds = new();

        public bool IsBossUnlocked;
        public bool IsRunInitialized;
    }
}