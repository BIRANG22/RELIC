using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class LocalizedTMPTextTests
{
    [Test]
    public void ShouldManageText_ReturnsFalseForExplicitDynamicLocalizationTarget()
    {
        GameObject target = new("Dynamic", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LocalizationIgnore));
        try
        {
            Assert.That(LocalizedTMPText.ShouldManageText(target.GetComponent<TMP_Text>()), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void ShouldManageText_ReturnsFalseForDropdownDynamicText()
    {
        GameObject root = new("Dropdown", typeof(TMP_Dropdown));
        GameObject caption = new("Caption", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        caption.transform.SetParent(root.transform, false);

        try
        {
            Assert.That(LocalizedTMPText.ShouldManageText(caption.GetComponent<TMP_Text>()), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [TestCase("Assets/Project/PrefabsR/MenuPanel.prefab", "quit_Text")]
    [TestCase("Assets/Project/PrefabsR/Check.prefab", "Slogan")]
    public void DynamicPrefabText_IsExcludedFromStaticLocalization(string prefabPath, string objectName)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            TMP_Text text = FindText(root, objectName);

            Assert.That(text, Is.Not.Null, $"{objectName} TMP text was not found.");
            Assert.That(text.GetComponent<LocalizationIgnore>(), Is.Not.Null);
            Assert.That(text.GetComponent<LocalizedTMPText>(), Is.Null);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static TMP_Text FindText(GameObject root, string objectName)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.gameObject.name == objectName)
                return text;
        }

        return null;
    }
}
