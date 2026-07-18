using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCharacterUiIdleAssetTests
{
    private static readonly (string sourcePath, string uiPath)[] ClipPairs =
    {
        (
            "Assets/Project/PrefabsR/Character/A/Clip/hilt_select_idle.anim",
            "Assets/Project/PrefabsR/Character/A/Clip/A_Robby_UI_Idle.anim"
        ),
        (
            "Assets/Project/PrefabsR/Character/B/Clip/kaya_select_idle.anim",
            "Assets/Project/PrefabsR/Character/B/Clip/B_Robby_UI_Idle.anim"
        ),
        (
            "Assets/Project/PrefabsR/Character/C/Clip/haze_select_idle.anim",
            "Assets/Project/PrefabsR/Character/C/Clip/C_Robby_UI_Idle.anim"
        )
    };

    private static readonly (string controllerPath, string uiPath)[] ControllerPairs =
    {
        (
            "Assets/Project/PrefabsR/Character/A/Controller/A_Robby_UI_idle.controller",
            "Assets/Project/PrefabsR/Character/A/Clip/A_Robby_UI_Idle.anim"
        ),
        (
            "Assets/Project/PrefabsR/Character/B/Controller/B_Robby_UI_idle.controller",
            "Assets/Project/PrefabsR/Character/B/Clip/B_Robby_UI_Idle.anim"
        ),
        (
            "Assets/Project/PrefabsR/Character/C/Controller/C_Robby_UI_idle.controller",
            "Assets/Project/PrefabsR/Character/C/Clip/C_Robby_UI_Idle.anim"
        )
    };

    private static readonly (string worldControllerPath, string uiControllerPath, string uiPrefabPath)[] FullAnimatorPairs =
    {
        (
            "Assets/Project/PrefabsR/Character/A/Controller/A_Robby_idle.controller",
            "Assets/Project/PrefabsR/Character/A/Controller/A_Robby_UI_idle.controller",
            "Assets/Project/PrefabsR/Character/A/A_UI_idle.prefab"
        ),
        (
            "Assets/Project/PrefabsR/Character/B/Controller/B_Robby_idle.controller",
            "Assets/Project/PrefabsR/Character/B/Controller/B_Robby_UI_idle.controller",
            "Assets/Project/PrefabsR/Character/B/B_UI_idle.prefab"
        ),
        (
            "Assets/Project/PrefabsR/Character/C/Controller/C_Robby_idle.controller",
            "Assets/Project/PrefabsR/Character/C/Controller/C_Robby_UI_idle.controller",
            "Assets/Project/PrefabsR/Character/C/C_UI_idle.prefab"
        )
    };

    private static readonly (string uiPrefabPath, Vector2 expectedSize)[] PreviewSizeCases =
    {
        (
            "Assets/Project/PrefabsR/Character/A/A_UI_idle.prefab",
            new Vector2(2275f, 1280f)
        ),
        (
            "Assets/Project/PrefabsR/Character/B/B_UI_idle.prefab",
            new Vector2(2275f, 1280f)
        ),
        (
            "Assets/Project/PrefabsR/Character/C/C_UI_idle.prefab",
            new Vector2(2502f, 1408f)
        )
    };

    [TestCaseSource(nameof(ClipPairs))]
    public void UiIdleClip_UsesSameSpriteFramesAndTimingAsRobbyIdle(
        (string sourcePath, string uiPath) paths)
    {
        AnimationClip sourceClip = LoadClip(paths.sourcePath);
        AnimationClip uiClip = LoadClip(paths.uiPath);

        EditorCurveBinding sourceBinding = GetSingleSpriteBinding(sourceClip);
        EditorCurveBinding uiBinding = GetSingleSpriteBinding(uiClip);

        Assert.That(uiBinding.type, Is.EqualTo(typeof(Image)));
        Assert.That(uiBinding.propertyName, Is.EqualTo("m_Sprite"));
        Assert.That(uiBinding.path, Is.Empty);

        ObjectReferenceKeyframe[] sourceFrames =
            AnimationUtility.GetObjectReferenceCurve(sourceClip, sourceBinding);
        ObjectReferenceKeyframe[] uiFrames =
            AnimationUtility.GetObjectReferenceCurve(uiClip, uiBinding);

        Assert.That(uiFrames.Length, Is.EqualTo(sourceFrames.Length));

        for (int frameIndex = 0; frameIndex < sourceFrames.Length; frameIndex++)
        {
            Assert.That(uiFrames[frameIndex].time, Is.EqualTo(sourceFrames[frameIndex].time));
            Assert.That(uiFrames[frameIndex].value, Is.SameAs(sourceFrames[frameIndex].value));
        }

        Assert.That(uiClip.frameRate, Is.EqualTo(sourceClip.frameRate));
        Assert.That(
            AnimationUtility.GetAnimationClipSettings(uiClip).loopTime,
            Is.EqualTo(AnimationUtility.GetAnimationClipSettings(sourceClip).loopTime)
        );
    }

    [TestCaseSource(nameof(ControllerPairs))]
    public void UiIdleController_DefaultStateUsesUiIdleClip(
        (string controllerPath, string uiPath) paths)
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(paths.controllerPath);
        AnimationClip uiClip = LoadClip(paths.uiPath);

        Assert.That(controller, Is.Not.Null);
        Assert.That(controller.layers, Has.Length.GreaterThanOrEqualTo(1));

        AnimatorState defaultState = controller.layers[0].stateMachine.defaultState;
        Assert.That(defaultState, Is.Not.Null);
        Assert.That(defaultState.motion, Is.SameAs(uiClip));
    }

    [TestCaseSource(nameof(FullAnimatorPairs))]
    public void UiController_HasSameStatesAndPlaybackSettingsAsRobbyController(
        (string worldControllerPath, string uiControllerPath, string uiPrefabPath) paths)
    {
        AnimatorController worldController = LoadController(paths.worldControllerPath);
        AnimatorController uiController = LoadController(paths.uiControllerPath);

        Dictionary<string, AnimatorState> worldStates = GetStatesByName(worldController);
        Dictionary<string, AnimatorState> uiStates = GetStatesByName(uiController);

        Assert.That(uiStates.Keys, Is.EquivalentTo(worldStates.Keys));

        foreach (KeyValuePair<string, AnimatorState> pair in worldStates)
        {
            AnimatorState uiState = uiStates[pair.Key];
            Assert.That(uiState.speed, Is.EqualTo(pair.Value.speed), pair.Key);
            Assert.That(uiState.cycleOffset, Is.EqualTo(pair.Value.cycleOffset), pair.Key);
            Assert.That(uiState.motion, Is.Not.Null, pair.Key);
        }
    }

    [TestCaseSource(nameof(FullAnimatorPairs))]
    public void UiPrefab_HasButtonResponsiveAnimatorTargetingOwnImageAndAnimator(
        (string worldControllerPath, string uiControllerPath, string uiPrefabPath) paths)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths.uiPrefabPath);
        Assert.That(prefab, Is.Not.Null);

        ButtonResponsiveSpriteAnimator responsive =
            prefab.GetComponent<ButtonResponsiveSpriteAnimator>();
        Assert.That(responsive, Is.Not.Null);

        SerializedObject serialized = new SerializedObject(responsive);
        Assert.That(
            serialized.FindProperty("targetImage").objectReferenceValue,
            Is.SameAs(prefab.GetComponent<Image>())
        );
        Assert.That(
            serialized.FindProperty("targetAnimator").objectReferenceValue,
            Is.SameAs(prefab.GetComponent<Animator>())
        );
        Assert.That(serialized.FindProperty("targetSpriteRenderer").objectReferenceValue, Is.Null);
        Assert.That(serialized.FindProperty("useAnimatorStates").boolValue, Is.True);
    }

    [TestCaseSource(nameof(PreviewSizeCases))]
    public void UiPrefab_MatchesFormerSpriteRendererProjectedSize(
        (string uiPrefabPath, Vector2 expectedSize) testCase)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(testCase.uiPrefabPath);
        Assert.That(prefab, Is.Not.Null);

        RectTransform rectTransform = prefab.GetComponent<RectTransform>();
        Image image = prefab.GetComponent<Image>();

        Assert.That(rectTransform, Is.Not.Null);
        Assert.That(rectTransform.sizeDelta, Is.EqualTo(testCase.expectedSize));
        Assert.That(image, Is.Not.Null);
        Assert.That(image.preserveAspect, Is.True);
    }

    [Test]
    public void LobbyScene_SpawnsCharacterBetweenBackMainAndEffectLobby()
    {
        const string scenePath = "Assets/Project/Scenes/YDM/Lobby.unity";
        string sceneYaml = File.ReadAllText(scenePath);

        const string expectedChildren =
            "  - {fileID: 2200000012}\n" +
            "  - {fileID: 2200000401}\n" +
            "  - {fileID: 2200000032}\n" +
            "  - {fileID: 2200000022}";

        Assert.That(sceneYaml, Does.Contain("m_Name: CharacterPreviewSpawnRoot"));
        Assert.That(sceneYaml, Does.Contain(expectedChildren));
        Assert.That(sceneYaml, Does.Contain("previewRoot: {fileID: 2200000401}"));
    }

    private static AnimationClip LoadClip(string assetPath)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        Assert.That(clip, Is.Not.Null, $"AnimationClip을 찾을 수 없습니다: {assetPath}");
        return clip;
    }

    private static AnimatorController LoadController(string assetPath)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
        Assert.That(controller, Is.Not.Null, $"AnimatorController를 찾을 수 없습니다: {assetPath}");
        return controller;
    }

    private static Dictionary<string, AnimatorState> GetStatesByName(AnimatorController controller)
    {
        return controller.layers[0].stateMachine.states
            .Select(childState => childState.state)
            .ToDictionary(state => state.name);
    }

    private static EditorCurveBinding GetSingleSpriteBinding(AnimationClip clip)
    {
        EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        Assert.That(bindings, Has.Length.EqualTo(1), $"{clip.name}의 Sprite 바인딩 수가 예상과 다릅니다.");
        return bindings[0];
    }
}
