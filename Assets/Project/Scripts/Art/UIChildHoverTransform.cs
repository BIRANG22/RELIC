using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class UIChildHoverTransform : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Serializable]
    private sealed class TargetTransformSetting
    {
        [Tooltip("호버 시 위치와 스케일을 변경할 자식 UI 오브젝트입니다.")]
        public RectTransform target;

        [Header("Hover Transform")]
        [Tooltip("호버 시 적용할 Anchored Position입니다.")]
        public Vector2 hoverAnchoredPosition = Vector2.zero;

        [Tooltip("호버 시 적용할 Local Scale입니다.")]
        public Vector3 hoverLocalScale = Vector3.one;

        [HideInInspector] public Vector2 defaultAnchoredPosition;
        [HideInInspector] public Vector3 defaultLocalScale = Vector3.one;
        [HideInInspector] public bool defaultCaptured;
    }

    [Header("Targets")]
    [Tooltip("Size를 늘려 여러 자식 UI 오브젝트를 등록할 수 있습니다. 각 타겟마다 호버 위치와 스케일을 따로 지정합니다.")]
    [SerializeField] private List<TargetTransformSetting> targets = new();

    [Header("Transition")]
    [Tooltip("기본 상태와 호버 상태 사이의 전환 시간입니다.")]
    [SerializeField, Min(0f)] private float transitionDuration = 0.15f;

    [Tooltip("전환에 사용할 보간 곡선입니다.")]
    [SerializeField] private AnimationCurve transitionCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Time.timeScale의 영향을 받지 않고 UI 애니메이션을 재생합니다.")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Default Transform")]
    [Tooltip("체크하면 시작 시 각 타겟의 현재 위치와 스케일을 기본값으로 자동 저장합니다.")]
    [SerializeField] private bool captureDefaultOnAwake = true;

    private Coroutine transitionRoutine;

    private void Awake()
    {
        EnsureTargets();

        if (captureDefaultOnAwake)
            CaptureCurrentAsDefault();
    }

    private void OnDisable()
    {
        StopCurrentTransition();
        ApplyDefaultImmediatelyInternal(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        EnsureTargets();
        EnsureDefaultsCaptured();
        StartTransition(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        EnsureTargets();
        EnsureDefaultsCaptured();
        StartTransition(false);
    }

    [ContextMenu("Capture Current As Default")]
    public void CaptureCurrentAsDefault()
    {
        EnsureTargets();

        for (int i = 0; i < targets.Count; i++)
        {
            TargetTransformSetting setting = targets[i];
            if (setting == null || setting.target == null)
                continue;

            setting.defaultAnchoredPosition = setting.target.anchoredPosition;
            setting.defaultLocalScale = setting.target.localScale;
            setting.defaultCaptured = true;
        }
    }

    [ContextMenu("Apply Default Immediately")]
    public void ApplyDefaultImmediately()
    {
        EnsureTargets();
        EnsureDefaultsCaptured();
        StopCurrentTransition();
        ApplyDefaultImmediatelyInternal(true);
    }

    [ContextMenu("Apply Hover Immediately")]
    public void ApplyHoverImmediately()
    {
        EnsureTargets();
        EnsureDefaultsCaptured();
        StopCurrentTransition();

        for (int i = 0; i < targets.Count; i++)
        {
            TargetTransformSetting setting = targets[i];
            if (setting == null || setting.target == null)
                continue;

            setting.target.anchoredPosition = setting.hoverAnchoredPosition;
            setting.target.localScale = setting.hoverLocalScale;
        }
    }

    private void EnsureTargets()
    {
        targets ??= new List<TargetTransformSetting>();

        if (targets.Count > 0)
            return;

        RectTransform firstChild =
            transform.childCount > 0 ? transform.GetChild(0) as RectTransform : null;

        if (firstChild == null)
            return;

        targets.Add(new TargetTransformSetting
        {
            target = firstChild,
            hoverAnchoredPosition = firstChild.anchoredPosition,
            hoverLocalScale = firstChild.localScale
        });
    }

    private void EnsureDefaultsCaptured()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            TargetTransformSetting setting = targets[i];
            if (setting == null || setting.target == null || setting.defaultCaptured)
                continue;

            setting.defaultAnchoredPosition = setting.target.anchoredPosition;
            setting.defaultLocalScale = setting.target.localScale;
            setting.defaultCaptured = true;
        }
    }

    private void StartTransition(bool toHover)
    {
        StopCurrentTransition();

        if (transitionDuration <= 0f)
        {
            if (toHover)
                ApplyHoverImmediately();
            else
                ApplyDefaultImmediately();

            return;
        }

        transitionRoutine = StartCoroutine(TransitionRoutine(toHover));
    }

    private IEnumerator TransitionRoutine(bool toHover)
    {
        int count = targets.Count;
        Vector2[] startPositions = new Vector2[count];
        Vector3[] startScales = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            TargetTransformSetting setting = targets[i];
            if (setting == null || setting.target == null)
                continue;

            startPositions[i] = setting.target.anchoredPosition;
            startScales[i] = setting.target.localScale;
        }

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / transitionDuration);
            float curveValue = transitionCurve != null
                ? transitionCurve.Evaluate(normalizedTime)
                : normalizedTime;

            for (int i = 0; i < count; i++)
            {
                TargetTransformSetting setting = targets[i];
                if (setting == null || setting.target == null)
                    continue;

                Vector2 targetPosition = toHover
                    ? setting.hoverAnchoredPosition
                    : setting.defaultAnchoredPosition;

                Vector3 targetScale = toHover
                    ? setting.hoverLocalScale
                    : setting.defaultLocalScale;

                setting.target.anchoredPosition = Vector2.LerpUnclamped(
                    startPositions[i],
                    targetPosition,
                    curveValue);

                setting.target.localScale = Vector3.LerpUnclamped(
                    startScales[i],
                    targetScale,
                    curveValue);
            }

            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            TargetTransformSetting setting = targets[i];
            if (setting == null || setting.target == null)
                continue;

            setting.target.anchoredPosition = toHover
                ? setting.hoverAnchoredPosition
                : setting.defaultAnchoredPosition;

            setting.target.localScale = toHover
                ? setting.hoverLocalScale
                : setting.defaultLocalScale;
        }

        transitionRoutine = null;
    }

    private void ApplyDefaultImmediatelyInternal(bool captureIfNeeded)
    {
        if (captureIfNeeded)
            EnsureDefaultsCaptured();

        for (int i = 0; i < targets.Count; i++)
        {
            TargetTransformSetting setting = targets[i];
            if (setting == null || setting.target == null || !setting.defaultCaptured)
                continue;

            setting.target.anchoredPosition = setting.defaultAnchoredPosition;
            setting.target.localScale = setting.defaultLocalScale;
        }
    }

    private void StopCurrentTransition()
    {
        if (transitionRoutine == null)
            return;

        StopCoroutine(transitionRoutine);
        transitionRoutine = null;
    }
}
