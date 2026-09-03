using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class GameBrightnessManager : MonoBehaviour
{
    private static GameBrightnessManager instance;

    [Header("Runtime Brightness Override")]
    [SerializeField] private float runtimeVolumePriority = 10000f;

    private Volume runtimeVolume;
    private VolumeProfile runtimeProfile;
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

        EnsureRuntimeVolumeOverride();
        ApplySavedBrightness();
    }

    private void OnDestroy()
    {
        if (instance != this)
            return;

        instance = null;

        if (runtimeProfile != null)
        {
            Destroy(runtimeProfile);
            runtimeProfile = null;
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
            instance.EnsureRuntimeVolumeOverride();
            return;
        }

        GameObject gameObject = new("GameBrightnessManager");
        instance = gameObject.AddComponent<GameBrightnessManager>();
    }

    private void Apply(float brightness)
    {
        EnsureRuntimeVolumeOverride();

        if (colorAdjustments == null)
            return;

        colorAdjustments.postExposure.overrideState = true;
        colorAdjustments.postExposure.value = BrightnessToPostExposure(brightness);
    }

    private void EnsureRuntimeVolumeOverride()
    {
        if (runtimeVolume == null)
        {
            runtimeVolume = GetComponent<Volume>();
            if (runtimeVolume == null)
                runtimeVolume = gameObject.AddComponent<Volume>();
        }

        runtimeVolume.isGlobal = true;
        runtimeVolume.weight = 1f;
        runtimeVolume.priority = runtimeVolumePriority;

        if (runtimeProfile == null)
        {
            runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            runtimeProfile.name = "RuntimeBrightnessProfile";
        }

        if (runtimeVolume.sharedProfile != runtimeProfile)
            runtimeVolume.sharedProfile = runtimeProfile;

        if (!runtimeProfile.TryGet(out colorAdjustments) || colorAdjustments == null)
            colorAdjustments = runtimeProfile.Add<ColorAdjustments>(true);

        colorAdjustments.active = true;
        colorAdjustments.postExposure.overrideState = true;
    }
}
