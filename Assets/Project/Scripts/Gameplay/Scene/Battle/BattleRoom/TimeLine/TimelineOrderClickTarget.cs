using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class TimelineOrderClickTarget : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, Min(1f)] private float hoverScaleMultiplier = 1.1f;
    [SerializeField, Min(0f)] private float hoverScaleDuration = 0.12f;

    private BattleTimelineGroupUI owner;
    private int orderIndex;
    private Vector3 baseScale;
    private bool baseScaleCached;
    private Coroutine scaleRoutine;

    private void Awake()
    {
        CacheBaseScale();
    }

    private void OnDisable()
    {
        ResetScaleImmediate();
    }

    public void Init(BattleTimelineGroupUI owner, int orderIndex)
    {
        this.owner = owner;
        this.orderIndex = orderIndex;
        CacheBaseScale();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (owner != null)
            owner.OnOrderClicked(orderIndex);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        AnimateScale(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AnimateScale(false);
    }

    private void CacheBaseScale()
    {
        if (baseScaleCached)
            return;

        baseScale = transform.localScale;
        baseScaleCached = true;
    }

    private void AnimateScale(bool hovered)
    {
        CacheBaseScale();

        if (scaleRoutine != null)
            StopCoroutine(scaleRoutine);

        Vector3 targetScale = hovered
            ? Vector3.Scale(baseScale, Vector3.one * hoverScaleMultiplier)
            : baseScale;

        scaleRoutine = StartCoroutine(AnimateScaleRoutine(targetScale));
    }

    private IEnumerator AnimateScaleRoutine(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float duration = Mathf.Max(0f, hoverScaleDuration);

        if (duration <= 0f)
        {
            transform.localScale = targetScale;
            scaleRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, t);
            yield return null;
        }

        transform.localScale = targetScale;
        scaleRoutine = null;
    }

    private void ResetScaleImmediate()
    {
        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
            scaleRoutine = null;
        }

        if (baseScaleCached)
            transform.localScale = baseScale;
    }
}
