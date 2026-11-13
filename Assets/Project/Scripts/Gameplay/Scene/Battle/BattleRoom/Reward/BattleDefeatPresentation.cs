using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public sealed class BattleDefeatPresentation : MonoBehaviour
{
    private const int MaxPresentedCharacters = 3;
    private static readonly int FogProgressPropertyId = Shader.PropertyToID("_progress");

    [Header("Scene References")]
    [SerializeField] private GameObject presentationRoot;
    [SerializeField] private Transform fogRoot;
    [SerializeField] private SpriteRenderer blackFogRenderer;
    [SerializeField] private GameObject presentationVfx;
    [SerializeField] private TMP_Text defeatText;
    [SerializeField] private CanvasGroup defeatTextCanvasGroup;
    [SerializeField] private Button nextButton;
    [SerializeField] private GameObject[] coveredUiRoots = Array.Empty<GameObject>();

    [Header("Timing")]
    [SerializeField, Min(0f)] private float fogStartDelay;
    [SerializeField, Min(0f)] private float fogDuration = 1.2f;

    [Header("Black Fog Shader Progress")]
    [SerializeField] private float fogProgressStartValue = 0f;
    [SerializeField] private float fogProgressEndValue = -2f;

    [Header("Fog SFX (Sound DB)")]
    [SerializeField, SoundId(SoundCategory.Sfx)] private string fogSfxId = "";
    [SerializeField, Range(0f, 1f)] private float fogSfxVolume = 1f;

    [SerializeField, Min(0f)] private float textDelay = 0.8f;
    [SerializeField, Min(0f)] private float textFadeDuration = 0.35f;
    [SerializeField, Min(0f)] private float resultDelay = 1.5f;

    [Header("Rendering")]
    [SerializeField] private string presentationLayerName = "DefeatPresentation";
    [SerializeField] private string presentationSortingLayerName = "DefeatPresentation";
    [SerializeField] private int fogSortingOrder;
    [SerializeField] private int characterSortingOrder = 22000;

    private Material blackFogMaterialInstance;
    private Coroutine routine;
    private Action completion;
    private bool awaitingNext;
    private bool fogSfxPlayed;

    public bool IsPlaying => routine != null || awaitingNext;

    public bool Play(IReadOnlyList<BattleCharacter> characters, Action onCompleted)
    {
        if (!isActiveAndEnabled || IsPlaying)
            return false;

        completion = onCompleted;
        awaitingNext = false;
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

    private IEnumerator PlayRoutine(IReadOnlyList<BattleCharacter> sourceCharacters)
    {
        IReadOnlyList<BattleCharacter> characters = SelectCharacters(sourceCharacters);
        if (sourceCharacters != null && sourceCharacters.Count > MaxPresentedCharacters)
            Debug.LogWarning("[BattleDefeatPresentation] More than three characters found; only three are presented.", this);

        PreparePresentation(characters);

        float totalDuration = Mathf.Max(
            fogStartDelay + fogDuration,
            textDelay + textFadeDuration);
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            UpdateFogProgress(elapsed);
            UpdateText(elapsed);

            yield return null;
        }

        UpdateFogProgress(float.MaxValue);
        UpdateText(float.MaxValue);
        if (resultDelay > 0f)
            yield return new WaitForSecondsRealtime(resultDelay);

        routine = null;
        awaitingNext = true;
        if (nextButton != null)
            nextButton.interactable = true;
        else
            Debug.LogWarning("[BattleDefeatPresentation] NextButton is not assigned. Assign a Button in the Inspector.", this);
    }

    private void HandleNextButtonClicked()
    {
        if (!awaitingNext)
            return;

        awaitingNext = false;
        if (nextButton != null)
            nextButton.interactable = false;

        Action callback = completion;
        completion = null;
        callback?.Invoke();
    }

    private void PreparePresentation(IReadOnlyList<BattleCharacter> characters)
    {
        fogSfxPlayed = false;
        int presentationLayer = LayerMask.NameToLayer(presentationLayerName);
        int presentationSortingLayer = SortingLayer.NameToID(presentationSortingLayerName);

        // Restart the pre-placed VFX each time the defeat presentation begins.
        // Disable before activating its parent so it doesn't play early.
        if (presentationVfx != null)
        {
            if (presentationVfx == presentationRoot)
                Debug.LogWarning("[BattleDefeatPresentation] Presentation VFX must be a child/object, not PresentationRoot itself.", this);
            else
                presentationVfx.SetActive(false);
        }

        if (presentationRoot != null)
            presentationRoot.SetActive(true);

        if (presentationVfx != null && presentationVfx != presentationRoot)
        {
            presentationVfx.SetActive(true);
            ParticleSystem[] particles = presentationVfx.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particle in particles)
            {
                if (!particle.gameObject.activeInHierarchy)
                    continue;
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particle.Play(true);
            }
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(HandleNextButtonClicked);
            nextButton.onClick.AddListener(HandleNextButtonClicked);
            // Keep the button active throughout the presentation so it shares
            // the parent CanvasGroup fade with the defeat text.
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = false;
        }

        // BlackFog SpriteRenderer에 실제로 적용된 머티리얼 인스턴스를 사용합니다.
        // 공유 머티리얼 에셋을 변경하지 않도록 Renderer.material을 통해 가져옵니다.
        if (blackFogRenderer == null && fogRoot != null)
            blackFogRenderer = fogRoot.GetComponent<SpriteRenderer>();

        if (blackFogRenderer != null)
        {
            if (blackFogMaterialInstance == null)
                blackFogMaterialInstance = blackFogRenderer.material;

            if (blackFogMaterialInstance != null && blackFogMaterialInstance.HasProperty(FogProgressPropertyId))
                blackFogMaterialInstance.SetFloat(FogProgressPropertyId, fogProgressStartValue);
            else
                Debug.LogWarning("[BattleDefeatPresentation] BlackFog material does not have _progress.", this);
        }
        else
        {
            Debug.LogWarning("[BattleDefeatPresentation] BlackFog SpriteRenderer is not assigned.", this);
        }

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

    private void UpdateFogProgress(float elapsed)
    {
        if (elapsed < fogStartDelay)
            return;

        // Play exactly once when Fog begins (not on every shader update).
        if (!fogSfxPlayed)
        {
            fogSfxPlayed = true;
            PlayFogSfx();
        }

        if (blackFogMaterialInstance == null || !blackFogMaterialInstance.HasProperty(FogProgressPropertyId))
            return;

        float progress = fogDuration <= 0f ? 1f : Mathf.Clamp01((elapsed - fogStartDelay) / fogDuration);
        blackFogMaterialInstance.SetFloat(FogProgressPropertyId, Mathf.Lerp(fogProgressStartValue, fogProgressEndValue, progress));
    }

    private void PlayFogSfx()
    {
        if (string.IsNullOrWhiteSpace(fogSfxId))
            return;

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager == null)
        {
            Debug.LogWarning("[BattleDefeatPresentation] AudioManager.Instance is missing; Fog SFX cannot play.", this);
            return;
        }

        audioManager.PlaySfx(fogSfxId.Trim(), Mathf.Clamp01(fogSfxVolume));
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

    private static int CompareCharacters(BattleCharacter left, BattleCharacter right)
    {
        int xComparison = left.transform.position.x.CompareTo(right.transform.position.x);
        if (xComparison != 0)
            return xComparison;

        return string.Compare(left.CharacterId, right.CharacterId, StringComparison.Ordinal);
    }

    private void OnDisable()
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(HandleNextButtonClicked);
            nextButton.interactable = false;
        }
        awaitingNext = false;
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        completion = null;
    }
}
