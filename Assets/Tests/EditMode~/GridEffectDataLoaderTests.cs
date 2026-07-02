using System.Collections.Generic;
using NUnit.Framework;
using Relic.Gameplay.Data;

public class GridEffectDataLoaderTests
{
    [Test]
    public void Load_MapsGridEffectSheetAndSkipsBlankRows()
    {
        Dictionary<string, List<Dictionary<string, string>>> workbook = new()
        {
            ["GridEffect"] = new List<Dictionary<string, string>>
            {
                new()
                {
                    ["GridEffectID"] = "GR_thorn",
                    ["Name"] = "Thorn",
                    ["Passed"] = "1",
                    ["Consumable"] = "1",
                    ["ValueRate"] = "5",
                    ["EffectIds"] = "E_Damage",
                    ["ToolTip"] = "Damage on pass"
                },
                new()
                {
                    ["GridEffectID"] = "",
                    ["Name"] = "",
                    ["Passed"] = "",
                    ["Consumable"] = "",
                    ["ValueRate"] = "",
                    ["EffectIds"] = "",
                    ["ToolTip"] = ""
                }
            }
        };

        List<GridEffectData> result = GridEffectCsvLoader.Load(workbook);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].GridEffectID, Is.EqualTo("GR_thorn"));
        Assert.That(result[0].Name, Is.EqualTo("Thorn"));
        Assert.That(result[0].Passed, Is.EqualTo(1));
        Assert.That(result[0].Consumable, Is.EqualTo(1));
        Assert.That(result[0].ValueRate, Is.EqualTo(5));
        Assert.That(result[0].EffectIds, Is.EqualTo("E_Damage"));
        Assert.That(result[0].ToolTip, Is.EqualTo("Damage on pass"));
    }

    [Test]
    public void Load_MapsKoreanColumnAliases()
    {
        Dictionary<string, List<Dictionary<string, string>>> workbook = new()
        {
            ["GridEffect"] = new List<Dictionary<string, string>>
            {
                new()
                {
                    ["GridEffectID"] = "GR_debris",
                    ["Name"] = "Debris",
                    ["\uD1B5\uACFC\uC720\uBB34"] = "0",
                    ["\uC18C\uBAA8\uC131"] = "0",
                    ["ValueRate"] = "0",
                    ["EffectIds"] = ""
                }
            }
        };

        List<GridEffectData> result = GridEffectCsvLoader.Load(workbook);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Passed, Is.EqualTo(0));
        Assert.That(result[0].Consumable, Is.EqualTo(0));
    }

    [Test]
    public void Load_MapsExpendableAliasToConsumable()
    {
        Dictionary<string, List<Dictionary<string, string>>> workbook = new()
        {
            ["GridEffect"] = new List<Dictionary<string, string>>
            {
                new()
                {
                    ["GridEffectID"] = "GR_helmet",
                    ["Name"] = "Helmet",
                    ["Passed"] = "1",
                    ["expendable"] = "1",
                    ["ValueRate"] = "3",
                    ["EffectIds"] = "E_Armor"
                }
            }
        };

        List<GridEffectData> result = GridEffectCsvLoader.Load(workbook);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Consumable, Is.EqualTo(1));
    }

    [Test]
    public void Initialize_AllowsLookupByGridEffectId()
    {
        GridEffectDatabase database = new();
        GridEffectData data = new()
        {
            GridEffectID = "GR_helmet",
            Name = "Helmet",
            Passed = 1,
            Consumable = 0,
            ValueRate = 3,
            EffectIds = "E_Armor"
        };

        database.Initialize(new[] { data });

        Assert.That(database.TryGet("GR_helmet", out GridEffectData loaded), Is.True);
        Assert.That(loaded, Is.SameAs(data));
    }
}
