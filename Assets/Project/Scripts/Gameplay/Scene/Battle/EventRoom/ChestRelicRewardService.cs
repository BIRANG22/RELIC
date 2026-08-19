using System.Collections.Generic;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;

public readonly struct ChestRelicReward
{
    public ChestRelicReward(RelicData relic, RelicRarity rarity)
    {
        Relic = relic;
        Rarity = rarity;
    }

    public RelicData Relic { get; }
    public RelicRarity Rarity { get; }
    public string RelicId => Relic?.FragmentId;
    public bool IsValid => Relic != null && !string.IsNullOrWhiteSpace(Relic.FragmentId);
}

public static class ChestRelicRewardService
{
    private static readonly RelicRarity[] RevealOrder =
    {
        RelicRarity.Common,
        RelicRarity.Uncommon,
        RelicRarity.Rare,
        RelicRarity.Unique
    };

    public static IReadOnlyList<RelicRarity> BuildRevealSequence(RelicRarity selectedRarity)
    {
        int rank = RelicRarityUtility.GetRevealRank(selectedRarity);
        List<RelicRarity> sequence = new();

        for (int i = 0; i < RevealOrder.Length && i < rank; i++)
            sequence.Add(RevealOrder[i]);

        return sequence;
    }

    public static int GetRequiredClickCount(RelicRarity selectedRarity)
    {
        return GetRevealClickCount(selectedRarity);
    }

    public static int GetRevealClickCount(RelicRarity selectedRarity)
    {
        return UnityEngine.Mathf.Max(1, RelicRarityUtility.GetRevealRank(selectedRarity));
    }

    public static int GetOpenClickCount(RelicRarity selectedRarity)
    {
        return GetRevealClickCount(selectedRarity) + 1;
    }

    public static bool TryRollReward(DataManager dataManager, out ChestRelicReward reward)
    {
        reward = default;

        if (dataManager == null || dataManager.RelicDatabase == null)
            return false;

        IReadOnlyList<RelicData> allRelics = dataManager.RelicDatabase.GetAll();
        HashSet<string> unavailableRelicIds = GetUnavailableRelicIds(dataManager);
        List<RelicData> candidates = GetChestRewardCandidates(allRelics, unavailableRelicIds);

        if (candidates.Count == 0)
            return false;

        RelicData selected = candidates[BattleRandom.Range(0, candidates.Count)];
        if (selected == null || !RelicRarityUtility.TryParseChestRarity(selected.Rarity, out RelicRarity rarity))
            return false;

        reward = new ChestRelicReward(selected, rarity);
        return true;
    }

    public static bool GrantReward(DataManager dataManager, ChestRelicReward reward)
    {
        if (dataManager == null || !reward.IsValid)
            return false;

        BattleRuntimeData runtime = dataManager.BattleRuntimeStore?.GetOrCreate();
        if (runtime == null)
            return false;

        runtime.OwnedRelicIds ??= new List<string>();
        string relicId = reward.RelicId.Trim();

        if (!ContainsRelicId(runtime.OwnedRelicIds, relicId))
            runtime.OwnedRelicIds.Add(relicId);

        RecordDiscoveryService.RegisterRelic(dataManager, relicId);
        dataManager.BattleRuntimeStore.Set(runtime);
        return true;
    }

    public static List<RelicData> GetChestRewardCandidates(
        IReadOnlyList<RelicData> allRelics,
        ISet<string> unavailableRelicIds)
    {
        List<RelicData> candidates = new();

        if (allRelics == null)
            return candidates;

        for (int i = 0; i < allRelics.Count; i++)
        {
            RelicData relic = allRelics[i];
            if (relic == null || string.IsNullOrWhiteSpace(relic.FragmentId))
                continue;

            string relicId = relic.FragmentId.Trim();
            if (unavailableRelicIds != null && unavailableRelicIds.Contains(relicId))
                continue;

            if (!RelicRarityUtility.TryParseChestRarity(relic.Rarity, out RelicRarity rarity))
                continue;

            if (!RelicRarityUtility.IsChestRarity(rarity))
                continue;

            candidates.Add(relic);
        }

        return candidates;
    }

    private static HashSet<string> GetUnavailableRelicIds(DataManager dataManager)
    {
        HashSet<string> ids = new();

        if (dataManager == null)
            return ids;

        BattleRuntimeData runtime = dataManager.BattleRuntimeStore?.GetOrCreate();
        if (runtime?.OwnedRelicIds != null)
        {
            for (int i = 0; i < runtime.OwnedRelicIds.Count; i++)
                AddRelicId(ids, runtime.OwnedRelicIds[i]);
        }

        IReadOnlyDictionary<string, CharacterRuntimeData> characters = dataManager.CharacterRuntimeStore?.GetAll();
        if (characters == null)
            return ids;

        foreach (KeyValuePair<string, CharacterRuntimeData> pair in characters)
        {
            CharacterRuntimeData character = pair.Value;
            if (character?.EquippedRelicIds == null)
                continue;

            for (int i = 0; i < character.EquippedRelicIds.Length; i++)
                AddRelicId(ids, character.EquippedRelicIds[i]);
        }

        return ids;
    }

    private static void AddRelicId(HashSet<string> ids, string relicId)
    {
        if (ids == null || string.IsNullOrWhiteSpace(relicId))
            return;

        ids.Add(relicId.Trim());
    }

    private static bool ContainsRelicId(IReadOnlyList<string> relicIds, string targetRelicId)
    {
        if (relicIds == null || string.IsNullOrWhiteSpace(targetRelicId))
            return false;

        for (int i = 0; i < relicIds.Count; i++)
        {
            if (string.Equals(relicIds[i]?.Trim(), targetRelicId, System.StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
