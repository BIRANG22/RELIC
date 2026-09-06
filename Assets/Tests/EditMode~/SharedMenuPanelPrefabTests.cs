#if UNITY_EDITOR
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

public class SharedMenuPanelPrefabTests
{
    private const string PrefabPath =
        "Assets/Project/PrefabsR/MenuPanel.prefab";
    private const string MenuFontGuid = "561f3002d19400a47bae19552e5b0ed5";
    private const long MenuFontFileId = 11400000;
    private const long MenuFontMaterialFileId = -7555710420834941891;

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

    [Test]
    public void MenuPanelPrefab_KoreanTextsUseKoreanFontAndMaterial()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);

        try
        {
            TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            Assert.That(texts, Is.Not.Empty);

            foreach (TextMeshProUGUI text in texts)
            {
                if (text == null || !ContainsKorean(text.text))
                    continue;

                Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(text.font, out string fontGuid, out long fontFileId), Is.True);
                Assert.That(fontGuid, Is.EqualTo(MenuFontGuid), text.name);
                Assert.That(fontFileId, Is.EqualTo(MenuFontFileId), text.name);
                Assert.That(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(text.fontSharedMaterial, out string materialGuid, out long materialFileId), Is.True);
                Assert.That(materialGuid, Is.EqualTo(MenuFontGuid), text.name);
                Assert.That(materialFileId, Is.EqualTo(MenuFontMaterialFileId), text.name);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [Test]
    public void MenuPanelTextRefresher_RefreshesInactiveChildTmpTextsWithoutChangingFont()
    {
        GameObject root = new("MenuPanel");
        GameObject textObject = new("ContinueText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(root.transform, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP/DungGeunMo SDF.asset");
        Assert.That(font, Is.Not.Null);
        text.font = font;
        text.fontSharedMaterial = font.material;
        text.text = "계속";
        textObject.SetActive(false);

        try
        {
            MenuPanelTextRefresher refresher = root.AddComponent<MenuPanelTextRefresher>();

            refresher.RefreshNow();

            Assert.That(text.font, Is.SameAs(font));
            Assert.That(text.fontSharedMaterial, Is.SameAs(font.material));
            Assert.That(text.havePropertiesChanged, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static bool ContainsKorean(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c >= '\uac00' && c <= '\ud7a3')
                return true;
        }

        return false;
    }
}
#endif
