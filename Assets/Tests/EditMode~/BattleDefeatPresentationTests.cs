using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class BattleDefeatPresentationTests
{
    private readonly List<GameObject> createdObjects = new();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
            Object.DestroyImmediate(createdObjects[i]);

        createdObjects.Clear();
    }

    [Test]
    public void SelectCharacters_OrdersByWorldXThenCharacterId_AndLimitsToThree()
    {
        BattleCharacter right = CreateCharacter("C_Right", 5f);
        BattleCharacter sameXSecond = CreateCharacter("C_B", 0f);
        BattleCharacter left = CreateCharacter("C_Left", -3f);
        BattleCharacter sameXFirst = CreateCharacter("C_A", 0f);

        IReadOnlyList<BattleCharacter> selected = BattleDefeatPresentation.SelectCharacters(
            new[] { right, sameXSecond, left, sameXFirst },
            3);

        Assert.That(selected, Has.Count.EqualTo(3));
        Assert.That(selected[0].CharacterId, Is.EqualTo("C_Left"));
        Assert.That(selected[1].CharacterId, Is.EqualTo("C_A"));
        Assert.That(selected[2].CharacterId, Is.EqualTo("C_B"));
    }

    [TestCase(1, new[] { 1 })]
    [TestCase(2, new[] { 0, 2 })]
    [TestCase(3, new[] { 0, 1, 2 })]
    public void ResolveAnchorIndices_CentersIncompleteParties(int count, int[] expected)
    {
        Assert.That(BattleDefeatPresentation.ResolveAnchorIndices(count), Is.EqualTo(expected));
    }

    [Test]
    public void ResolveAnchorIndices_ClampsUnsupportedCounts()
    {
        Assert.That(BattleDefeatPresentation.ResolveAnchorIndices(0), Is.Empty);
        Assert.That(BattleDefeatPresentation.ResolveAnchorIndices(4), Is.EqualTo(new[] { 0, 1, 2 }));
    }

    [Test]
    public void BattleResultChecker_UsesPresentationOnlyForPartyWipe()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/BattleResultChecker.cs");

        Assert.That(source, Does.Contain("ShowDefeatAfterCameraReturn(playPresentation: false)"));
        Assert.That(source, Does.Contain("ShowDefeatAfterCameraReturn(playPresentation: true)"));
        Assert.That(source, Does.Contain("defeatPresentation.Play"));
    }

    [Test]
    public void ExplorationResultPanel_DelaysBattleRoomCleanupUntilReturnToBase()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/ExplorationResultPanelUI.cs");
        int returnStart = source.IndexOf("private async void ReturnToBase", System.StringComparison.Ordinal);

        Assert.That(returnStart, Is.GreaterThanOrEqualTo(0));
        Assert.That(source.Substring(returnStart), Does.Contain("BattleRoomCleaner"));

        string checker = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/BattleResultChecker.cs");
        int openDefeat = checker.IndexOf("private void OpenDefeatExplorationResultPanel", System.StringComparison.Ordinal);
        int nextMethod = checker.IndexOf("private static", openDefeat + 1, System.StringComparison.Ordinal);
        string openDefeatBody = checker.Substring(openDefeat, nextMethod - openDefeat);
        Assert.That(openDefeatBody, Does.Not.Contain("CleanBattleRoom"));
    }

    [Test]
    public void BattleScene_HasWiredDefeatPresentationHierarchy()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity")
            .Replace("\r\n", "\n");

        Assert.That(scene, Does.Contain("m_Name: BattleDefeatPresentation"));
        Assert.That(scene, Does.Contain("m_Name: BlackFog"));
        Assert.That(scene, Does.Contain("m_Name: CharacterAnchorLeft"));
        Assert.That(scene, Does.Contain("m_Name: CharacterAnchorCenter"));
        Assert.That(scene, Does.Contain("m_Name: CharacterAnchorRight"));
        Assert.That(scene, Does.Contain("m_Name: ExplorationFailedText"));
        Assert.That(scene, Does.Contain("defeatPresentation: {fileID: 900000000000000003}"));
    }

    [Test]
    public void PresenterDisable_CancelsWithoutOpeningResultPanel()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/Reward/BattleDefeatPresentation.cs");
        int disableStart = source.IndexOf("private void OnDisable", System.StringComparison.Ordinal);
        string disableBody = source.Substring(disableStart);

        Assert.That(disableBody, Does.Not.Contain("callback?.Invoke"));
        Assert.That(disableBody, Does.Contain("completion = null"));
    }

    [Test]
    public void ApplyLayerRecursively_MovesWholeCharacterHierarchyToPresentationLayer()
    {
        GameObject root = CreateObject("CharacterRoot");
        GameObject child = CreateObject("CharacterChild");
        child.transform.SetParent(root.transform);
        root.layer = 0;
        child.layer = 9;

        MethodInfo applyLayerMethod = typeof(BattleDefeatPresentation).GetMethod(
            "ApplyLayerRecursively",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(applyLayerMethod, Is.Not.Null);
        applyLayerMethod.Invoke(null, new object[] { root, 12 });

        Assert.That(root.layer, Is.EqualTo(12));
        Assert.That(child.layer, Is.EqualTo(12));
    }

    [Test]
    public void PreparePresentation_MovesCharacterToPresentationSortingLayer()
    {
        GameObject presenterObject = CreateObject("Presenter");
        BattleDefeatPresentation presenter = presenterObject.AddComponent<BattleDefeatPresentation>();
        BattleCharacter character = CreateCharacter("C_Selected", 0f);
        SpriteRenderer renderer = character.gameObject.AddComponent<SpriteRenderer>();
        renderer.sortingLayerID = SortingLayer.NameToID("Unit");
        int presentationSortingLayerId = SortingLayer.NameToID("DefeatPresentation");

        MethodInfo prepareMethod = typeof(BattleDefeatPresentation).GetMethod(
            "PreparePresentation",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(prepareMethod, Is.Not.Null);
        prepareMethod.Invoke(presenter, new object[] { new[] { character } });

        Assert.That(renderer.sortingLayerID, Is.EqualTo(presentationSortingLayerId));
    }

    [Test]
    public void BattleScene_WiresDefeatOverlayCameraAndCoveredUiRoots()
    {
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity")
            .Replace("\r\n", "\n");
        string tags = File.ReadAllText("ProjectSettings/TagManager.asset");

        Assert.That(tags, Does.Contain("- DefeatPresentation"));
        Assert.That(scene, Does.Contain("m_Name: DefeatOverlayCamera"));
        Assert.That(scene, Does.Contain("m_CameraType: 1"));
        Assert.That(scene, Does.Contain("m_Cameras:\n  - {fileID: 900000000000000103}"));
        Assert.That(scene, Does.Contain("- {fileID: 1585080120}"));
        Assert.That(scene, Does.Contain("m_Name: DefeatPresentationLight"));
        Assert.That(scene, Does.Contain("m_ApplyToSortingLayers: 5a4eaa67"));

        int fogStart = scene.IndexOf("m_Name: BlackFog", System.StringComparison.Ordinal);
        int nextObject = scene.IndexOf("\n--- !u!1", fogStart, System.StringComparison.Ordinal);
        string fogBlock = scene.Substring(fogStart, nextObject - fogStart);
        Assert.That(fogBlock, Does.Contain("m_Color: {r: 0, g: 0, b: 0, a: 1}"));
    }

    [Test]
    public void DefeatFog_WritesDepthAndUsesDedicatedMaterial()
    {
        string shader = File.ReadAllText("Assets/Project/Shaders/DefeatFogDepth.shader");
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity")
            .Replace("\r\n", "\n");

        Assert.That(shader, Does.Contain("ZWrite On"));
        Assert.That(shader, Does.Contain("ZTest Always"));
        Assert.That(shader, Does.Contain("_EdgeWidth"));
        Assert.That(shader, Does.Contain("_NoiseStrength"));
        Assert.That(shader, Does.Contain("_NoiseSpeed"));
        Assert.That(shader, Does.Contain("smoothstep"));
        Assert.That(shader, Does.Contain("Blend SrcAlpha OneMinusSrcAlpha"));
        Assert.That(scene, Does.Contain(
            "m_Materials:\n  - {fileID: 2100000, guid: d1e7fa70000000000000000000000002, type: 2}"));
        Assert.That(scene, Does.Contain("m_LocalPosition: {x: 0, y: 15, z: 0.5}"));
    }

    private BattleCharacter CreateCharacter(string characterId, float x)
    {
        var gameObject = new GameObject(characterId);
        createdObjects.Add(gameObject);
        gameObject.transform.position = new Vector3(x, 0f, 0f);

        BattleCharacter character = gameObject.AddComponent<BattleCharacter>();
        character.Initialize(new CharacterRuntimeData { CharacterId = characterId, CurrentHP = 0 });
        return character;
    }

    private GameObject CreateObject(string name)
    {
        var gameObject = new GameObject(name);
        createdObjects.Add(gameObject);
        return gameObject;
    }
}
