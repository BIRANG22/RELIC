public sealed class RushEffect : BattleEffectBase
{
    public override string EffectId => "E_Rush";

    protected override void Apply(BattleEffectContext context)
    {
        // 이동과 충돌 판정은 BattleActionRunner가 그리드 상태를 기준으로 처리한다.
        // 이 핸들러는 Effect ID 등록과 비실행 경로의 안전한 호환을 담당한다.
    }
}
