namespace Relic.Gameplay.Data
{
    public enum EndTurn
    {
        None,
        Remove,
        Decrease,
        Maintain,
    }

    public enum EffectType
    {
        Neutral,
        Beneficial,
        Harmful,
    }

    [System.Serializable]
    public class EffectMasterData
    {
        public string EffectId;
        public string Name;

        public EffectType EffectType;
        public EndTurn EndTurn;

        public string ToolTip;
    }
}
