using UnityEngine;

public class FakePointLight : MonoBehaviour
{
    public Renderer targetRenderer;
    public float radius = 5f;
    public Color lightColor = Color.red;

    private Material runtimeMaterial;

    void Start()
    {
        if (targetRenderer != null)
        {
            runtimeMaterial = targetRenderer.material; // 한 번만 생성
        }
    }

    void Update()
    {
        if (runtimeMaterial == null) return;

        runtimeMaterial.SetVector("_FakePointLightPos", transform.position);
        runtimeMaterial.SetFloat("_FakePointLightRadius", radius);
        runtimeMaterial.SetColor("_FakePointLightColor", lightColor);
    }
}