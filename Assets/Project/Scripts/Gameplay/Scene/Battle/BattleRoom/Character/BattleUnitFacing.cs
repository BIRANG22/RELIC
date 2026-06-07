using UnityEngine;

public class BattleUnitFacing : MonoBehaviour
{
    [Header("Sprite Root")]
    [SerializeField] private Transform spriteRoot;

    [Header("Facing")]
    [SerializeField] private bool defaultFacingRight = true;
    [SerializeField] private bool startFacingRight = true;
    [SerializeField] private float minHorizontalDelta = 0.01f;

    private bool isFacingRight = true;
    private float originalAbsScaleX = 1f;
    private bool initialized;

    public bool IsFacingRight => isFacingRight;

    private void Awake()
    {
        InitializeIfNeeded();
        FaceRight(startFacingRight);
    }

    private void InitializeIfNeeded()
    {
        if (initialized)
            return;

        if (spriteRoot == null)
        {
            Transform found = transform.Find("SpriteRoot");
            spriteRoot = found != null ? found : transform;
        }

        originalAbsScaleX = Mathf.Abs(spriteRoot.localScale.x);

        if (originalAbsScaleX <= 0.0001f)
            originalAbsScaleX = 1f;

        initialized = true;
    }

    public void FaceByWorldTarget(Vector3 targetPosition)
    {
        float deltaX = targetPosition.x - transform.position.x;

        if (Mathf.Abs(deltaX) < minHorizontalDelta)
            return;

        FaceRight(deltaX > 0f);
    }

    public void FaceLeft()
    {
        FaceRight(false);
    }

    public void FaceRight()
    {
        FaceRight(true);
    }

    public void FaceRight(bool faceRight)
    {
        InitializeIfNeeded();

        isFacingRight = faceRight;

        Vector3 scale = spriteRoot.localScale;

        bool usePositiveScale = defaultFacingRight
            ? faceRight
            : !faceRight;

        scale.x = usePositiveScale
            ? originalAbsScaleX
            : -originalAbsScaleX;

        spriteRoot.localScale = scale;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (spriteRoot == null)
        {
            Transform found = transform.Find("SpriteRoot");
            if (found != null)
                spriteRoot = found;
        }
    }
#endif
}