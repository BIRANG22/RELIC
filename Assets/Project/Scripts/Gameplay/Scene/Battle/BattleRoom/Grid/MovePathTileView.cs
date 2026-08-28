using UnityEngine;

public enum MovePathTileKind
{
    Straight,
    Corner,
    CornerEnd,
    End
}

[RequireComponent(typeof(SpriteRenderer))]
public class MovePathTileView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite straightSprite;
    [SerializeField] private Sprite cornerSprite;
    [SerializeField] private Sprite cornerEndSprite;
    [SerializeField] private Sprite endSprite;
    [SerializeField] private float appliedRotationY;
    [SerializeField] private float appliedRotationZ;

    public MovePathTileKind Kind { get; private set; }
    public float AppliedRotationY => appliedRotationY;
    public float AppliedRotationZ => appliedRotationZ;

    private void Awake()
    {
        EnsureRenderer();
    }

    public void ConfigureSprites(Sprite straight, Sprite corner, Sprite cornerEnd, Sprite end)
    {
        straightSprite = straight;
        cornerSprite = corner;
        cornerEndSprite = cornerEnd;
        endSprite = end;
    }

    public void ConfigureSprites(Sprite straight, Sprite corner, Sprite end)
    {
        ConfigureSprites(straight, corner, null, end);
    }

    public void Apply(MovePathTileKind kind, float rotationZ)
    {
        Apply(kind, 0f, rotationZ);
    }

    public void Apply(MovePathTileKind kind, float rotationY, float rotationZ)
    {
        EnsureRenderer();

        Kind = kind;
        appliedRotationY = NormalizeRotation(rotationY);
        appliedRotationZ = NormalizeRotation(rotationZ);

        if (spriteRenderer != null)
            spriteRenderer.sprite = GetSprite(kind);

        transform.localEulerAngles = new Vector3(0f, appliedRotationY, appliedRotationZ);
    }

    public void ApplySorting(string sortingLayerName, int sortingOrder)
    {
        EnsureRenderer();

        if (spriteRenderer == null)
            return;

        if (!string.IsNullOrWhiteSpace(sortingLayerName))
            spriteRenderer.sortingLayerName = sortingLayerName;

        spriteRenderer.sortingOrder = sortingOrder;
    }

    private Sprite GetSprite(MovePathTileKind kind)
    {
        return kind switch
        {
            MovePathTileKind.Corner => cornerSprite,
            MovePathTileKind.CornerEnd => cornerEndSprite != null ? cornerEndSprite : endSprite,
            MovePathTileKind.End => endSprite,
            _ => straightSprite
        };
    }

    private void EnsureRenderer()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private static float NormalizeRotation(float rotationZ)
    {
        float normalized = rotationZ % 360f;

        if (normalized < 0f)
            normalized += 360f;

        return normalized;
    }
}
