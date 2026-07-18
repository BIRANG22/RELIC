using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera))]
public class HorizontalHubCameraDrag : MonoBehaviour
{
    [Header("스냅 위치")]
    [Tooltip("왼쪽부터 농장, 중앙, 공장 순서로 등록")]
    [SerializeField] private Transform[] snapPoints;

    [Tooltip("게임 시작 시 위치. 농장 0, 중앙 1, 공장 2")]
    [SerializeField] private int startIndex = 1;

    [Header("드래그")]
    [SerializeField] private float dragSpeed = 1f;

    [Tooltip("체크하면 마우스를 움직이는 방향과 반대로 카메라가 이동")]
    [SerializeField] private bool invertDrag = true;

    [Tooltip("UI 위에서 클릭했을 때 카메라 드래그를 막음")]
    [SerializeField] private bool blockDragOverUI = true;

    [Header("자동 스냅")]
    [SerializeField] private float snapSmoothTime = 0.25f;
    [SerializeField] private float snapStopDistance = 0.01f;

    private Camera targetCamera;

    private bool isDragging;
    private Vector3 previousMousePosition;

    private float targetX;
    private float snapVelocity;

    private int currentIndex;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
    }

    private void Start()
    {
        if (snapPoints == null || snapPoints.Length == 0)
            return;

        currentIndex = Mathf.Clamp(startIndex, 0, snapPoints.Length - 1);
        targetX = snapPoints[currentIndex].position.x;

        SetCameraX(targetX);
    }

    private void OnDisable()
    {
        isDragging = false;
        snapVelocity = 0f;
    }

    private void Update()
    {
        if (snapPoints == null || snapPoints.Length == 0)
            return;

        HandleMouseInput();

        if (!isDragging)
            UpdateSnapMovement();
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (blockDragOverUI &&
                EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            isDragging = true;
            snapVelocity = 0f;
            previousMousePosition = Input.mousePosition;
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            Vector3 currentMousePosition = Input.mousePosition;
            float mouseDeltaX = currentMousePosition.x - previousMousePosition.x;

            // 화면 픽셀 이동량을 월드 좌표 이동량으로 변환
            float worldUnitsPerPixel =
                (targetCamera.orthographicSize * 2f) / Screen.height;

            float moveAmount = mouseDeltaX * worldUnitsPerPixel * dragSpeed;

            if (invertDrag)
                moveAmount *= -1f;

            float newX = transform.position.x + moveAmount;
            newX = Mathf.Clamp(newX, GetMinimumX(), GetMaximumX());

            SetCameraX(newX);

            previousMousePosition = currentMousePosition;
        }

        if (isDragging && Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            SelectNearestSnapPoint();
        }
    }

    private void SelectNearestSnapPoint()
    {
        float currentX = transform.position.x;
        float nearestDistance = float.MaxValue;
        int nearestIndex = 0;

        for (int i = 0; i < snapPoints.Length; i++)
        {
            if (snapPoints[i] == null)
                continue;

            float distance = Mathf.Abs(currentX - snapPoints[i].position.x);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        currentIndex = nearestIndex;
        targetX = snapPoints[currentIndex].position.x;
    }

    private void UpdateSnapMovement()
    {
        float currentX = transform.position.x;

        float newX = Mathf.SmoothDamp(
            currentX,
            targetX,
            ref snapVelocity,
            snapSmoothTime
        );

        if (Mathf.Abs(newX - targetX) <= snapStopDistance)
        {
            newX = targetX;
            snapVelocity = 0f;
        }

        SetCameraX(newX);
    }

    private void SetCameraX(float x)
    {
        Vector3 position = transform.position;
        position.x = x;
        transform.position = position;
    }

    private float GetMinimumX()
    {
        float minimum = float.MaxValue;

        foreach (Transform point in snapPoints)
        {
            if (point != null)
                minimum = Mathf.Min(minimum, point.position.x);
        }

        return minimum;
    }

    private float GetMaximumX()
    {
        float maximum = float.MinValue;

        foreach (Transform point in snapPoints)
        {
            if (point != null)
                maximum = Mathf.Max(maximum, point.position.x);
        }

        return maximum;
    }

    public void MoveToArea(int index)
    {
        if (snapPoints == null || snapPoints.Length == 0)
            return;

        currentIndex = Mathf.Clamp(index, 0, snapPoints.Length - 1);
        targetX = snapPoints[currentIndex].position.x;
        isDragging = false;
    }
}