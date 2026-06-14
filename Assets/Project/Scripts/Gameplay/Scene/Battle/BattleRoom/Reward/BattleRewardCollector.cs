using System.Collections.Generic;
using UnityEngine;

public class BattleRewardCollector : MonoBehaviour
{
    public static BattleRewardCollector Instance { get; private set; }

    private readonly HashSet<string> collectedMonsterRuntimeIds = new();
    private readonly List<string> collectedDropTableIds = new();

    public IReadOnlyList<string> CollectedDropTableIds => collectedDropTableIds;

    private void Awake()
    {
        Instance = this;
    }

    public void Clear()
    {
        collectedMonsterRuntimeIds.Clear();
        collectedDropTableIds.Clear();
    }

    public void CollectMonsterDrop(string monsterRuntimeId, string dropTableId)
    {
        if (string.IsNullOrWhiteSpace(monsterRuntimeId))
            return;

        if (string.IsNullOrWhiteSpace(dropTableId))
            return;

        if (collectedMonsterRuntimeIds.Contains(monsterRuntimeId))
            return;

        collectedMonsterRuntimeIds.Add(monsterRuntimeId);
        collectedDropTableIds.Add(dropTableId);

        Debug.Log($"[BattleRewardCollector] Collect / Monster:{monsterRuntimeId} / DropTable:{dropTableId}");
    }
}