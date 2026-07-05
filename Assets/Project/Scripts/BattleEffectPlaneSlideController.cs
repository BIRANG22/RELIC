using System.Collections;
using UnityEngine;

/// <summary>
/// 전투 진행 중 월드 배경 효과 Plane 2개를 중앙으로 모았다가,
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
    [SerializeField] private bool followCameraY = true;
    [SerializeField] private bool followCameraZ = false;
    [SerializeField] private bool updateFollowInLateUpdate = true;

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

        if (!followCameraPosition)
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
            leftPlane.localPosition = ApplyCameraDeltaToLocalPosition(leftPlane, leftBasePosition);
        }

        if (rightPlane != null)
        {
            Vector3 rightBattlePosition = GetBattlePosition(rightReservePosition);
            Vector3 rightBasePosition = Vector3.LerpUnclamped(rightReservePosition, rightBattlePosition, t);
            rightPlane.localPosition = ApplyCameraDeltaToLocalPosition(rightPlane, rightBasePosition);
        }
    }

    private Vector3 GetBattlePosition(Vector3 reservePosition)
    {
        reservePosition.x = battleCenterX;
        return reservePosition;
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
