public class BattleEffectExecutor
{
    private readonly BattleEffectRegistry registry = new();

    public bool AcceptsDeadTarget(string effectId) =>
        registry.TryGet(effectId, out var effect) && effect.AcceptsDeadTarget;

    public bool AcceptsDeadTarget(Relic.Gameplay.Data.SkillMasterData skill)
    {
        if (skill?.EffectEntries == null) return false;
        foreach (var entry in skill.EffectEntries)
            if (entry != null && AcceptsDeadTarget(entry.EffectId)) return true;
        return false;
    }

    public void Execute(string effectId, BattleEffectContext context)
    {
        BattleEffectBase effect = registry.Get(effectId);

        if (effect == null)
            return;

        context.EffectId = effectId;
        effect.Execute(context);
    }

    public void ExecuteMany(string effectIds, BattleEffectContext context)
    {
        if (string.IsNullOrWhiteSpace(effectIds))
            return;

        string[] split = effectIds.Split(';');

        for (int i = 0; i < split.Length; i++)
            Execute(split[i], context);
    }
}