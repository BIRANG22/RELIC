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
    private enum ShopRarity
    {
        Common,
        Rare,
        Epic,
        Unique
    }

    public const int DefaultTotalGoodsCount = 4;
    public const int DefaultColumnCount = 4;

    public const int CommonSkillMinPrice = 10;
    public const int CommonSkillMaxPrice = 20;
    public const int RareSkillMinPrice = 20;
    public const int RareSkillMaxPrice = 30;
    public const int EpicSkillMinPrice = 30;
    public const int EpicSkillMaxPrice = 40;
    public const int RelicMinPrice = 80;
    public const int RelicMaxPrice = 100;

    public const float DefaultCommonWeight = 30f;
    public const float DefaultRareWeight = 30f;
    public const float DefaultEpicWeight = 25f;
    public const float DefaultUniqueWeight = 15f;

    public static List<RestRoomShopGoods> CreateStock(
        IReadOnlyList<SkillMasterData> allSkills,
        IReadOnlyList<RelicData> allRelics,
        IEnumerable<string> unavailableSkillIds,
        IEnumerable<string> unavailableRelicIds,
        ISkillRewardRandom random,
        float commonWeight = DefaultCommonWeight,
        float rareWeight = DefaultRareWeight,
        float epicWeight = DefaultEpicWeight,
        float uniqueWeight = DefaultUniqueWeight)
    {
        random ??= new UnitySkillRewardRandom();

        HashSet<string> blockedSkillIds = BuildIdSet(unavailableSkillIds);
        HashSet<string> blockedRelicIds = BuildIdSet(unavailableRelicIds);
        HashSet<string> selectedSkillIds = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> selectedRelicIds = new(StringComparer.OrdinalIgnoreCase);

        // 4개가 전부 같은 종류가 되지 않도록 기억 개수를 1~3개 중 하나로 먼저 결정합니다.
        int skillCount = random.Range(1, DefaultTotalGoodsCount);
        int relicCount = DefaultTotalGoodsCount - skillCount;

        List<RestRoomShopGoodsKind> kinds = new(DefaultTotalGoodsCount);
        for (int i = 0; i < skillCount; i++)
            kinds.Add(RestRoomShopGoodsKind.Skill);
        for (int i = 0; i < relicCount; i++)
            kinds.Add(RestRoomShopGoodsKind.Relic);

        Shuffle(kinds, random);

        List<RestRoomShopGoods> stock = new(DefaultTotalGoodsCount);

        for (int i = 0; i < kinds.Count; i++)
        {
            RestRoomShopGoods goods = kinds[i] == RestRoomShopGoodsKind.Skill
                ? TryCreateSkillGoods(
                    allSkills,
                    blockedSkillIds,
                    selectedSkillIds,
                    random,
                    commonWeight,
                    rareWeight,
                    epicWeight,
                    uniqueWeight)
                : TryCreateRelicGoods(
                    allRelics,
                    blockedRelicIds,
                    selectedRelicIds,
                    random,
                    commonWeight,
                    rareWeight,
                    epicWeight,
                    uniqueWeight);

            if (goods != null)
                stock.Add(goods);
        }

        return stock;
    }

    public static int RollSkillPrice(SkillRarity rarity, ISkillRewardRandom random)
    {
        random ??= new UnitySkillRewardRandom();

        return rarity switch
        {
            SkillRarity.Rare => RollInclusive(random, RareSkillMinPrice, RareSkillMaxPrice),
            SkillRarity.Epic => RollInclusive(random, EpicSkillMinPrice, EpicSkillMaxPrice),
            SkillRarity.Unique => RollInclusive(random, EpicSkillMinPrice, EpicSkillMaxPrice),
            _ => RollInclusive(random, CommonSkillMinPrice, CommonSkillMaxPrice)
        };
    }

    public static int RollRelicPrice(ISkillRewardRandom random)
    {
        random ??= new UnitySkillRewardRandom();
        return RollInclusive(random, RelicMinPrice, RelicMaxPrice);
    }

    private static RestRoomShopGoods TryCreateSkillGoods(
        IReadOnlyList<SkillMasterData> allSkills,
        ISet<string> blockedSkillIds,
        ISet<string> selectedSkillIds,
        ISkillRewardRandom random,
        float commonWeight,
        float rareWeight,
        float epicWeight,
        float uniqueWeight)
    {
        if (!TryRollAvailableSkillRarity(
                allSkills,
                blockedSkillIds,
                selectedSkillIds,
                random,
                commonWeight,
                rareWeight,
                epicWeight,
                uniqueWeight,
                out SkillRarity rarity))
        {
            return null;
        }

        List<SkillMasterData> candidates = BuildCoreSkillCandidates(
            allSkills,
            rarity,
            blockedSkillIds,
            selectedSkillIds);

        if (candidates.Count == 0)
            return null;

        int index = Mathf.Clamp(random.Range(0, candidates.Count), 0, candidates.Count - 1);
        SkillMasterData skill = candidates[index];

        if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
            return null;

        selectedSkillIds.Add(skill.SkillId.Trim());
        return CreateSkillGoods(skill, random);
    }

    private static RestRoomShopGoods TryCreateRelicGoods(
        IReadOnlyList<RelicData> allRelics,
        ISet<string> blockedRelicIds,
        ISet<string> selectedRelicIds,
        ISkillRewardRandom random,
        float commonWeight,
        float rareWeight,
        float epicWeight,
        float uniqueWeight)
    {
        if (!TryRollAvailableRelicRarity(
                allRelics,
                blockedRelicIds,
                selectedRelicIds,
                random,
                commonWeight,
                rareWeight,
                epicWeight,
                uniqueWeight,
                out RelicRarity rarity))
        {
            return null;
        }

        List<RelicData> candidates = BuildRelicCandidates(
            allRelics,
            rarity,
            blockedRelicIds,
            selectedRelicIds);

        if (candidates.Count == 0)
            return null;

        int index = Mathf.Clamp(random.Range(0, candidates.Count), 0, candidates.Count - 1);
        RelicData relic = candidates[index];

        if (relic == null || string.IsNullOrWhiteSpace(relic.FragmentId))
            return null;

        selectedRelicIds.Add(relic.FragmentId.Trim());
        return CreateRelicGoods(relic, random);
    }

    private static bool TryRollAvailableSkillRarity(
        IReadOnlyList<SkillMasterData> allSkills,
        ISet<string> blockedSkillIds,
        ISet<string> selectedSkillIds,
        ISkillRewardRandom random,
        float commonWeight,
        float rareWeight,
        float epicWeight,
        float uniqueWeight,
        out SkillRarity rarity)
    {
        rarity = SkillRarity.None;
        List<ShopRarity> available = new(4);

        AddIfSkillRarityAvailable(available, ShopRarity.Common, SkillRarity.Common, allSkills, blockedSkillIds, selectedSkillIds);
        AddIfSkillRarityAvailable(available, ShopRarity.Rare, SkillRarity.Rare, allSkills, blockedSkillIds, selectedSkillIds);
        AddIfSkillRarityAvailable(available, ShopRarity.Epic, SkillRarity.Epic, allSkills, blockedSkillIds, selectedSkillIds);
        AddIfSkillRarityAvailable(available, ShopRarity.Unique, SkillRarity.Unique, allSkills, blockedSkillIds, selectedSkillIds);

        if (!TryRollAvailableRarity(available, random, commonWeight, rareWeight, epicWeight, uniqueWeight, out ShopRarity rolled))
            return false;

        rarity = rolled switch
        {
            ShopRarity.Rare => SkillRarity.Rare,
            ShopRarity.Epic => SkillRarity.Epic,
            ShopRarity.Unique => SkillRarity.Unique,
            _ => SkillRarity.Common
        };

        return true;
    }

    private static bool TryRollAvailableRelicRarity(
        IReadOnlyList<RelicData> allRelics,
        ISet<string> blockedRelicIds,
        ISet<string> selectedRelicIds,
        ISkillRewardRandom random,
        float commonWeight,
        float rareWeight,
        float epicWeight,
        float uniqueWeight,
        out RelicRarity rarity)
    {
        rarity = RelicRarity.None;
        List<ShopRarity> available = new(4);

        AddIfRelicRarityAvailable(available, ShopRarity.Common, RelicRarity.Common, allRelics, blockedRelicIds, selectedRelicIds);
        AddIfRelicRarityAvailable(available, ShopRarity.Rare, RelicRarity.Rare, allRelics, blockedRelicIds, selectedRelicIds);
        AddIfRelicRarityAvailable(available, ShopRarity.Epic, RelicRarity.Epic, allRelics, blockedRelicIds, selectedRelicIds);
        AddIfRelicRarityAvailable(available, ShopRarity.Unique, RelicRarity.Unique, allRelics, blockedRelicIds, selectedRelicIds);

        if (!TryRollAvailableRarity(available, random, commonWeight, rareWeight, epicWeight, uniqueWeight, out ShopRarity rolled))
            return false;

        rarity = rolled switch
        {
            ShopRarity.Rare => RelicRarity.Rare,
            ShopRarity.Epic => RelicRarity.Epic,
            ShopRarity.Unique => RelicRarity.Unique,
            _ => RelicRarity.Common
        };

        return true;
    }

    private static bool TryRollAvailableRarity(
        IReadOnlyList<ShopRarity> available,
        ISkillRewardRandom random,
        float commonWeight,
        float rareWeight,
        float epicWeight,
        float uniqueWeight,
        out ShopRarity rarity)
    {
        rarity = ShopRarity.Common;

        if (available == null || available.Count == 0)
            return false;

        commonWeight = Mathf.Max(0f, commonWeight);
        rareWeight = Mathf.Max(0f, rareWeight);
        epicWeight = Mathf.Max(0f, epicWeight);
        uniqueWeight = Mathf.Max(0f, uniqueWeight);

        float total = 0f;
        for (int i = 0; i < available.Count; i++)
            total += GetRarityWeight(available[i], commonWeight, rareWeight, epicWeight, uniqueWeight);

        if (total <= 0f)
        {
            int fallbackIndex = Mathf.Clamp(random.Range(0, available.Count), 0, available.Count - 1);
            rarity = available[fallbackIndex];
            return true;
        }

        float roll = Mathf.Clamp01(random.Value()) * total;
        float cursor = 0f;

        for (int i = 0; i < available.Count; i++)
        {
            cursor += GetRarityWeight(available[i], commonWeight, rareWeight, epicWeight, uniqueWeight);
            if (roll <= cursor)
            {
                rarity = available[i];
                return true;
            }
        }

        rarity = available[available.Count - 1];
        return true;
    }

    private static float GetRarityWeight(
        ShopRarity rarity,
        float commonWeight,
        float rareWeight,
        float epicWeight,
        float uniqueWeight)
    {
        return rarity switch
        {
            ShopRarity.Rare => rareWeight,
            ShopRarity.Epic => epicWeight,
            ShopRarity.Unique => uniqueWeight,
            _ => commonWeight
        };
    }

    private static void AddIfSkillRarityAvailable(
        List<ShopRarity> available,
        ShopRarity shopRarity,
        SkillRarity skillRarity,
        IReadOnlyList<SkillMasterData> allSkills,
        ISet<string> blockedSkillIds,
        ISet<string> selectedSkillIds)
    {
        if (BuildCoreSkillCandidates(allSkills, skillRarity, blockedSkillIds, selectedSkillIds).Count > 0)
            available.Add(shopRarity);
    }

    private static void AddIfRelicRarityAvailable(
        List<ShopRarity> available,
        ShopRarity shopRarity,
        RelicRarity relicRarity,
        IReadOnlyList<RelicData> allRelics,
        ISet<string> blockedRelicIds,
        ISet<string> selectedRelicIds)
    {
        if (BuildRelicCandidates(allRelics, relicRarity, blockedRelicIds, selectedRelicIds).Count > 0)
            available.Add(shopRarity);
    }

    private static RestRoomShopGoods CreateSkillGoods(SkillMasterData skill, ISkillRewardRandom random)
    {
        string description = SkillTooltipFormatter.BuildSkillDescription(skill, null);

        return new RestRoomShopGoods(
            RestRoomShopGoodsKind.Skill,
            skill.SkillId,
            string.IsNullOrWhiteSpace(skill.Name) ? skill.SkillId : skill.Name,
            description,
            BattleEquipmentEffectService.ModifyShopPrice(RollSkillPrice(skill.Rarity, random)),
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
            BattleEquipmentEffectService.ModifyShopPrice(RollRelicPrice(random)),
            SkillRarity.None,
            null,
            relic);
    }

    private static List<SkillMasterData> BuildCoreSkillCandidates(
        IReadOnlyList<SkillMasterData> allSkills,
        SkillRarity rarity,
        ISet<string> blockedSkillIds,
        ISet<string> selectedSkillIds)
    {
        List<SkillMasterData> candidates = new();

        if (allSkills == null)
            return candidates;

        HashSet<string> usedIds = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < allSkills.Count; i++)
        {
            SkillMasterData skill = allSkills[i];

            if (!IsShopCoreSkill(skill) || skill.Rarity != rarity)
                continue;

            string id = skill.SkillId.Trim();

            if ((blockedSkillIds != null && blockedSkillIds.Contains(id)) ||
                (selectedSkillIds != null && selectedSkillIds.Contains(id)))
            {
                continue;
            }

            if (!usedIds.Add(id))
                continue;

            candidates.Add(skill);
        }

        return candidates;
    }

    private static List<RelicData> BuildRelicCandidates(
        IReadOnlyList<RelicData> allRelics,
        RelicRarity rarity,
        ISet<string> blockedRelicIds,
        ISet<string> selectedRelicIds)
    {
        List<RelicData> candidates = new();

        if (allRelics == null)
            return candidates;

        HashSet<string> usedIds = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < allRelics.Count; i++)
        {
            RelicData relic = allRelics[i];

            if (relic == null || string.IsNullOrWhiteSpace(relic.FragmentId))
                continue;

            if (!RelicRarityUtility.TryParseChestRarity(relic.Rarity, out RelicRarity parsedRarity) ||
                parsedRarity != rarity)
            {
                continue;
            }

            string id = relic.FragmentId.Trim();

            if ((blockedRelicIds != null && blockedRelicIds.Contains(id)) ||
                (selectedRelicIds != null && selectedRelicIds.Contains(id)))
            {
                continue;
            }

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
               SkillRarityUtility.IsCoreDropRarity(skill.Rarity) &&
               SkillRarityUtility.IsBaseSkillVariant(skill.SkillId);
    }

    private static void Shuffle<T>(IList<T> list, ISkillRewardRandom random)
    {
        if (list == null || random == null)
            return;

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Mathf.Clamp(random.Range(0, i + 1), 0, i);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static int RollInclusive(ISkillRewardRandom random, int minInclusive, int maxInclusive)
    {
        if (maxInclusive < minInclusive)
            (minInclusive, maxInclusive) = (maxInclusive, minInclusive);

        return random.Range(minInclusive, maxInclusive + 1);
    }
}
