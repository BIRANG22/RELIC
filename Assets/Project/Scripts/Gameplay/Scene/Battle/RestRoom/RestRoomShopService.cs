using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public enum RestRoomShopGoodsKind
{
    Skill,
    Relic
}

public sealed class RestRoomShopGoods
{
    public RestRoomShopGoods(
        RestRoomShopGoodsKind kind,
        string id,
        string displayName,
        string description,
        int price,
        SkillRarity skillRarity = SkillRarity.None,
        SkillMasterData skill = null,
        RelicData relic = null,
        Sprite icon = null)
    {
        Kind = kind;
        Id = string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
        Description = description ?? string.Empty;
        Price = Mathf.Max(0, price);
        SkillRarity = skillRarity;
        Skill = skill;
        Relic = relic;
        Icon = icon;
    }

    public RestRoomShopGoodsKind Kind { get; }
    public string Id { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public int Price { get; }
    public SkillRarity SkillRarity { get; }
    public SkillMasterData Skill { get; }
    public RelicData Relic { get; }
    public Sprite Icon { get; set; }
}

public static class RestRoomShopService
{
    public const int DefaultTotalGoodsCount = 8;
    public const int DefaultSkillGoodsCount = 4;
    public const int DefaultColumnCount = 4;

    public const int CommonSkillMinPrice = 10;
    public const int CommonSkillMaxPrice = 20;
    public const int RareSkillMinPrice = 20;
    public const int RareSkillMaxPrice = 30;
    public const int EpicSkillMinPrice = 30;
    public const int EpicSkillMaxPrice = 40;
    public const int RelicMinPrice = 80;
    public const int RelicMaxPrice = 100;

    public const float DefaultCommonWeight = 60f;
    public const float DefaultRareWeight = 30f;
    public const float DefaultEpicWeight = 10f;

    public static List<RestRoomShopGoods> CreateStock(
        IReadOnlyList<SkillMasterData> allSkills,
        IReadOnlyList<RelicData> allRelics,
        IEnumerable<string> unavailableRelicIds,
        ISkillRewardRandom random,
        int totalCount = DefaultTotalGoodsCount,
        int skillCount = DefaultSkillGoodsCount,
        float commonWeight = DefaultCommonWeight,
        float rareWeight = DefaultRareWeight,
        float epicWeight = DefaultEpicWeight)
    {
        random ??= new UnitySkillRewardRandom();

        totalCount = Math.Max(0, totalCount);
        skillCount = Math.Min(Math.Max(0, skillCount), totalCount);

        List<RestRoomShopGoods> stock = new();
        HashSet<string> selectedSkillIds = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < skillCount && stock.Count < totalCount; i++)
        {
            if (!TryRollCoreSkill(
                    allSkills,
                    random,
                    commonWeight,
                    rareWeight,
                    epicWeight,
                    selectedSkillIds,
                    out SkillMasterData skill))
            {
                break;
            }

            selectedSkillIds.Add(skill.SkillId.Trim());
            stock.Add(CreateSkillGoods(skill, random));
        }

        int relicTargetCount = totalCount - stock.Count;
        AddRelicGoods(stock, allRelics, unavailableRelicIds, random, relicTargetCount);

        while (stock.Count < totalCount)
        {
            if (!TryRollCoreSkill(
                    allSkills,
                    random,
                    commonWeight,
                    rareWeight,
                    epicWeight,
                    selectedSkillIds,
                    out SkillMasterData skill))
            {
                break;
            }

            selectedSkillIds.Add(skill.SkillId.Trim());
            stock.Add(CreateSkillGoods(skill, random));
        }

        return stock;
    }

    public static bool TryRollCoreSkill(
        IReadOnlyList<SkillMasterData> allSkills,
        ISkillRewardRandom random,
        float commonWeight,
        float rareWeight,
        float epicWeight,
        ISet<string> blockedSkillIds,
        out SkillMasterData skill)
    {
        skill = null;

        random ??= new UnitySkillRewardRandom();

        SkillRarity rarity = RollCoreSkillRarity(commonWeight, rareWeight, epicWeight, random);
        List<SkillMasterData> candidates = BuildCoreSkillCandidates(allSkills, rarity, blockedSkillIds);

        if (candidates.Count == 0)
            candidates = BuildCoreSkillCandidates(allSkills, null, blockedSkillIds);

        if (candidates.Count == 0)
            return false;

        int index = random.Range(0, candidates.Count);
        skill = candidates[Mathf.Clamp(index, 0, candidates.Count - 1)];
        return skill != null;
    }

    public static SkillRarity RollCoreSkillRarity(
        float commonWeight,
        float rareWeight,
        float epicWeight,
        ISkillRewardRandom random)
    {
        random ??= new UnitySkillRewardRandom();

        commonWeight = Mathf.Max(0f, commonWeight);
        rareWeight = Mathf.Max(0f, rareWeight);
        epicWeight = Mathf.Max(0f, epicWeight);

        float totalWeight = commonWeight + rareWeight + epicWeight;

        if (totalWeight <= 0f)
            return SkillRarity.CoreCommon;

        float roll = Mathf.Clamp01(random.Value()) * totalWeight;

        if (roll < commonWeight)
            return SkillRarity.CoreCommon;

        if (roll < commonWeight + rareWeight)
            return SkillRarity.CoreRare;

        return SkillRarity.CoreEpic;
    }

