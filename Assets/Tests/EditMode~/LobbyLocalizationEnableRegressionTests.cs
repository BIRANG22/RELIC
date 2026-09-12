using System;
using System.IO;
using NUnit.Framework;

public sealed class LobbyLocalizationEnableRegressionTests
{
    [TestCase(LobbyWorldObjectHoverName.HoverNameType.Research, "lobby.world_object.workbench")]
    [TestCase(LobbyWorldObjectHoverName.HoverNameType.Exploration, "lobby.world_object.statue")]
    [TestCase(LobbyWorldObjectHoverName.HoverNameType.Resonance, "lobby.world_object.stela")]
    [TestCase(LobbyWorldObjectHoverName.HoverNameType.Npc, "lobby.world_object.researcher_elric")]
    public void WorldObjectHoverName_UsesStableLocalizationKey(
        LobbyWorldObjectHoverName.HoverNameType hoverNameType,
        string expectedKey)
    {
        Assert.That(LobbyWorldObjectHoverName.GetDisplayNameKey(hoverNameType), Is.EqualTo(expectedKey));
    }

    [TestCase("Assets/Project/Scenes/YDM/Lobby.unity")]
    [TestCase("Assets/Project/PrefabsR/ErosionSlot.prefab")]
    [TestCase("Assets/Project/PrefabsR/Equip_panel.prefab")]
    public void TargetLocalizationAssets_DoNotContainDisabledRuntimeLocalizers(string assetPath)
    {
        string yaml = File.ReadAllText(assetPath);
        Assert.That(yaml, Does.Not.Match(
            "(?s)--- !u!114 (?:(?!--- !u!114 ).)*?m_Enabled: 0(?:(?!--- !u!114 ).)*?guid: 700ed754fb422984990c407c26f0065c"),
            $"{assetPath} contains a disabled LocalizedTMPText component");
    }

    [Test]
    public void WorldObjectHoverNameKeys_ArePresentInEveryStringTable()
    {
        string[] keys =
        {
            "lobby.world_object.workbench",
            "lobby.world_object.statue",
            "lobby.world_object.stela",
            "lobby.world_object.researcher_elric",
        };
        string shared = File.ReadAllText("Assets/Language/Text Shared Data.asset");
        string[] tables =
        {
            "Assets/Language/Text_ko.asset",
            "Assets/Language/Text_en.asset",
            "Assets/Language/Text_ja.asset",
            "Assets/Language/Text_zh-Hans.asset",
            "Assets/Language/Text_es.asset",
        };

        for (int index = 0; index < keys.Length; index++)
        {
            string key = keys[index];
            string id = (11012039836704772L + index).ToString();
            StringAssert.Contains(key, shared);
            foreach (string table in tables)
                StringAssert.Contains("m_Id: " + id, File.ReadAllText(table), table);
        }
    }

    [Test]
    public void TutorialDialogue_UsesDynamicLocalizationKeysAndKoreanEntries()
    {
        const string scenePath = "Assets/Project/Scenes/YDM/Lobby.unity";
        string scene = File.ReadAllText(scenePath);
        string koreanTable = File.ReadAllText("Assets/Language/Text_ko.asset");
        string[] keys =
        {
            "tutorial.intro.01", "tutorial.intro.02", "tutorial.intro.03",
            "tutorial.intro.04", "tutorial.intro.05", "tutorial.intro.06",
            "tutorial.intro.07", "tutorial.first_expedition.01",
            "tutorial.first_expedition.02", "tutorial.first_expedition.03",
        };

        StringAssert.Contains("guid: 426c88f35f804f14a7d43a9f9611e21b", scene);
        Assert.That(scene, Does.Not.Contain("ui.assets.project.scripts.lobbytutorialcontroller.text.5d1ae56d"));

        foreach (string key in keys)
            StringAssert.Contains("- " + key, scene);

        for (long id = 10999245267542016; id <= 10999245267542025; id++)
            StringAssert.Contains("m_Id: " + id, koreanTable);
    }
}
