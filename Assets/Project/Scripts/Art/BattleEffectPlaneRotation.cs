using System.Collections;
using UnityEngine;

public enum PlaneAttackType
{
    Melee,
    Ranged
}

/// <summary>
/// 전투 중 공격/충돌 시 Plane의 회전과 Noise Flow를 제어하고 위치 오프셋만 전달합니다.
/// 공격이 연속 등록된 경우 첫 공격에서 변경된 상태를 유지하고 마지막 공격이 끝나면 복구합니다.
/// </summary>
public class BattleEffectPlaneRotation : MonoBehaviour
{
    public static BattleEffectPlaneRotation Instance { get; private set; }

    [Header("대상 Plane")]
    [Tooltip("첫 번째 Plane입니다.")]
    [SerializeField] private Transform plane1;

    [Tooltip("두 번째 Plane입니다.")]
    [SerializeField] private Transform plane2;

    [Header("기본값 자동 저장")]
    [Tooltip("타격 위치 오프셋 계산 기준으로 시작 로컬 위치를 저장합니다.")]
    [SerializeField] private bool useCurrentPositionAsIdle = true;

    [Tooltip("게임 시작 시 Plane의 현재 로컬 회전을 기본 회전으로 저장합니다.")]
    [SerializeField] private bool useCurrentRotationAsIdle = true;

    [Header("기본 위치")]
    [SerializeField] private Vector3 plane1IdlePosition;
    [SerializeField] private Vector3 plane2IdlePosition;

    [Header("기본 회전")]
    [SerializeField] private Vector3 plane1IdleRotation;
    [SerializeField] private Vector3 plane2IdleRotation;

    [Header("공격 타입별 X 회전 변화량")]
    [Tooltip("근거리 공격 시 기본 X 회전에서 더하거나 뺄 값입니다. -10 또는 +10 중 하나가 적용됩니다.")]
    [SerializeField] private float meleeRotationAmount = 10f;

    [Tooltip("원거리 공격 시 기본 X 회전에서 더하거나 뺄 값입니다. -4 또는 +4 중 하나가 적용됩니다.")]
    [SerializeField] private float rangedRotationAmount = 4f;

    [Tooltip("충돌 피해 시 기본 X 회전에서 더하거나 뺄 값입니다. -2 또는 +2 중 하나가 적용됩니다.")]
    [SerializeField] private float collisionRotationAmount = 2f;

    [Header("공격 시 위치 변화")]
    [Tooltip("공격/충돌 연출 시 Plane의 기본 Y 위치에 더할 값입니다. -2.8 기준 -3.2가 됩니다.")]
    [SerializeField] private float plane1HitYOffset = -0.4f;

    [Tooltip("공격/충돌 연출 시 Plane (1)의 기본 Y 위치에 더할 값입니다. 3.2 기준 3.4가 됩니다.")]
    [SerializeField] private float plane2HitYOffset = 0.2f;

    [Header("Noise Flow 머티리얼")]
    [Tooltip("Noise Flow 값을 변경할 머티리얼입니다.")]
    [SerializeField] private Material noiseFlowMaterial;

    [Tooltip("Shader Graph에 설정된 Noise Flow의 Reference 이름입니다.")]
    [SerializeField] private string noiseFlowPropertyName = "_noiseflow";

    [Tooltip("게임 시작 시 머티리얼의 현재 Noise Flow X값을 기본값으로 저장합니다.")]
    [SerializeField] private bool useCurrentNoiseFlowXAsIdle = true;

    [Tooltip("평소 Noise Flow X값입니다.")]
    [SerializeField] private float idleNoiseFlowX = 0.01f;

    [Tooltip("공격/충돌 연출 시 사용할 기본 Noise Flow X값입니다.")]
    [SerializeField] private float hitNoiseFlowX = 1f;

    [Tooltip("공격/충돌마다 Noise Flow X에 더할 최소 랜덤값입니다.")]
    [SerializeField] private float randomNoiseFlowXMin = -0.2f;

    [Tooltip("공격/충돌마다 Noise Flow X에 더할 최대 랜덤값입니다.")]
    [SerializeField] private float randomNoiseFlowXMax = 0.2f;

    [Tooltip("연출 종료 후 Noise Flow 값을 기본값으로 되돌립니다.")]
    [SerializeField] private bool restoreNoiseFlowAfterHit = true;

