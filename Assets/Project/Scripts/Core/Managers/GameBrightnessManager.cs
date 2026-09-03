using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public sealed class GameBrightnessManager : MonoBehaviour
{
    private static GameBrightnessManager instance;

    [Header("Runtime Brightness Override")]
    [SerializeField] private float runtimeVolumePriority = 10000f;

    private Volume volume;
    private VolumeProfile runtimeProfile;
    private VolumeProfile runtimeProfileSource;
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
        if (instance != this)
            return;

        instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;

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
            return;
        }

        GameObject gameObject = new("GameBrightnessManager");
        instance = gameObject.AddComponent<GameBrightnessManager>();
    }

    private void Apply(float brightness)
    {
        EnableMainCameraPostProcessing();

        if (!TryResolveColorAdjustments())
            return;

        colorAdjustments.postExposure.overrideState = true;
        colorAdjustments.postExposure.value = BrightnessToPostExposure(brightness);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        volume = null;
        colorAdjustments = null;
        runtimeProfile = null;
        runtimeProfileSource = null;
        ApplySavedBrightness();
    }

    private bool TryResolveColorAdjustments()
    {
        if (colorAdjustments != null)
            return true;

        ResolveVolume();

        if (volume == null)
            return false;

        VolumeProfile profile = GetOrCreateRuntimeProfile(volume);
        if (profile == null)
            return false;

        if (!profile.TryGet(out colorAdjustments) || colorAdjustments == null)
            colorAdjustments = profile.Add<ColorAdjustments>(true);

        if (colorAdjustments == null)
            return false;

        colorAdjustments.active = true;
        return true;
    }

    private void ResolveVolume()
    {
        if (volume != null && volume.isActiveAndEnabled && volume.isGlobal)
            return;

        Scene activeScene = SceneManager.GetActiveScene();
        Volume[] volumes = FindObjectsByType<Volume>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Volume bestVolume = null;

        for (int i = 0; i < volumes.Length; i++)
        {
            Volume candidate = volumes[i];
            if (candidate == null || !candidate.isGlobal || candidate.gameObject.scene != activeScene)
                continue;

            if (bestVolume == null || candidate.priority > bestVolume.priority)
                bestVolume = candidate;
        }

        volume = bestVolume;
    }

    private VolumeProfile GetOrCreateRuntimeProfile(Volume targetVolume)
    {
        VolumeProfile sourceProfile = targetVolume.sharedProfile;

        if (runtimeProfile != null && runtimeProfileSource == sourceProfile)
            return runtimeProfile;

        if (runtimeProfile != null)
            Destroy(runtimeProfile);

        runtimeProfileSource = sourceProfile;
        runtimeProfile = sourceProfile != null
            ? Instantiate(sourceProfile)
            : ScriptableObject.CreateInstance<VolumeProfile>();

        runtimeProfile.name = "RuntimeBrightnessProfile";
        targetVolume.profile = runtimeProfile;
        return runtimeProfile;
    }

    private static void EnableMainCameraPostProcessing()
    {
        Camera targetCamera = Camera.main;
        if (targetCamera == null)
            targetCamera = FindFirstObjectByType<Camera>(FindObjectsInactive.Exclude);

        if (targetCamera == null)
            return;

        if (targetCamera.TryGetComponent(out UniversalAdditionalCameraData cameraData))
            cameraData.renderPostProcessing = true;
    }

    private void OnValidate()
    {
        if (runtimeVolumePriority < 0f)
            runtimeVolumePriority = 0f;
    }
}
