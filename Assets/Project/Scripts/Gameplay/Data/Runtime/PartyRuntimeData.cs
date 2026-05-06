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
        public int GridIndex = -1;
    }
}