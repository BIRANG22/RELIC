using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public sealed class ArabellaSpawnPresentationTests
{
    private const string PrefabPath =
        "Assets/Project/PrefabsR/Monster/Boss/01.Arabella/Arabella.prefab";
    private const string ControllerPath =
        "Assets/Project/PrefabsR/Monster/Boss/01.Arabella/Controller/Arabella_BattleAnimatorController.controller";
    private const string SceneControllerPath =
        "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleSceneController.cs";
    private const string LoaderPath =
        "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleRoomLoader.cs";
    private const string PresenterPath =
        "Assets/Project/Scripts/Gameplay/Monster/ArabellaSpawnPresentation.cs";
    private const string CameraControllerPath =
        "Assets/Project/Scripts/Gameplay/Scene/Battle/BattleRoom/BattleCameraController.cs";
    private const string BattleScenePath =
        "Assets/Project/Scenes/YDM/Battle.unity";
    private const string DebugBattleScenePath =
        "Assets/Project/Scenes/YDM/DebugBattle.unity";

    [Test]
    public void Presenter_ExposesOneShotAppearSequenceContract()
    {
        System.Type type = typeof(ArabellaSpawnPresentation);

        Assert.That(type.GetProperty("HasPlayed", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        Assert.That(type.GetProperty("CanPlay", BindingFlags.Instance | BindingFlags.Public), Is.Not.Null);
        MethodInfo play = type.GetMethod("Play", BindingFlags.Instance | BindingFlags.Public);
        Assert.That(play, Is.Not.Null);
        Assert.That(play.ReturnType, Is.EqualTo(typeof(IEnumerator)));
    }

    [Test]
    public void Presenter_UsesApprovedDialogueKeysAndFallbacks()
    {
        Assert.That(ArabellaSpawnPresentation.FirstDialogueKey,
            Is.EqualTo("battle.boss.arabella.appear.01"));
        Assert.That(ArabellaSpawnPresentation.SecondDialogueKey,
            Is.EqualTo("battle.boss.arabella.appear.02"));
        Assert.That(ArabellaSpawnPresentation.FirstDialogueFallback,
            Is.EqualTo("들리시나요? 이토록 아름다운 기도가..."));
        Assert.That(ArabellaSpawnPresentation.SecondDialogueFallback,
            Is.EqualTo("고통은 잠시뿐입니다. 구원은 영원하니까요"));
    }

    [Test]
    public void Presenter_HasExplicitAuthoredReferences()
    {
        System.Type type = typeof(ArabellaSpawnPresentation);

        AssertField(type, "animator", typeof(Animator));
        AssertField(type, "unitAnimator", typeof(BattleUnitAnimator));
        AssertField(type, "appear01Clip", typeof(AnimationClip));
        AssertField(type, "appear02Clip", typeof(AnimationClip));
        AssertField(type, "dialogueRoot", typeof(GameObject));
        AssertField(type, "dialogueText", typeof(TMP_Text));
        AssertField(type, "dialogueRect", typeof(RectTransform));
        AssertField(type, "dialogueLocalPosition", typeof(Vector3));
        AssertField(type, "dialogueSize", typeof(Vector2));
        AssertField(type, "firstDialogueDuration", typeof(float));
        AssertField(type, "secondDialogueDuration", typeof(float));
        AssertField(type, "animationSpeed", typeof(float));
        AssertField(type, "useIntroCamera", typeof(bool));
        AssertField(type, "cameraTargetOffset", typeof(Vector2));
        AssertField(type, "cameraOrthographicSize", typeof(float));
        AssertField(type, "cameraBlendInDuration", typeof(float));
        AssertField(type, "cameraBlendOutDuration", typeof(float));
    }

    [Test]
    public void ArabellaPrefab_WiresAppearPresentationAndDialoguePanel()
    {
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.That(prefab, Is.Not.Null);

        ArabellaSpawnPresentation presenter = prefab.GetComponent<ArabellaSpawnPresentation>();
        Assert.That(presenter, Is.Not.Null);

        var serialized = new UnityEditor.SerializedObject(presenter);
        Assert.That(serialized.FindProperty("animator").objectReferenceValue, Is.Not.Null);
        Assert.That(serialized.FindProperty("unitAnimator").objectReferenceValue, Is.Not.Null);
        Assert.That(serialized.FindProperty("appear01Clip").objectReferenceValue, Is.Not.Null);
        Assert.That(serialized.FindProperty("appear02Clip").objectReferenceValue, Is.Not.Null);
        Assert.That(serialized.FindProperty("dialogueRoot").objectReferenceValue, Is.Not.Null);
        Assert.That(serialized.FindProperty("dialogueText").objectReferenceValue, Is.Not.Null);
        Assert.That(serialized.FindProperty("dialogueRect").objectReferenceValue, Is.Not.Null);
        Assert.That(serialized.FindProperty("dialogueRoot").objectReferenceValue.name,
            Is.EqualTo("AppearDialogue"));
    }

    [Test]
    public void ArabellaController_ContainsAppearStatesAndKeepsIdleDefault()
    {
        string controller = File.ReadAllText(ControllerPath);
        string appear01Guid = ReadGuid(
            "Assets/Project/PrefabsR/Monster/Boss/01.Arabella/Clip/appear01.anim.meta");
        string appear02Guid = ReadGuid(
            "Assets/Project/PrefabsR/Monster/Boss/01.Arabella/Clip/appear02.anim.meta");

        StringAssert.Contains("  m_Name: Appear01\n", Normalize(controller));
        StringAssert.Contains("  m_Name: Appear02\n", Normalize(controller));
        StringAssert.Contains($"guid: {appear01Guid}", controller);
        StringAssert.Contains($"guid: {appear02Guid}", controller);
        StringAssert.Contains("  m_DefaultState: {fileID: -3987589496916183805}\n", Normalize(controller));
    }

    [Test]
    public void BossIntro_SpawnsThenPlaysPresenterThenBossNameBeforeReleasingGate()
    {
        string sceneController = Normalize(File.ReadAllText(SceneControllerPath));
        string loader = Normalize(File.ReadAllText(LoaderPath));

        int spawn = sceneController.IndexOf("loader?.CompleteDeferredMonsterSpawn();",
            System.StringComparison.Ordinal);
        int play = sceneController.IndexOf("yield return presenter.Play();",
            spawn,
            System.StringComparison.Ordinal);
        int bossReveal = sceneController.IndexOf("sharedBackgroundController.PlayCurrentBossReveal()",
            play,
            System.StringComparison.Ordinal);
        int gateRequest = sceneController.IndexOf("battleRoomIntroLoadGate.Request(",
            bossReveal,
            System.StringComparison.Ordinal);
        int release = sceneController.IndexOf("pendingBattleRoomUsesBossIntro = false;",
            gateRequest,
            System.StringComparison.Ordinal);

        Assert.That(spawn, Is.GreaterThanOrEqualTo(0));
        Assert.That(play, Is.GreaterThan(spawn));
        Assert.That(bossReveal, Is.GreaterThan(play));
        Assert.That(gateRequest, Is.GreaterThan(bossReveal));
        Assert.That(release, Is.GreaterThan(gateRequest));
        StringAssert.Contains("FindDeferredBossSpawnPresentation()", loader);
        StringAssert.Contains("ArabellaPhaseTransition.IsArabellaMonsterId", loader);
    }

    [Test]
    public void ResumedBossIntro_FinalizesWithoutPresenterOrBossNameReveal()
    {
        string sceneController = Normalize(File.ReadAllText(SceneControllerPath));
        int complete = sceneController.IndexOf("private void CompleteDeferredBossIntro()",
            System.StringComparison.Ordinal);
        int nameRevealMethod = sceneController.IndexOf(
            "private void PlayDeferredBossNameRevealOrFinalize()",
            complete,
            System.StringComparison.Ordinal);
        string block = sceneController.Substring(complete, nameRevealMethod - complete);

        StringAssert.Contains("if (!pendingBattleRoomUsesBossIntro)", block);
        StringAssert.Contains("FinalizeDeferredBossIntro();", block);
    }

    [Test]
    public void BossRoom_SpawnsBossInSameLoadPassAsParty()
    {
        string sceneController = Normalize(File.ReadAllText(SceneControllerPath));
        int load = sceneController.IndexOf("private void LoadBattleRoomNow()",
            System.StringComparison.Ordinal);
        int nextMethod = sceneController.IndexOf("private static bool HasMappedId",
            load,
            System.StringComparison.Ordinal);
        string block = sceneController.Substring(load, nextMethod - load);

        StringAssert.DoesNotContain("pendingBattleRoomUsesBossIntro ||", block);
        StringAssert.Contains("DeferMonsterSpawn != 0", block);
    }

    [TestCase(BattleScenePath)]
    [TestCase(DebugBattleScenePath)]
    public void BossNameReveal_StartsImmediatelyWithoutBlankLeadIn(string scenePath)
    {
        string scene = Normalize(File.ReadAllText(scenePath));
        int sequence = scene.IndexOf("--- !u!114 &1761047532", System.StringComparison.Ordinal);
        Assert.That(sequence, Is.GreaterThanOrEqualTo(0));

        string block = scene.Substring(sequence, System.Math.Min(2200, scene.Length - sequence));
        StringAssert.Contains("revealTime: 0\n", block);
        StringAssert.Contains("revealTime: 1\n", block);
        StringAssert.DoesNotContain("revealTime: 4\n", block);
        StringAssert.DoesNotContain("revealTime: 5\n", block);
    }

    [Test]
    public void ArabellaDialogue_IsAboveHeadAndRendersAboveUnit()
    {
        string prefab = Normalize(File.ReadAllText(PrefabPath));
        int dialogue = prefab.IndexOf("m_Name: AppearDialogue\n", System.StringComparison.Ordinal);
        Assert.That(dialogue, Is.GreaterThanOrEqualTo(0));

        string block = prefab.Substring(dialogue, System.Math.Min(2600, prefab.Length - dialogue));
        StringAssert.Contains("m_LocalPosition: {x: 0, y: 16, z: 0}\n", block);
        StringAssert.Contains("m_SortingLayerID: 1597553239\n", block);
        StringAssert.Contains("m_SortingOrder: 300\n", block);
    }

    [Test]
    public void Presenter_FreezesFirstAppearFrameUntilPartyEntryFinishes()
    {
        string presenter = Normalize(File.ReadAllText(PresenterPath));
        int prepare = presenter.IndexOf("private void PrepareFirstFrameIfIntroPlaying()",
            System.StringComparison.Ordinal);
        int play = presenter.IndexOf("public IEnumerator Play()",
            System.StringComparison.Ordinal);

        Assert.That(prepare, Is.GreaterThanOrEqualTo(0));
        Assert.That(play, Is.GreaterThan(prepare));
        StringAssert.Contains("BattleSceneController.IsBattleRoomIntroPlaying", presenter);
        StringAssert.Contains("animator.speed = 0f;", presenter.Substring(prepare, play - prepare));

        string playBlock = presenter.Substring(play);
        int resumeAnimator = playBlock.IndexOf("animator.speed = Mathf.Max(0.01f, animationSpeed);",
            System.StringComparison.Ordinal);
        int appear = playBlock.IndexOf("PlayAnimatorState(Appear01StateName);",
            System.StringComparison.Ordinal);
        Assert.That(resumeAnimator, Is.GreaterThanOrEqualTo(0));
        Assert.That(appear, Is.GreaterThan(resumeAnimator));
    }

    [Test]
    public void Presenter_ExposesInspectorDrivenCameraAndDialogueTiming()
    {
        string presenter = Normalize(File.ReadAllText(PresenterPath));

        StringAssert.Contains("BattleCameraController.Instance.TryBeginCinematicCamera()", presenter);
        StringAssert.Contains("ApplyCinematicCameraPose", presenter);
        StringAssert.Contains("cameraBlendInDuration", presenter);
        StringAssert.Contains("cameraBlendOutDuration", presenter);
        StringAssert.Contains("ResolvePresentationDuration", presenter);
        StringAssert.Contains("ApplyDialogueLayout", presenter);
    }

    [Test]
    public void BossIntroCancellation_CleansPresenterBeforeStoppingControllerRoutine()
    {
        System.Type type = typeof(ArabellaSpawnPresentation);
        MethodInfo cancel = type.GetMethod("CancelPresentation",
            BindingFlags.Instance | BindingFlags.Public);
        Assert.That(cancel, Is.Not.Null);
        Assert.That(cancel.ReturnType, Is.EqualTo(typeof(void)));

        string sceneController = Normalize(File.ReadAllText(SceneControllerPath));
        int method = sceneController.IndexOf("private void CancelPendingBattleRoomIntro()",
            System.StringComparison.Ordinal);
        int next = sceneController.IndexOf("private IEnumerator PlayDeferredBossIntroRoutine()",
            method,
            System.StringComparison.Ordinal);
        string block = sceneController.Substring(method, next - method);
        int cleanup = block.IndexOf("CancelPresentation();", System.StringComparison.Ordinal);
        int stop = block.IndexOf("StopCoroutine(deferredBossIntroRoutine);",
            System.StringComparison.Ordinal);

        Assert.That(cleanup, Is.GreaterThanOrEqualTo(0));
        Assert.That(stop, Is.GreaterThan(cleanup));
    }

    [Test]
    public void IntroCamera_AcquiresCinematicOwnershipExclusively()
    {
        string presenter = Normalize(File.ReadAllText(PresenterPath));
        string cameraController = Normalize(File.ReadAllText(CameraControllerPath));

        StringAssert.Contains("TryBeginCinematicCamera()", presenter);
        int method = cameraController.IndexOf("public bool TryBeginCinematicCamera()",
            System.StringComparison.Ordinal);
        Assert.That(method, Is.GreaterThanOrEqualTo(0));
        string block = cameraController.Substring(method,
            System.Math.Min(500, cameraController.Length - method));
        StringAssert.Contains("if (hasCinematicCameraState)", block);
        StringAssert.Contains("return false;", block);
        StringAssert.Contains("return BeginCinematicCamera();", block);
    }

    private static void AssertField(System.Type owner, string name, System.Type fieldType)
    {
        FieldInfo field = owner.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, name);
        Assert.That(field.FieldType, Is.EqualTo(fieldType), name);
    }

    private static string ReadGuid(string metaPath)
    {
        foreach (string line in File.ReadAllLines(metaPath))
        {
            if (line.StartsWith("guid: "))
                return line.Substring("guid: ".Length).Trim();
        }

        Assert.Fail($"GUID not found: {metaPath}");
        return string.Empty;
    }

    private static string Normalize(string value) => value.Replace("\r\n", "\n");
}
