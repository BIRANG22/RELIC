using UnityEngine;

[DisallowMultipleComponent]
internal sealed class SteamworksCallbackPump : MonoBehaviour
{
    private const string PumpObjectName = "[SteamworksCallbackPump]";

    private static SteamworksCallbackPump instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateAfterSceneLoad()
    {
        EnsureRunning();
    }

    internal static void EnsureRunning()
    {
        if (instance != null)
            return;

        SteamworksCallbackPump existing = FindFirstObjectByType<SteamworksCallbackPump>(
            FindObjectsInactive.Include);

        if (existing != null)
        {
            instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            return;
        }

        GameObject pumpObject = new GameObject(PumpObjectName);
        instance = pumpObject.AddComponent<SteamworksCallbackPump>();
        DontDestroyOnLoad(pumpObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        SteamLobbyInviteController.RunSteamCallbacksIfReady();
    }
}
