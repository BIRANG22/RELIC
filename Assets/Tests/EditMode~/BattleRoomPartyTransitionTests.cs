using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UnityEngine;

public sealed class BattleRoomPartyTransitionTests
{
    [Test]
    public void BattleSceneYaml_HasUniqueLocalObjectIdentifiers()
    {
        string scenePath = Path.Combine(Application.dataPath, "Project/Scenes/YDM/Battle.unity");
        string sceneYaml = File.ReadAllText(scenePath);
        MatchCollection declarations = Regex.Matches(sceneYaml, @"^--- !u!\d+ &(\d+)$", RegexOptions.Multiline);
        var identifiers = new HashSet<string>();

        foreach (Match declaration in declarations)
        {
            string identifier = declaration.Groups[1].Value;
            Assert.That(identifiers.Add(identifier), Is.True, $"Duplicate local object identifier: {identifier}");
        }
    }

    [Test]
    public void GetEntryStart_AddsEntryOffsetToFinalPosition()
    {
        Vector3 result = BattleRoomPartyTransitionMath.GetEntryStart(
            new Vector3(2f, 3f, 4f),
            new Vector3(-8f, 0f, 0f));

        Assert.That(result, Is.EqualTo(new Vector3(-6f, 3f, 4f)));
    }

    [Test]
    public void GetExitTarget_AddsExitOffsetToCurrentPosition()
    {
        Vector3 result = BattleRoomPartyTransitionMath.GetExitTarget(
            new Vector3(2f, 3f, 4f),
            new Vector3(8f, 0f, 0f));

        Assert.That(result, Is.EqualTo(new Vector3(10f, 3f, 4f)));
    }

    [Test]
    public void Evaluate_UsesClampedNormalizedTime()
    {
        Vector3 from = Vector3.zero;
        Vector3 to = new Vector3(10f, 20f, 30f);

        Assert.That(BattleRoomPartyTransitionMath.Evaluate(from, to, -1f, null), Is.EqualTo(from));
        Assert.That(BattleRoomPartyTransitionMath.Evaluate(from, to, 0.5f, null), Is.EqualTo(to * 0.5f));
        Assert.That(BattleRoomPartyTransitionMath.Evaluate(from, to, 2f, null), Is.EqualTo(to));
    }

    [Test]
    public void PrepareEntry_MovesAllPlayersLeftWithoutChangingGridIndex()
    {
        var root = new GameObject("BattleRoom");
        BattleRoomPartyTransitionPresenter presenter = root.AddComponent<BattleRoomPartyTransitionPresenter>();
        SetField(presenter, "entryOffset", new Vector3(-5f, 0f, 0f));

        BattleCharacter first = CreateCharacter(root.transform, "First", new Vector3(1f, 2f, 0f), 3);
        BattleCharacter second = CreateCharacter(root.transform, "Second", new Vector3(4f, 5f, 0f), 7);

        try
        {
            presenter.PrepareEntry();

            Assert.That(first.transform.position, Is.EqualTo(new Vector3(-4f, 2f, 0f)));
            Assert.That(second.transform.position, Is.EqualTo(new Vector3(-1f, 5f, 0f)));
            Assert.That(first.CurrentGridIndex, Is.EqualTo(3));
            Assert.That(second.CurrentGridIndex, Is.EqualTo(7));

            presenter.CancelAndRestore(true);
            Assert.That(first.transform.position, Is.EqualTo(new Vector3(1f, 2f, 0f)));
            Assert.That(second.transform.position, Is.EqualTo(new Vector3(4f, 5f, 0f)));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void PlayPreparedEntryAsync_WithZeroDuration_ReturnsCompletedTask()
    {
        var root = new GameObject("BattleRoom");
        BattleRoomPartyTransitionPresenter presenter = root.AddComponent<BattleRoomPartyTransitionPresenter>();
        SetField(presenter, "entryDelay", 0f);
        SetField(presenter, "entryDuration", 0f);
        CreateCharacter(root.transform, "Player", Vector3.zero, 1);

        try
        {
            presenter.PrepareEntry();

            Task task = null;
            Assert.DoesNotThrow(() => task = presenter.PlayPreparedEntryAsync());
            Assert.That(task, Is.Not.Null);
            Assert.That(task.IsCompleted, Is.True);
            Assert.That(presenter.IsPlaying, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void CancelAndRestore_WhenCapturedAnimatorWasDestroyed_DoesNotThrow()
    {
        var root = new GameObject("BattleRoom");
        BattleRoomPartyTransitionPresenter presenter = root.AddComponent<BattleRoomPartyTransitionPresenter>();
        BattleCharacter character = CreateCharacter(root.transform, "Player", Vector3.zero, 1);
        BattleUnitAnimator animator = character.gameObject.AddComponent<BattleUnitAnimator>();

        try
        {
            presenter.PrepareEntry();
            Object.DestroyImmediate(animator);

            Assert.DoesNotThrow(() => presenter.CancelAndRestore(true));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static BattleCharacter CreateCharacter(
        Transform parent,
        string name,
        Vector3 position,
        int gridIndex)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent);
        gameObject.transform.position = position;
        gameObject.AddComponent<SpriteRenderer>();
        BattleCharacter character = gameObject.AddComponent<BattleCharacter>();
        character.SetGridIndex(gridIndex);
        return character;
    }

    private static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }
}
