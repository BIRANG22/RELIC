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
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip rollSound;
    [SerializeField, Range(0f, 1f)] private float rollSoundVolume = 1f;

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
        EnsureReferences();
        NormalizeDiceImageLayout();

        if (rollRoutine != null)
            StopCoroutine(rollRoutine);

        gameObject.SetActive(true);
        rollRoutine = StartCoroutine(PlayRoutine(diceFaces, completed));
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

        animator.enabled = true;

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
            animator.Play(rollStateName, 0, 0f);
            animator.Update(0f);
            PlayRollSound();
        }
    }

    private void PlayRollSound()
    {
        if (audioSource == null || rollSound == null)
            return;

        audioSource.PlayOneShot(rollSound, Mathf.Clamp01(rollSoundVolume));
    }

    private void StopRollAnimation()
    {
        if (animator == null)
            return;

        animator.enabled = false;
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

    private void EnsureReferences()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (diceImages == null || diceImages.Length == 0)
            diceImages = GetComponentsInChildren<Image>(true);
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
}
