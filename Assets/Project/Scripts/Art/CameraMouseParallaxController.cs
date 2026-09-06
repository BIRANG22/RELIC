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
    private static int introPauseDepth;
    private static int uiPanelPauseDepth;
    private static bool lobbyContentPanelPause;


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticPauseState()
    {
        introPauseDepth = 0;
        uiPanelPauseDepth = 0;
        lobbyContentPanelPause = false;
    }

    public static void BeginIntroPause()
    {
        introPauseDepth++;
        ClearAllMouseParallaxImmediate();
    }

    public static void EndIntroPause()
    {
        if (introPauseDepth <= 0)
        {
            introPauseDepth = 0;
            return;
        }

        introPauseDepth--;

        if (introPauseDepth == 0)
            ClearAllMouseParallaxImmediate();
    }
    /// <summary>
    /// ���� �г��̳� �޴� �г��� ���� ���� �κ� ī�޶� ���콺 �з������� �����մϴ�.
    /// ���� �г��� ���� ������ ������ �г��� ���� ������ ���� ���¸� �����մϴ�.
    /// </summary>
    public static void BeginUiPanelPause()
    {
        uiPanelPauseDepth++;
    }

    /// <summary>
    /// �г� ���� ��û�� �ϳ� �����մϴ�. ��� �г��� ������ ���� �з������� �ٽ� �����մϴ�.
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

    public static bool IsUiPanelPauseActive => introPauseDepth > 0 || uiPanelPauseDepth > 0 || lobbyContentPanelPause;

    /// <summary>
    /// �κ��� ���� PositionPanel �̿��� ������ �г��� ���� �ִ� ���� ī�޶� �з������� �����մϴ�.
    /// �г� ��ȯ �� ���� ���� ���� �г��� �������� true/false�� ���� �����մϴ�.
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
        // �г�/���� ĸó�� ������ ���ȿ��� ���� ȭ�鿡 ����� �з����� �������� �����մϴ�.
        // ���� ��û�� ���� ���ÿ��� �ٸ� ī�޶� �̵� ������ ���� Transform�� �ٷ� �� �ֵ���
        // ���� �������� �з����� �������� �����մϴ�.
        if (IsParallaxPaused())
            return;

        RemoveMouseParallax();
    }

    private void LateUpdate()
    {
        ApplyMouseParallax();
    }

    /// <summary>
    /// ���� ��� ĸó ������ ȣ���մϴ�.
    /// ���� ȭ�鿡 ����Ǿ� �ִ� ���콺 �з����� ��ġ/ȸ���� �״�� ������ ä
    /// ĸó�� ���� ������ �߰� ī�޶� ������ �����մϴ�.
    /// </summary>
    public void BeginBlurCapturePause()
    {
        ResolveTargetCamera();
        blurCapturePauseDepth++;
    }

    /// <summary>
    /// ���� ��� ĸó�� ���� �� ȣ���մϴ�.
    /// ��ø�� ĸó ��û�� ��� ������ �� ���콺 �з������� �ٽ� ����մϴ�.
    /// </summary>
    public void EndBlurCapturePause()
    {
        if (blurCapturePauseDepth <= 0)
            return;

        blurCapturePauseDepth--;
    }

    /// <summary>
    /// �� ��Ʈ�ѷ��� ������ ī�޶� �����ϴ��� Ȯ���մϴ�.
    /// </summary>
    public bool UsesCamera(Camera camera)
    {
        if (camera == null)
            return false;

        ResolveTargetCamera();
        return targetCamera == camera;
    }


    private static void ClearAllMouseParallaxImmediate()
    {
        CameraMouseParallaxController[] controllers =
            Resources.FindObjectsOfTypeAll<CameraMouseParallaxController>();

        for (int i = 0; i < controllers.Length; i++)
        {
            CameraMouseParallaxController controller = controllers[i];
            if (controller != null)
                controller.ClearMouseParallaxImmediate();
        }
    }
    private bool IsParallaxPaused()
    {
        return blurCapturePauseDepth > 0 || introPauseDepth > 0 || uiPanelPauseDepth > 0 || lobbyContentPanelPause;
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
