using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class LobbyTutorialControllerVfxTests
{
    private const string AnchorVfxProxyObjectName = "AnchorVfxProxy";
    private const string AnchorVfxRendererRootName = "__LobbyTutorialAnchorVfxRenderer";

    [Test]
    public void SetTutorialDisplay_ShowsAnchorThroughFixedUiRenderTextureProxy()
    {
        LobbyTutorialController controller = CreateController(
            out GameObject root,
            out _,
            out GameObject anchorImage,
            out GameObject sourceVfxRoot,
            out ParticleSystem sourceParticles);

        try
        {
            InvokeSetTutorialDisplay(controller, true, false);

            RawImage proxyImage = GetAnchorProxyImage(anchorImage);
            RectTransform proxyRect = proxyImage.rectTransform;
            ParticleSystem runtimeParticles =
                FindRuntimeAnchorParticles(sourceParticles);

            Assert.That(sourceVfxRoot.activeSelf, Is.False);
            Assert.That(proxyImage.transform.parent, Is.SameAs(anchorImage.transform));
            Assert.That(proxyImage.enabled, Is.True);
            Assert.That(proxyImage.raycastTarget, Is.False);
            Assert.That(proxyImage.texture, Is.TypeOf<RenderTexture>());
            Assert.That(proxyRect.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(proxyRect.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(proxyRect.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(proxyRect.sizeDelta, Is.EqualTo(new Vector2(400f, 400f)));
            Assert.That(runtimeParticles, Is.Not.SameAs(sourceParticles));
            Assert.That(runtimeParticles.transform.IsChildOf(anchorImage.transform), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
            CleanupAnchorVfxRendererRoot();
        }
    }

    [Test]
    public void SetTutorialDisplay_HidesAnchorRenderTextureProxyWhenAnchorIsHidden()
    {
        LobbyTutorialController controller = CreateController(
            out GameObject root,
            out _,
            out GameObject anchorImage,
            out GameObject sourceVfxRoot,
            out _);

        try
        {
            InvokeSetTutorialDisplay(controller, true, false);
            RawImage proxyImage = GetAnchorProxyImage(anchorImage);

            InvokeSetTutorialDisplay(controller, false, false);

            Assert.That(sourceVfxRoot.activeSelf, Is.False);
            Assert.That(proxyImage.enabled, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
            CleanupAnchorVfxRendererRoot();
        }
    }

    private static LobbyTutorialController CreateController(
        out GameObject root,
        out GameObject tutorialDisplay,
        out GameObject anchorImage,
        out GameObject sourceVfxRoot,
        out ParticleSystem sourceParticles)
    {
        root = new GameObject("LobbyTutorialController", typeof(RectTransform));
        tutorialDisplay = new GameObject("TutorialDisplay", typeof(RectTransform));
        tutorialDisplay.transform.SetParent(root.transform, false);

        anchorImage = new GameObject(
            "AnchorImage",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        anchorImage.transform.SetParent(tutorialDisplay.transform, false);
        ((RectTransform)anchorImage.transform).sizeDelta = new Vector2(400f, 400f);

        sourceVfxRoot = new GameObject("Vfx_root_anchor");
        sourceVfxRoot.transform.SetParent(anchorImage.transform, false);

        GameObject startObject = new("Start", typeof(ParticleSystem));
        startObject.transform.SetParent(sourceVfxRoot.transform, false);
        sourceParticles = startObject.GetComponent<ParticleSystem>();

        GameObject fragmentGroup = new("FragmentGroup");
        fragmentGroup.transform.SetParent(tutorialDisplay.transform, false);

        LobbyTutorialController controller = root.AddComponent<LobbyTutorialController>();
        SetPrivateField(controller, "tutorialDisplay", tutorialDisplay);
        SetPrivateField(controller, "anchorImage", anchorImage);
        SetPrivateField(controller, "fragmentGroup", fragmentGroup);
        return controller;
    }

    private static RawImage GetAnchorProxyImage(GameObject anchorImage)
    {
        Transform proxy = anchorImage.transform.Find(AnchorVfxProxyObjectName);
        Assert.That(proxy, Is.Not.Null);

        RawImage proxyImage = proxy.GetComponent<RawImage>();
        Assert.That(proxyImage, Is.Not.Null);
        return proxyImage;
    }

    private static ParticleSystem FindRuntimeAnchorParticles(
        ParticleSystem sourceParticles)
    {
        GameObject rendererRoot = GameObject.Find(AnchorVfxRendererRootName);
        Assert.That(rendererRoot, Is.Not.Null);

        ParticleSystem[] particles =
            rendererRoot.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem candidate = particles[i];
            if (candidate != null &&
                candidate != sourceParticles &&
                candidate.name == sourceParticles.name)
            {
                return candidate;
            }
        }

        Assert.Fail("Runtime anchor particles were not created.");
        return null;
    }

    private static void InvokeSetTutorialDisplay(
        LobbyTutorialController controller,
        bool showAnchor,
        bool showFragments)
    {
        MethodInfo method = typeof(LobbyTutorialController).GetMethod(
            "SetTutorialDisplay",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(controller, new object[] { showAnchor, showFragments });
    }

    private static void SetPrivateField(
        LobbyTutorialController controller,
        string fieldName,
        object value)
    {
        FieldInfo field = typeof(LobbyTutorialController).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(controller, value);
    }

    private static void CleanupAnchorVfxRendererRoot()
    {
        GameObject rendererRoot = GameObject.Find(AnchorVfxRendererRootName);
        if (rendererRoot != null)
            Object.DestroyImmediate(rendererRoot);
    }
}
