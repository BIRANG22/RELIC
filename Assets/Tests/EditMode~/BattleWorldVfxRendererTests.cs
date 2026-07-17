using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleWorldVfxRendererTests
{
    private const string RendererRootName = "__BattleWorldVfxRenderer";

    [TearDown]
    public void TearDown()
    {
        GameObject rendererRoot = GameObject.Find(RendererRootName);
        if (rendererRoot != null)
            DestroyObject(rendererRoot);
    }

    [Test]
    public void BattleVfxEntry_DefaultsToIndividualWorldRenderTexture()
    {
        BattleVfxEntry entry = new();

        Assert.That(entry.renderMode, Is.EqualTo(BattleVfxRenderMode.IndividualWorldRenderTexture));
        Assert.That(entry.proxyBlendMode, Is.EqualTo(BattleVfxProxyBlendMode.Additive));
        Assert.That(entry.renderTextureWidth, Is.GreaterThan(0));
        Assert.That(entry.renderTextureHeight, Is.GreaterThan(0));
        Assert.That(entry.proxySortingLayerName, Is.EqualTo("Unit"));
        Assert.That(entry.proxySortingWorldYOffset, Is.EqualTo(0f));
        Assert.That(entry.sfx, Is.Not.Null);
        Assert.That(entry.sfx.playSfx, Is.False);
        Assert.That(entry.sfx.routeEmbeddedAudioSourcesThroughAudioManager, Is.True);
        Assert.That(entry.sfx.removeEmbeddedAudioSources, Is.True);
    }

    [Test]
    public void SortUtility_UsesSameNegativeYConventionAsUnitYSort()
    {
        int order = BattleWorldVfxSortUtility.CalculateSortingOrder(
            y: 1.25f,
            yMultiplier: 100f,
            offset: 7);

        Assert.That(order, Is.EqualTo(-118));
    }

    [Test]
    public void VfxHandle_UsesSortingTargetInsteadOfProxyY()
    {
        GameObject proxy = new("Proxy");
        GameObject sortingTarget = new("SpriteRoot");
        MeshRenderer proxyRenderer = proxy.AddComponent<MeshRenderer>();
        BattleWorldVfxHandle handle = proxy.AddComponent<BattleWorldVfxHandle>();

        try
        {
            proxy.transform.position = new Vector3(0f, 10f, 0f);
            sortingTarget.transform.position = new Vector3(0f, 2f, 0f);

            handle.Initialize(
                followTarget: null,
                followWorldOffset: Vector3.zero,
                proxyRenderer: proxyRenderer,
                sortingOrderOffset: 5,
                sortingWorldYOffset: 0f,
                yMultiplier: 100f,
                renderGroup: null,
                renderTexture: null,
                runtimeMaterial: null);

            handle.SetSortingTarget(sortingTarget.transform, -0.1f);

            Assert.That(
                proxyRenderer.sortingOrder,
                Is.EqualTo(BattleWorldVfxSortUtility.CalculateSortingOrder(1.9f, 100f, 5)));

            handle.SetWorldPosition(new Vector3(0f, 20f, 0f));

            Assert.That(
                proxyRenderer.sortingOrder,
                Is.EqualTo(BattleWorldVfxSortUtility.CalculateSortingOrder(1.9f, 100f, 5)));
        }
        finally
        {
            DestroyObject(sortingTarget);
            DestroyObject(proxy);
        }
    }

    [Test]
    public void BattleVfxAudioUtility_RemovesEmbeddedAudioSourcesFromRuntimeVfx()
    {
        GameObject vfx = new("VfxWithEmbeddedAudio");
        vfx.AddComponent<AudioSource>();

        try
        {
            BattleVfxAudioUtility.PlayAndStripEmbeddedAudioSources(
                vfx,
                new BattleVfxSfxEntry(),
                coroutineHost: null);

            Assert.That(vfx.GetComponentsInChildren<AudioSource>(true), Is.Empty);
        }
        finally
        {
            DestroyObject(vfx);
        }
    }

    [Test]
    public void AudioSourcePlaybackSettings_CopiesEmbeddedVfxAudioSourcePlaybackProperties()
    {
        GameObject sourceObject = new("EmbeddedAudioSource");
        GameObject targetObject = new("RoutedAudioSource");
        AudioClip clip = AudioClip.Create("VfxClip", 4410, 1, 44100, false);
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        AudioSource target = targetObject.AddComponent<AudioSource>();
        AnimationCurve customRolloff = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        try
        {
            source.clip = clip;
            source.outputAudioMixerGroup = null;
            source.bypassEffects = true;
            source.bypassListenerEffects = true;
            source.bypassReverbZones = true;
            source.playOnAwake = true;
            source.loop = true;
            source.priority = 44;
            source.volume = 0.42f;
            source.pitch = 1.35f;
            source.panStereo = -0.25f;
            source.spatialBlend = 0.75f;
            source.reverbZoneMix = 0.62f;
            source.dopplerLevel = 2.3f;
            source.spread = 155f;
            source.rolloffMode = AudioRolloffMode.Custom;
            source.minDistance = 2f;
            source.maxDistance = 35f;
            source.ignoreListenerPause = true;
            source.ignoreListenerVolume = true;
            source.spatialize = true;
            source.spatializePostEffects = true;
            source.velocityUpdateMode = AudioVelocityUpdateMode.Fixed;
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, customRolloff);
            sourceObject.transform.position = new Vector3(3f, 4f, 5f);
            sourceObject.transform.rotation = Quaternion.Euler(10f, 20f, 30f);

            AudioSourcePlaybackSettings settings = AudioSourcePlaybackSettings.From(source);
            settings.ApplyTo(target, volumeMultiplier: 0.5f);

            Assert.That(settings.WorldPosition, Is.EqualTo(sourceObject.transform.position));
            Assert.That(settings.WorldRotation.eulerAngles, Is.EqualTo(sourceObject.transform.rotation.eulerAngles));
            Assert.That(target.clip, Is.SameAs(clip));
            Assert.That(target.bypassEffects, Is.True);
            Assert.That(target.bypassListenerEffects, Is.True);
            Assert.That(target.bypassReverbZones, Is.True);
            Assert.That(target.playOnAwake, Is.False);
            Assert.That(target.loop, Is.True);
            Assert.That(target.priority, Is.EqualTo(44));
            Assert.That(target.volume, Is.EqualTo(0.21f).Within(0.0001f));
            Assert.That(target.pitch, Is.EqualTo(1.35f).Within(0.0001f));
            Assert.That(target.panStereo, Is.EqualTo(-0.25f).Within(0.0001f));
            Assert.That(target.spatialBlend, Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(target.reverbZoneMix, Is.EqualTo(0.62f).Within(0.0001f));
            Assert.That(target.dopplerLevel, Is.EqualTo(2.3f).Within(0.0001f));
            Assert.That(target.spread, Is.EqualTo(155f).Within(0.0001f));
            Assert.That(target.rolloffMode, Is.EqualTo(AudioRolloffMode.Custom));
            Assert.That(target.minDistance, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(target.maxDistance, Is.EqualTo(35f).Within(0.0001f));
            Assert.That(target.ignoreListenerPause, Is.True);
            Assert.That(target.ignoreListenerVolume, Is.True);
            Assert.That(target.spatialize, Is.True);
            Assert.That(target.spatializePostEffects, Is.True);
            Assert.That(target.velocityUpdateMode, Is.EqualTo(AudioVelocityUpdateMode.Fixed));
            Assert.That(
                target.GetCustomCurve(AudioSourceCurveType.CustomRolloff).keys.Length,
                Is.EqualTo(customRolloff.keys.Length));
        }
        finally
        {
            DestroyObject(clip);
            DestroyObject(targetObject);
            DestroyObject(sourceObject);
        }
    }

    [Test]
    public void BattleVfxSfxEntry_CopyFromKeepsExplicitSfxSettings()
    {
        BattleVfxSfxEntry source = new()
        {
            playSfx = true,
            sfxId = "vfx.skill",
            delay = 0.25f,
            volumeMultiplier = 0.5f,
            routeEmbeddedAudioSourcesThroughAudioManager = false,
            removeEmbeddedAudioSources = true
        };

        BattleVfxSfxEntry copy = BattleVfxSfxEntry.CopyFrom(source);

        Assert.That(copy, Is.Not.SameAs(source));
        Assert.That(copy.playSfx, Is.True);
        Assert.That(copy.sfxId, Is.EqualTo("vfx.skill"));
        Assert.That(copy.delay, Is.EqualTo(0.25f));
        Assert.That(copy.volumeMultiplier, Is.EqualTo(0.5f));
        Assert.That(copy.routeEmbeddedAudioSourcesThroughAudioManager, Is.False);
        Assert.That(copy.removeEmbeddedAudioSources, Is.True);
    }

    [Test]
    public void ProxyMaterialTemplate_LoadsFromResourcesWithWorldVfxShader()
    {
        Material material = Resources.Load<Material>("BattleWorldVfxProxyMaterial");

        Assert.That(material, Is.Not.Null);
        Assert.That(material.shader, Is.Not.Null);
        Assert.That(material.shader.name, Is.EqualTo("Relic/World/VFX RenderTexture Additive"));
    }

    [Test]
    public void CreateProxyMaterial_ClonesResourceTemplateAndAssignsRenderTexture()
    {
        GameObject rendererRoot = new("Renderer");
        BattleWorldVfxRenderer renderer = rendererRoot.AddComponent<BattleWorldVfxRenderer>();
        GameObject prefab = new("VfxPrefab");
        RenderTexture renderTexture = new(1, 1, 0, RenderTextureFormat.ARGB32);
        Material template = Resources.Load<Material>("BattleWorldVfxProxyMaterial");
        Material runtimeMaterial = null;

        try
        {
            BattleVfxEntry entry = new()
            {
                prefab = prefab
            };

            MethodInfo method = typeof(BattleWorldVfxRenderer).GetMethod(
                "CreateProxyMaterial",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            runtimeMaterial = (Material)method.Invoke(renderer, new object[] { entry, renderTexture });

            Assert.That(runtimeMaterial, Is.Not.Null);
            Assert.That(runtimeMaterial, Is.Not.SameAs(template));
            Assert.That(runtimeMaterial.shader.name, Is.EqualTo("Relic/World/VFX RenderTexture Additive"));
            Assert.That(runtimeMaterial.mainTexture, Is.SameAs(renderTexture));
        }
        finally
        {
            DestroyObject(runtimeMaterial);
            DestroyObject(renderTexture);
            DestroyObject(prefab);
            DestroyObject(rendererRoot);
        }
    }

    [Test]
    public void CreateProxyMaterial_UsesAlphaTemplateWhenEntryRequestsAlphaBlend()
    {
        GameObject rendererRoot = new("Renderer");
        BattleWorldVfxRenderer renderer = rendererRoot.AddComponent<BattleWorldVfxRenderer>();
        GameObject prefab = new("BlackVfxPrefab");
        RenderTexture renderTexture = new(1, 1, 0, RenderTextureFormat.ARGB32);
        Material runtimeMaterial = null;

        try
        {
            BattleVfxEntry entry = new()
            {
                prefab = prefab,
                proxyBlendMode = BattleVfxProxyBlendMode.Alpha
            };

            MethodInfo method = typeof(BattleWorldVfxRenderer).GetMethod(
                "CreateProxyMaterial",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            runtimeMaterial = (Material)method.Invoke(renderer, new object[] { entry, renderTexture });

            Assert.That(runtimeMaterial, Is.Not.Null);
            Assert.That(runtimeMaterial.shader.name, Is.EqualTo("Relic/World/VFX RenderTexture Alpha"));
            Assert.That(runtimeMaterial.mainTexture, Is.SameAs(renderTexture));
        }
        finally
        {
            DestroyObject(runtimeMaterial);
            DestroyObject(renderTexture);
            DestroyObject(prefab);
            DestroyObject(rendererRoot);
        }
    }

    [Test]
    public void TrySpawnDetached_ReturnsFalseWhenVfxSetupThrows()
    {
        GameObject prefab = new("ThrowingVfxPrefab");

        try
        {
            BattleVfxEntry entry = new()
            {
                prefab = prefab
            };

            bool spawned = false;
            BattleWorldVfxHandle handle = null;

            Assert.DoesNotThrow(() =>
            {
                spawned = BattleWorldVfxRenderer.TrySpawnDetached(
                    entry,
                    Vector3.zero,
                    renderLayer: 0,
                    visibleLayer: 0,
                    lifeTime: 0.1f,
                    _ => throw new InvalidCastException("simulated broken VFX prefab"),
                    out handle);
            });

            Assert.That(spawned, Is.False);
            Assert.That(handle, Is.Null);
        }
        finally
        {
            DestroyObject(prefab);
        }
    }

    private static void DestroyObject(UnityEngine.Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            UnityEngine.Object.Destroy(target);
        else
            UnityEngine.Object.DestroyImmediate(target);
    }
}
