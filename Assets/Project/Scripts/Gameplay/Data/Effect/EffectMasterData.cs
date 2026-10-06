namespace Relic.Gameplay.Data
{
    public enum EndTurn
    {
        None,
        Remove,
        Decrease,
        Maintain,
        DecreaseOnTrigger,
    }

    public enum EffectValueDecreaseRule
    {
        None = EndTurn.None,
        Remove = EndTurn.Remove,
        Decrease = EndTurn.Decrease,
        Maintain = EndTurn.Maintain,
        DecreaseOnTrigger = EndTurn.DecreaseOnTrigger
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
        public EffectValueDecreaseRule DecreaseRule => (EffectValueDecreaseRule)EndTurn;

        public string ToolTip;
    }
}
