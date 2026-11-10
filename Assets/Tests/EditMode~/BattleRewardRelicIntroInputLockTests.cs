using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class BattleRewardRelicIntroInputLockTests
{
    private const string SourcePath = "Assets/Project/Scripts/BattleRewardRelicPanelUI.cs";

    [Test]
    public void IntroInputBlocker_IsTransparentStretchedRaycastTargetAndCanBeReleased()
    {
        GameObject panelObject = new GameObject("Relic_panel", typeof(RectTransform));
        try
        {
            BattleRewardRelicPanelUI panel = panelObject.AddComponent<BattleRewardRelicPanelUI>();
            MethodInfo setBlocked = typeof(BattleRewardRelicPanelUI).GetMethod(
                "SetIntroInputBlocked", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(setBlocked, Is.Not.Null);
            setBlocked.Invoke(panel, new object[] { true });

            Transform blocker = panelObject.transform.Find("__IntroInputBlocker");
            Assert.That(blocker, Is.Not.Null);
            Assert.That(blocker.gameObject.activeSelf, Is.True);
            Assert.That(blocker.GetSiblingIndex(), Is.EqualTo(0));

            RectTransform rect = blocker as RectTransform;
            Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(rect.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.offsetMax, Is.EqualTo(Vector2.zero));

            Image image = blocker.GetComponent<Image>();
            Assert.That(image, Is.Not.Null);
            Assert.That(image.raycastTarget, Is.True);
            Assert.That(image.color.a, Is.EqualTo(0f));

            setBlocked.Invoke(panel, new object[] { false });
            Assert.That(blocker.gameObject.activeSelf, Is.False);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(panelObject);
        }
    }

    [Test]
    public void IntroInputBlocker_ParentDisableDoesNotChangeSiblingPositionOrLogError()
    {
        GameObject panelObject = new GameObject("Relic_panel", typeof(RectTransform));
        try
        {
            BattleRewardRelicPanelUI panel = panelObject.AddComponent<BattleRewardRelicPanelUI>();
            MethodInfo setBlocked = typeof(BattleRewardRelicPanelUI).GetMethod(
                "SetIntroInputBlocked", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(setBlocked, Is.Not.Null);
            setBlocked.Invoke(panel, new object[] { true });

            Transform blocker = panelObject.transform.Find("__IntroInputBlocker");
            Assert.That(blocker, Is.Not.Null);

            panelObject.SetActive(false);

            Assert.That(blocker.gameObject.activeSelf, Is.False);
            LogAssert.NoUnexpectedReceived();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(panelObject);
        }
    }

    [Test]
    public void ItemIntro_EnablesBlockerAndAllExitPathsReleaseIt()
    {
        string source = File.ReadAllText(SourcePath);
        string beginIntro = ExtractMethod(source, "private void BeginItemIntro()");
        string animateIntro = ExtractMethod(source, "private IEnumerator AnimateItemIntro(");
        string resetIntro = ExtractMethod(source, "private void ResetItemIntro()");

        Assert.That(beginIntro, Does.Contain("panelCanvasGroup.interactable = false;"));
        Assert.That(beginIntro, Does.Contain("panelCanvasGroup.blocksRaycasts = true;"));
        Assert.That(beginIntro, Does.Contain("SetIntroInputBlocked(true);"));
        Assert.That(animateIntro, Does.Contain("panelCanvasGroup.interactable = true;"));
        Assert.That(animateIntro, Does.Contain("panelCanvasGroup.blocksRaycasts = true;"));
        Assert.That(animateIntro, Does.Contain("SetIntroInputBlocked(false);"));
        Assert.That(resetIntro, Does.Contain("SetIntroInputBlocked(false);"));
    }

    private static string ExtractMethod(string source, string signature)
    {
        int start = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), signature);

        int bodyStart = source.IndexOf('{', start);
        int depth = 0;
        for (int i = bodyStart; i < source.Length; i++)
        {
            if (source[i] == '{') depth++;
            if (source[i] != '}') continue;

            depth--;
            if (depth == 0)
                return source.Substring(start, i - start + 1);
        }

        Assert.Fail($"Method body not found: {signature}");
        return string.Empty;
    }
}
