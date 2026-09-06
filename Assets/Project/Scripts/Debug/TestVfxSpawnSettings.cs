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
    public BattleVfxProxyBlendMode ProxyBlendMode = BattleVfxProxyBlendMode.Additive;

    [Header("Layer")]
    public string ObjectLayerName = "VFX";
    public string SortingLayerName = "Unit";
    public int SortingOrderOffset;
    public float SortingWorldYOffset;
    public float YMultiplier = 100f;

    [Header("RenderTexture Proxy")]
    public bool ScaleDirectWorldRendererToProxyHeight;
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
            proxyBlendMode = ProxyBlendMode,
            scaleDirectWorldRendererToProxyHeight = ScaleDirectWorldRendererToProxyHeight,
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
