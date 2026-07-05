using System.Collections;
using UnityEngine;

/// <summary>
/// 전투 진행 중 월드 배경 효과 Plane 2개를 화면 중앙으로 모았다가,
/// 다시 행동 예약 턴이 돌아오면 원래 위치로 되돌립니다.
/// BattleEffect 오브젝트에 붙이고 leftPlane/rightPlane에 Plane, Plane (1)을 연결해서 사용합니다.
/// </summary>
public class BattleEffectPlaneSlideController : MonoBehaviour
{
    [Header("Target Planes")]
    [SerializeField] private Transform leftPlane;
    [SerializeField] private Transform rightPlane;

    [Header("Battle Position")]
    [SerializeField] private float battleCenterX = 0f;
    [SerializeField] private bool cacheStartPositionOnAwake = true;
    [SerializeField] private Vector3 leftReservePosition = new Vector3(-50f, 0f, 0f);
    [SerializeField] private Vector3 rightReservePosition = new Vector3(50f, 0f, 0f);

    [Header("Camera Follow")]
    [SerializeField] private bool followCameraPosition = true;
    [SerializeField] private Camera followCamera;
    [SerializeField] private bool autoFindFollowCamera = true;
    [SerializeField] private bool followCameraX = true;
    // 메인 카메라가 그리드 위치로 줌인할 때 X/Y가 같이 움직이므로,
    // Plane도 카메라의 X/Y 이동량을 같이 따라가게 합니다.
    [SerializeField] private bool followCameraY = true;
    [SerializeField] private bool followCameraZ = false;
    [SerializeField] private bool updateFollowInLateUpdate = true;

    [Header("Camera Zoom Y Follow")]
    [SerializeField] private bool adjustYByCameraZ = true;
    [SerializeField] private float referenceCameraZ = -20f;
    [SerializeField] private float zoomCameraZ = -13f;
    [SerializeField] private bool clampCameraZoomLerp = true;
    [SerializeField] private float leftYAtReferenceZ = -4.5f;
    [SerializeField] private float leftYAtZoomZ = -3.5f;
    [SerializeField] private float rightYAtReferenceZ = 5f;
    [SerializeField] private float rightYAtZoomZ = 4f;

    [Header("Camera Y Direction Zoom Compensation")]
    [SerializeField] private bool adjustZoomTargetByCameraY = true;
    [SerializeField, Min(0f)] private float cameraYDirectionDeadZone = 0.05f;
    [SerializeField] private float leftYAtZoomZWhenCameraYMinus = -4.5f;
    [SerializeField] private float rightYAtZoomZWhenCameraYMinus = 3f;
    [SerializeField] private float rightYAtZoomZWhenCameraYPlus = 4.5f;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float moveDuration = 0.35f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool snapToReservePositionOnStart = true;

    private Coroutine moveRoutine;
    private bool cachedPositions;
    private float slide01;
    private Vector3 referenceCameraPosition;
    private bool hasReferenceCameraPosition;

    private void Awake()
    {
        ResolveFollowCamera();
        CaptureReferenceCameraPosition();

        if (cacheStartPositionOnAwake)
            CacheCurrentReservePositions();
    }

    private void Start()
    {
        ResolveFollowCamera();

        if (!hasReferenceCameraPosition)
            CaptureReferenceCameraPosition();

        if (!cachedPositions)
            CacheCurrentReservePositions();

        if (snapToReservePositionOnStart)
            SetReservePositionInstant();
    }

    private void LateUpdate()
    {
        if (!updateFollowInLateUpdate)
            return;

        // 카메라 위치 추적을 끈 상태라도, 카메라 Z 확대에 따른 Y 보간은 계속 갱신되어야 합니다.
        if (!followCameraPosition && !adjustYByCameraZ)
            return;

        ApplyPlanePositions(slide01);
    }

