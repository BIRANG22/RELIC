using System.Collections;
using UnityEngine;

/// <summary>
/// 전투 진행 중 월드 배경 효과 Plane 2개를 화면 중앙으로 모았다가,
/// 전투가 완전히 종료되면 원래 대기 위치로 되돌립니다.
/// BattleEffect 오브젝트에 붙이고 leftPlane/rightPlane에 Plane, Plane (1)을 연결해서 사용합니다.
/// </summary>
public class BattleEffectPlaneSlideController : MonoBehaviour
{
    public static BattleEffectPlaneSlideController Instance { get; private set; }

    [Header("Target Planes")]
    [SerializeField] private Transform leftPlane;
    [SerializeField] private Transform rightPlane;

    [Header("Battle Position")]
    [SerializeField] private float battleCenterX = 0f;
    [Tooltip("켜면 아래 각 Plane의 전투 중 목표 로컬 좌표를 사용합니다. 끄면 기존 Battle Center X를 사용합니다.")]
    [SerializeField] private bool useSpecifiedBattleLocalPositions = true;
    [Tooltip("전투 중 왼쪽 Plane의 목표 로컬 좌표입니다. 카메라 추적과 Z 줌에 따른 Y 보정은 별도로 적용됩니다.")]
    [SerializeField] private Vector3 leftBattleLocalPosition = new Vector3(0f, -4.5f, 0f);
    [Tooltip("전투 중 오른쪽 Plane의 목표 로컬 좌표입니다. 카메라 추적과 Z 줌에 따른 Y 보정은 별도로 적용됩니다.")]
    [SerializeField] private Vector3 rightBattleLocalPosition = new Vector3(0f, 5f, 0f);

    // These are captured from the actual objects' authored Transform.localPosition.
    // They are never populated from inspector reserve fields or re-captured during battle.
    private Vector3 leftOriginalLocalPosition;
    private Vector3 rightOriginalLocalPosition;

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

    [Header("Foreground Sorting")]
    [SerializeField, Min(1)] private int foregroundSortingOrderOffset = 100;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float moveDuration = 0.35f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool snapToReservePositionOnStart = true;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Application Focus Recovery")]
    [SerializeField] private bool resyncOnApplicationFocus = true;

    private Coroutine moveRoutine;
    private bool cachedPositions;
    private float slide01;
    private Vector3 referenceCameraPosition;
    private bool hasReferenceCameraPosition;
    private BattleTurnExecutor turnExecutor;

    [Header("Battle End Recovery")]
    [Tooltip("전투 완전 종료 시 두 Plane을 카메라 추적 보정 없이 원래 대기 좌표로 복귀시킵니다.")]
    [SerializeField] private bool resetPlanesOnBattleEnd = true;

    private bool battleEndResetApplied;
    private Coroutine battleEndReturnRoutine;
    private bool turnEndReturnCompleted;
    // Cache the exact local rotations of the planes controlled by this slide component.
    private Quaternion leftReserveRotation = Quaternion.identity;
    private Quaternion rightReserveRotation = Quaternion.identity;
    private bool reserveRotationsCaptured;
    private bool battleEndRotationRestored;

    [Header("Turn End Original Rotations")]
    [Tooltip("On: use the explicit local Euler angles below as each plane's exit rotation. Off: use each Plane's original local rotation captured at scene initialization.")]
    [SerializeField] private bool useSpecifiedBattleEndRotations = false;
    [Tooltip("Original LOCAL rotation for the left Plane before battle-end exit (degrees).")]
    [SerializeField] private Vector3 leftBattleEndOriginalRotation = Vector3.zero;
    [Tooltip("Original LOCAL rotation for the right Plane before battle-end exit (degrees).")]
    [SerializeField] private Vector3 rightBattleEndOriginalRotation = Vector3.zero;

