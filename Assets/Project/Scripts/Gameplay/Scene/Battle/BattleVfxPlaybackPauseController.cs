using System.Collections.Generic;
using UnityEngine;

public sealed class BattleVfxPlaybackPauseController : MonoBehaviour
{
    public const float SlowMotionSpeedMultiplier = 0.2f;

    private static readonly List<BattleVfxPlaybackPauseController> ActiveControllers = new();
    private static readonly List<ParticleSpeedState> SceneParticleStates = new();
    private static readonly List<AnimatorSpeedState> SceneAnimatorStates = new();
    private static int globalPauseDepth;

    private readonly List<ParticleSpeedState> particleStates = new();
    private readonly List<AnimatorSpeedState> animatorStates = new();

    private ParticleSystem[] particles;
    private Animator[] animators;
    private bool isSlowedByController;

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

    public static bool IsGlobalPauseActive => globalPauseDepth > 0;

    public static float ActiveSpeedMultiplier =>
        IsGlobalPauseActive ? SlowMotionSpeedMultiplier : 1f;

    public static void PauseAll()
    {
        bool firstPause = globalPauseDepth <= 0;
        globalPauseDepth++;

        if (!firstPause)
            return;

        BattleWorldVfxHandle.PauseAllActiveHandles();

        for (int i = ActiveControllers.Count - 1; i >= 0; i--)
        {
            BattleVfxPlaybackPauseController controller = ActiveControllers[i];

            if (controller == null)
            {
                ActiveControllers.RemoveAt(i);
                continue;
            }

            controller.ApplySlowMotionPlayback();
        }

        ApplySlowMotionToUncontrolledSceneVfx();
    }

    public static void ResumeAll()
    {
        if (globalPauseDepth <= 0)
            return;

        globalPauseDepth--;

        if (globalPauseDepth > 0)
            return;

        RestoreUncontrolledSceneVfx();
        BattleWorldVfxHandle.ResumeAllActiveHandles();

        for (int i = ActiveControllers.Count - 1; i >= 0; i--)
        {
            BattleVfxPlaybackPauseController controller = ActiveControllers[i];

            if (controller == null)
            {
                ActiveControllers.RemoveAt(i);
                continue;
            }

            controller.RestorePlayback();
        }
    }

    private void OnEnable()
    {
        if (!ActiveControllers.Contains(this))
            ActiveControllers.Add(this);

        if (IsGlobalPauseActive)
            ApplySlowMotionPlayback();
    }

    private void OnDisable()
    {
        RestorePlayback();
        ActiveControllers.Remove(this);
    }

    private void OnDestroy()
    {
        RestorePlayback();
        ActiveControllers.Remove(this);
    }

    private void ApplySlowMotionPlayback()
    {
        if (isSlowedByController)
            return;

        CachePlaybackComponents();
        particleStates.Clear();
        animatorStates.Clear();

        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem particle = particles[i];

            if (particle == null)
                continue;

            float simulationSpeed = GetParticleSimulationSpeed(particle);
            particleStates.Add(new ParticleSpeedState
            {
                Particle = particle,
                SimulationSpeed = simulationSpeed
            });

            SetParticleSimulationSpeed(particle, simulationSpeed * SlowMotionSpeedMultiplier);
        }

        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];

            if (animator == null)
                continue;

            animatorStates.Add(new AnimatorSpeedState
            {
                Animator = animator,
                Speed = animator.speed
            });

            animator.speed *= SlowMotionSpeedMultiplier;
        }

        isSlowedByController = true;
    }

    private void RestorePlayback()
    {
        if (!isSlowedByController)
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
        isSlowedByController = false;
    }

    private void CachePlaybackComponents()
    {
        particles = GetComponentsInChildren<ParticleSystem>(true);
        animators = GetComponentsInChildren<Animator>(true);
    }

    private static void ApplySlowMotionToUncontrolledSceneVfx()
    {
        SceneParticleStates.Clear();
        SceneAnimatorStates.Clear();

        ParticleSystem[] sceneParticles = Object.FindObjectsByType<ParticleSystem>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < sceneParticles.Length; i++)
        {
            ParticleSystem particle = sceneParticles[i];

            if (!IsUncontrolledBattleVfxComponent(particle))
                continue;

            float simulationSpeed = GetParticleSimulationSpeed(particle);
            SceneParticleStates.Add(new ParticleSpeedState
            {
                Particle = particle,
                SimulationSpeed = simulationSpeed
            });

            SetParticleSimulationSpeed(particle, simulationSpeed * SlowMotionSpeedMultiplier);
        }

        Animator[] sceneAnimators = Object.FindObjectsByType<Animator>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < sceneAnimators.Length; i++)
        {
            Animator animator = sceneAnimators[i];

            if (!IsUncontrolledBattleVfxComponent(animator))
                continue;

            SceneAnimatorStates.Add(new AnimatorSpeedState
            {
                Animator = animator,
                Speed = animator.speed
            });

            animator.speed *= SlowMotionSpeedMultiplier;
        }
    }

    private static void RestoreUncontrolledSceneVfx()
    {
        for (int i = 0; i < SceneParticleStates.Count; i++)
        {
            ParticleSpeedState state = SceneParticleStates[i];

            if (state.Particle != null)
                SetParticleSimulationSpeed(state.Particle, state.SimulationSpeed);
        }

        for (int i = 0; i < SceneAnimatorStates.Count; i++)
        {
            AnimatorSpeedState state = SceneAnimatorStates[i];

            if (state.Animator != null)
                state.Animator.speed = state.Speed;
        }

        SceneParticleStates.Clear();
        SceneAnimatorStates.Clear();
    }

    private static bool IsUncontrolledBattleVfxComponent(Component component)
    {
        if (component == null)
            return false;

        if (component.GetComponentInParent<BattleVfxPlaybackPauseController>() != null)
            return false;

        int vfxLayer = LayerMask.NameToLayer("VFX");
        if (vfxLayer >= 0 && component.gameObject.layer == vfxLayer)
            return true;

        Transform current = component.transform;
        while (current != null)
        {
            if (current.name == "__BattleWorldVfxRenderer" ||
                current.name == "RenderSpace" ||
                current.name == "WorldProxies")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
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
}
