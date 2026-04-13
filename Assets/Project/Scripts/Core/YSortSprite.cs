using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class YSortSprite : MonoBehaviour
{
    private SpriteRenderer sr;

    public int sortingOrderOffset = 0;
    public float yMultiplier = 100f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        sr.sortingOrder = (int)(-transform.position.y * yMultiplier) + sortingOrderOffset;
    }
}