    [Tooltip("한 턴 실행이 끝나 회전 복귀가 완료된 뒤, 원래 Transform 로컬 좌표까지 이동하는 시간(초)입니다.")]
    [SerializeField, Min(0f)] private float battleEndReturnDuration = 0.35f;
    [Tooltip("턴/전투 종료 복귀에 별도 곡선을 사용할지 여부입니다. 꺼져 있으면 기존 Move Curve를 사용합니다.")]
    [SerializeField] private bool useSeparateBattleEndReturnCurve = false;
    [SerializeField] private AnimationCurve battleEndReturnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("턴 실행 종료 시 Plane이 위치 복귀하기 전에 원래 회전값으로 돌아오는 시간(초). 0이면 즉시 복귀합니다.")]
    [SerializeField, Min(0f)] private float battleEndRotationReturnDuration = 0.12f;



    private void Awake()
    {
        Instance = this;

        ResolveFollowCamera();
        CaptureReferenceCameraPosition();
        CaptureReserveRotations();

        // Record the scene/prefab-authored local positions before any animation.
        CacheCurrentReservePositions();
    }

    private void Start()
    {
        ResolveFollowCamera();

        if (!hasReferenceCameraPosition)
            CaptureReferenceCameraPosition();

        if (!cachedPositions)
            CacheCurrentReservePositions();
        CaptureReserveRotations();

        if (snapToReservePositionOnStart)
            SetReservePositionInstant();
    }

    private void LateUpdate()
    {
        CheckBattleEndRecovery();

        // Maintain the exact saved local rotations once restoration completes.
        // Other camera-return callbacks cannot re-tilt a plane during its exit.
        if (battleEndReturnRoutine != null || (IsBattleFinished() && resetPlanesOnBattleEnd))
        {
            if (battleEndRotationRestored)
                ApplyReserveRotations();
            return;
        }

        if (!updateFollowInLateUpdate)
            return;

        // 카메라 위치 추적을 끈 상태라도, 카메라 Z 확대에 따른 Y 보간은 계속 갱신되어야 합니다.
        if (!followCameraPosition && !adjustYByCameraZ)
            return;

        // At idle, keep the original Transform coordinates, even if the camera moves.
        if (slide01 <= 0.0001f && moveRoutine == null)
        {
            if (leftPlane != null)
                leftPlane.localPosition = leftOriginalLocalPosition;
            if (rightPlane != null)
                rightPlane.localPosition = rightOriginalLocalPosition;
            return;
        }

        ApplyPlanePositions(slide01);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus || !resyncOnApplicationFocus || !isActiveAndEnabled)
            return;

        if (IsBattleFinished())
        {
            CheckBattleEndRecovery();
            return;
        }

        ResyncToCurrentBattleState();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus || !resyncOnApplicationFocus || !isActiveAndEnabled)
            return;

        if (IsBattleFinished())
        {
            CheckBattleEndRecovery();
            return;
        }

        ResyncToCurrentBattleState();
    }

    public bool TryGetForegroundSorting(out int sortingLayerId, out int sortingOrder)
    {
        sortingLayerId = 0;
        sortingOrder = 30000;

        Renderer bestRenderer = null;
        int bestLayerValue = int.MinValue;
        int bestOrder = int.MinValue;

        EvaluatePlaneRenderers(leftPlane, ref bestRenderer, ref bestLayerValue, ref bestOrder);
        EvaluatePlaneRenderers(rightPlane, ref bestRenderer, ref bestLayerValue, ref bestOrder);

        if (bestRenderer == null)
            return false;

        sortingLayerId = bestRenderer.sortingLayerID;
        sortingOrder = bestOrder + Mathf.Max(1, foregroundSortingOrderOffset);
        return true;
    }

    private static void EvaluatePlaneRenderers(
        Transform plane,
        ref Renderer bestRenderer,
        ref int bestLayerValue,
        ref int bestOrder)
    {
        if (plane == null)
            return;

        Renderer[] renderers = plane.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            int layerValue = SortingLayer.GetLayerValueFromID(renderer.sortingLayerID);

            if (bestRenderer == null ||
                layerValue > bestLayerValue ||
                (layerValue == bestLayerValue && renderer.sortingOrder > bestOrder))
            {
                bestRenderer = renderer;
                bestLayerValue = layerValue;
                bestOrder = renderer.sortingOrder;
            }
        }
    }

    private void OnEnable()
    {
        BattleTurnExecutor.BattleExecutionStarted -= MoveToBattleCenter;
        BattleTurnExecutor.BattleExecutionStarted += MoveToBattleCenter;
        BattleTurnExecutor.PlayerTurnReturned -= HandlePlayerTurnReturned;
        BattleTurnExecutor.PlayerTurnReturned += HandlePlayerTurnReturned;
    }

    private void OnDisable()
    {
        BattleTurnExecutor.BattleExecutionStarted -= MoveToBattleCenter;
        BattleTurnExecutor.PlayerTurnReturned -= HandlePlayerTurnReturned;
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }
        if (battleEndReturnRoutine != null)
        {
            StopCoroutine(battleEndReturnRoutine);
            battleEndReturnRoutine = null;
        }
        BattleActionForegroundRenderer.ClearAll();
    }

    private void HandlePlayerTurnReturned()
    {
        // The event fires after one complete reserved-action turn, not only on victory.
        // Victory has its own one-time path and must not restart the same animation.
        if (!isActiveAndEnabled || IsBattleFinished())
            return;

        BeginTurnEndReturn();
    }

    public void BeginTurnEndReturn()
    {
        if (!isActiveAndEnabled || IsBattleFinished() || turnEndReturnCompleted)
            return;

        StartRotationThenPositionReturn();
    }

    public void MoveToBattleCenter()
    {
        if (IsBattleFinished())
            return;

        if (!isActiveAndEnabled)
            return;

        if (!cachedPositions)
            CacheCurrentReservePositions();

        if (battleEndReturnRoutine != null)
        {
            StopCoroutine(battleEndReturnRoutine);
            battleEndReturnRoutine = null;
        }
        battleEndResetApplied = false;
        battleEndRotationRestored = false;
        turnEndReturnCompleted = false;
        BattleEffectPlaneRotation rotationController = BattleEffectPlaneRotation.Instance;
        if (rotationController != null)
            rotationController.UnlockRotationForNextTurn();
        PlayMove(1f);
    }

    // This API is also used by older turn-execution callers.
    public void MoveToReservePosition()
    {
        if (IsBattleFinished())
            BeginBattleEndReturn();
        else
            BeginTurnEndReturn();
    }

    public IEnumerator MoveToReserveAndWaitRoutine()
    {
        MoveToReservePosition();
        while (battleEndReturnRoutine != null && isActiveAndEnabled)
            yield return null;
    }

    // Called by BattleTurnExecutor on room end. Never bypass rotation restoration.
    public void BeginBattleEndReturn()
    {
        if (!isActiveAndEnabled || !resetPlanesOnBattleEnd || !IsBattleFinished())
            return;
        CheckBattleEndRecovery();
    }

    public void SetReservePositionInstant()
    {
        if (!cachedPositions)
            CacheCurrentReservePositions();

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        slide01 = 0f;
        // Do not apply camera-follow / zoom correction to the authored idle pose.
        if (leftPlane != null)
            leftPlane.localPosition = leftOriginalLocalPosition;
        if (rightPlane != null)
            rightPlane.localPosition = rightOriginalLocalPosition;
    }

    public void ForceResetToReservePositionInstant()
    {
        // During a completed battle, never interrupt rotation -> exit animation.
        // Keep this instant reset for scene initialization / emergency non-battle use.
        if (resetPlanesOnBattleEnd && IsBattleFinished())
        {
            BeginBattleEndReturn();
            return;
        }

        BattleActionForegroundRenderer.ClearAll();
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }
        if (battleEndReturnRoutine != null)
        {
            StopCoroutine(battleEndReturnRoutine);
            battleEndReturnRoutine = null;
        }
        if (!cachedPositions)
            CacheCurrentReservePositions();

        slide01 = 0f;
        if (leftPlane != null)
            leftPlane.localPosition = leftOriginalLocalPosition;
        if (rightPlane != null)
            rightPlane.localPosition = rightOriginalLocalPosition;
        battleEndRotationRestored = false;
        battleEndResetApplied = false;
    }

    private void CaptureReserveRotations()
    {
        if (reserveRotationsCaptured)
            return;

        if (leftPlane != null)
            leftReserveRotation = leftPlane.localRotation;
        if (rightPlane != null)
            rightReserveRotation = rightPlane.localRotation;
        reserveRotationsCaptured = true;
    }

    private void ApplyReserveRotations()
    {
        if (!reserveRotationsCaptured)
            CaptureReserveRotations();
        if (leftPlane != null)
            leftPlane.localRotation = leftReserveRotation;
        if (rightPlane != null)
            rightPlane.localRotation = rightReserveRotation;
    }

    private void CacheCurrentReservePositions()
    {
        // A battle action must never replace the return positions with an animated pose.
        if (cachedPositions)
            return;

        if (leftPlane == null && rightPlane == null)
            return;

        if (leftPlane != null)
            leftOriginalLocalPosition = leftPlane.localPosition;
        if (rightPlane != null)
            rightOriginalLocalPosition = rightPlane.localPosition;

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

    private static bool IsBattleFinished()
    {
        return BattleResultChecker.Instance != null && BattleResultChecker.Instance.BattleEnded;
    }

    private void CheckBattleEndRecovery()
    {
        if (!resetPlanesOnBattleEnd || !isActiveAndEnabled || !IsBattleFinished())
            return;
        if (battleEndResetApplied)
            return;

        // If the turn-end return was already initiated before victory, keep it.
        battleEndResetApplied = true;
        if (battleEndReturnRoutine == null)
            StartRotationThenPositionReturn();
    }

    // Single animation owner for a completed action turn and for battle victory:
    // restore rotation first, then return to scene-authored local positions.
    private void StartRotationThenPositionReturn()
    {
        if (!isActiveAndEnabled || battleEndReturnRoutine != null)
            return;
        if (!cachedPositions)
            CacheCurrentReservePositions();
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }
        BattleActionForegroundRenderer.ClearAll();
        battleEndRotationRestored = false;
        battleEndReturnRoutine = StartCoroutine(ReturnPlanesAfterBattleRoutine());
    }

    private IEnumerator ReturnPlanesAfterBattleRoutine()
    {
        // Phase 1: restore both planes' configured original rotations BEFORE moving.
        // Rotation owns localRotation and offsets; Slide owns localPosition.
        CaptureReserveRotations();
        BattleEffectPlaneRotation rotationController = BattleEffectPlaneRotation.Instance;
        if (rotationController != null)
        {
            // Stop all attack/camera-return rotation writers. The slide component
            // alone owns the configured ORIGINAL exit rotations from here onward.
            rotationController.LockRotationForBattleEnd();
        }

        // Do not overwrite the original authored/inspector rotations with the
        // Rotation component's idle values: they can intentionally be different.
        if (useSpecifiedBattleEndRotations)
        {
            leftReserveRotation = Quaternion.Euler(leftBattleEndOriginalRotation);
            rightReserveRotation = Quaternion.Euler(rightBattleEndOriginalRotation);
        }

        Quaternion leftFrom = leftPlane != null ? leftPlane.localRotation : leftReserveRotation;
        Quaternion rightFrom = rightPlane != null ? rightPlane.localRotation : rightReserveRotation;
        float rotationDuration = Mathf.Max(0f, battleEndRotationReturnDuration);
        AnimationCurve rotationCurve = useSeparateBattleEndReturnCurve ? battleEndReturnCurve : moveCurve;
        float rotationElapsed = 0f;
        while (rotationElapsed < rotationDuration)
        {
            rotationElapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(rotationElapsed / Mathf.Max(0.0001f, rotationDuration));
            float eased = rotationCurve != null ? Mathf.Clamp01(rotationCurve.Evaluate(t)) : t;
            if (leftPlane != null)
                leftPlane.localRotation = Quaternion.SlerpUnclamped(leftFrom, leftReserveRotation, eased);
            if (rightPlane != null)
                rightPlane.localRotation = Quaternion.SlerpUnclamped(rightFrom, rightReserveRotation, eased);
            // Keep both positions unchanged until rotation restoration finishes.
            yield return null;
        }

        // A different LateUpdate may have written after the last interpolation
        // frame. Explicitly apply the targets, then verify on the next frame.
        ApplyReserveRotations();
        yield return null;
        ApplyReserveRotations();
        battleEndRotationRestored = true;

        // Phase 2: move straight from the current local positions to the authored
        // original Transform.localPosition. No X-axis exit, hidden second stage,
        // screen-space conversion, or camera-follow offsets are used here.
        Vector3 leftStart = leftPlane != null ? leftPlane.localPosition : leftOriginalLocalPosition;
        Vector3 rightStart = rightPlane != null ? rightPlane.localPosition : rightOriginalLocalPosition;
        float duration = Mathf.Max(0f, battleEndReturnDuration);
        AnimationCurve curve = useSeparateBattleEndReturnCurve ? battleEndReturnCurve : moveCurve;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
            float eased = curve != null ? Mathf.Clamp01(curve.Evaluate(t)) : t;
            if (leftPlane != null)
                leftPlane.localPosition = Vector3.LerpUnclamped(leftStart, leftOriginalLocalPosition, eased);
            if (rightPlane != null)
                rightPlane.localPosition = Vector3.LerpUnclamped(rightStart, rightOriginalLocalPosition, eased);
            ApplyReserveRotations();
            yield return null;
        }

        // Assign exact authored positions at completion to avoid interpolation drift.
        slide01 = 0f;
        if (leftPlane != null)
            leftPlane.localPosition = leftOriginalLocalPosition;
        if (rightPlane != null)
            rightPlane.localPosition = rightOriginalLocalPosition;
        ApplyReserveRotations();
        turnEndReturnCompleted = !IsBattleFinished();
        battleEndReturnRoutine = null;
    }

    private void ResyncToCurrentBattleState()
    {
        if (IsBattleFinished())
        {
            CheckBattleEndRecovery();
            return;
        }

        if (turnExecutor == null)
            turnExecutor = FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Exclude);

        if (turnExecutor == null)
            return;

        // 게임 창의 포커스를 잃는 동안 코루틴이나 이벤트 타이밍이 끊겨도,
        // 현재 전투 상태를 기준으로 목표 위치를 다시 잡아 고정되는 현상을 복구합니다.
        if (turnExecutor.IsExecuting)
            MoveToBattleCenter();
        // 플레이어 턴 복귀 시에는 중앙 상태를 유지합니다.
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
            if (resetPlanesOnBattleEnd && IsBattleFinished())
            {
                moveRoutine = null;
                yield break;
            }
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curveT = moveCurve != null ? moveCurve.Evaluate(t) : t;

            slide01 = Mathf.LerpUnclamped(startSlide01, targetSlide01, curveT);
            ApplyPlanePositions(slide01);

            yield return null;
        }

        if (resetPlanesOnBattleEnd && IsBattleFinished())
        {
            moveRoutine = null;
            yield break;
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

        // The rotation-first return owns positions through the entire animation.
        if (battleEndReturnRoutine != null || (resetPlanesOnBattleEnd && IsBattleFinished()))
            return;

        if (followCameraPosition && !hasReferenceCameraPosition)
            CaptureReferenceCameraPosition();

        if (leftPlane != null)
        {
            Vector3 leftBattlePosition = useSpecifiedBattleLocalPositions
                ? leftBattleLocalPosition : GetBattlePosition(leftOriginalLocalPosition);
            Vector3 leftBasePosition = Vector3.LerpUnclamped(leftOriginalLocalPosition, leftBattlePosition, t);
            // Only correct the Y of the battle pose; the start pose must remain authored.
            if (adjustYByCameraZ)
                leftBasePosition.y = Mathf.LerpUnclamped(leftOriginalLocalPosition.y,
                    GetCameraZoomAdjustedY(leftBattlePosition.y, true), t);
            // 공격 연출은 위치를 직접 변경하지 않고 오프셋만 전달합니다.
            if (BattleEffectPlaneRotation.Instance != null)
                leftBasePosition += BattleEffectPlaneRotation.Instance.Plane1PositionOffset;
            leftPlane.localPosition = ApplyCameraDeltaToLocalPosition(leftPlane, leftBasePosition);
        }

        if (rightPlane != null)
        {
            Vector3 rightBattlePosition = useSpecifiedBattleLocalPositions
                ? rightBattleLocalPosition : GetBattlePosition(rightOriginalLocalPosition);
            Vector3 rightBasePosition = Vector3.LerpUnclamped(rightOriginalLocalPosition, rightBattlePosition, t);
            if (adjustYByCameraZ)
                rightBasePosition.y = Mathf.LerpUnclamped(rightOriginalLocalPosition.y,
                    GetCameraZoomAdjustedY(rightBattlePosition.y, false), t);
            if (BattleEffectPlaneRotation.Instance != null)
                rightBasePosition += BattleEffectPlaneRotation.Instance.Plane2PositionOffset;
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
