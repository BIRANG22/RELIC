namespace Relic.Gameplay.Data
{
    public enum EndTurn
    {
        None,
        Self,
        Target,
        PlayerParty,
        EnemyParty
    }

    [System.Serializable]
    public class EffectMasterData
    {
        public string EffectId;
        public string Name;

        public bool Nesting;
        public EndTurn TurnEndStatus;

        public string ToolTip;
    }
}