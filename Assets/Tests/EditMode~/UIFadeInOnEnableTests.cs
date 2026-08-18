using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class UIFadeInOnEnableTests
{
    [UnityTest]
    public IEnumerator TryFadeOutAndDeactivate_KeepsObjectActiveUntilFadeCompletes()
    {
        GameObject panelObject = new("FadePanel", typeof(RectTransform));
        GameObject imageObject = new("FadeTarget", typeof(RectTransform), typeof(Image));

        try
        {
            imageObject.transform.SetParent(panelObject.transform, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 1f);

            UIFadeInOnEnable fade = panelObject.AddComponent<UIFadeInOnEnable>();
            SetPrivateField(fade, "fadeDuration", 0.02f);

            yield return null;

            bool callbackCalled = false;
            bool started = fade.TryFadeOutAndDeactivate(() => callbackCalled = true);

            Assert.That(started, Is.True);
            Assert.That(panelObject.activeSelf, Is.True);
            Assert.That(callbackCalled, Is.False);

            yield return new WaitForSecondsRealtime(0.05f);

            Assert.That(panelObject.activeSelf, Is.False);
            Assert.That(callbackCalled, Is.True);
            Assert.That(image.color.a, Is.EqualTo(1f).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
        }
    }

    [Test]
    public void SetAlphaToZero_DoesNotFadeBlurBackgroundGraphics()
    {
        GameObject panelObject = new("FadePanel", typeof(RectTransform));
        GameObject imageObject = new("FadeTarget", typeof(RectTransform), typeof(Image));
        GameObject backgroundObject = new("BlurBackground", typeof(RectTransform), typeof(Image), typeof(UIBlurBackground));

        try
        {
            imageObject.transform.SetParent(panelObject.transform, false);
            backgroundObject.transform.SetParent(panelObject.transform, false);

            Image image = imageObject.GetComponent<Image>();
            image.color = Color.white;

            UIBlurBackground blurBackground = backgroundObject.GetComponent<UIBlurBackground>();
            InvokePrivate(blurBackground, "Awake");

            RawImage blurGraphic = GetPrivateField<RawImage>(blurBackground, "blurGraphic");
            Assert.That(blurGraphic, Is.Not.Null);
            blurGraphic.color = Color.white;

            UIFadeInOnEnable fade = panelObject.AddComponent<UIFadeInOnEnable>();
            InvokePrivate(fade, "Initialize");
            InvokePrivate(fade, "SetAlphaToZero");

            Assert.That(image.color.a, Is.EqualTo(0f).Within(0.001f));
            Assert.That(
                blurGraphic.color.a,
                Is.EqualTo(1f).Within(0.001f),
                "The captured blur surface must stay visible while the rest of Equip_panel fades in.");
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null);
        method.Invoke(target, null);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
        where T : class
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null);
        return field.GetValue(target) as T;
    }
}
