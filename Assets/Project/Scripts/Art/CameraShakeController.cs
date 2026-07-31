using System.Collections;
using UnityEngine;

public class CameraShakeController : MonoBehaviour
{
    [Header("기본 흔들림 설정")]
    [Tooltip("흔들림이 지속되는 시간")]
    [Min(0f)]
    [SerializeField] private float defaultDuration = 0.2f;

    [Tooltip("카메라가 흔들리는 위치 범위")]
    [Min(0f)]
    [SerializeField] private float defaultStrength = 0.15f;

    [Tooltip("흔들림의 빠르기")]
    [Min(0f)]
    [SerializeField] private float defaultFrequency = 30f;

    [Header("축 설정")]
    [SerializeField] private bool shakeX = true;
    [SerializeField] private bool shakeY = true;
    [SerializeField] private bool shakeZ = false;

    [Header("시간 설정")]
    [Tooltip("Time Scale이 0이어도 흔들림을 실행")]
    [SerializeField] private bool useUnscaledTime = false;

    private Coroutine shakeCoroutine;
    private Vector3 originalLocalPosition;

    private void Awake()
    {
        originalLocalPosition = transform.localPosition;
    }

    /// <summary>
    /// 인스펙터에서 설정한 기본값으로 흔듭니다.
    /// </summary>
    public void Shake()
    {
        Shake(defaultDuration, defaultStrength, defaultFrequency);
    }

    /// <summary>
    /// 전달받은 설정으로 흔듭니다.
    /// </summary>
    public void Shake(
        float duration,
        float strength,
        float frequency)
    {
        StopShake();

        originalLocalPosition = transform.localPosition;

        shakeCoroutine = StartCoroutine(
            ShakeRoutine(
                Mathf.Max(0f, duration),
                Mathf.Max(0f, strength),
                Mathf.Max(0f, frequency)
            )
        );
    }

    private IEnumerator ShakeRoutine(
        float duration,
        float strength,
        float frequency)
    {
        float elapsedTime = 0f;
        float randomSeedX = Random.Range(0f, 100f);
        float randomSeedY = Random.Range(0f, 100f);
        float randomSeedZ = Random.Range(0f, 100f);

        while (elapsedTime < duration)
        {
            elapsedTime += GetDeltaTime();

            float progress = duration > 0f
                ? Mathf.Clamp01(elapsedTime / duration)
                : 1f;

            // 흔들림이 끝날수록 세기를 줄임
            float currentStrength =
                strength * (1f - progress);

            float noiseTime = elapsedTime * frequency;

            float offsetX = shakeX
                ? GetNoise(randomSeedX, noiseTime) * currentStrength
                : 0f;

            float offsetY = shakeY
                ? GetNoise(randomSeedY, noiseTime) * currentStrength
                : 0f;

            float offsetZ = shakeZ
                ? GetNoise(randomSeedZ, noiseTime) * currentStrength
                : 0f;

            transform.localPosition =
                originalLocalPosition +
                new Vector3(offsetX, offsetY, offsetZ);

            yield return null;
        }

        transform.localPosition = originalLocalPosition;
        shakeCoroutine = null;
    }

    private float GetNoise(float seed, float time)
    {
        return Mathf.PerlinNoise(seed, time) * 2f - 1f;
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime
            ? Time.unscaledDeltaTime
            : Time.deltaTime;
    }

    public void StopShake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        transform.localPosition = originalLocalPosition;
    }

    private void OnDisable()
    {
        StopShake();
    }
}