using System;

namespace Relic.Gameplay.Data
{
    [Serializable]
    public sealed class CultureTankResearchRuntimeData
    {
        public string TankId;
        public string ItemId;
        public long StartedAtUtcTicks;
        public int DurationSeconds = CultureTankResearchService.DefaultResearchDurationSeconds;
        public bool IsCompleted;
    }

    [Serializable]
    public sealed class CultureTankBattleStartEffectRuntimeData
    {
        public string SourceItemId;
        public string EffectId;
        public int Value;
        public int Count;
        public int RemainingBattleStarts;
    }
}
