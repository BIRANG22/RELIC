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
    public void ConfigureText_AddsSingleRuntimeLocalizedTextOwner()
    {
        TextMeshProUGUI text = CreateText("닫기");

        bool changed = StaticLocalizationMigration.ConfigureText(text, "common.close");

        LocalizedTMPText localizer = text.GetComponent<LocalizedTMPText>();
        Assert.That(changed, Is.True);
        Assert.That(localizer, Is.Not.Null);
        Assert.That(localizer.LocalizationKey, Is.EqualTo("common.close"));
        Assert.That(text.GetComponent<LocalizeStringEvent>(), Is.Null);
    }

    [Test]
    public void ConfigureText_WhenAlreadyConfigured_DoesNotAddDuplicateComponent()
    {
        TextMeshProUGUI text = CreateText("닫기");

        StaticLocalizationMigration.ConfigureText(text, "common.close");
        bool changedAgain = StaticLocalizationMigration.ConfigureText(text, "common.close");

        Assert.That(changedAgain, Is.False);
        Assert.That(text.GetComponents<LocalizedTMPText>(), Has.Length.EqualTo(1));
    }

    [Test]
    public void ConfigureText_WhenExistingKeyDiffers_DoesNotOverwriteSemanticKey()
    {
        TextMeshProUGUI text = CreateText("탐사진행");
        StaticLocalizationMigration.ConfigureText(text, "title.explore_continue");

        bool changed = StaticLocalizationMigration.ConfigureText(text, "lobby.explore");

        Assert.That(changed, Is.False);
        Assert.That(text.GetComponent<LocalizedTMPText>().LocalizationKey,
            Is.EqualTo("title.explore_continue"));
    }

    [Test]
    public void RepairTextBinding_WhenExistingKeyDiffers_ReplacesIncorrectKey()
    {
        TextMeshProUGUI text = CreateText("탐사진행");
        StaticLocalizationMigration.ConfigureText(text, "lobby.explore");

        bool changed = StaticLocalizationMigration.RepairTextBinding(text, "title.explore_continue");

        Assert.That(changed, Is.True);
        Assert.That(text.GetComponent<LocalizedTMPText>().LocalizationKey,
            Is.EqualTo("title.explore_continue"));
    }

    private TextMeshProUGUI CreateText(string value)
    {
        textObject = new GameObject("Localized Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        return text;
    }
}
