/// <summary>
/// 모르트의 사령술(E_Spawn_Soldier) 효과입니다.
/// MortNecromancyTracker에 기록된 부활 가능한 병사 한 명을 다시 생성합니다.
/// </summary>
public class SpawnSoldierEffect : BattleEffectBase
{
    public override string EffectId => "E_Spawn_Soldier";

    protected override void Apply(BattleEffectContext context)
    {
        if (context == null || context.MonsterCaster == null || context.GridManager == null)
            return;

        MortNecromancyTracker.TryRespawnReadySoldier(
            context.MonsterCaster,
            context.GridManager);
    }
}
