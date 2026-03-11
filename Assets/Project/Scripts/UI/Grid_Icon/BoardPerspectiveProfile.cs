using UnityEngine;

[System.Serializable]
public class BoardPerspectiveProfile
{
    [System.Serializable]
    public struct QuadWarpProfile
    {
        public Vector2 topLeft;
        public Vector2 topRight;
        public Vector2 bottomRight;
        public Vector2 bottomLeft;
    }

    [Header("Board Size")]
    public float boardWidth = 1200f;
    public float boardHeight = 420f;
    public float boardCenterY = 0f;

    [Header("Perspective Control")]
    [Range(0f, 1f)] public float perspectiveAmount = 0.5f;

    [Header("Perspective Range")]
    public float topInsetMin = 0.00f;
    public float topInsetMax = 0.28f;

    public float topDropMin = 0.00f;
    public float topDropMax = 0.18f;

    public float bottomExpandMin = 0.00f;
    public float bottomExpandMax = 0.04f;

    public float bottomLiftMin = 0.00f;
    public float bottomLiftMax = 0.10f;

    [Header("Whole Board Quad Warp (Runtime/Preview)")]
    public QuadWarpProfile boardQuad;

    [Header("Grid")]
    public int columns = 6;
    public int rows = 2;

    [Header("Cell Padding")]
    [Range(0f, 0.45f)] public float cellPaddingX = 0f;
    [Range(0f, 0.45f)] public float cellPaddingY = 0f;

    [Header("Frame")]
    public float frameScale = 1.0f;

    [Header("Icon")]
    public float iconScaleFar = 0.72f;
    public float iconScaleNear = 0.82f;
    public float iconYOffsetFar = 6f;
    public float iconYOffsetNear = 14f;

    [Header("Icon Perspective")]
    [Range(0f, 1f)] public float iconPerspectiveAmount = 0.20f;
    public float iconWarpStrengthMin = 0.00f;
    public float iconWarpStrengthMax = 0.80f;

    [Header("Row Motion")]
    public float rowRaiseYOffset = 45f;
    public float rowRaiseScale = 1.04f;
    public float rowRaiseXSpread = 25f;

    [Header("Slot Motion")]
    public float slotRaiseYOffset = 70f;
    public float slotRaiseScale = 1.12f;
    public float slotRaiseXOffset = 0f;

    [Header("Slot Visual Bias")]
    public float slotRaiseIconYOffset = 12f;
    public float slotRaiseIconScale = 1.05f;
}