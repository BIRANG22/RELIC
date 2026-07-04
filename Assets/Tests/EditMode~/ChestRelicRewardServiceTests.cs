using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public class ChestRelicRewardServiceTests
{
    [Test]
    public void RelicCsvLoader_MapsKoreanRarityColumn()
    {
        var workbook = new Dictionary<string, List<Dictionary<string, string>>>
        {
            ["Relic"] = new()
            {
                new Dictionary<string, string>
                {
                    ["FragmentId"] = "Relic_99",
                    ["Name"] = "테스트 유물",
                    ["레어도"] = "레어"
                }
            }
        };

        List<RelicData> relics = RelicCsvLoader.Load(workbook);

        Assert.That(relics, Has.Count.EqualTo(1));
        Assert.That(relics[0].Rarity, Is.EqualTo("레어"));
        Assert.That(RelicRarityUtility.TryParseChestRarity(relics[0].Rarity, out RelicRarity rarity), Is.True);
        Assert.That(rarity, Is.EqualTo(RelicRarity.Rare));
    }

    [Test]
    public void BuildRevealSequence_ContainsCommonThroughSelectedRarity()
    {
        IReadOnlyList<RelicRarity> sequence = ChestRelicRewardService.BuildRevealSequence(RelicRarity.Rare);

        Assert.That(sequence, Is.EqualTo(new[]
        {
            RelicRarity.Common,
            RelicRarity.Uncommon,
            RelicRarity.Rare
        }));
    }

    [Test]
    public void GetOpenClickCount_WaitsOneClickAfterFinalRarityReveal()
    {
        Assert.That(ChestRelicRewardService.GetRevealClickCount(RelicRarity.Rare), Is.EqualTo(3));
        Assert.That(ChestRelicRewardService.GetOpenClickCount(RelicRarity.Rare), Is.EqualTo(4));
    }

    [Test]
    public void GetOpenClickCount_UniqueRequiresFourRevealClicksAndOneOpenClick()
    {
        Assert.That(ChestRelicRewardService.GetRevealClickCount(RelicRarity.Unique), Is.EqualTo(4));
        Assert.That(ChestRelicRewardService.GetOpenClickCount(RelicRarity.Unique), Is.EqualTo(5));
    }

    [Test]
    public void GetChestRewardCandidates_FiltersChestRarityAndUnavailableRelics()
    {
        var relics = new List<RelicData>
        {
            new() { FragmentId = "Relic_01", Rarity = "Start" },
            new() { FragmentId = "Relic_02", Rarity = "Shop" },
            new() { FragmentId = "Relic_03", Rarity = "Common" },
            new() { FragmentId = "Relic_04", Rarity = "Uncommon" },
            new() { FragmentId = "Relic_05", Rarity = "Rare" },
            new() { FragmentId = "Relic_06", Rarity = "Unique" }
        };

        var unavailable = new HashSet<string> { "Relic_04" };

        List<RelicData> candidates = ChestRelicRewardService.GetChestRewardCandidates(relics, unavailable);

        Assert.That(candidates.ConvertAll(x => x.FragmentId), Is.EqualTo(new[]
        {
            "Relic_03",
            "Relic_05",
            "Relic_06"
        }));
    }
}
