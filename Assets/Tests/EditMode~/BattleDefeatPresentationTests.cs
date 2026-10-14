using System.Collections.Generic;
using System.IO;
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
        string scene = File.ReadAllText("Assets/Project/Scenes/YDM/Battle.unity");

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

    private BattleCharacter CreateCharacter(string characterId, float x)
    {
        var gameObject = new GameObject(characterId);
        createdObjects.Add(gameObject);
        gameObject.transform.position = new Vector3(x, 0f, 0f);

        BattleCharacter character = gameObject.AddComponent<BattleCharacter>();
        character.Initialize(new CharacterRuntimeData { CharacterId = characterId, CurrentHP = 0 });
        return character;
    }
}
