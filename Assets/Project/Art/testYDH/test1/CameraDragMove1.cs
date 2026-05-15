using UnityEngine;

public class CameraDragMove1 : MonoBehaviour
{
    [Header("Drag Movement")]
    [SerializeField] private float dragPower = 0.01f;
    [SerializeField] private float maxDragOffsetX = 2f;
    [SerializeField] private float maxDragOffsetY = 1.2f;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSensitivity = 5f; // 휠 민감도
    [SerializeField] private float minZoomZ = -15f;      // 가장 먼 거리
    [SerializeField] private float maxZoomZ = -5f;       // 가장 가까운 거리

    [Header("Smooth")]
    [SerializeField] private float smoothSpeed = 8f;

    private Vector3 startPosition;
    private Vector3 dragStartMousePosition;
    private Vector3 dragStartOffset;
    private Vector3 dragOffset;

    // 현재 목표로 하는 Z값을 저장하는 변수
    private float targetZoomZ;

    private bool isDragging;

    private void Start()
    {
        startPosition = transform.position;
        // 현재 카메라의 Z값을 초기 목표값으로 설정
        targetZoomZ = transform.position.z;
    }

    private void LateUpdate()
    {
        HandleDragMove();
        HandleZoom();

        // X, Y는 드래그 오프셋 적용 / Z는 휠로 계산된 targetZoomZ 적용
        Vector3 targetPosition = new Vector3(
            startPosition.x + dragOffset.x,
            startPosition.y + dragOffset.y,
            targetZoomZ
        );

        // 최종 위치로 부드럽게 이동
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * smoothSpeed
        );
    }

    private void HandleDragMove()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            dragStartMousePosition = Input.mousePosition;
            dragStartOffset = dragOffset;
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector3 dragDelta = dragStartMousePosition - Input.mousePosition;

            float offsetX = dragDelta.x * dragPower;
            float offsetY = dragDelta.y * dragPower;

            dragOffset = dragStartOffset + new Vector3(offsetX, offsetY, 0f);
            dragOffset.x = Mathf.Clamp(dragOffset.x, -maxDragOffsetX, maxDragOffsetX);
            dragOffset.y = Mathf.Clamp(dragOffset.y, -maxDragOffsetY, maxDragOffsetY);
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    private void HandleZoom()
    {
        // 휠 입력 (위로:+값, 아래로:-값)
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            // 현재 목표 Z값에 입력값을 누적시킴 (이래야 중간에 멈춤 가능)
            targetZoomZ += scrollInput * zoomSensitivity;

            // 정해진 범위 안에서만 움직이도록 제한
            targetZoomZ = Mathf.Clamp(targetZoomZ, minZoomZ, maxZoomZ);
        }
    }
}