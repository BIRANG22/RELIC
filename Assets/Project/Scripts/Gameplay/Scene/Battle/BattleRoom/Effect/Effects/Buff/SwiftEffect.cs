public class SwiftEffect : BattleEffectBase
{
    private const string TimeDistortionSkillId = "S_Core_51";
    private const string TimeDistortionPlusSkillId = "S_Core_52";
    private const string SpeedsterSkillId = "S_Core_79";
    private const string SpeedsterPlusSkillId = "S_Core_80";

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

        return skillId == TimeDistortionSkillId ||
               skillId == TimeDistortionPlusSkillId ||
               skillId == SpeedsterSkillId ||
               skillId == SpeedsterPlusSkillId;
    }
}