using UnityEngine;

public class CameraDragMove : MonoBehaviour
{
    [Header("Drag Movement")]
    [SerializeField] private float dragPower = 0.01f;
    [SerializeField] private float maxDragOffsetX = 2f;
    [SerializeField] private float maxDragOffsetY = 1.2f;

    [Header("Smooth")]
    [SerializeField] private float smoothSpeed = 8f;

    private Vector3 startPosition;

    private Vector3 dragStartMousePosition;
    private Vector3 dragStartOffset;
    private Vector3 dragOffset;

    private bool isDragging;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void LateUpdate()
    {
        HandleDragMove();

        Vector3 targetPosition = startPosition + dragOffset;

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
            // 방향 반전 적용
            // 마우스를 오른쪽으로 당기면 카메라는 왼쪽으로 이동
            // 마우스를 왼쪽으로 당기면 카메라는 오른쪽으로 이동
            Vector3 dragDelta = dragStartMousePosition - Input.mousePosition;

            float offsetX = dragDelta.x * dragPower;
            float offsetY = dragDelta.y * dragPower;

            dragOffset = dragStartOffset + new Vector3(offsetX, offsetY, 0f);

            dragOffset.x = Mathf.Clamp(
                dragOffset.x,
                -maxDragOffsetX,
                maxDragOffsetX
            );

            dragOffset.y = Mathf.Clamp(
                dragOffset.y,
                -maxDragOffsetY,
                maxDragOffsetY
            );
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }
}