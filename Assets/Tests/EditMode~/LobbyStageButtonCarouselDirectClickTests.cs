using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class LobbyStageButtonCarouselDirectClickTests
{
    private GameObject root;

    [TearDown]
    public void TearDown()
    {
        if (root != null)
            Object.DestroyImmediate(root);
    }

    [Test]
    public void HandleStageButtonClick_MovesSideCard_ButConfirmsCenteredCard()
    {
        root = new GameObject("StageSelectPanel", typeof(RectTransform));
        LobbyStageButtonCarousel carousel = root.AddComponent<LobbyStageButtonCarousel>();

        Button first = CreateButton("Stage1");
        Button second = CreateButton("Stage2");
        SetPrivateField(carousel, "stageButtons", new[] { first, second });
        SetPrivateField(carousel, "currentIndex", 0);

        Assert.That(carousel.HandleStageButtonClick(second), Is.True);
        Assert.That(carousel.CurrentIndex, Is.EqualTo(1));
        Assert.That(carousel.HandleStageButtonClick(second), Is.False);
    }

    private Button CreateButton(string name)
    {
        GameObject buttonObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(root.transform, false);
        return buttonObject.GetComponent<Button>();
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
        field.SetValue(target, value);
    }
}
