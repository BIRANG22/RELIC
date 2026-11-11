using UnityEngine;

/// <summary>
/// Moves an image once from left to right around its original position.
/// Works with UI RectTransform and world-space Transform.
/// </summary>
public class ImageHorizontalFloat : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveDistance = 10f;
    [SerializeField, Min(0.01f)] private float moveDuration = 1f;
    [SerializeField] private bool useUnscaledTime = true;

    private RectTransform rectTransform;
    private Vector3 originalPosition;
    private float elapsed;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        originalPosition = GetPosition();
    }

    private void OnEnable()
    {
        elapsed = 0f;
        SetPosition(originalPosition + Vector3.left * moveDistance);
    }

    private void Update()
    {
        if (elapsed >= moveDuration) return;

        elapsed = Mathf.Min(moveDuration, elapsed +
            (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime));

        float t = elapsed / moveDuration;
        t = t * t * (3f - 2f * t); // SmoothStep
        SetPosition(originalPosition + Vector3.right * Mathf.Lerp(-moveDistance, moveDistance, t));
    }

    private Vector3 GetPosition()
    {
        return rectTransform != null ? rectTransform.anchoredPosition3D : transform.localPosition;
    }

    private void SetPosition(Vector3 position)
    {
        if (rectTransform != null) rectTransform.anchoredPosition3D = position;
        else transform.localPosition = position;
    }
}
