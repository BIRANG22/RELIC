using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class EventDiceRollPresenter : MonoBehaviour
{
    [Header("Dice")]
    [SerializeField] private Image[] diceImages = new Image[3];
    [SerializeField] private Sprite[] faceSprites = new Sprite[6];

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

    private void Awake()
    {
        EnsureReferences();

        if (hideOnAwake)
            gameObject.SetActive(false);
    }

    public bool IsReady => diceImages != null && diceImages.Length > 0;

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
        gameObject.SetActive(false);
    }

    public void ConfigureForTest(Image[] images, Sprite[] sprites, float duration)
    {
        diceImages = images;
        faceSprites = sprites;
        rollDuration = Mathf.Max(0f, duration);
        NormalizeDiceImageLayout();
    }

    private IEnumerator PlayRoutine(IReadOnlyList<int> diceFaces, Action completed)
    {
        ApplyDiceFaces(diceFaces);
        yield return null;
        PlayRollAnimation();

        float safeDuration = Mathf.Max(0f, rollDuration);
        if (safeDuration > 0f)
            yield return new WaitForSecondsRealtime(safeDuration);
        else
            yield return null;

        StopRollAnimation();
        ApplyDiceFaces(diceFaces);
        completed?.Invoke();
        rollRoutine = null;
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

    private Sprite GetFaceSprite(int face)
    {
        int index = Mathf.Clamp(face, 1, 6) - 1;
        if (faceSprites == null || index < 0 || index >= faceSprites.Length)
            return null;

        return faceSprites[index];
    }

    private void PlayRollAnimation()
    {
        if (animator == null)
            return;

        EnsureAnimatorHierarchyActive();
        animator.enabled = true;

        if (!animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
        {
            PlayRollSound();
            return;
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

    private void PlayRollSound()
    {
        if (AudioManager.Instance == null || string.IsNullOrWhiteSpace(rollSfxId))
            return;

        AudioManager.Instance.PlaySfx(rollSfxId, rollSfxVolumeMultiplier);
    }

    private void StopRollAnimation()
    {
        if (animator == null)
            return;

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
}
