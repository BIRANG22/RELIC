using System.Collections;
using UnityEngine;

/// <summary>
/// 전투에서 피해가 실제로 적중했을 때
/// 두 Plane의 위치, 회전, 머티리얼 Noise Flow X값을 변경합니다.
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


    [Header("유효타 위치")]
    [Tooltip("유효타 발생 시 첫 번째 Plane이 이동할 최종 로컬 위치입니다.")]
    [SerializeField] private Vector3 plane1HitPosition;

    [Tooltip("유효타 발생 시 두 번째 Plane이 이동할 최종 로컬 위치입니다.")]
    [SerializeField] private Vector3 plane2HitPosition;


    [Header("유효타 기본 회전")]
    [Tooltip("랜덤 회전값이 더해지기 전 첫 번째 Plane의 유효타 회전입니다.")]
    [SerializeField]
    private Vector3 plane1HitRotation =
        new Vector3(0f, 0f, 15f);

    [Tooltip("랜덤 회전값이 더해지기 전 두 번째 Plane의 유효타 회전입니다.")]
    [SerializeField]
    private Vector3 plane2HitRotation =
        new Vector3(0f, 0f, -15f);


    [Header("랜덤 회전 증감폭")]
    [Tooltip("유효타마다 두 Plane에 공통으로 더할 최소 회전값입니다.")]
    [SerializeField]
    private Vector3 randomRotationMin =
        new Vector3(0f, 0f, -10f);

    [Tooltip("유효타마다 두 Plane에 공통으로 더할 최대 회전값입니다.")]
    [SerializeField]
    private Vector3 randomRotationMax =
        new Vector3(0f, 0f, 10f);

    [Tooltip("체크하면 두 번째 Plane에는 랜덤 회전값을 반대로 적용합니다.")]
    [SerializeField] private bool invertRandomRotationForPlane2;


    [Header("Noise Flow 머티리얼")]
    [Tooltip("Noise Flow 값을 변경할 머티리얼입니다.")]
    [SerializeField] private Material noiseFlowMaterial;

    [Tooltip("Shader Graph에 설정된 Noise Flow의 Reference 이름입니다.")]
    [SerializeField] private string noiseFlowPropertyName = "_noiseflow";

    [Tooltip("게임 시작 시 머티리얼의 현재 Noise Flow X값을 기본값으로 저장합니다.")]
    [SerializeField] private bool useCurrentNoiseFlowXAsIdle = true;

    [Tooltip("평소 Noise Flow X값입니다.")]
    [SerializeField] private float idleNoiseFlowX = 0.01f;

    [Tooltip("유효타 발생 시 사용할 기본 Noise Flow X값입니다.")]
    [SerializeField] private float hitNoiseFlowX = 1f;

    [Tooltip("유효타마다 Noise Flow X에 더할 최소 랜덤값입니다.")]
    [SerializeField] private float randomNoiseFlowXMin = -0.2f;

    [Tooltip("유효타마다 Noise Flow X에 더할 최대 랜덤값입니다.")]
    [SerializeField] private float randomNoiseFlowXMax = 0.2f;

    [Tooltip("연출 종료 후 Noise Flow 값을 기본값으로 되돌립니다.")]
    [SerializeField] private bool restoreNoiseFlowAfterHit = true;


    [Header("연출 시간")]
    [Tooltip("유효타 위치와 회전으로 움직이는 시간입니다.")]
    [SerializeField] private float moveOutDuration = 0.08f;

    [Tooltip("유효타 위치, 회전, Noise Flow X값을 유지하는 시간입니다.")]
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
    [Tooltip("유효타 연출과 생성된 랜덤값을 Console에 표시합니다.")]
    [SerializeField] private bool showDebugLog;


    // 두 Plane의 기본 회전값입니다.
    private Quaternion plane1IdleQuaternion;
    private Quaternion plane2IdleQuaternion;

    // 현재 실행 중인 유효타 연출 코루틴입니다.
    private Coroutine hitRoutine;

    // Noise Flow 프로퍼티 ID입니다.
    private int noiseFlowPropertyId;

    // 머티리얼에 Noise Flow 프로퍼티가 존재하는지 확인합니다.
    private bool hasNoiseFlowProperty;

    // 게임 시작 시 Noise Flow 전체 Vector 값을 저장합니다.
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
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 현재 컴포넌트를 전역 인스턴스로 등록합니다.
    /// </summary>
    private void RegisterInstance()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[BattleEffectPlaneRotation] 컴포넌트가 두 개 이상 존재합니다.",
                this
            );
        }

        Instance = this;
    }

    /// <summary>
    /// BattleHitImpactFeedback에서 호출하는 정적 함수입니다.
    /// </summary>
    public static void PlayHitRotationFeedback()
    {
        BattleEffectPlaneRotation controller = Instance;

        // Instance가 없다면 비활성 오브젝트까지 포함하여 씬에서 찾습니다.
        if (controller == null)
        {
            controller = FindFirstObjectByType<BattleEffectPlaneRotation>(
                FindObjectsInactive.Include
            );

            if (controller != null)
            {
                Instance = controller;
            }
        }

        if (controller == null)
        {
            Debug.LogWarning(
                "[BattleEffectPlaneRotation] 씬에서 컴포넌트를 찾지 못했습니다. " +
                "BattleEffect 오브젝트에 스크립트가 붙어 있는지 확인하세요."
            );

            return;
        }

        if (!controller.gameObject.activeInHierarchy)
        {
            Debug.LogWarning(
                "[BattleEffectPlaneRotation] BattleEffect 오브젝트가 비활성화되어 있습니다.",
                controller
            );

            return;
        }

        if (!controller.enabled)
        {
            Debug.LogWarning(
                "[BattleEffectPlaneRotation] 컴포넌트가 비활성화되어 있습니다.",
                controller
            );

            return;
        }

        controller.PlayHitEffect();
    }

    /// <summary>
    /// 유효타 위치, 회전, Noise Flow 연출을 실행합니다.
    /// </summary>
    public void PlayHitEffect()
    {
        FindPlanesAutomatically();

        if (plane1 == null && plane2 == null)
        {
            Debug.LogWarning(
                "[BattleEffectPlaneRotation] 대상 Plane이 연결되지 않았습니다.",
                this
            );

            return;
        }

        // 이전 유효타 연출이 실행 중이면 중지합니다.
        if (hitRoutine != null)
        {
            StopCoroutine(hitRoutine);
            hitRoutine = null;
        }

        // 연속 공격에서도 각 타격이 확실하게 보이도록
        // 새로운 연출 전에 기본 위치와 회전으로 복구합니다.
        ApplyIdleValues();

        hitRoutine = StartCoroutine(PlayHitEffectRoutine());
    }

    /// <summary>
    /// 실행 중인 연출을 중지하고 기본 상태로 복구합니다.
    /// </summary>
    public void StopHitEffect()
    {
        if (hitRoutine != null)
        {
            StopCoroutine(hitRoutine);
            hitRoutine = null;
        }

        ApplyIdleValues();

        if (restoreNoiseFlowAfterHit)
        {
            RestoreNoiseFlow();
        }
    }

    /// <summary>
    /// 유효타 연출을 순서대로 실행합니다.
    /// </summary>
    private IEnumerator PlayHitEffectRoutine()
    {
        // 이번 타격에 사용할 공통 랜덤 회전 증감값입니다.
        Vector3 sharedRandomRotation = CreateRandomRotation();

        // 첫 번째 Plane의 최종 유효타 회전값입니다.
        Vector3 currentPlane1HitRotation =
            plane1HitRotation + sharedRandomRotation;

        // 설정에 따라 두 번째 Plane에는
        // 같은 랜덤값 또는 반대 랜덤값을 적용합니다.
        Vector3 plane2RandomRotation =
            invertRandomRotationForPlane2
                ? -sharedRandomRotation
                : sharedRandomRotation;

        // 두 번째 Plane의 최종 유효타 회전값입니다.
        Vector3 currentPlane2HitRotation =
            plane2HitRotation + plane2RandomRotation;

        Quaternion plane1HitQuaternion =
            Quaternion.Euler(currentPlane1HitRotation);

        Quaternion plane2HitQuaternion =
            Quaternion.Euler(currentPlane2HitRotation);

        // 이번 타격에서 사용할 Noise Flow X값을 생성합니다.
        float currentHitNoiseFlowX = CreateHitNoiseFlowX();

        // 유효타가 발생한 순간 Noise Flow X값을 변경합니다.
        SetNoiseFlowX(currentHitNoiseFlowX);

        if (showDebugLog)
        {
            Debug.Log(
                "[BattleEffectPlaneRotation] 유효타 연출 실행\n" +
                $"공통 랜덤 회전값: {sharedRandomRotation}\n" +
                $"Plane 1 최종 회전값: {currentPlane1HitRotation}\n" +
                $"Plane 2 최종 회전값: {currentPlane2HitRotation}\n" +
                $"Noise Flow X: {currentHitNoiseFlowX}",
                this
            );
        }

        // 기본 상태에서 유효타 위치와 회전으로 이동합니다.
        yield return AnimateValues(
            plane1IdlePosition,
            plane1HitPosition,
            plane1IdleQuaternion,
            plane1HitQuaternion,

            plane2IdlePosition,
            plane2HitPosition,
            plane2IdleQuaternion,
            plane2HitQuaternion,

            moveOutDuration,
            moveOutCurve
        );

        // 유효타 위치, 회전, Noise Flow X값을 잠시 고정합니다.
        yield return HoldHitValues(
            plane1HitPosition,
            plane1HitQuaternion,

            plane2HitPosition,
            plane2HitQuaternion,

            currentHitNoiseFlowX,
            holdDuration
        );

        // 유효타 상태에서 기본 위치와 회전으로 돌아옵니다.
        yield return AnimateValues(
            plane1HitPosition,
            plane1IdlePosition,
            plane1HitQuaternion,
            plane1IdleQuaternion,

            plane2HitPosition,
            plane2IdlePosition,
            plane2HitQuaternion,
            plane2IdleQuaternion,

            returnDuration,
            returnCurve
        );

        ApplyIdleValues();

        if (restoreNoiseFlowAfterHit)
        {
            RestoreNoiseFlow();
        }

        hitRoutine = null;
    }

    /// <summary>
    /// 유효타마다 사용할 랜덤 회전 증감값을 생성합니다.
    /// </summary>
    private Vector3 CreateRandomRotation()
    {
        float minX = Mathf.Min(
            randomRotationMin.x,
            randomRotationMax.x
        );

        float maxX = Mathf.Max(
            randomRotationMin.x,
            randomRotationMax.x
        );

        float minY = Mathf.Min(
            randomRotationMin.y,
            randomRotationMax.y
        );

        float maxY = Mathf.Max(
            randomRotationMin.y,
            randomRotationMax.y
        );

        float minZ = Mathf.Min(
            randomRotationMin.z,
            randomRotationMax.z
        );

        float maxZ = Mathf.Max(
            randomRotationMin.z,
            randomRotationMax.z
        );

        return new Vector3(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY),
            Random.Range(minZ, maxZ)
        );
    }

    /// <summary>
    /// 이번 유효타에서 사용할 Noise Flow X값을 생성합니다.
    /// </summary>
    private float CreateHitNoiseFlowX()
    {
        float minimum = Mathf.Min(
            randomNoiseFlowXMin,
            randomNoiseFlowXMax
        );

        float maximum = Mathf.Max(
            randomNoiseFlowXMin,
            randomNoiseFlowXMax
        );

        return hitNoiseFlowX + Random.Range(minimum, maximum);
    }

    /// <summary>
    /// 두 Plane의 로컬 위치와 로컬 회전을 동시에 변경합니다.
    /// </summary>
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
        // 시간이 0이면 즉시 목표값을 적용합니다.
        if (duration <= 0f)
        {
            ApplyValues(
                plane1PositionTo,
                plane1RotationTo,
                plane2PositionTo,
                plane2RotationTo
            );

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(elapsed / duration);

            float evaluatedTime = curve != null
                ? curve.Evaluate(normalizedTime)
                : normalizedTime;

            Vector3 currentPlane1Position =
                Vector3.LerpUnclamped(
                    plane1PositionFrom,
                    plane1PositionTo,
                    evaluatedTime
                );

            Quaternion currentPlane1Rotation =
                Quaternion.SlerpUnclamped(
                    plane1RotationFrom,
                    plane1RotationTo,
                    evaluatedTime
                );

            Vector3 currentPlane2Position =
                Vector3.LerpUnclamped(
                    plane2PositionFrom,
                    plane2PositionTo,
                    evaluatedTime
                );

            Quaternion currentPlane2Rotation =
                Quaternion.SlerpUnclamped(
                    plane2RotationFrom,
                    plane2RotationTo,
                    evaluatedTime
                );

            ApplyValues(
                currentPlane1Position,
                currentPlane1Rotation,
                currentPlane2Position,
                currentPlane2Rotation
            );

            yield return null;
        }

        // 마지막 프레임에서 정확한 목표값을 적용합니다.
        ApplyValues(
            plane1PositionTo,
            plane1RotationTo,
            plane2PositionTo,
            plane2RotationTo
        );
    }

    /// <summary>
    /// 지정한 시간 동안 유효타 위치, 회전,
    /// Noise Flow X값을 매 프레임 유지합니다.
    /// </summary>
    private IEnumerator HoldHitValues(
        Vector3 plane1Position,
        Quaternion plane1Rotation,
        Vector3 plane2Position,
        Quaternion plane2Rotation,
        float noiseFlowX,
        float duration)
    {
        if (duration <= 0f)
        {
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            // 다른 스크립트가 Plane 값을 변경해도
            // 유효타 상태를 유지할 수 있도록 매 프레임 다시 적용합니다.
            ApplyValues(
                plane1Position,
                plane1Rotation,
                plane2Position,
                plane2Rotation
            );

            // Noise Flow X값도 매 프레임 유지합니다.
            SetNoiseFlowX(noiseFlowX);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        ApplyValues(
            plane1Position,
            plane1Rotation,
            plane2Position,
            plane2Rotation
        );

        SetNoiseFlowX(noiseFlowX);
    }

    /// <summary>
    /// 두 Plane에 로컬 위치와 로컬 회전을 적용합니다.
    /// </summary>
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

    /// <summary>
    /// 두 Plane을 기본 위치와 회전으로 복구합니다.
    /// </summary>
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

    /// <summary>
    /// 게임 시작 시 기본 위치와 회전값을 저장합니다.
    /// </summary>
    private void SaveIdleValues()
    {
        if (useCurrentPositionAsIdle)
        {
            if (plane1 != null)
            {
                plane1IdlePosition =
                    plane1.localPosition;
            }

            if (plane2 != null)
            {
                plane2IdlePosition =
                    plane2.localPosition;
            }
        }

        if (useCurrentRotationAsIdle)
        {
            if (plane1 != null)
            {
                plane1IdleRotation =
                    plane1.localEulerAngles;
            }

            if (plane2 != null)
            {
                plane2IdleRotation =
                    plane2.localEulerAngles;
            }
        }

        plane1IdleQuaternion =
            Quaternion.Euler(plane1IdleRotation);

        plane2IdleQuaternion =
            Quaternion.Euler(plane2IdleRotation);
    }

    /// <summary>
    /// Noise Flow 프로퍼티를 확인하고 초기값을 저장합니다.
    /// </summary>
    private void InitializeNoiseFlow()
    {
        hasNoiseFlowProperty = false;

        if (noiseFlowMaterial == null)
        {
            if (showDebugLog)
            {
                Debug.LogWarning(
                    "[BattleEffectPlaneRotation] Noise Flow 머티리얼이 연결되지 않았습니다.",
                    this
                );
            }

            return;
        }

        noiseFlowPropertyId =
            Shader.PropertyToID(noiseFlowPropertyName);

        if (!noiseFlowMaterial.HasProperty(noiseFlowPropertyId))
        {
            Debug.LogWarning(
                "[BattleEffectPlaneRotation] 머티리얼에서 프로퍼티를 찾지 못했습니다.\n" +
                $"프로퍼티 이름: {noiseFlowPropertyName}\n" +
                "Shader Graph의 Reference 이름을 확인하세요.",
                noiseFlowMaterial
            );

            return;
        }

        hasNoiseFlowProperty = true;

        // Noise Flow의 X, Y, Z, W 전체 값을 저장합니다.
        idleNoiseFlowVector =
            noiseFlowMaterial.GetVector(noiseFlowPropertyId);

        if (useCurrentNoiseFlowXAsIdle)
        {
            // 머티리얼에 현재 적용된 X값을 기본값으로 저장합니다.
            idleNoiseFlowX =
                idleNoiseFlowVector.x;
        }
        else
        {
            // 인스펙터에 입력한 X값을 기본값으로 적용합니다.
            idleNoiseFlowVector.x =
                idleNoiseFlowX;

            noiseFlowMaterial.SetVector(
                noiseFlowPropertyId,
                idleNoiseFlowVector
            );
        }
    }

    /// <summary>
    /// Noise Flow Vector의 X값만 변경합니다.
    /// Y, Z, W값은 현재 값을 유지합니다.
    /// </summary>
    private void SetNoiseFlowX(float xValue)
    {
        if (!hasNoiseFlowProperty || noiseFlowMaterial == null)
        {
            return;
        }

        Vector4 currentNoiseFlow =
            noiseFlowMaterial.GetVector(noiseFlowPropertyId);

        currentNoiseFlow.x = xValue;

        noiseFlowMaterial.SetVector(
            noiseFlowPropertyId,
            currentNoiseFlow
        );
    }

    /// <summary>
    /// Noise Flow를 게임 시작 시 저장한 전체 Vector 값으로 복구합니다.
    /// </summary>
    private void RestoreNoiseFlow()
    {
        if (!hasNoiseFlowProperty || noiseFlowMaterial == null)
        {
            return;
        }

        noiseFlowMaterial.SetVector(
            noiseFlowPropertyId,
            idleNoiseFlowVector
        );
    }

    /// <summary>
    /// 인스펙터에 연결되지 않은 Plane을
    /// 자식 오브젝트의 이름으로 자동 탐색합니다.
    /// </summary>
    private void FindPlanesAutomatically()
    {
        if (plane1 != null && plane2 != null)
        {
            return;
        }

        Transform[] children =
            GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];

            if (child == transform)
            {
                continue;
            }

            if (plane1 == null && child.name == "Plane")
            {
                plane1 = child;
                continue;
            }

            if (plane2 == null && child.name == "Plane (1)")
            {
                plane2 = child;
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        moveOutDuration =
            Mathf.Max(0f, moveOutDuration);

        holdDuration =
            Mathf.Max(0f, holdDuration);

        returnDuration =
            Mathf.Max(0f, returnDuration);
    }
#endif
}