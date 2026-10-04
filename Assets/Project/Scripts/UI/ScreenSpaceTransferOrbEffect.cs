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

    [Header("Motion Randomness")]
    [Tooltip("도착점으로 빨려 들어가는 곡선의 상승 관성에 적용할 랜덤 배율 범위입니다.")]
    [SerializeField] private Vector2 arcHeightMultiplierRange = new(0.72f, 1.28f);
    [Tooltip("진행 방향의 수직 방향으로 포물선 중심점을 랜덤하게 흔드는 화면 픽셀 범위입니다.")]
    [SerializeField, Min(0f)] private float lateralRandomness = 130f;
    [Tooltip("각 연출의 전체 이동 시간에 적용할 랜덤 배율 범위입니다.")]
    [SerializeField] private Vector2 durationMultiplierRange = new(0.9f, 1.08f);

    [Header("Launch And Suck")]
    [Tooltip("시작점에서 도착점 반대 대각선 방향으로 먼저 튀어 오르는 가로 거리 범위입니다.")]
    [SerializeField] private Vector2 oppositeKickHorizontalRange = new(85f, 155f);
    [Tooltip("반대 대각선 방향으로 튀어 오를 때의 세로 상승 거리 범위입니다.")]
    [SerializeField] private Vector2 oppositeKickVerticalRange = new(120f, 210f);
    [Tooltip("첫 튀어 오름에 추가되는 좌우 랜덤 흔들림입니다.")]
    [SerializeField, Min(0f)] private float oppositeKickSideRandomness = 30f;
    [Tooltip("전체 연출 시간 중 시작점에서 반대 대각선으로 튀어 오르는 구간의 비율입니다.")]
    [SerializeField, Range(0.12f, 0.55f)] private float kickDurationRatio = 0.32f;
    [Tooltip("도착점으로 빨려 들어갈 때 곡선에 남길 상승 관성의 크기입니다.")]
    [SerializeField, Min(0f)] private float suckCurveLift = 75f;

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
    private float trailTimer;

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
    /// 호출한 UI가 닫히거나 비활성화되어도 연출이 끝까지 재생되도록
    /// 이 효과 오브젝트 자신이 코루틴을 실행합니다.
    /// </summary>
    public void PlayDetached(Vector2 startScreen, Vector2 endScreen, Color color)
    {
        StartCoroutine(Play(startScreen, endScreen, color));
    }

    /// <summary>
    /// UI 계층 상태와 무관하게 재생할 수 있도록, 호출자가 구매 직후 확보한 화면 좌표만 받습니다.
    /// </summary>
    public IEnumerator Play(Vector2 startScreen, Vector2 endScreen, Color color)
    {
        if (effectRect == null || orbImage == null)
        {
            Destroy(gameObject);
            return null;
        }

        orbImage.color = color;
        orbImage.rectTransform.anchoredPosition = ScreenToLocalPosition(startScreen);

        return PlayRoutine(startScreen, endScreen, color);
    }

    private IEnumerator PlayRoutine(Vector2 startScreen, Vector2 endScreen, Color color)
    {
        trailTimer = 0f;

        float durationMultiplier = Random.Range(
            Mathf.Min(durationMultiplierRange.x, durationMultiplierRange.y),
            Mathf.Max(durationMultiplierRange.x, durationMultiplierRange.y));
        float safeDuration = Mathf.Max(0.01f, duration * durationMultiplier);

        float kickRatio = Mathf.Clamp(kickDurationRatio, 0.12f, 0.55f);
        float kickDuration = safeDuration * kickRatio;
        float suckDuration = Mathf.Max(0.01f, safeDuration - kickDuration);

        Vector2 travel = endScreen - startScreen;
        Vector2 travelDirection = travel.sqrMagnitude > 0.001f ? travel.normalized : Vector2.right;
        Vector2 perpendicular = new(-travelDirection.y, travelDirection.x);

        float horizontalKick = Random.Range(
            Mathf.Min(oppositeKickHorizontalRange.x, oppositeKickHorizontalRange.y),
            Mathf.Max(oppositeKickHorizontalRange.x, oppositeKickHorizontalRange.y));
        float verticalKick = Random.Range(
            Mathf.Min(oppositeKickVerticalRange.x, oppositeKickVerticalRange.y),
            Mathf.Max(oppositeKickVerticalRange.x, oppositeKickVerticalRange.y));

        // 도착지가 시작점의 오른쪽이면 왼쪽 위, 왼쪽이면 오른쪽 위로 먼저 튀어 오릅니다.
        // 거의 수직 방향일 때는 좌우 중 한 방향을 랜덤하게 선택합니다.
        float oppositeHorizontalSign;
        if (Mathf.Abs(travel.x) > 1f)
            oppositeHorizontalSign = -Mathf.Sign(travel.x);
        else
            oppositeHorizontalSign = Random.value < 0.5f ? -1f : 1f;

        float randomSide = Random.Range(-oppositeKickSideRandomness, oppositeKickSideRandomness);
        Vector2 kickPoint = startScreen
            + new Vector2(oppositeHorizontalSign * horizontalKick, verticalKick)
            + perpendicular * randomSide;

        // 시작점에서 도착점 반대 대각선으로 먼저 튕겨 올라갑니다.
        yield return AnimateSegment(
            kickDuration,
            t => Vector2.LerpUnclamped(startScreen, kickPoint, EaseOutCubic(t)),
            color);

        // 튀어 오른 관성을 살짝 남긴 채, 도착점으로 갈수록 강하게 가속되어 빨려 들어갑니다.
        float randomArcMultiplier = Random.Range(
            Mathf.Min(arcHeightMultiplierRange.x, arcHeightMultiplierRange.y),
            Mathf.Max(arcHeightMultiplierRange.x, arcHeightMultiplierRange.y));
        float curveSide = Random.Range(-lateralRandomness, lateralRandomness) * 0.35f;
        Vector2 suckControl = Vector2.Lerp(kickPoint, endScreen, 0.35f)
            + Vector2.up * (suckCurveLift * randomArcMultiplier)
            + perpendicular * curveSide;

        yield return AnimateSegment(
            suckDuration,
            t => EvaluateQuadraticBezier(kickPoint, suckControl, endScreen, EaseInCubic(t)),
            color);

        orbImage.rectTransform.anchoredPosition = ScreenToLocalPosition(endScreen);

        while (trailGhosts.Count > 0)
        {
            UpdateTrailGhosts(Time.unscaledDeltaTime);
            yield return null;
        }

        Destroy(gameObject);
    }

    private IEnumerator AnimateSegment(
        float segmentDuration,
        System.Func<float, Vector2> positionEvaluator,
        Color color)
    {
        float elapsed = 0f;
        float safeSegmentDuration = Mathf.Max(0.01f, segmentDuration);

        while (elapsed < safeSegmentDuration)
        {
            float deltaTime = Time.unscaledDeltaTime;
            elapsed += deltaTime;
            float t = Mathf.Clamp01(elapsed / safeSegmentDuration);
            Vector2 screenPosition = positionEvaluator(t);

            orbImage.rectTransform.anchoredPosition = ScreenToLocalPosition(screenPosition);
            trailTimer += deltaTime;
            SpawnTrailGhosts(color, ref trailTimer);
            UpdateTrailGhosts(deltaTime);
            yield return null;
        }
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

    private static float EaseInCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t;
    }

}
