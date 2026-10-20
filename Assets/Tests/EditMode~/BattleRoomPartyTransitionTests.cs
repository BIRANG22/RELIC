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
    public async Task MapToRoomTransition_InvokesUncoverStartedImmediatelyBeforeUncoverPlayback()
    {
        var transitionObject = new GameObject("BattlePanelTransition");
        BattleDiagonalSceneTransition transition =
            transitionObject.AddComponent<BattleDiagonalSceneTransition>();
        var callbacks = new List<string>();

        try
        {
            SetPrivateField(transition, "coverDuration", 0f);
            SetPrivateField(transition, "uncoverDuration", 0f);
            SetPrivateField(transition, "coveredHoldDuration", 0f);

            await transition.PlayMapToRoomAsync(
                () => callbacks.Add("covered"),
                () => callbacks.Add("uncover"));

            CollectionAssert.AreEqual(new[] { "covered", "uncover" }, callbacks);
        }
        finally
        {
            Object.DestroyImmediate(transitionObject);
        }
    }

    [Test]
    public void BattleRoomLoader_ExposesPreparedEntryPlaybackAndDeferredMonsterCompletion()
    {
        Assert.That(
            typeof(BattleRoomLoader).GetMethod("PlayPreparedPartyEntry", BindingFlags.Instance | BindingFlags.Public),
            Is.Not.Null);
        Assert.That(
            typeof(BattleRoomLoader).GetMethod("CompleteDeferredMonsterSpawn", BindingFlags.Instance | BindingFlags.Public),
            Is.Not.Null);
    }

    [Test]
    public void BossEntry_LoadsPartyBeforeDeferredRevealAndWaitsForWalkCompletion()
    {
        string controllerPath = Path.Combine(
            Application.dataPath,
            "Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs");
        string source = File.ReadAllText(controllerPath);

        StringAssert.Contains(
            "ShowRoomBackground(battleRoom, nodeData, true, false);",
            source);
        StringAssert.Contains(
            "LoadBattleRoomNow();\n            deferredBossIntroRoutine = StartCoroutine(PlayDeferredBossIntroRoutine());",
            source.Replace("\r\n", "\n"));

        int routineStart = source.IndexOf(
            "private IEnumerator PlayDeferredBossIntroRoutine",
            System.StringComparison.Ordinal);
        int routineEnd = source.IndexOf(
            "private void CompleteDeferredBossIntro",
            routineStart,
            System.StringComparison.Ordinal);
        string routine = source.Substring(routineStart, routineEnd - routineStart);
        int waitForLoad = routine.IndexOf("!loader.IsLoaded", System.StringComparison.Ordinal);
        int waitForWalk = routine.IndexOf("partyPresenter.IsPlaying", System.StringComparison.Ordinal);
        int playReveal = routine.IndexOf("PlayCurrentBossReveal", System.StringComparison.Ordinal);

        Assert.That(waitForLoad, Is.GreaterThanOrEqualTo(0));
        Assert.That(waitForWalk, Is.GreaterThan(waitForLoad));
        Assert.That(playReveal, Is.GreaterThan(waitForWalk));

        int completionStart = routineEnd;
        int completionEnd = source.IndexOf(
            "private void HandleBattleRoomIntroCompletedAndLoad",
            completionStart,
            System.StringComparison.Ordinal);
        string completion = source.Substring(completionStart, completionEnd - completionStart);
        int spawnBoss = completion.IndexOf("CompleteDeferredMonsterSpawn", System.StringComparison.Ordinal);
        int releaseIntro = completion.IndexOf("SetBattleRoomIntroPlaying(false)", System.StringComparison.Ordinal);

        Assert.That(spawnBoss, Is.GreaterThanOrEqualTo(0));
        Assert.That(releaseIntro, Is.GreaterThan(spawnBoss));
    }

    [Test]
    public void MapSelection_StartsBattlePartyEntryFromUncoverCallbackOnly()
    {
        string controllerPath = Path.Combine(
            Application.dataPath,
            "Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs");
        string source = File.ReadAllText(controllerPath);
        int selectionStart = source.IndexOf(
            "public async void OnMapNodeSelected(GeneratedMapNodeData nodeData)",
            System.StringComparison.Ordinal);
        int selectionEnd = source.IndexOf(
            "public void OnMapNodeSelectedByIndex",
            selectionStart,
            System.StringComparison.Ordinal);
        string selection = source.Substring(selectionStart, selectionEnd - selectionStart);

        int openRoom = selection.IndexOf("HandleSelectedMap(nodeData);", System.StringComparison.Ordinal);
        int uncoverCallback = selection.IndexOf(
            "PlayPreparedBattleRoomPartyEntry();",
            System.StringComparison.Ordinal);

        Assert.That(openRoom, Is.GreaterThanOrEqualTo(0));
        Assert.That(uncoverCallback, Is.GreaterThan(openRoom));
        StringAssert.Contains("if (!isBattleNode)", selection);
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field: {fieldName}");
        field.SetValue(target, value);
    }

    [Test]
    public void StageEntryRoutine_StartsRoomAndPartyEntryBeforePositionPanelFade()
    {
        string controllerPath = Path.Combine(
            Application.dataPath,
            "Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs");
        string source = File.ReadAllText(controllerPath);
        int routineStart = source.IndexOf(
            "private IEnumerator PlayStageEntryThenOpenNodeRoutine",
            System.StringComparison.Ordinal);
        int routineEnd = source.IndexOf(
            "private void AutoFindBack2NameIfNeeded",
            routineStart,
            System.StringComparison.Ordinal);
        string routine = source.Substring(routineStart, routineEnd - routineStart);

        int openRoom = routine.IndexOf("HandleSelectedMap(entryNode);", System.StringComparison.Ordinal);
        int startWalk = routine.IndexOf("StartSharedPartyEntryTransitionIfNeeded();", System.StringComparison.Ordinal);
        int fade = routine.IndexOf("float fadeDuration", System.StringComparison.Ordinal);
        int hidePanel = routine.IndexOf("positionPanel.SetActive(false);", System.StringComparison.Ordinal);
        int continueIntro = routine.IndexOf("CountPresentedRoomEntry(entryNode);", System.StringComparison.Ordinal);

        Assert.That(openRoom, Is.GreaterThanOrEqualTo(0));
        Assert.That(startWalk, Is.GreaterThan(openRoom));
        Assert.That(fade, Is.GreaterThan(startWalk));
        Assert.That(hidePanel, Is.GreaterThan(fade));
        Assert.That(continueIntro, Is.GreaterThan(hidePanel));
    }

    [Test]
    public void CharacterHudVisibilitySuppression_ShowsOnlySelectedHudAfterRelease()
    {
        var hudRoot = new GameObject("HUDRoot");
        var selectedObject = new GameObject("SelectedHUD");
        var unselectedObject = new GameObject("UnselectedHUD");
        selectedObject.transform.SetParent(hudRoot.transform);
        unselectedObject.transform.SetParent(hudRoot.transform);
        HUDSlot selectedHud = selectedObject.AddComponent<HUDSlot>();
        HUDSlot unselectedHud = unselectedObject.AddComponent<HUDSlot>();
        selectedHud.Bind(new Relic.Gameplay.Data.CharacterRuntimeData
            { CharacterId = "Selected", MaxHP = 10, CurrentHP = 10 });
        unselectedHud.Bind(new Relic.Gameplay.Data.CharacterRuntimeData
            { CharacterId = "Unselected", MaxHP = 10, CurrentHP = 10 });
        selectedHud.Hide();
        unselectedHud.Hide();

        try
        {
            BattleRoomLoader.SetCharacterWorldHudVisibilitySuppressed(hudRoot.transform, true);
            selectedHud.SetCharacterSelected(true);

            Assert.That(selectedObject.activeSelf, Is.False);
            Assert.That(unselectedObject.activeSelf, Is.False);

            BattleRoomLoader.SetCharacterWorldHudVisibilitySuppressed(hudRoot.transform, false);

            Assert.That(selectedObject.activeSelf, Is.True);
            Assert.That(unselectedObject.activeSelf, Is.True);
            Assert.That(selectedObject.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            Assert.That(unselectedObject.GetComponent<CanvasGroup>().alpha, Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(hudRoot);
        }
    }

    [Test]
    public void BattleScene_TransitionUsesDoubleDistanceAndDurationForVisibleWalkLoop()
    {
        string scenePath = Path.Combine(Application.dataPath, "Project/Scenes/YDM/Battle.unity");
        string sceneYaml = File.ReadAllText(scenePath);

        MatchCollection entryOffsets = Regex.Matches(
            sceneYaml,
            @"entryOffset: \{x: -5\.3333335, y: 0, z: 0\}");
        MatchCollection exitOffsets = Regex.Matches(
            sceneYaml,
            @"exitOffset: \{x: 5\.3333335, y: 0, z: 0\}");
        MatchCollection entryDurations = Regex.Matches(sceneYaml, @"entryDuration: 1\.1");
        MatchCollection exitDurations = Regex.Matches(sceneYaml, @"exitDuration: 1\.2");
        MatchCollection immediateBattleEntries = Regex.Matches(sceneYaml, @"entryDelay: 0(?:\r?\n)");
        MatchCollection sharedRoomEntryDelays = Regex.Matches(sceneYaml, @"entryDelay: 0\.05");

        Assert.That(entryOffsets.Count, Is.EqualTo(2));
        Assert.That(exitOffsets.Count, Is.EqualTo(2));
        Assert.That(entryDurations.Count, Is.EqualTo(2));
        Assert.That(exitDurations.Count, Is.EqualTo(2));
        Assert.That(immediateBattleEntries.Count, Is.EqualTo(1));
        Assert.That(sharedRoomEntryDelays.Count, Is.EqualTo(1));
    }

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
    public void MapSelectionMarker_UsesBattleWalkControllerThenRestoresEventController()
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

            Assert.That(marker.PlayTransitionWalk(), Is.True);
            Assert.That(eventAnimator.runtimeAnimatorController, Is.SameAs(battleController));
            Assert.That(eventAnimator.GetCurrentAnimatorStateInfo(0).IsName("walk"), Is.True);

            marker.RestoreTransitionIdle();
            Assert.That(eventAnimator.runtimeAnimatorController, Is.SameAs(eventController));
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void PlayWalk_DoesNotSpawnConfiguredMoveVfx()
    {
        var characterObject = new GameObject("TransitionCharacter");
        var vfxPrefab = new GameObject("TransitionMoveVfx");

        BattleUnitAnimator character = characterObject.AddComponent<BattleUnitAnimator>();
        SetField(character, "moveVfx", new BattleVfxEntry
        {
            prefab = vfxPrefab,
            renderMode = BattleVfxRenderMode.IndividualWorldRenderTexture
        });

        try
        {
            character.PlayWalk();

            Assert.That(characterObject.transform.Find("TransitionMoveVfx(Clone)"), Is.Null);
        }
        finally
        {
            Object.DestroyImmediate(characterObject);
            Object.DestroyImmediate(vfxPrefab);
        }
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
