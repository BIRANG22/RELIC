using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleDebugKillAllMonsters : MonoBehaviour
{
    [SerializeField] private KeyCode killKey = KeyCode.K;

    private void Update()
    {
        if (!Input.GetKeyDown(killKey))
            return;

        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null || monster.RuntimeData == null)
                continue;

            monster.RuntimeData.CurrentHP = 0;
            monster.RuntimeData.CurrentShield = 0;

            if (BattleRewardCollector.Instance != null)
                BattleRewardCollector.Instance.CollectMonsterReward(monster.RuntimeData);

            monster.RefreshHUD();

            Debug.Log($"[DebugKill] Monster:{monster.RuntimeData.MonsterId}");
        }

        if (BattleResultChecker.Instance != null)
            BattleResultChecker.Instance.CheckBattleEnd();
    }
}

