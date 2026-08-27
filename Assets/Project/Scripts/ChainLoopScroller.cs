using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 지도 UI의 두 체인 라인을 왼쪽으로 끊김 없이 반복 이동시킵니다.
/// 기본 배치가 -500, 1000이고 한 라인의 길이가 1500이라면,
/// 왼쪽 라인이 -2000에 도달할 때 1000으로 순환하여 두 라인이 계속 이어집니다.
/// </summary>
public class ChainLoopScroller : MonoBehaviour
{
    [Header("움직일 라인")]
    [FormerlySerializedAs("line1")]
    [SerializeField] private RectTransform line;
    [SerializeField] private RectTransform line2;

    [Header("이동 설정")]
    [Tooltip("초당 왼쪽으로 이동하는 거리입니다.")]
    [SerializeField, Min(0f)] private float moveSpeed = 10f;

    [Tooltip("체인 라인 하나의 길이이자 두 라인 사이의 간격입니다.")]
    [SerializeField, Min(0.01f)] private float segmentLength = 1500f;

    [Tooltip("첫 번째 라인의 시작 X 위치입니다.")]
    [SerializeField] private float firstStartX = -500f;

    [Tooltip("라인이 이 X 위치에 도달하면 오른쪽 끝으로 순환합니다.")]
    [SerializeField] private float wrapBoundaryX = -2000f;

    [Tooltip("Unity 창 전환이나 에디터 일시 정지 후 한 프레임에 과도하게 이동하지 않도록 제한하는 최대 델타 시간입니다.")]
    [SerializeField, Min(0.001f)] private float maxFrameDeltaTime = 0.05f;

    private const int LineCount = 2;

    private void Update()
    {
        if (!HasAllLines() || moveSpeed <= 0f || segmentLength <= 0f)
            return;

        float deltaTime = Mathf.Min(Time.unscaledDeltaTime, maxFrameDeltaTime);
        float moveAmount = moveSpeed * deltaTime;

        MoveAndWrap(line, moveAmount);
        MoveAndWrap(line2, moveAmount);
    }

    [ContextMenu("Reset Positions")]
    public void ResetPositions()
    {
        if (!HasAllLines())
            return;

        SetAnchoredX(line, firstStartX);
        SetAnchoredX(line2, firstStartX + segmentLength);
    }

    private void MoveAndWrap(RectTransform target, float moveAmount)
    {
        float x = target.anchoredPosition.x - moveAmount;
        float totalSpan = segmentLength * LineCount;

        // 창 전환 등으로 델타가 커져도 올바른 순환 위치로 복구합니다.
        while (x <= wrapBoundaryX)
            x += totalSpan;

        SetAnchoredX(target, x);
    }

    public Vector3[] CaptureWorldPositions()
    {
        if (!HasAllLines())
            return null;

        return new[]
        {
            line.position,
            line2.position
        };
    }

    public void RestoreWorldPositions(Vector3[] worldPositions)
    {
        if (!HasAllLines() || worldPositions == null || worldPositions.Length < LineCount)
            return;

        line.position = worldPositions[0];
        line2.position = worldPositions[1];
    }

    private bool HasAllLines()
    {
        return line != null && line2 != null;
    }

    private static void SetAnchoredX(RectTransform target, float x)
    {
        Vector2 position = target.anchoredPosition;
        position.x = x;
        target.anchoredPosition = position;
    }
}
