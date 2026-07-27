using System.Text;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if STEAMWORKS_NET
using Steamworks;
#endif

[DisallowMultipleComponent]
public class SteamLobbyInviteController : MonoBehaviour
{
    private const int DefaultMaxMembers = 3;
    private const float MinSyncInterval = 0.1f;

    [Header("Lobby")]
    [SerializeField, Range(1, 250)] private int maxMembers = DefaultMaxMembers;
    [SerializeField] private bool createFriendsOnlyLobby = true;
    [SerializeField] private bool openInviteDialogAfterLobbyCreate = true;
    [SerializeField] private bool applyLobbyMembersToPartyRuntime = true;
    [SerializeField, Min(MinSyncInterval)] private float memberDataSyncInterval = 0.5f;

    [Header("Button")]
    [SerializeField] private Button inviteButton;

    [Header("Status UI")]
    [SerializeField] private bool createStatusPanelIfMissing = true;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text membersText;

    private static bool steamApiInitialized;

    private readonly string[] lastSyncedCharacterIds = new string[DefaultMaxMembers];
    private bool didInitializeSteamApi;
    private bool isCreatingLobby;
    private bool pendingInviteDialog;
    private float nextMemberSyncTime;
    private string lastStatus = "Steam lobby idle.";

#if STEAMWORKS_NET
    private CSteamID currentLobbyId;
    private CallResult<LobbyCreated_t> lobbyCreatedCallResult;
    private CallResult<LobbyEnter_t> lobbyEnterCallResult;
    private Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    private Callback<LobbyChatUpdate_t> lobbyChatUpdate;
    private Callback<LobbyDataUpdate_t> lobbyDataUpdate;
#endif

    private void Awake()
    {
        BindReferences();
        CreateStatusPanelIfNeeded();
        InitializeSteam();
        RefreshStatusPanel();
    }

    private void Update()
    {
#if STEAMWORKS_NET
        if (steamApiInitialized)
        {
            SteamAPI.RunCallbacks();
            SyncLocalMemberDataIfNeeded(false);
        }
#endif
    }

    private void OnDestroy()
    {
#if STEAMWORKS_NET
        if (didInitializeSteamApi)
        {
            SteamAPI.Shutdown();
            steamApiInitialized = false;
            didInitializeSteamApi = false;
        }
#endif
    }

    public void OpenInviteFlow()
    {
#if STEAMWORKS_NET
        if (!EnsureSteamReady())
            return;

        if (HasCurrentLobby())
        {
            OpenInviteDialog();
            return;
        }

        pendingInviteDialog = openInviteDialogAfterLobbyCreate;
        CreateLobby();
#else
        SetStatus("Steamworks.NET package is not resolved yet.");
#endif
    }

    private void BindReferences()
    {
        if (inviteButton == null)
            inviteButton = GetComponent<Button>();
    }

    private void InitializeSteam()
    {
#if STEAMWORKS_NET
        if (steamApiInitialized)
        {
            RegisterCallbacks();
            SetStatus("Steam API already initialized.");
            ProcessLaunchCommandLine();
            return;
        }

        if (!SteamAPI.IsSteamRunning())
        {
            SetStatus("Steam client is not running.");
            return;
        }

        try
        {
            steamApiInitialized = SteamAPI.Init();
            didInitializeSteamApi = steamApiInitialized;
        }
        catch (System.Exception exception)
        {
            Debug.LogError("[SteamLobbyInviteController] SteamAPI.Init failed. " + exception, this);
            steamApiInitialized = false;
        }

        if (!steamApiInitialized)
        {
            SetStatus("Steam API init failed. Check Steam client and steam_appid.txt.");
            return;
        }

        RegisterCallbacks();
        SetStatus("Steam ready: " + SteamFriends.GetPersonaName());
        ProcessLaunchCommandLine();
#else
        SetStatus("Steamworks.NET package is not installed or Unity has not resolved it.");
#endif
    }

#if STEAMWORKS_NET
    private bool EnsureSteamReady()
    {
        if (steamApiInitialized)
            return true;

        InitializeSteam();
        RefreshStatusPanel();
        return steamApiInitialized;
    }

