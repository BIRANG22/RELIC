namespace Relic.Gameplay.Data
{
    public enum EndTurn
    {
        None,
        ReMove,
        Decrease,
        Maintain,
    }

    [System.Serializable]
    public class EffectMasterData
    {
        public string EffectId;
        public string Name;

        public bool Nesting;
        public EndTurn EndTurn;

        public string ToolTip;
    }
}