#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;

public class SharedUpgradePanelPrefabTests
{
    [Test]
    public void SharedPrefab_ContainsBothControllersAndSelector()
    {
        const string path = "Assets/Project/PrefabsR/UpgradePanel.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);

        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponent<LobbySkillUpgradePanelUI>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<SkillUpgradePanel>(), Is.Not.Null);
        Assert.That(prefab.GetComponent<SkillUpgradePanelContextSelector>(), Is.Not.Null);
    }
}
#endif
