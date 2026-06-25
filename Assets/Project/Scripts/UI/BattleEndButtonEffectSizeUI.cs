using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class BattleEndButtonEffectSizeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [Header("Target")]
    [SerializeField] private RectTransform effectRect;
    [SerializeField] private bool autoFindEffect = true;

    [Header("Size")]
    [SerializeField] private Vector2 normalSize = new Vector2(180f, 180f);
    [SerializeField] private Vector2 hoverSize = new Vector2(240f, 240f);
    [SerializeField] private Vector2 pressedSize = new Vector2(140f, 140f);

    [Header("Animation")]
    [SerializeField, Min(0f)] private float resizeDuration = 0.08f;
    [SerializeField] private bool useUnscaledTime = true;

    private Coroutine resizeRoutine;
    private bool isPointerInside;
    private bool isPressedUntilTurnReturned;

    private void Awake()
    {
        BindEffectIfNeeded();
        SetSizeImmediate(normalSize);
    }

    private void OnEnable()
    {
        BattleTurnExecutor.PlayerTurnReturned -= ResetToNormalSize;
        BattleTurnExecutor.PlayerTurnReturned += ResetToNormalSize;
    }

    private void OnDisable()
    {
        BattleTurnExecutor.PlayerTurnReturned -= ResetToNormalSize;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;

        if (isPressedUntilTurnReturned)
            return;

        ResizeTo(hoverSize);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;

        if (isPressedUntilTurnReturned)
            return;

        ResizeTo(isPointerInside ? hoverSize : normalSize);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressedUntilTurnReturned = true;
        ResizeTo(pressedSize);
    }

    private void ResetToNormalSize()
    {
        isPressedUntilTurnReturned = false;
        ResizeTo(normalSize);
    }

    private void BindEffectIfNeeded()
    {
        if (effectRect != null || !autoFindEffect)
            return;

        Transform effect = transform.Find("Effect");
        if (effect == null)
            return;

        effectRect = effect as RectTransform;
    }

    private void ResizeTo(Vector2 targetSize)
    {
        BindEffectIfNeeded();

        if (effectRect == null)
            return;

        if (resizeRoutine != null)
            StopCoroutine(resizeRoutine);

        if (resizeDuration <= 0f || !gameObject.activeInHierarchy)
        {
            SetSizeImmediate(targetSize);
            return;
        }

        resizeRoutine = StartCoroutine(ResizeRoutine(targetSize));
    }

    private IEnumerator ResizeRoutine(Vector2 targetSize)
    {
        Vector2 startSize = effectRect.sizeDelta;
        float elapsed = 0f;

        while (elapsed < resizeDuration)
        {
            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += deltaTime;

            float t = resizeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / resizeDuration);
            effectRect.sizeDelta = Vector2.Lerp(startSize, targetSize, t);

            yield return null;
        }

        effectRect.sizeDelta = targetSize;
        resizeRoutine = null;
    }

    private void SetSizeImmediate(Vector2 size)
    {
        BindEffectIfNeeded();

        if (effectRect == null)
            return;

        effectRect.sizeDelta = size;
    }
}
