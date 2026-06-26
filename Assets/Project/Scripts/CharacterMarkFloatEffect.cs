using UnityEngine;

public sealed class CharacterMarkFloatEffect : MonoBehaviour
{
    [Header("Float")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private float moveAmount = 8f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Vector2 moveDirection = Vector2.up;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Optional Scale Breath")]
    [SerializeField] private bool useScaleBreath = false;
    [SerializeField] private float scaleAmount = 0.03f;
    [SerializeField] private float scaleSpeed = 2f;

    private RectTransform rectTransform;
    private Vector2 baseAnchoredPosition;
    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;
    private float startTime;
    private bool hasBaseValue;
    private bool isPlaying;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        CacheBaseValue();
    }

    private void OnEnable()
    {
        CacheBaseValue();
        startTime = GetCurrentTime();
        isPlaying = playOnEnable;
    }

    private void OnDisable()
    {
        RestoreBaseValue();
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        if (!hasBaseValue)
            CacheBaseValue();

        float time = GetCurrentTime() - startTime;
        float wave = Mathf.Sin(time * moveSpeed * Mathf.PI * 2f);
        Vector2 direction = moveDirection.sqrMagnitude > 0.0001f ? moveDirection.normalized : Vector2.up;
        Vector2 offset = direction * (wave * moveAmount);

        if (rectTransform != null)
            rectTransform.anchoredPosition = baseAnchoredPosition + offset;
        else
            transform.localPosition = baseLocalPosition + new Vector3(offset.x, offset.y, 0f);

        if (useScaleBreath)
        {
            float scaleWave = Mathf.Sin(time * scaleSpeed * Mathf.PI * 2f);
            float scale = 1f + (scaleWave * scaleAmount);
            transform.localScale = baseLocalScale * scale;
        }
        else
        {
            transform.localScale = baseLocalScale;
        }
    }

    public void Play()
    {
        CacheBaseValue();
        startTime = GetCurrentTime();
        isPlaying = true;
    }

    public void Stop(bool restorePosition = true)
    {
        isPlaying = false;

        if (restorePosition)
            RestoreBaseValue();
    }

    public void RefreshBaseValue()
    {
        CacheBaseValue(true);
    }

    private void CacheBaseValue(bool force = false)
    {
        if (hasBaseValue && !force)
            return;

        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        if (rectTransform != null)
            baseAnchoredPosition = rectTransform.anchoredPosition;
        else
            baseLocalPosition = transform.localPosition;

        baseLocalScale = transform.localScale;
        hasBaseValue = true;
    }

    private void RestoreBaseValue()
    {
        if (!hasBaseValue)
            return;

        if (rectTransform != null)
            rectTransform.anchoredPosition = baseAnchoredPosition;
        else
            transform.localPosition = baseLocalPosition;

        transform.localScale = baseLocalScale;
    }

    private float GetCurrentTime()
    {
        return useUnscaledTime ? Time.unscaledTime : Time.time;
    }
}
