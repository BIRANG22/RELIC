using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 미리 고정한 두 화면 좌표를 연결하는 프레젠테이션 전용 이동 효과입니다.
/// 상위 Canvas보다 높은 정렬 순서의 중첩 Canvas에서 렌더링하므로 모달 블러 위에서도 보입니다.
/// </summary>
[RequireComponent(typeof(Canvas))]
public sealed class ScreenSpaceTransferOrbEffect : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private RawImage orbImage;
    [SerializeField] private Texture orbTexture;
    [SerializeField] private Vector2 orbSize = new(120f, 120f);
    [SerializeField, Min(0.01f)] private float duration = 0.6f;
    [SerializeField, Min(0f)] private float arcHeight = 220f;
    [Tooltip("로비의 블러/상단 UI Canvas(현재 최대 10001)보다 앞에 표시할 정렬 순서입니다.")]
    [SerializeField, Min(10002)] private int sortingOrder = 20000;

    [Header("Trail")]
    [SerializeField, Min(0.005f)] private float trailSpawnInterval = 0.025f;
    [SerializeField, Min(0.01f)] private float trailLifetime = 0.22f;
    [SerializeField, Range(0f, 1f)] private float trailStartAlpha = 0.55f;
    [SerializeField, Range(0.01f, 1f)] private float trailEndScale = 0.2f;

    private readonly List<TrailGhost> trailGhosts = new();
    private Canvas effectCanvas;
    private RectTransform effectRect;

    private sealed class TrailGhost
    {
        public RawImage Image;
        public float Age;
    }

    private void Awake()
    {
        effectCanvas = GetComponent<Canvas>();
        effectCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        effectCanvas.worldCamera = null;
        effectCanvas.overrideSorting = true;
        effectCanvas.sortingOrder = sortingOrder;
        effectRect = transform as RectTransform;
        effectRect.localScale = Vector3.one;

        if (orbImage == null)
            orbImage = GetComponentInChildren<RawImage>(true);

        if (orbImage != null)
        {
            if (orbTexture != null)
                orbImage.texture = orbTexture;
            orbImage.raycastTarget = false;
            orbImage.rectTransform.sizeDelta = orbSize;
        }
    }

    /// <summary>
    /// UI 계층 상태와 무관하게 재생할 수 있도록, 호출자가 구매 직후 확보한 화면 좌표만 받습니다.
    /// </summary>
    public IEnumerator Play(Vector2 startScreen, Vector2 endScreen, Color color)
    {
        if (effectRect == null || orbImage == null)
        {
            Destroy(gameObject);
            yield break;
        }

        orbImage.color = color;
        float elapsed = 0f;
        float trailTimer = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            float deltaTime = Time.unscaledDeltaTime;
            elapsed += deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            Vector2 controlScreen = (startScreen + endScreen) * 0.5f + Vector2.up * arcHeight;
            Vector2 screenPosition = EvaluateQuadraticBezier(startScreen, controlScreen, endScreen, EaseOutCubic(t));

            orbImage.rectTransform.anchoredPosition = ScreenToLocalPosition(screenPosition);
            trailTimer += deltaTime;
            SpawnTrailGhosts(color, ref trailTimer);
            UpdateTrailGhosts(deltaTime);
            yield return null;
        }

        while (trailGhosts.Count > 0)
        {
            UpdateTrailGhosts(Time.unscaledDeltaTime);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void SpawnTrailGhosts(Color color, ref float timer)
    {
        float interval = Mathf.Max(0.005f, trailSpawnInterval);
        while (timer >= interval)
        {
            timer -= interval;
            GameObject ghostObject = new("ScreenSpaceTransferOrbTrail", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            RectTransform ghostRect = ghostObject.GetComponent<RectTransform>();
            ghostRect.SetParent(orbImage.rectTransform.parent, false);
            ghostRect.anchorMin = orbImage.rectTransform.anchorMin;
            ghostRect.anchorMax = orbImage.rectTransform.anchorMax;
            ghostRect.pivot = orbImage.rectTransform.pivot;
            ghostRect.sizeDelta = orbImage.rectTransform.sizeDelta;
            ghostRect.anchoredPosition = orbImage.rectTransform.anchoredPosition;
            ghostRect.localScale = orbImage.rectTransform.localScale;
            ghostRect.SetSiblingIndex(Mathf.Max(0, orbImage.rectTransform.GetSiblingIndex()));

            RawImage image = ghostObject.GetComponent<RawImage>();
            image.texture = orbImage.texture;
            image.color = new Color(color.r, color.g, color.b, color.a * trailStartAlpha);
            image.raycastTarget = false;
            trailGhosts.Add(new TrailGhost { Image = image });
        }
    }

    private void UpdateTrailGhosts(float deltaTime)
    {
        float lifetime = Mathf.Max(0.01f, trailLifetime);
        for (int i = trailGhosts.Count - 1; i >= 0; i--)
        {
            TrailGhost ghost = trailGhosts[i];
            if (ghost?.Image == null)
            {
                trailGhosts.RemoveAt(i);
                continue;
            }

            ghost.Age += deltaTime;
            float t = Mathf.Clamp01(ghost.Age / lifetime);
            Color color = ghost.Image.color;
            color.a = trailStartAlpha * (1f - t);
            ghost.Image.color = color;
            ghost.Image.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, trailEndScale, t);
            if (t < 1f)
                continue;

            Destroy(ghost.Image.gameObject);
            trailGhosts.RemoveAt(i);
        }
    }

    private Vector2 ScreenToLocalPosition(Vector2 screenPosition)
    {
        Camera canvasCamera = effectCanvas != null && effectCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : effectCanvas != null ? effectCanvas.worldCamera : null;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            effectRect,
            screenPosition,
            canvasCamera,
            out Vector2 localPoint)
            ? localPoint
            : Vector2.zero;
    }

    /// <summary>
    /// RectTransform의 현재 중심을 화면 좌표로 변환합니다.
    /// 재생 시작 전에 호출해 UI 활성 상태 변화로부터 연출 좌표를 분리합니다.
    /// </summary>
    public static Vector2 GetRectScreenCenter(RectTransform rect, Camera camera)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return RectTransformUtility.WorldToScreenPoint(camera, (corners[0] + corners[2]) * 0.5f);
    }

    /// <summary>
    /// RectTransform을 직접 소유한 가장 가까운 Canvas를 기준으로 화면 좌표 변환 카메라를 결정합니다.
    /// 로비 PositionPanel처럼 Overlay 루트 아래에 World Space Canvas가 중첩된 경우에도
    /// 해당 World Space 좌표를 Camera.main으로 투영해야 합니다.
    /// </summary>
    public static Camera ResolveUiCamera(RectTransform rect, Camera fallbackCamera)
    {
        if (rect == null)
            return fallbackCamera;

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera != null ? canvas.worldCamera : fallbackCamera;
    }

    public static Vector2 EvaluateQuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t)
    {
        t = Mathf.Clamp01(t);
        float inverse = 1f - t;
        return inverse * inverse * start + 2f * inverse * t * control + t * t * end;
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        float inverse = 1f - t;
        return 1f - inverse * inverse * inverse;
    }
}
