using System.Collections.Generic;

namespace Relic.Gameplay.Data
{
    [System.Serializable]
    public class PartyRuntimeData
    {
        public List<PartySlotRuntimeData> Slots = new(3);
    }

    [System.Serializable]
    public class PartySlotRuntimeData
    {
        public string CharacterId;

        public int SpawnGridIndex = -1;   // 전투 시작 위치
        public int CurrentGridIndex = -1; // 전투 중 현재 위치
    }
}