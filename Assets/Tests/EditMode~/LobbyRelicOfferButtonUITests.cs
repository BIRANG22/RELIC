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
    [TestCase(RelicRarity.Uncommon, 92, 219, 131)]
    [TestCase(RelicRarity.Rare, 78, 141, 255)]
    [TestCase(RelicRarity.Unique, 255, 179, 71)]
    public void GetColor_ReturnsConfiguredRelicRarityColor(
        RelicRarity rarity,
        int red,
        int green,
        int blue)
    {
        Color actual = LobbyRelicRarityPalette.GetColor(rarity);
        Color expected = new(red / 255f, green / 255f, blue / 255f, 1f);

        Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.0001f));
        Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.0001f));
        Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.0001f));
        Assert.That(actual.a, Is.EqualTo(1f));
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
            Assert.That(ringRoot.activeSelf, Is.True);
            Assert.That(actual.r, Is.EqualTo(78f / 255f).Within(0.0001f));
            Assert.That(actual.g, Is.EqualTo(141f / 255f).Within(0.0001f));
            Assert.That(actual.b, Is.EqualTo(1f).Within(0.0001f));
        }
        finally
        {
            Object.DestroyImmediate(root);
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
            rarityParticles.Emit(1);

            view.ShowSold();

            Assert.That(ringRoot.activeSelf, Is.True);
            Assert.That(rarityParticles.isEmitting, Is.False);

            float timeout = Time.realtimeSinceStartup + 0.5f;
            while (ringRoot.activeSelf && Time.realtimeSinceStartup < timeout)
                yield return null;

            Assert.That(ringRoot.activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
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

            view.OnPointerExit(null);

            Assert.That(hoverState, Is.False);
            Assert.That(icon.localScale, Is.EqualTo(Vector3.one));
        }
        finally
        {
            Object.DestroyImmediate(root);
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
        ringRoot.transform.SetParent(root.transform, false);

        GameObject rarityObject = new("03", typeof(ParticleSystem));
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