    private void OnEnable()
    {
        BattleTurnExecutor.BattleExecutionStarted -= MoveToBattleCenter;
        BattleTurnExecutor.BattleExecutionStarted += MoveToBattleCenter;

        BattleTurnExecutor.PlayerTurnReturned -= MoveToReservePosition;
        BattleTurnExecutor.PlayerTurnReturned += MoveToReservePosition;
    }

    private void OnDisable()
    {
        BattleTurnExecutor.BattleExecutionStarted -= MoveToBattleCenter;
        BattleTurnExecutor.PlayerTurnReturned -= MoveToReservePosition;
    }

    public void MoveToBattleCenter()
    {
        if (!isActiveAndEnabled)
            return;

        if (!cachedPositions)
            CacheCurrentReservePositions();

        PlayMove(1f);
    }

    public void MoveToReservePosition()
    {
        if (!isActiveAndEnabled)
            return;

        if (!cachedPositions)
            CacheCurrentReservePositions();

        PlayMove(0f);
    }

    public void SetReservePositionInstant()
    {
        if (!cachedPositions)
            CacheCurrentReservePositions();

        slide01 = 0f;
        ApplyPlanePositions(slide01);
    }

    public void ForceResetToReservePositionInstant()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        if (!cachedPositions)
            CacheCurrentReservePositions();

