using System;
using System.IO;
using NUnit.Framework;

public class DebugBattleSceneConfigurationTests
{
    private const string DebugBattleScenePath = "Assets/Project/Scenes/YDM/DebugBattle.unity";
    private const string DataManagerGuid = "bc31f73c849d5c0458e5b706f96d67b2";
    private const string SkillVfxDatabaseGuid = "64d6b86b66c94a0fa7609aa53000565e";
    private const string GridEffectTooltipUiGuid = "dcda88b42eeeaf84db1781a5be81d9e6";

    [Test]
    public void DebugBattleScene_HasDataManagerForDirectScenePlay()
    {
        string sceneText = File.ReadAllText(DebugBattleScenePath);

        Assert.That(sceneText, Does.Contain("m_Name: DataManager"));
        Assert.That(sceneText, Does.Contain($"guid: {DataManagerGuid}"));
        Assert.That(sceneText, Does.Contain($"skillVfxDatabase: {{fileID: 11400000, guid: {SkillVfxDatabaseGuid}, type: 2}}"));
    }

    [Test]
    public void DebugBattleScene_DataManagerIsRegisteredAsSceneRoot()
    {
        string sceneText = File.ReadAllText(DebugBattleScenePath);
        int dataManagerTransformIndex = sceneText.IndexOf("--- !u!4 &910050502", StringComparison.Ordinal);
        int sceneRootsIndex = sceneText.IndexOf("SceneRoots:", StringComparison.Ordinal);

        Assert.That(dataManagerTransformIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(sceneRootsIndex, Is.GreaterThan(dataManagerTransformIndex));
        Assert.That(sceneText.IndexOf("- {fileID: 910050502}", sceneRootsIndex, StringComparison.Ordinal),
            Is.GreaterThan(sceneRootsIndex));
    }

    [Test]
    public void DebugBattleScene_HasGridEffectTooltipUiForGridHover()
    {
        string sceneText = File.ReadAllText(DebugBattleScenePath);
        int battleHudCanvasIndex = sceneText.IndexOf("--- !u!224 &1512511010", StringComparison.Ordinal);
        int tooltipTransformIndex = sceneText.IndexOf("--- !u!224 &2067877214", StringComparison.Ordinal);

        Assert.That(sceneText, Does.Contain("m_Name: GridEffectTooltipUI"));
        Assert.That(sceneText, Does.Contain($"guid: {GridEffectTooltipUiGuid}"));
        Assert.That(tooltipTransformIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(sceneText.IndexOf("- {fileID: 2067877214}", battleHudCanvasIndex, StringComparison.Ordinal),
            Is.GreaterThan(battleHudCanvasIndex));
        Assert.That(sceneText.IndexOf("m_Father: {fileID: 1512511010}", tooltipTransformIndex, StringComparison.Ordinal),
            Is.GreaterThan(tooltipTransformIndex));
        Assert.That(sceneText, Does.Contain("nameText: {fileID: 889724577}"));
        Assert.That(sceneText, Does.Contain("toolTipText: {fileID: 1963026312}"));
    }
}
