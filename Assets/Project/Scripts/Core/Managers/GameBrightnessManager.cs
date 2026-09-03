using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public sealed class GameBrightnessManager : MonoBehaviour
{
    private static GameBrightnessManager instance;

    [SerializeField] private Volume volume;

    private ColorAdjustments colorAdjustments;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplySavedBrightness();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    public static float BrightnessToPostExposure(float brightness)
    {
        return (Mathf.Clamp01(brightness) - 0.5f) * 4f;
    }

    public static void ApplySavedBrightness()
    {
        float brightness = Settings.Instance != null
            ? Settings.Instance.Brightness
            : 0.5f;

        ApplyBrightness(brightness);
    }

    public static void ApplyBrightness(float brightness)
    {
        EnsureInstance();

        if (instance != null)
            instance.Apply(brightness);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        GameBrightnessManager existing = FindFirstObjectByType<GameBrightnessManager>(FindObjectsInactive.Include);
        if (existing != null)
        {
            instance = existing;
            return;
        }

        GameObject gameObject = new("GameBrightnessManager");
        instance = gameObject.AddComponent<GameBrightnessManager>();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        volume = null;
        colorAdjustments = null;
        ApplySavedBrightness();
    }

    private void Apply(float brightness)
    {
        if (!TryResolveColorAdjustments())
            return;

        colorAdjustments.postExposure.overrideState = true;
        colorAdjustments.postExposure.value = BrightnessToPostExposure(brightness);
    }

    private bool TryResolveColorAdjustments()
    {
        if (colorAdjustments != null)
            return true;

        if (volume == null)
            volume = FindFirstObjectByType<Volume>(FindObjectsInactive.Exclude);

        if (volume == null || volume.profile == null)
            return false;

        return volume.profile.TryGet(out colorAdjustments);
    }
}
