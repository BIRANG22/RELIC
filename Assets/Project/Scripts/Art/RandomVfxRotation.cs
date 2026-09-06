using UnityEngine;

/// <summary>
/// Applies one random local rotation whenever this GameObject is enabled.
/// Child objects inherit the same parent rotation automatically.
/// </summary>
public class RandomVfxRotation : MonoBehaviour
{
    [Header("Random Rotation Range")]
    [SerializeField] private Vector2 xRange = new Vector2(40f, 140f);
    [SerializeField] private Vector2 yRange = new Vector2(0f, 180f);
    [SerializeField] private Vector2 zRange = new Vector2(0f, 180f);

    private Quaternion initialLocalRotation;

    private void Awake()
    {
        initialLocalRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        ApplyRandomRotation();
    }

    [ContextMenu("Apply Random Rotation")]
    public void ApplyRandomRotation()
    {
        float x = Random.Range(xRange.x, xRange.y);
        float y = Random.Range(yRange.x, yRange.y);
        float z = Random.Range(zRange.x, zRange.y);

        transform.localRotation = initialLocalRotation * Quaternion.Euler(x, y, z);
    }
}
