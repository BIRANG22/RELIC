using System.Collections;
using UnityEngine;

public class CursorClickColor : MonoBehaviour
{
    [Header("커서 이미지")]
    [SerializeField] private Texture2D normalCursor;
    [SerializeField] private Texture2D clickedCursor;

    [Header("커서 클릭 위치")]
    [SerializeField] private Vector2 hotSpot = Vector2.zero;

    [Header("클릭 커서 최소 유지 시간")]
    [SerializeField] private float clickHoldTime = 0.15f;

    private static CursorClickColor instance;

    private Coroutine restoreCoroutine;
    private bool isMousePressed;

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
        SetNormalCursor();
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
    }

    private IEnumerator RestoreCursorAfterDelay()
    {
        yield return new WaitForSecondsRealtime(clickHoldTime);

        if (!isMousePressed)
        {
            SetNormalCursor();
        }

        restoreCoroutine = null;
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