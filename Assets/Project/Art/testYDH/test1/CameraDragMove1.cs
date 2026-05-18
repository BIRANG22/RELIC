using UnityEngine;

public class CameraDragMove1 : MonoBehaviour
{
    [Header("Drag Movement")]
    [SerializeField] private float maxDragOffsetX = 2f;
    [SerializeField] private float maxDragOffsetY = 1.2f;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSensitivity = 2f;
    [SerializeField] private float minZoomZ = -15f;
    [SerializeField] private float maxZoomZ = -5f;

    [Header("Smooth Settings")]
    // 이 값이 높을수록 미끄러짐 없이 마우스/휠을 멈춘 자리에 '착' 하고 즉시 멈춥니다.
    [SerializeField] private float smoothSpeed = 15f;

    private Vector3 startPosition;

    // 마우스 월드 좌표 계산용 변수
    private Vector3 dragStartWorldPos;
    private Vector3 cameraStartPosOnDrag;

    // 최종적으로 제한 범위 내에서 누적될 오프셋
    private Vector3 currentOffset;

    private bool isDragging;

    private void Start()
    {
        startPosition = transform.position;
        currentOffset = Vector3.zero;
    }

    private void LateUpdate()
    {
        HandleDragMove();
        HandleZoom();

        // 시작 위치에 실시간 계산된 오프셋(X, Y, Z)을 적용하여 목표 위치 설정
        Vector3 targetPosition = startPosition + currentOffset;

        // 최종 위치로 부드럽게 보간 이동 (smoothSpeed가 높아 거의 즉각 반응합니다)
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

            // 클릭한 순간의 마우스 위치를 월드 좌표(Z축 반영)로 변환
            dragStartWorldPos = GetMouseWorldPosition();
            // 클릭한 순간의 카메라 위치 저장
            cameraStartPosOnDrag = transform.position;
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            // 현재 마우스의 월드 좌표
            Vector3 currentMouseWorldPos = GetMouseWorldPosition();

            // 마우스가 처음 클릭된 곳으로부터 얼마나 움직였는지 계산 (이동량 구하기)
            Vector3 mouseDelta = dragStartWorldPos - currentMouseWorldPos;

            // 마우스가 왼쪽으로 가면 카메라는 오른쪽으로 가야 화면이 따라오므로 더해줍니다.
            Vector3 targetCameraPos = cameraStartPosOnDrag + new Vector3(mouseDelta.x, mouseDelta.y, 0f);

            // 해당 목표 위치가 시작 위치로부터 지정된 범위를 벗어나지 않도록 '즉시' 제한합니다.
            float targetOffsetX = Mathf.Clamp(targetCameraPos.x - startPosition.x, -maxDragOffsetX, maxDragOffsetX);
            float targetOffsetY = Mathf.Clamp(targetCameraPos.y - startPosition.y, -maxDragOffsetY, maxDragOffsetY);

            // 계산된 정밀 오프셋을 적용
            currentOffset.x = targetOffsetX;
            currentOffset.y = targetOffsetY;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    private void HandleZoom()
    {
        // 휠을 굴린 '입력 양' 자체를 가져옵니다. (위로 굴리면 +, 아래로 굴리면 -)
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scrollInput) > 0.001f)
        {
            // [수정] -= 로 변경하여 휠 업(+) 시 Z값이 감소(줌인)하고, 휠 다운(-) 시 Z값이 증가(줌아웃)하게 만듭니다.
            currentOffset.z -= scrollInput * zoomSensitivity;

            // 절대적인 Z 위치 계산 후 한계치 체크
            float targetAbsoluteZ = startPosition.z + currentOffset.z;
            targetAbsoluteZ = Mathf.Clamp(targetAbsoluteZ, minZoomZ, maxZoomZ);

            // 한계치에 걸린 값을 오프셋에 다시 역산해서 반영
            currentOffset.z = targetAbsoluteZ - startPosition.z;
        }
    }

    /// <summary>
    /// 현재 마우스 포인터의 위치를 게임 월드(3D 공간) 좌표로 정확하게 변환해주는 함수입니다.
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        // 카메라와 원점(0) 사이의 거리를 기준으로 마우스 깊이를 설정합니다.
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = Mathf.Abs(transform.position.z);

        return Camera.main.ScreenToWorldPoint(mouseScreenPos);
    }
}