    private void RegisterCallbacks()
    {
        if (lobbyCreatedCallResult == null)
            lobbyCreatedCallResult = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);

        if (lobbyEnterCallResult == null)
            lobbyEnterCallResult = CallResult<LobbyEnter_t>.Create(OnLobbyEntered);

        if (gameLobbyJoinRequested == null)
            gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);

        if (lobbyChatUpdate == null)
            lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdated);

        if (lobbyDataUpdate == null)
            lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdated);
    }

    private void CreateLobby()
    {
        if (isCreatingLobby)
        {
            SetStatus("Steam lobby is already being created.");
            return;
        }

        isCreatingLobby = true;
        ELobbyType lobbyType = createFriendsOnlyLobby
            ? ELobbyType.k_ELobbyTypeFriendsOnly
            : ELobbyType.k_ELobbyTypePrivate;

        SteamAPICall_t call = SteamMatchmaking.CreateLobby(lobbyType, Mathf.Clamp(maxMembers, 1, 250));
        lobbyCreatedCallResult.Set(call);
        SetStatus("Creating Steam lobby...");
    }

    private void OnLobbyCreated(LobbyCreated_t callback, bool ioFailure)
    {
        isCreatingLobby = false;

        if (ioFailure || callback.m_eResult != EResult.k_EResultOK)
        {
            pendingInviteDialog = false;
            SetStatus("Lobby create failed: " + callback.m_eResult);
            return;
        }

        currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        SteamMatchmaking.SetLobbyData(currentLobbyId, "game", "RELIC");
        SteamMatchmaking.SetLobbyData(currentLobbyId, "flow", "lobby-invite-smoke-test");
        SteamMatchmaking.SetLobbyData(currentLobbyId, "version", Application.version);
        SyncLocalMemberDataIfNeeded(true);

        SetStatus("Lobby created: " + ToSteamIdValue(currentLobbyId));

        if (pendingInviteDialog)
        {
            pendingInviteDialog = false;
            OpenInviteDialog();
        }

        RefreshStatusPanel();
    }

    private void OnLobbyEntered(LobbyEnter_t callback, bool ioFailure)
    {
        if (ioFailure)
        {
            SetStatus("Lobby join failed.");
            return;
        }

        currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        SyncLocalMemberDataIfNeeded(true);
        RefreshMembersIntoPartyRuntime();
        SetStatus("Lobby entered: " + ToSteamIdValue(currentLobbyId));
        RefreshStatusPanel();
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyChatUpdated(LobbyChatUpdate_t callback)
    {
        if (HasCurrentLobby() && callback.m_ulSteamIDLobby != ToSteamIdValue(currentLobbyId))
            return;

        RefreshMembersIntoPartyRuntime();
        RefreshStatusPanel();
    }

    private void OnLobbyDataUpdated(LobbyDataUpdate_t callback)
    {
        if (HasCurrentLobby() && callback.m_ulSteamIDLobby != ToSteamIdValue(currentLobbyId))
            return;

        RefreshMembersIntoPartyRuntime();
        RefreshStatusPanel();
    }

    private void ProcessLaunchCommandLine()
    {
        string commandLine;
        int commandLineLength = SteamApps.GetLaunchCommandLine(out commandLine, 2048);

        if (commandLineLength <= 0)
            return;

        if (!SteamLobbyLaunchCommandParser.TryParseLobbyId(commandLine, out ulong lobbyId))
            return;

        JoinLobby(new CSteamID(lobbyId));
    }

    private void JoinLobby(CSteamID lobbyId)
    {
        if (!lobbyId.IsValid())
        {
            SetStatus("Invite lobby id is invalid.");
            return;
        }

        SteamAPICall_t call = SteamMatchmaking.JoinLobby(lobbyId);
        lobbyEnterCallResult.Set(call);
        SetStatus("Joining Steam lobby: " + ToSteamIdValue(lobbyId));
    }

    private void OpenInviteDialog()
    {
        if (!HasCurrentLobby())
        {
            SetStatus("Steam lobby is missing.");
            return;
        }

        SteamFriends.ActivateGameOverlayInviteDialog(currentLobbyId);
        SetStatus("Steam invite overlay opened.");
        RefreshStatusPanel();
    }

    private bool HasCurrentLobby()
    {
        return currentLobbyId.IsValid();
    }

    private void SyncLocalMemberDataIfNeeded(bool force)
    {
        if (!HasCurrentLobby())
            return;

        if (!force && Time.unscaledTime < nextMemberSyncTime)
            return;

        nextMemberSyncTime = Time.unscaledTime + Mathf.Max(MinSyncInterval, memberDataSyncInterval);
        int localSlotIndex = ResolveLocalMemberSlotIndex();
        string characterId = ResolveLocalCharacterId(localSlotIndex);

        if (!force && localSlotIndex >= 0 && localSlotIndex < lastSyncedCharacterIds.Length &&
            lastSyncedCharacterIds[localSlotIndex] == characterId)
        {
            return;
        }

        SteamMatchmaking.SetLobbyMemberData(currentLobbyId, "slotIndex", localSlotIndex.ToString());
        SteamMatchmaking.SetLobbyMemberData(currentLobbyId, "characterId", characterId ?? "");
        SteamMatchmaking.SetLobbyMemberData(currentLobbyId, "ready", "false");

        for (int i = 0; i < lastSyncedCharacterIds.Length; i++)
        {
            string slotCharacterId = ResolvePartySlotCharacterId(i);
            SteamMatchmaking.SetLobbyMemberData(currentLobbyId, "partySlot" + i, slotCharacterId ?? "");
            lastSyncedCharacterIds[i] = i == localSlotIndex ? characterId : lastSyncedCharacterIds[i];
        }

        RefreshStatusPanel();
    }

    private int ResolveLocalMemberSlotIndex()
    {
        if (!HasCurrentLobby())
            return 0;

        CSteamID localSteamId = SteamUser.GetSteamID();
        int memberCount = SteamMatchmaking.GetNumLobbyMembers(currentLobbyId);

        for (int i = 0; i < memberCount; i++)
        {
            if (SteamMatchmaking.GetLobbyMemberByIndex(currentLobbyId, i) == localSteamId)
                return Mathf.Clamp(i, 0, Mathf.Max(0, maxMembers - 1));
        }

        return 0;
    }

    private string ResolveLocalCharacterId(int localSlotIndex)
    {
        string slotCharacterId = ResolvePartySlotCharacterId(localSlotIndex);

        if (!string.IsNullOrWhiteSpace(slotCharacterId))
            return slotCharacterId;

        for (int i = 0; i < DefaultMaxMembers; i++)
        {
            slotCharacterId = ResolvePartySlotCharacterId(i);

            if (!string.IsNullOrWhiteSpace(slotCharacterId))
                return slotCharacterId;
        }

        return "";
    }

    private string ResolvePartySlotCharacterId(int slotIndex)
    {
        if (slotIndex < 0)
            return "";

        PartyRuntimeStore partyStore = DataManager.Instance != null
            ? DataManager.Instance.PartyRuntimeStore
            : null;

        if (partyStore == null || slotIndex >= partyStore.MaxPartyCountValue)
            return "";

        return partyStore.GetCharacterId(slotIndex) ?? "";
    }

    private void RefreshMembersIntoPartyRuntime()
    {
        if (!applyLobbyMembersToPartyRuntime || !HasCurrentLobby())
            return;

        if (DataManager.Instance == null || DataManager.Instance.PartyRuntimeStore == null)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        int maxPartyCount = Mathf.Min(partyStore.MaxPartyCountValue, maxMembers);
        int memberCount = Mathf.Min(SteamMatchmaking.GetNumLobbyMembers(currentLobbyId), maxPartyCount);

        partyStore.Clear();

        for (int i = 0; i < memberCount; i++)
        {
            CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(currentLobbyId, i);
            string characterId = SteamMatchmaking.GetLobbyMemberData(currentLobbyId, memberId, "characterId");

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            partyStore.SetCharacter(i, characterId);
            partyStore.SetSpawnGridIndex(i, 6 + i);
        }

        RefreshPartyViews();
    }

    private void RefreshPartyViews()
    {
        PartySlot[] partySlots = FindObjectsByType<PartySlot>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < partySlots.Length; i++)
            partySlots[i]?.RefreshFromRuntime();

        LobbyPartyStatusIconPresenter[] presenters = FindObjectsByType<LobbyPartyStatusIconPresenter>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < presenters.Length; i++)
            presenters[i]?.Refresh();

        SpawnGridPanel[] spawnGridPanels = FindObjectsByType<SpawnGridPanel>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < spawnGridPanels.Length; i++)
        {
            spawnGridPanels[i]?.AutoPlacePartyIfNeeded();
            spawnGridPanels[i]?.Refresh();
        }
    }
