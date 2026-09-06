using UnityEngine;

public class LightPulse : MonoBehaviour
{
    [Header("Scale")]
    [SerializeField] private float minScale = 0.7f;
    [SerializeField] private float maxScale = 1.3f;

    [Header("Speed")]
    [SerializeField] private float speed = 0.5f;

    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.time * speed * Mathf.PI * 2f) + 1f) * 0.5f;
        float scale = Mathf.Lerp(minScale, maxScale, t);

        transform.localScale = baseScale * scale;
    }
}