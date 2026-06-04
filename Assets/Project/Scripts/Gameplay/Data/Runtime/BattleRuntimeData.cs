using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [System.Serializable]
    public class BattleRuntimeData
    {
        // 소지 재화
        public int Remnant;

        // 소지 렐릭
        public List<string> OwnedRelicIds = new();

        // 가방 내부 아이템
        public List<string> BagItemIds = new();

        // 스킬 인벤토리 내부 스킬
        public List<string> SkillInventoryIds = new();

        // 진행 상태
        public int CurrentBattleCount;
        public int CurrentRewardCount;

        public bool IsBattleRunInitialized;
    }
}