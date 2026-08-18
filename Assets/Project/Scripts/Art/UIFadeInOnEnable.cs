using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIFadeInOnEnable : MonoBehaviour
{
    [Header("페이드 설정")]
    [Tooltip("알파값이 0에서 원래 값까지 올라가는 데 걸리는 시간")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("제외 설정")]
    [Tooltip("페이드 효과에서 제외할 자식 오브젝트입니다. 해당 오브젝트의 모든 자식도 함께 제외됩니다.")]
    [SerializeField] private List<GameObject> excludedObjects = new List<GameObject>();

    private class GraphicFadeData
    {
        public Graphic graphic;
        public float originalAlpha;
    }

    private readonly List<GraphicFadeData> fadeTargets = new List<GraphicFadeData>();

    private Coroutine fadeCoroutine;
    private Action fadeOutCompletedCallback;
    private CanvasGroup interactionCanvasGroup;
    private bool addedInteractionCanvasGroup;
    private bool capturedInteractionCanvasGroupState;
    private bool originalInteractable;
    private bool originalBlocksRaycasts;

    private bool initialized = false;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (!initialized)
        {
            Initialize();
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeOutCompletedCallback = null;
        RestoreInteractionState();
        SetAlphaToZero();

        fadeCoroutine = StartCoroutine(FadeIn());
    }

    private void OnDisable()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        fadeOutCompletedCallback = null;
        RestoreInteractionState();
        RestoreOriginalAlpha();
    }

    public bool TryFadeOutAndDeactivate(Action completedCallback = null)
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            return false;

        if (!initialized)
            Initialize();

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        fadeOutCompletedCallback = completedCallback;
        BlockInteraction();
        fadeCoroutine = StartCoroutine(FadeOutAndDeactivate());
        return true;
    }

    /// <summary>
    /// 현재 오브젝트와 모든 자식의 Graphic을 찾아
    /// 페이드 대상 목록을 생성합니다.
    /// </summary>
    private void Initialize()
    {
        fadeTargets.Clear();

        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in graphics)
        {
            if (graphic == null)
            {
                continue;
            }

            // 제외 대상에 포함되어 있으면 페이드 대상에서 제외
            if (IsExcluded(graphic.transform))
            {
                continue;
            }

            GraphicFadeData data = new GraphicFadeData
            {
                graphic = graphic,
                originalAlpha = graphic.color.a
            };

            fadeTargets.Add(data);
        }

        initialized = true;
    }

    /// <summary>
    /// 해당 Transform이 제외 오브젝트 또는
    /// 제외 오브젝트의 자식인지 확인합니다.
    /// </summary>
    private bool IsExcluded(Transform target)
    {
        if (target != null && target.GetComponentInParent<UIBlurBackground>(true) != null)
            return true;

        foreach (GameObject excludedObject in excludedObjects)
        {
            if (excludedObject == null)
            {
                continue;
            }

            Transform excludedTransform = excludedObject.transform;

            // 제외 오브젝트 자기 자신
            if (target == excludedTransform)
            {
                return true;
            }

            // 제외 오브젝트의 자식
            if (target.IsChildOf(excludedTransform))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 페이드 대상들의 알파값을 0으로 만듭니다.
    /// </summary>
    private void SetAlphaToZero()
    {
        foreach (GraphicFadeData data in fadeTargets)
        {
            if (data.graphic == null)
            {
                continue;
            }

            Color color = data.graphic.color;
            color.a = 0f;
            data.graphic.color = color;
        }
    }

    /// <summary>
    /// 알파값을 원래 값으로 서서히 복원합니다.
    /// </summary>
    private IEnumerator FadeIn()
    {
        if (fadeDuration <= 0f)
        {
            RestoreOriginalAlpha();
            fadeCoroutine = null;
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsedTime / fadeDuration);

            // 시작과 끝이 자연스럽도록 처리
            t = Mathf.SmoothStep(0f, 1f, t);

            foreach (GraphicFadeData data in fadeTargets)
            {
                if (data.graphic == null)
                {
                    continue;
                }

                Color color = data.graphic.color;

                // 원래 가지고 있던 알파값까지만 복원
                color.a = Mathf.Lerp(0f, data.originalAlpha, t);

                data.graphic.color = color;
            }

            yield return null;
        }

        RestoreOriginalAlpha();

        fadeCoroutine = null;
    }

    private IEnumerator FadeOutAndDeactivate()
    {
        List<float> startAlphas = CaptureCurrentAlphas();

        if (fadeDuration <= 0f)
        {
            SetAlphaToZero();
            CompleteFadeOutAndDeactivate();
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsedTime / fadeDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < fadeTargets.Count; i++)
            {
                GraphicFadeData data = fadeTargets[i];
                if (data.graphic == null)
                    continue;

                Color color = data.graphic.color;
                float startAlpha = i < startAlphas.Count ? startAlphas[i] : color.a;
                color.a = Mathf.Lerp(startAlpha, 0f, t);
                data.graphic.color = color;
            }

            yield return null;
        }

        SetAlphaToZero();
        CompleteFadeOutAndDeactivate();
    }

    private List<float> CaptureCurrentAlphas()
    {
        List<float> startAlphas = new List<float>(fadeTargets.Count);
        for (int i = 0; i < fadeTargets.Count; i++)
        {
            Graphic graphic = fadeTargets[i].graphic;
            startAlphas.Add(graphic != null ? graphic.color.a : 0f);
        }

        return startAlphas;
    }

    private void CompleteFadeOutAndDeactivate()
    {
        Action callback = fadeOutCompletedCallback;
        fadeOutCompletedCallback = null;
        fadeCoroutine = null;

        gameObject.SetActive(false);
        callback?.Invoke();
    }

    private void BlockInteraction()
    {
        EnsureInteractionCanvasGroup();

        if (interactionCanvasGroup == null)
            return;

        interactionCanvasGroup.interactable = false;
        interactionCanvasGroup.blocksRaycasts = false;
    }

    private void EnsureInteractionCanvasGroup()
    {
        if (interactionCanvasGroup != null)
            return;

        interactionCanvasGroup = GetComponent<CanvasGroup>();
        addedInteractionCanvasGroup = interactionCanvasGroup == null;

        if (interactionCanvasGroup == null)
            interactionCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (interactionCanvasGroup == null || capturedInteractionCanvasGroupState)
            return;

        originalInteractable = interactionCanvasGroup.interactable;
        originalBlocksRaycasts = interactionCanvasGroup.blocksRaycasts;
        capturedInteractionCanvasGroupState = true;
    }

    private void RestoreInteractionState()
    {
        if (interactionCanvasGroup != null && capturedInteractionCanvasGroupState)
        {
            interactionCanvasGroup.interactable = originalInteractable;
            interactionCanvasGroup.blocksRaycasts = originalBlocksRaycasts;
        }

        if (addedInteractionCanvasGroup && interactionCanvasGroup != null)
        {
            if (Application.isPlaying)
                Destroy(interactionCanvasGroup);
            else
                DestroyImmediate(interactionCanvasGroup);
        }

        interactionCanvasGroup = null;
        addedInteractionCanvasGroup = false;
        capturedInteractionCanvasGroupState = false;
    }

    /// <summary>
    /// 모든 페이드 대상의 원래 알파값을 복원합니다.
    /// </summary>
    private void RestoreOriginalAlpha()
    {
        foreach (GraphicFadeData data in fadeTargets)
        {
            if (data.graphic == null)
            {
                continue;
            }

            Color color = data.graphic.color;
            color.a = data.originalAlpha;
            data.graphic.color = color;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 인스펙터에서 제외 대상을 변경한 경우
        // 플레이 시 다시 목록을 생성하도록 합니다.
        initialized = false;
    }
#endif
}