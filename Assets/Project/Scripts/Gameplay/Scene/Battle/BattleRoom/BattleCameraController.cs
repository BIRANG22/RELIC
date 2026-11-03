using Relic.Gameplay.Monster;
using System.Collections;
using UnityEngine;

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

    [SerializeField] private Vector2 zoomOffset = new Vector2(0f, 0.35f);
    [SerializeField] private bool usePositionZZoom = true;
    [SerializeField] private bool useFixedZoomZPosition = true;
    [SerializeField] private float zoomZPosition = -13f;
    [SerializeField] private float zoomZOffset = 5f;
    [SerializeField] private bool useOrthographicSizeZoom = false;
    [SerializeField] private bool clampZoomPosition = false;
    [SerializeField] private AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

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

    [Header("Monster Info Focus")]
    [SerializeField] private bool enableMonsterInfoFocus = true;
    [SerializeField] private float monsterInfoFocusDuration = 0.22f;
    [SerializeField] private float monsterInfoReturnDuration = 0.22f;
    [SerializeField] private Vector2 monsterInfoFocusOffset = new Vector2(0f, 0.25f);
    [SerializeField, Min(0f)] private float monsterInfoFocusSideOffset = 4.0f;
    [SerializeField] private bool useMonsterInfoFocusZ = true;
    [SerializeField] private bool useFixedMonsterInfoFocusZ = false;
    [SerializeField] private float monsterInfoFocusZPosition = -18f;
    [SerializeField] private float monsterInfoFocusZOffset = 2f;
    [SerializeField] private bool useMonsterInfoFocusOrthographicSize = false;
    [SerializeField] private float monsterInfoFocusOrthographicSize = 4.4f;
    [SerializeField] private bool clampMonsterInfoFocusPosition = false;
    [Tooltip("몬스터 정보 선택 시 캐릭터 선택 포커스보다 카메라를 더 가까이 이동할 Z 거리입니다. 0이면 동일한 줌입니다.")]
    [SerializeField, Min(0f)] private float monsterInfoExtraZoomZ = 0.8f;
    [Tooltip("직교 카메라에서 몬스터 정보 선택 시 추가로 줄일 Orthographic Size입니다.")]
    [SerializeField, Min(0f)] private float monsterInfoExtraZoomOrthographicSize = 0.35f;
    [Tooltip("몬스터 정보 포커스 추가 줌 강도 배율입니다. 기존 추가 줌 설정에 곱해지며, 1이면 이전과 동일합니다.")]
    [SerializeField, Min(0f)] private float monsterInfoExtraZoomStrength = 3.5f;
    [Tooltip("몬스터 정보창 줌인 시 실제 몬스터 위치를 얼마나 따라갈지 설정합니다. 1이면 몬스터 위치 기준, 0이면 이전 그리드 기준입니다.")]
    [SerializeField, Range(0f, 1f)] private float monsterInfoWorldPositionWeight = 1f;
    [Tooltip("정보 패널이 몬스터를 가리지 않도록 카메라 중심을 좌우로 옮기는 거리입니다. 몬스터는 패널 반대편에 표시됩니다.")]
    [SerializeField, Min(0f)] private float monsterInfoScreenSideMargin = 0.9f;

    [Header("Camera Position Clamp")]
    [SerializeField] private Vector2 minCameraPosition = new Vector2(-0.5f, -1f);
    [SerializeField] private Vector2 maxCameraPosition = new Vector2(0.5f, 1f);

    [Header("Panel Down Position")]
    [Tooltip("BattleCharacterPanel과 BattleSlot이 내려가 있을 때 사용할 Main Camera Y 위치입니다.")]
    [SerializeField] private float panelDownPositionY = 1.5f;

    [Header("Mouse Parallax")]
    [SerializeField] private bool enableMouseParallax = true;
    [SerializeField] private Vector2 mouseParallaxPositionAmount = new Vector2(0.08f, 0.05f);
    [SerializeField] private Vector2 mouseParallaxRotationAmount = new Vector2(1f, 1f);
    [SerializeField, Min(0f)] private float mouseParallaxSmoothSpeed = 8f;
    [SerializeField, Range(0f, 1f)] private float mouseParallaxCameraMotionMultiplier = 0.35f;

    private float defaultSize;
    private Vector3 defaultPosition;
    private Coroutine routine;
    private Coroutine panelDownReturnRoutine;
    private bool holdDefaultReturn;
    private bool hasActiveCombatZoom;
    private bool hasActiveMonsterInfoFocus;

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
    private const float ZoomFollowSmoothTime = 0.045f;

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
        RestoreCinematicCameraImmediate();
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

    public void FocusMonsterInfoForGridIndex(Transform target, int gridIndex)
    {
        // 몬스터 정보 선택도 캐릭터 선택과 동일한 그리드 기준 카메라 포커스를 사용합니다.
        // 정보 패널의 표시를 기다리지 않고 선택 순간부터 한 번만 이동합니다.
        if (!enableMonsterInfoFocus || targetCamera == null || target == null || hasActiveCombatZoom)
            return;

        RemoveMouseParallax();
        EndZoomFollowTarget();
        if (routine != null)
            StopCoroutine(routine);
        ClearImpactOffset();

        Vector2 gridOffset = GetCharacterSelectionGridFocusOffset(gridIndex);
        Vector3 gridTargetPos = defaultPosition;
        if (useGridBasedCharacterSelectionFocusOffset && gridIndex >= 0)
        {
            gridTargetPos.x += gridOffset.x;
            gridTargetPos.y += gridOffset.y;
        }
        else
        {
            gridTargetPos.x += characterSelectionFocusOffset.x;
            gridTargetPos.y += characterSelectionFocusOffset.y;
        }

        // 단순히 현재 화면을 확대하지 않고 실제 클릭한 몬스터 쪽으로 줌인합니다.
        // 패널이 왼쪽에 열리면 몬스터를 오른쪽에, 반대라면 왼쪽에 배치합니다.
        int rowCount = Mathf.Max(1, characterSelectionFocusGridRowCount);
        int columnCount = Mathf.Max(1, characterSelectionFocusGridColumnCount);
        bool panelOnLeft = gridIndex < 0 || gridIndex >= rowCount * (columnCount / 2);
        float sideMargin = panelOnLeft ? -monsterInfoScreenSideMargin : monsterInfoScreenSideMargin;
        Vector3 monsterTargetPos = defaultPosition;
        monsterTargetPos.x = target.position.x + monsterInfoFocusOffset.x + sideMargin;
        monsterTargetPos.y = target.position.y + monsterInfoFocusOffset.y;

        Vector3 targetPos = Vector3.Lerp(
            gridTargetPos, monsterTargetPos, Mathf.Clamp01(monsterInfoWorldPositionWeight));

        // 기존 추가 줌 강도를 유지하면서 위치와 확대를 하나의 이동으로 진행합니다.
        targetPos.z = useCharacterSelectionFocusZ
            ? GetCharacterSelectionFocusZPosition() + monsterInfoExtraZoomZ * monsterInfoExtraZoomStrength
            : targetCamera.transform.position.z + monsterInfoExtraZoomZ * monsterInfoExtraZoomStrength;
        float targetSize = useCharacterSelectionFocusOrthographicSize
            ? characterSelectionFocusOrthographicSize
            : targetCamera.orthographicSize;
        bool zoomOrthographic = useCharacterSelectionFocusOrthographicSize || targetCamera.orthographic;
        if (zoomOrthographic)
            targetSize = Mathf.Max(0.1f, targetSize - monsterInfoExtraZoomOrthographicSize * monsterInfoExtraZoomStrength);

        hasActiveMonsterInfoFocus = true;
        MoveCameraWithOptionalImmediate(
            targetPos,
            targetSize,
            Mathf.Max(characterSelectionFocusDuration, minimumCharacterSelectionFocusDuration),
            clampCharacterSelectionFocusPosition,
            zoomOrthographic);
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

    public IEnumerator ZoomTo(Transform target)
    {
        if (targetCamera == null || target == null)
            yield break;

        yield return ZoomToPosition(target.position);
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

    /// <summary>
    /// 시전자와 실제 효과 대상 전체를 한 구도로 잡습니다.
    /// 시전자 -> 피격자 이동 단계를 없애고, 시작 위치에서 최종 구도로 한 번만 이동합니다.
    /// 버프/디버프는 원거리 줌을 기준으로 하며 시전자 자신이 대상에 포함되어도 추가 이동하지 않습니다.
    /// </summary>
    public IEnumerator ZoomForActionTargets(
        Transform caster, System.Collections.Generic.IReadOnlyList<Transform> targets,
        bool isStatusSkill, bool isRangedSkill)
    {
        if (targetCamera == null || caster == null)
            yield break;

        bool useRangedFrame = isStatusSkill || isRangedSkill;
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

        Vector3 destination = targetCamera.transform.position;
        destination.x = (minX + maxX) * 0.5f + zoomOffset.x;
        destination.y = (minY + maxY) * 0.5f + zoomOffset.y;

        float desiredZ = useRangedFrame ? rangedExecutionZoomZPosition :
            usePositionZZoom ? GetZoomZPosition() : destination.z;
        float desiredSize = !useRangedFrame && useOrthographicSizeZoom
            ? zoomSize : targetCamera.orthographicSize;

        // 카메라 비율에 맞춰 시전자와 대상 전부를 보여줄 최소 화면 크기를 계산합니다.
        // 원거리/상태 효과는 기존 원거리 Z를 최대 확대 한도로 사용합니다.
        float aspect = Mathf.Max(0.1f, targetCamera.aspect);
        float paddingFactor = 1f + Mathf.Max(0f, actionFramePadding);
        float halfWidth = Mathf.Max(0f, (maxX - minX) * 0.5f) * paddingFactor;
        float halfHeight = Mathf.Max(actionFrameMinHalfHeight,
            Mathf.Max(0f, (maxY - minY) * 0.5f) * paddingFactor);
        float requiredHalfHeight = Mathf.Max(halfHeight, halfWidth / aspect);
        if (targetCamera.orthographic)
        {
            desiredSize = Mathf.Max(desiredSize, requiredHalfHeight);
        }
        else if (usePositionZZoom)
        {
            float verticalHalfFov = targetCamera.fieldOfView * Mathf.Deg2Rad * 0.5f;
            float tanHalfFov = Mathf.Max(0.01f, Mathf.Tan(verticalHalfFov));
            float requiredDistance = requiredHalfHeight / tanHalfFov;
            // 더 가까이 가지 않으며, 범위가 넓으면 필요한 만큼 뒤에서 촬영합니다.
            destination.z = Mathf.Min(desiredZ, caster.position.z - requiredDistance);
        }
        if (targetCamera.orthographic || !usePositionZZoom)
            destination.z = usePositionZZoom ? desiredZ : targetCamera.transform.position.z;

        float duration = useRangedFrame ? rangedExecutionZoomDuration : zoomDuration;
        routine = StartCoroutine(MoveCamera(
            destination, desiredSize, duration, false,
            targetCamera.orthographic || (!useRangedFrame && useOrthographicSizeZoom)));
        yield return routine;
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

    public void FocusMonsterInfo(Transform target)
    {
        FocusMonsterInfo(target, 0f);
    }

    public void FocusMonsterInfoWithPanelSide(Transform target, bool panelOnLeft)
    {
        float sideOffset = Mathf.Abs(monsterInfoFocusSideOffset);
        float horizontalOffset = panelOnLeft
            ? -sideOffset
            : sideOffset;

        FocusMonsterInfo(target, horizontalOffset);
    }

    private void FocusMonsterInfo(Transform target, float horizontalOffset)
    {
        if (!enableMonsterInfoFocus)
            return;

        if (targetCamera == null || target == null)
            return;

        if (hasActiveCombatZoom)
            return;

        RemoveMouseParallax();
        EndZoomFollowTarget();

        if (routine != null)
            StopCoroutine(routine);

        ClearImpactOffset();

        Vector3 targetPos = target.position;
        targetPos.x += monsterInfoFocusOffset.x + horizontalOffset;
        targetPos.y += monsterInfoFocusOffset.y;
        targetPos.z = useMonsterInfoFocusZ
            ? GetMonsterInfoFocusZPosition()
            : targetCamera.transform.position.z;

        float targetSize = useMonsterInfoFocusOrthographicSize
            ? monsterInfoFocusOrthographicSize
            : targetCamera.orthographicSize;

        hasActiveMonsterInfoFocus = true;
        MoveCameraWithOptionalImmediate(
            targetPos,
            targetSize,
            monsterInfoFocusDuration,
            clampMonsterInfoFocusPosition,
            useMonsterInfoFocusOrthographicSize);
    }

    public void ReturnDefaultFromMonsterInfoFocus()
    {
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
        MoveCameraWithOptionalImmediate(
            GetNeutralBattlePosition(),
            defaultSize,
            monsterInfoReturnDuration,
            false,
            true);
    }

    private Vector3 GetNeutralBattlePosition()
    {
        Vector3 neutralPosition = defaultPosition;
        neutralPosition.y = panelDownPositionY;
        return neutralPosition;
    }

    public void ForceReturnMapImmediate()
    {
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

    private void StartReturnPanelDownInternal(float moveDuration)
    {
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
        if (targetCamera == null)
            yield break;

        RemoveMouseParallax();
        EndZoomFollowTarget();

        if (routine != null)
            StopCoroutine(routine);

        ClearImpactOffset();

        ApplyCameraRotationZ(0f);
        ResetDamageImpactRotationSequence();

        routine = StartCoroutine(MoveCamera(GetNeutralBattlePosition(), defaultSize, moveDuration, false, true));
        yield return routine;

        ApplyCameraRotationZ(0f);
        hasActiveCombatZoom = false;
        ResetCombatFocusTracking();
        hasActiveMonsterInfoFocus = false;
    }

    public void ForceReturnPanelDownImmediate()
    {
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
    }

    public IEnumerator ReturnDefault()
    {
        if (targetCamera == null)
            yield break;

        // 카메라/Plane의 기존 복귀 속도를 유지하고 마지막 행동 유닛의 Idle 전환만 맞춥니다.
        if (hasActiveCombatZoom)
            BattleUnitAnimator.SyncLastActionIdleWithCameraReturn(returnDuration);

        RemoveMouseParallax();
        EndZoomFollowTarget();

        if (routine != null)
            StopCoroutine(routine);

        ClearImpactOffset();

        ApplyCameraRotationZ(0f);
        ResetDamageImpactRotationSequence();

        // BattleEffect의 복귀 시작과 줌아웃의 길이/곡선을 일치시킵니다.
        yield return BeginBattleEffectReturnWithLead(returnDuration);
        routine = StartCoroutine(MoveCamera(GetNeutralBattlePosition(), defaultSize, returnDuration, false, true, true));
        yield return routine;

        ApplyCameraRotationZ(0f);
        hasActiveCombatZoom = false;
        ResetCombatFocusTracking();
        hasActiveMonsterInfoFocus = false;
    }

    private void ResetCombatFocusTracking()
    {
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
        targetCamera.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
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
        bool synchronizePlaneReturn = false)
    {
        Vector3 startPos = targetCamera.transform.position;
        float startSize = targetCamera.orthographicSize;

        if (!usePositionZZoom)
            targetPos.z = startPos.z;

        if (duration <= 0f)
        {
            targetCamera.transform.position = clampPosition ? ClampCameraPosition(targetPos) : targetPos;

            if (applyOrthographicSize)
                targetCamera.orthographicSize = targetSize;
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

            if (synchronizePlaneReturn)
                UpdateBattleEffectReturnAlongCamera(t);
            yield return null;
        }

        targetCamera.transform.position = clampPosition ? ClampCameraPosition(targetPos) : targetPos;

        if (applyOrthographicSize)
            targetCamera.orthographicSize = targetSize;
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

        targetCamera.transform.position = Vector3.SmoothDamp(
            targetCamera.transform.position,
            targetPosition,
            ref zoomFollowVelocity,
            ZoomFollowSmoothTime,
            Mathf.Infinity,
            Time.deltaTime);
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

    private float GetMonsterInfoFocusZPosition()
    {
        if (useFixedMonsterInfoFocusZ)
            return monsterInfoFocusZPosition;

        return defaultPosition.z + monsterInfoFocusZOffset;
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
        zoomSize = Mathf.Max(0.1f, zoomSize);
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
        monsterInfoFocusDuration = Mathf.Max(0f, monsterInfoFocusDuration);
        monsterInfoReturnDuration = Mathf.Max(0f, monsterInfoReturnDuration);
        monsterInfoFocusSideOffset = Mathf.Max(0f, monsterInfoFocusSideOffset);
        monsterInfoFocusZOffset = Mathf.Max(0f, monsterInfoFocusZOffset);
        monsterInfoFocusOrthographicSize = Mathf.Max(0.1f, monsterInfoFocusOrthographicSize);
        characterSelectionFocusGridColumnCount = Mathf.Max(1, characterSelectionFocusGridColumnCount);
        characterSelectionFocusGridRowCount = Mathf.Max(1, characterSelectionFocusGridRowCount);
        mouseParallaxPositionAmount.x = Mathf.Max(0f, mouseParallaxPositionAmount.x);
        mouseParallaxPositionAmount.y = Mathf.Max(0f, mouseParallaxPositionAmount.y);
        mouseParallaxRotationAmount.x = Mathf.Max(0f, mouseParallaxRotationAmount.x);
        mouseParallaxRotationAmount.y = Mathf.Max(0f, mouseParallaxRotationAmount.y);
        mouseParallaxSmoothSpeed = Mathf.Max(0f, mouseParallaxSmoothSpeed);
        mouseParallaxCameraMotionMultiplier = Mathf.Clamp01(mouseParallaxCameraMotionMultiplier);
    }
#endif
}
