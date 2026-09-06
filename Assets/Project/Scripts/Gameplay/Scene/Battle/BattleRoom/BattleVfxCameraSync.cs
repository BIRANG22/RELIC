using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(10000)]
public sealed class BattleVfxCameraSync : MonoBehaviour
{
    [SerializeField] private Camera sourceCamera;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool autoFindSourceCamera = true;
    [SerializeField] private bool copyTransform = true;
    [SerializeField] private bool copyProjection = true;
    [SerializeField] private bool copyViewport = true;
    [SerializeField] private bool copyAspect = true;
    [SerializeField] private bool lockRenderTextureReferenceState;

    private Camera referenceTargetCamera;
    private CameraReferenceState targetReferenceState;
    private bool hasTargetReferenceState;

    private void Reset()
    {
        targetCamera = GetComponent<Camera>();
        ResolveSourceCamera();
    }

    private void Awake()
    {
        ResolveTargetCamera();
        CaptureTargetReferenceState();
        ResolveSourceCamera();
        SyncNow();
    }

    private void OnEnable()
    {
        ResolveTargetCamera();
        EnsureTargetReferenceState();
        ResolveSourceCamera();
        SyncNow();
    }

    private void LateUpdate()
    {
        SyncNow();
    }

    private void OnPreCull()
    {
        SyncNow();
    }

    public void SyncNow()
    {
        ResolveTargetCamera();
        ResolveSourceCamera();

        if (targetCamera == null)
            return;

        bool hasSourceCamera = sourceCamera != null && sourceCamera != targetCamera;
        bool rendersToTexture = targetCamera.targetTexture != null;
        bool shouldUseReferenceState = rendersToTexture && lockRenderTextureReferenceState;

        if (!hasSourceCamera && !shouldUseReferenceState)
            return;

        if (shouldUseReferenceState)
            EnsureTargetReferenceState();

        if (copyTransform)
        {
            if (shouldUseReferenceState)
                RestoreReferenceTransform();
            else if (hasSourceCamera)
                CopyTransform();
        }

        if (copyViewport)
            targetCamera.rect = rendersToTexture ? new Rect(0f, 0f, 1f, 1f) : sourceCamera.rect;

        if (copyAspect)
            targetCamera.aspect = rendersToTexture ? GetTargetTextureAspect() : sourceCamera.aspect;

        if (copyProjection)
        {
            if (shouldUseReferenceState)
                RestoreReferenceProjection();
            else if (hasSourceCamera)
                CopyProjection(rendersToTexture);
        }
    }

    private float GetTargetTextureAspect()
    {
        RenderTexture targetTexture = targetCamera != null ? targetCamera.targetTexture : null;
        if (targetTexture == null || targetTexture.width <= 0 || targetTexture.height <= 0)
            return targetCamera != null ? targetCamera.aspect : sourceCamera != null ? sourceCamera.aspect : 1f;

        return targetTexture.width / (float)targetTexture.height;
    }

    private void CopyTransform()
    {
        Transform sourceTransform = sourceCamera.transform;
        Transform targetTransform = targetCamera.transform;

        targetTransform.SetPositionAndRotation(sourceTransform.position, sourceTransform.rotation);
    }

    private void CopyProjection(bool rendersToTexture)
    {
        targetCamera.orthographic = sourceCamera.orthographic;
        targetCamera.orthographicSize = sourceCamera.orthographicSize;
        targetCamera.fieldOfView = sourceCamera.fieldOfView;
        targetCamera.nearClipPlane = sourceCamera.nearClipPlane;
        targetCamera.farClipPlane = sourceCamera.farClipPlane;
        targetCamera.usePhysicalProperties = sourceCamera.usePhysicalProperties;
        targetCamera.sensorSize = sourceCamera.sensorSize;
        targetCamera.lensShift = sourceCamera.lensShift;
        targetCamera.focalLength = sourceCamera.focalLength;
        targetCamera.gateFit = sourceCamera.gateFit;

        if (rendersToTexture)
            targetCamera.ResetProjectionMatrix();
        else
            targetCamera.projectionMatrix = sourceCamera.projectionMatrix;
    }

    private void EnsureTargetReferenceState()
    {
        if (hasTargetReferenceState && referenceTargetCamera == targetCamera)
            return;

        CaptureTargetReferenceState();
    }

    private void CaptureTargetReferenceState()
    {
        if (targetCamera == null)
        {
            referenceTargetCamera = null;
            hasTargetReferenceState = false;
            return;
        }

        referenceTargetCamera = targetCamera;
        targetReferenceState = CameraReferenceState.Capture(targetCamera);
        hasTargetReferenceState = true;
    }

    private void RestoreReferenceTransform()
    {
        if (!hasTargetReferenceState)
            return;

        targetCamera.transform.SetPositionAndRotation(
            targetReferenceState.Position,
            targetReferenceState.Rotation);
    }

    private void RestoreReferenceProjection()
    {
        if (!hasTargetReferenceState)
            return;

        targetReferenceState.ApplyProjection(targetCamera);
    }

    private void ResolveTargetCamera()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();
    }

    private void ResolveSourceCamera()
    {
        if (sourceCamera != null || !autoFindSourceCamera)
            return;

        sourceCamera = Camera.main;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();
    }
#endif

    private readonly struct CameraReferenceState
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        private readonly bool orthographic;
        private readonly float orthographicSize;
        private readonly float fieldOfView;
        private readonly float nearClipPlane;
        private readonly float farClipPlane;
        private readonly bool usePhysicalProperties;
        private readonly Vector2 sensorSize;
        private readonly Vector2 lensShift;
        private readonly float focalLength;
        private readonly Camera.GateFitMode gateFit;

        private CameraReferenceState(Camera camera)
        {
            Position = camera.transform.position;
            Rotation = camera.transform.rotation;
            orthographic = camera.orthographic;
            orthographicSize = camera.orthographicSize;
            fieldOfView = camera.fieldOfView;
            nearClipPlane = camera.nearClipPlane;
            farClipPlane = camera.farClipPlane;
            usePhysicalProperties = camera.usePhysicalProperties;
            sensorSize = camera.sensorSize;
            lensShift = camera.lensShift;
            focalLength = camera.focalLength;
            gateFit = camera.gateFit;
        }

        public static CameraReferenceState Capture(Camera camera)
        {
            return new CameraReferenceState(camera);
        }

        public void ApplyProjection(Camera camera)
        {
            camera.orthographic = orthographic;
            camera.orthographicSize = orthographicSize;
            camera.fieldOfView = fieldOfView;
            camera.nearClipPlane = nearClipPlane;
            camera.farClipPlane = farClipPlane;
            camera.usePhysicalProperties = usePhysicalProperties;
            camera.sensorSize = sensorSize;
            camera.lensShift = lensShift;
            camera.focalLength = focalLength;
            camera.gateFit = gateFit;
            camera.ResetProjectionMatrix();
        }
    }
}
