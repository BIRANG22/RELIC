using System;
using UnityEngine;

[Serializable]
public class TestVfxSpawnSettings
{
    [Header("Target Offset")]
    public Vector3 SpawnPositionOffset = Vector3.zero;
    public Vector3 RotationEuler = Vector3.zero;
    public Vector3 ScaleMultiplier = Vector3.one;

    [Header("VFX Entry")]
    public VfxFlipType FlipType = VfxFlipType.None;
    public BattleVfxRenderMode RenderMode = BattleVfxRenderMode.IndividualWorldRenderTexture;

    [Header("SFX")]
    public bool PlaySfx;
    public string SfxId;
    public float SfxDelay;
    public float SfxVolumeMultiplier = 1f;
    public bool RouteEmbeddedAudioSourcesThroughAudioManager = true;
    public bool RemoveEmbeddedAudioSources = true;

    [Header("Layer")]
    public string ObjectLayerName = "VFX";
    public string SortingLayerName = "Unit";
    public int SortingOrderOffset;
    public float SortingWorldYOffset;
    public float YMultiplier = 100f;

    [Header("RenderTexture Proxy")]
    public int RenderTextureWidth = 512;
    public int RenderTextureHeight = 512;
    public float RenderCameraOrthographicSize = 5f;
    public float ProxyWorldHeight = 10f;
    public Vector3 ProxyWorldOffset = Vector3.zero;

    [Header("Lifetime")]
    public float LifeTime = 2f;
    public bool AutoDestroy = true;

    public BattleVfxEntry ToEntry(GameObject prefab)
    {
        return new BattleVfxEntry
        {
            prefab = prefab,
            flipType = FlipType,
            renderMode = RenderMode,
            sfx = new BattleVfxSfxEntry
            {
                playSfx = PlaySfx,
                sfxId = SfxId,
                delay = Mathf.Max(0f, SfxDelay),
                volumeMultiplier = Mathf.Max(0f, SfxVolumeMultiplier),
                routeEmbeddedAudioSourcesThroughAudioManager =
                    RouteEmbeddedAudioSourcesThroughAudioManager,
                removeEmbeddedAudioSources = RemoveEmbeddedAudioSources
            },
            renderTextureWidth = Mathf.Max(1, RenderTextureWidth),
            renderTextureHeight = Mathf.Max(1, RenderTextureHeight),
            renderCameraOrthographicSize = Mathf.Max(0.01f, RenderCameraOrthographicSize),
            proxyWorldHeight = Mathf.Max(0.01f, ProxyWorldHeight),
            proxyWorldOffset = ProxyWorldOffset,
            proxySortingLayerName = SortingLayerName,
            proxySortingOrderOffset = SortingOrderOffset,
            proxySortingWorldYOffset = SortingWorldYOffset,
            proxyYMultiplier = Mathf.Max(0.01f, YMultiplier)
        };
    }

    public float SafeLifeTime()
    {
        return Mathf.Max(0.01f, LifeTime);
    }

    public Vector3 SafeScaleMultiplier()
    {
        return new Vector3(
            Mathf.Approximately(ScaleMultiplier.x, 0f) ? 1f : ScaleMultiplier.x,
            Mathf.Approximately(ScaleMultiplier.y, 0f) ? 1f : ScaleMultiplier.y,
            Mathf.Approximately(ScaleMultiplier.z, 0f) ? 1f : ScaleMultiplier.z);
    }
}