    [Header("Hit Noise Flow Pulse")]
    [Tooltip("실제 타격 순간 Hit Noise Flow X를 유지하는 시간(초)입니다. 시간이 지나면 Idle 값으로 돌아옵니다.")]
    [SerializeField, Min(0f)] private float hitNoiseFlowPulseDuration = 0.10f;

    [Header("연출 시간")]
    [Tooltip("공격 위치와 회전으로 움직이는 시간입니다.")]
    [SerializeField] private float moveOutDuration = 0.08f;

    [Tooltip("충돌 피해 연출에서 변경된 상태를 유지하는 시간입니다.")]
    [SerializeField] private float holdDuration = 0.3f;

    [Tooltip("기본 위치와 회전으로 돌아오는 시간입니다.")]
    [SerializeField] private float returnDuration = 0.18f;

    [Header("연출 곡선")]
    [SerializeField]
    private AnimationCurve moveOutCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField]
    private AnimationCurve returnCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("디버그")]
    [Tooltip("연출 타입과 최종 회전값을 Console에 표시합니다.")]
    [SerializeField] private bool showDebugLog;

    private Quaternion plane1IdleQuaternion;
    private Quaternion plane2IdleQuaternion;
    private Coroutine hitRoutine;

    private bool persistentAttackActive;
    private bool releasePersistentAttackRequested;
    private float syncedReturnDuration = -1f;
    private AnimationCurve syncedReturnCurve;
    private int lastCollisionFeedbackFrame = -1;
    private float lastAttackDirection;
    private string currentActionOwnerKey = string.Empty;
    private bool cameraReturnDriven;
    private bool battleEndRotationInProgress;
    private bool battleEndRotationLocked;
    private Vector3 cameraReturnPlane1FromPosition;
    private Vector3 cameraReturnPlane2FromPosition;
    private Quaternion cameraReturnPlane1FromRotation;
    private Quaternion cameraReturnPlane2FromRotation;

    // 위치의 최종 소유자는 BattleEffectPlaneSlideController입니다.
    // 기존 타격 Y 이동은 여기서 오프셋만 계산해 SlideController가 반영합니다.
    public Vector3 Plane1PositionOffset { get; private set; }
    public Vector3 Plane2PositionOffset { get; private set; }

    // Expose the actual idle rotation of the planes used by the attack rotation system.
    public bool TryGetIdleRotation(Transform target, out Quaternion idleRotation)
    {
        if (target != null && target == plane1)
        {
            idleRotation = plane1IdleQuaternion;
            return true;
        }
        if (target != null && target == plane2)
        {
            idleRotation = plane2IdleQuaternion;
            return true;
        }
        idleRotation = Quaternion.identity;
        return false;
    }

    // Stop all attack/camera-return writers before the slide controller starts
    // the battle-end rotation phase. The slide controller owns the final poses.
    public void LockRotationForBattleEnd()
    {
        battleEndRotationInProgress = true;
        battleEndRotationLocked = true;
        StopCurrentRoutineWithoutReset();
        persistentAttackActive = false;
        releasePersistentAttackRequested = false;
        cameraReturnDriven = false;
        lastAttackDirection = 0f;
        currentActionOwnerKey = string.Empty;
        syncedReturnDuration = -1f;
        syncedReturnCurve = null;
        Plane1PositionOffset = Vector3.zero;
        Plane2PositionOffset = Vector3.zero;
        StopHitNoiseFlowPulse();
    }

    // The slide controller locks rotation writers while returning to the idle pose.
    // Re-enable attack/collision rotation effects only when a new execution begins.
    public void UnlockRotationForNextTurn()
    {
        if (BattleResultChecker.Instance != null && BattleResultChecker.Instance.BattleEnded)
            return;
        battleEndRotationInProgress = false;
        battleEndRotationLocked = false;
    }

    private Vector3 activePlane1Position;
    private Vector3 activePlane2Position;
    private Quaternion activePlane1Rotation;
    private Quaternion activePlane2Rotation;
    private Coroutine noiseFlowPulseRoutine;

    private int noiseFlowPropertyId;
    private bool hasNoiseFlowProperty;
    private Vector4 idleNoiseFlowVector;

    private void Awake()
    {
        RegisterInstance();
        FindPlanesAutomatically();
        SaveIdleValues();
        InitializeNoiseFlow();
        ApplyIdleValues();
    }

    private void OnEnable()
    {
        RegisterInstance();
        if (BattleResultChecker.Instance == null || !BattleResultChecker.Instance.BattleEnded)
        {
            battleEndRotationInProgress = false;
            battleEndRotationLocked = false;
        }
    }

    private void OnDisable()
    {
        StopHitEffect();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void RegisterInstance()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[BattleEffectPlaneRotation] 컴포넌트가 두 개 이상 존재합니다.",
                this);
        }

        Instance = this;
    }

    /// <summary>
    /// 이전 호출부 호환용입니다. 단독 근거리 타격 연출을 재생합니다.
    /// </summary>
    public static void PlayHitRotationFeedback()
    {
        if (!TryResolveController(out BattleEffectPlaneRotation controller))
            return;

        controller.PlaySingleAttackEffect(PlaneAttackType.Melee);
    }

    /// <summary>
    /// 공격 실행 시작 시 호출합니다. 같은 공격이 연속 등록된 경우 첫 실행에서만 호출합니다.
    /// </summary>
    public static void BeginAttackRotationFeedback(PlaneAttackType attackType)
    {
        if (!TryResolveController(out BattleEffectPlaneRotation controller))
            return;

        controller.BeginPersistentAttackEffect(attackType);
    }

    /// <summary>
    /// 공격 실행 종료 시 호출합니다. 연속 등록된 경우 마지막 공격이 끝날 때 호출합니다.
    /// </summary>
    public static void EndAttackRotationFeedback()
    {
        if (!TryResolveController(out BattleEffectPlaneRotation controller))
            return;

        controller.EndPersistentAttackEffect();
    }

    /// <summary>
    /// 충돌 피해 발생 시 ±2도 연출을 한 번 재생합니다.
    /// </summary>
    public static void PlayCollisionRotationFeedback()
    {
        if (!TryResolveController(out BattleEffectPlaneRotation controller))
            return;

        controller.PlayCollisionEffect();
    }

    /// <summary>피격 임팩트가 발생한 순간 Noise Flow를 잠깐 빠르게 한 후 자동 복귀합니다.</summary>
    public static void PlayHitNoiseFlowPulse()
    {
        if (!TryResolveController(out BattleEffectPlaneRotation controller))
            return;
        controller.StartHitNoiseFlowPulse();
    }

    private static bool TryResolveController(out BattleEffectPlaneRotation controller)
    {
        controller = Instance;

        if (controller == null)
        {
            controller = FindFirstObjectByType<BattleEffectPlaneRotation>(
                FindObjectsInactive.Include);

            if (controller != null)
                Instance = controller;
        }

        if (controller == null ||
            !controller.gameObject.activeInHierarchy ||
            !controller.enabled)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 카메라 줌아웃이 시작될 때 호출합니다. 공격 연출의 복귀를 같은 길이와 곡선으로 맞춥니다.
    /// </summary>
    public static bool HasActiveAttackRotationFeedback()
    {
        return TryResolveController(out BattleEffectPlaneRotation controller) &&
               controller.persistentAttackActive;
    }

    public static void BeginCameraReturnFeedback(float duration, AnimationCurve curve)
    {
        if (!TryResolveController(out BattleEffectPlaneRotation controller))
            return;

        controller.SynchronizeCameraReturn(duration, curve);
    }

    /// <summary>
    /// 카메라가 실제로 이동한 보간값을 전달받아 같은 프레임에 Plane도 복귀시킵니다.
    /// </summary>
    public static void UpdateCameraReturnFeedback(float curvedProgress)
    {
        if (!TryResolveController(out BattleEffectPlaneRotation controller))
            return;

        controller.ApplyCameraReturnProgress(curvedProgress);
    }

    private void SynchronizeCameraReturn(float duration, AnimationCurve curve)
    {
        if (!persistentAttackActive)
            return;

        // 각자 코루틴을 진행하면 실행 순서에 따라 한 프레임 늦을 수 있으므로
        // 카메라 이동 루프에서 직접 동일한 보간값을 전달받습니다.
        StopCurrentRoutineWithoutReset();
        cameraReturnPlane1FromPosition = plane1IdlePosition + Plane1PositionOffset;
        cameraReturnPlane2FromPosition = plane2IdlePosition + Plane2PositionOffset;
        cameraReturnPlane1FromRotation = plane1 != null ? plane1.localRotation : activePlane1Rotation;
        cameraReturnPlane2FromRotation = plane2 != null ? plane2.localRotation : activePlane2Rotation;
        cameraReturnDriven = true;
        releasePersistentAttackRequested = false;
    }

    private void ApplyCameraReturnProgress(float progress)
    {
        if (battleEndRotationInProgress || battleEndRotationLocked)
            return;
        if (!cameraReturnDriven)
            return;

        float t = Mathf.Clamp01(progress);
        ApplyValues(
            Vector3.LerpUnclamped(cameraReturnPlane1FromPosition, plane1IdlePosition, t),
            Quaternion.SlerpUnclamped(cameraReturnPlane1FromRotation, plane1IdleQuaternion, t),
            Vector3.LerpUnclamped(cameraReturnPlane2FromPosition, plane2IdlePosition, t),
            Quaternion.SlerpUnclamped(cameraReturnPlane2FromRotation, plane2IdleQuaternion, t));

        if (t >= 1f)
        {
            cameraReturnDriven = false;
            persistentAttackActive = false;
            releasePersistentAttackRequested = false;
            ApplyIdleValues();

        }
    }

    /// <summary>
    /// 같은 유닛의 후속 행동은 기울어진 상태를 유지합니다.
    /// 다른 유닛으로 행동 주체가 바뀌면 기본 회전으로 돌아간 뒤 행동을 시작합니다.
    /// </summary>
    public IEnumerator PrepareForActionOwner(string ownerKey)
    {
        if (string.IsNullOrEmpty(ownerKey) ||
            (BattleResultChecker.Instance != null && BattleResultChecker.Instance.BattleEnded))
            yield break;

        if (string.Equals(currentActionOwnerKey, ownerKey, System.StringComparison.Ordinal))
            yield break;

        bool hadPreviousOwner = !string.IsNullOrEmpty(currentActionOwnerKey);
        currentActionOwnerKey = ownerKey;
        if (!hadPreviousOwner)
        {
            lastAttackDirection = 0f;
            yield break;
        }

        FindPlanesAutomatically();
        StopCurrentRoutineWithoutReset();
        cameraReturnDriven = false;
        persistentAttackActive = false;
        releasePersistentAttackRequested = false;
        syncedReturnDuration = -1f;
        syncedReturnCurve = null;

        Quaternion from1 = plane1 != null ? plane1.localRotation : plane1IdleQuaternion;
        Quaternion from2 = plane2 != null ? plane2.localRotation : plane2IdleQuaternion;
        float duration = Mathf.Max(0f, returnDuration);
        float elapsed = 0f;
        while (elapsed < duration && !battleEndRotationLocked && !battleEndRotationInProgress)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
            t = returnCurve != null ? Mathf.Clamp01(returnCurve.Evaluate(t)) : t;
            if (plane1 != null)
                plane1.localRotation = Quaternion.SlerpUnclamped(from1, plane1IdleQuaternion, t);
            if (plane2 != null)
                plane2.localRotation = Quaternion.SlerpUnclamped(from2, plane2IdleQuaternion, t);
            yield return null;
        }
        if (!battleEndRotationLocked && !battleEndRotationInProgress)
        {
            if (plane1 != null) plane1.localRotation = plane1IdleQuaternion;
            if (plane2 != null) plane2.localRotation = plane2IdleQuaternion;
        }
        lastAttackDirection = 0f;
        Plane1PositionOffset = Vector3.zero;
        Plane2PositionOffset = Vector3.zero;
    }

    private void BeginPersistentAttackEffect(PlaneAttackType attackType)
    {
        if (BattleResultChecker.Instance != null && BattleResultChecker.Instance.BattleEnded)
            return;
        battleEndRotationLocked = false;
        battleEndRotationInProgress = false;

        FindPlanesAutomatically();

        if (plane1 == null && plane2 == null)
            return;

        // 같은 줌 상태에서 공격/반격이 이어져도 새 행동마다 회전을 갱신합니다.
        // 기본 각도로 되돌리지 않고 현재 각도에서 반대 방향으로 전환합니다.
        Vector3 from1Position = plane1IdlePosition + Plane1PositionOffset;
        Vector3 from2Position = plane2IdlePosition + Plane2PositionOffset;
        Quaternion from1Rotation = plane1 != null ? plane1.localRotation : plane1IdleQuaternion;
        Quaternion from2Rotation = plane2 != null ? plane2.localRotation : plane2IdleQuaternion;
        StopCurrentRoutineWithoutReset();
        cameraReturnDriven = false;
        persistentAttackActive = true;
        releasePersistentAttackRequested = false;
        syncedReturnDuration = -1f;
        syncedReturnCurve = null;

        float amount = attackType == PlaneAttackType.Ranged
            ? rangedRotationAmount
            : meleeRotationAmount;
        lastAttackDirection = lastAttackDirection == 0f
            ? (Random.value < 0.5f ? -1f : 1f)
            : -lastAttackDirection;
        PrepareActiveValues(amount, attackType.ToString(), lastAttackDirection);
        hitRoutine = StartCoroutine(PlayPersistentAttackRoutine(
            from1Position, from1Rotation, from2Position, from2Rotation));
    }

    private void EndPersistentAttackEffect()
    {
        if (!persistentAttackActive || cameraReturnDriven)
            return;

        // 카메라가 확대 중이면, 공격 종료 직후가 아니라 줌아웃 시작에 맞춰 복귀합니다.
        BattleCameraController cameraController = BattleCameraController.Instance;
        if (cameraController != null && cameraController.IsCombatZoomActive)
        {
            return;
        }

        releasePersistentAttackRequested = true;
    }

    private void PlaySingleAttackEffect(PlaneAttackType attackType)
    {
        if (BattleResultChecker.Instance != null && BattleResultChecker.Instance.BattleEnded)
            return;

        if (persistentAttackActive)
            return;

        StopCurrentRoutineWithoutReset();
        ApplyIdleValues();

        float rotationAmount = attackType == PlaneAttackType.Ranged
            ? rangedRotationAmount
            : meleeRotationAmount;

        PrepareActiveValues(rotationAmount, attackType.ToString());
        hitRoutine = StartCoroutine(PlayOneShotRoutine());
    }

    private void PlayCollisionEffect()
    {
        if (BattleResultChecker.Instance != null && BattleResultChecker.Instance.BattleEnded)
            return;

        StartHitNoiseFlowPulse();

        // 공격 연출 유지 중에는 공격의 Plane 상태를 우선합니다.
        if (persistentAttackActive)
            return;

        // 한 번의 충돌에서 양쪽 유닛이 같은 프레임에 피해를 받아도
        // Plane 연출은 한 번만 실행합니다.
        if (lastCollisionFeedbackFrame == Time.frameCount)
            return;

        lastCollisionFeedbackFrame = Time.frameCount;
        StopCurrentRoutineWithoutReset();
        ApplyIdleValues();
        PrepareActiveValues(collisionRotationAmount, "Collision");
        hitRoutine = StartCoroutine(PlayOneShotRoutine());
    }

    private void PrepareActiveValues(float rotationAmount, string presentationName, float forcedDirection = 0f)
    {
        float direction = forcedDirection == 0f
            ? (Random.value < 0.5f ? -1f : 1f)
            : forcedDirection;
        float signedAmount = Mathf.Abs(rotationAmount) * direction;

        activePlane1Position = plane1IdlePosition;
        activePlane1Position.y += plane1HitYOffset;

        activePlane2Position = plane2IdlePosition;
        activePlane2Position.y += plane2HitYOffset;

        Vector3 plane1Rotation = plane1IdleRotation;
        plane1Rotation.x += signedAmount;

        Vector3 plane2Rotation = plane2IdleRotation;
        plane2Rotation.x += signedAmount;

        activePlane1Rotation = Quaternion.Euler(plane1Rotation);
        activePlane2Rotation = Quaternion.Euler(plane2Rotation);

        if (showDebugLog)
        {
            Debug.Log(
                $"[BattleEffectPlaneRotation] {presentationName} / " +
                $"변화량:{signedAmount} / " +
                $"Plane X:{plane1Rotation.x} / Plane (1) X:{plane2Rotation.x}",
                this);
        }
    }

    private IEnumerator PlayPersistentAttackRoutine(
        Vector3 from1Position, Quaternion from1Rotation,
        Vector3 from2Position, Quaternion from2Rotation)
    {
        yield return AnimateValues(
            from1Position,
            activePlane1Position,
            from1Rotation,
            activePlane1Rotation,
            from2Position,
            activePlane2Position,
            from2Rotation,
            activePlane2Rotation,
            moveOutDuration,
            moveOutCurve);

        while (!releasePersistentAttackRequested)
        {
            ApplyActiveValues();
            yield return null;
        }

        yield return ReturnToIdleRoutine(syncedReturnDuration, syncedReturnCurve);

        persistentAttackActive = false;
        releasePersistentAttackRequested = false;
        syncedReturnDuration = -1f;
        syncedReturnCurve = null;
        hitRoutine = null;
    }

    private IEnumerator PlayOneShotRoutine()
    {
        yield return AnimateValues(
            plane1IdlePosition,
            activePlane1Position,
            plane1IdleQuaternion,
            activePlane1Rotation,
            plane2IdlePosition,
            activePlane2Position,
            plane2IdleQuaternion,
            activePlane2Rotation,
            moveOutDuration,
            moveOutCurve);

        yield return HoldHitValues(
            activePlane1Position,
            activePlane1Rotation,
            activePlane2Position,
            activePlane2Rotation,
            holdDuration);

        yield return ReturnToIdleRoutine();
        hitRoutine = null;
    }

    private IEnumerator ReturnToIdleRoutine(float durationOverride = -1f, AnimationCurve curveOverride = null)
    {
        Vector3 plane1FromPosition = plane1IdlePosition + Plane1PositionOffset;
        Quaternion plane1FromRotation = plane1 != null
            ? plane1.localRotation
            : activePlane1Rotation;

        Vector3 plane2FromPosition = plane2IdlePosition + Plane2PositionOffset;
        Quaternion plane2FromRotation = plane2 != null
            ? plane2.localRotation
            : activePlane2Rotation;

        yield return AnimateValues(
            plane1FromPosition,
            plane1IdlePosition,
            plane1FromRotation,
            plane1IdleQuaternion,
            plane2FromPosition,
            plane2IdlePosition,
            plane2FromRotation,
            plane2IdleQuaternion,
            durationOverride >= 0f ? durationOverride : returnDuration,
            curveOverride ?? returnCurve);

        ApplyIdleValues();
    }

    private void ApplyActiveValues()
    {
        ApplyValues(
            activePlane1Position,
            activePlane1Rotation,
            activePlane2Position,
            activePlane2Rotation);

    }

    /// <summary>
    /// At battle end, return both planes to their stored idle rotations before
    /// SlideController moves either plane off screen. Does not write positions.
    /// </summary>
    public IEnumerator RestoreForBattleEnd(
        float duration, AnimationCurve curve,
        Transform exitPlane1 = null, Quaternion? exitPlane1Idle = null,
        Transform exitPlane2 = null, Quaternion? exitPlane2Idle = null)
    {
        // This controller must not re-tilt either plane after battle end.
        battleEndRotationInProgress = true;
        battleEndRotationLocked = false;
        StopCurrentRoutineWithoutReset();
        persistentAttackActive = false;
        releasePersistentAttackRequested = false;
        cameraReturnDriven = false;
        lastAttackDirection = 0f;
        currentActionOwnerKey = string.Empty;
        syncedReturnDuration = -1f;
        syncedReturnCurve = null;
        Plane1PositionOffset = Vector3.zero;
        Plane2PositionOffset = Vector3.zero;
        StopHitNoiseFlowPulse();

        Transform actual1 = exitPlane1 != null ? exitPlane1 : plane1;
        Transform actual2 = exitPlane2 != null ? exitPlane2 : plane2;
        Quaternion idle1 = exitPlane1Idle ?? plane1IdleQuaternion;
        Quaternion idle2 = exitPlane2Idle ?? plane2IdleQuaternion;
        Quaternion from1 = actual1 != null ? actual1.localRotation : idle1;
        Quaternion from2 = actual2 != null ? actual2.localRotation : idle2;
        float safeDuration = Mathf.Max(0f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, safeDuration));
            float eased = curve != null ? Mathf.Clamp01(curve.Evaluate(t)) : t;
            if (actual1 != null)
                actual1.localRotation = Quaternion.SlerpUnclamped(from1, idle1, eased);
            if (actual2 != null)
                actual2.localRotation = Quaternion.SlerpUnclamped(from2, idle2, eased);
            yield return null;
        }

        if (actual1 != null)
            actual1.localRotation = idle1;
        if (actual2 != null)
            actual2.localRotation = idle2;
        battleEndRotationInProgress = false;
        battleEndRotationLocked = true;
    }

    public void StopHitEffect()
    {
        persistentAttackActive = false;
        releasePersistentAttackRequested = false;
        cameraReturnDriven = false;
        lastAttackDirection = 0f;
        currentActionOwnerKey = string.Empty;
        syncedReturnDuration = -1f;
        syncedReturnCurve = null;
        StopCurrentRoutineWithoutReset();
        if (!battleEndRotationInProgress && !battleEndRotationLocked)
            ApplyIdleValues();
        else
        {
            Plane1PositionOffset = Vector3.zero;
            Plane2PositionOffset = Vector3.zero;
        }

        StopHitNoiseFlowPulse();
    }

    private void StopCurrentRoutineWithoutReset()
    {
        if (hitRoutine == null)
            return;

        StopCoroutine(hitRoutine);
        hitRoutine = null;
    }

    private void StartHitNoiseFlowPulse()
    {
        if (!hasNoiseFlowProperty || noiseFlowMaterial == null ||
            (BattleResultChecker.Instance != null && BattleResultChecker.Instance.BattleEnded))
            return;

        if (noiseFlowPulseRoutine != null)
        {
            StopCoroutine(noiseFlowPulseRoutine);
            noiseFlowPulseRoutine = null;
        }

        // 연속 타격은 타격할 때마다 유지 시간을 새로 시작합니다.
        SetNoiseFlowX(CreateHitNoiseFlowX());
        if (hitNoiseFlowPulseDuration <= 0f)
        {
            RestoreNoiseFlow();
            return;
        }
        noiseFlowPulseRoutine = StartCoroutine(HitNoiseFlowPulseRoutine());
    }

    private IEnumerator HitNoiseFlowPulseRoutine()
    {
        float elapsed = 0f;
        while (elapsed < hitNoiseFlowPulseDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        RestoreNoiseFlow();
        noiseFlowPulseRoutine = null;
    }

    private void StopHitNoiseFlowPulse()
    {
        if (noiseFlowPulseRoutine != null)
        {
            StopCoroutine(noiseFlowPulseRoutine);
            noiseFlowPulseRoutine = null;
        }
        if (restoreNoiseFlowAfterHit)
            RestoreNoiseFlow();
    }

    private float CreateHitNoiseFlowX()
    {
        float minimum = Mathf.Min(randomNoiseFlowXMin, randomNoiseFlowXMax);
        float maximum = Mathf.Max(randomNoiseFlowXMin, randomNoiseFlowXMax);
        return hitNoiseFlowX + Random.Range(minimum, maximum);
    }

    private IEnumerator AnimateValues(
        Vector3 plane1PositionFrom,
        Vector3 plane1PositionTo,
        Quaternion plane1RotationFrom,
        Quaternion plane1RotationTo,
        Vector3 plane2PositionFrom,
        Vector3 plane2PositionTo,
        Quaternion plane2RotationFrom,
        Quaternion plane2RotationTo,
        float duration,
        AnimationCurve curve)
    {
        if (duration <= 0f)
        {
            ApplyValues(
                plane1PositionTo,
                plane1RotationTo,
                plane2PositionTo,
                plane2RotationTo);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float evaluatedTime = curve != null
                ? curve.Evaluate(normalizedTime)
                : normalizedTime;

            ApplyValues(
                Vector3.LerpUnclamped(plane1PositionFrom, plane1PositionTo, evaluatedTime),
                Quaternion.SlerpUnclamped(plane1RotationFrom, plane1RotationTo, evaluatedTime),
                Vector3.LerpUnclamped(plane2PositionFrom, plane2PositionTo, evaluatedTime),
                Quaternion.SlerpUnclamped(plane2RotationFrom, plane2RotationTo, evaluatedTime));

            yield return null;
        }

        ApplyValues(
            plane1PositionTo,
            plane1RotationTo,
            plane2PositionTo,
            plane2RotationTo);
    }

    private IEnumerator HoldHitValues(
        Vector3 plane1Position,
        Quaternion plane1Rotation,
        Vector3 plane2Position,
        Quaternion plane2Rotation,
        float duration)
    {
        if (duration <= 0f)
            yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            ApplyValues(
                plane1Position,
                plane1Rotation,
                plane2Position,
                plane2Rotation);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void ApplyValues(
        Vector3 plane1Position,
        Quaternion plane1Rotation,
        Vector3 plane2Position,
        Quaternion plane2Rotation)
    {
        if (battleEndRotationInProgress || battleEndRotationLocked)
            return;
        // 변경된 위치를 직접 기록하지 않습니다. SlideController가 다음 LateUpdate에서 합성합니다.
        Plane1PositionOffset = plane1Position - plane1IdlePosition;
        Plane2PositionOffset = plane2Position - plane2IdlePosition;

        if (plane1 != null)
            plane1.localRotation = plane1Rotation;

        if (plane2 != null)
            plane2.localRotation = plane2Rotation;
    }

    private void ApplyIdleValues()
    {
        Plane1PositionOffset = Vector3.zero;
        Plane2PositionOffset = Vector3.zero;

        if (plane1 != null)
            plane1.localRotation = plane1IdleQuaternion;

        if (plane2 != null)
            plane2.localRotation = plane2IdleQuaternion;
    }

    private void SaveIdleValues()
    {
        if (useCurrentPositionAsIdle)
        {
            if (plane1 != null)
                plane1IdlePosition = plane1.localPosition;

            if (plane2 != null)
                plane2IdlePosition = plane2.localPosition;
        }

        if (useCurrentRotationAsIdle)
        {
            if (plane1 != null)
                plane1IdleRotation = plane1.localEulerAngles;

            if (plane2 != null)
                plane2IdleRotation = plane2.localEulerAngles;
        }

        plane1IdleQuaternion = Quaternion.Euler(plane1IdleRotation);
        plane2IdleQuaternion = Quaternion.Euler(plane2IdleRotation);
    }

    private void InitializeNoiseFlow()
    {
        hasNoiseFlowProperty = false;

        if (noiseFlowMaterial == null)
            return;

        noiseFlowPropertyId = Shader.PropertyToID(noiseFlowPropertyName);

        if (!noiseFlowMaterial.HasProperty(noiseFlowPropertyId))
        {
            Debug.LogWarning(
                "[BattleEffectPlaneRotation] 머티리얼에서 Noise Flow 프로퍼티를 찾지 못했습니다.\n" +
                $"프로퍼티 이름: {noiseFlowPropertyName}",
                noiseFlowMaterial);
            return;
        }

        hasNoiseFlowProperty = true;
        idleNoiseFlowVector = noiseFlowMaterial.GetVector(noiseFlowPropertyId);

        if (useCurrentNoiseFlowXAsIdle)
        {
            idleNoiseFlowX = idleNoiseFlowVector.x;
        }
        else
        {
            idleNoiseFlowVector.x = idleNoiseFlowX;
            noiseFlowMaterial.SetVector(noiseFlowPropertyId, idleNoiseFlowVector);
        }
    }

    private void SetNoiseFlowX(float xValue)
    {
        if (!hasNoiseFlowProperty || noiseFlowMaterial == null)
            return;

        Vector4 currentNoiseFlow = noiseFlowMaterial.GetVector(noiseFlowPropertyId);
        currentNoiseFlow.x = xValue;
        noiseFlowMaterial.SetVector(noiseFlowPropertyId, currentNoiseFlow);
    }

    private void RestoreNoiseFlow()
    {
        if (!hasNoiseFlowProperty || noiseFlowMaterial == null)
            return;

        noiseFlowMaterial.SetVector(noiseFlowPropertyId, idleNoiseFlowVector);
    }

    private void FindPlanesAutomatically()
    {
        if (plane1 != null && plane2 != null)
            return;

        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];

            if (child == transform)
                continue;

            if (plane1 == null && child.name == "Plane")
            {
                plane1 = child;
                continue;
            }

            if (plane2 == null && child.name == "Plane (1)")
                plane2 = child;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        meleeRotationAmount = Mathf.Max(0f, meleeRotationAmount);
        rangedRotationAmount = Mathf.Max(0f, rangedRotationAmount);
        collisionRotationAmount = Mathf.Max(0f, collisionRotationAmount);
        moveOutDuration = Mathf.Max(0f, moveOutDuration);
        holdDuration = Mathf.Max(0f, holdDuration);
        returnDuration = Mathf.Max(0f, returnDuration);
        hitNoiseFlowPulseDuration = Mathf.Max(0f, hitNoiseFlowPulseDuration);
    }
#endif
}
