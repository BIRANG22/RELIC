public interface IBattleEffect
{
    string EffectId { get; }
    void Apply(BattleEffectContext context);
}