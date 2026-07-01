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

    private void Reset()
    {
        targetCamera = GetComponent<Camera>();
        ResolveSourceCamera();
    }

    private void Awake()
    {
        ResolveTargetCamera();
        ResolveSourceCamera();
        SyncNow();
    }

    private void LateUpdate()
    {
        SyncNow();
    }

    public void SyncNow()
    {
        ResolveTargetCamera();
        ResolveSourceCamera();

        if (sourceCamera == null || targetCamera == null || sourceCamera == targetCamera)
            return;

        if (copyTransform)
            CopyTransform();

        if (copyViewport)
            targetCamera.rect = sourceCamera.rect;

        if (copyAspect)
            targetCamera.aspect = sourceCamera.aspect;

        if (copyProjection)
            CopyProjection();
    }

    private void CopyTransform()
    {
        Transform sourceTransform = sourceCamera.transform;
        Transform targetTransform = targetCamera.transform;

        targetTransform.SetPositionAndRotation(sourceTransform.position, sourceTransform.rotation);
    }

    private void CopyProjection()
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
        targetCamera.projectionMatrix = sourceCamera.projectionMatrix;
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
}
