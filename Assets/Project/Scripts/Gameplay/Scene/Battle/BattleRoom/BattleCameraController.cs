using Relic.Gameplay.Monster;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BattleCameraController : MonoBehaviour
{
    public static BattleCameraController Instance { get; private set; }

    [SerializeField] private Camera targetCamera;

    [Header("Zoom")]
    [SerializeField] private float zoomSize = 3.7f;
    [SerializeField] private float zoomDuration = 0.3f;
    [SerializeField] private float returnDuration = 0.3f;

    [Header("BattleEffect 복귀 선행")]
    [Tooltip("공격 연출 종료 시 BattleEffect 회전이 카메라 줌아웃보다 먼저 복귀하기 시작하는 시간(초)입니다. 0이면 동시 시작합니다.")]
    [SerializeField, Min(0f)] private float battleEffectReturnLeadTime = 0.05f;

    private float activeBattleEffectReturnLeadProgress;
    [Header("Monster Phase Lighting")]
    [Tooltip("Scene Global Light 2D used for monster-specific camera step lighting. Assign once on BattleCameraController.")]
    [SerializeField] private Light2D monsterPhaseGlobalLight2D;
    public Light2D MonsterPhaseGlobalLight2D => monsterPhaseGlobalLight2D;
    private Coroutine monsterPhaseLightingRoutine;
    private bool monsterPhaseLightingSaved;
    private float monsterPhaseOriginalLightIntensity;

    private void StartMonsterPhaseStepLighting(MonsterPhaseCameraSequence.CameraStep step)
    {
        if (monsterPhaseGlobalLight2D == null || step == null || !step.overrideLightIntensity)
            return;
        if (!monsterPhaseLightingSaved)
        {
            monsterPhaseOriginalLightIntensity = monsterPhaseGlobalLight2D.intensity;
            monsterPhaseLightingSaved = true;
        }
        StartMonsterPhaseLightTransition(Mathf.Max(0f, step.lightIntensity), step.duration, step.curve);
    }

    private void StartMonsterPhaseReturnLighting(MonsterPhaseCameraSequence sequence)
    {
        if (!monsterPhaseLightingSaved || monsterPhaseGlobalLight2D == null || sequence == null)
            return;
        StartMonsterPhaseLightTransition(monsterPhaseOriginalLightIntensity,
            sequence.ReturnDuration, sequence.ReturnCurve);
    }

    private void StartMonsterPhaseLightTransition(float intensity, float duration, AnimationCurve curve)
    {
        if (monsterPhaseLightingRoutine != null)
        {
            StopCoroutine(monsterPhaseLightingRoutine);
            monsterPhaseLightingRoutine = null;
        }
        if (monsterPhaseGlobalLight2D == null)
            return;
        if (duration <= 0f || !isActiveAndEnabled)
        {
            monsterPhaseGlobalLight2D.intensity = intensity;
            return;
        }
        monsterPhaseLightingRoutine = StartCoroutine(AnimateMonsterPhaseLight(intensity, duration, curve));
    }

    private IEnumerator AnimateMonsterPhaseLight(float intensity, float duration, AnimationCurve curve)
    {
        if (monsterPhaseGlobalLight2D == null)
            yield break;
        float startingIntensity = monsterPhaseGlobalLight2D.intensity;
        float elapsed = 0f;
        while (elapsed < duration && monsterPhaseGlobalLight2D != null)
        {
            elapsed += GetCameraDeltaTime();
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = curve != null ? curve.Evaluate(t) : t * t * (3f - 2f * t);
            monsterPhaseGlobalLight2D.intensity = Mathf.Lerp(startingIntensity, intensity, eased);
            yield return null;
        }
        if (monsterPhaseGlobalLight2D != null)
            monsterPhaseGlobalLight2D.intensity = intensity;
        monsterPhaseLightingRoutine = null;
    }

    private void RestoreMonsterPhaseLightingImmediate()
    {
        if (monsterPhaseLightingRoutine != null)
        {
            StopCoroutine(monsterPhaseLightingRoutine);
            monsterPhaseLightingRoutine = null;
        }
        if (monsterPhaseLightingSaved && monsterPhaseGlobalLight2D != null)
            monsterPhaseGlobalLight2D.intensity = monsterPhaseOriginalLightIntensity;
        monsterPhaseLightingSaved = false;
    }

    private MonsterPhaseCameraSequence activeMonsterPhaseCamera;
    private Coroutine monsterPhaseStepsRoutine;
    private Coroutine monsterPhaseReturnRoutine;
    private bool monsterPhaseStepsRunning;
    private bool monsterPhaseReturnRequested;
    private bool monsterPhaseReturning;

    private void ClearMonsterPhaseCameraState()
    {
        RestoreMonsterPhaseLightingImmediate();
        if (monsterPhaseStepsRoutine != null)
            StopCoroutine(monsterPhaseStepsRoutine);
        if (monsterPhaseReturnRoutine != null)
            StopCoroutine(monsterPhaseReturnRoutine);
        monsterPhaseStepsRoutine = null;
        monsterPhaseReturnRoutine = null;
        monsterPhaseStepsRunning = false;
        monsterPhaseReturnRequested = false;
        monsterPhaseReturning = false;
        activeMonsterPhaseCamera = null;
    }


    [SerializeField] private Vector2 zoomOffset = new Vector2(0f, 0.35f);
    [SerializeField] private bool usePositionZZoom = true;
    [SerializeField] private bool useFixedZoomZPosition = true;
    [SerializeField] private float zoomZPosition = -13f;
    [SerializeField] private float zoomZOffset = 5f;

    [Header("Movement Camera Zoom")]
    [Tooltip("그리드 이동 전용 카메라 Z 좌표입니다. 기본 -18이며 공격 및 피격 줌 설정에는 영향을 주지 않습니다.")]
    [SerializeField] private float movementZoomZPosition = -18f;
    [SerializeField] private bool useOrthographicSizeZoom = false;
    [SerializeField] private bool clampZoomPosition = false;
    [SerializeField] private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Combat Edge Framing")]
    [Tooltip("전투 그리드의 좌우 끝 두 열에 포커스할 때 화면 바깥 배경 노출을 줄입니다.")]
    [SerializeField] private bool useCombatEdgeFraming = true;
    [Tooltip("왼쪽 0~9번 그리드에 포커스할 때 카메라를 오른쪽으로 이동할 거리입니다.")]
    [SerializeField, Min(0f)] private float leftEdgeCameraInset = 3.0f;
    [Tooltip("오른쪽 25~34번 그리드에 포커스할 때 카메라를 왼쪽으로 이동할 거리입니다.")]
    [SerializeField, Min(0f)] private float rightEdgeCameraInset = 3.0f;
    [Tooltip("좌우 끝 두 열의 아래쪽 3~4행에 포커스할 때 카메라를 맵 위쪽으로 이동할 거리입니다.")]
    [SerializeField, Min(0f)] private float bottomEdgeCameraInset = 1.4f;
    private GridManager combatEdgeGridManager;

    [Header("Action Cinematic Camera")]
    [Tooltip("전투 행동 중 시전자와 대상 방향에 따라 Y축 카메라 회전을 적용합니다.")]
    [SerializeField] private bool enableActionDirectionYaw = true;
    [Tooltip("오른쪽 공격은 음수, 왼쪽 공격은 양수로 적용되는 회전 크기입니다.")]
    [SerializeField, Range(0f, 15f)] private float actionDirectionYaw = 5f;
    [Tooltip("대상과 시전자의 X 거리 차이가 이 값 미만이면 카메라를 정면으로 유지합니다.")]
    [SerializeField, Min(0f)] private float actionYawDeadZone = 0.15f;
    [Tooltip("회전 중 주변 배경이 지나치게 잘리지 않도록 원근 카메라의 거리를 보정합니다.")]
    [SerializeField] private bool compensateActionYawFrame = true;

    [Header("Continuous Turn Camera")]
    [Tooltip("연속 행동이 실행되는 동안 기본 위치 복귀를 보류하고 다음 행동 구도로 바로 이동합니다.")]
    [SerializeField] private bool preserveCameraBetweenActions = true;

    private bool turnExecutionEventActive;
    private bool pendingTurnCameraReturn;
    private BattleTurnExecutor cachedTurnExecutor;
    private Coroutine deferredReturnRoutine;

    private void OnEnable()
    {
        BattleTurnExecutor.BattleExecutionStarted -= HandleTurnExecutionStarted;
        BattleTurnExecutor.BattleExecutionStarted += HandleTurnExecutionStarted;
        BattleTurnExecutor.PlayerTurnReturned -= HandlePlayerTurnReturned;
        BattleTurnExecutor.PlayerTurnReturned += HandlePlayerTurnReturned;
    }

    private void HandleTurnExecutionStarted()
    {
        turnExecutionEventActive = true;
        gridMovementFocusedUnit = null;
        pendingTurnCameraReturn = false;
        if (deferredReturnRoutine != null)
        {
            StopCoroutine(deferredReturnRoutine);
            deferredReturnRoutine = null;
        }
    }

    private void HandlePlayerTurnReturned()
    {
        turnExecutionEventActive = false;
        if (!pendingTurnCameraReturn || !isActiveAndEnabled)
            return;

        pendingTurnCameraReturn = false;
        if (deferredReturnRoutine != null)
            StopCoroutine(deferredReturnRoutine);
        deferredReturnRoutine = StartCoroutine(FinishDeferredTurnCameraReturn());
    }

    private IEnumerator FinishDeferredTurnCameraReturn()
    {
        // 이전 행동/타격 효과가 종료되는 프레임에서 카메라가 튀지 않도록 다음 프레임에 복귀합니다.
        yield return null;
        // PlayerTurnReturned can be emitted before IsExecuting is cleared.
        // Wait for that flag to settle rather than abandoning the zoom-out.
        while (!turnExecutionEventActive && IsTurnCameraExecutionActive())
            yield return null;
        deferredReturnRoutine = null;
        if (turnExecutionEventActive)
            yield break;

        // 턴 종료는 전투 종료가 아닙니다. 예약 입력으로 돌아갈 때는
        // 기본 카메라(Y=1)를 경유하지 않고 현재 선택 캐릭터 구도로 복귀합니다.
        if (TryRestoreSelectedCharacterFocusAfterTurn())
            yield break;

        // 선택된 캐릭터가 없거나 전투가 끝난 경우에만 기본 구도로 돌아갑니다.
        yield return ReturnDefault();
    }

    private bool TryRestoreSelectedCharacterFocusAfterTurn()
    {
        if (BattleResultChecker.Instance != null && BattleResultChecker.Instance.BattleEnded)
            return false;

        BattleTimelineController timeline =
            FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);
        if (!enableCharacterSelectionFocus || timeline == null || timeline.SelectedCharacter == null)
            return false;

        if (targetCamera == null)
            targetCamera = Camera.main;
        if (targetCamera == null)
            return false;

        // 마지막 전투 행동의 카메라 소유권만 해제합니다.
        // ReturnDefault()로 중간에 기본 위치까지 줌아웃하지 않습니다.
        RemoveMouseParallax();
        EndZoomFollowTarget();
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        if (panelDownReturnRoutine != null)
        {
            StopCoroutine(panelDownReturnRoutine);
            panelDownReturnRoutine = null;
        }
        ClearImpactOffset();
        BattleEffectPlaneRotation.BeginCameraReturnFeedback(returnDuration, zoomCurve);
        ApplyCameraRotationZ(0f);
        ResetDamageImpactRotationSequence();
        hasActiveCombatZoom = false;
        ResetCombatFocusTracking();
        hasActiveMonsterInfoFocus = false;
        hasActiveGridSelectionFocus = false;

        timeline.RefocusCurrentSelectedCharacterWhenInputReady();
        return true;
    }

    private bool ShouldPreserveTurnSelectionFocus()
    {
        if (BattleResultChecker.Instance != null && BattleResultChecker.Instance.BattleEnded)
            return false;
        if (!turnExecutionEventActive && !pendingTurnCameraReturn && deferredReturnRoutine == null)
            return false;

        BattleTimelineController timeline =
            FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);
        return timeline != null && timeline.SelectedCharacter != null;
    }

    private bool IsTurnCameraExecutionActive()
    {
        if (!preserveCameraBetweenActions)
            return false;

        if (cachedTurnExecutor == null)
            cachedTurnExecutor = FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Exclude);

        return turnExecutionEventActive ||
               (cachedTurnExecutor != null && cachedTurnExecutor.IsExecuting);
    }

    [Header("Damage Impact")]
    [SerializeField] private bool enableDamageImpact = true;
    [SerializeField] private bool enableDamageImpactRotation = true;
    [SerializeField] private float[] damageImpactRotationZPattern = { 0f, -1f, 1f };
    [SerializeField] private float impactZoomAmount = 0f;
    [SerializeField] private float impactZoomZOffset = 1.5f;
    [SerializeField] private float impactZoomInDuration = 0.08f;
    [SerializeField] private float impactZoomOutDuration = 0.12f;
    [SerializeField] private float impactShakeDuration = 0.08f;
    [SerializeField] private float impactShakeStrength = 0.1f;
    [SerializeField] private float impactShakeFrequency = 20f;
    [SerializeField] private float impactHitStopDuration = 0.1f;
    [SerializeField, Range(0f, 1f)] private float impactHitStopTimeScale = 0.08f;
    [SerializeField, Min(1f)] private float impactRecoverySpeedMultiplier = 1.5f;
    [SerializeField, Min(0f)] private float impactRecoverySpeedDuration = 0.18f;
    [SerializeField] private bool useUnscaledTimeForImpact = true;

    [Header("Grid Movement Combat Zoom")]
    [Tooltip("전투 행동이 명시적으로 요청한 이동 유닛 중심으로 카메라 줌을 적용합니다.")]
    [SerializeField] private bool enableGridMovementImpactZoom = true;
    private Transform gridMovementFocusedUnit;

    [Header("Movement To Action Camera Transition")]
    [Tooltip("이동 직후 구도 유지 판정 시 화면 가장자리의 여유 비율입니다. 0.08은 상하좌우 8% 여유를 의미합니다.")]
    [SerializeField, Range(0f, 0.3f)] private float movementToActionViewportPadding = 0.08f;


    [Header("Directional Impact Beats")]
    [SerializeField, Min(0f)] private float impactCompressionDistance = 0.08f;
    [SerializeField, Min(0f)] private float impactRecoilDistance = 0.16f;
    [SerializeField, Min(0f)] private float impactOvershootDistance = 0.035f;
    [SerializeField, Min(0f)] private float impactCompressionDuration = 0.035f;
    [SerializeField, Min(0f)] private float impactRecoilDuration = 0.045f;
    [SerializeField, Min(0f)] private float impactSettleDuration = 0.09f;

    [Header("Ranged Skill Zoom")]
    [Tooltip("원거리 스킬 실행 시작 시 카메라가 이동할 Z 위치입니다. X/Y는 이동하지 않습니다.")]
    [SerializeField] private float rangedExecutionZoomZPosition = -18f;
    [Tooltip("원거리 스킬 실행 시작 시 Z 줌에 걸리는 시간입니다.")]
    [SerializeField] private float rangedExecutionZoomDuration = 0.25f;

    [Header("Combat Target Focus")]
    [Tooltip("시전자에서 피격 대상으로 이동할 때의 부드러운 전환 시간입니다.")]
    [SerializeField, Min(0.05f)] private float combatTargetFocusDuration = 0.38f;
    [Tooltip("한 번의 대상 포커스 이동에서 허용할 최대 XY 거리입니다. 0이면 제한하지 않습니다.")]
    [SerializeField, Min(0f)] private float combatTargetFocusMaxDistance = 0f;
    [Tooltip("버프/디버프 시전자 포커스 이동에 걸리는 시간입니다. 줌 거리에는 원거리 설정을 사용합니다.")]
    [SerializeField, Min(0.05f)] private float statusCasterFocusDuration = 0.30f;

    [Header("Status Effect Zoom Pulse")]
    [Tooltip("버프/디버프 적용 순간 화면을 살짝 확대했다가 복귀하는 연출을 사용합니다. 화면 흔들림과 히트스톱은 사용하지 않습니다.")]
    [SerializeField] private bool enableStatusEffectZoomPulse = true;
    [Tooltip("상태 효과 적용 시 카메라가 추가로 가까워지는 Z 거리입니다. 원근 카메라에서 사용합니다.")]
    [SerializeField, Min(0f)] private float statusEffectPulseZOffset = 0.45f;
    [Tooltip("직교 카메라에서 상태 효과 적용 시 줄어드는 Orthographic Size입니다.")]
    [SerializeField, Min(0f)] private float statusEffectPulseSizeOffset = 0.20f;
    [SerializeField, Min(0f)] private float statusEffectPulseZoomInDuration = 0.10f;
    [SerializeField, Min(0f)] private float statusEffectPulseHoldDuration = 0.09f;
    [SerializeField, Min(0f)] private float statusEffectPulseZoomOutDuration = 0.17f;
    [Tooltip("실제 버프/디버프 대상이 있을 때 공격 적중 연출에 가까운 템포를 유지하는 최소 줌 펄스 시간(초)입니다. 부족한 시간은 확대 유지 구간에 추가됩니다.")]
    [SerializeField, Min(0f)] private float statusEffectPulseMinimumDuration = 0.60f;

    [Header("Ranged Damage Impact")]
    [Tooltip("원거리 공격 명중 시 적용할 약한 Z 줌 거리입니다. 근거리 impactZoomZOffset과 별도로 사용합니다.")]
    [SerializeField] private float rangedImpactZoomZOffset = 0.5f;
    [Tooltip("원거리 공격 명중 시 약한 줌이 들어가는 시간입니다.")]
    [SerializeField] private float rangedImpactZoomInDuration = 0.06f;
    [Tooltip("원거리 공격 명중 후 원래 카메라 Z 위치로 돌아오는 시간입니다.")]
    [SerializeField] private float rangedImpactZoomOutDuration = 0.12f;

    [Header("Map Camera")]
    [SerializeField] private Vector3 mapPosition = new Vector3(0f, 0f, -20f);

    [Header("Character Selection Focus")]
    [SerializeField] private bool enableCharacterSelectionFocus = true;
    [SerializeField] private float characterSelectionFocusDuration = 0.55f;
    [SerializeField] private float minimumCharacterSelectionFocusDuration = 0.55f;
    [SerializeField] private Vector2 characterSelectionFocusOffset = new Vector2(0f, 0.25f);
    [SerializeField] private bool useGridBasedCharacterSelectionFocusOffset = true;
    [SerializeField] private int characterSelectionFocusGridColumnCount = 7;
    [SerializeField] private int characterSelectionFocusGridRowCount = 5;
    [SerializeField] private Vector2 characterSelectionFocusLeftTopOffset = new Vector2(-1f, 0.5f);
    [SerializeField] private Vector2 characterSelectionFocusLeftBottomOffset = new Vector2(-1f, -1f);
    [SerializeField] private Vector2 characterSelectionFocusRightTopOffset = new Vector2(1f, 0.5f);
    [SerializeField] private Vector2 characterSelectionFocusRightBottomOffset = new Vector2(1f, -1f);
    [SerializeField] private bool useCharacterSelectionFocusZ = true;
    [SerializeField] private bool useFixedCharacterSelectionFocusZ = true;
    [SerializeField] private float characterSelectionFocusZPosition = -17.5f;
    [SerializeField] private float characterSelectionFocusZOffset = 2.5f;
    [SerializeField] private bool useCharacterSelectionFocusOrthographicSize = false;
    [SerializeField] private float characterSelectionFocusOrthographicSize = 4.4f;
    [SerializeField] private bool clampCharacterSelectionFocusPosition = false;

    [Header("Camera Position Clamp")]
    [SerializeField] private Vector2 minCameraPosition = new Vector2(-0.5f, -1f);
    [SerializeField] private Vector2 maxCameraPosition = new Vector2(0.5f, 1f);

    [Header("Panel Down Position")]
    // BattleCharacterPanel과 BattleSlot이 내려간 상태에서는 카메라 Y를 항상 1로 유지합니다.
    // 기존 씬/프리팹에 저장된 1.5 값의 영향을 받지 않도록 상수로 정의합니다.
    [Header("Monster Info Focus (Panel Down)")]
    [SerializeField] private bool enableMonsterInfoFocus = true;
    [SerializeField] private Vector2 monsterInfoFocusOffset = Vector2.zero;
    [SerializeField, Min(0f)] private float monsterInfoFocusXMultiplier = 1.75f;
    [SerializeField] private float monsterInfoFocusZOffset = 2f;
    [SerializeField, Min(0f)] private float monsterInfoFocusDuration = 0.3f;

    private const float PanelDownPositionY = 1f;

    [Header("Mouse Parallax")]
    [SerializeField] private bool enableMouseParallax = true;
    [SerializeField] private Vector2 mouseParallaxPositionAmount = new Vector2(0.08f, 0.05f);
    [Tooltip("마우스 상하 이동에 따른 카메라 X 회전, 좌우 이동에 따른 Y 회전의 기본 각도입니다.")]
    [SerializeField] private Vector2 mouseParallaxRotationAmount = new Vector2(1f, 1f);
    [Tooltip("Mouse Parallax Y 회전 추가 배율입니다. 0이면 Y 회전 없음, 1이면 기존 설정 유지입니다.")]
    [SerializeField, Min(0f)] private float mouseParallaxYRotationMultiplier = 1f;
    [Tooltip("캐릭터/몬스터 그리드 선택 포커스 중에도 줌 상태의 Parallax 강도를 사용합니다.")]
    [SerializeField] private bool treatGridSelectionAsZoomForParallax = true;
    [SerializeField, Min(0f)] private float mouseParallaxSmoothSpeed = 8f;
    [SerializeField, Range(0f, 1f)] private float mouseParallaxCameraMotionMultiplier = 0.35f;

    private float defaultSize;
    private Vector3 defaultPosition;
    private Coroutine routine;
    private Coroutine panelDownReturnRoutine;
    private bool holdDefaultReturn;
    private bool hasActiveCombatZoom;
    private bool hasActiveMonsterInfoFocus;
    private bool hasActiveGridSelectionFocus;

    private enum CombatCameraFocusMode
    {
        None,
        Melee,
        Ranged
    }

    private Transform activeCombatFocusOwner;
    private CombatCameraFocusMode activeCombatFocusMode = CombatCameraFocusMode.None;

    private Transform zoomFollowTarget;
    private Vector3 zoomFollowVelocity;
    [Header("Grid Movement Camera Smoothing")]
    [SerializeField, Min(0.01f)] private float movementFollowSmoothTime = 0.12f;
    private Vector3 movementFocusVelocity;
    private float ZoomFollowSmoothTime => Mathf.Max(0.01f, movementFollowSmoothTime);

    private Vector3 activeImpactOffset;
    private Vector3 lastImpactAppliedPosition;
    private bool isImpactHitStopActive;
    private float previousTimeScale = 1f;
    private Coroutine impactRecoverySpeedRoutine;
    private bool isImpactRecoverySpeedActive;
    private float impactRecoveryBaseTimeScale = 1f;
    private int damageImpactRotationIndex;
    private int nextImpactHorizontalDirection = 1;
    private bool hasPreparedDamageImpact;
    private bool hasPreparedRangedImpact;
    private float preparedImpactBaseX;
    private float preparedImpactBaseZ;

    private Vector2 currentMouseParallax;
    private Vector3 lastMouseParallaxPositionOffset;
    private Quaternion lastMouseParallaxRotationOffset = Quaternion.identity;
    private bool hasAppliedMouseParallax;
    private Coroutine externalShakeRoutine;
    private Vector3 externalShakeOffset;
    private Vector3 appliedExternalShakeOffset;
    private bool hasAppliedExternalShake;
    private bool hasCinematicCameraState;
    private Vector3 cinematicRestorePosition;
    private Quaternion cinematicRestoreRotation;
    private float cinematicRestoreSize;

    public bool IsCombatZoomActive => hasActiveCombatZoom;
    public bool IsMonsterInfoFocusActive => hasActiveMonsterInfoFocus;

    private void Awake()
    {
        Instance = this;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null)
        {
            defaultSize = targetCamera.orthographicSize;
            // 씬 복원 시 임시 지도/연출 카메라 위치를 전투 포커스 기준으로 저장하지 않습니다.
            defaultPosition = mapPosition;
        }
    }

    private void OnDisable()
    {
        BattleTurnExecutor.BattleExecutionStarted -= HandleTurnExecutionStarted;
        BattleTurnExecutor.PlayerTurnReturned -= HandlePlayerTurnReturned;
        turnExecutionEventActive = false;
        pendingTurnCameraReturn = false;
        if (deferredReturnRoutine != null)
        {
            StopCoroutine(deferredReturnRoutine);
            deferredReturnRoutine = null;
        }
        gridMovementFocusedUnit = null;
        RestoreCinematicCameraImmediate();
        ClearMonsterPhaseCameraState();
        StopExternalShake();
        RemoveMouseParallax();
        RestoreTimeScaleIfNeeded();
        CancelImpactRecoverySpeedBoost();
        ClearImpactOffset();
        ForceReturnDefaultImmediate();
    }

    private void Update()
    {
        RemoveExternalShakeOffset();
        RemoveMouseParallax();
        HandleZoomFollowTarget();
    }

    private void LateUpdate()
    {
        ApplyMouseParallax();
        ApplyExternalShakeOffset();
    }

    public bool UsesCameraTransform(Transform candidate)
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();
        if (targetCamera == null)
            targetCamera = Camera.main;

        return targetCamera != null && candidate == targetCamera.transform;
    }

    // The monster information panel uses the panel-down camera height as its origin,
    // then offsets the focus toward the monster's grid cell.
    public void FocusMonsterInfoForGridIndex(Transform target, int gridIndex)
    {
        if (!enableMonsterInfoFocus || target == null || hasActiveCombatZoom)
            return;

        if (targetCamera == null)
            targetCamera = Camera.main;
        if (targetCamera == null)
            return;

        if (panelDownReturnRoutine != null)
        {
            StopCoroutine(panelDownReturnRoutine);
            panelDownReturnRoutine = null;
        }

        RemoveMouseParallax();
        EndZoomFollowTarget();
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        ClearImpactOffset();
        ApplyCameraRotationZ(0f);

        Vector2 gridOffset = GetCharacterSelectionGridFocusOffset(gridIndex);
        Vector3 destination = GetNeutralBattlePosition();
        destination.x += gridOffset.x * monsterInfoFocusXMultiplier + monsterInfoFocusOffset.x;
        destination.y += gridOffset.y + monsterInfoFocusOffset.y;
        destination.z = defaultPosition.z + monsterInfoFocusZOffset;

        hasActiveMonsterInfoFocus = true;
        hasActiveGridSelectionFocus = false;
        MoveCameraWithOptionalImmediate(
            destination,
            defaultSize,
            Mathf.Approximately(monsterInfoFocusDuration, 0.55f) || Mathf.Approximately(monsterInfoFocusDuration, 0.2f)
                ? 0.3f : monsterInfoFocusDuration,
            clampCharacterSelectionFocusPosition,
            true);
    }

    public bool TryBeginCinematicCamera()
    {
        if (hasCinematicCameraState)
            return false;

        return BeginCinematicCamera();
    }

    public bool BeginCinematicCamera()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
        if (targetCamera == null)
            return false;

        RemoveExternalShakeOffset();
        RemoveMouseParallax();
        EndZoomFollowTarget();
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        ClearImpactOffset();

        if (!hasCinematicCameraState)
        {
            cinematicRestorePosition = targetCamera.transform.position;
            cinematicRestoreRotation = targetCamera.transform.rotation;
            cinematicRestoreSize = targetCamera.orthographicSize;
            hasCinematicCameraState = true;
        }

        return true;
    }

    public void ApplyCinematicCameraPose(
        Vector3 position,
        Quaternion rotation,
        float orthographicSize,
        bool applyPosition,
        bool applyRotation,
        bool applyOrthographicSize)
    {
        if (targetCamera == null || !hasCinematicCameraState)
            return;

        RemoveExternalShakeOffset();
        RemoveMouseParallax();

        Transform cameraTransform = targetCamera.transform;
        if (applyPosition)
            cameraTransform.position = position;
        if (applyRotation)
            cameraTransform.rotation = rotation;
        if (applyOrthographicSize && targetCamera.orthographic)
            targetCamera.orthographicSize = Mathf.Max(0.01f, orthographicSize);
    }

    public void RestoreCinematicCameraImmediate()
    {
        if (!hasCinematicCameraState)
            return;

        if (targetCamera != null)
        {
            RemoveExternalShakeOffset();
            RemoveMouseParallax();
            targetCamera.transform.SetPositionAndRotation(
                cinematicRestorePosition,
                cinematicRestoreRotation);
            if (targetCamera.orthographic)
                targetCamera.orthographicSize = cinematicRestoreSize;
        }

        hasCinematicCameraState = false;
    }

    public void EndCinematicCameraKeepingCurrentPose()
    {
        hasCinematicCameraState = false;
    }

    public void PlayExternalShake(float duration, float strength, float frequency, bool useUnscaledTime)
    {
        StopExternalShake();

        if (!isActiveAndEnabled || targetCamera == null || duration <= 0f || strength <= 0f)
            return;

        externalShakeRoutine = StartCoroutine(
            ExternalShakeRoutine(duration, strength, frequency, useUnscaledTime));
    }

    public void StopExternalShake()
    {
        if (externalShakeRoutine != null)
        {
            StopCoroutine(externalShakeRoutine);
            externalShakeRoutine = null;
        }

        RemoveExternalShakeOffset();
        externalShakeOffset = Vector3.zero;
    }

    private IEnumerator ExternalShakeRoutine(
        float duration,
        float strength,
        float frequency,
        bool useUnscaledTime)
    {
        float safeDuration = Mathf.Max(0f, duration);
        float safeStrength = Mathf.Max(0f, strength);
        float safeFrequency = Mathf.Max(0f, frequency);
        float seedX = Random.Range(0f, 1000f);
        float seedY = Random.Range(0f, 1000f);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float progress = safeDuration > 0f ? Mathf.Clamp01(elapsed / safeDuration) : 1f;
            float amount = safeStrength * (1f - progress);
            float sample = elapsed * safeFrequency;
            externalShakeOffset = new Vector3(
                (Mathf.PerlinNoise(seedX, sample) * 2f - 1f) * amount,
                (Mathf.PerlinNoise(seedY, sample) * 2f - 1f) * amount,
                0f);
            yield return null;
        }

        externalShakeOffset = Vector3.zero;
        externalShakeRoutine = null;
    }

    private void ApplyExternalShakeOffset()
    {
        if (targetCamera == null || externalShakeOffset == Vector3.zero)
            return;

        appliedExternalShakeOffset = externalShakeOffset;
        targetCamera.transform.localPosition += appliedExternalShakeOffset;
        hasAppliedExternalShake = true;
    }

    private void RemoveExternalShakeOffset()
    {
        if (targetCamera == null || !hasAppliedExternalShake)
            return;

        targetCamera.transform.localPosition -= appliedExternalShakeOffset;
        appliedExternalShakeOffset = Vector3.zero;
        hasAppliedExternalShake = false;
    }

    // Ultimate intro uses the movement focus distance, not the attack zoom.
    // The action camera can then take over when the skill actually executes.
    public IEnumerator FocusForUltimateIntroduction(Transform caster)
    {
        if (targetCamera == null || caster == null)
            yield break;

        hasActiveGridSelectionFocus = false;
        hasActiveCombatZoom = true;
        activeCombatFocusOwner = caster;
        activeCombatFocusMode = CombatCameraFocusMode.None;
        RemoveMouseParallax();
        hasActiveMonsterInfoFocus = false;
        EndZoomFollowTarget();
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        ClearImpactOffset();

        Vector3 destination = caster.position;
        destination.y += zoomOffset.y;
        destination.z = usePositionZZoom
            ? movementZoomZPosition
            : targetCamera.transform.position.z;
        float targetSize = useOrthographicSizeZoom
            ? zoomSize : targetCamera.orthographicSize;
        destination.x = CalculateCameraXForScreenAnchor(
            caster.position, destination.z, targetCamera.transform.rotation,
            0.5f, targetSize) + zoomOffset.x;
        // Apply the same inward framing as direct movement before the cutscene starts.
        destination += GetCombatEdgeCameraOffset(caster.position);

        // Combat focus is intentionally not clamped to the grid camera bounds.
        routine = StartCoroutine(MoveCamera(
            destination, targetSize, zoomDuration, false, useOrthographicSizeZoom));
        yield return routine;
    }

    public IEnumerator ZoomTo(Transform target)
    {
        if (targetCamera == null || target == null)
            yield break;

        yield return ZoomToPosition(target.position);
    }

    // Legacy entry points retained for existing boss phase call sites.
    // All camera settings live exclusively on the monster's own component.
    // No component means no phase-camera animation (and no global fallback).
    public IEnumerator PlayArabellaPhaseTransitionCamera(Transform target)
    {
        MonsterPhaseCameraSequence sequence = target != null
            ? target.GetComponentInParent<MonsterPhaseCameraSequence>() : null;
        if (sequence == null || !sequence.isActiveAndEnabled || !sequence.EnablePhaseCamera)
            yield break;

        // Start the camera sequence independently. The phase caller may yield this
        // legacy IEnumerator before playing its 2Phase clip; waiting here would
        // incorrectly postpone the animation until all camera steps had finished.
        // The camera remains owned by the monster and is returned by the existing
        // return call (or the component's Auto Return After Steps setting).
        StartCoroutine(PlayMonsterPhaseCamera(sequence.FocusTarget, sequence));
        yield break;
    }

    public IEnumerator ReturnFromArabellaPhaseTransitionCamera()
    {
        MonsterPhaseCameraSequence sequence = activeMonsterPhaseCamera;
        if (sequence != null)
            yield return ReturnFromMonsterPhaseCamera(sequence);
    }

    /// <summary>
    /// Play only the sequence configured on this particular monster prefab.
    /// This is not a global camera preset; callers explicitly select a monster.
    /// </summary>
    public IEnumerator PlayMonsterPhaseCamera(Transform target, MonsterPhaseCameraSequence sequence)
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
        if (targetCamera == null || target == null || sequence == null || !sequence.EnablePhaseCamera)
            yield break;

        // Repeated calls from the same monster must not restart its unfinished steps.
        if (activeMonsterPhaseCamera == sequence && (monsterPhaseStepsRunning || monsterPhaseReturning))
            yield break;

        // Another monster explicitly taking ownership may cancel the previous sequence.
        if (activeMonsterPhaseCamera != null)
            ClearMonsterPhaseCameraState();
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        StopExternalShake();
        RemoveMouseParallax();
        EndZoomFollowTarget();
        ClearImpactOffset();
        hasActiveMonsterInfoFocus = false;
        hasActiveGridSelectionFocus = false;
        activeMonsterPhaseCamera = sequence;
        sequence.NotifyPhaseCameraStarted();
        monsterPhaseStepsRunning = true;
        monsterPhaseReturnRequested = false;
        monsterPhaseReturning = false;

        // This coroutine owns the phase steps independently of the general combat camera.
        monsterPhaseStepsRoutine = StartCoroutine(RunMonsterPhaseCameraSteps(target, sequence));
        yield return monsterPhaseStepsRoutine;
    }

    private IEnumerator RunMonsterPhaseCameraSteps(Transform target, MonsterPhaseCameraSequence sequence)
    {
        MonsterPhaseCameraSequence.CameraStep[] steps = sequence.Steps;
        if (steps != null)
        {
            foreach (MonsterPhaseCameraSequence.CameraStep step in steps)
            {
                if (target == null || sequence == null || activeMonsterPhaseCamera != sequence)
                    yield break;
                if (step == null || !step.enabled)
                    continue;

                Vector3 destination = target.position + new Vector3(step.offset.x, step.offset.y, 0f);
                destination.z = step.zPosition;
                StartMonsterPhaseStepLighting(step);
                yield return MoveCameraUsingCurve(destination, step.orthographicSize,
                    step.duration, step.curve, targetCamera.orthographic);

                if (step.shake && step.shakeDuration > 0f && step.shakeStrength > 0f)
                    PlayExternalShake(step.shakeDuration, step.shakeStrength,
                        step.shakeFrequency, true);

                float elapsed = 0f;
                while (elapsed < step.holdDuration)
                {
                    elapsed += GetCameraDeltaTime();
                    yield return null;
                }
            }
        }

        monsterPhaseStepsRunning = false;
        monsterPhaseStepsRoutine = null;
        hasActiveCombatZoom = true;

        // The animation may request return before the last step completes.
        // In that case, defer return until this point instead of aborting the steps.
        if (activeMonsterPhaseCamera == sequence &&
            (monsterPhaseReturnRequested || sequence.AutoReturnAfterSteps))
        {
            if (!monsterPhaseReturning)
            {
                monsterPhaseReturning = true;
                monsterPhaseReturnRoutine = StartCoroutine(ReturnFromMonsterPhaseCameraAfterAnimation(sequence));
            }
        }
    }

    public IEnumerator ReturnFromMonsterPhaseCamera(MonsterPhaseCameraSequence sequence)
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
        if (targetCamera == null || sequence == null || activeMonsterPhaseCamera != sequence)
            yield break;

        // Do not interrupt the camera steps, even if the phase clip ends first.
        monsterPhaseReturnRequested = true;
        while (activeMonsterPhaseCamera == sequence && monsterPhaseStepsRunning)
            yield return null;

        if (activeMonsterPhaseCamera != sequence)
            yield break;

        // Camera steps and the configured phase Animator must BOTH finish before return.
        yield return sequence.WaitForPhaseAnimationCompletion();
        if (activeMonsterPhaseCamera != sequence)
            yield break;

        if (!monsterPhaseReturning)
        {
            monsterPhaseReturning = true;
            monsterPhaseReturnRoutine = StartCoroutine(ReturnFromMonsterPhaseCameraSteps(sequence));
        }
        while (activeMonsterPhaseCamera == sequence && monsterPhaseReturning)
            yield return null;
    }

    private IEnumerator ReturnFromMonsterPhaseCameraAfterAnimation(MonsterPhaseCameraSequence sequence)
    {
        yield return sequence.WaitForPhaseAnimationCompletion();
        if (activeMonsterPhaseCamera != sequence)
            yield break;
        yield return ReturnFromMonsterPhaseCameraSteps(sequence);
    }

    private IEnumerator ReturnFromMonsterPhaseCameraSteps(MonsterPhaseCameraSequence sequence)
    {
        if (targetCamera == null || activeMonsterPhaseCamera != sequence)
            yield break;
        StopExternalShake();
        RemoveMouseParallax();
        EndZoomFollowTarget();
        ClearImpactOffset();
        ApplyCameraRotationZ(0f);
        ResetDamageImpactRotationSequence();
        StartMonsterPhaseReturnLighting(sequence);

        yield return MoveCameraUsingCurve(GetNeutralBattlePosition(), defaultSize,
            sequence.ReturnDuration, sequence.ReturnCurve, true, true);
        RestoreMonsterPhaseLightingImmediate();

        hasActiveCombatZoom = false;
        ResetCombatFocusTracking();
        hasActiveMonsterInfoFocus = false;
        hasActiveGridSelectionFocus = false;
        monsterPhaseReturnRoutine = null;
        monsterPhaseReturning = false;
        monsterPhaseReturnRequested = false;
        activeMonsterPhaseCamera = null;
    }

    private IEnumerator MoveCameraUsingCurve(
        Vector3 targetPosition,
        float targetSize,
        float duration,
        AnimationCurve curve,
        bool applyOrthographicSize,
        bool resetRotation = false)
    {
        Vector3 startPosition = targetCamera.transform.position;
        float startSize = targetCamera.orthographicSize;
        Quaternion startRotation = targetCamera.transform.rotation;

        if (duration <= 0f)
        {
            targetCamera.transform.position = targetPosition;
            if (applyOrthographicSize)
                targetCamera.orthographicSize = targetSize;
            if (resetRotation)
                targetCamera.transform.rotation = Quaternion.identity;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += GetCameraDeltaTime();
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = curve != null ? curve.Evaluate(t) : t * t * (3f - 2f * t);
            targetCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, eased);
            if (applyOrthographicSize)
                targetCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, eased);
            if (resetRotation)
                targetCamera.transform.rotation = Quaternion.Slerp(startRotation, Quaternion.identity, eased);
            yield return null;
        }

        targetCamera.transform.position = targetPosition;
        if (applyOrthographicSize)
            targetCamera.orthographicSize = targetSize;
        if (resetRotation)
            targetCamera.transform.rotation = Quaternion.identity;
    }

    public IEnumerator ZoomToBetween(Transform first, Transform second)
    {
        if (targetCamera == null || first == null || second == null)
            yield break;

        Vector3 focusPosition = (first.position + second.position) * 0.5f;
        yield return ZoomToPosition(focusPosition);
    }

    public IEnumerator ZoomToAttacker(Transform attacker)
    {
        if (targetCamera == null || attacker == null)
            yield break;

        // 같은 공격자가 같은 근거리 연출을 연속으로 사용할 때만 현재 줌을 유지합니다.
        // 공격자가 바뀌거나 원거리 -> 근거리처럼 연출 타입이 바뀌면 새 기준으로 다시 포커스합니다.
        if (hasActiveCombatZoom &&
            activeCombatFocusOwner == attacker &&
            activeCombatFocusMode == CombatCameraFocusMode.Melee)
        {
            yield break;
        }

        hasActiveGridSelectionFocus = false;
        hasActiveCombatZoom = true;
        activeCombatFocusOwner = attacker;
        activeCombatFocusMode = CombatCameraFocusMode.Melee;
        ResetDamageImpactRotationSequence();

        yield return ZoomToPosition(attacker.position);
    }

    public IEnumerator ZoomToHitTarget(Transform hitTarget)
    {
        // 기존 호출부 호환용이다. 실제 기준은 피격자가 아니라 최초 타격자다.
        yield return ZoomToAttacker(hitTarget);
    }

    /// <summary>
    /// 원거리 스킬 실행용 카메라 줌입니다.
    /// 공격자 위치를 따라가지 않고 현재 X/Y를 유지한 채 Z만 약하게 확대합니다.
    /// </summary>
    public IEnumerator ZoomForRangedSkill(Transform attacker = null)
    {
        if (targetCamera == null)
            yield break;

        // 같은 공격자가 같은 원거리 연출을 연속으로 사용할 때만 현재 줌을 유지합니다.
        // 공격자가 바뀌거나 근거리 -> 원거리처럼 연출 타입이 바뀌면 Z 줌을 다시 갱신합니다.
        if (hasActiveCombatZoom &&
            activeCombatFocusOwner == attacker &&
            activeCombatFocusMode == CombatCameraFocusMode.Ranged)
        {
            yield break;
        }

        hasActiveGridSelectionFocus = false;
        hasActiveCombatZoom = true;
        activeCombatFocusOwner = attacker;
        activeCombatFocusMode = CombatCameraFocusMode.Ranged;
        ResetDamageImpactRotationSequence();
        RemoveMouseParallax();
        hasActiveMonsterInfoFocus = false;
        EndZoomFollowTarget();

        if (routine != null)
            StopCoroutine(routine);

        ClearImpactOffset();

        Vector3 targetPos = targetCamera.transform.position;
        targetPos.z = rangedExecutionZoomZPosition;

        routine = StartCoroutine(MoveCamera(
            targetPos,
            targetCamera.orthographicSize,
            rangedExecutionZoomDuration,
            false,
            false));
        yield return routine;
    }

    /// <summary>
    /// 버프/디버프는 원거리 Z 줌을 사용하지만 XY는 시전자를 향해 부드럽게 이동합니다.
    /// </summary>
    public IEnumerator ZoomForStatusSkill(Transform caster)
    {
        if (targetCamera == null || caster == null)
            yield break;

        hasActiveGridSelectionFocus = false;
        hasActiveCombatZoom = true;
        activeCombatFocusOwner = caster;
        activeCombatFocusMode = CombatCameraFocusMode.Ranged;
        ResetDamageImpactRotationSequence();
        RemoveMouseParallax();
        hasActiveMonsterInfoFocus = false;
        EndZoomFollowTarget();

        if (routine != null)
            StopCoroutine(routine);

        ClearImpactOffset();
        Vector3 destination = caster.position;
        destination.x += zoomOffset.x;
        destination.y += zoomOffset.y;
        destination.z = rangedExecutionZoomZPosition;

        routine = StartCoroutine(MoveCamera(
            destination,
            targetCamera.orthographicSize,
            Mathf.Max(0.05f, statusCasterFocusDuration),
            false,
            false));
        yield return routine;
    }

    /// <summary>
    /// 대상들의 중심으로 전투 카메라를 천천히 이동합니다. 현재 줌 깊이는 유지합니다.
    /// 시전자 포커스와의 급격한 XY 전환을 방지합니다.
    /// </summary>
    public IEnumerator FocusCombatTargets(System.Collections.Generic.IReadOnlyList<Transform> targets)
    {
        if (targetCamera == null || targets == null || targets.Count == 0)
            yield break;

        Vector3 center = Vector3.zero;
        int validCount = 0;
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null)
                continue;
            center += targets[i].position;
            validCount++;
        }

        if (validCount == 0)
            yield break;

        center /= validCount;
        Vector3 start = targetCamera.transform.position;
        Vector2 destinationXY = new Vector2(center.x + zoomOffset.x, center.y + zoomOffset.y);
        Vector2 offset = destinationXY - new Vector2(start.x, start.y);
        if (combatTargetFocusMaxDistance > 0f)
            offset = Vector2.ClampMagnitude(offset, combatTargetFocusMaxDistance);

        if (offset.sqrMagnitude < 0.0001f)
            yield break;

        RemoveMouseParallax();
        EndZoomFollowTarget();
        if (routine != null)
            StopCoroutine(routine);
        ClearImpactOffset();

        Vector3 destination = new Vector3(start.x + offset.x, start.y + offset.y, start.z);
        routine = StartCoroutine(MoveCamera(
            destination,
            targetCamera.orthographicSize,
            Mathf.Max(0.05f, combatTargetFocusDuration),
            false,
            false));
        yield return routine;
    }

    [Header("행동 범위 카메라 구도")]
    [Tooltip("시전자와 모든 대상이 화면 가장자리에 닿지 않도록 확보하는 여백 비율입니다.")]
    [SerializeField, Range(0f, 0.5f)] private float actionFramePadding = 0.20f;
    [Tooltip("단일 대상일 때도 지나치게 확대되지 않도록 보장하는 반쪽 화면 높이(월드 단위)입니다.")]
    [SerializeField, Min(0.1f)] private float actionFrameMinHalfHeight = 1.1f;
    [Tooltip("원거리 공격 시 시전자가 배치될 화면 좌우 가장자리 여백 비율입니다. 0.15는 왼쪽/오른쪽에서 15% 지점입니다.")]
    [SerializeField, Range(0.05f, 0.4f)] private float rangedCasterScreenEdgePadding = 0.15f;

    /// <summary>
    /// 시전자와 실제 효과 대상 전체를 한 구도로 잡습니다.
    /// 시전자 -> 피격자 이동 단계를 없애고, 시작 위치에서 최종 구도로 한 번만 이동합니다.
    /// 버프/디버프는 원거리 줌을 기준으로 하며 시전자 자신이 대상에 포함되어도 추가 이동하지 않습니다.
    /// </summary>
    public IEnumerator ZoomForActionTargets(
        Transform caster, System.Collections.Generic.IReadOnlyList<Transform> targets,
        bool isStatusSkill, bool isRangedSkill, Vector3? fixedWorldTarget = null)
    {
        if (targetCamera == null || caster == null)
            yield break;

        // EndZoomFollowTarget() clears this flag; capture it before changing camera ownership.
        bool transitioningFromGridMovement = gridMovementFocusedUnit != null;
        Vector3 movementCameraPosition = targetCamera.transform.position;
        bool useRangedFrame = isStatusSkill || isRangedSkill;
        hasActiveGridSelectionFocus = false;
        hasActiveCombatZoom = true;
        activeCombatFocusOwner = caster;
        activeCombatFocusMode = useRangedFrame
            ? CombatCameraFocusMode.Ranged : CombatCameraFocusMode.Melee;
        ResetDamageImpactRotationSequence();
        RemoveMouseParallax();
        hasActiveMonsterInfoFocus = false;
        EndZoomFollowTarget();
        if (routine != null)
            StopCoroutine(routine);
        ClearImpactOffset();

        // 시전자를 항상 포함하므로 자기 자신 버프, 대상이 없는 공격도 같은 흐름입니다.
        float minX = caster.position.x;
        float maxX = minX;
        float minY = caster.position.y;
        float maxY = minY;
        if (targets != null)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                Transform target = targets[i];
                if (target == null)
                    continue;
                Vector3 p = target.position;
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y);
                maxY = Mathf.Max(maxY, p.y);
            }
        }

        // Ground-targeted projectiles have a fixed impact cell, not a moving
        // Transform. Include that cell in the framing bounds.
        if (fixedWorldTarget.HasValue)
        {
            Vector3 point = fixedWorldTarget.Value;
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minY = Mathf.Min(minY, point.y);
            maxY = Mathf.Max(maxY, point.y);
        }

        // 근거리 공격은 시전자와 실제 피격 대상(들)을 포함한 구도의 중심을 사용합니다.
        // 직접 그리드 이동은 별도의 FocusMovingGridUnit에서 이동 유닛을 중심으로 유지합니다.
        // 원거리 공격은 대상 방향에 따라 시전자를 화면 좌/우 가장자리 근처에 배치합니다.
        // 상태 효과는 기존처럼 시전자와 효과 대상 전체의 중앙 구도를 유지합니다.
        Vector3 destination = targetCamera.transform.position;
        float targetAverageX = caster.position.x;
        int targetCountForFraming = 0;
        if (targets != null)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                Transform target = targets[i];
                if (target == null || target == caster)
                    continue;
                targetAverageX += target.position.x;
                targetCountForFraming++;
            }
        }
        if (fixedWorldTarget.HasValue)
        {
            targetAverageX = fixedWorldTarget.Value.x;
            targetCountForFraming = 1;
        }
        else if (targetCountForFraming > 0)
            targetAverageX = (targetAverageX - caster.position.x) / targetCountForFraming;
        else
            targetAverageX = caster.position.x;

        // All attacks frame caster + actual targets/impact at their midpoint.
        // An edge frame with targets directly above/below used to send the camera
        // far along X even though the attack has no horizontal separation.
        bool rangedEdgeFrame = false;
        float attackDirection = targetAverageX >= caster.position.x ? 1f : -1f;
        if (Mathf.Abs(targetAverageX - caster.position.x) < 0.001f)
            rangedEdgeFrame = false;

        // Melee: center the frame on both caster and hit target(s), not caster alone.
        // Status skills retain their shared bounds framing; ranged attacks retain
        // the dedicated edge framing below.
        destination.x = (minX + maxX) * 0.5f + zoomOffset.x;
        destination.y = (minY + maxY) * 0.5f + zoomOffset.y;

        float desiredZ = useRangedFrame ? rangedExecutionZoomZPosition :
            usePositionZZoom ? GetZoomZPosition() : destination.z;
        float desiredSize = !useRangedFrame && useOrthographicSizeZoom
            ? zoomSize : targetCamera.orthographicSize;

        float aspect = Mathf.Max(0.1f, targetCamera.aspect);
        float paddingFactor = 1f + Mathf.Max(0f, actionFramePadding);
        float halfWidth = Mathf.Max(0f, (maxX - minX) * 0.5f) * paddingFactor;
        float halfHeight = Mathf.Max(actionFrameMinHalfHeight,
            Mathf.Max(0f, (maxY - minY) * 0.5f) * paddingFactor);
        float requiredHalfHeight = Mathf.Max(halfHeight, halfWidth / aspect);

        if (rangedEdgeFrame)
        {
            // Keep the caster near an edge while giving the targets the rest of
            // the screen. The farthest target determines the minimum view width.
            float farthestForward = 0f;
            float farthestBackward = 0f;
            if (targets != null)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    Transform target = targets[i];
                    if (target == null)
                        continue;
                    float forward = (target.position.x - caster.position.x) * attackDirection;
                    farthestForward = Mathf.Max(farthestForward, forward);
                    farthestBackward = Mathf.Max(farthestBackward, -forward);
                }
            }
            float edgeFraction = Mathf.Clamp(rangedCasterScreenEdgePadding, 0.05f, 0.4f);
            float forwardWidth = farthestForward * paddingFactor / (2f * (1f - edgeFraction));
            float backwardWidth = farthestBackward * paddingFactor / (2f * edgeFraction);
            requiredHalfHeight = Mathf.Max(requiredHalfHeight,
                Mathf.Max(forwardWidth, backwardWidth) / aspect);
        }

        if (targetCamera.orthographic)
        {
            desiredSize = Mathf.Max(desiredSize, requiredHalfHeight);
        }
        else if (usePositionZZoom)
        {
            float verticalHalfFov = targetCamera.fieldOfView * Mathf.Deg2Rad * 0.5f;
            float tanHalfFov = Mathf.Max(0.01f, Mathf.Tan(verticalHalfFov));
            float requiredDistance = requiredHalfHeight / tanHalfFov;
            destination.z = Mathf.Min(desiredZ, caster.position.z - requiredDistance);
        }
        if (targetCamera.orthographic || !usePositionZZoom)
            destination.z = usePositionZZoom ? desiredZ : targetCamera.transform.position.z;

        if (rangedEdgeFrame)
        {
            float edgeFraction = Mathf.Clamp(rangedCasterScreenEdgePadding, 0.05f, 0.4f);
            float viewHalfWidth = targetCamera.orthographic
                ? desiredSize * aspect
                : Mathf.Max(0.01f, caster.position.z - destination.z)
                    * Mathf.Tan(targetCamera.fieldOfView * Mathf.Deg2Rad * 0.5f) * aspect;
            destination.x = caster.position.x + zoomOffset.x
                + attackDirection * viewHalfWidth * (1f - 2f * edgeFraction);
        }

        float actionYaw = 0f;
        if (enableActionDirectionYaw && !isStatusSkill && fixedWorldTarget.HasValue)
        {
            float differenceX = fixedWorldTarget.Value.x - caster.position.x;
            if (Mathf.Abs(differenceX) >= actionYawDeadZone)
                actionYaw = differenceX < 0f ? actionDirectionYaw : -actionDirectionYaw;
        }
        else if (enableActionDirectionYaw && !isStatusSkill && targets != null)
        {
            float targetXSum = 0f;
            int targetCount = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] == null || targets[i] == caster)
                    continue;
                targetXSum += targets[i].position.x;
                targetCount++;
            }
            if (targetCount > 0)
            {
                float differenceX = targetXSum / targetCount - caster.position.x;
                if (Mathf.Abs(differenceX) >= actionYawDeadZone)
                    actionYaw = differenceX < 0f ? actionDirectionYaw : -actionDirectionYaw;
            }
        }

        // 카메라 회전으로 화면 가장자리가 잘리는 것을 줄이기 위해 원근 거리만 보수적으로 보정합니다.
        if (compensateActionYawFrame && !targetCamera.orthographic && usePositionZZoom && Mathf.Abs(actionYaw) > 0.001f)
        {
            float yawRadians = Mathf.Abs(actionYaw) * Mathf.Deg2Rad;
            float extraDistance = requiredHalfHeight * Mathf.Tan(yawRadians);
            destination.z = Mathf.Min(destination.z, caster.position.z - requiredHalfHeight /
                Mathf.Max(0.01f, Mathf.Tan(targetCamera.fieldOfView * Mathf.Deg2Rad * 0.5f)) - extraDistance);
        }

        // The camera's yaw changes the projected screen X of every target.
        // Solve for the camera X that puts the desired anchor at its requested
        // viewport position AFTER yaw and Z framing have been decided.
        // Melee: caster + all hit targets at screen center.
        // Ranged: caster near the screen edge facing the targets.
        // Status: preserve the existing shared center framing.
        Vector3 screenAnchor = new Vector3(
            (minX + maxX) * 0.5f,
            (minY + maxY) * 0.5f,
            caster.position.z);
        float anchorViewportX = 0.5f;
        if (rangedEdgeFrame)
        {
            screenAnchor = caster.position;
            float edgeFraction = Mathf.Clamp(rangedCasterScreenEdgePadding, 0.05f, 0.4f);
            anchorViewportX = attackDirection > 0f ? edgeFraction : 1f - edgeFraction;
        }
        Quaternion framingRotation = Quaternion.Euler(0f, actionYaw, 0f);
        destination.x = CalculateCameraXForScreenAnchor(
            screenAnchor, destination.z, framingRotation, anchorViewportX, desiredSize)
            + zoomOffset.x;
        destination += GetCombatEdgeCameraOffset(screenAnchor);

        // Normal attacks obey the inspector clamp. A direct grid movement may already
        // have placed the camera beyond that range. Clamping the *first* frame of the
        // next action would snap the camera back into the grid.
        bool keepMovementXY = false;
        if (keepMovementXY)
        {
            destination.x = movementCameraPosition.x;
            destination.y = movementCameraPosition.y;
        }

        // Combat actions use the same unrestricted framing as direct movement.
        // Do not snap back to the inspector's normal/selection camera bounds.
        float duration = useRangedFrame ? rangedExecutionZoomDuration : zoomDuration;
        routine = StartCoroutine(MoveCamera(
            destination, desiredSize, duration, false,
            targetCamera.orthographic || (!useRangedFrame && useOrthographicSizeZoom),
            false, actionYaw));
        yield return routine;
    }

    // Preview the new action's zoom/yaw while retaining the current movement X/Y.
    // When every participant fits inside this view, no recentering is necessary.
    private bool AreActionTargetsVisibleAtCameraPosition(
        Vector3 currentPosition, float actionZ, float actionYaw, float actionSize,
        Transform caster, System.Collections.Generic.IReadOnlyList<Transform> targets)
    {
        if (targetCamera == null || caster == null)
            return false;

        Transform cameraTransform = targetCamera.transform;
        Vector3 originalPosition = cameraTransform.position;
        Quaternion originalRotation = cameraTransform.rotation;
        float originalSize = targetCamera.orthographicSize;
        float pad = Mathf.Clamp(movementToActionViewportPadding, 0f, 0.3f);
        try
        {
            cameraTransform.position = new Vector3(currentPosition.x, currentPosition.y, actionZ);
            cameraTransform.rotation = Quaternion.Euler(0f, actionYaw, 0f);
            if (targetCamera.orthographic)
                targetCamera.orthographicSize = actionSize;

            if (!IsWithinActionViewport(caster.position, pad))
                return false;
            if (targets != null)
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    Transform target = targets[i];
                    if (target != null && !IsWithinActionViewport(target.position, pad))
                        return false;
                }
            }
            return true;
        }
        finally
        {
            cameraTransform.position = originalPosition;
            cameraTransform.rotation = originalRotation;
            if (targetCamera.orthographic)
                targetCamera.orthographicSize = originalSize;
        }
    }

    private bool IsWithinActionViewport(Vector3 worldPosition, float padding)
    {
        Vector3 viewport = targetCamera.WorldToViewportPoint(worldPosition);
        return viewport.z > 0f &&
               viewport.x >= padding && viewport.x <= 1f - padding &&
               viewport.y >= padding && viewport.y <= 1f - padding;
    }

    // Place a world-space anchor at a specific horizontal screen fraction,
    // while preserving the requested camera Z and yaw.  The perspective case
    // also accounts for the change in camera-relative depth caused by yaw.
    // The grid is indexed by column (five rows per column). The camera is
    // nudged inward near columns 0-1 and 5-6, without enabling world clamps.
    // This also works with attacks focused between units, not only cell centers.
    // Both the movement focus and all action/cutscene focuses use this one
    // world-space offset. The bottom correction applies only at the two outer
    // columns, not to the center of the battlefield.
    private Vector3 GetCombatEdgeCameraOffset(Vector3 focusWorldPosition)
    {
        if (!useCombatEdgeFraming)
            return Vector3.zero;

        if (combatEdgeGridManager == null)
            combatEdgeGridManager = FindFirstObjectByType<GridManager>();
        GridManager grid = combatEdgeGridManager;
        if (grid == null || grid.Width < 5 || grid.Height < 5)
            return Vector3.zero;

        int height = grid.Height;
        float leftStartX = grid.GetWorldPositionByIndex(0).x;
        float leftEndX = grid.GetWorldPositionByIndex(height).x;
        float rightStartX = grid.GetWorldPositionByIndex((grid.Width - 2) * height).x;
        float rightEndX = grid.GetWorldPositionByIndex((grid.Width - 1) * height).x;
        bool increasingX = rightEndX > leftStartX;
        float directionalX = increasingX ? focusWorldPosition.x : -focusWorldPosition.x;
        float leftBoundary = increasingX ? leftEndX : -leftEndX;
        float rightBoundary = increasingX ? rightStartX : -rightStartX;

        float leftFadeEnd = leftBoundary + Mathf.Abs(leftEndX - leftStartX);
        float rightFadeStart = rightBoundary - Mathf.Abs(rightEndX - rightStartX);
        float leftWeight = 0f;
        float rightWeight = 0f;
        if (directionalX <= leftFadeEnd)
        {
            float t = Mathf.InverseLerp(leftBoundary, leftFadeEnd, directionalX);
            leftWeight = 1f - Mathf.SmoothStep(0f, 1f, t);
        }
        else if (directionalX >= rightFadeStart)
        {
            float t = Mathf.InverseLerp(rightFadeStart, rightBoundary, directionalX);
            rightWeight = Mathf.SmoothStep(0f, 1f, t);
        }

        float xInset = leftEdgeCameraInset * leftWeight - rightEdgeCameraInset * rightWeight;
        if (!increasingX)
            xInset = -xInset;

        // With column-major indexing, rows 3 and 4 are the bottom two cells.
        // Start blending at row 2 and reach full correction at row 3.
        // Derive the positive/upward direction from actual grid world positions.
        float row2Y = grid.GetWorldPositionByIndex(2).y;
        float row3Y = grid.GetWorldPositionByIndex(3).y;
        float topY = grid.GetWorldPositionByIndex(0).y;
        float bottomY = grid.GetWorldPositionByIndex(height - 1).y;
        float bottomWeight = Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(row2Y, row3Y, focusWorldPosition.y));
        float towardTop = topY >= bottomY ? 1f : -1f;
        float yInset = towardTop * bottomEdgeCameraInset *
            Mathf.Max(leftWeight, rightWeight) * bottomWeight;
        return new Vector3(xInset, yInset, 0f);
    }

    private float CalculateCameraXForScreenAnchor(
        Vector3 anchor, float cameraZ, Quaternion cameraRotation,
        float viewportX, float orthographicSize)
    {
        if (targetCamera == null)
            return anchor.x;

        Vector3 right = cameraRotation * Vector3.right;
        Vector3 forward = cameraRotation * Vector3.forward;
        float depthDelta = anchor.z - cameraZ;
        float centeredX = Mathf.Clamp01(viewportX) * 2f - 1f;
        float localX;
        float localZ;

        if (targetCamera.orthographic)
        {
            localX = centeredX * Mathf.Max(0.01f, orthographicSize) * targetCamera.aspect;
            if (Mathf.Abs(forward.z) < 0.0001f)
                return anchor.x;
            localZ = (depthDelta - right.z * localX) / forward.z;
        }
        else
        {
            float tangent = Mathf.Tan(targetCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            float screenSlope = centeredX * tangent * targetCamera.aspect;
            float denominator = forward.z + right.z * screenSlope;
            if (Mathf.Abs(denominator) < 0.0001f)
                return anchor.x;
            localZ = depthDelta / denominator;
            localX = screenSlope * localZ;
        }

        return anchor.x - (right.x * localX + forward.x * localZ);
    }

    public void SetHoldDefaultReturn(bool hold)
    {
        holdDefaultReturn = hold;
    }

    public void BeginZoomFollowTarget(Transform target)
    {
        zoomFollowTarget = target;
        zoomFollowVelocity = Vector3.zero;
    }

    public void EndZoomFollowTarget()
    {
        gridMovementFocusedUnit = null;
        zoomFollowTarget = null;
        zoomFollowVelocity = Vector3.zero;
    }

    public IEnumerator ReturnDefaultIfNotHeld()
    {
        if (holdDefaultReturn)
            yield break;

        yield return ReturnDefault();
    }

    public IEnumerator ZoomToPosition(Vector3 worldPosition)
    {
        if (targetCamera == null)
            yield break;

        RemoveMouseParallax();
        hasActiveMonsterInfoFocus = false;
        EndZoomFollowTarget();

        if (routine != null)
            StopCoroutine(routine);

        ClearImpactOffset();

        Vector3 targetPos = worldPosition;
        targetPos.x += zoomOffset.x;
        targetPos.y += zoomOffset.y;
        targetPos.z = usePositionZZoom
            ? GetZoomZPosition()
            : targetCamera.transform.position.z;

        float targetSize = useOrthographicSizeZoom ? zoomSize : targetCamera.orthographicSize;
        routine = StartCoroutine(MoveCamera(
            targetPos,
            targetSize,
            zoomDuration,
            clampZoomPosition,
            useOrthographicSizeZoom));
        yield return routine;
    }

    public void FocusOnCharacterSelection(Transform target)
    {
        FocusOnCharacterSelection(target, -1);
    }

    public void FocusOnCharacterSelection(Transform target, int gridIndex)
    {
        if (!enableCharacterSelectionFocus)
            return;

        if (targetCamera == null || target == null)
            return;

        if (hasActiveCombatZoom)
            return;

        RemoveMouseParallax();
        hasActiveMonsterInfoFocus = false;
        hasActiveGridSelectionFocus = true;
        EndZoomFollowTarget();

        if (routine != null)
            StopCoroutine(routine);

        ClearImpactOffset();

        Vector2 gridFocusOffset = GetCharacterSelectionGridFocusOffset(gridIndex);

        // 선택 캐릭터 포커스는 캐릭터의 월드 좌표를 기준으로 잡지 않는다.
        // 기본 카메라 위치를 기준으로 그리드 번호에 따른 고정 포커스 좌표만 적용한다.
        // 기본 카메라가 (0, 0, -20)이라면 선택 포커스 좌표는 아래 범위 안에서 움직인다.
        // X: -1 ~ 1, Y: -1 ~ 0.5, Z: -17.5
        Vector3 targetPos = defaultPosition;

        if (useGridBasedCharacterSelectionFocusOffset && gridIndex >= 0)
        {
            targetPos.x = defaultPosition.x + gridFocusOffset.x;
            targetPos.y = defaultPosition.y + gridFocusOffset.y;
        }
        else
        {
            targetPos.x += characterSelectionFocusOffset.x;
            targetPos.y += characterSelectionFocusOffset.y;
        }

        targetPos.z = useCharacterSelectionFocusZ
            ? GetCharacterSelectionFocusZPosition()
            : targetCamera.transform.position.z;

        float targetSize = useCharacterSelectionFocusOrthographicSize
            ? characterSelectionFocusOrthographicSize
            : targetCamera.orthographicSize;

        float focusDuration = Mathf.Max(characterSelectionFocusDuration, minimumCharacterSelectionFocusDuration);

        routine = StartCoroutine(MoveCamera(
            targetPos,
            targetSize,
            focusDuration,
            clampCharacterSelectionFocusPosition,
            useCharacterSelectionFocusOrthographicSize));
    }

    // Compatibility with other monster selection callers; the shared focus settings apply.
    public void FocusMonsterInfo(Transform target)
    {
        FocusMonsterInfoForGridIndex(target, -1);
    }

    public void FocusMonsterInfoWithPanelSide(Transform target, bool panelOnLeft)
    {
        FocusMonsterInfoForGridIndex(target, -1);
    }

    public void ReturnDefaultFromMonsterInfoFocus()
    {
        if (activeMonsterPhaseCamera != null)
            return;
        if (!hasActiveMonsterInfoFocus)
            return;

        // BattleCharacterPanel에 캐릭터 또는 몬스터 선택이 남아 있다면
        // 패널 내부 UI 조작으로 간주하고 선택 포커스를 유지합니다.
        if (HasActiveInfoSelection())
            return;

        if (targetCamera == null)
            return;

        RemoveMouseParallax();
        if (!isActiveAndEnabled)
        {
            ForceReturnDefaultImmediate();
            return;
        }

        EndZoomFollowTarget();

        if (routine != null)
            StopCoroutine(routine);

        ClearImpactOffset();

        hasActiveMonsterInfoFocus = false;
        hasActiveGridSelectionFocus = false;
        MoveCameraWithOptionalImmediate(
            GetNeutralBattlePosition(),
            defaultSize,
            Mathf.Max(characterSelectionFocusDuration, minimumCharacterSelectionFocusDuration),
            false,
            true);
    }

    private Vector3 GetNeutralBattlePosition()
    {
        Vector3 neutralPosition = defaultPosition;
        neutralPosition.y = PanelDownPositionY;
        return neutralPosition;
    }

    public void ForceReturnMapImmediate()
    {
        ClearMonsterPhaseCameraState();
        pendingTurnCameraReturn = false;
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        if (panelDownReturnRoutine != null)
        {
            StopCoroutine(panelDownReturnRoutine);
            panelDownReturnRoutine = null;
        }

        ClearMouseParallaxImmediate();
        EndZoomFollowTarget();

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        ClearImpactOffset();

        targetCamera.transform.position = mapPosition;
        targetCamera.transform.rotation = Quaternion.identity;
        targetCamera.orthographicSize = defaultSize;
        BattleEffectPlaneRotation.UpdateCameraReturnFeedback(1f);
        hasActiveCombatZoom = false;
        ResetCombatFocusTracking();
        ResetDamageImpactRotationSequence();
        hasActiveMonsterInfoFocus = false;
        hasActiveGridSelectionFocus = false;
    }

    public void StartReturnPanelDown()
    {
        if (panelDownReturnRoutine != null)
            return;

        StartReturnPanelDownInternal(returnDuration);
    }

    public void StartReturnPanelDown(float moveDuration)
    {
        if (panelDownReturnRoutine != null)
        {
            StopCoroutine(panelDownReturnRoutine);
            panelDownReturnRoutine = null;
        }

        StartReturnPanelDownInternal(moveDuration);
    }

    private void StartReturnPanelDownInternal(float moveDuration, bool forceMonsterInfo = false)
    {
        if (activeMonsterPhaseCamera != null)
            return;
        // 실행 중 또는 턴 종료 후 캐릭터 포커스 복귀가 예정되어 있다면
        // 패널 이동 이벤트가 카메라를 Y=1로 덮어쓰지 않게 합니다.
        if (!forceMonsterInfo && (hasActiveMonsterInfoFocus || ShouldPreserveTurnSelectionFocus()))
            return;

        if (targetCamera == null)
            return;

        RemoveMouseParallax();
        if (!isActiveAndEnabled)
        {
            ForceReturnPanelDownImmediate();
            return;
        }

        panelDownReturnRoutine = StartCoroutine(ReturnPanelDownRoutine(moveDuration));
    }

    private IEnumerator ReturnPanelDownRoutine(float moveDuration)
    {
        yield return ReturnPanelDown(moveDuration);
        panelDownReturnRoutine = null;
    }

    public IEnumerator ReturnPanelDown()
    {
        yield return ReturnPanelDown(returnDuration);
    }

    private IEnumerator ReturnPanelDown(float moveDuration)
    {
        if (activeMonsterPhaseCamera != null)
            yield break;
        if (targetCamera == null)
            yield break;

        RemoveMouseParallax();
        EndZoomFollowTarget();

        if (routine != null)
            StopCoroutine(routine);

        ClearImpactOffset();

        ApplyCameraRotationZ(0f);
        ResetDamageImpactRotationSequence();

        routine = StartCoroutine(MoveCamera(GetNeutralBattlePosition(), defaultSize, moveDuration, false, true, false, 0f));
        yield return routine;

        ApplyCameraRotationZ(0f);
        hasActiveCombatZoom = false;
        ResetCombatFocusTracking();
        hasActiveMonsterInfoFocus = false;
        hasActiveGridSelectionFocus = false;
    }

    public void ForceReturnPanelDownImmediate()
    {
        ClearMonsterPhaseCameraState();
        pendingTurnCameraReturn = false;
        if (targetCamera == null)
            return;

        if (panelDownReturnRoutine != null)
        {
            StopCoroutine(panelDownReturnRoutine);
            panelDownReturnRoutine = null;
        }

        ClearMouseParallaxImmediate();
        EndZoomFollowTarget();

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        ClearImpactOffset();

        targetCamera.transform.position = GetNeutralBattlePosition();
        targetCamera.transform.rotation = Quaternion.identity;
        targetCamera.orthographicSize = defaultSize;
        BattleEffectPlaneRotation.UpdateCameraReturnFeedback(1f);
        hasActiveCombatZoom = false;
        ResetCombatFocusTracking();
        ResetDamageImpactRotationSequence();
        hasActiveMonsterInfoFocus = false;
        hasActiveGridSelectionFocus = false;
    }

    public void StartReturnDefault()
    {
        // BattleCharacterPanel이 올라와 있는 동안에는 카메라를 기본 위치로
        // 복귀시키지 않습니다. 실제 선택이 모두 해제되어 패널이 내려갈 때만
        // 기본 위치 복귀를 허용합니다.
        if (HasActiveInfoSelection())
            return;

        StartReturnDefaultCore();
    }

    public void StartReturnDefaultKeepingCharacterSelection()
    {
        // 우클릭으로 캐릭터 선택은 유지하면서 카메라만 복귀할 때 사용합니다.
        // 몬스터 정보 포커스가 활성 상태라면 기존 닫기 흐름이 소유권을 유지합니다.
        if (MonsterUnit.CurrentInfoSelectedMonster != null)
            return;

        StartReturnDefaultCore();
    }

    private void StartReturnDefaultCore()
    {
        if (activeMonsterPhaseCamera != null)
            return;

        if (targetCamera == null)
            return;

        RemoveMouseParallax();
        if (!isActiveAndEnabled)
        {
            ForceReturnDefaultImmediate();
            return;
        }

        StartCoroutine(ReturnDefault());
    }

    private bool HasActiveInfoSelection()
    {
        if (MonsterUnit.CurrentInfoSelectedMonster != null)
            return true;

        BattleTimelineController timelineController =
            FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);

        return timelineController != null &&
               timelineController.SelectedCharacter != null;
    }

    // Boss intro camera framing is temporary. Resume camera focus calculations
    // from the map's baseline Y rather than the temporary panel-down Y.
    public void ForceReturnSceneStartPositionImmediate()
    {
        ForceReturnDefaultImmediate();

        // A resumed scene may initialize the camera controller while the camera
        // is already at the panel-down height. Do not cache that as the baseline.
        defaultPosition.y = mapPosition.y;
        if (targetCamera != null)
            targetCamera.transform.position = defaultPosition;
    }

    public void ForceReturnDefaultImmediate()
    {
        ClearMonsterPhaseCameraState();
        pendingTurnCameraReturn = false;
        if (targetCamera == null)
            return;

        if (panelDownReturnRoutine != null)
        {
            StopCoroutine(panelDownReturnRoutine);
            panelDownReturnRoutine = null;
        }

        ClearMouseParallaxImmediate();
        EndZoomFollowTarget();

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        ClearImpactOffset();

        targetCamera.transform.position = GetNeutralBattlePosition();
        targetCamera.transform.rotation = Quaternion.identity;
        targetCamera.orthographicSize = defaultSize;
        hasActiveCombatZoom = false;
        ResetCombatFocusTracking();
        ResetDamageImpactRotationSequence();
        hasActiveMonsterInfoFocus = false;
        hasActiveGridSelectionFocus = false;
    }

    /// <summary>
    /// 전투 종료 시 씬 기본 카메라 위치로 부드럽게 돌아갑니다.
    /// 인트로 텍스트와 결과 패널은 이 코루틴 종료 후 표시합니다.
    /// </summary>
    private IEnumerator BeginBattleEffectReturnWithLead(float cameraDuration)
    {
        activeBattleEffectReturnLeadProgress = 0f;
        bool hasEffect = BattleEffectPlaneRotation.HasActiveAttackRotationFeedback();
        BattleEffectPlaneRotation.BeginCameraReturnFeedback(cameraDuration, zoomCurve);
        if (!hasEffect || cameraDuration <= 0f || battleEffectReturnLeadTime <= 0f)
            yield break;

        float lead = Mathf.Min(battleEffectReturnLeadTime, cameraDuration * 0.5f);
        float elapsed = 0f;
        while (elapsed < lead)
        {
            elapsed += GetCameraDeltaTime();
            float t = Mathf.Clamp01(elapsed / lead);
            float leadFraction = lead / (cameraDuration + lead);
            activeBattleEffectReturnLeadProgress = leadFraction;
            BattleEffectPlaneRotation.UpdateCameraReturnFeedback(EvaluateZoomCurve(t * leadFraction));
            yield return null;
        }
    }

    private void UpdateBattleEffectReturnAlongCamera(float cameraProgress)
    {
        float t = Mathf.Lerp(activeBattleEffectReturnLeadProgress, 1f, Mathf.Clamp01(cameraProgress));
        BattleEffectPlaneRotation.UpdateCameraReturnFeedback(EvaluateZoomCurve(t));
    }

    public IEnumerator ReturnSceneStartSmooth()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
        if (targetCamera == null)
            yield break;

        if (panelDownReturnRoutine != null)
        {
            StopCoroutine(panelDownReturnRoutine);
            panelDownReturnRoutine = null;
        }

        RemoveMouseParallax();
        EndZoomFollowTarget();
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        ClearImpactOffset();
        ResetDamageImpactRotationSequence();

        // 전투 종료 처리는 카메라의 일반 이동 코루틴(routine)에 종속시키지 않습니다.
        // 다른 포커스 동작이 해당 코루틴을 중단해도 보상 흐름은 계속되어야 합니다.
        Vector3 startPosition = targetCamera.transform.position;
        float startSize = targetCamera.orthographicSize;
        Quaternion startRotation = targetCamera.transform.rotation;
        Vector3 destination = GetNeutralBattlePosition();
        if (!usePositionZZoom)
            destination.z = startPosition.z;

        float duration = Mathf.Max(0f, returnDuration);
        yield return BeginBattleEffectReturnWithLead(duration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EvaluateZoomCurve(t);
            targetCamera.transform.position = Vector3.Lerp(startPosition, destination, eased);
            targetCamera.transform.rotation = Quaternion.Slerp(startRotation, Quaternion.identity, eased);
            targetCamera.orthographicSize = Mathf.Lerp(startSize, defaultSize, eased);
            UpdateBattleEffectReturnAlongCamera(t);
            yield return null;
        }

        targetCamera.transform.position = destination;
        targetCamera.transform.rotation = Quaternion.identity;
        targetCamera.orthographicSize = defaultSize;
        hasActiveCombatZoom = false;
        ResetCombatFocusTracking();
        hasActiveMonsterInfoFocus = false;
        hasActiveGridSelectionFocus = false;
    }

    public IEnumerator ReturnDefault()
    {
        return ReturnDefaultInternal(false);
    }

    // 5번 슬롯까지 모두 완료된 후에는 실행 플래그가 남아 있어도
    // 턴 종료 회복 및 패시브 효과에 앞서 카메라를 복귀시킵니다.
    public IEnumerator ReturnDefaultAfterTimelineActions()
    {
        return ReturnDefaultInternal(true);
    }

    private IEnumerator ReturnDefaultInternal(bool timelineActionsCompleted)
    {
        // Phase camera owns its framing until all steps and its own return finish.
        if (activeMonsterPhaseCamera != null)
            yield break;
        if (targetCamera == null)
            yield break;

        // 턴 중간에는 복귀하지 않고, 모든 예약 행동 완료 후에만 예외를 허용합니다.
        if (!timelineActionsCompleted && IsTurnCameraExecutionActive())
        {
            pendingTurnCameraReturn = true;
            yield break;
        }
        pendingTurnCameraReturn = false;

        // 카메라/Plane의 기존 복귀 속도를 유지하고 마지막 행동 유닛의 Idle 전환만 맞춥니다.
        if (hasActiveCombatZoom)
            BattleUnitAnimator.SyncLastActionIdleWithCameraReturn(returnDuration);

        RemoveMouseParallax();
        EndZoomFollowTarget();

        if (routine != null)
            StopCoroutine(routine);

        ClearImpactOffset();

        ResetDamageImpactRotationSequence();

        // BattleEffect의 복귀 시작과 줌아웃의 길이/곡선을 일치시킵니다.
        yield return BeginBattleEffectReturnWithLead(returnDuration);
        routine = StartCoroutine(MoveCamera(GetNeutralBattlePosition(), defaultSize, returnDuration, false, true, true, 0f));
        yield return routine;

        ApplyCameraRotationZ(0f);
        hasActiveCombatZoom = false;
        ResetCombatFocusTracking();
        hasActiveMonsterInfoFocus = false;
        hasActiveGridSelectionFocus = false;
    }

    private void ResetCombatFocusTracking()
    {
        gridMovementFocusedUnit = null;
        activeCombatFocusOwner = null;
        activeCombatFocusMode = CombatCameraFocusMode.None;
    }

    /// <summary>
    /// 버프/디버프가 실제로 적용된 직후 현재 구도에서 짧게 확대했다가 복귀합니다.
    /// 시전자/대상 간 재이동, 화면 흔들림, 히트스톱 없이 X/Y 및 회전을 유지합니다.
    /// </summary>
    public IEnumerator PlayStatusEffectZoomPulse()
    {
        if (!enableStatusEffectZoomPulse || targetCamera == null)
            yield break;

        // 다른 카메라 이동과 중첩하지 않도록 현재 구도 진입이 끝날 때까지 기다립니다.
        if (routine != null)
            yield return routine;

        float baseSize = targetCamera.orthographicSize;
        float baseZ = targetCamera.transform.position.z;
        bool orthographic = targetCamera.orthographic;
        float targetSize = orthographic
            ? Mathf.Max(0.1f, baseSize - statusEffectPulseSizeOffset)
            : baseSize;
        float targetZ = orthographic
            ? baseZ : baseZ + statusEffectPulseZOffset;

        // 버프/디버프는 공격처럼 피해 히트스톱/충격 연출이 없기 때문에
        // 기존 짧은 줌 펄스(기본 0.36초)만 재생하면 대상이 있어도 지나치게 빨리 끝납니다.
        // 전체 구도를 유지한 채 확대 유지 시간을 보충하여 공격 적중과 유사한 템포로 맞춥니다.
        // 이 메서드는 유효 대상이 있을 때만 호출되므로 빈 범위의 30% 빠른 종료는 유지됩니다.
        float zoomInDuration = Mathf.Max(0f, statusEffectPulseZoomInDuration);
        float zoomOutDuration = Mathf.Max(0f, statusEffectPulseZoomOutDuration);
        float holdDuration = Mathf.Max(0f, statusEffectPulseHoldDuration);
        holdDuration += Mathf.Max(0f,
            statusEffectPulseMinimumDuration - (zoomInDuration + holdDuration + zoomOutDuration));
        float[] durations = { zoomInDuration, holdDuration, zoomOutDuration };
        float elapsed = 0f;
        while (elapsed < durations[0])
        {
            elapsed += GetImpactDeltaTime();
            float t = EvaluateZoomCurve(Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, durations[0])));
            ApplyStatusPulseZoom(Mathf.Lerp(baseSize, targetSize, t), Mathf.Lerp(baseZ, targetZ, t), orthographic);
            yield return null;
        }
        ApplyStatusPulseZoom(targetSize, targetZ, orthographic);

        elapsed = 0f;
        while (elapsed < durations[1])
        {
            elapsed += GetImpactDeltaTime();
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < durations[2])
        {
            elapsed += GetImpactDeltaTime();
            float t = EvaluateZoomCurve(Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, durations[2])));
            ApplyStatusPulseZoom(Mathf.Lerp(targetSize, baseSize, t), Mathf.Lerp(targetZ, baseZ, t), orthographic);
            yield return null;
        }
        ApplyStatusPulseZoom(baseSize, baseZ, orthographic);
    }

    private void ApplyStatusPulseZoom(float size, float z, bool orthographic)
    {
        if (targetCamera == null)
            return;

        if (orthographic)
            targetCamera.orthographicSize = size;
        else
        {
            Vector3 position = targetCamera.transform.position;
            position.z = z;
            targetCamera.transform.position = position;
        }
    }

    public IEnumerator PlayDamageImpact()
    {
        if (targetCamera == null || !enableDamageImpact)
            yield break;

        RemoveMouseParallax();
        ApplyNextDamageImpactRotation();

        if (routine != null)
            yield return routine;

        ClearImpactOffset();

        float baseSize = targetCamera.orthographicSize;
        float targetSize = Mathf.Max(0.1f, baseSize - Mathf.Max(0f, impactZoomAmount));
        float baseZ = hasPreparedDamageImpact
            ? preparedImpactBaseZ
            : targetCamera.transform.position.z;
        float targetZ = usePositionZZoom ? baseZ + Mathf.Max(0f, impactZoomZOffset) : baseZ;

        float baseX = hasPreparedDamageImpact
            ? preparedImpactBaseX
            : targetCamera.transform.position.x;
        int impactDirection = ConsumeNextImpactDirection();
        float compressionX = CalculateImpactBeatOffset(impactDirection, impactCompressionDistance).x;
        float recoilX = CalculateImpactBeatOffset(impactDirection, impactRecoilDistance, true).x;
        float overshootX = CalculateImpactBeatOffset(impactDirection, impactOvershootDistance).x;

        if (!hasPreparedDamageImpact)
            yield return LerpImpactHorizontalPosition(baseX, baseX + compressionX, impactCompressionDuration);

        hasPreparedDamageImpact = false;
        yield return LerpImpactZoom(baseSize, targetSize, baseZ, targetZ, impactZoomInDuration);
        yield return ShakeAndHitStop();
        yield return LerpImpactHorizontalPosition(
            targetCamera.transform.position.x,
            baseX + recoilX,
            impactRecoilDuration);
        yield return LerpImpactZoom(targetCamera.orthographicSize, baseSize, targetCamera.transform.position.z, baseZ, impactZoomOutDuration);
        yield return LerpImpactHorizontalPosition(
            targetCamera.transform.position.x,
            baseX + overshootX,
            impactSettleDuration * 0.45f);
        yield return LerpImpactHorizontalPosition(
            targetCamera.transform.position.x,
            baseX,
            impactSettleDuration * 0.55f);

        if (useOrthographicSizeZoom)
            targetCamera.orthographicSize = baseSize;

        Vector3 restoredPosition = targetCamera.transform.position;
        restoredPosition.z = baseZ;
        targetCamera.transform.position = restoredPosition;
        ClearImpactOffset();
    }

    /// <summary>
    /// 원거리 공격용 피격 카메라 연출입니다.
    /// 공격자 위치로 이동하거나 추적하지 않고, 현재 카메라 위치에서 약한 Z 줌과 흔들림/히트스톱만 재생합니다.
    /// </summary>
    public IEnumerator PlayRangedDamageImpact()
    {
        if (targetCamera == null || !enableDamageImpact)
            yield break;

        RemoveMouseParallax();

        if (routine != null)
            yield return routine;

        ClearImpactOffset();

        float baseSize = targetCamera.orthographicSize;
        float baseZ = hasPreparedRangedImpact
            ? preparedImpactBaseZ
            : targetCamera.transform.position.z;
        float targetZ = usePositionZZoom
            ? baseZ + Mathf.Max(0f, rangedImpactZoomZOffset)
            : baseZ;

        if (!hasPreparedRangedImpact)
        {
            yield return LerpImpactZoom(
                baseSize,
                baseSize,
                baseZ,
                targetZ,
                rangedImpactZoomInDuration);
        }

        hasPreparedRangedImpact = false;

        yield return ShakeAndHitStop();

        float recoilZ = CalculateImpactDepthBeat(baseZ, rangedImpactZoomZOffset * 0.35f, true);
        yield return LerpImpactZoom(
            targetCamera.orthographicSize,
            baseSize,
            targetCamera.transform.position.z,
            recoilZ,
            impactRecoilDuration);

        yield return LerpImpactZoom(
            targetCamera.orthographicSize,
            baseSize,
            targetCamera.transform.position.z,
            CalculateImpactDepthBeat(baseZ, rangedImpactZoomZOffset * 0.12f, false),
            rangedImpactZoomOutDuration * 0.45f);
        yield return LerpImpactZoom(
            targetCamera.orthographicSize,
            baseSize,
            targetCamera.transform.position.z,
            baseZ,
            rangedImpactZoomOutDuration * 0.55f);

        Vector3 restoredPosition = targetCamera.transform.position;
        restoredPosition.z = baseZ;
        targetCamera.transform.position = restoredPosition;
        ClearImpactOffset();
    }


    private void ApplyNextDamageImpactRotation()
    {
        if (targetCamera == null || !enableDamageImpactRotation)
            return;

        float rotationZ = 0f;

        if (damageImpactRotationZPattern != null && damageImpactRotationZPattern.Length > 0)
        {
            int patternIndex = Mathf.Clamp(damageImpactRotationIndex, 0, damageImpactRotationZPattern.Length - 1);
            rotationZ = damageImpactRotationZPattern[patternIndex];

            // 3회 이상 연속 타격이 이어지면 마지막 두 값(-1, 1)을 번갈아 사용한다.
            if (damageImpactRotationIndex >= damageImpactRotationZPattern.Length && damageImpactRotationZPattern.Length >= 2)
            {
                int alternateIndex = damageImpactRotationZPattern.Length - 2 + ((damageImpactRotationIndex - damageImpactRotationZPattern.Length) % 2);
                rotationZ = damageImpactRotationZPattern[alternateIndex];
            }
        }

        ApplyCameraRotationZ(rotationZ);
        damageImpactRotationIndex++;
    }

    private void ApplyCameraRotationZ(float rotationZ)
    {
        if (targetCamera == null)
            return;

        RemoveMouseParallax();
        Vector3 current = targetCamera.transform.eulerAngles;
        targetCamera.transform.rotation = Quaternion.Euler(0f, Mathf.DeltaAngle(0f, current.y), rotationZ);
    }

    private void ResetDamageImpactRotationSequence()
    {
        damageImpactRotationIndex = 0;
    }

    private IEnumerator MoveCamera(
        Vector3 targetPos,
        float targetSize,
        float duration,
        bool clampPosition = true,
        bool applyOrthographicSize = false,
        bool synchronizePlaneReturn = false,
        float targetYaw = float.NaN)
    {
        bool animateYaw = !float.IsNaN(targetYaw);
        Quaternion startRotation = targetCamera.transform.rotation;
        Quaternion destinationRotation = animateYaw ? Quaternion.Euler(0f, targetYaw, 0f) : startRotation;
        Vector3 startPos = targetCamera.transform.position;
        float startSize = targetCamera.orthographicSize;

        if (!usePositionZZoom)
            targetPos.z = startPos.z;

        if (duration <= 0f)
        {
            targetCamera.transform.position = clampPosition ? ClampCameraPosition(targetPos) : targetPos;

            if (applyOrthographicSize)
                targetCamera.orthographicSize = targetSize;
            if (animateYaw)
                targetCamera.transform.rotation = destinationRotation;
            if (synchronizePlaneReturn)
                BattleEffectPlaneRotation.UpdateCameraReturnFeedback(1f);
            routine = null;
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += GetCameraDeltaTime();
            float t = Mathf.Clamp01(timer / duration);
            float curvedT = EvaluateZoomCurve(t);

            Vector3 nextPosition = Vector3.Lerp(startPos, targetPos, curvedT);
            targetCamera.transform.position = clampPosition ? ClampCameraPosition(nextPosition) : nextPosition;

            if (applyOrthographicSize)
            {
                targetCamera.orthographicSize =
                    Mathf.Lerp(startSize, targetSize, curvedT);
            }

            if (animateYaw)
                targetCamera.transform.rotation = Quaternion.Slerp(startRotation, destinationRotation, curvedT);
            if (synchronizePlaneReturn)
                UpdateBattleEffectReturnAlongCamera(t);
            yield return null;
        }

        targetCamera.transform.position = clampPosition ? ClampCameraPosition(targetPos) : targetPos;

        if (applyOrthographicSize)
            targetCamera.orthographicSize = targetSize;
        if (animateYaw)
            targetCamera.transform.rotation = destinationRotation;
        if (synchronizePlaneReturn)
            BattleEffectPlaneRotation.UpdateCameraReturnFeedback(1f);
        routine = null;
    }

    private void MoveCameraWithOptionalImmediate(
        Vector3 targetPos,
        float targetSize,
        float duration,
        bool clampPosition,
        bool applyOrthographicSize)
    {
        if (targetCamera == null)
            return;

        if (duration <= 0f)
        {
            targetCamera.transform.position = clampPosition ? ClampCameraPosition(targetPos) : targetPos;

            if (applyOrthographicSize)
                targetCamera.orthographicSize = targetSize;

            routine = null;
            return;
        }

        routine = StartCoroutine(MoveCamera(
            targetPos,
            targetSize,
            duration,
            clampPosition,
            applyOrthographicSize));
    }

    private IEnumerator LerpImpactZoom(float startSize, float targetSize, float startZ, float targetZ, float duration)
    {
        if (targetCamera == null)
            yield break;

        if (duration <= 0f)
        {
            if (useOrthographicSizeZoom)
                targetCamera.orthographicSize = targetSize;

            Vector3 instantPosition = targetCamera.transform.position;
            instantPosition.z = targetZ;
            targetCamera.transform.position = instantPosition;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += GetImpactDeltaTime();
            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = EvaluateZoomCurve(t);

            if (useOrthographicSizeZoom)
                targetCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, curvedT);

            Vector3 position = targetCamera.transform.position;
            position.z = Mathf.Lerp(startZ, targetZ, curvedT);
            targetCamera.transform.position = position;

            yield return null;
        }

        if (useOrthographicSizeZoom)
            targetCamera.orthographicSize = targetSize;

        Vector3 finalPosition = targetCamera.transform.position;
        finalPosition.z = targetZ;
        targetCamera.transform.position = finalPosition;
    }

    private IEnumerator LerpImpactHorizontalPosition(float startX, float targetX, float duration)
    {
        if (targetCamera == null)
            yield break;

        if (duration <= 0f)
        {
            Vector3 instantPosition = targetCamera.transform.position;
            instantPosition.x = targetX;
            targetCamera.transform.position = ClampCombatImpactPosition(instantPosition);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += GetImpactDeltaTime();
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 position = targetCamera.transform.position;
            position.x = Mathf.Lerp(startX, targetX, EvaluateZoomCurve(t));
            targetCamera.transform.position = ClampCombatImpactPosition(position);
            yield return null;
        }

        Vector3 finalPosition = targetCamera.transform.position;
        finalPosition.x = targetX;
        targetCamera.transform.position = ClampCombatImpactPosition(finalPosition);
    }

    public void SetNextImpactDirection(int horizontalDirection)
    {
        nextImpactHorizontalDirection = NormalizeImpactDirection(horizontalDirection);
    }

    public IEnumerator PrepareDamageImpact(int horizontalDirection)
    {
        if (targetCamera == null || !enableDamageImpact)
            yield break;

        SetNextImpactDirection(horizontalDirection);
        preparedImpactBaseX = targetCamera.transform.position.x;
        preparedImpactBaseZ = targetCamera.transform.position.z;
        hasPreparedDamageImpact = true;
        float compressionX = CalculateImpactBeatOffset(
            horizontalDirection,
            impactCompressionDistance).x;
        yield return LerpImpactHorizontalPosition(
            preparedImpactBaseX,
            preparedImpactBaseX + compressionX,
            impactCompressionDuration);
    }

    public IEnumerator PrepareRangedDamageImpact()
    {
        if (targetCamera == null || !enableDamageImpact)
            yield break;

        preparedImpactBaseZ = targetCamera.transform.position.z;
        hasPreparedRangedImpact = true;
        float compressedZ = CalculateImpactDepthBeat(
            preparedImpactBaseZ,
            rangedImpactZoomZOffset,
            false);
        yield return LerpImpactZoom(
            targetCamera.orthographicSize,
            targetCamera.orthographicSize,
            preparedImpactBaseZ,
            compressedZ,
            rangedImpactZoomInDuration);
    }

    public void CancelPreparedDamageImpact()
    {
        if (targetCamera == null)
            return;

        if (hasPreparedDamageImpact)
        {
            Vector3 position = targetCamera.transform.position;
            position.x = preparedImpactBaseX;
            targetCamera.transform.position = ClampCombatImpactPosition(position);
        }

        if (hasPreparedRangedImpact)
        {
            Vector3 position = targetCamera.transform.position;
            position.z = preparedImpactBaseZ;
            targetCamera.transform.position = position;
        }

        hasPreparedDamageImpact = false;
        hasPreparedRangedImpact = false;
    }

    private int ConsumeNextImpactDirection()
    {
        int direction = nextImpactHorizontalDirection;
        nextImpactHorizontalDirection = 1;
        return direction;
    }

    public static int NormalizeImpactDirection(int horizontalDirection)
    {
        return horizontalDirection < 0 ? -1 : 1;
    }

    public static Vector3 CalculateImpactBeatOffset(
        int horizontalDirection,
        float distance,
        bool invertDirection = false)
    {
        float safeDistance = Mathf.Max(0f, distance);
        int direction = NormalizeImpactDirection(horizontalDirection);
        if (invertDirection)
            direction *= -1;

        return new Vector3(direction * safeDistance, 0f, 0f);
    }

    public static float CalculateImpactDepthBeat(float baseZ, float distance, bool recoil)
    {
        float safeDistance = Mathf.Max(0f, distance);
        return baseZ + (recoil ? -safeDistance : safeDistance);
    }

    private IEnumerator ShakeAndHitStop()
    {
        if (targetCamera == null)
            yield break;

        // 타격 순간에만 Noise Flow를 잠깐 가속합니다 (공격 시작이나 줌 유지와 무관).
        BattleEffectPlaneRotation.PlayHitNoiseFlowPulse();

        float shakeDuration = Mathf.Max(0f, impactShakeDuration);
        float shakeStrength = Mathf.Max(0f, impactShakeStrength);
        float shakeFrequency = Mathf.Max(1f, impactShakeFrequency);
        float hitStopDuration = Mathf.Max(0f, impactHitStopDuration);

        if (shakeDuration <= 0f && hitStopDuration <= 0f)
            yield break;

        float elapsed = 0f;
        float hitStopElapsed = 0f;
        bool hitStopRunning = hitStopDuration > 0f;
        float seedX = Random.Range(0f, 1000f);
        float seedY = Random.Range(0f, 1000f);

        if (hitStopRunning)
        {
            CancelImpactRecoverySpeedBoost();
            previousTimeScale = Time.timeScale;
            BattleVfxPlaybackPauseController.PauseAll();
            Time.timeScale = Mathf.Min(previousTimeScale, Mathf.Clamp01(impactHitStopTimeScale));
            isImpactHitStopActive = true;
        }

        activeImpactOffset = Vector3.zero;
        lastImpactAppliedPosition = targetCamera.transform.position;

        while (elapsed < shakeDuration || hitStopRunning)
        {
            float deltaTime = GetImpactDeltaTime();

            if (hitStopRunning)
            {
                hitStopElapsed +=
                    BattleConsecutiveActionPresentationContext.ScaleDeltaTime(
                        Time.unscaledDeltaTime);

                if (hitStopElapsed >= hitStopDuration)
                {
                    RestoreTimeScaleIfNeeded();
                    hitStopRunning = false;
                }
            }

            if (elapsed < shakeDuration && shakeStrength > 0f)
            {
                elapsed += deltaTime;
                float t = shakeDuration > 0f ? Mathf.Clamp01(elapsed / shakeDuration) : 1f;
                float fade = 1f - t;

                Vector3 currentPosition = targetCamera.transform.position;
                Vector3 basePosition = currentPosition;

                if ((currentPosition - lastImpactAppliedPosition).sqrMagnitude < 0.0001f)
                    basePosition = currentPosition - activeImpactOffset;

                float noiseTime = elapsed * shakeFrequency;
                float x = (Mathf.PerlinNoise(seedX, noiseTime) - 0.5f) * 2f;
                float y = (Mathf.PerlinNoise(seedY, noiseTime) - 0.5f) * 2f;

                activeImpactOffset = new Vector3(x, y, 0f) * shakeStrength * fade;
                targetCamera.transform.position = ClampCombatImpactPosition(basePosition + activeImpactOffset);
                lastImpactAppliedPosition = targetCamera.transform.position;
            }

            yield return null;
        }

        RestoreTimeScaleIfNeeded();
        ClearImpactOffset();
    }

    private void HandleZoomFollowTarget()
    {
        if (targetCamera == null || zoomFollowTarget == null || routine != null)
            return;

        Vector3 targetPosition = zoomFollowTarget.position;
        targetPosition.x += zoomOffset.x;
        targetPosition.y += zoomOffset.y;
        targetPosition.z = targetCamera.transform.position.z;
        targetPosition.x = CalculateCameraXForScreenAnchor(
            zoomFollowTarget.position, targetPosition.z, targetCamera.transform.rotation,
            0.5f, targetCamera.orthographicSize) + zoomOffset.x;
        targetPosition += GetCombatEdgeCameraOffset(zoomFollowTarget.position);

        // Keep the moving unit centered, without the normal camera position clamp.
        targetCamera.transform.position = Vector3.SmoothDamp(
            targetCamera.transform.position,
            targetPosition,
            ref zoomFollowVelocity,
            ZoomFollowSmoothTime,
            Mathf.Infinity,
            Time.deltaTime);
    }

    // 직접 Move 행동과 명시적으로 연출 소유권을 받은 강제 이동만 이 API를 사용합니다.
    public void BeginDirectGridMovement(Transform movingUnit)
    {
        NotifyGridMovementImpactZoom(movingUnit);
    }

    // 현재 소유자가 예상한 유닛일 때만 다음 유닛으로 원자적으로 포커스를 넘깁니다.
    // 지연된 이전 행동 콜백이 새 카메라 소유권을 빼앗는 것을 방지합니다.
    public bool TransferDirectGridMovementFocus(Transform expectedCurrentUnit, Transform nextUnit)
    {
        if (expectedCurrentUnit == null || nextUnit == null)
            return false;

        bool ownsDirectFocus = gridMovementFocusedUnit == expectedCurrentUnit;
        bool ownsFollowFocus = zoomFollowTarget == expectedCurrentUnit;
        if (!ownsDirectFocus && !ownsFollowFocus)
            return false;

        NotifyGridMovementImpactZoom(nextUnit);
        return gridMovementFocusedUnit == nextUnit;
    }

    // Call after the direct Move action ends. Keep the combat zoom until turn end,
    // but do not follow this unit when other actions later displace it.
    public void EndDirectGridMovement(Transform movingUnit)
    {
        if (movingUnit == null || gridMovementFocusedUnit != movingUnit)
            return;

        gridMovementFocusedUnit = null;
        if (zoomFollowTarget == movingUnit)
        {
            zoomFollowTarget = null;
            zoomFollowVelocity = Vector3.zero;
        }
    }

    // Compatibility for older explicit Move-action callers without a unit.
    // No reliable unit can be inferred from position deltas, so do nothing.
    public void NotifyGridMovementImpactZoom()
    {
    }

    // The moving character/monster, rather than the previous camera position,
    // becomes the framing center. Zoom is held until execution returns to the player.
    public void NotifyGridMovementImpactZoom(Transform movingUnit)
    {
        if (!enableGridMovementImpactZoom || !isActiveAndEnabled || targetCamera == null ||
            hasCinematicCameraState || movingUnit == null || !movingUnit.gameObject.activeInHierarchy)
            return;

        if (cachedTurnExecutor == null)
            cachedTurnExecutor = FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Exclude);

        bool executing = turnExecutionEventActive ||
                         (cachedTurnExecutor != null && cachedTurnExecutor.IsExecuting);
        if (!executing)
            return;

        // A repeated grid step by the same unit should extend its live camera
        // tracking, not restart the same zoom tween at every cell boundary.
        if (gridMovementFocusedUnit == movingUnit && hasActiveCombatZoom)
            return;

        RemoveMouseParallax();
        EndZoomFollowTarget();
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        ClearImpactOffset();

        gridMovementFocusedUnit = movingUnit;
        movementFocusVelocity = Vector3.zero;
        hasActiveGridSelectionFocus = false;
        hasActiveMonsterInfoFocus = false;
        hasActiveCombatZoom = true;
        activeCombatFocusOwner = null;
        activeCombatFocusMode = CombatCameraFocusMode.None;
        pendingTurnCameraReturn = true;

        routine = StartCoroutine(FocusMovingGridUnit(movingUnit));
    }

    private IEnumerator FocusMovingGridUnit(Transform unit)
    {
        if (targetCamera == null || unit == null)
        {
            routine = null;
            yield break;
        }

        Vector3 startPosition = targetCamera.transform.position;
        float startSize = targetCamera.orthographicSize;
        float targetSize = useOrthographicSizeZoom ? zoomSize : startSize;
        float duration = Mathf.Max(0f, zoomDuration);
        float elapsed = 0f;

        // Sample unit.position each frame so the XY focus follows a unit that
        // is travelling between grid cells during the zoom-in itself.
        while (elapsed < duration && unit != null && gridMovementFocusedUnit == unit)
        {
            elapsed += GetCameraDeltaTime();
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EvaluateZoomCurve(t);
            Vector3 desiredPosition = unit.position;
            desiredPosition.x += zoomOffset.x;
            desiredPosition.y += zoomOffset.y;
            desiredPosition.z = usePositionZZoom ? movementZoomZPosition : startPosition.z;
            desiredPosition.x = CalculateCameraXForScreenAnchor(
                unit.position, desiredPosition.z, targetCamera.transform.rotation,
                0.5f, useOrthographicSizeZoom ? Mathf.Lerp(startSize, targetSize, eased) : startSize)
                + zoomOffset.x;
            desiredPosition += GetCombatEdgeCameraOffset(unit.position);
            // Follow the changing target continuously instead of lerping from an
            // obsolete starting point. This avoids cornering jitter on L-shaped paths.
            Vector3 nextXY = Vector3.SmoothDamp(
                targetCamera.transform.position, desiredPosition,
                ref movementFocusVelocity, ZoomFollowSmoothTime,
                Mathf.Infinity, GetCameraDeltaTime());
            nextXY.z = Mathf.Lerp(startPosition.z, desiredPosition.z, eased);
            targetCamera.transform.position = nextXY;
            if (useOrthographicSizeZoom)
                targetCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, eased);
            yield return null;
        }

        if (unit != null && gridMovementFocusedUnit == unit)
        {
            Vector3 finalPosition = unit.position;
            finalPosition.x += zoomOffset.x;
            finalPosition.y += zoomOffset.y;
            finalPosition.z = usePositionZZoom ? movementZoomZPosition : targetCamera.transform.position.z;
            finalPosition.x = CalculateCameraXForScreenAnchor(
                unit.position, finalPosition.z, targetCamera.transform.rotation,
                0.5f, targetSize) + zoomOffset.x;
            finalPosition += GetCombatEdgeCameraOffset(unit.position);
            // Do not snap to the unit at the zoom boundary: preserve camera
            // velocity and let normal follow settle smoothly around turns.
            if (useOrthographicSizeZoom)
                targetCamera.orthographicSize = targetSize;
            // Keep tracking the unit after zoom-in, without zooming out at the
            // end of movement. A later attack focus naturally takes ownership.
            zoomFollowTarget = unit;
            zoomFollowVelocity = movementFocusVelocity;
        }
        routine = null;
    }

    private void ApplyMouseParallax()
    {
        if (targetCamera == null)
            return;

        RemoveMouseParallax();

        if (!enableMouseParallax)
        {
            currentMouseParallax = Vector2.zero;
            return;
        }

        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        Vector2 targetParallax = NormalizeMousePositionForParallax(Input.mousePosition, screenSize);
        float interpolation = GetMouseParallaxInterpolation();
        currentMouseParallax = Vector2.Lerp(currentMouseParallax, targetParallax, interpolation);

        float intensityMultiplier = GetMouseParallaxIntensityMultiplier();
        lastMouseParallaxPositionOffset = CalculateMouseParallaxPositionOffset(
            currentMouseParallax,
            mouseParallaxPositionAmount,
            intensityMultiplier);

        Vector3 eulerOffset = CalculateMouseParallaxEulerOffset(
            currentMouseParallax,
            mouseParallaxRotationAmount,
            intensityMultiplier);

        // 기존 Vector2.y 설정을 보존하면서 Y 회전만 추가로 독립 조절합니다.
        eulerOffset.y *= mouseParallaxYRotationMultiplier;
        lastMouseParallaxRotationOffset = Quaternion.Euler(eulerOffset);

        Transform cameraTransform = targetCamera.transform;
        cameraTransform.SetPositionAndRotation(
            cameraTransform.position + lastMouseParallaxPositionOffset,
            cameraTransform.rotation * lastMouseParallaxRotationOffset);

        hasAppliedMouseParallax = true;
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

    private float GetMouseParallaxInterpolation()
    {
        float smoothSpeed = Mathf.Max(0f, mouseParallaxSmoothSpeed);

        if (smoothSpeed <= 0f)
            return 1f;

        return 1f - Mathf.Exp(-smoothSpeed * Time.unscaledDeltaTime);
    }

    private float GetMouseParallaxIntensityMultiplier()
    {
        bool isCameraMotionActive =
            routine != null ||
            hasActiveCombatZoom ||
            hasActiveMonsterInfoFocus ||
            (treatGridSelectionAsZoomForParallax && hasActiveGridSelectionFocus) ||
            isImpactHitStopActive ||
            activeImpactOffset.sqrMagnitude > 0.000001f;

        return isCameraMotionActive ? Mathf.Clamp01(mouseParallaxCameraMotionMultiplier) : 1f;
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
            clampedOffset.x * positionAmount.x * multiplier,
            clampedOffset.y * positionAmount.y * multiplier,
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
            -clampedOffset.y * rotationAmount.x * multiplier,
            clampedOffset.x * rotationAmount.y * multiplier,
            0f);
    }

    private static Vector2 ClampMouseParallaxOffset(Vector2 normalizedMouseOffset)
    {
        return new Vector2(
            Mathf.Clamp(normalizedMouseOffset.x, -1f, 1f),
            Mathf.Clamp(normalizedMouseOffset.y, -1f, 1f));
    }

    private float GetZoomZPosition()
    {
        if (useFixedZoomZPosition)
            return zoomZPosition;

        return defaultPosition.z + zoomZOffset;
    }

    private float GetCharacterSelectionFocusZPosition()
    {
        if (useFixedCharacterSelectionFocusZ)
            return characterSelectionFocusZPosition;

        return defaultPosition.z + characterSelectionFocusZOffset;
    }

    private Vector2 GetCharacterSelectionGridFocusOffset(int gridIndex)
    {
        if (!useGridBasedCharacterSelectionFocusOffset || gridIndex < 0)
            return Vector2.zero;

        int columnCount = Mathf.Max(1, characterSelectionFocusGridColumnCount);
        int rowCount = Mathf.Max(1, characterSelectionFocusGridRowCount);
        int maxIndex = columnCount * rowCount - 1;

        if (gridIndex > maxIndex)
            return Vector2.zero;

        int column = gridIndex / rowCount;
        int row = gridIndex % rowCount;

        float horizontalT = columnCount <= 1 ? 0f : Mathf.Clamp01(column / (float)(columnCount - 1));
        float verticalT = rowCount <= 1 ? 0f : Mathf.Clamp01(row / (float)(rowCount - 1));

        Vector2 topOffset = Vector2.Lerp(
            characterSelectionFocusLeftTopOffset,
            characterSelectionFocusRightTopOffset,
            horizontalT);

        Vector2 bottomOffset = Vector2.Lerp(
            characterSelectionFocusLeftBottomOffset,
            characterSelectionFocusRightBottomOffset,
            horizontalT);

        return Vector2.Lerp(topOffset, bottomOffset, verticalT);
    }

    private float EvaluateZoomCurve(float t)
    {
        if (zoomCurve != null)
            return zoomCurve.Evaluate(t);

        return t * t * (3f - 2f * t);
    }

    private float GetImpactDeltaTime()
    {
        float deltaTime = useUnscaledTimeForImpact ? Time.unscaledDeltaTime : Time.deltaTime;

        if (useUnscaledTimeForImpact && isImpactRecoverySpeedActive)
            deltaTime *= Mathf.Max(1f, impactRecoverySpeedMultiplier);

        return BattleConsecutiveActionPresentationContext.ScaleDeltaTime(deltaTime);
    }

    private float GetCameraDeltaTime()
    {
        return BattleConsecutiveActionPresentationContext.ScaleDeltaTime(Time.deltaTime);
    }

    private void RestoreTimeScaleIfNeeded()
    {
        if (!isImpactHitStopActive)
            return;

        Time.timeScale = previousTimeScale;
        BattleVfxPlaybackPauseController.ResumeAll();
        isImpactHitStopActive = false;
        StartImpactRecoverySpeedBoost(previousTimeScale);
    }

    private void StartImpactRecoverySpeedBoost(float baseTimeScale)
    {
        if (!isActiveAndEnabled ||
            impactRecoverySpeedDuration <= 0f ||
            impactRecoverySpeedMultiplier <= 1f)
        {
            return;
        }

        CancelImpactRecoverySpeedBoost();
        impactRecoverySpeedRoutine = StartCoroutine(PlayImpactRecoverySpeedBoost(baseTimeScale));
    }

    private IEnumerator PlayImpactRecoverySpeedBoost(float baseTimeScale)
    {
        impactRecoveryBaseTimeScale = Mathf.Max(0f, baseTimeScale);
        isImpactRecoverySpeedActive = true;
        Time.timeScale = impactRecoveryBaseTimeScale * Mathf.Max(1f, impactRecoverySpeedMultiplier);

        float elapsed = 0f;

        while (elapsed < impactRecoverySpeedDuration && !isImpactHitStopActive)
        {
            elapsed += BattleConsecutiveActionPresentationContext.ScaleDeltaTime(
                Time.unscaledDeltaTime);
            yield return null;
        }

        impactRecoverySpeedRoutine = null;
        EndImpactRecoverySpeedBoost();
    }

    private void CancelImpactRecoverySpeedBoost()
    {
        if (impactRecoverySpeedRoutine != null)
        {
            StopCoroutine(impactRecoverySpeedRoutine);
            impactRecoverySpeedRoutine = null;
        }

        EndImpactRecoverySpeedBoost();
    }

    private void EndImpactRecoverySpeedBoost()
    {
        if (!isImpactRecoverySpeedActive)
            return;

        Time.timeScale = impactRecoveryBaseTimeScale;
        isImpactRecoverySpeedActive = false;
    }

    private void ClearImpactOffset()
    {
        if (targetCamera == null)
            return;

        if (activeImpactOffset.sqrMagnitude > 0.000001f)
        {
            Vector3 currentPosition = targetCamera.transform.position;

            if ((currentPosition - lastImpactAppliedPosition).sqrMagnitude < 0.0001f)
                targetCamera.transform.position = ClampCombatImpactPosition(currentPosition - activeImpactOffset);
        }

        activeImpactOffset = Vector3.zero;
        lastImpactAppliedPosition = targetCamera.transform.position;
    }

    // 그리드 외곽에서 이미 잡힌 전투 줌 구도가 위치 제한으로 밀리거나
    // 타격 후 반대 방향으로 튀지 않도록, 전투 줌 중에는 위치 보정을 하지 않습니다.
    // 일반 카메라 이동 및 정보 패널의 ClampCameraPosition 설정은 유지합니다.
    private Vector3 ClampCombatImpactPosition(Vector3 position)
    {
        return hasActiveCombatZoom ? position : ClampCameraPosition(position);
    }

    private Vector3 ClampCameraPosition(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, minCameraPosition.x, maxCameraPosition.x);
        position.y = Mathf.Clamp(position.y, minCameraPosition.y, maxCameraPosition.y);
        return position;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        monsterInfoFocusXMultiplier = Mathf.Max(0f, monsterInfoFocusXMultiplier);
        monsterInfoFocusDuration = Mathf.Max(0f, monsterInfoFocusDuration);
        zoomSize = Mathf.Max(0.1f, zoomSize);
        rangedCasterScreenEdgePadding = Mathf.Clamp(rangedCasterScreenEdgePadding, 0.05f, 0.4f);
        zoomDuration = Mathf.Max(0f, zoomDuration);
        returnDuration = Mathf.Max(0f, returnDuration);
        battleEffectReturnLeadTime = Mathf.Max(0f, battleEffectReturnLeadTime);
        zoomZOffset = Mathf.Max(0f, zoomZOffset);
        impactZoomAmount = Mathf.Max(0f, impactZoomAmount);
        impactZoomZOffset = Mathf.Max(0f, impactZoomZOffset);
        impactZoomInDuration = Mathf.Max(0f, impactZoomInDuration);
        impactZoomOutDuration = Mathf.Max(0f, impactZoomOutDuration);
        impactShakeDuration = Mathf.Max(0f, impactShakeDuration);
        impactShakeStrength = Mathf.Max(0f, impactShakeStrength);
        impactShakeFrequency = Mathf.Max(1f, impactShakeFrequency);
        impactHitStopDuration = Mathf.Max(0f, impactHitStopDuration);
        impactHitStopTimeScale = Mathf.Clamp01(impactHitStopTimeScale);
        rangedImpactZoomZOffset = Mathf.Max(0f, rangedImpactZoomZOffset);
        rangedImpactZoomInDuration = Mathf.Max(0f, rangedImpactZoomInDuration);
        rangedImpactZoomOutDuration = Mathf.Max(0f, rangedImpactZoomOutDuration);
        impactRecoverySpeedMultiplier = Mathf.Max(1f, impactRecoverySpeedMultiplier);
        impactRecoverySpeedDuration = Mathf.Max(0f, impactRecoverySpeedDuration);
        impactCompressionDistance = Mathf.Max(0f, impactCompressionDistance);
        impactRecoilDistance = Mathf.Max(0f, impactRecoilDistance);
        impactOvershootDistance = Mathf.Max(0f, impactOvershootDistance);
        impactCompressionDuration = Mathf.Max(0f, impactCompressionDuration);
        impactRecoilDuration = Mathf.Max(0f, impactRecoilDuration);
        impactSettleDuration = Mathf.Max(0f, impactSettleDuration);
        characterSelectionFocusGridColumnCount = Mathf.Max(1, characterSelectionFocusGridColumnCount);
        characterSelectionFocusGridRowCount = Mathf.Max(1, characterSelectionFocusGridRowCount);
        mouseParallaxPositionAmount.x = Mathf.Max(0f, mouseParallaxPositionAmount.x);
        mouseParallaxPositionAmount.y = Mathf.Max(0f, mouseParallaxPositionAmount.y);
        mouseParallaxRotationAmount.x = Mathf.Max(0f, mouseParallaxRotationAmount.x);
        mouseParallaxRotationAmount.y = Mathf.Max(0f, mouseParallaxRotationAmount.y);
        mouseParallaxYRotationMultiplier = Mathf.Max(0f, mouseParallaxYRotationMultiplier);
        mouseParallaxSmoothSpeed = Mathf.Max(0f, mouseParallaxSmoothSpeed);
        mouseParallaxCameraMotionMultiplier = Mathf.Clamp01(mouseParallaxCameraMotionMultiplier);
    }
#endif
}
