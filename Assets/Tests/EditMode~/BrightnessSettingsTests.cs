using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class BrightnessSettingsTests
{
    private const string BrightnessPrefsKey = "Brightness";

    private bool hadBrightnessPreference;
    private float originalBrightnessPreference;

    [SetUp]
    public void SetUp()
    {
        hadBrightnessPreference = PlayerPrefs.HasKey(BrightnessPrefsKey);
        originalBrightnessPreference = PlayerPrefs.GetFloat(BrightnessPrefsKey, 0.5f);
        PlayerPrefs.DeleteKey(BrightnessPrefsKey);
        PlayerPrefs.Save();
    }

    [TearDown]
    public void TearDown()
    {
        ChangeBrightness[] changeBrightnessComponents = Object.FindObjectsByType<ChangeBrightness>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = changeBrightnessComponents.Length - 1; i >= 0; i--)
            Object.DestroyImmediate(changeBrightnessComponents[i].gameObject);

        Settings[] settings = Object.FindObjectsByType<Settings>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = settings.Length - 1; i >= 0; i--)
            Object.DestroyImmediate(settings[i].gameObject);

        if (hadBrightnessPreference)
            PlayerPrefs.SetFloat(BrightnessPrefsKey, originalBrightnessPreference);
        else
            PlayerPrefs.DeleteKey(BrightnessPrefsKey);

        PlayerPrefs.Save();
    }

    [Test]
    public void Settings_LoadDefaultsBrightnessToHalf()
    {
        Settings settings = CreateSettings();

        settings.Load();

        Assert.That(settings.Brightness, Is.EqualTo(0.5f));
    }

    [Test]
    public void Settings_SavePersistsBrightness()
    {
        Settings settings = CreateSettings();
        settings.Brightness = 0.8f;

        settings.Save();

        Assert.That(PlayerPrefs.GetFloat(BrightnessPrefsKey), Is.EqualTo(0.8f));
    }

    [Test]
    public void ChangeBrightness_InitializesSliderFromSettings()
    {
        Settings settings = CreateSettings();
        settings.Brightness = 0.25f;
        Slider slider = CreateSliderWithChangeBrightness(out _);

        slider.GetComponent<ChangeBrightness>().SendMessage("Start");

        Assert.That(slider.value, Is.EqualTo(0.25f));
    }

    [Test]
    public void ChangeBrightness_ChangingSliderSavesBrightness()
    {
        Settings settings = CreateSettings();
        settings.Brightness = 0.5f;
        Slider slider = CreateSliderWithChangeBrightness(out _);
        slider.GetComponent<ChangeBrightness>().SendMessage("Start");

        slider.value = 0.9f;

        Assert.That(settings.Brightness, Is.EqualTo(0.9f));
        Assert.That(PlayerPrefs.GetFloat(BrightnessPrefsKey), Is.EqualTo(0.9f));
    }

    [Test]
    public void GameBrightnessManager_ConvertsSliderValueToPostExposure()
    {
        Assert.That(GameBrightnessManager.BrightnessToPostExposure(0f), Is.EqualTo(-2f));
        Assert.That(GameBrightnessManager.BrightnessToPostExposure(0.5f), Is.EqualTo(0f));
        Assert.That(GameBrightnessManager.BrightnessToPostExposure(1f), Is.EqualTo(2f));
    }

    private static Settings CreateSettings()
    {
        GameObject gameObject = new("Settings");
        return gameObject.AddComponent<Settings>();
    }

    private static Slider CreateSliderWithChangeBrightness(out ChangeBrightness changeBrightness)
    {
        GameObject gameObject = new("BrightnessSlider", typeof(RectTransform), typeof(Slider));
        Slider slider = gameObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        changeBrightness = gameObject.AddComponent<ChangeBrightness>();
        return slider;
    }
}
