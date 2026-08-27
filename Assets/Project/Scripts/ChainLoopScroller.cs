using UnityEngine;

/// <summary>
/// 세 개의 체인 라인을 왼쪽으로 끊김 없이 반복 이동시킵니다.
/// 기본 배치는 -500, 1000, 2500이며 각 라인의 길이와 간격은 1500입니다.
/// 왼쪽 라인이 -2000에 도달하면 2500으로 이동해 세 라인이 항상 이어지도록 유지합니다.
/// </summary>
public class ChainLoopScroller : MonoBehaviour
{
    [Header("움직일 라인")]
    [SerializeField] private RectTransform line1;
    [SerializeField] private RectTransform line2;
    [SerializeField] private RectTransform line3;

    [Header("이동 설정")]
    [Tooltip("초당 왼쪽으로 이동하는 거리입니다.")]
    [SerializeField, Min(0f)] private float moveSpeed = 20f;

    [Tooltip("체인 라인 하나의 길이이자 라인 사이의 간격입니다.")]
    [SerializeField, Min(0.01f)] private float segmentLength = 1500f;

    [Tooltip("첫 번째 라인의 시작 X 위치입니다.")]
    [SerializeField] private float firstStartX = -500f;

    [Tooltip("라인이 이 X 위치에 도달하면 가장 오른쪽으로 순환합니다.")]
    [SerializeField] private float wrapBoundaryX = -2000f;

    [Tooltip("Unity 창 전환이나 에디터 일시 정지 후 한 프레임에 과도하게 이동하지 않도록 사용할 최대 델타 시간입니다.")]
    [SerializeField, Min(0.001f)] private float maxFrameDeltaTime = 0.05f;

    private const int LineCount = 3;

    private void Awake()
    {
        ResolveLines();
    }

    private void OnEnable()
    {
        // 턴 전환이나 TimelineBar 교체 중에는 체인 위치를 초기화하지 않습니다.
        // 전투방 최초 진입 초기화는 BattleTimelineController에서 명시적으로 ResetPositions를 호출합니다.
        ResolveLines();
    }

    private void Update()
    {
        if (!HasAllLines() || moveSpeed <= 0f || segmentLength <= 0f)
            return;

        float deltaTime = Mathf.Min(Time.unscaledDeltaTime, maxFrameDeltaTime);
        float moveAmount = moveSpeed * deltaTime;

        MoveAllLines(moveAmount);
    }

    [ContextMenu("Reset Positions")]
    public void ResetPositions()
    {
        ResolveLines();

        if (!HasAllLines())
            return;

        SetAnchoredX(line1, firstStartX);
        SetAnchoredX(line2, firstStartX + segmentLength);
        SetAnchoredX(line3, firstStartX + segmentLength * 2f);
    }

    private void MoveAllLines(float moveAmount)
    {
        float totalSpan = segmentLength * LineCount;

        MoveAndWrap(line1, moveAmount, totalSpan);
        MoveAndWrap(line2, moveAmount, totalSpan);
        MoveAndWrap(line3, moveAmount, totalSpan);
    }

    private void MoveAndWrap(RectTransform target, float moveAmount, float totalSpan)
    {
        if (target == null)
            return;

        float x = target.anchoredPosition.x - moveAmount;

        // 한 프레임의 이동량이 커져도 항상 올바른 순환 위치로 복구합니다.
        while (x <= wrapBoundaryX)
            x += totalSpan;

        SetAnchoredX(target, x);
    }

    public Vector3[] CaptureWorldPositions()
    {
        ResolveLines();

        if (!HasAllLines())
            return null;

        return new[]
        {
            line1.position,
            line2.position,
            line3.position
        };
    }

    public void RestoreWorldPositions(Vector3[] worldPositions)
    {
        ResolveLines();

        if (!HasAllLines() || worldPositions == null || worldPositions.Length < LineCount)
            return;

        line1.position = worldPositions[0];
        line2.position = worldPositions[1];
        line3.position = worldPositions[2];
    }

    private bool HasAllLines()
    {
        return line1 != null && line2 != null && line3 != null;
    }

    private static void SetAnchoredX(RectTransform target, float x)
    {
        Vector2 position = target.anchoredPosition;
        position.x = x;
        target.anchoredPosition = position;
    }

    private void ResolveLines()
    {
        if (line1 == null)
            line1 = FindRectTransformByName("chain_line1");

        if (line2 == null)
            line2 = FindRectTransformByName("chain_line2");

        if (line3 == null)
            line3 = FindRectTransformByName("chain_line3");
    }

    private RectTransform FindRectTransformByName(string targetName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && child.name == targetName)
                return child as RectTransform;
        }

        return null;
    }
}
