using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [System.Serializable]
    public class PlayerRuntimeData
    {
        public int Level = 1;
        public int Exp = 0;
        public int TotalExp = 0;

        // 도감에서 한 번이라도 획득한 콘텐츠를 영구적으로 기억합니다.
        public List<string> DiscoveredSkillIds = new();
        public List<string> DiscoveredRuneIds = new();
        public List<string> DiscoveredRelicIds = new();
        public List<string> DiscoveredCompoundIds = new();
        public List<string> DiscoveredItemIds = new();
    }
}
