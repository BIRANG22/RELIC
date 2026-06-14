using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Relic.Gameplay.Data;
using UnityEngine;

public class BattleRewardCollector : MonoBehaviour
{
    public static BattleRewardCollector Instance { get; private set; }

    private readonly HashSet<string> collectedMonsterKeys = new();
    private readonly List<MonsterRuntimeData> collectedMonsters = new();

    public IReadOnlyList<MonsterRuntimeData> CollectedMonsters => collectedMonsters;

    private void Awake()
    {
        Instance = this;
    }

    public void Clear()
    {
        collectedMonsterKeys.Clear();
        collectedMonsters.Clear();
    }

    public void CollectMonsterReward(MonsterRuntimeData monsterData)
    {
        if (monsterData == null)
            return;

        string monsterKey = GetMonsterKey(monsterData);

        if (!collectedMonsterKeys.Add(monsterKey))
        {
            Debug.LogWarning($"[BattleRewardCollector] 이미 수집한 몬스터 보상입니다. MonsterKey:{monsterKey} / MonsterId:{monsterData.MonsterId}");
            return;
        }

        collectedMonsters.Add(monsterData);

        Debug.Log($"[BattleRewardCollector] Collect / MonsterKey:{monsterKey} / MonsterId:{monsterData.MonsterId}");
    }

    private string GetMonsterKey(MonsterRuntimeData monsterData)
    {
        if (monsterData == null)
            return "Monster:null";

        if (!string.IsNullOrWhiteSpace(monsterData.RuntimeId))
            return $"Runtime:{monsterData.RuntimeId.Trim()}";

        return $"Reference:{RuntimeHelpers.GetHashCode(monsterData)}:{monsterData.MonsterId}";
    }
}
