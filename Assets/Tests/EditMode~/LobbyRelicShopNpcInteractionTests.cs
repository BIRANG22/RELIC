using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyRelicShopNpcInteractionTests
{
    private const string ScenePath = "Assets/Project/Scenes/YDM/Lobby.unity";
    private const string InteractionPath =
        "Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicShopNpcInteraction.cs";
    private const string InteractionGuid = "8f1d8c3bb10d4efc965c2a6bdac3a9e1";

    [Test]
    public void LobbyNpc_UsesMouseReleaseInteractionConnectedToShopPresenter()
    {
        string source = File.ReadAllText(InteractionPath);
        string scene = File.ReadAllText(ScenePath);

        Assert.That(source, Does.Contain("private void OnMouseUpAsButton()"));
        Assert.That(source, Does.Not.Contain("private void OnMouseDown()"));
        Assert.That(source, Does.Contain("presenter?.Open();"));
        Assert.That(scene, Does.Contain($"guid: {InteractionGuid}"));
        Assert.That(scene, Does.Contain("presenter: {fileID: 2200000502}"));
    }

    [Test]
    public void LobbyScene_ContainsSerializedRelicShopPanelReferences()
    {
        string presenter = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Lobby/RelicShop/LobbyRelicShopPresenter.cs");
        string scene = File.ReadAllText(ScenePath);

        Assert.That(presenter, Does.Contain("[SerializeField] private GameObject panelRoot;"));
        Assert.That(presenter, Does.Contain("[SerializeField] private LobbyRelicOfferButtonUI[] offerButtons"));
        Assert.That(presenter, Does.Contain("[SerializeField] private LobbyRelicRefreshButtonUI refreshButton;"));
        Assert.That(scene, Does.Contain("m_Name: RelicShopPanel"));
        Assert.That(scene, Does.Contain("panelRoot: {fileID: 230010000}"));
        Assert.That(scene, Does.Contain("- {fileID: 230010020}"));
        Assert.That(scene, Does.Contain("- {fileID: 230010030}"));
        Assert.That(scene, Does.Contain("- {fileID: 230010040}"));
        Assert.That(scene, Does.Contain("refreshButton: {fileID: 230010051}"));
    }

    [Test]
    public void LobbyScene_RelicShopViewsUseSerializedImagesAndPriceTexts()
    {
        Scene previewScene = EditorSceneManager.OpenPreviewScene(ScenePath);

        try
        {
            LobbyRelicShopPresenter presenter = FindComponentInScene<LobbyRelicShopPresenter>(previewScene);
            Assert.That(presenter, Is.Not.Null);

            var serializedPresenter = new SerializedObject(presenter);
            SerializedProperty offerButtons = serializedPresenter.FindProperty("offerButtons");
            Assert.That(offerButtons.arraySize, Is.EqualTo(3));

            for (int i = 0; i < offerButtons.arraySize; i++)
            {
                LobbyRelicOfferButtonUI offer =
                    offerButtons.GetArrayElementAtIndex(i).objectReferenceValue as LobbyRelicOfferButtonUI;
                Assert.That(offer, Is.Not.Null);
                AssertViewReferencesAreSerialized(offer);
                AssertRarityRingReferencesAreSerialized(offer);
            }

            LobbyRelicRefreshButtonUI refresh =
                serializedPresenter.FindProperty("refreshButton").objectReferenceValue as LobbyRelicRefreshButtonUI;
            Assert.That(refresh, Is.Not.Null);
            AssertViewReferencesAreSerialized(refresh);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    private static void AssertViewReferencesAreSerialized(MonoBehaviour view)
    {
        var serializedView = new SerializedObject(view);
        Assert.That(serializedView.FindProperty("button").objectReferenceValue, Is.Not.Null);
        Assert.That(serializedView.FindProperty("iconImage").objectReferenceValue, Is.Not.Null);
        Assert.That(serializedView.FindProperty("priceText").objectReferenceValue, Is.Not.Null);
    }

    private static void AssertRarityRingReferencesAreSerialized(LobbyRelicOfferButtonUI offer)
    {
        var serializedView = new SerializedObject(offer);
        SerializedProperty ringProperty = serializedView.FindProperty("rarityRingRoot");
        SerializedProperty particlesProperty = serializedView.FindProperty("rarityParticles");

        Assert.That(ringProperty, Is.Not.Null);
        Assert.That(particlesProperty, Is.Not.Null);

        GameObject ringRoot = ringProperty.objectReferenceValue as GameObject;
        ParticleSystem rarityParticles = particlesProperty.objectReferenceValue as ParticleSystem;

        Assert.That(ringRoot, Is.Not.Null);
        Assert.That(rarityParticles, Is.Not.Null);
        Assert.That(ringRoot.transform.parent, Is.EqualTo(offer.transform));
        Assert.That(rarityParticles.transform.name, Is.EqualTo("03"));
        Assert.That(rarityParticles.transform.IsChildOf(ringRoot.transform), Is.True);
    }

    private static T FindComponentInScene<T>(Scene scene)
        where T : Component
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            T component = roots[i].GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }
}
