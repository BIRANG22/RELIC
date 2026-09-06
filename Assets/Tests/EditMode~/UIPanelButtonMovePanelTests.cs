using NUnit.Framework;
using UnityEngine;

public sealed class UIPanelButtonMovePanelTests
{
    [Test]
    public void SetMovePanelOpen_OpensWithoutTogglingClosedOnRepeatedOpen()
    {
        GameObject panelObject = new("BagPanel", typeof(RectTransform));
        GameObject buttonObject = new("BagButton");

        try
        {
            RectTransform panel = panelObject.GetComponent<RectTransform>();
            panel.anchoredPosition = new Vector2(1100f, 194f);

            UIPanelButton button = buttonObject.AddComponent<UIPanelButton>();
            button.ConfigurePanelMove(panel, new Vector2(-270f, 0f));

            button.SetMovePanelOpen(true, true);

            Assert.That(panel.anchoredPosition.x, Is.EqualTo(830f).Within(0.01f));
            Assert.That(panel.anchoredPosition.y, Is.EqualTo(194f).Within(0.01f));

            button.SetMovePanelOpen(true, true);

            Assert.That(panel.anchoredPosition.x, Is.EqualTo(830f).Within(0.01f));
            Assert.That(panel.anchoredPosition.y, Is.EqualTo(194f).Within(0.01f));

            button.SetMovePanelOpen(false, true);

            Assert.That(panel.anchoredPosition.x, Is.EqualTo(1100f).Within(0.01f));
            Assert.That(panel.anchoredPosition.y, Is.EqualTo(194f).Within(0.01f));
        }
        finally
        {
            Object.DestroyImmediate(buttonObject);
            Object.DestroyImmediate(panelObject);
        }
    }
}
