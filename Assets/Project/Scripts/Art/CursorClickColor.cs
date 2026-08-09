using UnityEngine;

public class CursorClickColor : MonoBehaviour
{
    [Header("커서 이미지")]
    [SerializeField] private Texture2D normalCursor;
    [SerializeField] private Texture2D clickedCursor;

    [Header("커서 클릭 위치")]
    [SerializeField] private Vector2 hotSpot = Vector2.zero;

    [Header("클릭 효과")]
    [SerializeField, Min(0f)] private float minimumClickDuration = 0.1f;

    private static CursorClickColor instance;

    private float clickStartTime;
    private bool isClickedCursorActive;
    private bool waitingForMinimumDuration;

    private void Awake()
    {
        // 이미 커서 매니저가 존재하면 중복 제거
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
        SetNormalCursor();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            clickStartTime = Time.unscaledTime;
            waitingForMinimumDuration = false;
            SetClickedCursor();
        }

        if (Input.GetMouseButtonUp(0))
        {
            float elapsed = Time.unscaledTime - clickStartTime;

            if (elapsed >= minimumClickDuration)
            {
                SetNormalCursor();
            }
            else
            {
                waitingForMinimumDuration = true;
            }
        }

        if (waitingForMinimumDuration &&
            isClickedCursorActive &&
            Time.unscaledTime - clickStartTime >= minimumClickDuration)
        {
            waitingForMinimumDuration = false;
            SetNormalCursor();
        }
    }

    private void SetNormalCursor()
    {
        Cursor.SetCursor(normalCursor, hotSpot, CursorMode.Auto);
        isClickedCursorActive = false;
        waitingForMinimumDuration = false;
    }

    private void SetClickedCursor()
    {
        Cursor.SetCursor(clickedCursor, hotSpot, CursorMode.Auto);
        isClickedCursorActive = true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        minimumClickDuration = Mathf.Max(0f, minimumClickDuration);
    }
#endif
}
