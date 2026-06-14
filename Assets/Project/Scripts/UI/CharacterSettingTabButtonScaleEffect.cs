using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterSettingTabButtonScaleEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform scaleTarget;
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float breathMaxScale = 1.12f;
    [SerializeField] private float selectedScale = 1.2f;
    [SerializeField] private float scaleInDuration = 0.12f;
    [SerializeField] private float scaleOutDuration = 0.10f;
    [SerializeField] private float breathSpeed = 3.5f;

    private Vector3 originalScale = Vector3.one;
    private Coroutine scaleRoutine;
    private Coroutine breathRoutine;
    private bool initialized;
    private bool isPointerInside;
    private bool isSelected;

    private RectTransform Target
    {
        get
        {
            if (scaleTarget == null)
                scaleTarget = transform as RectTransform;

            return scaleTarget;
        }
    }

    private void Awake()
    {
        InitializeOriginalScale();
    }

    private void OnEnable()
    {
        InitializeOriginalScale();

        if (isSelected)
            SetScaleImmediate(selectedScale);
        else
            SetScaleImmediate(1f);
    }

    private void OnDisable()
    {
        StopAllScaleRoutines();
        isPointerInside = false;

        if (!isSelected)
            SetScaleImmediate(1f);
    }

    public void Setup(
        float newHoverScale,
        float newBreathMaxScale,
        float newSelectedScale,
        float newScaleInDuration,
        float newScaleOutDuration,
        float newBreathSpeed)
    {
        InitializeOriginalScale();

        hoverScale = Mathf.Max(0.01f, newHoverScale);
        breathMaxScale = Mathf.Max(hoverScale, newBreathMaxScale);
        selectedScale = Mathf.Max(0.01f, newSelectedScale);
        scaleInDuration = Mathf.Max(0.01f, newScaleInDuration);
        scaleOutDuration = Mathf.Max(0.01f, newScaleOutDuration);
        breathSpeed = Mathf.Max(0.01f, newBreathSpeed);

        if (isSelected)
            SetScaleImmediate(selectedScale);
        else if (!isPointerInside)
            SetScaleImmediate(1f);
    }

    public void SetSelected(bool selected)
    {
        if (isSelected == selected)
        {
            if (selected)
                SetScaleImmediate(selectedScale);

            return;
        }

        isSelected = selected;
        StopAllScaleRoutines();

        if (isSelected)
        {
            StartScaleRoutine(selectedScale, scaleInDuration, false);
            return;
        }

        if (isPointerInside)
            StartHoverBreath();
        else
            StartScaleRoutine(1f, scaleOutDuration, false);
    }

    public void ResetScaleImmediate()
    {
        StopAllScaleRoutines();
        isPointerInside = false;

        if (isSelected)
            SetScaleImmediate(selectedScale);
        else
            SetScaleImmediate(1f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;

        if (isSelected)
            return;

        StartHoverBreath();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;

        if (isSelected)
            return;

        StopAllScaleRoutines();
        StartScaleRoutine(1f, scaleOutDuration, false);
    }

    private void StartHoverBreath()
    {
        StopAllScaleRoutines();
        StartScaleRoutine(hoverScale, scaleInDuration, true);
    }

    private void StartScaleRoutine(float targetMultiplier, float duration, bool startBreathAfter)
    {
        if (!gameObject.activeInHierarchy)
        {
            SetScaleImmediate(targetMultiplier);
            return;
        }

        scaleRoutine = StartCoroutine(ScaleRoutine(targetMultiplier, duration, startBreathAfter));
    }

    private IEnumerator ScaleRoutine(float targetMultiplier, float duration, bool startBreathAfter)
    {
        RectTransform target = Target;

        if (target == null)
            yield break;

        Vector3 startScale = target.localScale;
        Vector3 endScale = originalScale * targetMultiplier;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            target.localScale = Vector3.LerpUnclamped(startScale, endScale, t);
            yield return null;
        }

        target.localScale = endScale;
        scaleRoutine = null;

        if (startBreathAfter && isPointerInside && !isSelected)
            breathRoutine = StartCoroutine(BreathRoutine());
    }

    private IEnumerator BreathRoutine()
    {
        RectTransform target = Target;

        if (target == null)
            yield break;

        float time = 0f;

        while (isPointerInside && !isSelected)
        {
            time += Time.unscaledDeltaTime * breathSpeed;
            float t = (Mathf.Sin(time) + 1f) * 0.5f;
            float scale = Mathf.Lerp(hoverScale, breathMaxScale, t);
            target.localScale = originalScale * scale;
            yield return null;
        }

        breathRoutine = null;
    }

    private void StopAllScaleRoutines()
    {
        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
            scaleRoutine = null;
        }

        if (breathRoutine != null)
        {
            StopCoroutine(breathRoutine);
            breathRoutine = null;
        }
    }

    private void SetScaleImmediate(float multiplier)
    {
        RectTransform target = Target;

        if (target != null)
            target.localScale = originalScale * multiplier;
    }

    private void InitializeOriginalScale()
    {
        if (initialized)
            return;

        RectTransform target = Target;

        if (target == null)
            return;

        originalScale = target.localScale;
        initialized = true;
    }
}
