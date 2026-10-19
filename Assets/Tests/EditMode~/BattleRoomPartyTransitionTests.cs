using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

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
    public void PrepareEntry_UsesExplicitRootOutsidePresenterHierarchy()
    {
        var presenterObject = new GameObject("Presenter");
        var actorRoot = new GameObject("ActorRoot");
        BattleRoomPartyTransitionPresenter presenter =
            presenterObject.AddComponent<BattleRoomPartyTransitionPresenter>();
        SetField(presenter, "entryOffset", new Vector3(-5f, 0f, 0f));
        BattleCharacter character = CreateCharacter(
            actorRoot.transform,
            "Player",
            new Vector3(2f, 3f, 0f),
            4);

        try
        {
            presenter.PrepareEntry(actorRoot.transform);

            Assert.That(character.transform.position, Is.EqualTo(new Vector3(-3f, 3f, 0f)));
        }
        finally
        {
            Object.DestroyImmediate(presenterObject);
            Object.DestroyImmediate(actorRoot);
        }
    }

    [Test]
    public void PrepareEntry_CapturesActorsFromAllProvidedRoots()
    {
        var presenterObject = new GameObject("Presenter");
        var sharedRoot = new GameObject("SharedRoot");
        var roomRoot = new GameObject("RoomRoot");
        BattleRoomPartyTransitionPresenter presenter =
            presenterObject.AddComponent<BattleRoomPartyTransitionPresenter>();
        SetField(presenter, "entryOffset", new Vector3(-5f, 0f, 0f));
        BattleCharacter sharedCharacter = CreateCharacter(
            sharedRoot.transform, "SharedPlayer", new Vector3(2f, 0f, 0f), 1);
        BattleCharacter roomCharacter = CreateCharacter(
            roomRoot.transform, "RoomPlayer", new Vector3(4f, 0f, 0f), 2);

        try
        {
            presenter.PrepareEntry(sharedRoot.transform, roomRoot.transform);

            Assert.That(sharedCharacter.transform.position, Is.EqualTo(new Vector3(-3f, 0f, 0f)));
            Assert.That(roomCharacter.transform.position, Is.EqualTo(new Vector3(-1f, 0f, 0f)));
        }
        finally
        {
            Object.DestroyImmediate(presenterObject);
            Object.DestroyImmediate(sharedRoot);
            Object.DestroyImmediate(roomRoot);
        }
    }

    [Test]
    public void PrepareEntry_CapturesActorsUnderAnInactiveSpawnRoot()
    {
        var presenterObject = new GameObject("Presenter");
        var spawnRoot = new GameObject("InactiveSpawnRoot");
        BattleRoomPartyTransitionPresenter presenter =
            presenterObject.AddComponent<BattleRoomPartyTransitionPresenter>();
        SetField(presenter, "entryOffset", new Vector3(-5f, 0f, 0f));
        BattleCharacter character = CreateCharacter(
            spawnRoot.transform, "Player", new Vector3(3f, 2f, 0f), 1);
        spawnRoot.SetActive(false);

        try
        {
            presenter.PrepareEntry(spawnRoot.transform);

            Assert.That(character.transform.position, Is.EqualTo(new Vector3(-2f, 2f, 0f)));
        }
        finally
        {
            Object.DestroyImmediate(presenterObject);
            Object.DestroyImmediate(spawnRoot);
        }
    }

    [Test]
    public void PrepareEntry_CapturesMapSelectionMarkerWithoutBattleCharacter()
    {
        var presenterObject = new GameObject("Presenter");
        var actorRoot = new GameObject("ActorRoot");
        BattleRoomPartyTransitionPresenter presenter =
            presenterObject.AddComponent<BattleRoomPartyTransitionPresenter>();
        SetField(presenter, "entryOffset", new Vector3(-4f, 0f, 0f));

        var markedActor = new GameObject("MarkedActor");
        markedActor.transform.SetParent(actorRoot.transform);
        markedActor.transform.position = new Vector3(3f, 1f, 0f);
        markedActor.AddComponent<SpriteRenderer>();
        markedActor.AddComponent<BattleMapSelectionCharacterMarker>();

        try
        {
            presenter.PrepareEntry(actorRoot.transform);

            Assert.That(markedActor.transform.position, Is.EqualTo(new Vector3(-1f, 1f, 0f)));
        }
        finally
        {
            Object.DestroyImmediate(presenterObject);
            Object.DestroyImmediate(actorRoot);
        }
    }

    [Test]
    public void MapSelectionMarker_UsesBattleMoveControllerThenRestoresEventController()
    {
        const string eventPrefabPath =
            "Assets/Project/PrefabsR/Character/A/Cha_01_idle_0.prefab";
        const string battlePrefabPath =
            "Assets/Project/PrefabsR/Character/A/A_BattlePrefab.prefab";

        GameObject eventPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(eventPrefabPath);
        GameObject battlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(battlePrefabPath);
        Assert.That(eventPrefab, Is.Not.Null);
        Assert.That(battlePrefab, Is.Not.Null);

        GameObject instance = Object.Instantiate(eventPrefab);
        try
        {
            Animator eventAnimator = instance.GetComponentInChildren<Animator>(true);
            Animator battleAnimator = battlePrefab.GetComponentInChildren<Animator>(true);
            RuntimeAnimatorController eventController = eventAnimator.runtimeAnimatorController;
            RuntimeAnimatorController battleController = battleAnimator.runtimeAnimatorController;
            BattleMapSelectionCharacterMarker marker =
                instance.AddComponent<BattleMapSelectionCharacterMarker>();

            marker.ConfigureTransitionAnimator(battleController);

            Assert.That(marker.PlayTransitionMove(), Is.True);
            Assert.That(eventAnimator.runtimeAnimatorController, Is.SameAs(battleController));

            marker.RestoreTransitionIdle();
            Assert.That(eventAnimator.runtimeAnimatorController, Is.SameAs(eventController));
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void PlayMoveVfxFrom_UsesExplicitCharacterRootInsteadOfDefaultVfxAnchor()
    {
        var sourceObject = new GameObject("MoveVfxTemplate");
        var characterObject = new GameObject("CurrentCharacter");
        var anchorObject = new GameObject("CharacterVfxAnchor");
        var characterRoot = new GameObject("CharacterRoot");
        var vfxPrefab = new GameObject("TransitionMoveVfx");
        anchorObject.transform.SetParent(characterObject.transform, false);
        anchorObject.transform.localPosition = new Vector3(1f, 2f, 3f);
        characterRoot.transform.position = new Vector3(7f, 8f, 9f);
        vfxPrefab.transform.localPosition = new Vector3(-3f, -5.09f, 0f);

        BattleUnitAnimator source = sourceObject.AddComponent<BattleUnitAnimator>();
        BattleUnitAnimator character = characterObject.AddComponent<BattleUnitAnimator>();
        SetField(source, "moveVfx", new BattleVfxEntry
        {
            prefab = vfxPrefab,
            renderMode = BattleVfxRenderMode.DirectWorldRenderer
        });
        SetField(character, "vfxSpawnPoint", anchorObject.transform);

        GameObject spawned = null;
        try
        {
            Assert.That(character.PlayMoveVfxFrom(source, characterRoot.transform), Is.True);

            Transform spawnedTransform = characterRoot.transform.Find("TransitionMoveVfx(Clone)");
            Assert.That(spawnedTransform, Is.Not.Null);
            Assert.That(spawnedTransform.position, Is.EqualTo(characterRoot.transform.position));
            Assert.That(spawnedTransform.localPosition, Is.EqualTo(Vector3.zero));
            spawned = spawnedTransform.gameObject;
        }
        finally
        {
            if (spawned != null)
                Object.DestroyImmediate(spawned);
            Object.DestroyImmediate(sourceObject);
            Object.DestroyImmediate(characterObject);
            Object.DestroyImmediate(characterRoot);
            Object.DestroyImmediate(vfxPrefab);
        }
    }

    [Test]
    public void CalculateMoveVfxScaleMultiplier_MatchesBattleSpawnScale()
    {
        Vector3 multiplier = BattleUnitAnimator.CalculateMoveVfxScaleMultiplier(
            new Vector3(0.18f, 0.18f, 0.18f),
            new Vector3(2f, 3f, 4f));

        Assert.That(multiplier.x, Is.EqualTo(0.09f).Within(0.0001f));
        Assert.That(multiplier.y, Is.EqualTo(0.06f).Within(0.0001f));
        Assert.That(multiplier.z, Is.EqualTo(0.045f).Within(0.0001f));
    }

    [Test]
    public void MoveCharacterVfx_PreservesBattleRootPositionOffset()
    {
        const string vfxPrefabPath = "Assets/Project/Art/VFX/Cha/Vfx_move_cha.prefab";
        GameObject vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(vfxPrefabPath);

        Assert.That(vfxPrefab, Is.Not.Null);
        Assert.That(vfxPrefab.transform.localPosition, Is.EqualTo(new Vector3(-3f, -5.09f, 0f)));
    }

    [Test]
    public void ResolveMovingCharacterAnchor_PrefersCharacterTransform()
    {
        var ally = new GameObject("Ally");
        var spawnPoint = new GameObject("SpawnPoint");

        try
        {
            Assert.That(
                MapRoomController.ResolveMovingCharacterAnchor(ally, spawnPoint.transform),
                Is.SameAs(ally.transform));
        }
        finally
        {
            Object.DestroyImmediate(ally);
            Object.DestroyImmediate(spawnPoint);
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
