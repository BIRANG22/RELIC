using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MapChapterSelectBattleEntryTests
{
    private GameObject buttonObject;

    [TearDown]
    public void TearDown()
    {
        if (buttonObject != null)
            Object.DestroyImmediate(buttonObject);
    }

    [Test]
    public void DirectSelection_InvokesSuccessEventOnlyWhenUnlocked()
    {
        buttonObject = new GameObject(
            "Stage1",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(MapChapterSelectButton));
        MapChapterSelectButton chapterButton =
            buttonObject.GetComponent<MapChapterSelectButton>();
        UnityEvent successEvent = new();
        int invocationCount = 0;
        successEvent.AddListener(() => invocationCount++);
        SetPrivateField(chapterButton, "enterBattleOnDirectSelect", true);
        SetPrivateField(chapterButton, "directSelectionSucceeded", successEvent);

        SetPrivateField(chapterButton, "isLocked", true);
        chapterButton.OnClickSelectChapter();
        Assert.That(invocationCount, Is.Zero);

        SetPrivateField(chapterButton, "isLocked", false);
        chapterButton.OnClickSelectChapter();
        Assert.That(invocationCount, Is.EqualTo(1));
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
