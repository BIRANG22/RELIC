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
                    ["Name"] = "가시",
                    ["Passed"] = "-1",
                    ["ValueRate"] = "5",
                    ["EffectIds"] = "E_Damage",
                    ["ToolTip"] = "피해"
                },
                new()
                {
                    ["GridEffectID"] = "",
                    ["Name"] = "",
                    ["Passed"] = "",
                    ["ValueRate"] = "",
                    ["EffectIds"] = "",
                    ["ToolTip"] = ""
                }
            }
        };

        List<GridEffectData> result = GridEffectCsvLoader.Load(workbook);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].GridEffectID, Is.EqualTo("GR_thorn"));
        Assert.That(result[0].Name, Is.EqualTo("가시"));
        Assert.That(result[0].Passed, Is.EqualTo(-1));
        Assert.That(result[0].ValueRate, Is.EqualTo(5));
        Assert.That(result[0].EffectIds, Is.EqualTo("E_Damage"));
        Assert.That(result[0].ToolTip, Is.EqualTo("피해"));
    }

    [Test]
    public void Initialize_AllowsLookupByGridEffectId()
    {
        GridEffectDatabase database = new();
        GridEffectData data = new()
        {
            GridEffectID = "GR_helmet",
            Name = "투구",
            Passed = -1,
            ValueRate = 3,
            EffectIds = "E_Armor"
        };

        database.Initialize(new[] { data });

        Assert.That(database.TryGet("GR_helmet", out GridEffectData loaded), Is.True);
        Assert.That(loaded, Is.SameAs(data));
    }
}
