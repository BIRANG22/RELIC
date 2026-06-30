using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 로비 배경에 있는 월드 오브젝트 휠들을 각자 정해진 시간 간격마다 Z축으로 한 스텝씩 회전시킵니다.
/// 휠마다 회전 각도, 회전 시간, 반복 간격, 시작 지연 시간을 따로 설정할 수 있습니다.
/// </summary>
public sealed class LobbyWheelStepRotator : MonoBehaviour
{
    [System.Serializable]
    private sealed class WheelTarget
    {
        [Tooltip("회전시킬 월드 오브젝트입니다. 예: wheel04, wheel03, wheel02")]
        public Transform target;

        [Tooltip("한 번 회전할 Z 각도입니다. 양수/음수로 방향을 다르게 줄 수 있습니다.")]
        public float stepAngleZ = 45f;

        [Tooltip("이 휠이 한 스텝 회전하는 데 걸리는 시간입니다. 0이면 즉시 회전합니다.")]
        [Min(0f)] public float duration = 0.8f;

        [Tooltip("자동 회전 시 몇 초마다 한 번씩 회전할지 정합니다. Duration보다 길게 두면 회전 후 멈춰있는 시간이 생깁니다.")]
        [Min(0.01f)] public float interval = 2f;

        [Tooltip("오브젝트가 켜진 뒤 첫 회전을 시작하기 전까지 기다릴 시간입니다.")]
        [Min(0f)] public float startDelay = 0f;

        [Tooltip("회전 보간 곡선입니다. 곡선이 비어 있거나 평평하면 자동으로 부드러운 기본 보간을 사용합니다.")]
        public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    [Header("Wheel Targets")]
    [SerializeField] private List<WheelTarget> wheels = new List<WheelTarget>();

    [Header("Auto Rotation")]
    [Tooltip("켜두면 이 오브젝트가 활성화되어 있는 동안 각 휠이 설정한 시간 간격마다 자동으로 회전합니다.")]
    [SerializeField] private bool autoRotateOnEnable = true;

    [Tooltip("켜두면 활성화 직후 첫 회전은 기다리지 않고 바로 실행합니다. 꺼두면 Start Delay와 Interval을 기준으로 시작합니다.")]
    [SerializeField] private bool rotateImmediatelyOnEnable = false;

    [Header("Playback")]
    [Tooltip("Time.timeScale 영향을 받지 않고 회전합니다.")]
    [SerializeField] private bool useUnscaledTime = true;

    [Tooltip("수동 RotateOnce() 호출 시 이미 회전 중이면 다음 회전을 예약합니다.")]
    [SerializeField] private bool queueInputWhileRotating = true;

    private readonly List<Coroutine> autoRoutines = new List<Coroutine>();
    private Coroutine rotateRoutine;
    private int queuedStepCount;

    private void OnEnable()
    {
        if (autoRotateOnEnable)
            StartAutoRotation();
    }

    private void OnDisable()
    {
        StopAutoRotation();

        if (rotateRoutine != null)
        {
            StopCoroutine(rotateRoutine);
            rotateRoutine = null;
        }

        queuedStepCount = 0;
    }

    /// <summary>
    /// 모든 휠의 자동 회전을 시작합니다.
    /// </summary>
    public void StartAutoRotation()
    {
        StopAutoRotation();

        for (int i = 0; i < wheels.Count; i++)
        {
            WheelTarget wheel = wheels[i];
            if (wheel == null || wheel.target == null)
                continue;

            autoRoutines.Add(StartCoroutine(AutoRotateWheelRoutine(wheel)));
        }
    }

    /// <summary>
    /// 모든 휠의 자동 회전을 멈춥니다.
    /// </summary>
    public void StopAutoRotation()
    {
        for (int i = 0; i < autoRoutines.Count; i++)
        {
            if (autoRoutines[i] != null)
                StopCoroutine(autoRoutines[i]);
        }

        autoRoutines.Clear();
    }

    /// <summary>
    /// Inspector의 Button OnClick, Animation Event, 다른 스크립트에서 호출할 수 있습니다.
    /// 호출될 때마다 모든 휠이 각자 설정한 Z 각도만큼 한 번 회전합니다.
    /// </summary>
    public void RotateOnce()
    {
        if (rotateRoutine != null)
        {
            if (queueInputWhileRotating)
                queuedStepCount++;

            return;
        }

        rotateRoutine = StartCoroutine(RotateAllRoutine());
    }

    /// <summary>
    /// 애니메이션 없이 모든 휠을 즉시 한 스텝 회전합니다.
    /// </summary>
    public void RotateOnceInstant()
    {
        for (int i = 0; i < wheels.Count; i++)
        {
            WheelTarget wheel = wheels[i];
            if (wheel == null || wheel.target == null)
                continue;

            wheel.target.localRotation = wheel.target.localRotation * Quaternion.Euler(0f, 0f, wheel.stepAngleZ);
        }
    }

