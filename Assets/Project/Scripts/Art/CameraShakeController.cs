using UnityEngine;

/// <summary>
/// 현재 카메라 이동을 유지하면서 흔들림 오프셋만 적용합니다.
/// BattleCameraController의 LateUpdate 이후에 흔들림을 적용합니다.
/// </summary>
[DefaultExecutionOrder(1000)]
public class CameraShakeController : MonoBehaviour
{
    [Header("기본 흔들림 설정")]
    [Tooltip("흔들림이 지속되는 시간(초)")]
    [Min(0f)] [SerializeField] private float defaultDuration = 0.2f;

    [Tooltip("카메라 흔들림의 최대 이동 거리")]
    [Min(0f)] [SerializeField] private float defaultStrength = 0.15f;

    [Tooltip("흔들림의 빠르기")]
    [Min(0f)] [SerializeField] private float defaultFrequency = 30f;

    [Header("축 설정")]
    [SerializeField] private bool shakeX = true;
    [SerializeField] private bool shakeY = true;
    [SerializeField] private bool shakeZ = false;

    [Header("시간 설정")]
    [Tooltip("Time Scale이 0이어도 흔들림을 진행합니다.")]
    [SerializeField] private bool useUnscaledTime = false;

    private bool isShaking;
    private float elapsed;
    private float duration;
    private float strength;
    private float frequency;
    private float seedX;
    private float seedY;
    private float seedZ;
    private Vector3 appliedLocalOffset;

    public void Shake()
    {
        Shake(defaultDuration, defaultStrength, defaultFrequency);
    }

    public void Shake(float requestedDuration, float requestedStrength, float requestedFrequency)
    {
        // 흔들림 중에 새 요청이 와도 현재 위치를 저장하거나 원위치로 강제 이동하지 않습니다.
        RemoveAppliedOffset();

        duration = Mathf.Max(0f, requestedDuration);
        strength = Mathf.Max(0f, requestedStrength);
        frequency = Mathf.Max(0f, requestedFrequency);
        elapsed = 0f;
        seedX = Random.Range(0f, 1000f);
        seedY = Random.Range(0f, 1000f);
        seedZ = Random.Range(0f, 1000f);
        isShaking = duration > 0f && strength > 0f;
    }

    private void LateUpdate()
    {
        // 다른 카메라 시스템이 이번 프레임에 이동시킨 위치를 그대로 유지합니다.
        // 전 프레임의 흔들림 성분만 제거하고 새로운 흔들림을 더합니다.
        RemoveAppliedOffset();
        if (!isShaking)
            return;

        elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / duration);
        if (progress >= 1f)
        {
            isShaking = false;
            return;
        }

        float amount = strength * (1f - progress);
        float time = elapsed * frequency;
        appliedLocalOffset = new Vector3(
            shakeX ? Noise(seedX, time) * amount : 0f,
            shakeY ? Noise(seedY, time) * amount : 0f,
            shakeZ ? Noise(seedZ, time) * amount : 0f
        );
        transform.localPosition += appliedLocalOffset;
    }

    private static float Noise(float seed, float time)
    {
        return Mathf.PerlinNoise(seed, time) * 2f - 1f;
    }

    private void RemoveAppliedOffset()
    {
        if (appliedLocalOffset == Vector3.zero)
            return;

        transform.localPosition -= appliedLocalOffset;
        appliedLocalOffset = Vector3.zero;
    }

    public void StopShake()
    {
        isShaking = false;
        RemoveAppliedOffset();
    }

    private void OnDisable()
    {
        StopShake();
    }
}
