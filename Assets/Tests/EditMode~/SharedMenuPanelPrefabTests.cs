#if UNITY_EDITOR
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class SharedMenuPanelPrefabTests
{
    private const string PrefabPath =
        "Assets/Project/PrefabsR/MenuPanel.prefab";

    [Test]
    public void MenuPanelPrefab_UsesLobbyHierarchyAndStartsClosed()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.name, Is.EqualTo("MenuPanel"));
        Assert.That(prefab.activeSelf, Is.False);
        Assert.That(
            prefab.GetComponentsInChildren<Transform>(true).Length,
            Is.EqualTo(25));
        Assert.That(prefab.transform.Find("Continue"), Is.Not.Null);
        Assert.That(prefab.transform.Find("Continue/Button_in"), Is.Not.Null);
        Assert.That(prefab.transform.Find("Option"), Is.Not.Null);
        Assert.That(prefab.transform.Find("Giveup"), Is.Not.Null);
        Assert.That(prefab.transform.Find("Quit"), Is.Not.Null);
    }

    [TestCase("Assets/Project/Scenes/YDM/Lobby.unity")]
    [TestCase("Assets/Project/Scenes/YDM/Battle.unity")]
    public void Scene_ReferencesSharedMenuPanelPrefab(string scenePath)
    {
        string guid = AssetDatabase.AssetPathToGUID(PrefabPath);
        string sceneYaml = File.ReadAllText(scenePath);

        Assert.That(guid, Is.Not.Empty);
        Assert.That(
            sceneYaml,
            Does.Contain(
                $"m_SourcePrefab: {{fileID: 100100000, guid: {guid}, type: 3}}"));
    }
}
#endif
