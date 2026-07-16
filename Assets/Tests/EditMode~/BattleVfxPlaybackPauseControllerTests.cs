using NUnit.Framework;
using UnityEngine;

public class BattleVfxPlaybackPauseControllerTests
{
    [Test]
    public void PauseAll_SlowsPlayingParticleAndResumeAllRestoresIt()
    {
        GameObject vfxObject = new("PauseController_Particle");

        try
        {
            ParticleSystem particle = vfxObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particle.main;
            main.simulationSpeed = 0.8f;
            vfxObject.AddComponent<BattleVfxPlaybackPauseController>();

            particle.Play();
            Assert.That(particle.isPlaying, Is.True);

            BattleVfxPlaybackPauseController.PauseAll();

            main = particle.main;
            Assert.That(particle.isPlaying, Is.True);
            Assert.That(
                main.simulationSpeed,
                Is.EqualTo(0.8f * BattleVfxPlaybackPauseController.SlowMotionSpeedMultiplier)
                    .Within(0.0001f));

            BattleVfxPlaybackPauseController.ResumeAll();

            main = particle.main;
            Assert.That(main.simulationSpeed, Is.EqualTo(0.8f).Within(0.0001f));
        }
        finally
        {
            BattleVfxPlaybackPauseController.ResumeAll();
            Object.DestroyImmediate(vfxObject);
        }
    }

    [Test]
    public void PauseAll_RestoresAnimatorSpeedOnResumeAll()
    {
        GameObject vfxObject = new("PauseController_Animator");

        try
        {
            Animator animator = vfxObject.AddComponent<Animator>();
            animator.speed = 0.7f;
            vfxObject.AddComponent<BattleVfxPlaybackPauseController>();

            BattleVfxPlaybackPauseController.PauseAll();

            Assert.That(
                animator.speed,
                Is.EqualTo(0.7f * BattleVfxPlaybackPauseController.SlowMotionSpeedMultiplier)
                    .Within(0.0001f));

            BattleVfxPlaybackPauseController.ResumeAll();

            Assert.That(animator.speed, Is.EqualTo(0.7f).Within(0.0001f));
        }
        finally
        {
            BattleVfxPlaybackPauseController.ResumeAll();
            Object.DestroyImmediate(vfxObject);
        }
    }

    [Test]
    public void PauseAll_SlowsPlayOnAwakeParticleEvenBeforeFirstFrame()
    {
        GameObject vfxObject = new("PauseController_PlayOnAwake");

        try
        {
            ParticleSystem particle = vfxObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particle.main;
            main.playOnAwake = true;
            vfxObject.AddComponent<BattleVfxPlaybackPauseController>();

            BattleVfxPlaybackPauseController.PauseAll();

            main = particle.main;
            Assert.That(
                main.simulationSpeed,
                Is.EqualTo(BattleVfxPlaybackPauseController.SlowMotionSpeedMultiplier)
                    .Within(0.0001f));

            BattleVfxPlaybackPauseController.ResumeAll();

            main = particle.main;
            Assert.That(main.simulationSpeed, Is.EqualTo(1f).Within(0.0001f));
        }
        finally
        {
            BattleVfxPlaybackPauseController.ResumeAll();
            Object.DestroyImmediate(vfxObject);
        }
    }

    [Test]
    public void PauseAll_SlowsUncontrolledWorldRenderParticle()
    {
        GameObject renderRoot = new("RenderSpace");
        GameObject vfxObject = new("UncontrolledWorldVfx");
        vfxObject.transform.SetParent(renderRoot.transform, false);

        try
        {
            ParticleSystem particle = vfxObject.AddComponent<ParticleSystem>();
            particle.Play();
            Assert.That(particle.isPlaying, Is.True);

            BattleVfxPlaybackPauseController.PauseAll();

            ParticleSystem.MainModule main = particle.main;
            Assert.That(
                main.simulationSpeed,
                Is.EqualTo(BattleVfxPlaybackPauseController.SlowMotionSpeedMultiplier)
                    .Within(0.0001f));

            BattleVfxPlaybackPauseController.ResumeAll();

            main = particle.main;
            Assert.That(main.simulationSpeed, Is.EqualTo(1f).Within(0.0001f));
        }
        finally
        {
            BattleVfxPlaybackPauseController.ResumeAll();
            Object.DestroyImmediate(renderRoot);
        }
    }

    [Test]
    public void PauseAll_KeepsWorldRenderCameraEnabled()
    {
        GameObject rendererRoot = new("__BattleWorldVfxRenderer");
        GameObject renderRoot = new("RenderSpace");
        GameObject renderGroup = new("WorldVfxRender_000");
        GameObject cameraObject = new("Camera");

        try
        {
            renderRoot.transform.SetParent(rendererRoot.transform, false);
            renderGroup.transform.SetParent(renderRoot.transform, false);
            cameraObject.transform.SetParent(renderGroup.transform, false);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.enabled = true;

            BattleVfxPlaybackPauseController.PauseAll();

            Assert.That(camera.enabled, Is.True);

            BattleVfxPlaybackPauseController.ResumeAll();

            Assert.That(camera.enabled, Is.True);
        }
        finally
        {
            BattleVfxPlaybackPauseController.ResumeAll();
            Object.DestroyImmediate(rendererRoot);
        }
    }

    [Test]
    public void PauseAll_SlowsActiveWorldVfxHandleRenderGroup()
    {
        GameObject proxy = GameObject.CreatePrimitive(PrimitiveType.Quad);
        GameObject renderGroup = new("DetachedRenderGroup");
        GameObject particleObject = new("Particle");
        GameObject cameraObject = new("Camera");

        try
        {
            particleObject.transform.SetParent(renderGroup.transform, false);
            cameraObject.transform.SetParent(renderGroup.transform, false);

            ParticleSystem particle = particleObject.AddComponent<ParticleSystem>();
            particle.Play();

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.enabled = true;

            BattleWorldVfxHandle handle = proxy.AddComponent<BattleWorldVfxHandle>();
            handle.Initialize(
                null,
                Vector3.zero,
                proxy.GetComponent<Renderer>(),
                0,
                0f,
                100f,
                renderGroup,
                null,
                null);

            BattleVfxPlaybackPauseController.PauseAll();

            ParticleSystem.MainModule main = particle.main;
            Assert.That(
                main.simulationSpeed,
                Is.EqualTo(BattleVfxPlaybackPauseController.SlowMotionSpeedMultiplier)
                    .Within(0.0001f));
            Assert.That(camera.enabled, Is.True);

            BattleVfxPlaybackPauseController.ResumeAll();

            main = particle.main;
            Assert.That(main.simulationSpeed, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(camera.enabled, Is.True);
        }
        finally
        {
            BattleVfxPlaybackPauseController.ResumeAll();
            Object.DestroyImmediate(proxy);
            Object.DestroyImmediate(renderGroup);
        }
    }
}