        slide01 = 0f;
        ApplyPlanePositions(slide01);
    }

    private void CacheCurrentReservePositions()
    {
        if (leftPlane != null)
            leftReservePosition = leftPlane.localPosition;

        if (rightPlane != null)
            rightReservePosition = rightPlane.localPosition;

        cachedPositions = true;
    }

    private void ResolveFollowCamera()
    {
        if (!autoFindFollowCamera)
            return;

        if (followCamera != null)
            return;

        followCamera = Camera.main;

        if (followCamera != null)
            return;

        BattleCameraController battleCameraController = BattleCameraController.Instance;
        if (battleCameraController == null)
            return;

        followCamera = battleCameraController.GetComponent<Camera>();
        if (followCamera == null)
            followCamera = battleCameraController.GetComponentInChildren<Camera>();
    }

    private void CaptureReferenceCameraPosition()
    {
        ResolveFollowCamera();

        if (followCamera == null)
            return;

        referenceCameraPosition = followCamera.transform.position;
        hasReferenceCameraPosition = true;
    }

    private void PlayMove(float targetSlide01)
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(MoveRoutine(targetSlide01));
    }

    private IEnumerator MoveRoutine(float targetSlide01)
    {
        float startSlide01 = slide01;
        float duration = Mathf.Max(0f, moveDuration);

        if (duration <= 0f)
        {
            slide01 = Mathf.Clamp01(targetSlide01);
            ApplyPlanePositions(slide01);
            moveRoutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curveT = moveCurve != null ? moveCurve.Evaluate(t) : t;

            slide01 = Mathf.LerpUnclamped(startSlide01, targetSlide01, curveT);
            ApplyPlanePositions(slide01);

            yield return null;
        }

        slide01 = Mathf.Clamp01(targetSlide01);
        ApplyPlanePositions(slide01);
        moveRoutine = null;
    }

    private void ApplyPlanePositions(float t)
    {
        if (!cachedPositions)
            CacheCurrentReservePositions();

        ResolveFollowCamera();

        if (followCameraPosition && !hasReferenceCameraPosition)
            CaptureReferenceCameraPosition();

        if (leftPlane != null)
        {
            Vector3 leftBattlePosition = GetBattlePosition(leftReservePosition);
            Vector3 leftBasePosition = Vector3.LerpUnclamped(leftReservePosition, leftBattlePosition, t);
            leftBasePosition.y = GetCameraZoomAdjustedY(leftBasePosition.y, true);
            leftPlane.localPosition = ApplyCameraDeltaToLocalPosition(leftPlane, leftBasePosition);
        }

        if (rightPlane != null)
        {
            Vector3 rightBattlePosition = GetBattlePosition(rightReservePosition);
            Vector3 rightBasePosition = Vector3.LerpUnclamped(rightReservePosition, rightBattlePosition, t);
            rightBasePosition.y = GetCameraZoomAdjustedY(rightBasePosition.y, false);
            rightPlane.localPosition = ApplyCameraDeltaToLocalPosition(rightPlane, rightBasePosition);
        }
    }

    private Vector3 GetBattlePosition(Vector3 reservePosition)
    {
        reservePosition.x = battleCenterX;
        return reservePosition;
    }

    private float GetCameraZoomAdjustedY(float fallbackY, bool isLeftPlane)
    {
        if (!adjustYByCameraZ)
            return fallbackY;

        ResolveFollowCamera();
        if (followCamera == null)
            return fallbackY;

        // 카메라가 그리드 위치에 따라 -20에서 -13까지 전부 가지 않고 중간 값에서 멈춰도,
        // 현재 카메라 Z값을 그대로 사용해 Plane Y값을 같은 비율로 보간합니다.
        float zoomT = GetCameraZoomLerp01();

        if (isLeftPlane)
            return Mathf.LerpUnclamped(leftYAtReferenceZ, GetLeftZoomTargetY(), zoomT);

        return Mathf.LerpUnclamped(rightYAtReferenceZ, GetRightZoomTargetY(), zoomT);
    }

    private float GetLeftZoomTargetY()
    {
        if (!adjustZoomTargetByCameraY)
            return leftYAtZoomZ;

        float cameraDeltaY = GetCameraDeltaY();

        // 카메라가 아래쪽으로 이동하는 그리드 위치에서는 아래 Plane이 올라오지 않도록
        // -3.5 대신 -4.5에 머물게 합니다.
        if (cameraDeltaY < -cameraYDirectionDeadZone)
            return leftYAtZoomZWhenCameraYMinus;

        return leftYAtZoomZ;
    }

    private float GetRightZoomTargetY()
    {
        if (!adjustZoomTargetByCameraY)
            return rightYAtZoomZ;

        float cameraDeltaY = GetCameraDeltaY();

        // 카메라가 아래쪽으로 이동하는 그리드 위치에서는 위 Plane을 더 내려서
        // 4 대신 3까지 보간합니다.
        if (cameraDeltaY < -cameraYDirectionDeadZone)
            return rightYAtZoomZWhenCameraYMinus;

        // 카메라가 위쪽으로 이동하는 그리드 위치에서는 위 Plane이 너무 많이 내려오지 않도록
        // 4 대신 4.5까지만 보간합니다.
        if (cameraDeltaY > cameraYDirectionDeadZone)
            return rightYAtZoomZWhenCameraYPlus;

        return rightYAtZoomZ;
    }

    private float GetCameraDeltaY()
    {
        if (followCamera == null || !hasReferenceCameraPosition)
            return 0f;

        return followCamera.transform.position.y - referenceCameraPosition.y;
    }

    private float GetCameraZoomLerp01()
    {
        if (followCamera == null)
            return 0f;

        float range = zoomCameraZ - referenceCameraZ;
        if (Mathf.Approximately(range, 0f))
            return 0f;

        float t = (followCamera.transform.position.z - referenceCameraZ) / range;
        return clampCameraZoomLerp ? Mathf.Clamp01(t) : t;
    }

    private Vector3 ApplyCameraDeltaToLocalPosition(Transform target, Vector3 baseLocalPosition)
    {
        if (!followCameraPosition)
            return baseLocalPosition;

        if (followCamera == null || !hasReferenceCameraPosition)
            return baseLocalPosition;

        Vector3 cameraDelta = followCamera.transform.position - referenceCameraPosition;

        if (!followCameraX)
            cameraDelta.x = 0f;

        if (!followCameraY)
            cameraDelta.y = 0f;

        if (!followCameraZ)
            cameraDelta.z = 0f;

        if (target.parent == null)
            return baseLocalPosition + cameraDelta;

        Vector3 baseWorldPosition = target.parent.TransformPoint(baseLocalPosition);
        Vector3 targetWorldPosition = baseWorldPosition + cameraDelta;
        return target.parent.InverseTransformPoint(targetWorldPosition);
    }
}
