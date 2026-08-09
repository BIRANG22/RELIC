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

    [Header("Camera Position Clamp")]
    [SerializeField] private Vector2 minCameraPosition = new Vector2(-0.5f, -1f);
    [SerializeField] private Vector2 maxCameraPosition = new Vector2(0.5f, 1f);

    private float defaultSize;
    private Vector3 defaultPosition;
    private Coroutine routine;
    private bool holdDefaultReturn;
    private bool hasActiveCombatZoom;
    private bool hasActiveMonsterInfoFocus;

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
            defaultPosition = targetCamera.transform.position;
        }
    }

    private void OnDisable()
    {
        RestoreTimeScaleIfNeeded();
        CancelImpactRecoverySpeedBoost();
        ClearImpactOffset();
        ForceReturnDefaultImmediate();
    }

    private void Update()
    {
        HandleZoomFollowTarget();
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

        // 전투 줌은 한 연속 행동 묶음에서 처음 잡은 타격자 기준으로 한 번만 이동한다.
        // 피격자가 바뀌어도 피격자 위치로 다시 줌 이동하지 않는다.
        if (hasActiveCombatZoom)
            yield break;

        hasActiveCombatZoom = true;
        ResetDamageImpactRotationSequence();

        yield return ZoomToPosition(attacker.position);
    }

    public IEnumerator ZoomToHitTarget(Transform hitTarget)
    {
        // 기존 호출부 호환용이다. 실제 기준은 피격자가 아니라 최초 타격자다.
        yield return ZoomToAttacker(hitTarget);
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
            defaultPosition,
            defaultSize,
            monsterInfoReturnDuration,
            false,
            true);
    }

    public void StartReturnDefault()
    {
        // BattleCharacterPanel이 올라와 있는 동안에는 카메라를 기본 위치로
        // 복귀시키지 않습니다. 실제 선택이 모두 해제되어 패널이 내려갈 때만
        // 기본 위치 복귀를 허용합니다.
        if (HasActiveInfoSelection())
            return;

        if (targetCamera == null)
            return;

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

    public void ForceReturnDefaultImmediate()
    {
        if (targetCamera == null)
            return;

        EndZoomFollowTarget();

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        ClearImpactOffset();

        targetCamera.transform.position = defaultPosition;
        targetCamera.transform.rotation = Quaternion.identity;
        targetCamera.orthographicSize = defaultSize;
        hasActiveCombatZoom = false;
        ResetDamageImpactRotationSequence();
        hasActiveMonsterInfoFocus = false;
    }

    public IEnumerator ReturnDefault()
    {
        if (targetCamera == null)
            yield break;

        EndZoomFollowTarget();

        if (routine != null)
            StopCoroutine(routine);

        ClearImpactOffset();

        ApplyCameraRotationZ(0f);
        ResetDamageImpactRotationSequence();

        routine = StartCoroutine(MoveCamera(defaultPosition, defaultSize, returnDuration, false, true));
        yield return routine;

        ApplyCameraRotationZ(0f);
        hasActiveCombatZoom = false;
        hasActiveMonsterInfoFocus = false;
    }

    public IEnumerator PlayDamageImpact()
    {
        if (targetCamera == null || !enableDamageImpact)
            yield break;

        ApplyNextDamageImpactRotation();

        if (routine != null)
            yield return routine;

        ClearImpactOffset();

        float baseSize = targetCamera.orthographicSize;
        float targetSize = Mathf.Max(0.1f, baseSize - Mathf.Max(0f, impactZoomAmount));
        float baseZ = targetCamera.transform.position.z;
        float targetZ = usePositionZZoom ? baseZ + Mathf.Max(0f, impactZoomZOffset) : baseZ;

        yield return LerpImpactZoom(baseSize, targetSize, baseZ, targetZ, impactZoomInDuration);
        yield return ShakeAndHitStop();
        yield return LerpImpactZoom(targetCamera.orthographicSize, baseSize, targetCamera.transform.position.z, baseZ, impactZoomOutDuration);

        if (useOrthographicSizeZoom)
            targetCamera.orthographicSize = baseSize;

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
        bool applyOrthographicSize = false)
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

            yield return null;
        }

        targetCamera.transform.position = clampPosition ? ClampCameraPosition(targetPos) : targetPos;

        if (applyOrthographicSize)
            targetCamera.orthographicSize = targetSize;
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
                hitStopElapsed += Time.unscaledDeltaTime;

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
                targetCamera.transform.position = ClampCameraPosition(basePosition + activeImpactOffset);
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

        return deltaTime;
    }

    private float GetCameraDeltaTime()
    {
        return Time.deltaTime;
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
            elapsed += Time.unscaledDeltaTime;
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
                targetCamera.transform.position = ClampCameraPosition(currentPosition - activeImpactOffset);
        }

        activeImpactOffset = Vector3.zero;
        lastImpactAppliedPosition = targetCamera.transform.position;
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
        impactRecoverySpeedMultiplier = Mathf.Max(1f, impactRecoverySpeedMultiplier);
        impactRecoverySpeedDuration = Mathf.Max(0f, impactRecoverySpeedDuration);
        monsterInfoFocusDuration = Mathf.Max(0f, monsterInfoFocusDuration);
        monsterInfoReturnDuration = Mathf.Max(0f, monsterInfoReturnDuration);
        monsterInfoFocusSideOffset = Mathf.Max(0f, monsterInfoFocusSideOffset);
        monsterInfoFocusZOffset = Mathf.Max(0f, monsterInfoFocusZOffset);
        monsterInfoFocusOrthographicSize = Mathf.Max(0.1f, monsterInfoFocusOrthographicSize);
        characterSelectionFocusGridColumnCount = Mathf.Max(1, characterSelectionFocusGridColumnCount);
        characterSelectionFocusGridRowCount = Mathf.Max(1, characterSelectionFocusGridRowCount);
    }
#endif
}
