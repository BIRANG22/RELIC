using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CursorClickColor : MonoBehaviour
{
    [Header("커서 이미지")]
    [SerializeField] private Texture2D normalCursor;
    [SerializeField] private Texture2D hoverCursor;
    [SerializeField] private Texture2D clickedCursor;

    [Header("커서 클릭 위치")]
    [SerializeField] private Vector2 hotSpot = Vector2.zero;

    [Header("클릭 커서 최소 유지 시간")]
    [SerializeField] private float clickHoldTime = 0.15f;

    [Header("호버 감지")]
    [Tooltip("체크하면 EventSystem의 UI 버튼/Selectable 위에서 호버 커서를 사용합니다.")]
    [SerializeField] private bool detectUIHover = true;

    [Tooltip("체크하면 2D Collider 위에서 호버 커서를 사용합니다.")]
    [SerializeField] private bool detect2DColliderHover = true;

    [Tooltip("체크하면 3D Collider 위에서 호버 커서를 사용합니다.")]
    [SerializeField] private bool detect3DColliderHover = true;

    [Tooltip("월드 Collider 호버 감지에 사용할 레이어입니다.")]
    [SerializeField] private LayerMask hoverLayerMask = ~0;

    [Tooltip("3D Collider 호버 감지 최대 거리입니다.")]
    [SerializeField] private float hoverRayDistance = 1000f;

    private static CursorClickColor instance;

    private Coroutine restoreCoroutine;
    private bool isMousePressed;
    private CursorState currentState = CursorState.None;

    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();

    private enum CursorState
    {
        None,
        Normal,
        Hover,
        Clicked
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        RefreshCursorForCurrentState();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isMousePressed = true;

            if (restoreCoroutine != null)
            {
                StopCoroutine(restoreCoroutine);
                restoreCoroutine = null;
            }

            SetClickedCursor();
        }

        if (Input.GetMouseButtonUp(0))
        {
            isMousePressed = false;

            if (restoreCoroutine != null)
            {
                StopCoroutine(restoreCoroutine);
            }

            restoreCoroutine = StartCoroutine(RestoreCursorAfterDelay());
        }

        // 클릭 중이거나 클릭 커서 유지 시간이 남아 있으면 호버 상태로 덮어쓰지 않습니다.
        if (isMousePressed || restoreCoroutine != null)
        {
            return;
        }

        RefreshCursorForCurrentState();
    }

    private IEnumerator RestoreCursorAfterDelay()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, clickHoldTime));

        restoreCoroutine = null;

        if (!isMousePressed)
        {
            RefreshCursorForCurrentState();
        }
    }

    private void RefreshCursorForCurrentState()
    {
        if (isMousePressed)
        {
            SetClickedCursor();
            return;
        }

        if (IsPointerOverHoverTarget())
        {
            SetHoverCursor();
        }
        else
        {
            SetNormalCursor();
        }
    }

    private bool IsPointerOverHoverTarget()
    {
        if (detectUIHover && IsPointerOverSelectableUI())
        {
            return true;
        }

        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            return false;
        }

        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);

        if (detect2DColliderHover)
        {
            RaycastHit2D hit2D = Physics2D.GetRayIntersection(
                ray,
                Mathf.Infinity,
                hoverLayerMask.value);

            if (hit2D.collider != null)
            {
                return true;
            }
        }

        if (detect3DColliderHover && Physics.Raycast(
                ray,
                out _,
                Mathf.Max(0f, hoverRayDistance),
                hoverLayerMask.value,
                QueryTriggerInteraction.Collide))
        {
            return true;
        }

        return false;
    }

    private bool IsPointerOverSelectableUI()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, uiRaycastResults);

        for (int i = 0; i < uiRaycastResults.Count; i++)
        {
            GameObject hitObject = uiRaycastResults[i].gameObject;
            if (hitObject == null)
            {
                continue;
            }

            Selectable selectable = hitObject.GetComponentInParent<Selectable>();
            if (selectable != null && selectable.IsInteractable())
            {
                return true;
            }
        }

        return false;
    }

    private void SetNormalCursor()
    {
        SetCursor(CursorState.Normal, normalCursor);
    }

    private void SetHoverCursor()
    {
        SetCursor(CursorState.Hover, hoverCursor != null ? hoverCursor : normalCursor);
    }

    private void SetClickedCursor()
    {
        SetCursor(CursorState.Clicked, clickedCursor);
    }

    private void SetCursor(CursorState state, Texture2D texture)
    {
        if (currentState == state)
        {
            return;
        }

        currentState = state;
        Cursor.SetCursor(texture, hotSpot, CursorMode.Auto);
    }
}
