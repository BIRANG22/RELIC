using System.Collections.Generic;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleResultChecker : MonoBehaviour
{
    public static BattleResultChecker Instance { get; private set; }

    [SerializeField] private BattleRewardResolver rewardResolver;
    [SerializeField] private BattleRewardPanelUI rewardPanel;

    private bool battleEnded;

    private void Awake()
    {
        Instance = this;
    }

    public void ResetBattle()
    {
        battleEnded = false;

        if (BattleRewardCollector.Instance != null)
            BattleRewardCollector.Instance.Clear();
    }

    public bool CheckBattleEnd()
    {
        if (battleEnded)
            return false;

        if (IsAllPlayersDead())
        {
            battleEnded = true;
            Debug.Log("[BattleResultChecker] Battle Lose");
            return true;
        }

        if (IsAllMonstersDead())
        {
            battleEnded = true;
            Debug.Log("[BattleResultChecker] Battle Win");

            OpenRewardPanel();
            return true;
        }

        return false;
    }

    private void OpenRewardPanel()
    {
        if (rewardResolver == null || rewardPanel == null)
            return;

        IReadOnlyList<MonsterRuntimeData> monsters =
            BattleRewardCollector.Instance != null
                ? BattleRewardCollector.Instance.CollectedMonsters
                : null;

        Debug.Log($"[BattleResultChecker] RewardMonsterCount:{monsters?.Count ?? 0}");

        List<BattleRewardData> rewards = rewardResolver.Resolve(monsters);

        Debug.Log($"[BattleResultChecker] ResolvedRewardCount:{rewards.Count}");

        rewardPanel.Open(rewards);
    }

    private bool IsAllMonstersDead()
    {
        MonsterUnit[] monsters = Object.FindObjectsByType<MonsterUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        int aliveCount = 0;

        for (int i = 0; i < monsters.Length; i++)
        {
            MonsterUnit monster = monsters[i];

            if (monster == null || monster.RuntimeData == null)
                continue;

            if (!monster.RuntimeData.IsDead)
                aliveCount++;
        }

        return aliveCount <= 0;
    }

    private bool IsAllPlayersDead()
    {
        BattleCharacter[] characters = Object.FindObjectsByType<BattleCharacter>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        if (characters.Length == 0)
            return false;

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == null || characters[i].RuntimeData == null)
                continue;

            if (characters[i].RuntimeData.CurrentHealth > 0)
                return false;
        }

        return true;
    }
}
