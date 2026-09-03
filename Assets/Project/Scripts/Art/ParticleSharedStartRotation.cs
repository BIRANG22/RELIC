using UnityEngine;

/// <summary>
/// 기준 파티클에서 3D Start Rotation 값을 한 번 추첨한 뒤,
/// 자신과 모든 자식 Particle System에 동일하게 적용합니다.
/// </summary>
[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public sealed class ParticleSharedStartRotation : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("랜덤 회전 범위를 가져올 기준 파티클입니다. 비워 두면 이 오브젝트에서 찾습니다.")]
    [SerializeField] private ParticleSystem sourceParticleSystem;

    [Header("Playback")]
    [Tooltip("회전 적용 전에 기존 파티클을 지우고 기준 파티클부터 다시 재생합니다.")]
    [SerializeField] private bool restartAfterApply = true;

    private ParticleSystem[] targetParticleSystems;
    private ParticleSystem.MinMaxCurve sourceRotationX;
    private ParticleSystem.MinMaxCurve sourceRotationY;
    private ParticleSystem.MinMaxCurve sourceRotationZ;
    private bool initialized;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        ApplySharedStartRotation();
    }

    /// <summary>
    /// 기준 파티클의 회전 범위에서 X/Y/Z를 각각 한 번만 추첨하여
    /// 모든 대상 파티클에 같은 값으로 적용합니다.
    /// </summary>
    public void ApplySharedStartRotation()
    {
        if (!initialized && !Initialize())
            return;

        float rotationX = sourceRotationX.Evaluate(0f, Random.value);
        float rotationY = sourceRotationY.Evaluate(0f, Random.value);
        float rotationZ = sourceRotationZ.Evaluate(0f, Random.value);

        if (restartAfterApply)
            sourceParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        for (int i = 0; i < targetParticleSystems.Length; i++)
        {
            ParticleSystem particleSystem = targetParticleSystems[i];
            if (particleSystem == null)
                continue;

            ParticleSystem.MainModule main = particleSystem.main;
            main.startRotation3D = true;
            main.startRotationX = rotationX;
            main.startRotationY = rotationY;
            main.startRotationZ = rotationZ;
        }

        if (restartAfterApply)
            sourceParticleSystem.Play(true);
    }

    private bool Initialize()
    {
        if (sourceParticleSystem == null)
            sourceParticleSystem = GetComponent<ParticleSystem>();

        if (sourceParticleSystem == null)
        {
            Debug.LogError(
                $"[{nameof(ParticleSharedStartRotation)}] 기준 Particle System을 찾을 수 없습니다.",
                this);
            enabled = false;
            return false;
        }

        targetParticleSystems = sourceParticleSystem.GetComponentsInChildren<ParticleSystem>(true);

        ParticleSystem.MainModule sourceMain = sourceParticleSystem.main;
        sourceRotationX = sourceMain.startRotationX;
        sourceRotationY = sourceMain.startRotationY;
        sourceRotationZ = sourceMain.startRotationZ;

        initialized = true;
        return true;
    }
}
