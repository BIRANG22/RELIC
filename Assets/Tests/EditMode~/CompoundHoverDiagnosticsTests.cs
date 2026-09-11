using NUnit.Framework;
using TMPro;
using UnityEngine;

public sealed class DynamicHoverLocalizationTests
{
    private GameObject tooltipObject;
    private GameObject fixedRootObject;
    private GameObject compoundTooltipObject;

    [TearDown]
    public void TearDown()
    {
        if (tooltipObject != null)
            Object.DestroyImmediate(tooltipObject);
        if (fixedRootObject != null)
            Object.DestroyImmediate(fixedRootObject);
        if (compoundTooltipObject != null)
            Object.DestroyImmediate(compoundTooltipObject);
    }

    [Test]
    public void SetFixedRoot_MarksResolvedFixedTextsAsDynamic()
    {
        tooltipObject = new GameObject("Tooltip", typeof(RectTransform));
        InfoTooltip tooltip = tooltipObject.AddComponent<InfoTooltip>();
        fixedRootObject = new GameObject("Fixed Root", typeof(RectTransform));
        TextMeshProUGUI title = CreateText(fixedRootObject.transform, "Title");
        TextMeshProUGUI effect = CreateText(fixedRootObject.transform, "Effect");
        title.gameObject.AddComponent<LocalizedTMPText>();
        effect.gameObject.AddComponent<LocalizedTMPText>();

        tooltip.SetFixedRoot(fixedRootObject.GetComponent<RectTransform>());

        AssertDynamic(title);
        AssertDynamic(effect);
    }

    [Test]
    public void CompoundNameinfo_AwakeMarksResolvedNameTextAsDynamic()
    {
        compoundTooltipObject = new GameObject("Nameinfo", typeof(RectTransform));
        TextMeshProUGUI nameText = CreateText(compoundTooltipObject.transform, "Name");
        nameText.gameObject.AddComponent<LocalizedTMPText>();

        compoundTooltipObject.AddComponent<CompoundReferenceNameTooltip>();

        AssertDynamic(nameText);
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        return textObject.GetComponent<TextMeshProUGUI>();
    }

    private static void AssertDynamic(TMP_Text text)
    {
        Assert.That(text.GetComponent<LocalizationIgnore>(), Is.Not.Null);
        LocalizedTMPText localizer = text.GetComponent<LocalizedTMPText>();
        Assert.That(localizer == null || !localizer.enabled, Is.True);
    }
}
