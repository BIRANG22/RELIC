using System.IO;
using NUnit.Framework;

public class BattleInventoryPanelPrefabConnectionTests
{
    [Test]
    public void BattleScene_ReferencesSharedInventoryPanelPrefab()
    {
        const string scenePath = "Assets/Project/Scenes/YDM/Battle.unity";
        const string prefabMetaPath = "Assets/Project/PrefabsR/InventoryPanel.prefab.meta";

        string guidLine = System.Array.Find(
            File.ReadAllLines(prefabMetaPath),
            line => line.StartsWith("guid:"));
        string guid = guidLine.Substring("guid:".Length).Trim();
        string sceneYaml = File.ReadAllText(scenePath);

        Assert.That(sceneYaml, Does.Contain($"m_SourcePrefab: {{fileID: 100100000, guid: {guid}, type: 3}}"));
    }
}
