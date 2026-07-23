using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera))]
public class HorizontalHubCameraDrag : MonoBehaviour
{
    [Header("드래그")]
    [Tooltip("마우스 드래그에 따른 X축 카메라 이동 속도")]
    [SerializeField] private float horizontalDragSpeed = 1f;

    [Tooltip("마우스 드래그에 따른 Y축 카메라 이동 속도")]
    [SerializeField] private float verticalDragSpeed = 1f;

    [Tooltip("체크하면 마우스를 움직이는 방향과 반대로 카메라가 이동")]
    [SerializeField] private bool invertDrag = true;

    [Tooltip("UI 위에서 클릭했을 때 카메라 드래그를 막음")]
    [SerializeField] private bool blockDragOverUI = true;

    [Header("드래그 차단 패널")]
    [Tooltip("이 패널이 활성화되어 있으면 카메라 드래그를 사용하지 않습니다. 비어 있으면 CharacterSettingPanel을 자동으로 찾습니다.")]
    [SerializeField] private GameObject characterSettingPanel;

    [Header("부드러운 이동")]
    [Tooltip("목표 위치까지 부드럽게 이동하는 시간. 값이 작을수록 빠르게 따라감")]
    [Min(0.01f)]
    [SerializeField] private float smoothTime = 0.12f;

    [Tooltip("부드러운 이동 중 허용할 최대 속도")]
    [Min(0.01f)]
    [SerializeField] private float maximumSmoothSpeed = 100f;

    [Header("기본 위치 복귀")]
    [Tooltip("체크하면 게임 시작 시 카메라 위치를 기본 위치로 저장합니다.")]
    [SerializeField] private bool useInitialPositionAsDefault = true;

    [Tooltip("초기 위치를 사용하지 않을 때 복귀할 카메라 기본 위치입니다.")]
    [SerializeField] private Vector3 defaultPosition;

    [Header("X축 이동 범위")]
    [SerializeField] private float minimumX = -12.5f;
    [SerializeField] private float maximumX = 11f;

    [Header("Y축 이동 범위")]
    [SerializeField] private float minimumY = -3f;
    [SerializeField] private float maximumY = 3f;

    private Camera targetCamera;
    private bool isDragging;
    private Vector3 previousMousePosition;
    private Vector3 targetPosition;
    private Vector3 smoothVelocity;
    private Vector3 cachedDefaultPosition;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        FindCharacterSettingPanelIfNeeded();

        cachedDefaultPosition = useInitialPositionAsDefault
            ? transform.position
            : ClampPosition(defaultPosition);

        ResetTargetPosition();
    }

    private void OnEnable()
    {
        ResetTargetPosition();
    }

    private void OnDisable()
    {
        isDragging = false;
        smoothVelocity = Vector3.zero;
    }

    private void Update()
    {
        if (IsDragBlocked())
        {
            isDragging = false;
            return;
        }

        HandleMouseInput();
    }

    private void LateUpdate()
    {
        MoveCameraSmoothly();
    }

    private bool IsDragBlocked()
    {
        FindCharacterSettingPanelIfNeeded();
        return characterSettingPanel != null && characterSettingPanel.activeInHierarchy;
    }

    private void FindCharacterSettingPanelIfNeeded()
    {
        if (characterSettingPanel != null)
            return;

        Transform[] transforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null &&
                transforms[i].name == "CharacterSettingPanel")
            {
                characterSettingPanel = transforms[i].gameObject;
                break;
            }
        }
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
            Vector3 mouseDelta = currentMousePosition - previousMousePosition;

            // 화면 픽셀 이동량을 직교 카메라 기준 월드 좌표 이동량으로 변환한다.
            float worldUnitsPerPixel =
                (targetCamera.orthographicSize * 2f) / Mathf.Max(1, Screen.height);

            float moveX = mouseDelta.x * worldUnitsPerPixel * horizontalDragSpeed;
            float moveY = mouseDelta.y * worldUnitsPerPixel * verticalDragSpeed;

            if (invertDrag)
            {
                moveX *= -1f;
                moveY *= -1f;
            }

            targetPosition.x = Mathf.Clamp(
                targetPosition.x + moveX,
                minimumX,
                maximumX);

            targetPosition.y = Mathf.Clamp(
                targetPosition.y + moveY,
                minimumY,
                maximumY);

            previousMousePosition = currentMousePosition;
        }

        if (isDragging && Input.GetMouseButtonUp(0))
            isDragging = false;
    }

    private void MoveCameraSmoothly()
    {
        Vector3 currentPosition = transform.position;

        targetPosition = ClampPosition(targetPosition);
        targetPosition.z = currentPosition.z;

        transform.position = Vector3.SmoothDamp(
            currentPosition,
            targetPosition,
            ref smoothVelocity,
            smoothTime,
            maximumSmoothSpeed,
            Time.unscaledDeltaTime);
    }

    /// <summary>
    /// 카메라를 저장된 기본 위치로 부드럽게 복귀시킵니다.
    /// 필요할 때 외부 버튼이나 전환 스크립트에서 호출할 수 있습니다.
    /// </summary>
    public void ResetToDefaultPosition()
    {
        isDragging = false;
        smoothVelocity = Vector3.zero;
        targetPosition = ClampPosition(cachedDefaultPosition);
    }

    /// <summary>
    /// 카메라를 저장된 기본 위치로 즉시 복귀시킵니다.
    /// </summary>
    public void ResetToDefaultPositionImmediate()
    {
        isDragging = false;
        smoothVelocity = Vector3.zero;
        targetPosition = ClampPosition(cachedDefaultPosition);
        transform.position = targetPosition;
    }

    /// <summary>
    /// 현재 카메라 위치를 새로운 기본 위치로 저장합니다.
    /// </summary>
    public void SaveCurrentPositionAsDefault()
    {
        cachedDefaultPosition = ClampPosition(transform.position);
    }

    private void ResetTargetPosition()
    {
        targetPosition = ClampPosition(transform.position);
        transform.position = targetPosition;
        smoothVelocity = Vector3.zero;
    }

    private Vector3 ClampPosition(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, minimumX, maximumX);
        position.y = Mathf.Clamp(position.y, minimumY, maximumY);
        return position;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (minimumX > maximumX)
            maximumX = minimumX;

        if (minimumY > maximumY)
            maximumY = minimumY;

        smoothTime = Mathf.Max(0.01f, smoothTime);
        maximumSmoothSpeed = Mathf.Max(0.01f, maximumSmoothSpeed);

        if (!useInitialPositionAsDefault)
            defaultPosition = ClampPosition(defaultPosition);
    }
#endif
}
