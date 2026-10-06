using System.IO;
using NUnit.Framework;
using UnityEditor;

public class InventoryPanelRetirementTests
{
    private const string RetiredPrefabPath = "Assets/Project/PrefabsR/InventoryPanel.prefab";
    private const string RetiredPrefabGuid = "0a507e024825bfc4da47bb4c3e123a0a";

    private static readonly string[] BattleScenePaths =
    {
        "Assets/Project/Scenes/YDM/Battle.unity",
        "Assets/Project/Scenes/YDM/DebugBattle.unity",
        "Assets/Project/Scenes/YDH/Battletest.unity"
    };

    [Test]
    public void RetiredInventoryPanelPrefab_DoesNotExist()
    {
        Assert.That(AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(RetiredPrefabPath), Is.Null);
        Assert.That(File.Exists(RetiredPrefabPath), Is.False);
    }

    [TestCaseSource(nameof(BattleScenePaths))]
    public void BattleScene_DoesNotReferenceRetiredInventoryPanelPrefab(string scenePath)
    {
        string sceneText = File.ReadAllText(scenePath);

        Assert.That(sceneText, Does.Not.Contain(RetiredPrefabGuid));
        Assert.That(sceneText, Does.Not.Contain("inventoryPanelObjectNames:"));
    }
}
