using UnityEngine;

public class BattleUnitFacing : MonoBehaviour
{
    [Header("Sprite Root")]
    [SerializeField] private Transform spriteRoot;

    [Header("Linked Facing Roots")]
    [Tooltip("spriteRoot와 같은 방향으로 함께 뒤집을 오브젝트들입니다. 몬스터 본체 외에 그림자, 이펙트 루트 등 2개를 연결해두면 같이 facing됩니다.")]
    [SerializeField] private Transform[] linkedFacingRoots;

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

    public void InitializeIfNeeded()
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

        FlipTransform(spriteRoot);
        FlipLinkedFacingRoots();

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

    private void FlipLinkedFacingRoots()
    {
        if (linkedFacingRoots == null)
            return;

        for (int i = 0; i < linkedFacingRoots.Length; i++)
            FlipTransform(linkedFacingRoots[i]);
    }

    private void FlipTransform(Transform target)
    {
        if (target == null)
            return;

        Vector3 scale = target.localScale;
        scale.x *= -1f;
        target.localScale = scale;
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
