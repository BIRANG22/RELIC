using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

public sealed class ErosionTooltipLocalizationOwnershipTests
{
    [Test]
    public void TooltipErosionText_DisablesEveryCompetingLocalizationWriter()
    {
        GameObject root = new GameObject("ErosionCatalog");
        root.SetActive(false);

        try
        {
            ErosionDifficultyCatalogUI catalog = root.AddComponent<ErosionDifficultyCatalogUI>();
            GameObject textObject = new GameObject("ErosionText");
            textObject.transform.SetParent(root.transform);
            TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
            LocalizedTMPText fixedWriter = textObject.AddComponent<LocalizedTMPText>();
            DynamicLocalizedTMPText dynamicWriter = textObject.AddComponent<DynamicLocalizedTMPText>();
            LocalizeStringEvent legacyWriter = textObject.AddComponent<LocalizeStringEvent>();

            typeof(ErosionDifficultyCatalogUI)
                .GetField("tooltipErosionText", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(catalog, text);

            MethodInfo protect = typeof(ErosionDifficultyCatalogUI).GetMethod(
                "EnsureDynamicTooltipTextOwnership",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(protect, Is.Not.Null);
            protect.Invoke(catalog, null);

            Assert.That(textObject.GetComponent<LocalizationAutoBindingIgnore>(), Is.Not.Null);
            Assert.That(fixedWriter.enabled, Is.False);
            Assert.That(dynamicWriter.enabled, Is.False);
            Assert.That(legacyWriter.enabled, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
