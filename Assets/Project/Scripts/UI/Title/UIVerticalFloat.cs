using UnityEngine;

public class UIVerticalFloat : MonoBehaviour
{
    public enum MoveDirection
    {
        Vertical,
        Horizontal
    }

    [Header("Target")]
    [SerializeField] private RectTransform target;

    [Header("Move Direction")]
    [Tooltip("Vertical은 위아래, Horizontal은 좌우로 움직입니다.")]
    [SerializeField] private MoveDirection moveDirection = MoveDirection.Vertical;

    [Header("Float Option")]
    [Tooltip("이동하는 거리입니다.")]
    [SerializeField] private float moveAmount = 10f;

    [Tooltip("이동 속도입니다.")]
    [SerializeField] private float moveSpeed = 1.5f;

    [Tooltip("게임이 일시정지되어도 움직이게 합니다.")]
    [SerializeField] private bool useUnscaledTime = true;

    [Tooltip("오브젝트가 비활성화될 때 원래 위치로 돌아갑니다.")]
    [SerializeField] private bool restorePositionOnDisable = true;

    private Vector2 startAnchoredPosition;


    private void Awake()
    {
        if (target == null)
        {
            target = GetComponent<RectTransform>();
        }

        CacheStartPosition();
    }


    private void OnEnable()
    {
        CacheStartPosition();
    }


    private void Update()
    {
        if (target == null)
        {
            return;
        }

        float time = useUnscaledTime
            ? Time.unscaledTime
            : Time.time;

        float offset =
            Mathf.Sin(time * moveSpeed) * moveAmount;

        Vector2 moveOffset;

        if (moveDirection == MoveDirection.Vertical)
        {
            moveOffset = new Vector2(0f, offset);
        }
        else
        {
            moveOffset = new Vector2(offset, 0f);
        }

        target.anchoredPosition =
            startAnchoredPosition + moveOffset;
    }


    private void OnDisable()
    {
        if (!restorePositionOnDisable)
        {
            return;
        }

        if (target != null)
        {
            target.anchoredPosition =
                startAnchoredPosition;
        }
    }


    /// <summary>
    /// 현재 위치를 움직임의 기준 위치로 저장합니다.
    /// </summary>
    private void CacheStartPosition()
    {
        if (target == null)
        {
            return;
        }

        startAnchoredPosition =
            target.anchoredPosition;
    }
}