using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyRelicOfferButtonUITests
{
    private const string LobbyScenePath = "Assets/Project/Scenes/YDM/Lobby.unity";
    private const string RarityRingProxyObjectName = "RarityRingVfxProxy";
    private const string RarityRingRendererRootName = "__LobbyRelicOfferRarityVfxRenderer";

    [Test]
    public void LobbyScene_RelicIconsRenderAboveMagicRingVfx()
    {
        Scene scene = EditorSceneManager.OpenPreviewScene(LobbyScenePath);

        try
        {
            GameObject panel = FindGameObject(scene, "RelicShopPanel");
            Assert.That(panel, Is.Not.Null);

            Image[] images = panel.GetComponentsInChildren<Image>(true);
            int configuredIconCount = 0;
            foreach (Image image in images)
            {
                if (image.name != "RelicIcon")
                    continue;

                Canvas canvas = image.GetComponent<Canvas>();
                Assert.That(canvas, Is.Not.Null, $"{image.transform.parent.name}/RelicIcon");
                Assert.That(canvas.overrideSorting, Is.True);
                Assert.That(canvas.sortingLayerName, Is.EqualTo("Unit"));
                Assert.That(canvas.sortingOrder, Is.EqualTo(10));
                configuredIconCount++;
            }

            Assert.That(configuredIconCount, Is.EqualTo(3));
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    [TestCase(RelicRarity.Common, 200, 208, 217)]
    [TestCase(RelicRarity.Rare, 92, 219, 131)]
    [TestCase(RelicRarity.Epic, 78, 141, 255)]
    [TestCase(RelicRarity.Unique, 255, 179, 71)]
    public void CurrentRarityColor_ReturnsConfiguredRelicRarityColor(
        RelicRarity rarity,
        int red,
        int green,
        int blue)
    {
        LobbyRelicOfferButtonUI view = CreateConfiguredOfferView(
            out GameObject root,
            out _,
            out _);

        try
        {
            view.Bind(
                new LobbyRelicOffer("relic-test", 100),
                null,
                rarity,
                null);

            Color actual = view.CurrentRarityColor;
            Color expected = new(red / 255f, green / 255f, blue / 255f, 1f);

            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.0001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.0001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.0001f));
            Assert.That(actual.a, Is.EqualTo(1f));
        }
        finally
        {
            Object.DestroyImmediate(root);
            CleanupRarityRingRendererRoot();
        }
    }

    [Test]
    public void Bind_AppliesRarityColorAndShowsRing()
    {
        LobbyRelicOfferButtonUI view = CreateConfiguredOfferView(
            out GameObject root,
            out GameObject ringRoot,
            out ParticleSystem rarityParticles);

        try
        {
            ringRoot.SetActive(false);

            view.Bind(
                new LobbyRelicOffer("relic-test", 100),
                null,
                RelicRarity.Rare,
                null);

            Color actual = rarityParticles.main.startColor.color;
            RawImage proxyImage = GetRarityRingProxyImage(root);
            ParticleSystem runtimeParticles =
                FindRuntimeRarityParticles(rarityParticles);
            Color runtimeActual = runtimeParticles.main.startColor.color;

            Assert.That(ringRoot.activeSelf, Is.False);
            Assert.That(proxyImage.enabled, Is.True);
            Assert.That(proxyImage.texture, Is.TypeOf<RenderTexture>());
            Assert.That(runtimeParticles.gameObject.activeInHierarchy, Is.True);
            Assert.That(actual.r, Is.EqualTo(78f / 255f).Within(0.0001f));
            Assert.That(actual.g, Is.EqualTo(141f / 255f).Within(0.0001f));
            Assert.That(actual.b, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(runtimeActual.r, Is.EqualTo(78f / 255f).Within(0.0001f));
            Assert.That(runtimeActual.g, Is.EqualTo(141f / 255f).Within(0.0001f));
            Assert.That(runtimeActual.b, Is.EqualTo(1f).Within(0.0001f));
        }
        finally
        {
            Object.DestroyImmediate(root);
            CleanupRarityRingRendererRoot();
        }
    }

    [Test]
    public void Bind_RoutesRarityRingThroughPerOfferUiRenderTextureProxy()
    {
        LobbyRelicOfferButtonUI view = CreateConfiguredOfferView(
            out GameObject root,
            out GameObject ringRoot,
            out ParticleSystem rarityParticles);

        try
        {
            ringRoot.SetActive(false);

            view.Bind(
                new LobbyRelicOffer("relic-test", 100),
                null,
                RelicRarity.Common,
                null);

            RawImage proxyImage = GetRarityRingProxyImage(root);
            RectTransform proxyRect = proxyImage.rectTransform;
            ParticleSystem runtimeParticles =
                FindRuntimeRarityParticles(rarityParticles);

            Assert.That(proxyImage.transform.parent, Is.SameAs(root.transform));
            Assert.That(proxyImage.transform.GetSiblingIndex(), Is.EqualTo(0));
            Assert.That(proxyImage.raycastTarget, Is.False);
            Assert.That(proxyRect.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(proxyRect.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(proxyRect.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(proxyRect.sizeDelta, Is.EqualTo(new Vector2(250f, 250f)));
            Assert.That(ringRoot.activeSelf, Is.False);
            Assert.That(runtimeParticles, Is.Not.SameAs(rarityParticles));
            Assert.That(runtimeParticles.transform.IsChildOf(root.transform), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
            CleanupRarityRingRendererRoot();
        }
    }

    [UnityTest]
    public IEnumerator ShowSold_StopsThenHidesRarityRing()
    {
        LobbyRelicOfferButtonUI view = CreateConfiguredOfferView(
            out GameObject root,
            out GameObject ringRoot,
            out ParticleSystem rarityParticles);

        try
        {
            ParticleSystem.MainModule main = rarityParticles.main;
            main.loop = true;
            main.startLifetime = 0.05f;

            ParticleSystem.EmissionModule emission = rarityParticles.emission;
            emission.rateOverTime = 0f;

            view.Bind(
                new LobbyRelicOffer("relic-test", 100),
                null,
                RelicRarity.Common,
                null);
            RawImage proxyImage = GetRarityRingProxyImage(root);
            ParticleSystem runtimeParticles =
                FindRuntimeRarityParticles(rarityParticles);
            runtimeParticles.Emit(1);

            view.ShowSold();

            Assert.That(ringRoot.activeSelf, Is.False);
            Assert.That(runtimeParticles.isEmitting, Is.False);

            float timeout = Time.realtimeSinceStartup + 0.5f;
            while (proxyImage.enabled && Time.realtimeSinceStartup < timeout)
                yield return null;

            Assert.That(proxyImage.enabled, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
            CleanupRarityRingRendererRoot();
        }
    }

    [Test]
    public void ShowEmpty_WhenSerializedViewIsMissing_DoesNotCreateChildren()
    {
        var root = new GameObject(
            "RelicOffer",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        LobbyRelicOfferButtonUI view = root.AddComponent<LobbyRelicOfferButtonUI>();

        try
        {
            view.ShowEmpty();

            Assert.That(root.transform.childCount, Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void Initialize_WhenSerializedRefreshViewIsMissing_DoesNotCreateChildren()
    {
        var root = new GameObject(
            "RelicRefresh",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        LobbyRelicRefreshButtonUI view = root.AddComponent<LobbyRelicRefreshButtonUI>();

        try
        {
            view.Initialize(null);

            Assert.That(root.transform.childCount, Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void Initialize_PreservesSceneAssignedRefreshSprite()
    {
        var root = new GameObject(
            "RelicRefresh",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        var iconObject = new GameObject(
            "RefreshIcon",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        iconObject.transform.SetParent(root.transform, false);
        var priceObject = new GameObject(
            "Price",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        priceObject.transform.SetParent(root.transform, false);

        Sprite sceneSprite = Sprite.Create(
            new Texture2D(2, 2),
            new Rect(0, 0, 2, 2),
            Vector2.zero);
        Image icon = iconObject.GetComponent<Image>();
        icon.sprite = sceneSprite;
        LobbyRelicRefreshButtonUI view = root.AddComponent<LobbyRelicRefreshButtonUI>();

        try
        {
            view.Initialize(null);
            Assert.That(icon.sprite, Is.SameAs(sceneSprite));
        }
        finally
        {
            Texture2D texture = sceneSprite.texture;
            Object.DestroyImmediate(sceneSprite);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void PointerHover_ReportsRelicAndScalesIconThenRestoresIt()
    {
        LobbyRelicOfferButtonUI view = CreateConfiguredOfferView(
            out GameObject root,
            out _,
            out _);
        string hoveredRelicId = null;
        bool? hoverState = null;

        try
        {
            view.Bind(
                new LobbyRelicOffer("relic-test", 100),
                null,
                RelicRarity.Common,
                null,
                (relicId, hovered) =>
                {
                    hoveredRelicId = relicId;
                    hoverState = hovered;
                });

            RectTransform icon = root.transform.Find("RelicIcon") as RectTransform;
            view.OnPointerEnter(null);

            Assert.That(hoveredRelicId, Is.EqualTo("relic-test"));
            Assert.That(hoverState, Is.True);
            Assert.That(icon.localScale, Is.EqualTo(Vector3.one * 1.12f));
            Assert.That(
                root.transform.Find(RarityRingProxyObjectName).localScale,
                Is.EqualTo(Vector3.one * 1.12f));

            view.OnPointerExit(null);

            Assert.That(hoverState, Is.False);
            Assert.That(icon.localScale, Is.EqualTo(Vector3.one));
            Assert.That(
                root.transform.Find(RarityRingProxyObjectName).localScale,
                Is.EqualTo(Vector3.one));
        }
        finally
        {
            Object.DestroyImmediate(root);
            CleanupRarityRingRendererRoot();
        }
    }

    [Test]
    public void DescriptionView_UsesExistingRelicNameAndEffectWithoutCreatingText()
    {
        GameObject presenterObject = new("Presenter");
        LobbyRelicShopPresenter presenter = presenterObject.AddComponent<LobbyRelicShopPresenter>();
        GameObject panel = new("RelicShopPanel");
        panel.transform.SetParent(presenterObject.transform, false);
        GameObject info = new("relic_info");
        info.transform.SetParent(panel.transform, false);
        TMP_Text name = new GameObject("relic_name", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TMP_Text>();
        name.transform.SetParent(info.transform, false);
        TMP_Text effect = new GameObject("relic_effect", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TMP_Text>();
        effect.transform.SetParent(info.transform, false);

        try
        {
            SetPrivateField(presenter, "panelRoot", panel);
            MethodInfo ensure = typeof(LobbyRelicShopPresenter).GetMethod(
                "EnsureDescriptionView",
                BindingFlags.Instance | BindingFlags.NonPublic);

            ensure.Invoke(presenter, null);

            Assert.That(info.activeSelf, Is.True);
            Assert.That(info.transform.childCount, Is.EqualTo(2));
            Assert.That(GetPrivateField<TMP_Text>(presenter, "relicDescriptionNameText"), Is.SameAs(name));
            Assert.That(GetPrivateField<TMP_Text>(presenter, "relicDescriptionBodyText"), Is.SameAs(effect));
        }
        finally
        {
            Object.DestroyImmediate(presenterObject);
        }
    }

    private static LobbyRelicOfferButtonUI CreateConfiguredOfferView(
        out GameObject root,
        out GameObject ringRoot,
        out ParticleSystem rarityParticles)
    {
        root = new GameObject(
            "RelicOffer",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));

        GameObject iconObject = new(
            "RelicIcon",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        iconObject.transform.SetParent(root.transform, false);

        GameObject priceObject = new(
            "Price",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        priceObject.transform.SetParent(root.transform, false);

        ringRoot = new GameObject("magic_ring_06");
        ringRoot.layer = 9;
        ringRoot.transform.SetParent(root.transform, false);

        GameObject rarityObject = new("03", typeof(ParticleSystem));
        rarityObject.layer = 9;
        rarityObject.transform.SetParent(ringRoot.transform, false);
        rarityParticles = rarityObject.GetComponent<ParticleSystem>();

        LobbyRelicOfferButtonUI view = root.AddComponent<LobbyRelicOfferButtonUI>();
        SetPrivateField(view, "button", root.GetComponent<Button>());
        SetPrivateField(view, "iconImage", iconObject.GetComponent<Image>());
        SetPrivateField(view, "priceText", priceObject.GetComponent<TMP_Text>());
        SetPrivateField(view, "rarityRingRoot", ringRoot);
        SetPrivateField(view, "rarityParticles", rarityParticles);
        return view;
    }

    private static RawImage GetRarityRingProxyImage(GameObject root)
    {
        Transform proxy = root.transform.Find(RarityRingProxyObjectName);
        Assert.That(proxy, Is.Not.Null);

        RawImage proxyImage = proxy.GetComponent<RawImage>();
        Assert.That(proxyImage, Is.Not.Null);
        return proxyImage;
    }

    private static ParticleSystem FindRuntimeRarityParticles(
        ParticleSystem sourceParticles)
    {
        GameObject rendererRoot = GameObject.Find(RarityRingRendererRootName);
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

        Assert.Fail("Runtime rarity particles were not created.");
        return null;
    }

    private static void CleanupRarityRingRendererRoot()
    {
        GameObject rendererRoot = GameObject.Find(RarityRingRendererRootName);
        if (rendererRoot != null)
            Object.DestroyImmediate(rendererRoot);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (T)field.GetValue(target);
    }

    private static GameObject FindGameObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == objectName)
                    return child.gameObject;
            }
        }

        return null;
    }
}
