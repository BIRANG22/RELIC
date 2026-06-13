using UnityEngine;

public class BattleUnitFacing : MonoBehaviour
{
    [Header("Sprite Root")]
    [SerializeField] private Transform spriteRoot;

    [Header("Facing")]
    [SerializeField] private bool startFacingRight = true;
    [SerializeField] private float minHorizontalDelta = 0.01f;

    private bool isFacingRight = true;
    private bool initialized;

    public bool IsFacingRight => isFacingRight;

    private void Awake()
    {
        InitializeIfNeeded();

        if (!startFacingRight)
            FlipOnce();
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

        initialized = true;
    }

    public void SetFacingStateOnly(bool faceRight)
    {
        isFacingRight = faceRight;
    }

    public void FlipOnce()
    {
        InitializeIfNeeded();

        Vector3 scale = spriteRoot.localScale;
        scale.x *= -1f;
        spriteRoot.localScale = scale;

        isFacingRight = !isFacingRight;
    }

    public void FaceRight(bool faceRight)
    {
        InitializeIfNeeded();

        if (isFacingRight == faceRight)
            return;

        FlipOnce();
    }

    public void FaceByMoveOffset(Vector2Int moveOffset)
    {
        if (moveOffset.x > 0)
            FaceRight(true);
        else if (moveOffset.x < 0)
            FaceRight(false);
    }

    public void FaceByWorldTarget(Vector3 targetPosition)
    {
        float deltaX = targetPosition.x - transform.position.x;

        if (Mathf.Abs(deltaX) < minHorizontalDelta)
            return;

        FaceRight(deltaX > 0f);
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