using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

public class BattleMapPanelPromotionSceneTests
{
    private const string ScenePath = "Assets/Project/Scenes/YDM/Battle.unity";

    [Test]
    public void BattleScene_ContainsSinglePromotedMapPanel()
    {
        string scene = File.ReadAllText(ScenePath);

        Assert.That(Regex.Matches(scene, @"(?m)^  m_Name: MapPanel$").Count, Is.EqualTo(1));
        Assert.That(scene, Does.Not.Contain("m_Name: MapPanel2"));
        Assert.That(scene, Does.Not.Contain("&511562511"));
        Assert.That(scene, Does.Not.Contain("&511562512"));
        Assert.That(scene, Does.Not.Contain("&511562513"));
        Assert.That(scene, Does.Not.Contain("&511562514"));
    }

    [Test]
    public void BattleScene_NextNodeSelectionRootBelongsToPromotedMapPanel()
    {
        string scene = File.ReadAllText(ScenePath);

        StringAssert.Contains("m_Name: NextNodeSelectionRoot", scene);
        StringAssert.Contains("m_Father: {fileID: 1138866862}", scene);
        StringAssert.Contains("nextNodeSelectionPanel: {fileID: 2147000004}", scene);
    }

    [Test]
    public void BattleScene_MapSpawnerRootsBelongToScrollContent()
    {
        string scene = File.ReadAllText(ScenePath);

        StringAssert.Contains("m_Name: LineRoot", scene);
        StringAssert.Contains("lineRoot: {fileID: 2147000102}", scene);
        StringAssert.Contains("m_Father: {fileID: 758910581}", scene);
        StringAssert.Contains("m_Children:\n  - {fileID: 2147000102}\n  - {fileID: 465380115}",
            scene.Replace("\r\n", "\n"));
    }

    [Test]
    public void BattleScene_MapPanelReferencesDisabledBattleCharacterPanelUiBlock()
    {
        string scene = File.ReadAllText(ScenePath);
        Match mapPanel = Regex.Match(scene,
            @"(?s)--- !u!1 &1138866861\s+GameObject:.*?(?=--- !u!)");
        Match legacyPanelUi = Regex.Match(scene,
            @"(?s)--- !u!114 &1138866863\s+MonoBehaviour:.*?(?=--- !u!)");

        Assert.That(mapPanel.Success, Is.True);
        Assert.That(legacyPanelUi.Success, Is.True);
        StringAssert.Contains("component: {fileID: 1138866863}", mapPanel.Value);
        StringAssert.Contains("m_Enabled: 0", legacyPanelUi.Value);
    }

    [Test]
    public void NodePrefab_UsesFortyPixelRect()
    {
        string prefab = File.ReadAllText("Assets/Project/PrefabsR/Map/NodePrefab.prefab");

        StringAssert.Contains("m_SizeDelta: {x: 40, y: 40}", prefab);
    }

    [Test]
    public void BattleScene_MapViewportBackgroundDoesNotBlockNodeRaycasts()
    {
        string scene = File.ReadAllText(ScenePath);
        Match backgroundImage = Regex.Match(scene,
            @"(?s)--- !u!114 &1463830777\s+MonoBehaviour:.*?(?=--- !u!)");

        Assert.That(backgroundImage.Success, Is.True);
        StringAssert.Contains("m_RaycastTarget: 0", backgroundImage.Value);
    }
}
