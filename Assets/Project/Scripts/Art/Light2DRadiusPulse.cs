using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Light2DRadiusPulse : MonoBehaviour
{
    [SerializeField] private Light2D targetLight;

    [Header("Outer Radius")]
    [SerializeField] private float radiusA = 1f;
    [SerializeField] private float radiusB = 2f;

    [Header("Speed")]
    [SerializeField] private float speed = 1f;

    private void Reset()
    {
        targetLight = GetComponent<Light2D>();
    }

    private void Awake()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light2D>();
    }

    private void Update()
    {
        if (targetLight == null)
            return;

        float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
        targetLight.pointLightOuterRadius = Mathf.Lerp(radiusA, radiusB, t);
    }
}
