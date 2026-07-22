using Relic.Gameplay.Monster;
using UnityEngine;

public sealed class DebugBattleTargetController : MonoBehaviour
{
    private void OnEnable()
    {
        BattleTurnExecutor.PlayerTurnReturned += RestoreTargetHp;
    }

    private void OnDisable()
    {
        BattleTurnExecutor.PlayerTurnReturned -= RestoreTargetHp;
    }

    private static void RestoreTargetHp()
    {
        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];
            if (monster == null || !DebugBattleTargetRules.TryRestoreFullHp(monster.RuntimeData))
                continue;

            monster.RefreshHUD();
        }
    }
}
