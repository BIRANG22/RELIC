namespace Relic.Gameplay.Data
{
    public enum EffectTargetType
    {
        None,
        Self,
        Target,
        PlayerParty,
        EnemyParty
    }

    public enum EffectProcessType
    {
        None,
        Instant,     // 즉시 적용
        Status,      // 상태이상/버프
        Movement     // 이동/넉백 등
    }

    [System.Serializable]
    public class EffectMasterData
    {
        public string EffectId;
        public string Name;

        public EffectTargetType Target;
        public EffectProcessType ProcessType;

        public bool UsesValue;
        public bool UsesTurn;

        public int DurationTurn;

        public bool Stackable;
        public int MaxStack;

        public bool Removable;

        public bool CheckPierce;
        public bool CheckHeavyStrike;
    }
}