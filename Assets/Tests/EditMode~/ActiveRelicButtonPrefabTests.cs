using NUnit.Framework;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ActiveRelicButtonPrefabTests
{
    private const string ActiveRelicPrefabPath = "Assets/Project/PrefabsR/HUD_Prefab/Relic.prefab";
    private const string BattleScenePath = "Assets/Project/Scenes/YDM/Battle.unity";

    [Test]
    public void ActiveRelicPrefabHasButtonControllerAndBlueHoverBackground()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ActiveRelicPrefabPath);

        Assert.NotNull(prefab);
        Assert.NotNull(prefab.GetComponent<ActiveRelicButtonUI>());

        Button button = prefab.GetComponent<Button>();
        Assert.NotNull(button);

        Image background = prefab.transform.Find("BackGround")?.GetComponent<Image>();
        Assert.NotNull(background);
        Assert.AreSame(background, button.targetGraphic);
        Assert.AreEqual(Selectable.Transition.ColorTint, button.transition);

        Color highlightedColor = button.colors.highlightedColor;
        Assert.That(highlightedColor.r, Is.EqualTo(0.30588236f).Within(0.001f));
        Assert.That(highlightedColor.g, Is.EqualTo(0.4f).Within(0.001f));
        Assert.That(highlightedColor.b, Is.EqualTo(0.8745098f).Within(0.001f));
        Assert.That(highlightedColor.a, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void ActiveRelicPrefabRootUsesButtonSizedTransform()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ActiveRelicPrefabPath);

        Assert.NotNull(prefab);

        RectTransform rect = prefab.GetComponent<RectTransform>();
        Assert.NotNull(rect);
        Assert.That(rect.sizeDelta.x, Is.LessThanOrEqualTo(160f));
        Assert.That(rect.sizeDelta.y, Is.LessThanOrEqualTo(160f));
        Assert.That(rect.localScale.x, Is.EqualTo(1f).Within(0.001f));
        Assert.That(rect.localScale.y, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void BattleSceneKeepsActiveRelicFixedRectButtonSized()
    {
        string sceneText = File.ReadAllText(BattleScenePath);

        StringAssert.Contains("activeRelicFixedSize: {x: 100, y: 100}", sceneText);
        Assert.That(sceneText, Does.Not.Contain("activeRelicFixedSize: {x: 1080, y: 1080}"));
    }
}
