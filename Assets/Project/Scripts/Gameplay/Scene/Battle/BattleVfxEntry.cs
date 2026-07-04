using UnityEngine;

public enum BattleVfxRenderMode
{
    IndividualWorldRenderTexture,
    SharedRenderTextureOverlay,
    DirectWorldRenderer
}

[System.Serializable]
public class BattleVfxEntry
{
    public GameObject prefab;
    public VfxFlipType flipType;

    [Header("Render Routing")]
    public BattleVfxRenderMode renderMode = BattleVfxRenderMode.IndividualWorldRenderTexture;

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
