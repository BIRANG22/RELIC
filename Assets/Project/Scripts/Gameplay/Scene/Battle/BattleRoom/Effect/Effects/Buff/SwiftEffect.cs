public class SwiftEffect : BattleEffectBase
{
    private const string TimeDistortionSkillId = "S_Core_26";
    private const string SpeedsterSkillId = "S_Core_40";

    public override string EffectId => "E_Swift";

    protected override void Apply(BattleEffectContext context)
    {
        if (context == null)
            return;

        if (context.PlayerTarget != null)
        {
            if (ShouldQueueForNextTurn(context))
            {
                BattleTurnExecutor turnExecutor =
                    UnityEngine.Object.FindFirstObjectByType<BattleTurnExecutor>();

                if (turnExecutor != null)
                {
                    turnExecutor.QueueNextTurnSwift(
                        context.PlayerTarget,
                        context.Value,
                        context.Count);
                }

                return;
            }

            BattleEffectUtility.AddStatusToPlayer(
                context.PlayerTarget,
                EffectId,
                context.Value,
                context.Count);
            return;
        }

        if (context.PlayerCaster != null)
        {
            BattleEffectUtility.AddStatusToPlayer(context.PlayerTarget, EffectId, context.Value, context.Count);
            return;
        }

        if (context.MonsterTarget != null)
        {
            BattleEffectUtility.AddStatusToMonster(context.MonsterTarget, EffectId, context.Value, context.Count);
            return;
        }

        if (context.MonsterCaster != null)
        {
            BattleEffectUtility.AddStatusToMonster(context.MonsterTarget, EffectId, context.Value, context.Count);
        }
    }

    private static bool ShouldQueueForNextTurn(BattleEffectContext context)
    {
        string skillId = context?.PlayerSkillData?.SkillId;

        return skillId == TimeDistortionSkillId || skillId == SpeedsterSkillId;
    }
}
