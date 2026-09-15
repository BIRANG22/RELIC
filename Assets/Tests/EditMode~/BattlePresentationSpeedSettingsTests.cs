using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using TMPro;

public class BattlePresentationSpeedSettingsTests
{
    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteKey(BattlePresentationSpeedSettings.PreferenceKey);
        PlayerPrefs.Save();
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteKey(BattlePresentationSpeedSettings.PreferenceKey);
        PlayerPrefs.Save();
    }

    [Test]
    public void CurrentMultiplier_WhenNoPreferenceExists_ReturnsOne()
    {
        Assert.That(BattlePresentationSpeedSettings.CurrentMultiplier, Is.EqualTo(1f));
    }

    [TestCase(0, 1f)]
    [TestCase(1, 1.5f)]
    [TestCase(2, 2f)]
    [TestCase(3, 3f)]
    [TestCase(4, 4f)]
    public void SetSelectedIndex_PersistsSupportedMultiplier(int selectedIndex, float expectedMultiplier)
    {
        BattlePresentationSpeedSettings.SetSelectedIndex(selectedIndex);

        Assert.That(BattlePresentationSpeedSettings.CurrentMultiplier, Is.EqualTo(expectedMultiplier));
        Assert.That(PlayerPrefs.GetInt(BattlePresentationSpeedSettings.PreferenceKey), Is.EqualTo(selectedIndex));
    }

    [Test]
    public void ScaleDuration_AtFourTimesSpeed_OnlyShortensBattlePresentationDuration()
    {
        BattlePresentationSpeedSettings.SetSelectedIndex(4);

        Assert.That(BattlePresentationSpeedSettings.ScaleDuration(2f), Is.EqualTo(0.5f));
    }

    [Test]
    public void SetSelectedIndex_WithInvalidValue_RestoresDefaultSpeed()
    {
        BattlePresentationSpeedSettings.SetSelectedIndex(99);

        Assert.That(BattlePresentationSpeedSettings.CurrentMultiplier, Is.EqualTo(1f));
        Assert.That(PlayerPrefs.GetInt(BattlePresentationSpeedSettings.PreferenceKey), Is.EqualTo(0));
    }

    [Test]
    public void OptionPanel_CycleBattlePresentationSpeed_UpdatesTheDisplayedSpeed()
    {
        BattlePresentationSpeedSettings.SetSelectedIndex(3);
        GameObject owner = new GameObject("Speed Option");
        TextMeshProUGUI label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        OptionPanelUI panel = owner.AddComponent<OptionPanelUI>();
        SetPrivateField(panel, "battlePresentationSpeedLabel", label);

        panel.CycleBattlePresentationSpeed();

        Assert.That(label.text, Is.EqualTo("4.0x"));
    }

    [Test]
    public void OptionPanel_CycleBattlePresentationSpeed_WrapsAfterFourTimesSpeed()
    {
        BattlePresentationSpeedSettings.SetSelectedIndex(4);
        GameObject owner = new GameObject("Speed Option");
        TextMeshProUGUI label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        OptionPanelUI panel = owner.AddComponent<OptionPanelUI>();
        SetPrivateField(panel, "battlePresentationSpeedLabel", label);

        panel.CycleBattlePresentationSpeed();

        Assert.That(BattlePresentationSpeedSettings.CurrentIndex, Is.EqualTo(0));
        Assert.That(label.text, Is.EqualTo("1.0x"));
    }

    [Test]
    public void RestorePlaybackSpeed_AfterPresentationSpeedWasApplied_RestoresIdleAnimatorToOne()
    {
        BattlePresentationSpeedSettings.SetSelectedIndex(4);
        GameObject owner = new GameObject("Battle Unit Animator", typeof(Animator));
        BattleUnitAnimator unitAnimator = owner.AddComponent<BattleUnitAnimator>();
        SetPrivateField(unitAnimator, "animator", owner.GetComponent<Animator>());

        unitAnimator.SetPlaybackSpeed(1.5f);
        unitAnimator.RestorePlaybackSpeed();

        Assert.That(owner.GetComponent<Animator>().speed, Is.EqualTo(1f));
        Object.DestroyImmediate(owner);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' was not found.");
        field.SetValue(instance, value);
    }
}