#endif

    private void CreateStatusPanelIfNeeded()
    {
        if (!createStatusPanelIfMissing || statusText != null || membersText != null)
            return;

        RectTransform buttonRect = transform as RectTransform;
        Transform parent = transform.parent != null ? transform.parent : transform;

        GameObject panelObject = new GameObject("SteamLobbyStatusPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.SetParent(parent, false);
        panelRect.anchorMin = buttonRect != null ? buttonRect.anchorMin : new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = buttonRect != null ? buttonRect.anchorMax : new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.sizeDelta = new Vector2(520f, 176f);
        panelRect.anchoredPosition = buttonRect != null
            ? buttonRect.anchoredPosition + new Vector2(290f, 42f)
            : Vector2.zero;

        Image background = panelObject.GetComponent<Image>();
        background.color = new Color(0.02f, 0.04f, 0.07f, 0.82f);
        background.raycastTarget = false;

        statusText = CreateText(panelRect, "Status", new Vector2(0f, 54f), 24, TextAlignmentOptions.Left);
        membersText = CreateText(panelRect, "Members", new Vector2(0f, -22f), 20, TextAlignmentOptions.TopLeft);
    }

    private TMP_Text CreateText(RectTransform parent, string objectName, Vector2 anchoredPosition, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(-28f, objectName == "Status" ? 48f : 104f);
        rect.anchoredPosition = anchoredPosition;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.text = "";
        return text;
    }

    private void SetStatus(string status)
    {
        lastStatus = status;
        RefreshStatusPanel();
        Debug.Log("[SteamLobbyInviteController] " + status, this);
    }

    private void RefreshStatusPanel()
    {
        if (statusText != null)
            statusText.text = lastStatus;

        if (membersText != null)
            membersText.text = BuildMembersText();
    }

    private string BuildMembersText()
    {
#if STEAMWORKS_NET
        if (!steamApiInitialized)
            return "Steam API is not ready.";

        if (!HasCurrentLobby())
            return "Lobby: none";

        StringBuilder builder = new StringBuilder();
        builder.Append("Lobby: ");
        builder.Append(ToSteamIdValue(currentLobbyId));
        builder.AppendLine();

        int memberCount = SteamMatchmaking.GetNumLobbyMembers(currentLobbyId);

        for (int i = 0; i < memberCount; i++)
        {
            CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(currentLobbyId, i);
            string personaName = SteamFriends.GetFriendPersonaName(memberId);
            string slotIndex = SteamMatchmaking.GetLobbyMemberData(currentLobbyId, memberId, "slotIndex");
            string characterId = SteamMatchmaking.GetLobbyMemberData(currentLobbyId, memberId, "characterId");

            builder.Append(i + 1);
            builder.Append(". ");
            builder.Append(string.IsNullOrWhiteSpace(personaName) ? ToSteamIdValue(memberId).ToString() : personaName);
            builder.Append(" / slot ");
            builder.Append(string.IsNullOrWhiteSpace(slotIndex) ? i.ToString() : slotIndex);
            builder.Append(" / ");
            builder.Append(string.IsNullOrWhiteSpace(characterId) ? "no character" : characterId);
            builder.AppendLine();
        }

        return builder.ToString();
#else
        return "Steamworks.NET is not resolved.";
#endif
    }

#if STEAMWORKS_NET
    private static ulong ToSteamIdValue(CSteamID steamId)
    {
        return steamId.m_SteamID;
    }
#endif
}
