using System.Collections;
using UnityEngine;

public class BattleCameraController : MonoBehaviour
{
    public static BattleCameraController Instance { get; private set; }

    [SerializeField] private Camera targetCamera;

    [Header("Zoom")]
    [SerializeField] private float zoomSize = 4.2f;
    [SerializeField] private float zoomDuration = 0.18f;
    [SerializeField] private float returnDuration = 0.18f;
    [SerializeField] private Vector2 zoomOffset = new Vector2(0f, 0.35f);

    [Header("Drag")]
    [SerializeField] private bool enableMouseDrag = true;
    [SerializeField] private float dragSpeed = 1f;
    [SerializeField] private Vector2 minCameraPosition = new Vector2(-3f, -2f);
    [SerializeField] private Vector2 maxCameraPosition = new Vector2(3f, 2f);

    private float defaultSize;
    private Vector3 defaultPosition;
    private Coroutine routine;
    private Vector3 lastMouseWorldPosition;
    private bool isDragging;

    private void Awake()
    {
        Instance = this;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null)
        {
            defaultSize = targetCamera.orthographicSize;
            defaultPosition = targetCamera.transform.position;
        }
    }

    private void Update()
    {
        HandleMouseDrag();
    }

    public IEnumerator ZoomTo(Transform target)
    {
        if (targetCamera == null || target == null)
            yield break;

        if (routine != null)
            StopCoroutine(routine);

        Vector3 targetPos = target.position;
        targetPos.x += zoomOffset.x;
        targetPos.y += zoomOffset.y;
        targetPos.z = targetCamera.transform.position.z;

        routine = StartCoroutine(MoveCamera(targetPos, zoomSize, zoomDuration));
        yield return routine;
    }

    public IEnumerator ReturnDefault()
    {
        if (targetCamera == null)
            yield break;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(MoveCamera(defaultPosition, defaultSize, returnDuration));
        yield return routine;
    }

    private IEnumerator MoveCamera(Vector3 targetPos, float targetSize, float duration)
    {
        Vector3 startPos = targetCamera.transform.position;
        float startSize = targetCamera.orthographicSize;

        targetPos.z = startPos.z;

        if (duration <= 0f)
        {
            targetCamera.transform.position = ClampCameraPosition(targetPos);
            targetCamera.orthographicSize = targetSize;
            routine = null;
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            t = t * t * (3f - 2f * t);

            targetCamera.transform.position =
                ClampCameraPosition(Vector3.Lerp(startPos, targetPos, t));

            targetCamera.orthographicSize =
                Mathf.Lerp(startSize, targetSize, t);

            yield return null;
        }

        targetCamera.transform.position = ClampCameraPosition(targetPos);
        targetCamera.orthographicSize = targetSize;
        routine = null;
    }

    private void HandleMouseDrag()
    {
        if (!enableMouseDrag || targetCamera == null)
            return;

        if (routine != null)
            return;

        if (Input.GetMouseButtonDown(2) || Input.GetMouseButtonDown(1))
        {
            isDragging = true;
            lastMouseWorldPosition = GetMouseWorldPosition();
        }

        if (Input.GetMouseButtonUp(2) || Input.GetMouseButtonUp(1))
            isDragging = false;

        if (!isDragging)
            return;

        Vector3 currentMouseWorldPosition = GetMouseWorldPosition();
        Vector3 delta = lastMouseWorldPosition - currentMouseWorldPosition;

        targetCamera.transform.position =
            ClampCameraPosition(targetCamera.transform.position + delta * dragSpeed);

        lastMouseWorldPosition = GetMouseWorldPosition();
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouse = Input.mousePosition;
        mouse.z = Mathf.Abs(targetCamera.transform.position.z);
        return targetCamera.ScreenToWorldPoint(mouse);
    }

    private Vector3 ClampCameraPosition(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, minCameraPosition.x, maxCameraPosition.x);
        position.y = Mathf.Clamp(position.y, minCameraPosition.y, maxCameraPosition.y);
        return position;
    }
}