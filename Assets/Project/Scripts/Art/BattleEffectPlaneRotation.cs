using System.Collections;
using UnityEngine;

public enum PlaneAttackType
{
    Melee,
    Ranged
}

/// <summary>
/// 전투 중 공격/충돌 피해 연출에 맞춰 Plane과 Plane (1)의 위치와 X 회전을 변경합니다.
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
    [Tooltip("게임 시작 시 Plane의 현재 로컬 위치를 기본 위치로 저장합니다.")]
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
    private int lastCollisionFeedbackFrame = -1;

    private Vector3 activePlane1Position;
    private Vector3 activePlane2Position;
    private Quaternion activePlane1Rotation;
    private Quaternion activePlane2Rotation;
    private float activeNoiseFlowX;

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

    private void BeginPersistentAttackEffect(PlaneAttackType attackType)
    {
        FindPlanesAutomatically();

        if (plane1 == null && plane2 == null)
            return;

        if (persistentAttackActive && !releasePersistentAttackRequested)
            return;

        // 직전 공격의 복귀가 시작된 직후 다음 공격이 실행되면
        // 복귀 연출을 중단하고 새 공격 연출을 즉시 시작합니다.
        StopCurrentRoutineWithoutReset();
        persistentAttackActive = false;
        releasePersistentAttackRequested = false;
        ApplyIdleValues();

        persistentAttackActive = true;
        releasePersistentAttackRequested = false;

        float rotationAmount = attackType == PlaneAttackType.Ranged
            ? rangedRotationAmount
            : meleeRotationAmount;

        PrepareActiveValues(rotationAmount, attackType.ToString());
        hitRoutine = StartCoroutine(PlayPersistentAttackRoutine());
    }

    private void EndPersistentAttackEffect()
    {
        if (!persistentAttackActive)
            return;

        releasePersistentAttackRequested = true;
    }

    private void PlaySingleAttackEffect(PlaneAttackType attackType)
    {
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

    private void PrepareActiveValues(float rotationAmount, string presentationName)
    {
        float direction = Random.value < 0.5f ? -1f : 1f;
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
        activeNoiseFlowX = CreateHitNoiseFlowX();

        if (showDebugLog)
        {
            Debug.Log(
                $"[BattleEffectPlaneRotation] {presentationName} / " +
                $"변화량:{signedAmount} / " +
                $"Plane X:{plane1Rotation.x} / Plane (1) X:{plane2Rotation.x}",
                this);
        }
    }

    private IEnumerator PlayPersistentAttackRoutine()
    {
        SetNoiseFlowX(activeNoiseFlowX);

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

        while (!releasePersistentAttackRequested)
        {
            ApplyActiveValues();
            yield return null;
        }

        yield return ReturnToIdleRoutine();

        persistentAttackActive = false;
        releasePersistentAttackRequested = false;
        hitRoutine = null;
    }

    private IEnumerator PlayOneShotRoutine()
    {
        SetNoiseFlowX(activeNoiseFlowX);

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
            activeNoiseFlowX,
            holdDuration);

        yield return ReturnToIdleRoutine();
        hitRoutine = null;
    }

    private IEnumerator ReturnToIdleRoutine()
    {
        Vector3 plane1FromPosition = plane1 != null
            ? plane1.localPosition
            : activePlane1Position;
        Quaternion plane1FromRotation = plane1 != null
            ? plane1.localRotation
            : activePlane1Rotation;

        Vector3 plane2FromPosition = plane2 != null
            ? plane2.localPosition
            : activePlane2Position;
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
            returnDuration,
            returnCurve);

        ApplyIdleValues();

        if (restoreNoiseFlowAfterHit)
            RestoreNoiseFlow();
    }

    private void ApplyActiveValues()
    {
        ApplyValues(
            activePlane1Position,
            activePlane1Rotation,
            activePlane2Position,
            activePlane2Rotation);

        SetNoiseFlowX(activeNoiseFlowX);
    }

    public void StopHitEffect()
    {
        persistentAttackActive = false;
        releasePersistentAttackRequested = false;
        StopCurrentRoutineWithoutReset();
        ApplyIdleValues();

        if (restoreNoiseFlowAfterHit)
            RestoreNoiseFlow();
    }

    private void StopCurrentRoutineWithoutReset()
    {
        if (hitRoutine == null)
            return;

        StopCoroutine(hitRoutine);
        hitRoutine = null;
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
        float noiseFlowX,
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
            SetNoiseFlowX(noiseFlowX);

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
        if (plane1 != null)
        {
            plane1.localPosition = plane1Position;
            plane1.localRotation = plane1Rotation;
        }

        if (plane2 != null)
        {
            plane2.localPosition = plane2Position;
            plane2.localRotation = plane2Rotation;
        }
    }

    private void ApplyIdleValues()
    {
        if (plane1 != null)
        {
            plane1.localPosition = plane1IdlePosition;
            plane1.localRotation = plane1IdleQuaternion;
        }

        if (plane2 != null)
        {
            plane2.localPosition = plane2IdlePosition;
            plane2.localRotation = plane2IdleQuaternion;
        }
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
    }
#endif
}
