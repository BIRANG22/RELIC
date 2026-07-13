namespace Relic.Gameplay.Data
{
    public enum EndTurn
    {
        None,
        Remove,
        Decrease,
        Maintain,
    }

    [System.Serializable]
    public class EffectMasterData
    {
        public string EffectId;
        public string Name;


        public EndTurn EndTurn;

        public string ToolTip;
    }
}