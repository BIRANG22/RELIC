using System.Collections.Generic;
using UnityEngine;

public enum BattleVfxRenderMode
{
    IndividualWorldRenderTexture,
    SharedRenderTextureOverlay,
    DirectWorldRenderer
}

public enum BattleVfxProxyBlendMode
{
    Additive,
    Alpha
}

public enum BattleVfxSpawnAnchor
{
    Caster,
    SelectedGrid
}

[System.Serializable]
public class BattleVfxSfxEntry
{
    [Header("SFX ID")]
    public bool playSfx;
    [SoundId(SoundCategory.SkillSfx)]
    public string sfxId;
    [Min(0f)] public float delay;
    [Min(0f)] public float volumeMultiplier = 1f;

    [Header("Embedded AudioSource Migration")]
    public bool routeEmbeddedAudioSourcesThroughAudioManager = false;
    public bool removeEmbeddedAudioSources = true;

    [Header("Additional SFX")]
    public List<BattleVfxAdditionalSfxEntry> additionalSfx = new();

    public static BattleVfxSfxEntry CopyFrom(BattleVfxSfxEntry source)
    {
        if (source == null)
            return new BattleVfxSfxEntry();

        return new BattleVfxSfxEntry
        {
            playSfx = source.playSfx,
            sfxId = source.sfxId,
            delay = source.delay,
            volumeMultiplier = source.volumeMultiplier,
            routeEmbeddedAudioSourcesThroughAudioManager =
                source.routeEmbeddedAudioSourcesThroughAudioManager,
            removeEmbeddedAudioSources = source.removeEmbeddedAudioSources,
            additionalSfx = CopyAdditionalSfx(source.additionalSfx)
        };
    }

    private static List<BattleVfxAdditionalSfxEntry> CopyAdditionalSfx(
        IReadOnlyList<BattleVfxAdditionalSfxEntry> source)
    {
        List<BattleVfxAdditionalSfxEntry> copy = new();

        if (source == null)
            return copy;

        for (int i = 0; i < source.Count; i++)
            copy.Add(BattleVfxAdditionalSfxEntry.CopyFrom(source[i]));

        return copy;
    }
}

[System.Serializable]
public class BattleVfxAdditionalSfxEntry
{
    [SoundId(SoundCategory.SkillSfx)]
    public string sfxId;
    [Min(0f)] public float delay;
    [Min(0f)] public float volumeMultiplier = 1f;

    public static BattleVfxAdditionalSfxEntry CopyFrom(BattleVfxAdditionalSfxEntry source)
    {
        if (source == null)
            return new BattleVfxAdditionalSfxEntry();

        return new BattleVfxAdditionalSfxEntry
        {
            sfxId = source.sfxId,
            delay = source.delay,
            volumeMultiplier = source.volumeMultiplier
        };
    }
}

[System.Serializable]
public class BattleVfxEntry
{
    public GameObject prefab;
    public VfxFlipType flipType;

    [Header("SFX")]
    public BattleVfxSfxEntry sfx = new();

    [Header("Render Routing")]
    public BattleVfxRenderMode renderMode = BattleVfxRenderMode.IndividualWorldRenderTexture;
    public BattleVfxProxyBlendMode proxyBlendMode = BattleVfxProxyBlendMode.Additive;
    public BattleVfxSpawnAnchor spawnAnchor = BattleVfxSpawnAnchor.Caster;

    [Header("Direct World Renderer")]
    public bool scaleDirectWorldRendererToProxyHeight;

    [Header("Individual World RenderTexture")]
    [Min(1)] public int renderTextureWidth = 512;
    [Min(1)] public int renderTextureHeight = 512;
    [Min(0.01f)] public float renderCameraOrthographicSize = 5f;
    [Min(0.01f)] public float proxyWorldHeight = 10f;
    public Vector3 proxyWorldOffset = Vector3.zero;
    public string proxySortingLayerName = "Unit";
    public int proxySortingOrderOffset = 0;
    public float proxySortingWorldYOffset = 0f;
    [Min(0.01f)] public float proxyYMultiplier = 100f;
}
