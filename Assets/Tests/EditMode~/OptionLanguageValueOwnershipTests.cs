using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

public sealed class OptionLanguageValueOwnershipTests
{
    [TestCase("English")]
    [TestCase("日本語")]
    [TestCase("简体中文")]
    [TestCase("繁體中文")]
    [TestCase("Español")]
    public void Reopen_PreventsRememberedKoreanSourceFromOverwritingSelectedLanguage(string selectedName)
    {
        GameObject root = new GameObject("OptionLanguageOwnership");
        root.SetActive(false);
        try
        {
            OptionPanelUI panel = root.AddComponent<OptionPanelUI>();
            GameObject valueObject = new GameObject("ValueText", typeof(RectTransform));
            valueObject.transform.SetParent(root.transform, false);
            TMP_Text text = valueObject.AddComponent<TextMeshProUGUI>();
            valueObject.AddComponent<LocalizationIgnore>();
            LocalizedTMPText staticWriter = valueObject.AddComponent<LocalizedTMPText>();
            DynamicLocalizedTMPText dynamicWriter = valueObject.AddComponent<DynamicLocalizedTMPText>();
            LocalizeStringEvent legacyWriter = valueObject.AddComponent<LocalizeStringEvent>();

            // The auto-localizer previously captured this source while Korean was selected.
            var source = new DynamicLocalizationSourceState(new LocalizationBindingResolver(new[]
            {
                new LocalizationBindingEntry("ui.option.label.f8b78ea7", "한국어")
            }));
            Assert.That(source.UpdateSource("한국어"), Is.True);
            SetField(dynamicWriter, "sourceState", source);
            SetField(dynamicWriter, "target", text);
            SetField(panel, "languageValueText", text);

            MethodInfo bind = typeof(OptionPanelUI).GetMethod("BindArrowSelectors", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int reopen = 0; reopen < 2; reopen++)
            {
                bind.Invoke(panel, null);
                text.text = selectedName;

                Assert.That(valueObject.GetComponent<LocalizationAutoBindingIgnore>(), Is.Not.Null,
                    "Prevent RuntimeTMPTextAutoLocalizer from attaching another remembered-source writer");
                Assert.That(staticWriter.enabled, Is.False);
                Assert.That(dynamicWriter.enabled, Is.False);
                Assert.That(legacyWriter.enabled, Is.False);

                // The global coordinator directly refreshes disabled writers too.
                dynamicWriter.RefreshForLocale(null);
                staticWriter.RefreshForLocale(null);
                Assert.That(text.text, Is.EqualTo(selectedName));
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void SetField(object instance, string name, object value)
    {
        instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(instance, value);
    }
}
