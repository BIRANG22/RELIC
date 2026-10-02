using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum BattleUiCornerFeedbackStyle
{
    FourCorners,
    DiagonalStamp,
    RadialBurst
}

[RequireComponent(typeof(CanvasRenderer))]
public sealed class BattleUiCornerFeedback : MaskableGraphic
{
    [Header("Shape")]
    [SerializeField] private BattleUiCornerFeedbackStyle style;
    [SerializeField, Min(0f)] private float padding = 8f;
    [SerializeField, Min(1f)] private float lineThickness = 2f;
    [SerializeField, Range(0.1f, 0.8f)] private float cornerLengthRatio = 0.35f;
    [SerializeField, Range(0f, 1f)] private float glowAlpha = 0.16f;

    [Header("Radial Burst")]
    [SerializeField, Range(4, 32)] private int rayCount = 16;
    [SerializeField, Min(0f)] private float rayStartOffset = 4f;
    [SerializeField, Min(1f)] private float rayMinLength = 18f;
    [SerializeField, Min(1f)] private float rayMaxLength = 48f;
    [SerializeField, Min(0.5f)] private float rayWidth = 3f;
    [SerializeField, Range(0f, 1f)] private float rayOuterAlpha;

    [Header("Animation")]
    [SerializeField, Min(0.01f)] private float duration = 0.2f;
    [SerializeField, Min(0.01f)] private float startScale = 0.88f;
    [SerializeField, Min(0.01f)] private float endScale = 1.15f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private bool destroyOnDisable;

