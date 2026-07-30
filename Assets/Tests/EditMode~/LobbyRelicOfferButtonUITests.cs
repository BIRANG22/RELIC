using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class LobbyRelicOfferButtonUITests
{
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
            view.Initialize(null, null);

            Assert.That(root.transform.childCount, Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(root);
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
}
