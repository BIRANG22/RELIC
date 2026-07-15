using UnityEngine;

public class UIVerticalFloat : MonoBehaviour
{
    public enum MoveDirection
    {
        Vertical,
        Horizontal
    }

    [Header("Target")]
    [Tooltip("움직일 UI 또는 일반 스프라이트 오브젝트입니다. 비워두면 이 스크립트가 붙은 오브젝트를 사용합니다.")]
    [SerializeField] private Transform target;

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

    [Header("Sprite Option")]
    [Tooltip("일반 스프라이트의 이동 거리에 별도의 배율을 적용합니다. UI와 월드 좌표 크기가 다를 때 조절하세요.")]
    [SerializeField] private float spriteMoveMultiplier = 0.01f;

    private RectTransform targetRectTransform;

    private Vector2 startAnchoredPosition;
    private Vector3 startLocalPosition;

    private bool isUITarget;


    private void Awake()
    {
        if (target == null)
        {
            target = transform;
        }

        InitializeTarget();
        CacheStartPosition();
    }


    private void OnEnable()
    {
        if (target == null)
        {
            target = transform;
        }

        InitializeTarget();
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

        float offset = Mathf.Sin(time * moveSpeed) * moveAmount;

        if (isUITarget)
        {
            MoveUI(offset);
        }
        else
        {
            MoveSprite(offset);
        }
    }


    private void OnDisable()
    {
        if (!restorePositionOnDisable || target == null)
        {
            return;
        }

        RestoreStartPosition();
    }


    /// <summary>
    /// 대상이 UI인지 일반 Transform인지 확인합니다.
    /// </summary>
    private void InitializeTarget()
    {
        if (target == null)
        {
            targetRectTransform = null;
            isUITarget = false;
            return;
        }

        targetRectTransform = target as RectTransform;
        isUITarget = targetRectTransform != null;
    }


    /// <summary>
    /// UI 오브젝트를 움직입니다.
    /// </summary>
    private void MoveUI(float offset)
    {
        if (targetRectTransform == null)
        {
            return;
        }

        Vector2 moveOffset;

        if (moveDirection == MoveDirection.Vertical)
        {
            moveOffset = new Vector2(0f, offset);
        }
        else
        {
            moveOffset = new Vector2(offset, 0f);
        }

        targetRectTransform.anchoredPosition =
            startAnchoredPosition + moveOffset;
    }


    /// <summary>
    /// 일반 스프라이트 또는 월드 오브젝트를 움직입니다.
    /// </summary>
    private void MoveSprite(float offset)
    {
        float worldOffset = offset * spriteMoveMultiplier;

        Vector3 moveOffset;

        if (moveDirection == MoveDirection.Vertical)
        {
            moveOffset = new Vector3(0f, worldOffset, 0f);
        }
        else
        {
            moveOffset = new Vector3(worldOffset, 0f, 0f);
        }

        target.localPosition =
            startLocalPosition + moveOffset;
    }


    /// <summary>
    /// 현재 위치를 움직임의 기준 위치로 저장합니다.
    /// </summary>
    public void CacheStartPosition()
    {
        if (target == null)
        {
            return;
        }

        if (isUITarget && targetRectTransform != null)
        {
            startAnchoredPosition =
                targetRectTransform.anchoredPosition;
        }
        else
        {
            startLocalPosition =
                target.localPosition;
        }
    }


    /// <summary>
    /// 저장된 기준 위치로 되돌립니다.
    /// </summary>
    public void RestoreStartPosition()
    {
        if (target == null)
        {
            return;
        }

        if (isUITarget && targetRectTransform != null)
        {
            targetRectTransform.anchoredPosition =
                startAnchoredPosition;
        }
        else
        {
            target.localPosition =
                startLocalPosition;
        }
    }


    /// <summary>
    /// 외부에서 움직일 대상을 변경할 때 사용합니다.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        InitializeTarget();
        CacheStartPosition();
    }
}