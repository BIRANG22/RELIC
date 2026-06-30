using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class RestRoomShopTests
{
    [Test]
    public void CreateStock_BuildsEightGoodsWithFourSkillsAndFourAvailableRelics()
    {
        List<SkillMasterData> skills = new()
        {
            CreateSkill("S_Core_01", SkillRarity.CoreCommon),
            CreateSkill("S_Core_03", SkillRarity.CoreCommon),
            CreateSkill("S_Core_05", SkillRarity.CoreCommon),
            CreateSkill("S_Core_07", SkillRarity.CoreCommon)
        };

        List<RelicData> relics = new()
        {
            CreateRelic("R_001"),
            CreateRelic("R_002"),
            CreateRelic("R_003"),
            CreateRelic("R_004"),
            CreateRelic("R_005"),
            CreateRelic("R_006")
        };

        HashSet<string> unavailableRelics = new() { "R_001", "R_002" };
        SequenceRestRoomShopRandom random = new(Array.Empty<float>(), Array.Empty<int>());

        List<RestRoomShopGoods> stock = RestRoomShopService.CreateStock(
            skills,
            relics,
            unavailableRelics,
            random);

        Assert.That(stock, Has.Count.EqualTo(8));
        Assert.That(stock.Count(goods => goods.Kind == RestRoomShopGoodsKind.Skill), Is.EqualTo(4));
        Assert.That(stock.Count(goods => goods.Kind == RestRoomShopGoodsKind.Relic), Is.EqualTo(4));
        Assert.That(stock.Where(goods => goods.Kind == RestRoomShopGoodsKind.Relic).Select(goods => goods.Id),
            Is.EquivalentTo(new[] { "R_003", "R_004", "R_005", "R_006" }));
        Assert.That(stock.Where(goods => goods.Kind == RestRoomShopGoodsKind.Skill),
            Has.All.Matches<RestRoomShopGoods>(goods => goods.Price >= 10 && goods.Price <= 20));
        Assert.That(stock.Where(goods => goods.Kind == RestRoomShopGoodsKind.Relic),
            Has.All.Matches<RestRoomShopGoods>(goods => goods.Price >= 80 && goods.Price <= 100));
    }

    [Test]
    public void TryRollCoreSkill_UsesRarityWeightsAndBaseSCoreFilter()
    {
        List<SkillMasterData> skills = new()
        {
            CreateSkill("S_Core_01", SkillRarity.CoreCommon),
            CreateSkill("S_Core_09", SkillRarity.CoreRare),
            CreateSkill("S_Core_10", SkillRarity.CoreRare),
            CreateSkill("S_Public_01", SkillRarity.Shared, Category.Public),
            CreateSkill("S_Core_15", SkillRarity.CoreEpic)
        };

        SequenceRestRoomShopRandom random = new(new[] { 0f }, new[] { 0 });

        bool rolled = RestRoomShopService.TryRollCoreSkill(
            skills,
            random,
            commonWeight: 0f,
            rareWeight: 1f,
            epicWeight: 0f,
            blockedSkillIds: null,
            out SkillMasterData skill);

        Assert.That(rolled, Is.True);
        Assert.That(skill.SkillId, Is.EqualTo("S_Core_09"));
    }

    [Test]
    public void RollSkillPrice_UsesRarityRanges()
    {
        SequenceRestRoomShopRandom random = new(
            Array.Empty<float>(),
            new[] { 10, 20, 20, 30, 30, 40 });

        Assert.That(RestRoomShopService.RollSkillPrice(SkillRarity.CoreCommon, random), Is.EqualTo(10));
        Assert.That(RestRoomShopService.RollSkillPrice(SkillRarity.CoreCommon, random), Is.EqualTo(20));
        Assert.That(RestRoomShopService.RollSkillPrice(SkillRarity.CoreRare, random), Is.EqualTo(20));
        Assert.That(RestRoomShopService.RollSkillPrice(SkillRarity.CoreRare, random), Is.EqualTo(30));
        Assert.That(RestRoomShopService.RollSkillPrice(SkillRarity.CoreEpic, random), Is.EqualTo(30));
        Assert.That(RestRoomShopService.RollSkillPrice(SkillRarity.CoreEpic, random), Is.EqualTo(40));
    }

    [Test]
    public void RestRoomShopPanel_DefaultLayoutPlacesRowsAtRequestedY()
    {
        GameObject panelObject = new("RestRoomShopPanel");

        try
        {
            RestRoomShopPanel panel = panelObject.AddComponent<RestRoomShopPanel>();
            MethodInfo method = typeof(RestRoomShopPanel)
                .GetMethod("CalculateAnchoredPosition", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);

            Vector2 firstRowPosition = (Vector2)method.Invoke(panel, new object[] { 0 });
            Vector2 secondRowPosition = (Vector2)method.Invoke(panel, new object[] { 4 });

            Assert.That(firstRowPosition.y, Is.EqualTo(120f));
            Assert.That(secondRowPosition.y, Is.EqualTo(-200f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(panelObject);
        }
    }

    [Test]
    public void BattleRuntimeStore_GetOrCreateStartsNewRunWithOneHundredRemnant()
    {
        BattleRuntimeStore store = new();

        BattleRuntimeData runtime = store.GetOrCreate();

        Assert.That(runtime.Remnant, Is.EqualTo(100));
    }

    private static SkillMasterData CreateSkill(
        string skillId,
        SkillRarity rarity,
        Category category = Category.Core)
    {
        return new SkillMasterData
        {
            SkillId = skillId,
            Name = skillId,
            Category = category,
            Rarity = rarity
        };
    }

    private static RelicData CreateRelic(string relicId)
    {
        return new RelicData
        {
            FragmentId = relicId,
            Name = relicId,
            EffectDesc = relicId
        };
    }

    private sealed class SequenceRestRoomShopRandom : ISkillRewardRandom
    {
        private readonly Queue<float> values;
        private readonly Queue<int> ranges;

        public SequenceRestRoomShopRandom(IEnumerable<float> values, IEnumerable<int> ranges)
        {
            this.values = new Queue<float>(values);
            this.ranges = new Queue<int>(ranges);
        }

        public float Value()
        {
            return values.Count > 0 ? values.Dequeue() : 0f;
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (ranges.Count <= 0)
                return minInclusive;

            int value = ranges.Dequeue();

            if (value < minInclusive)
                return minInclusive;

            if (value >= maxExclusive)
                return maxExclusive - 1;

            return value;
        }
    }
}
