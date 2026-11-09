using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;

public static class BattleWardReactionService
{
    private const string WardEffectId = "E_Ward";

    public static int ResolveReactionDamage(
        IReadOnlyList<StatusEffectRuntimeData> statuses,
        int directDamageDealt)
    {
        if (directDamageDealt <= 0 || statuses == null)
            return 0;

        int reactionDamage = 0;

        for (int i = 0; i < statuses.Count; i++)
        {
            StatusEffectRuntimeData status = statuses[i];

            if (status == null ||
                !string.Equals(status.EffectId, WardEffectId, StringComparison.Ordinal) ||
                status.Stack <= 0 ||
                status.TurnCount <= 0)
            {
                continue;
            }

            reactionDamage += status.Stack;
        }

        return Math.Max(0, reactionDamage);
    }

    public static void Apply(
        BattleEffectContext context,
        int directDamageDealt,
        int reactionDamage)
    {
        if (context == null || directDamageDealt <= 0 || reactionDamage <= 0)
            return;

        if (context.PlayerTarget != null && context.MonsterCaster != null)
        {
            BattleEffectUtility.DamageMonster(context.MonsterCaster, reactionDamage);
            ConsumeTriggeredWard(context.PlayerTarget.RuntimeData?.StatusEffects);
            return;
        }

        if (context.MonsterTarget != null && context.PlayerCaster != null)
        {
            BattleEffectUtility.DamagePlayer(context.PlayerCaster, reactionDamage);
            ConsumeTriggeredWard(context.MonsterTarget.RuntimeData?.StatusEffects);
        }
    }

    private static void ConsumeTriggeredWard(List<StatusEffectRuntimeData> statuses)
    {
        // 가시 반격이 실제로 발동했을 때만 수치를 1 소모합니다.
        // Effect 시트의 E_Ward.EndTurn은 DecreaseOnTrigger로 설정해야 합니다.
        BattleStatusEffectService.TryConsumeOnTrigger(statuses, WardEffectId);
    }
}
