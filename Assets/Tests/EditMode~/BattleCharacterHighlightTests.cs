using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class BattleCharacterHighlightTests
{
    private GameObject characterObject;
    private readonly List<UnityEngine.Object> createdAssets = new();
    private readonly List<string> createdAssetPaths = new();

    [TearDown]
    public void TearDown()
    {
        if (characterObject != null)
            Object.DestroyImmediate(characterObject);

        for (int i = createdAssets.Count - 1; i >= 0; i--)
        {
            if (createdAssets[i] != null)
                Object.DestroyImmediate(createdAssets[i]);
        }

        for (int i = createdAssetPaths.Count - 1; i >= 0; i--)
        {
            if (!string.IsNullOrEmpty(createdAssetPaths[i]))
                AssetDatabase.DeleteAsset(createdAssetPaths[i]);
        }

        createdAssets.Clear();
        createdAssetPaths.Clear();
    }

    [Test]
    public void TimelineHoverHighlight_HidesWithAlphaWithoutDisablingObject()
    {
        BattleCharacter character = CreateCharacterWithHighlight(
            out GameObject highlightObject,
            out SpriteRenderer highlightRenderer);

        character.SetTimelineHoverHighlight(false);

        Assert.That(highlightObject.activeSelf, Is.True);
        Assert.That(highlightRenderer.color.a, Is.EqualTo(0f).Within(0.001f));

        character.SetTimelineHoverHighlight(true);

        Assert.That(highlightObject.activeSelf, Is.True);
        Assert.That(highlightRenderer.color.a, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void SelectionFeedback_UsesHighlightAlphaWithoutScalingSpriteRoot()
    {
        BattleCharacter character = CreateCharacterWithHighlight(
            out _,
            out SpriteRenderer highlightRenderer);

        GameObject spriteRoot = new("SpriteRoot");
        spriteRoot.transform.SetParent(characterObject.transform);
        spriteRoot.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

        character.SetSelectionScaleFeedback(true);

        Assert.That(spriteRoot.transform.localScale, Is.EqualTo(new Vector3(1.2f, 1.2f, 1f)));
        Assert.That(highlightRenderer.color.a, Is.EqualTo(1f).Within(0.001f));

        character.SetSelectionScaleFeedback(false);

        Assert.That(spriteRoot.transform.localScale, Is.EqualTo(new Vector3(1.2f, 1.2f, 1f)));
        Assert.That(highlightRenderer.color.a, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void TimelineHoverHighlight_DoesNotCopySourceSpriteToHighlightSprites()
    {
        BattleCharacter character = CreateCharacterWithHighlight(
            out _,
            out SpriteRenderer highlightRenderer,
            out SpriteRenderer idleBackRenderer);

        SpriteRenderer sourceRenderer = CreateSourceRenderer();
        Sprite firstSprite = CreateSprite(Color.red);
        Sprite shadowSprite = CreateSprite(Color.black);
        Sprite idleBackSprite = CreateSprite(Color.blue);

        sourceRenderer.sprite = firstSprite;
        highlightRenderer.sprite = shadowSprite;
        idleBackRenderer.sprite = idleBackSprite;

        character.SetTimelineHoverHighlight(true);
        InvokePrivate(character, "LateUpdate");

        Assert.That(highlightRenderer.sprite, Is.EqualTo(shadowSprite));
        Assert.That(idleBackRenderer.sprite, Is.EqualTo(idleBackSprite));
    }

    [Test]
    public void TimelineHoverHighlight_PausesIdleBackAnimatorAndSyncsSourceNormalizedTime()
    {
        BattleCharacter character = CreateCharacterWithHighlight(
            out GameObject highlightObject,
            out _,
            out _);

        Animator sourceAnimator = CreateSourceAnimator();
        Animator idleBackAnimator = highlightObject.transform.Find("Idle_Back").gameObject.AddComponent<Animator>();
        idleBackAnimator.runtimeAnimatorController = CreateAnimatorController("Idle_Back");

        sourceAnimator.Play("Idle", 0, 0.625f);
        sourceAnimator.Update(0f);
        character.SetTimelineHoverHighlight(true);
        InvokePrivate(character, "LateUpdate");

        float idleBackTime = idleBackAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f;
        Assert.That(idleBackAnimator.enabled, Is.True);
        Assert.That(idleBackAnimator.speed, Is.EqualTo(0f));
        Assert.That(idleBackTime, Is.EqualTo(0.625f).Within(0.02f));
    }

    [Test]
    public void TimelineHoverHighlight_SortsHighlightIdleBackAndShadowBehindSource()
    {
        BattleCharacter character = CreateCharacterWithHighlight(
            out _,
            out SpriteRenderer highlightRenderer,
            out SpriteRenderer idleBackRenderer);

        SpriteRenderer sourceRenderer = CreateSourceRenderer();
        SpriteRenderer shadowRenderer = CreateShadowRenderer();
        sourceRenderer.sortingLayerID = SortingLayer.NameToID("Default");
        sourceRenderer.sortingOrder = 7;
        shadowRenderer.sortingLayerID = SortingLayer.NameToID("Unit");
        shadowRenderer.sortingOrder = 99;

        character.SetTimelineHoverHighlight(true);
        InvokePrivate(character, "LateUpdate");

        Assert.That(highlightRenderer.sortingLayerID, Is.EqualTo(sourceRenderer.sortingLayerID));
        Assert.That(highlightRenderer.sortingOrder, Is.EqualTo(sourceRenderer.sortingOrder - 1));
        Assert.That(idleBackRenderer.sortingLayerID, Is.EqualTo(sourceRenderer.sortingLayerID));
        Assert.That(idleBackRenderer.sortingOrder, Is.EqualTo(sourceRenderer.sortingOrder - 1));
        Assert.That(shadowRenderer.sortingLayerID, Is.EqualTo(sourceRenderer.sortingLayerID));
        Assert.That(shadowRenderer.sortingOrder, Is.EqualTo(sourceRenderer.sortingOrder - 1));
    }

    [Test]
    public void TimelineHoverHighlight_StaysOffForDeadCharacter()
    {
        BattleCharacter character = CreateCharacterWithHighlight(
            out GameObject highlightObject,
            out SpriteRenderer highlightRenderer,
            out SpriteRenderer idleBackRenderer);

        character.Initialize(new Relic.Gameplay.Data.CharacterRuntimeData
        {
            CharacterId = "Char_Dead_Highlight",
            MaxHP = 10,
            CurrentHP = 0
        });

        character.SetTimelineHoverHighlight(true);
        InvokePrivate(character, "LateUpdate");

        Assert.That(highlightObject.activeSelf, Is.False);
        Assert.That(highlightRenderer.color.a, Is.EqualTo(0f).Within(0.001f));
        Assert.That(idleBackRenderer.color.a, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void SelectionFeedback_DoesNotPlayIdleForDeadCharacter()
    {
        BattleCharacter character = CreateCharacterWithHighlight(
            out _,
            out _);

        Animator animator = CreateSourceAnimator("Idle", "Dead");
        character.Initialize(new Relic.Gameplay.Data.CharacterRuntimeData
        {
            CharacterId = "Char_Dead_Selection",
            MaxHP = 10,
            CurrentHP = 0
        });

        animator.Play("Dead", 0, 0f);
        animator.Update(0f);

        character.SetSelectionScaleFeedback(false);
        animator.Update(0f);

        Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Dead"), Is.True);
    }

    private BattleCharacter CreateCharacterWithHighlight(
        out GameObject highlightObject,
        out SpriteRenderer highlightRenderer,
        out SpriteRenderer idleBackRenderer)
    {
        characterObject = new GameObject("BattleCharacter_Highlight_Test");
        BattleCharacter character = characterObject.AddComponent<BattleCharacter>();

        highlightObject = new GameObject("HighlightSprite");
        highlightObject.transform.SetParent(characterObject.transform);
        highlightRenderer = highlightObject.AddComponent<SpriteRenderer>();
        highlightRenderer.color = Color.white;

        GameObject idleBackObject = new("Idle_Back");
        idleBackObject.transform.SetParent(highlightObject.transform);
        idleBackRenderer = idleBackObject.AddComponent<SpriteRenderer>();
        idleBackRenderer.color = Color.white;

        SetPrivateField(character, "timelineHoverHighlightObject", highlightObject);

        return character;
    }

    private BattleCharacter CreateCharacterWithHighlight(
        out GameObject highlightObject,
        out SpriteRenderer highlightRenderer)
    {
        return CreateCharacterWithHighlight(
            out highlightObject,
            out highlightRenderer,
            out _);
    }

    private SpriteRenderer CreateSourceRenderer()
    {
        GameObject spriteRoot = new("SpriteRoot");
        spriteRoot.transform.SetParent(characterObject.transform);
        return spriteRoot.AddComponent<SpriteRenderer>();
    }

    private SpriteRenderer CreateShadowRenderer()
    {
        GameObject shadow = new("Shadow");
        shadow.transform.SetParent(characterObject.transform);
        return shadow.AddComponent<SpriteRenderer>();
    }

    private Animator CreateSourceAnimator(params string[] stateNames)
    {
        GameObject spriteRoot = new("SpriteRoot");
        spriteRoot.transform.SetParent(characterObject.transform);

        Animator animator = spriteRoot.AddComponent<Animator>();
        animator.runtimeAnimatorController =
            CreateAnimatorController(stateNames == null || stateNames.Length <= 0
                ? new[] { "Idle" }
                : stateNames);
        return animator;
    }

    private RuntimeAnimatorController CreateAnimatorController(params string[] stateNames)
    {
        if (stateNames == null || stateNames.Length <= 0)
            stateNames = new[] { "Idle" };

        string folderPath = "Assets/TempBattleCharacterHighlightTests";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "TempBattleCharacterHighlightTests");
            createdAssetPaths.Add(folderPath);
        }

        string stateName = stateNames[0];
        string controllerPath = AssetDatabase.GenerateUniqueAssetPath(
            $"{folderPath}/{stateName}_Test.controller");
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        createdAssetPaths.Add(controllerPath);

        for (int i = 0; i < stateNames.Length; i++)
        {
            AnimationClip clip = new() { frameRate = 8f };
            clip.SetCurve(
                "",
                typeof(Transform),
                "m_LocalPosition.x",
                AnimationCurve.Linear(0f, 0f, 1f, 1f));
            AssetDatabase.AddObjectToAsset(clip, controller);

            AnimatorState state = controller.layers[0].stateMachine.AddState(stateNames[i]);
            state.motion = clip;

            if (i == 0)
                controller.layers[0].stateMachine.defaultState = state;
        }

        AssetDatabase.SaveAssets();

        return controller;
    }

    private Sprite CreateSprite(Color color)
    {
        Texture2D texture = new(2, 2);
        texture.SetPixels(new[] { color, color, color, color });
        texture.Apply();
        createdAssets.Add(texture);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 2f, 2f),
            new Vector2(0.5f, 0.5f));
        createdAssets.Add(sprite);
        return sprite;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, $"Missing private field: {fieldName}");
        field.SetValue(target, value);
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"Missing private method: {methodName}");
        method.Invoke(target, null);
    }
}
