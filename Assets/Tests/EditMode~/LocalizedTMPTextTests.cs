using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LocalizedTMPTextTests
{
    [Test]
    public void ShouldManageText_ReturnsFalseForExplicitDynamicLocalizationTarget()
    {
        GameObject target = new("Dynamic", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LocalizationIgnore));
        try
        {
            Assert.That(LocalizedTMPText.ShouldManageText(target.GetComponent<TMP_Text>()), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void ShouldManageText_ReturnsFalseForDropdownDynamicText()
    {
        GameObject root = new("Dropdown", typeof(TMP_Dropdown));
        GameObject caption = new("Caption", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        caption.transform.SetParent(root.transform, false);

        try
        {
            Assert.That(LocalizedTMPText.ShouldManageText(caption.GetComponent<TMP_Text>()), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
