using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// WebGL에서 지원되지 않는 Visual Effect Graph를 미리 지정한 Shuriken VFX로 대체합니다.
/// 전투 결과와 무관한 프레젠테이션 전용 컴포넌트입니다.
/// </summary>
public sealed class WebGlVfxFallback : MonoBehaviour
{
    [SerializeField] private GameObject webGlFallbackPrefab;

    private GameObject runtimeFallback;

    private void Awake()
    {
        ApplyForPlatform(Application.platform);
    }

    private void OnEnable()
    {
        ApplyForPlatform(Application.platform);
    }

    private void OnDestroy()
    {
        if (runtimeFallback != null)
        {
            Destroy(runtimeFallback);
        }
    }

    public static bool UsesFallback(RuntimePlatform platform)
    {
        return platform == RuntimePlatform.WebGLPlayer;
    }

    public void ApplyForPlatform(RuntimePlatform platform)
    {
        bool useFallback = UsesFallback(platform);
        SetVisualEffectsEnabled(!useFallback);

        if (!useFallback || webGlFallbackPrefab == null)
        {
            if (runtimeFallback != null)
            {
                runtimeFallback.SetActive(false);
            }

            return;
        }

        EnsureFallback();
        runtimeFallback.SetActive(true);
    }

    private void SetVisualEffectsEnabled(bool enabled)
    {
        VisualEffect[] visualEffects = GetComponentsInChildren<VisualEffect>(true);
        for (int i = 0; i < visualEffects.Length; i++)
        {
            if (visualEffects[i] != null)
            {
                visualEffects[i].enabled = enabled;
            }
        }
    }

    private void EnsureFallback()
    {
        if (runtimeFallback != null)
        {
            return;
        }

        runtimeFallback = Instantiate(webGlFallbackPrefab, transform, false);
        runtimeFallback.name = webGlFallbackPrefab.name + "_WebGlFallback";
    }
}
