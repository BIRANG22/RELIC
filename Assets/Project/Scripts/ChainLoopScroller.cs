using UnityEngine;

/// <summary>
/// Chain 마스크 안의 두 RectTransform을 왼쪽으로 끊김 없이 반복 이동시킵니다.
/// Line은 X=0, Line2는 X=loopDistance에서 시작하며,
/// 두 라인의 간격을 항상 loopDistance로 유지합니다.
/// </summary>
public class ChainLoopScroller : MonoBehaviour
{
    [Header("움직일 라인")]
    [SerializeField] private RectTransform line;
    [SerializeField] private RectTransform line2;

    [Header("이동 설정")]
    [Tooltip("초당 왼쪽으로 이동하는 거리입니다.")]
    [SerializeField, Min(0f)] private float moveSpeed = 20f;

    [Tooltip("두 라인 사이의 반복 거리입니다. 기본값은 1500입니다.")]
    [SerializeField, Min(0.01f)] private float loopDistance = 1500f;

    [Tooltip("Unity 창 전환이나 에디터 일시 정지 후 한 프레임에 과도하게 이동하지 않도록 사용할 최대 델타 시간입니다.")]
    [SerializeField, Min(0.001f)] private float maxFrameDeltaTime = 0.05f;

    [Tooltip("활성화될 때 Line/Line2 위치를 각각 0, loopDistance로 초기화합니다.")]
    [SerializeField] private bool resetPositionOnEnable = true;

    private float scrollOffset;

    private void Awake()
    {
        ResolveLines();
    }

    private void OnEnable()
    {
        ResolveLines();

        if (resetPositionOnEnable)
        {
            ResetPositions();
        }
        else
        {
            SyncOffsetFromCurrentPosition();
            ApplyPositions();
        }
    }

    private void Update()
    {
        if (line == null || line2 == null || moveSpeed <= 0f || loopDistance <= 0f)
            return;

        float deltaTime = Mathf.Min(Time.unscaledDeltaTime, maxFrameDeltaTime);
        scrollOffset = Mathf.Repeat(scrollOffset + moveSpeed * deltaTime, loopDistance);

        ApplyPositions();
    }

    [ContextMenu("Reset Positions")]
    public void ResetPositions()
    {
        if (line == null || line2 == null)
            return;

        scrollOffset = 0f;
        ApplyPositions();
    }

    private void ApplyPositions()
    {
        float firstX = -scrollOffset;
        SetAnchoredX(line, firstX);
        SetAnchoredX(line2, firstX + loopDistance);
    }

    private void SyncOffsetFromCurrentPosition()
    {
        if (line == null || loopDistance <= 0f)
        {
            scrollOffset = 0f;
            return;
        }

        scrollOffset = Mathf.Repeat(-line.anchoredPosition.x, loopDistance);
    }

    private static void SetAnchoredX(RectTransform target, float x)
    {
        Vector2 position = target.anchoredPosition;
        position.x = x;
        target.anchoredPosition = position;
    }

    private void ResolveLines()
    {
        if (line == null)
            line = transform.Find("Line") as RectTransform;

        if (line2 == null)
            line2 = transform.Find("Line2") as RectTransform;
    }
}
