using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class BattleWorldVfxHandle : MonoBehaviour
{
    private static readonly List<BattleWorldVfxHandle> ActiveHandles = new();

    private Transform followTarget;
    private Vector3 followWorldOffset;
    private Renderer proxyRenderer;
    private int sortingOrderOffset;
    private float sortingWorldYOffset;
    private float yMultiplier;
    private Transform sortingTarget;
    private float sortingTargetWorldYOffset;
    private GameObject renderGroup;
    private RenderTexture renderTexture;
    private Material runtimeMaterial;
    private bool cleanedUp;
    private bool renderPlaybackSlowed;

    private readonly List<ParticleSpeedState> particleStates = new();
    private readonly List<AnimatorSpeedState> animatorStates = new();

    private struct ParticleSpeedState
    {
        public ParticleSystem Particle;
        public float SimulationSpeed;
    }

    private struct AnimatorSpeedState
    {
        public Animator Animator;
        public float Speed;
    }

    public static void PauseAllActiveHandles()
    {
        for (int i = ActiveHandles.Count - 1; i >= 0; i--)
        {
            BattleWorldVfxHandle handle = ActiveHandles[i];

            if (handle == null)
            {
                ActiveHandles.RemoveAt(i);
                continue;
            }

            handle.ApplySlowMotionPlayback();
        }
    }

    public static void ResumeAllActiveHandles()
    {
        for (int i = ActiveHandles.Count - 1; i >= 0; i--)
        {
            BattleWorldVfxHandle handle = ActiveHandles[i];

            if (handle == null)
            {
                ActiveHandles.RemoveAt(i);
                continue;
            }

            handle.RestorePlayback();
        }
    }

    public void Initialize(
        Transform followTarget,
        Vector3 followWorldOffset,
        Renderer proxyRenderer,
        int sortingOrderOffset,
        float sortingWorldYOffset,
        float yMultiplier,
        GameObject renderGroup,
        RenderTexture renderTexture,
        Material runtimeMaterial)
    {
        this.followTarget = followTarget;
        this.followWorldOffset = followWorldOffset;
        this.proxyRenderer = proxyRenderer;
        this.sortingOrderOffset = sortingOrderOffset;
        this.sortingWorldYOffset = sortingWorldYOffset;
        this.yMultiplier = yMultiplier;
        this.renderGroup = renderGroup;
        this.renderTexture = renderTexture;
        this.runtimeMaterial = runtimeMaterial;

        RefreshTransformAndSorting();

        if (BattleVfxPlaybackPauseController.IsGlobalPauseActive)
            ApplySlowMotionPlayback();
    }

    public void SetSortingTarget(Transform target, float worldYOffset)
    {
        sortingTarget = target;
        sortingTargetWorldYOffset = worldYOffset;
        RefreshSorting();
    }

    public void SetWorldPosition(Vector3 position)
    {
        followTarget = null;
        transform.position = position + followWorldOffset;
        RefreshSorting();
    }

    public IEnumerator DestroyAfter(float lifeTime)
    {
        if (lifeTime <= 0f)
        {
            yield return null;
        }
        else
        {
            float elapsed = 0f;

            while (elapsed < lifeTime)
            {
                elapsed += BattleVfxPlaybackPauseController.IsGlobalPauseActive
                    ? Time.deltaTime * BattleVfxPlaybackPauseController.ActiveSpeedMultiplier
                    : Time.deltaTime;

                yield return null;
            }
        }

        if (this != null)
            Destroy(gameObject);
    }

    private void OnEnable()
    {
        if (!ActiveHandles.Contains(this))
            ActiveHandles.Add(this);
    }

    private void OnDisable()
    {
        RestorePlayback();
        ActiveHandles.Remove(this);
    }

    private void LateUpdate()
    {
        if (followTarget == null && sortingTarget == null)
            return;

        RefreshTransformAndSorting();
    }

    private void OnDestroy()
    {
        RestorePlayback();
        ActiveHandles.Remove(this);
        Cleanup();
    }

    private void RefreshTransformAndSorting()
    {
        if (followTarget != null)
            transform.position = followTarget.position + followWorldOffset;

        RefreshSorting();
    }

    private void RefreshSorting()
    {
        if (proxyRenderer == null)
            return;

        float sortingY = transform.position.y + sortingWorldYOffset;

        if (sortingTarget != null)
            sortingY = sortingTarget.position.y + sortingTargetWorldYOffset;

        proxyRenderer.sortingOrder = BattleWorldVfxSortUtility.CalculateSortingOrder(
            sortingY,
            yMultiplier,
            sortingOrderOffset);
    }

    private void ApplySlowMotionPlayback()
    {
        if (renderPlaybackSlowed || renderGroup == null)
            return;

        particleStates.Clear();
        animatorStates.Clear();

        ParticleSystem[] particles = renderGroup.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem particle = particles[i];

            if (particle == null || IsManagedByPlaybackController(particle))
                continue;

            float simulationSpeed = GetParticleSimulationSpeed(particle);
            particleStates.Add(new ParticleSpeedState
            {
                Particle = particle,
                SimulationSpeed = simulationSpeed
            });

            SetParticleSimulationSpeed(
                particle,
                simulationSpeed * BattleVfxPlaybackPauseController.SlowMotionSpeedMultiplier);
        }

        Animator[] animators = renderGroup.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];

            if (animator == null || IsManagedByPlaybackController(animator))
                continue;

            animatorStates.Add(new AnimatorSpeedState
            {
                Animator = animator,
                Speed = animator.speed
            });

            animator.speed *= BattleVfxPlaybackPauseController.SlowMotionSpeedMultiplier;
        }

        renderPlaybackSlowed = true;
    }

    private void RestorePlayback()
    {
        if (!renderPlaybackSlowed)
            return;

        for (int i = 0; i < particleStates.Count; i++)
        {
            ParticleSpeedState state = particleStates[i];

            if (state.Particle != null)
                SetParticleSimulationSpeed(state.Particle, state.SimulationSpeed);
        }

        for (int i = 0; i < animatorStates.Count; i++)
        {
            AnimatorSpeedState state = animatorStates[i];

            if (state.Animator != null)
                state.Animator.speed = state.Speed;
        }

        particleStates.Clear();
        animatorStates.Clear();
        renderPlaybackSlowed = false;
    }

    private static bool IsManagedByPlaybackController(Component component)
    {
        return component != null &&
               component.GetComponentInParent<BattleVfxPlaybackPauseController>() != null;
    }

    private static float GetParticleSimulationSpeed(ParticleSystem particle)
    {
        ParticleSystem.MainModule main = particle.main;
        return main.simulationSpeed;
    }

    private static void SetParticleSimulationSpeed(ParticleSystem particle, float simulationSpeed)
    {
        ParticleSystem.MainModule main = particle.main;
        main.simulationSpeed = Mathf.Max(0f, simulationSpeed);
    }

    private void Cleanup()
    {
        if (cleanedUp)
            return;

        cleanedUp = true;

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }

        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
            runtimeMaterial = null;
        }

        if (renderGroup != null)
        {
            Destroy(renderGroup);
            renderGroup = null;
        }
    }
}
