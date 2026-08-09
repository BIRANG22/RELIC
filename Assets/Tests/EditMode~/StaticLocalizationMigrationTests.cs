using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

public class StaticLocalizationMigrationTests
{
    private GameObject textObject;

    [TearDown]
    public void TearDown()
    {
        if (textObject != null)
            Object.DestroyImmediate(textObject);
    }

    [Test]
    public void ConfigureText_AddsLocalizedStringReferenceAndTextListener()
    {
        TextMeshProUGUI text = CreateText("닫기");

        bool changed = StaticLocalizationMigration.ConfigureText(text, "common.close");

        LocalizeStringEvent localizer = text.GetComponent<LocalizeStringEvent>();
        Assert.That(changed, Is.True);
        Assert.That(localizer, Is.Not.Null);
        Assert.That(localizer.StringReference.TableReference.TableCollectionName, Is.EqualTo("Text"));
        Assert.That(localizer.StringReference.TableEntryReference.Key, Is.EqualTo("common.close"));
        Assert.That(localizer.OnUpdateString.GetPersistentEventCount(), Is.EqualTo(1));
        Assert.That(localizer.OnUpdateString.GetPersistentTarget(0), Is.SameAs(text));
    }

    [Test]
    public void ConfigureText_WhenAlreadyConfigured_DoesNotAddDuplicateComponent()
    {
        TextMeshProUGUI text = CreateText("닫기");

        StaticLocalizationMigration.ConfigureText(text, "common.close");
        bool changedAgain = StaticLocalizationMigration.ConfigureText(text, "common.close");

        Assert.That(changedAgain, Is.False);
        Assert.That(text.GetComponents<LocalizeStringEvent>(), Has.Length.EqualTo(1));
    }

    private TextMeshProUGUI CreateText(string value)
    {
        textObject = new GameObject("Localized Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        return text;
    }
}
