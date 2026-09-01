using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class EventDiceRollPresenter : MonoBehaviour
{
    private enum InteractiveState
    {
        Hidden,
        ReadyToRoll,
        Rolling,
        ReadyToConfirm,
        Closing
    }

    [Header("Dice")]
    [SerializeField] private Image[] diceImages = new Image[3];
    [SerializeField] private Sprite[] faceSprites = new Sprite[6];

    [Header("Result UI")]
    [SerializeField] private Button rollButton;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private TMP_Text rollButtonText;
    [SerializeField, Min(0.01f)] private float buttonFadeDuration = 0.2f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string rollTriggerName = "Roll";
    [SerializeField] private string rollStateName = "Roll";
    [SerializeField, Min(0f)] private float rollDuration = 0.8f;
    [SerializeField] private bool hideOnAwake = true;

    [Header("Sound")]
    [SerializeField, SoundId(SoundCategory.Sfx)] private string rollSfxId = AudioIds.Sfx.BattleEventDiceRoll;
    [SerializeField, Min(0f)] private float rollSfxVolumeMultiplier = 1f;

    private Coroutine rollRoutine;
    private CanvasGroup rollButtonCanvasGroup;
    private InteractiveState interactiveState = InteractiveState.Hidden;
    private int[] pendingDiceFaces = Array.Empty<int>();
    private string pendingDetailText = string.Empty;
    private Action pendingConfirmAction;
    private bool buttonListenerBound;

    private void Awake()
    {
        EnsureReferences();
        BindRollButton();

        if (hideOnAwake)
            gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        UnbindRollButton();
    }

    public bool IsReady =>
        diceImages != null &&
        diceImages.Length > 0 &&
        rollButton != null;

    public void PrepareForInteractiveUse()
    {
        PrepareForPlay();
        BindRollButton();
        EnsureRollButtonCanvasGroup();
    }

    public void ShowInteractive(
        IReadOnlyList<int> diceFaces,
        string resultDetailText,
        Action confirmed)
    {
        PrepareForPlay();
        BindRollButton();

        if (rollRoutine != null)
        {
            StopCoroutine(rollRoutine);
            rollRoutine = null;
        }

        pendingDiceFaces = CopyDiceFaces(diceFaces);
        pendingDetailText = resultDetailText ?? string.Empty;
        pendingConfirmAction = confirmed;
        interactiveState = InteractiveState.ReadyToRoll;

        StopRollAnimation();
        ClearResultTexts();
        SetRollButtonLabel("굴리기");
        SetRollButtonVisibleImmediate(true);
        SetRollButtonInteractable(true);
    }

    public void Play(IReadOnlyList<int> diceFaces, Action completed)
    {
        PrepareForPlay();

        if (rollRoutine != null)
            StopCoroutine(rollRoutine);

        rollRoutine = StartCoroutine(PlayRoutine(diceFaces, completed));
    }

    public IEnumerator PlayFromHost(IReadOnlyList<int> diceFaces, Action completed)
    {
        PrepareForPlay();

        if (rollRoutine != null)
        {
            StopCoroutine(rollRoutine);
            rollRoutine = null;
        }

        yield return PlayRoutine(diceFaces, completed);
    }

    public void HideImmediate()
    {
        if (rollRoutine != null)
        {
            StopCoroutine(rollRoutine);
            rollRoutine = null;
        }

        StopRollAnimation();
        pendingDiceFaces = Array.Empty<int>();
        pendingDetailText = string.Empty;
        pendingConfirmAction = null;
        interactiveState = InteractiveState.Hidden;
        gameObject.SetActive(false);
    }

    public void ConfigureForTest(Image[] images, Sprite[] sprites, float duration)
    {
        diceImages = images;
        faceSprites = sprites;
        rollDuration = Mathf.Max(0f, duration);
        NormalizeDiceImageLayout();
    }

    private void OnRollButtonClicked()
    {
        if (rollRoutine != null)
            return;

        if (interactiveState == InteractiveState.ReadyToRoll)
        {
            rollRoutine = StartCoroutine(RollInteractiveRoutine());
            return;
        }

        if (interactiveState == InteractiveState.ReadyToConfirm)
            rollRoutine = StartCoroutine(ConfirmInteractiveRoutine());
    }

    private IEnumerator RollInteractiveRoutine()
    {
        interactiveState = InteractiveState.Rolling;
        SetRollButtonInteractable(false);
        yield return FadeRollButton(1f, 0f);
        rollButton.gameObject.SetActive(false);

        float duration = Mathf.Max(0f, rollDuration);
        PlayRollAnimation(duration);
        if (duration > 0f)
            yield return new WaitForSecondsRealtime(duration);
        else
            yield return null;

        // Roll 애니메이션이 끝나면 0번 자세로 즉시 복귀한 뒤 Animator를 멈춥니다.
        // 그 직후에만 최종 Face Sprite를 넣어서 회전 프레임과 결과 눈이 섞이지 않게 합니다.
        ResetRollAnimationToStartPose();
        ApplyDiceFaces(pendingDiceFaces);

        int total = SumDiceFaces(pendingDiceFaces);
        if (valueText != null)
            valueText.text = total.ToString();
        if (detailText != null)
            detailText.text = pendingDetailText;

        SetRollButtonLabel("확인");
        SetRollButtonVisibleImmediate(false);
        rollButton.gameObject.SetActive(true);
        yield return FadeRollButton(0f, 1f);

        interactiveState = InteractiveState.ReadyToConfirm;
        SetRollButtonInteractable(true);
        rollRoutine = null;
    }

    private IEnumerator ConfirmInteractiveRoutine()
    {
        interactiveState = InteractiveState.Closing;
        SetRollButtonInteractable(false);
        yield return FadeRollButton(1f, 0f);

        Action confirmed = pendingConfirmAction;
        pendingConfirmAction = null;
        pendingDiceFaces = Array.Empty<int>();
        pendingDetailText = string.Empty;
        interactiveState = InteractiveState.Hidden;
        rollRoutine = null;

        gameObject.SetActive(false);
        confirmed?.Invoke();
    }

    private IEnumerator FadeRollButton(float from, float to)
    {
        EnsureRollButtonCanvasGroup();
        if (rollButtonCanvasGroup == null)
            yield break;

        float duration = Mathf.Max(0.01f, buttonFadeDuration);
        float elapsed = 0f;
        rollButtonCanvasGroup.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rollButtonCanvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        rollButtonCanvasGroup.alpha = to;
    }

    private IEnumerator PlayRoutine(IReadOnlyList<int> diceFaces, Action completed)
    {
        ApplyDiceFaces(diceFaces);
        yield return null;

        float safeDuration = Mathf.Max(0f, rollDuration);
        PlayRollAnimation(safeDuration);
        if (safeDuration > 0f)
            yield return new WaitForSecondsRealtime(safeDuration);
        else
            yield return null;

        ResetRollAnimationToStartPose();
        ApplyDiceFaces(diceFaces);
        completed?.Invoke();
        rollRoutine = null;
    }


    private void SetDieFace(int dieIndex, int face)
    {
        if (diceImages == null || dieIndex < 0 || dieIndex >= diceImages.Length)
            return;

        Image image = diceImages[dieIndex];
        if (image == null)
            return;

        Sprite sprite = GetFaceSprite(face);
        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    private void ApplyDiceFaces(IReadOnlyList<int> diceFaces)
    {
        if (diceImages == null || diceFaces == null)
            return;

        int count = Mathf.Min(diceImages.Length, diceFaces.Count);
        for (int i = 0; i < count; i++)
        {
            Image image = diceImages[i];
            if (image == null)
                continue;

            Sprite sprite = GetFaceSprite(diceFaces[i]);
            image.sprite = sprite;
            image.enabled = sprite != null;
        }
    }

    private void ApplyRandomDiceFaces()
    {
        if (diceImages == null)
            return;

        for (int i = 0; i < diceImages.Length; i++)
        {
            Image image = diceImages[i];
            if (image == null)
                continue;

            Sprite sprite = GetFaceSprite(UnityEngine.Random.Range(1, 7));
            image.sprite = sprite;
            image.enabled = sprite != null;
        }
    }

    private Sprite GetFaceSprite(int face)
    {
        int index = Mathf.Clamp(face, 1, 6) - 1;
        if (faceSprites == null || index < 0 || index >= faceSprites.Length)
            return null;

        return faceSprites[index];
    }

    private void PlayRollAnimation(float targetDuration)
    {
        if (animator == null)
        {
            PlayRollSound();
            return;
        }

        EnsureAnimatorHierarchyActive();
        animator.enabled = true;
        animator.speed = 1f;

        if (!animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
        {
            PlayRollSound();
            return;
        }

        float clipLength = GetRollClipLength();
        if (clipLength > 0f && targetDuration > 0f)
        {
            animator.speed = clipLength / targetDuration;
        }

        if (!string.IsNullOrWhiteSpace(rollTriggerName) &&
            HasAnimatorParameter(animator, rollTriggerName, AnimatorControllerParameterType.Trigger))
        {
            animator.ResetTrigger(rollTriggerName);
            animator.SetTrigger(rollTriggerName);
            PlayRollSound();
            return;
        }

        if (!string.IsNullOrWhiteSpace(rollStateName))
        {
            if (!HasAnimatorState(animator, rollStateName))
            {
                PlayRollSound();
                return;
            }

            animator.Play(rollStateName, 0, 0f);
            animator.Update(0f);
            PlayRollSound();
        }
    }


    private float GetRollClipLength()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return 0f;

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        if (clips == null || clips.Length == 0)
            return 0f;

        if (!string.IsNullOrWhiteSpace(rollStateName))
        {
            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip != null &&
                    string.Equals(clip.name, rollStateName, StringComparison.OrdinalIgnoreCase))
                {
                    return clip.length;
                }
            }
        }

        // Controller에 단일 Roll 클립만 있는 경우에는 해당 길이를 사용합니다.
        if (clips.Length == 1 && clips[0] != null)
            return clips[0].length;

        return 0f;
    }

    private void PlayRollSound()
    {
        if (AudioManager.Instance == null || string.IsNullOrWhiteSpace(rollSfxId))
            return;

        AudioManager.Instance.PlaySfx(rollSfxId, rollSfxVolumeMultiplier);
    }

    private void ResetRollAnimationToStartPose()
    {
        if (animator == null)
            return;

        animator.speed = 1f;
        animator.enabled = true;

        if (animator.runtimeAnimatorController != null &&
            !string.IsNullOrWhiteSpace(rollStateName) &&
            HasAnimatorState(animator, rollStateName))
        {
            animator.Play(rollStateName, 0, 0f);
            animator.Update(0f);
        }

        animator.enabled = false;
    }

    private void StopRollAnimation()
    {
        if (animator == null)
            return;

        animator.speed = 1f;
        animator.enabled = false;
    }

    private void EnsureAnimatorHierarchyActive()
    {
        if (animator == null)
            return;

        Transform current = animator.transform;
        while (current != null && current != transform.parent)
        {
            if (!current.gameObject.activeSelf)
                current.gameObject.SetActive(true);

            current = current.parent;
        }
    }

    private static bool HasAnimatorParameter(
        Animator targetAnimator,
        string parameterName,
        AnimatorControllerParameterType parameterType)
    {
        if (targetAnimator == null || string.IsNullOrWhiteSpace(parameterName))
            return false;

        AnimatorControllerParameter[] parameters = targetAnimator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];
            if (parameter != null &&
                parameter.type == parameterType &&
                string.Equals(parameter.name, parameterName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAnimatorState(Animator targetAnimator, string stateName)
    {
        if (targetAnimator == null || string.IsNullOrWhiteSpace(stateName))
            return false;

        int shortStateHash = Animator.StringToHash(stateName);
        if (targetAnimator.HasState(0, shortStateHash))
            return true;

        int fullStateHash = Animator.StringToHash("Base Layer." + stateName);
        return targetAnimator.HasState(0, fullStateHash);
    }

    private void EnsureReferences()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (diceImages == null || diceImages.Length == 0)
            diceImages = GetComponentsInChildren<Image>(true);

        if (rollButton == null)
        {
            Transform buttonTransform = FindChildRecursive(transform, "RollButton");
            if (buttonTransform != null)
                rollButton = buttonTransform.GetComponent<Button>();
        }

        if (valueText == null)
        {
            Transform valueTransform = FindChildRecursive(transform, "Valuetext");
            if (valueTransform == null)
                valueTransform = FindChildRecursive(transform, "ValueText");
            if (valueTransform != null)
                valueText = valueTransform.GetComponent<TMP_Text>();
        }

        if (detailText == null)
        {
            Transform detailTransform = FindChildRecursive(transform, "Detailtext");
            if (detailTransform == null)
                detailTransform = FindChildRecursive(transform, "DetailText");
            if (detailTransform != null)
                detailText = detailTransform.GetComponent<TMP_Text>();
        }

        if (rollButtonText == null && rollButton != null)
            rollButtonText = rollButton.GetComponentInChildren<TMP_Text>(true);

        EnsureRollButtonCanvasGroup();
    }

    private void BindRollButton()
    {
        EnsureReferences();
        if (rollButton == null || buttonListenerBound)
            return;

        rollButton.onClick.AddListener(OnRollButtonClicked);
        buttonListenerBound = true;
    }

    private void UnbindRollButton()
    {
        if (rollButton == null || !buttonListenerBound)
            return;

        rollButton.onClick.RemoveListener(OnRollButtonClicked);
        buttonListenerBound = false;
    }

    private void EnsureRollButtonCanvasGroup()
    {
        if (rollButton == null)
            return;

        if (rollButtonCanvasGroup == null)
            rollButtonCanvasGroup = rollButton.GetComponent<CanvasGroup>();

        if (rollButtonCanvasGroup == null)
            rollButtonCanvasGroup = rollButton.gameObject.AddComponent<CanvasGroup>();
    }

    private void PrepareForPlay()
    {
        EnsureParentHierarchyActive();
        gameObject.SetActive(true);
        EnsureReferences();
        NormalizeDiceImageLayout();
        ForceLayoutRefresh();
    }

    private void EnsureParentHierarchyActive()
    {
        Transform current = transform.parent;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
                current.gameObject.SetActive(true);

            current = current.parent;
        }
    }

    private void NormalizeDiceImageLayout()
    {
        if (diceImages == null)
            return;

        for (int i = 0; i < diceImages.Length; i++)
        {
            Image image = diceImages[i];
            if (image == null)
                continue;

            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.fillCenter = true;
        }
    }

    private void ForceLayoutRefresh()
    {
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    private void ClearResultTexts()
    {
        if (valueText != null)
            valueText.text = string.Empty;
        if (detailText != null)
            detailText.text = string.Empty;
    }

    private void SetRollButtonLabel(string label)
    {
        if (rollButtonText != null)
            rollButtonText.text = label ?? string.Empty;
    }

    private void SetRollButtonInteractable(bool interactable)
    {
        if (rollButton != null)
            rollButton.interactable = interactable;

        if (rollButtonCanvasGroup != null)
        {
            rollButtonCanvasGroup.interactable = interactable;
            rollButtonCanvasGroup.blocksRaycasts = interactable;
        }
    }

    private void SetRollButtonVisibleImmediate(bool visible)
    {
        if (rollButton == null)
            return;

        rollButton.gameObject.SetActive(true);
        EnsureRollButtonCanvasGroup();
        if (rollButtonCanvasGroup != null)
            rollButtonCanvasGroup.alpha = visible ? 1f : 0f;
    }

    private static int[] CopyDiceFaces(IReadOnlyList<int> diceFaces)
    {
        if (diceFaces == null || diceFaces.Count == 0)
            return Array.Empty<int>();

        int[] copy = new int[diceFaces.Count];
        for (int i = 0; i < diceFaces.Count; i++)
            copy[i] = Mathf.Clamp(diceFaces[i], 1, 6);
        return copy;
    }

    private static int SumDiceFaces(IReadOnlyList<int> diceFaces)
    {
        if (diceFaces == null)
            return 0;

        int total = 0;
        for (int i = 0; i < diceFaces.Count; i++)
            total += Mathf.Clamp(diceFaces[i], 1, 6);
        return total;
    }

    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == targetName)
                return child;

            Transform nested = FindChildRecursive(child, targetName);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
