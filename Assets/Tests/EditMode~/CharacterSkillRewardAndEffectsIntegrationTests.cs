using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;

public class CharacterSkillRewardAndEffectsIntegrationTests
{
    private sealed class SequenceRandom : ISkillRewardRandom
    {
        private readonly Queue<float> values = new();
        private readonly Queue<int> ranges = new();

        public SequenceRandom(IEnumerable<float> valueSequence, IEnumerable<int> rangeSequence)
        {
            foreach (float value in valueSequence) values.Enqueue(value);
            foreach (int value in rangeSequence) ranges.Enqueue(value);
        }

        public float Value() => values.Dequeue();
        public int Range(int minInclusive, int maxExclusive) => ranges.Dequeue();
    }

    [Test]
    public void RewardOffers_RollRarityIndependentlyAndRenormalizeAvailableRarities()
    {
        BattleMapData settings = new()
        {
            MemoryRareChance = 10,
            MemoryEpicChance = 60,
            MemoryUniqueChance = 30
        };
        List<SkillMasterData> candidates = new()
        {
            RewardSkill("Rare_1", SkillRarity.Rare),
            RewardSkill("Rare_2", SkillRarity.Rare),
            RewardSkill("Rare_3", SkillRarity.Rare),
            RewardSkill("Epic_1", SkillRarity.Epic),
            RewardSkill("Unique_1", SkillRarity.Unique)
        };
        SequenceRandom random = new(new[] { 0.05f, 0.20f, 0.90f }, new[] { 2, 0, 0 });

        List<SkillMasterData> offers = SkillRewardRoller.RollOffersWithoutReplacement(
            settings, candidates, 3, random);

        Assert.That(offers, Has.Count.EqualTo(3));
        Assert.That(offers.ConvertAll(skill => skill.Rarity), Is.EqualTo(new[]
        {
            SkillRarity.Rare, SkillRarity.Epic, SkillRarity.Unique
        }));
        Assert.That(offers[0].SkillId, Is.EqualTo("Rare_3"));

        List<SkillMasterData> sparseCandidates = new()
        {
            RewardSkill("Sparse_Rare", SkillRarity.Rare),
            RewardSkill("Sparse_Unique", SkillRarity.Unique)
        };
        SequenceRandom fallbackRandom = new(new[] { 0.50f, 0.50f }, new[] { 0, 0 });

        List<SkillMasterData> fallbackOffers = SkillRewardRoller.RollOffersWithoutReplacement(
            settings, sparseCandidates, 2, fallbackRandom);

        Assert.That(fallbackOffers.ConvertAll(skill => skill.Rarity), Is.EqualTo(new[]
        {
            SkillRarity.Unique, SkillRarity.Rare
        }));
    }

    [TestCase("X", 0, "X")]
    [TestCase("3X", 0, "3X")]
    [TestCase("", 4, "4")]
    public void RewardCostDisplay_PrefersFormula(string formula, int numericCost, string expected)
    {
        SkillMasterData skill = new()
        {
            ResourceCostFormula = formula,
            ResourceCostValue = numericCost
        };

        Assert.That(BattleRewardSkillPanelUI.GetResourceCostDisplayText(skill), Is.EqualTo(expected));
    }

    [Test]
    public void CompatibleReward_XReservation_ResultAndPermanentGrowth_KeepStableIds()
    {
        SkillMasterData skill = new()
        {
            SkillId = "S_Core_01",
            CharacterId = "Char_01",
            Category = Category.Core,
            Rarity = SkillRarity.Common,
            ReferenceResource = ReferenceResource.Cost,
            ResourceCostFormula = "3X",
            EffectIds = "E_Strike;E_ValueUpOnKill",
            ValueRate = "2X;2",
            CountRate = "1;1"
        };

        IReadOnlyList<SkillMasterData> candidates = SkillRewardRoller.GetCandidates(
            new[] { skill },
            SkillRarity.Common,
            new[] { "Char_01" });
        Assert.That(candidates, Has.Count.EqualTo(1));

        CharacterRuntimeData caster = new() { CharacterId = "Char_01", CurrentCost = 11 };
        PlayerReservedCommand command = new(caster, skill);
        Assert.That(command.ResolvedX, Is.EqualTo(3));
        Assert.That(command.ResolvedEffectValues[0], Is.EqualTo(6));

        command.ExecutionResult.RecordImpact(new SkillImpactResult
        {
            TargetRuntimeId = "Monster_Runtime_01",
            TargetGridIndex = 12,
            Attempted = true,
            Hit = true,
            DamageAmount = 6,
            Killed = true
        });

        SkillRuntimeStore store = new();
        store.AddPermanentValueBonus(
            command.CharacterId,
            command.SkillId,
            NewSkillEffectRules.ResolvePermanentValueBonus(
                command.ExecutionResult.KillCount,
                2));

        Assert.That(command.ExecutionResult.KillCount, Is.EqualTo(1));
        Assert.That(store.GetPermanentValueBonus("Char_01", "S_Core_01"), Is.EqualTo(2));
    }

    [Test]
    public void BattleRandom_SameSeedProducesSameTargetOrder()
    {
        string[] targets = { "M1", "M2", "M3" };
        List<string> first = PickSequence(targets, 8675309, 8);
        List<string> second = PickSequence(targets, 8675309, 8);

        Assert.That(second, Is.EqualTo(first));
    }

    private static List<string> PickSequence(
        IReadOnlyList<string> targets,
        int seed,
        int count)
    {
        BattleRandom.SetSeed(seed);
        List<string> result = new();
        for (int i = 0; i < count; i++)
            result.Add(BattleRandom.Pick(targets));
        BattleRandom.ClearSeed();
        return result;
    }

    private static SkillMasterData RewardSkill(string id, SkillRarity rarity) => new()
    {
        SkillId = id,
        Category = Category.Core,
        Rarity = rarity
    };
}