    public static int RollSkillPrice(SkillRarity rarity, ISkillRewardRandom random)
    {
        random ??= new UnitySkillRewardRandom();

        return rarity switch
        {
            SkillRarity.CoreRare => RollInclusive(random, RareSkillMinPrice, RareSkillMaxPrice),
            SkillRarity.CoreEpic => RollInclusive(random, EpicSkillMinPrice, EpicSkillMaxPrice),
            _ => RollInclusive(random, CommonSkillMinPrice, CommonSkillMaxPrice)
        };
    }

    public static int RollRelicPrice(ISkillRewardRandom random)
    {
        random ??= new UnitySkillRewardRandom();
        return RollInclusive(random, RelicMinPrice, RelicMaxPrice);
    }

    private static RestRoomShopGoods CreateSkillGoods(SkillMasterData skill, ISkillRewardRandom random)
    {
        string description = SkillTooltipFormatter.BuildSkillDescription(skill, null);

        return new RestRoomShopGoods(
            RestRoomShopGoodsKind.Skill,
            skill.SkillId,
            string.IsNullOrWhiteSpace(skill.Name) ? skill.SkillId : skill.Name,
            description,
            RollSkillPrice(skill.Rarity, random),
            skill.Rarity,
            skill,
            null,
            skill.Icon);
    }

    private static RestRoomShopGoods CreateRelicGoods(RelicData relic, ISkillRewardRandom random)
    {
        return new RestRoomShopGoods(
            RestRoomShopGoodsKind.Relic,
            relic.FragmentId,
            string.IsNullOrWhiteSpace(relic.Name) ? relic.FragmentId : relic.Name,
            relic.EffectDesc,
            RollRelicPrice(random),
            SkillRarity.None,
            null,
            relic);
    }

    private static void AddRelicGoods(
        List<RestRoomShopGoods> stock,
        IReadOnlyList<RelicData> allRelics,
        IEnumerable<string> unavailableRelicIds,
        ISkillRewardRandom random,
        int maxCount)
    {
        if (stock == null || maxCount <= 0)
            return;

        List<RelicData> candidates = BuildRelicCandidates(allRelics, unavailableRelicIds);

        for (int i = 0; i < maxCount && candidates.Count > 0; i++)
        {
            int index = random.Range(0, candidates.Count);
            index = Mathf.Clamp(index, 0, candidates.Count - 1);

            RelicData relic = candidates[index];
            candidates.RemoveAt(index);

            stock.Add(CreateRelicGoods(relic, random));
        }
    }

    private static List<SkillMasterData> BuildCoreSkillCandidates(
        IReadOnlyList<SkillMasterData> allSkills,
        SkillRarity? rarity,
        ISet<string> blockedSkillIds)
    {
        List<SkillMasterData> candidates = new();

        if (allSkills == null)
            return candidates;

        HashSet<string> usedIds = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < allSkills.Count; i++)
        {
            SkillMasterData skill = allSkills[i];

            if (!IsShopCoreSkill(skill))
                continue;

            if (rarity.HasValue && skill.Rarity != rarity.Value)
                continue;

            string id = skill.SkillId.Trim();

            if (blockedSkillIds != null && blockedSkillIds.Contains(id))
                continue;

            if (!usedIds.Add(id))
                continue;

            candidates.Add(skill);
        }

        return candidates;
    }

    private static List<RelicData> BuildRelicCandidates(
        IReadOnlyList<RelicData> allRelics,
        IEnumerable<string> unavailableRelicIds)
    {
        List<RelicData> candidates = new();

        if (allRelics == null)
            return candidates;

        HashSet<string> unavailableIds = BuildIdSet(unavailableRelicIds);
        HashSet<string> usedIds = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < allRelics.Count; i++)
        {
            RelicData relic = allRelics[i];

            if (relic == null || string.IsNullOrWhiteSpace(relic.FragmentId))
                continue;

            string id = relic.FragmentId.Trim();

            if (unavailableIds.Contains(id))
                continue;

            if (!usedIds.Add(id))
                continue;

            candidates.Add(relic);
        }

        return candidates;
    }

    private static HashSet<string> BuildIdSet(IEnumerable<string> ids)
    {
        HashSet<string> set = new(StringComparer.OrdinalIgnoreCase);

        if (ids == null)
            return set;

        foreach (string id in ids)
        {
            if (!string.IsNullOrWhiteSpace(id))
                set.Add(id.Trim());
        }

        return set;
    }

    private static bool IsShopCoreSkill(SkillMasterData skill)
    {
        return skill != null &&
               !string.IsNullOrWhiteSpace(skill.SkillId) &&
               skill.Category == Category.Core &&
               skill.SkillId.Trim().StartsWith("S_Core_", StringComparison.OrdinalIgnoreCase) &&
               SkillRarityUtility.IsCoreDropRarity(skill.Rarity) &&
               SkillRarityUtility.IsBaseSkillVariant(skill.SkillId);
    }

    private static int RollInclusive(ISkillRewardRandom random, int minInclusive, int maxInclusive)
    {
        if (maxInclusive < minInclusive)
            (minInclusive, maxInclusive) = (maxInclusive, minInclusive);

        return random.Range(minInclusive, maxInclusive + 1);
    }
}