    private IEnumerator AutoRotateWheelRoutine(WheelTarget wheel)
    {
        if (rotateImmediatelyOnEnable)
        {
            yield return RotateSingleWheelRoutine(wheel);
        }
        else
        {
            float firstWait = wheel.startDelay > 0f ? wheel.startDelay : wheel.interval;
            yield return WaitForSecondsByMode(firstWait);
        }

        while (enabled && gameObject.activeInHierarchy)
        {
            yield return RotateSingleWheelRoutine(wheel);
            yield return WaitForSecondsByMode(Mathf.Max(0.01f, wheel.interval));
        }
    }

    private IEnumerator RotateAllRoutine()
    {
        do
        {
            queuedStepCount = Mathf.Max(0, queuedStepCount - 1);
            yield return RotateAllOneStepRoutine();
        }
        while (queuedStepCount > 0);

        rotateRoutine = null;
    }

    private IEnumerator RotateAllOneStepRoutine()
    {
        int count = wheels.Count;
        Quaternion[] startRotations = new Quaternion[count];
        Quaternion[] targetRotations = new Quaternion[count];
        float maxDuration = 0f;

        for (int i = 0; i < count; i++)
        {
            WheelTarget wheel = wheels[i];
            if (wheel == null || wheel.target == null)
                continue;

            startRotations[i] = wheel.target.localRotation;
            targetRotations[i] = startRotations[i] * Quaternion.Euler(0f, 0f, wheel.stepAngleZ);
            maxDuration = Mathf.Max(maxDuration, wheel.duration);
        }

        if (maxDuration <= 0f)
        {
            ApplyWheelRotations(targetRotations);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < maxDuration)
        {
            elapsed += GetDeltaTime();

            for (int i = 0; i < count; i++)
            {
                WheelTarget wheel = wheels[i];
                if (wheel == null || wheel.target == null)
                    continue;

                float duration = Mathf.Max(0.0001f, wheel.duration);
                float normalized = Mathf.Clamp01(elapsed / duration);
                float curveValue = EvaluateCurve(wheel.curve, normalized);
                wheel.target.localRotation = Quaternion.LerpUnclamped(startRotations[i], targetRotations[i], curveValue);
            }

            yield return null;
        }

        ApplyWheelRotations(targetRotations);
    }

    private IEnumerator RotateSingleWheelRoutine(WheelTarget wheel)
    {
        if (wheel == null || wheel.target == null)
            yield break;

        Quaternion startRotation = wheel.target.localRotation;
        Quaternion targetRotation = startRotation * Quaternion.Euler(0f, 0f, wheel.stepAngleZ);

        if (wheel.duration <= 0f)
        {
            wheel.target.localRotation = targetRotation;
            yield break;
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.0001f, wheel.duration);
        while (elapsed < duration)
        {
            elapsed += GetDeltaTime();
            float normalized = Mathf.Clamp01(elapsed / duration);
            float curveValue = EvaluateCurve(wheel.curve, normalized);
            wheel.target.localRotation = Quaternion.LerpUnclamped(startRotation, targetRotation, curveValue);
            yield return null;
        }

        wheel.target.localRotation = targetRotation;
    }

    private IEnumerator WaitForSecondsByMode(float seconds)
    {
        if (seconds <= 0f)
            yield break;

        if (useUnscaledTime)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(seconds);
        }
    }

    private void ApplyWheelRotations(Quaternion[] targetRotations)
    {
        for (int i = 0; i < wheels.Count; i++)
        {
            WheelTarget wheel = wheels[i];
            if (wheel == null || wheel.target == null)
                continue;

            wheel.target.localRotation = targetRotations[i];
        }
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private static float EvaluateCurve(AnimationCurve curve, float normalized)
    {
        normalized = Mathf.Clamp01(normalized);

        if (curve == null || curve.length < 2)
            return Mathf.SmoothStep(0f, 1f, normalized);

        float start = curve.Evaluate(0f);
        float end = curve.Evaluate(1f);

        // 곡선이 평평하게 저장되어 있으면 프레임 중간값이 계속 0이 되어 마지막에 순간 이동처럼 보일 수 있습니다.
        // 이 경우 기본 SmoothStep을 사용해서 실제 회전이 보이게 합니다.
        if (Mathf.Abs(end - start) < 0.0001f)
            return Mathf.SmoothStep(0f, 1f, normalized);

        float value = Mathf.InverseLerp(start, end, curve.Evaluate(normalized));
        return Mathf.Clamp01(value);
    }
}