    public static void Spawn(BattleUiCornerFeedback prefab, RectTransform target)
    {
        if (prefab == null || target == null)
            return;

        // 타임라인 아이콘은 예약 갱신 중 잠깐 비활성화될 수 있습니다.
        // 그 순간 기존 피드백 코루틴이 중단되면 오브젝트가 남을 수 있으므로,
        // 같은 대상에 남아 있는 이전 피드백을 먼저 정리합니다.
        BattleUiCornerFeedback[] existingFeedbacks =
            target.GetComponentsInChildren<BattleUiCornerFeedback>(true);
        for (int i = 0; i < existingFeedbacks.Length; i++)
        {
            BattleUiCornerFeedback existing = existingFeedbacks[i];
            if (existing != null && existing.transform.parent == target)
                Destroy(existing.gameObject);
        }

        BattleUiCornerFeedback instance = Instantiate(prefab, target, false);
        instance.destroyOnDisable = true;
        RectTransform rect = instance.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.SetAsLastSibling();
        instance.raycastTarget = false;
        instance.StartCoroutine(instance.PlayRoutine());
    }

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
        canvasRenderer.SetAlpha(0f);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        // 부모 아이콘이 비활성화되면 코루틴이 끝까지 진행되지 못할 수 있습니다.
        // Spawn으로 생성된 일회성 피드백은 비활성화되는 즉시 제거해 잔상을 남기지 않습니다.
        if (destroyOnDisable && Application.isPlaying)
            Destroy(gameObject);
    }

    private IEnumerator PlayRoutine()
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);
        canvasRenderer.SetAlpha(1f);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float scaleT = scaleCurve != null ? scaleCurve.Evaluate(t) : t;
            rectTransform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, scaleT);
            canvasRenderer.SetAlpha(1f - t);
            yield return null;
        }

        Destroy(gameObject);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        rect.xMin -= padding;
        rect.xMax += padding;
        rect.yMin -= padding;
        rect.yMax += padding;

        if (style == BattleUiCornerFeedbackStyle.RadialBurst)
        {
            AddRadialBurst(vh, rect);
            return;
        }

        AddQuad(vh, rect, new Color(color.r, color.g, color.b, color.a * glowAlpha));

        float horizontalLength = rect.width * cornerLengthRatio;
        float verticalLength = rect.height * cornerLengthRatio;
        Color lineColor = color;

        if (style == BattleUiCornerFeedbackStyle.FourCorners)
        {
            AddCorner(vh, rect.xMin, rect.yMax, 1f, -1f, horizontalLength, verticalLength, lineColor);
            AddCorner(vh, rect.xMax, rect.yMax, -1f, -1f, horizontalLength, verticalLength, lineColor);
            AddCorner(vh, rect.xMin, rect.yMin, 1f, 1f, horizontalLength, verticalLength, lineColor);
            AddCorner(vh, rect.xMax, rect.yMin, -1f, 1f, horizontalLength, verticalLength, lineColor);
        }
        else
        {
            AddCorner(vh, rect.xMin, rect.yMin, 1f, 1f, horizontalLength, verticalLength, lineColor);
            AddCorner(vh, rect.xMax, rect.yMax, -1f, -1f, horizontalLength, verticalLength, lineColor);
        }
    }

    private void AddRadialBurst(VertexHelper vh, Rect rect)
    {
        Vector2 center = rect.center;
        float halfWidth = rect.width * 0.5f;
        float halfHeight = rect.height * 0.5f;
        Color transparent = new Color(color.r, color.g, color.b, 0f);
        Color glow = new Color(color.r, color.g, color.b, color.a * glowAlpha);
        AddGradientQuad(vh, rect, glow, transparent);

        int safeRayCount = Mathf.Max(4, rayCount);
        float minLength = Mathf.Min(rayMinLength, rayMaxLength);
        float maxLength = Mathf.Max(rayMinLength, rayMaxLength);

        for (int i = 0; i < safeRayCount; i++)
        {
            float angle = (360f / safeRayCount) * i + ((i % 3) - 1) * 2.5f;
            float radians = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            float boundaryDistance = Mathf.Min(
                Mathf.Abs(direction.x) > 0.0001f ? halfWidth / Mathf.Abs(direction.x) : float.MaxValue,
                Mathf.Abs(direction.y) > 0.0001f ? halfHeight / Mathf.Abs(direction.y) : float.MaxValue);
            // 오른쪽 위-왼쪽 아래 축은 평균적으로 길고, 반대 대각선으로 갈수록 짧아진다.
            // 인덱스 기반 편차를 더해 매번 동일하게 재생되면서도 길이가 균등해 보이지 않게 한다.
            float diagonalWeight = Mathf.Pow(Mathf.Abs(Vector2.Dot(direction, new Vector2(0.7071068f, 0.7071068f))), 1.25f);
            float irregularity = Mathf.Repeat(Mathf.Sin((i + 1) * 12.9898f) * 43758.5453f, 1f);
            float variation = Mathf.Lerp(0.84f, 1.12f, irregularity);
            float length = Mathf.Lerp(minLength, maxLength, diagonalWeight) * variation;
            float width = rayWidth * (i % 4 == 0 ? 1.35f : 1f);
            Vector2 start = center + direction * (boundaryDistance + rayStartOffset);
            Vector2 end = start + direction * length;
            AddTaperedRay(vh, start, end, perpendicular, width);
        }
    }

    private void AddTaperedRay(VertexHelper vh, Vector2 start, Vector2 end, Vector2 perpendicular, float width)
    {
        int startIndex = vh.currentVertCount;
        Color inner = color;
        Color outer = new Color(color.r, color.g, color.b, color.a * rayOuterAlpha);
        Vector2 innerHalfWidth = perpendicular * (width * 0.5f);
        Vector2 outerHalfWidth = perpendicular * (width * 0.08f);

        vh.AddVert(start - innerHalfWidth, inner, Vector2.zero);
        vh.AddVert(start + innerHalfWidth, inner, Vector2.up);
        vh.AddVert(end + outerHalfWidth, outer, Vector2.one);
        vh.AddVert(end - outerHalfWidth, outer, Vector2.right);
        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
    }

    private static void AddGradientQuad(VertexHelper vh, Rect rect, Color centerColor, Color edgeColor)
    {
        int centerIndex = vh.currentVertCount;
        Vector2 center = rect.center;
        vh.AddVert(center, centerColor, new Vector2(0.5f, 0.5f));
        vh.AddVert(new Vector2(rect.xMin, rect.yMin), edgeColor, Vector2.zero);
        vh.AddVert(new Vector2(rect.xMin, rect.yMax), edgeColor, Vector2.up);
        vh.AddVert(new Vector2(rect.xMax, rect.yMax), edgeColor, Vector2.one);
        vh.AddVert(new Vector2(rect.xMax, rect.yMin), edgeColor, Vector2.right);
        vh.AddTriangle(centerIndex, centerIndex + 1, centerIndex + 2);
        vh.AddTriangle(centerIndex, centerIndex + 2, centerIndex + 3);
        vh.AddTriangle(centerIndex, centerIndex + 3, centerIndex + 4);
        vh.AddTriangle(centerIndex, centerIndex + 4, centerIndex + 1);
    }

    private void AddCorner(
        VertexHelper vh,
        float x,
        float y,
        float horizontalDirection,
        float verticalDirection,
        float horizontalLength,
        float verticalLength,
        Color vertexColor)
    {
        float half = lineThickness * 0.5f;
        AddQuad(vh, new Rect(
            Mathf.Min(x, x + horizontalDirection * horizontalLength),
            y - half,
            horizontalLength,
            lineThickness), vertexColor);
        AddQuad(vh, new Rect(
            x - half,
            Mathf.Min(y, y + verticalDirection * verticalLength),
            lineThickness,
            verticalLength), vertexColor);
    }

    private static void AddQuad(VertexHelper vh, Rect rect, Color vertexColor)
    {
        int start = vh.currentVertCount;
        vh.AddVert(new Vector3(rect.xMin, rect.yMin), vertexColor, Vector2.zero);
        vh.AddVert(new Vector3(rect.xMin, rect.yMax), vertexColor, Vector2.up);
        vh.AddVert(new Vector3(rect.xMax, rect.yMax), vertexColor, Vector2.one);
        vh.AddVert(new Vector3(rect.xMax, rect.yMin), vertexColor, Vector2.right);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }
}
