using UnityEngine;

public class CursorClickColor : MonoBehaviour
{
    [Header("커서 이미지")]
    [SerializeField] private Texture2D normalCursor;
    [SerializeField] private Texture2D clickedCursor;

    [Header("커서 클릭 위치")]
    [SerializeField] private Vector2 hotSpot = Vector2.zero;

    private static CursorClickColor instance;

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
            SetClickedCursor();
        }

        if (Input.GetMouseButtonUp(0))
        {
            SetNormalCursor();
        }
    }

    private void SetNormalCursor()
    {
        Cursor.SetCursor(normalCursor, hotSpot, CursorMode.Auto);
    }

    private void SetClickedCursor()
    {
        Cursor.SetCursor(clickedCursor, hotSpot, CursorMode.Auto);
    }
}