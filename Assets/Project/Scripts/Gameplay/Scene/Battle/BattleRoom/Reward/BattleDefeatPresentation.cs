using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class BattleDefeatPresentation : MonoBehaviour
{
    private const int MaxPresentedCharacters = 3;

    [Header("Scene References")]
    [SerializeField] private GameObject presentationRoot;
    [SerializeField] private Transform fogRoot;
    [SerializeField] private Transform fogStartAnchor;
    [SerializeField] private Transform fogEndAnchor;
    [SerializeField] private Transform[] characterAnchors = new Transform[MaxPresentedCharacters];
    [SerializeField] private TMP_Text defeatText;
    [SerializeField] private CanvasGroup defeatTextCanvasGroup;
    [SerializeField] private GameObject[] coveredUiRoots = Array.Empty<GameObject>();

    [Header("Timing")]
    [SerializeField, Min(0f)] private float fogStartDelay;
    [SerializeField, Min(0f)] private float fogDuration = 1.2f;
    [SerializeField, Min(0f)] private float characterMoveDelay = 0.25f;
    [SerializeField, Min(0f)] private float characterMoveDuration = 1f;
    [SerializeField] private AnimationCurve characterMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Min(0f)] private float textDelay = 0.8f;
    [SerializeField, Min(0f)] private float textFadeDuration = 0.35f;
    [SerializeField, Min(0f)] private float resultDelay = 1.5f;

    [Header("Rendering")]
    [SerializeField] private string presentationLayerName = "DefeatPresentation";
    [SerializeField] private string presentationSortingLayerName = "DefeatPresentation";
    [SerializeField] private int fogSortingOrder;
    [SerializeField] private int characterSortingOrder = 22000;

    private Coroutine routine;
    private Action completion;

    public bool IsPlaying => routine != null;

    public bool Play(IReadOnlyList<BattleCharacter> characters, Action onCompleted)
    {
        if (!isActiveAndEnabled || routine != null)
            return false;

        completion = onCompleted;
        routine = StartCoroutine(PlayRoutine(characters));
        return true;
    }

    public static IReadOnlyList<BattleCharacter> SelectCharacters(
        IReadOnlyList<BattleCharacter> characters,
        int maxCount = MaxPresentedCharacters)
    {
        var selected = new List<BattleCharacter>();
        if (characters == null || maxCount <= 0)
            return selected;

        for (int i = 0; i < characters.Count; i++)
        {
            BattleCharacter character = characters[i];
            if (character != null && character.RuntimeData != null)
                selected.Add(character);
        }

        selected.Sort(CompareCharacters);
        if (selected.Count > maxCount)
            selected.RemoveRange(maxCount, selected.Count - maxCount);
        return selected;
    }

    public static int[] ResolveAnchorIndices(int characterCount)
    {
        if (characterCount <= 0)
            return Array.Empty<int>();
        if (characterCount == 1)
            return new[] { 1 };
        if (characterCount == 2)
            return new[] { 0, 2 };
        return new[] { 0, 1, 2 };
    }

    private IEnumerator PlayRoutine(IReadOnlyList<BattleCharacter> sourceCharacters)
    {
        IReadOnlyList<BattleCharacter> characters = SelectCharacters(sourceCharacters);
        if (sourceCharacters != null && sourceCharacters.Count > MaxPresentedCharacters)
            Debug.LogWarning("[BattleDefeatPresentation] More than three characters found; only three are presented.", this);

        PreparePresentation(characters);

        float totalDuration = Mathf.Max(
            fogStartDelay + fogDuration,
            Mathf.Max(characterMoveDelay + characterMoveDuration, textDelay + textFadeDuration));
        float elapsed = 0f;
        Vector3 fogStart = fogStartAnchor != null ? fogStartAnchor.position : fogRoot != null ? fogRoot.position : Vector3.zero;
        Vector3 fogEnd = fogEndAnchor != null ? fogEndAnchor.position : fogStart;
        Vector3[] characterStarts = CaptureCharacterPositions(characters);
        int[] anchorIndices = ResolveAnchorIndices(characters.Count);

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            UpdateFog(elapsed, fogStart, fogEnd);
            UpdateCharacters(elapsed, characters, characterStarts, anchorIndices);
            UpdateText(elapsed);

            yield return null;
        }

        UpdateFog(float.MaxValue, fogStart, fogEnd);
        UpdateCharacters(float.MaxValue, characters, characterStarts, anchorIndices);
        UpdateText(float.MaxValue);
        if (resultDelay > 0f)
            yield return new WaitForSecondsRealtime(resultDelay);

        routine = null;
        Action callback = completion;
        completion = null;
        callback?.Invoke();
    }

    private void PreparePresentation(IReadOnlyList<BattleCharacter> characters)
    {
        int presentationLayer = LayerMask.NameToLayer(presentationLayerName);
        int presentationSortingLayer = SortingLayer.NameToID(presentationSortingLayerName);

        if (presentationRoot != null)
            presentationRoot.SetActive(true);

        if (fogRoot != null && fogStartAnchor != null)
            fogRoot.position = fogStartAnchor.position;

        if (fogRoot != null)
        {
            ApplyLayerRecursively(fogRoot.gameObject, presentationLayer);
            Renderer[] fogRenderers = fogRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < fogRenderers.Length; i++)
            {
                fogRenderers[i].sortingLayerID = presentationSortingLayer;
                fogRenderers[i].sortingOrder = fogSortingOrder;
            }
        }

        if (defeatText != null)
            defeatText.text = GameLocalization.Get("battle.result.exploration_failed", "탐사 실패");
        if (defeatTextCanvasGroup != null)
            defeatTextCanvasGroup.alpha = 0f;

        for (int i = 0; i < coveredUiRoots.Length; i++)
            if (coveredUiRoots[i] != null)
                coveredUiRoots[i].SetActive(false);

        for (int i = 0; i < characters.Count; i++)
        {
            BattleCharacter character = characters[i];
            ApplyLayerRecursively(character.gameObject, presentationLayer);
            character.SetTimelineHoverHighlight(false);
            character.HideHUDFromTimelineIfNotSelected();

            YSortSprite[] sorters = character.GetComponentsInChildren<YSortSprite>(true);
            for (int sorterIndex = 0; sorterIndex < sorters.Length; sorterIndex++)
                sorters[sorterIndex].enabled = false;

            SpriteRenderer[] renderers = character.GetComponentsInChildren<SpriteRenderer>(true);
            int minimumOrder = int.MaxValue;
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                minimumOrder = Mathf.Min(minimumOrder, renderers[rendererIndex].sortingOrder);

            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                int relativeOrder = minimumOrder == int.MaxValue
                    ? rendererIndex
                    : renderers[rendererIndex].sortingOrder - minimumOrder;
                renderers[rendererIndex].sortingOrder = characterSortingOrder + relativeOrder;
                renderers[rendererIndex].sortingLayerID = presentationSortingLayer;
            }

            SortingGroup[] sortingGroups = character.GetComponentsInChildren<SortingGroup>(true);
            for (int groupIndex = 0; groupIndex < sortingGroups.Length; groupIndex++)
            {
                sortingGroups[groupIndex].sortingLayerID = presentationSortingLayer;
                sortingGroups[groupIndex].sortingOrder = characterSortingOrder;
            }
        }
    }

    private void UpdateFog(float elapsed, Vector3 start, Vector3 end)
    {
        if (fogRoot == null || elapsed < fogStartDelay)
            return;

        float progress = fogDuration <= 0f ? 1f : Mathf.Clamp01((elapsed - fogStartDelay) / fogDuration);
        fogRoot.position = Vector3.LerpUnclamped(start, end, progress);
    }

    private void UpdateCharacters(
        float elapsed,
        IReadOnlyList<BattleCharacter> characters,
        IReadOnlyList<Vector3> starts,
        IReadOnlyList<int> anchorIndices)
    {
        if (elapsed < characterMoveDelay)
            return;

        float progress = characterMoveDuration <= 0f
            ? 1f
            : Mathf.Clamp01((elapsed - characterMoveDelay) / characterMoveDuration);
        float curved = characterMoveCurve != null ? characterMoveCurve.Evaluate(progress) : progress;

        for (int i = 0; i < characters.Count && i < anchorIndices.Count; i++)
        {
            int anchorIndex = anchorIndices[i];
            if (characterAnchors == null ||
                anchorIndex < 0 ||
                anchorIndex >= characterAnchors.Length ||
                characterAnchors[anchorIndex] == null)
                continue;

            characters[i].transform.position = Vector3.LerpUnclamped(
                starts[i],
                characterAnchors[anchorIndex].position,
                curved);
        }
    }

    private void UpdateText(float elapsed)
    {
        if (defeatTextCanvasGroup == null || elapsed < textDelay)
            return;

        defeatTextCanvasGroup.alpha = textFadeDuration <= 0f
            ? 1f
            : Mathf.Clamp01((elapsed - textDelay) / textFadeDuration);
    }

    private static void ApplyLayerRecursively(GameObject root, int layer)
    {
        if (root == null || layer < 0)
            return;

        root.layer = layer;
        Transform transform = root.transform;
        for (int i = 0; i < transform.childCount; i++)
            ApplyLayerRecursively(transform.GetChild(i).gameObject, layer);
    }

    private static Vector3[] CaptureCharacterPositions(IReadOnlyList<BattleCharacter> characters)
    {
        var positions = new Vector3[characters.Count];
        for (int i = 0; i < characters.Count; i++)
            positions[i] = characters[i].transform.position;
        return positions;
    }

    private static int CompareCharacters(BattleCharacter left, BattleCharacter right)
    {
        int xComparison = left.transform.position.x.CompareTo(right.transform.position.x);
        if (xComparison != 0)
            return xComparison;

        return string.Compare(left.CharacterId, right.CharacterId, StringComparison.Ordinal);
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        completion = null;
    }
}
