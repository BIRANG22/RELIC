using UnityEngine;

[DisallowMultipleComponent]
public sealed class CameraMouseParallaxController : MonoBehaviour
{
    private const float CameraMotionPositionThresholdSqr = 0.000001f;
    private const float CameraMotionRotationThreshold = 0.01f;

    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("Mouse Parallax")]
    [SerializeField] private bool enableMouseParallax = true;
    [SerializeField] private Vector2 mouseParallaxPositionAmount = new Vector2(0.08f, 0.03f);
    [SerializeField] private Vector2 mouseParallaxRotationAmount = new Vector2(3f, 1.5f);
    [SerializeField, Min(0f)] private float mouseParallaxSmoothSpeed = 8f;
    [SerializeField, Range(0f, 1f)] private float mouseParallaxCameraMotionMultiplier = 0.35f;

    private Vector2 currentMouseParallax;
    private Vector3 lastMouseParallaxPositionOffset;
    private Quaternion lastMouseParallaxRotationOffset = Quaternion.identity;
    private bool hasAppliedMouseParallax;
    private Vector3 previousBasePosition;
    private Quaternion previousBaseRotation = Quaternion.identity;
    private bool hasPreviousBaseTransform;
    private int blurCapturePauseDepth;
    private static int uiPanelPauseDepth;
    private static bool lobbyContentPanelPause;


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticPauseState()
    {
        uiPanelPauseDepth = 0;
        lobbyContentPanelPause = false;
    }

    /// <summary>
    /// 블러 패널이나 메뉴 패널이 열린 동안 로비 카메라 마우스 패럴랙스를 정지합니다.
    /// 여러 패널이 겹쳐 열려도 마지막 패널이 닫힐 때까지 정지 상태를 유지합니다.
    /// </summary>
    public static void BeginUiPanelPause()
    {
        uiPanelPauseDepth++;
    }

    /// <summary>
    /// 패널 정지 요청을 하나 해제합니다. 모든 패널이 닫혔을 때만 패럴랙스가 다시 동작합니다.
    /// </summary>
    public static void EndUiPanelPause()
    {
        if (uiPanelPauseDepth <= 0)
        {
            uiPanelPauseDepth = 0;
            return;
        }

        uiPanelPauseDepth--;
    }

    public static bool IsUiPanelPauseActive => uiPanelPauseDepth > 0 || lobbyContentPanelPause;

    /// <summary>
    /// 로비의 메인 PositionPanel 이외의 콘텐츠 패널이 열려 있는 동안 카메라 패럴랙스를 정지합니다.
    /// 패널 전환 시 현재 열린 메인 패널을 기준으로 true/false를 직접 갱신합니다.
    /// </summary>
    public static void SetLobbyContentPanelPause(bool shouldPause)
    {
        lobbyContentPanelPause = shouldPause;
    }

    private void Awake()
    {
        ResolveTargetCamera();
        CaptureBaseTransform();
    }

    private void OnEnable()
    {
        ResolveTargetCamera();
        ClearMouseParallaxImmediate();
        CaptureBaseTransform();
    }

    private void OnDisable()
    {
        ClearMouseParallaxImmediate();
        hasPreviousBaseTransform = false;
        blurCapturePauseDepth = 0;
    }

    private void Update()
    {
        // 패널/블러 캡처로 정지된 동안에는 현재 화면에 적용된 패럴랙스 오프셋을 유지합니다.
        // 정지 요청이 없는 평상시에만 다른 카메라 이동 로직이 기준 Transform을 다룰 수 있도록
        // 이전 프레임의 패럴랙스 오프셋을 제거합니다.
        if (IsParallaxPaused())
            return;

        RemoveMouseParallax();
    }

    private void LateUpdate()
    {
        ApplyMouseParallax();
    }

    /// <summary>
    /// 블러 배경 캡처 직전에 호출합니다.
    /// 현재 화면에 적용되어 있는 마우스 패럴랙스 위치/회전을 그대로 유지한 채
    /// 캡처가 끝날 때까지 추가 카메라 무빙만 정지합니다.
    /// </summary>
    public void BeginBlurCapturePause()
    {
        ResolveTargetCamera();
        blurCapturePauseDepth++;
    }

    /// <summary>
    /// 블러 배경 캡처가 끝난 뒤 호출합니다.
    /// 중첩된 캡처 요청이 모두 끝났을 때 마우스 패럴랙스를 다시 허용합니다.
    /// </summary>
    public void EndBlurCapturePause()
    {
        if (blurCapturePauseDepth <= 0)
            return;

        blurCapturePauseDepth--;
    }

    /// <summary>
    /// 이 컨트롤러가 지정한 카메라를 제어하는지 확인합니다.
    /// </summary>
    public bool UsesCamera(Camera camera)
    {
        if (camera == null)
            return false;

        ResolveTargetCamera();
        return targetCamera == camera;
    }


    private bool IsParallaxPaused()
    {
        return blurCapturePauseDepth > 0 || uiPanelPauseDepth > 0 || lobbyContentPanelPause;
    }

    private void ApplyMouseParallax()
    {
        ResolveTargetCamera();

        if (targetCamera == null)
            return;

        if (IsParallaxPaused())
            return;

        RemoveMouseParallax();

        Transform cameraTransform = targetCamera.transform;
        Vector3 basePosition = cameraTransform.position;
        Quaternion baseRotation = cameraTransform.rotation;

        if (!enableMouseParallax)
        {
            currentMouseParallax = Vector2.zero;
            StoreBaseTransform(basePosition, baseRotation);
            return;
        }

        bool isCameraMoving = HasBaseCameraMoved(basePosition, baseRotation);
        float interpolation = GetMouseParallaxInterpolation();
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        Vector2 targetParallax = NormalizeMousePositionForParallax(Input.mousePosition, screenSize);
        currentMouseParallax = Vector2.Lerp(currentMouseParallax, targetParallax, interpolation);

        float intensityMultiplier = GetMouseParallaxIntensityMultiplier(
            isCameraMoving,
            mouseParallaxCameraMotionMultiplier);

        lastMouseParallaxPositionOffset = CalculateMouseParallaxPositionOffset(
            currentMouseParallax,
            mouseParallaxPositionAmount,
            intensityMultiplier);

        Vector3 eulerOffset = CalculateMouseParallaxEulerOffset(
            currentMouseParallax,
            mouseParallaxRotationAmount,
            intensityMultiplier);

        lastMouseParallaxRotationOffset = Quaternion.Euler(eulerOffset);

        cameraTransform.SetPositionAndRotation(
            basePosition + lastMouseParallaxPositionOffset,
            baseRotation * lastMouseParallaxRotationOffset);

        hasAppliedMouseParallax = true;
        StoreBaseTransform(basePosition, baseRotation);
    }

    private void RemoveMouseParallax()
    {
        if (targetCamera == null || !hasAppliedMouseParallax)
            return;

        Transform cameraTransform = targetCamera.transform;
        cameraTransform.SetPositionAndRotation(
            cameraTransform.position - lastMouseParallaxPositionOffset,
            cameraTransform.rotation * Quaternion.Inverse(lastMouseParallaxRotationOffset));

        lastMouseParallaxPositionOffset = Vector3.zero;
        lastMouseParallaxRotationOffset = Quaternion.identity;
        hasAppliedMouseParallax = false;
    }

    private void ClearMouseParallaxImmediate()
    {
        RemoveMouseParallax();
        currentMouseParallax = Vector2.zero;
    }

    private void ResolveTargetCamera()
    {
        if (targetCamera != null)
            return;

        targetCamera = GetComponent<Camera>();

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void CaptureBaseTransform()
    {
        if (targetCamera == null)
            return;

        StoreBaseTransform(targetCamera.transform.position, targetCamera.transform.rotation);
    }

    private void StoreBaseTransform(Vector3 basePosition, Quaternion baseRotation)
    {
        previousBasePosition = basePosition;
        previousBaseRotation = baseRotation;
        hasPreviousBaseTransform = true;
    }

    private bool HasBaseCameraMoved(Vector3 basePosition, Quaternion baseRotation)
    {
        if (!hasPreviousBaseTransform)
            return false;

        return (basePosition - previousBasePosition).sqrMagnitude > CameraMotionPositionThresholdSqr ||
               Quaternion.Angle(baseRotation, previousBaseRotation) > CameraMotionRotationThreshold;
    }

    private float GetMouseParallaxInterpolation()
    {
        float smoothSpeed = Mathf.Max(0f, mouseParallaxSmoothSpeed);

        if (smoothSpeed <= 0f)
            return 1f;

        return 1f - Mathf.Exp(-smoothSpeed * Time.unscaledDeltaTime);
    }

    public static Vector2 NormalizeMousePositionForParallax(Vector2 mousePosition, Vector2 screenSize)
    {
        if (screenSize.x <= 0f || screenSize.y <= 0f)
            return Vector2.zero;

        float normalizedX = (mousePosition.x / screenSize.x - 0.5f) * 2f;
        float normalizedY = (mousePosition.y / screenSize.y - 0.5f) * 2f;

        return new Vector2(
            Mathf.Clamp(normalizedX, -1f, 1f),
            Mathf.Clamp(normalizedY, -1f, 1f));
    }

    public static Vector3 CalculateMouseParallaxPositionOffset(
        Vector2 normalizedMouseOffset,
        Vector2 positionAmount,
        float intensityMultiplier)
    {
        Vector2 clampedOffset = ClampMouseParallaxOffset(normalizedMouseOffset);
        float multiplier = Mathf.Max(0f, intensityMultiplier);

        return new Vector3(
            clampedOffset.x * Mathf.Max(0f, positionAmount.x) * multiplier,
            clampedOffset.y * Mathf.Max(0f, positionAmount.y) * multiplier,
            0f);
    }

    public static Vector3 CalculateMouseParallaxEulerOffset(
        Vector2 normalizedMouseOffset,
        Vector2 rotationAmount,
        float intensityMultiplier)
    {
        Vector2 clampedOffset = ClampMouseParallaxOffset(normalizedMouseOffset);
        float multiplier = Mathf.Max(0f, intensityMultiplier);

        return new Vector3(
            -clampedOffset.y * Mathf.Max(0f, rotationAmount.x) * multiplier,
            clampedOffset.x * Mathf.Max(0f, rotationAmount.y) * multiplier,
            0f);
    }

    public static float GetMouseParallaxIntensityMultiplier(
        bool isCameraMoving,
        float cameraMotionMultiplier)
    {
        return isCameraMoving ? Mathf.Clamp01(cameraMotionMultiplier) : 1f;
    }

    private static Vector2 ClampMouseParallaxOffset(Vector2 normalizedMouseOffset)
    {
        return new Vector2(
            Mathf.Clamp(normalizedMouseOffset.x, -1f, 1f),
            Mathf.Clamp(normalizedMouseOffset.y, -1f, 1f));
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        mouseParallaxPositionAmount.x = Mathf.Max(0f, mouseParallaxPositionAmount.x);
        mouseParallaxPositionAmount.y = Mathf.Max(0f, mouseParallaxPositionAmount.y);
        mouseParallaxRotationAmount.x = Mathf.Max(0f, mouseParallaxRotationAmount.x);
        mouseParallaxRotationAmount.y = Mathf.Max(0f, mouseParallaxRotationAmount.y);
        mouseParallaxSmoothSpeed = Mathf.Max(0f, mouseParallaxSmoothSpeed);
        mouseParallaxCameraMotionMultiplier = Mathf.Clamp01(mouseParallaxCameraMotionMultiplier);
    }
#endif
}
