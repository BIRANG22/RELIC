using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public class BattleRewardResolver : MonoBehaviour
{
    public List<BattleRewardData> Resolve(IReadOnlyList<string> dropTableIds)
    {
        List<BattleRewardData> rewards = new();

        Debug.Log($"[BattleRewardResolver] Resolve Start / DropTableCount:{dropTableIds.Count}");

        if (dropTableIds == null || DataManager.Instance == null)
            return rewards;

        for (int i = 0; i < dropTableIds.Count; i++)
        {
            string dropTableId = dropTableIds[i];

            Debug.Log($"[BattleRewardResolver] DropTableId:{dropTableId}");

            List<RewardTableData> entries =
                DataManager.Instance.RewardTableDatabase.GetEntries(dropTableId);

            Debug.Log($"[BattleRewardResolver] EntryCount:{entries.Count}");

            for (int j = 0; j < entries.Count; j++)
            {
                TryResolveEntry(entries[j], rewards);
            }
        }

        return rewards;
    }

    private void TryResolveEntry(RewardTableData entry, List<BattleRewardData> rewards)
    {
        if (entry == null)
            return;

        if (Random.value > entry.Chance)
            return;

        switch (entry.DropType)
        {
            case "Remnant":
                AddRemnant(entry, rewards);
                break;

            case "Item":
                AddItem(entry, rewards);
                break;

            case "Relic":
                AddRelic(entry, rewards);
                break;
        }
    }

    private void AddRemnant(RewardTableData entry, List<BattleRewardData> rewards)
    {
        int amount = Random.Range(entry.MinAmount, entry.MaxAmount + 1);

        rewards.Add(new BattleRewardData
        {
            Type = BattleRewardType.Remnant,
            RewardId = "0",
            Amount = amount,
            Name = "Remnant"
        });
    }

    private void AddItem(RewardTableData entry, List<BattleRewardData> rewards)
    {
        if (string.IsNullOrWhiteSpace(entry.DropId))
            return;

        ItemData item = DataManager.Instance.ItemDatabase.Get(entry.DropId);

        Sprite icon = null;

        if (DataManager.Instance.ItemIconDatabase != null)
            DataManager.Instance.ItemIconDatabase.TryGetIcon(entry.DropId, out icon);

        rewards.Add(new BattleRewardData
        {
            Type = BattleRewardType.Item,
            RewardId = entry.DropId,
            Amount = 1,
            Icon = icon,
            Name = item != null ? item.Name : entry.DropId
        });
    }

    private void AddRelic(RewardTableData entry, List<BattleRewardData> rewards)
    {
        RelicData relic = GetRandomRelic();

        if (relic == null)
            return;

        Sprite icon = null;

        if (DataManager.Instance.RelicIconDatabase != null)
            DataManager.Instance.RelicIconDatabase.TryGetIcon(relic.FragmentId, out icon);

        rewards.Add(new BattleRewardData
        {
            Type = BattleRewardType.Relic,
            RewardId = relic.FragmentId,
            Amount = 1,
            Icon = icon,
            Name = relic.Name
        });
    }

    private RelicData GetRandomRelic()
    {
        IReadOnlyList<RelicData> allRelics =
            DataManager.Instance.RelicDatabase.GetAll();

        if (allRelics == null || allRelics.Count == 0)
            return null;

        return allRelics[Random.Range(0, allRelics.Count)];
    }
}