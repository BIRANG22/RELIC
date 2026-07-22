using System;
using Relic.Gameplay.Data;

public static class DebugBattleTargetRules
{
    public const string RuntimeId = "DebugBattle_Target";
    public const int MaxHp = 999;

    public static void Configure(MonsterRuntimeData data)
    {
        if (data == null)
            return;

        data.RuntimeId = RuntimeId;
        data.MaxHP = MaxHp;
        data.CurrentHP = MaxHp;
        data.CurrentShield = 0;
    }

    public static bool IsDebugTarget(MonsterRuntimeData data)
    {
        return data != null &&
               string.Equals(data.RuntimeId, RuntimeId, StringComparison.Ordinal);
    }

    public static bool TryRestoreFullHp(MonsterRuntimeData data)
    {
        if (!IsDebugTarget(data) || data.IsDead)
            return false;

        data.MaxHP = MaxHp;
        data.CurrentHP = MaxHp;
        return true;
    }
}
