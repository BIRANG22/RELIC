using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera))]
public class HorizontalHubCameraDrag : MonoBehaviour
{
    [Header("드래그")]
    [SerializeField] private float dragSpeed = 1f;

    [Tooltip("체크하면 마우스를 움직이는 방향과 반대로 카메라가 이동")]
    [SerializeField] private bool invertDrag = true;

    [Tooltip("UI 위에서 클릭했을 때 카메라 드래그를 막음")]
    [SerializeField] private bool blockDragOverUI = true;

    [Header("이동 범위")]
    [SerializeField] private float minimumX = -12.5f;
    [SerializeField] private float maximumX = 11f;

    private Camera targetCamera;
    private bool isDragging;
    private Vector3 previousMousePosition;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
    }

    private void OnDisable()
    {
        isDragging = false;
    }

    private void Update()
    {
        HandleMouseInput();
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
            newX = Mathf.Clamp(newX, minimumX, maximumX);

            SetCameraX(newX);
            previousMousePosition = currentMousePosition;
        }

        if (isDragging && Input.GetMouseButtonUp(0))
            isDragging = false;
    }

    private void SetCameraX(float x)
    {
        Vector3 position = transform.position;
        position.x = x;
        transform.position = position;
    }
}
