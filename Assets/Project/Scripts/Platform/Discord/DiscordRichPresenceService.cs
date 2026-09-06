using System;
using Discord.Sdk;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DiscordRichPresenceService : MonoBehaviour
{
    public const ulong ApplicationId = 1533104947875549325UL;
    private const float RefreshIntervalSeconds = 5f;

    private static DiscordRichPresenceService instance;

    private Client client;
    private long startUnixSeconds;
    private float nextRefreshTime;
    private bool isShuttingDown;

    public DiscordPresenceStatus Status { get; private set; } =
        DiscordPresenceStatus.Initializing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateService()
    {
        if (instance != null)
            return;

        GameObject root = new("Discord Rich Presence");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<DiscordRichPresenceService>();
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
        startUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        InitializeClient();
    }

    private void Update()
    {
        if (isShuttingDown ||
            Status == DiscordPresenceStatus.Error ||
            Time.unscaledTime < nextRefreshTime)
            return;

        RefreshPresence();
    }

    private void OnActiveSceneChanged(Scene previous, Scene current)
    {
        RefreshPresence();
    }

    private void InitializeClient()
    {
        if (!DiscordPresencePolicy.TryValidateApplicationId(ApplicationId, out string error))
        {
            Status = DiscordPresenceStatus.Error;
            Debug.LogError($"[DiscordPresence] {error}");
            return;
        }

        try
        {
            client = new Client();
            client.SetApplicationId(ApplicationId);
            Status = DiscordPresenceStatus.Initializing;
            Debug.Log($"[DiscordPresence] SDK initialized. ApplicationId:{ApplicationId}");
            RefreshPresence();
        }
        catch (Exception exception)
        {
            Status = DiscordPresenceStatus.Error;
            Debug.LogWarning($"[DiscordPresence] SDK initialization failed: {exception.Message}");
            DisposeClient();
        }
    }

    private void RefreshPresence()
    {
        nextRefreshTime = Time.unscaledTime + RefreshIntervalSeconds;

        if (client == null || isShuttingDown)
            return;

        try
        {
            DataManager dataManager = DataManager.Instance;
            MapRuntimeData map = dataManager?.MapRuntimeStore?.Get();
            PartyRuntimeStore party = dataManager?.PartyRuntimeStore;
            CharacterDatabase characters = dataManager?.CharacterDatabase;
            string sceneName = SceneManager.GetActiveScene().name;

            DiscordPresenceSnapshot snapshot = DiscordPresenceSnapshotBuilder.Build(
                sceneName,
                map,
                party,
                characters,
                startUnixSeconds);

            using Activity activity = new();
            activity.SetType(ActivityTypes.Playing);
            activity.SetDetails(snapshot.Details);
            activity.SetState(snapshot.State);

            using ActivityTimestamps timestamps = new();
            timestamps.SetStart((ulong)Math.Max(0L, snapshot.StartUnixSeconds));
            activity.SetTimestamps(timestamps);

            client.UpdateRichPresence(activity, OnPresenceUpdated);
        }
        catch (Exception exception)
        {
            Status = DiscordPresenceStatus.Error;
            Debug.LogWarning($"[DiscordPresence] Presence update threw: {exception.Message}");
        }
    }

    private void OnPresenceUpdated(ClientResult result)
    {
        DiscordPresencePolicy.InvokeSafely(
            () => ApplyPresenceResult(result),
            exception =>
            {
                Status = DiscordPresenceStatus.Error;
                Debug.LogWarning(
                    $"[DiscordPresence] SDK callback failed: {exception.Message}");
            });
    }

    private void ApplyPresenceResult(ClientResult result)
    {
        bool successful = result != null && result.Successful();
        DiscordPresenceStatus previous = Status;
        Status = DiscordPresencePolicy.FromUpdateResult(successful);

        if (successful)
        {
            if (previous != DiscordPresenceStatus.Ready)
                Debug.Log("[DiscordPresence] Ready - Rich Presence is active.");
            return;
        }

        ErrorType errorType = result != null
            ? result.Type()
            : ErrorType.ClientNotReady;

        if (DiscordPresencePolicy.IsExpectedClientUnavailable(errorType))
            return;

        string error = result != null ? result.Error() : "No SDK result";
        Status = DiscordPresenceStatus.Error;
        Debug.LogWarning(
            $"[DiscordPresence] Presence update failed. Type:{errorType}, Error:{error}");
    }

    private void OnApplicationQuit()
    {
        isShuttingDown = true;
        ClearAndDispose();
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;

        if (instance == this)
            instance = null;

        if (!isShuttingDown)
            ClearAndDispose();
    }

    private void ClearAndDispose()
    {
        if (client != null)
        {
            DiscordPresencePolicy.InvokeSafely(
                client.ClearRichPresence,
                exception => Debug.LogWarning(
                    $"[DiscordPresence] Presence cleanup failed: {exception.Message}"));
        }

        DisposeClient();
    }

    private void DisposeClient()
    {
        Client clientToDispose = client;
        client = null;

        if (clientToDispose == null)
            return;

        DiscordPresencePolicy.InvokeSafely(
            clientToDispose.Dispose,
            exception => Debug.LogWarning(
                $"[DiscordPresence] SDK dispose failed: {exception.Message}"));
    }
}
