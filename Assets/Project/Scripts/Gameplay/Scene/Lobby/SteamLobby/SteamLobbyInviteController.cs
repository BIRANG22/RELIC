using System.IO;
using System.Text;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
    [SerializeField] private TMP_InputField lobbyIdInput;

    private static bool steamApiInitialized;
    private static bool ownsSteamApi;
    private static bool steamShutdownRegistered;

    private readonly string[] lastSyncedCharacterIds = new string[DefaultMaxMembers];
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
    private Callback<GameOverlayActivated_t> gameOverlayActivated;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSteamRuntimeState()
    {
#if STEAMWORKS_NET
        if (ownsSteamApi && steamApiInitialized)
            SteamAPI.Shutdown();
#endif

        Application.quitting -= ShutdownSteamOnApplicationQuit;
        steamApiInitialized = false;
        ownsSteamApi = false;
        steamShutdownRegistered = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void InitializeSteamBeforeSplashScreen()
    {
#if STEAMWORKS_NET
        if (TryInitializeSteamApi(out string failure))
        {
            Debug.Log("[SteamLobbyInviteController] Steam API initialized before splash screen.");
            return;
        }

        Debug.LogWarning(
            "[SteamLobbyInviteController] Early Steam initialization did not complete. " +
            failure);
#endif
    }

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        HandleLobbyIdPasteShortcut();
#endif
    }

    private void OnDestroy()
    {
#if STEAMWORKS_NET
        lobbyCreatedCallResult?.Dispose();
        lobbyEnterCallResult?.Dispose();
        gameLobbyJoinRequested?.Dispose();
        lobbyChatUpdate?.Dispose();
        lobbyDataUpdate?.Dispose();
        gameOverlayActivated?.Dispose();
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

    public void CopyCurrentLobbyId()
    {
#if STEAMWORKS_NET
        if (!EnsureSteamReady())
            return;

        if (!HasCurrentLobby())
        {
            SetStatus("Create or join a Steam lobby before copying its ID.");
            return;
        }

        string lobbyId = ToSteamIdValue(currentLobbyId).ToString();
        GUIUtility.systemCopyBuffer = lobbyId;
        SetStatus("Lobby ID copied: " + lobbyId);
#else
        SetStatus("Steamworks.NET package is not resolved yet.");
#endif
    }

    public void JoinLobbyByIdInput()
    {
#if STEAMWORKS_NET
        if (!EnsureSteamReady())
            return;

        string input = lobbyIdInput != null ? lobbyIdInput.text : "";

        if (!SteamLobbyIdParser.TryParse(input, out ulong lobbyId, out string error))
        {
            SetStatus(error);
            return;
        }

        CSteamID steamLobbyId = new CSteamID(lobbyId);

        if (!steamLobbyId.IsValid())
        {
            SetStatus("Lobby ID is not a valid Steam ID.");
            return;
        }

        JoinLobby(steamLobbyId);
#else
        SetStatus("Steamworks.NET package is not resolved yet.");
#endif
    }

    public void PasteLobbyIdFromClipboard()
    {
        if (lobbyIdInput == null)
            return;

        if (!SteamLobbyIdParser.TryParse(
                GUIUtility.systemCopyBuffer,
                out ulong lobbyId,
                out string error))
        {
            SetStatus("Clipboard: " + error);
            return;
        }

        string lobbyIdText = lobbyId.ToString();
        lobbyIdInput.text = lobbyIdText;
        lobbyIdInput.caretPosition = lobbyIdText.Length;
        lobbyIdInput.ActivateInputField();
        SetStatus("Lobby ID pasted.");
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void HandleLobbyIdPasteShortcut()
    {
        if (lobbyIdInput == null ||
            EventSystem.current == null ||
            EventSystem.current.currentSelectedGameObject != lobbyIdInput.gameObject)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        bool controlPressed = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;

        if (controlPressed && keyboard.vKey.wasPressedThisFrame)
            PasteLobbyIdFromClipboard();
    }
#endif

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

        if (!TryInitializeSteamApi(out string failure))
        {
            string expectedAppIdPath = GetExpectedSteamAppIdPath();
            bool appIdFileExists = File.Exists(expectedAppIdPath);
            Debug.LogError("[SteamLobbyInviteController] " + failure, this);
            SetStatus(
                "Steam API init failed. App ID file exists: " +
                appIdFileExists +
                "\n" +
                expectedAppIdPath);
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

        if (gameOverlayActivated == null)
            gameOverlayActivated = Callback<GameOverlayActivated_t>.Create(OnGameOverlayActivated);
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

    private void OnGameOverlayActivated(GameOverlayActivated_t callback)
    {
        bool active = callback.m_bActive != 0;
        Debug.Log(
            "[SteamLobbyInviteController] Steam overlay " +
            (active ? "activated." : "deactivated."),
            this);

        if (active)
            SetStatus("Steam overlay activated.");
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
        SetStatus("Steam invite overlay requested.");
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
        RectTransform buttonRect = transform as RectTransform;
        Transform parent = transform.parent != null ? transform.parent : transform;

        if (createStatusPanelIfMissing && statusText == null && membersText == null)
        {
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        CreateDevelopmentToolsIfNeeded(parent, buttonRect);
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void CreateDevelopmentToolsIfNeeded(Transform parent, RectTransform buttonRect)
    {
        if (lobbyIdInput != null)
            return;

        GameObject panelObject = new GameObject("SteamLobbyDevelopmentTools", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.SetParent(parent, false);
        panelRect.anchorMin = buttonRect != null ? buttonRect.anchorMin : new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = buttonRect != null ? buttonRect.anchorMax : new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.sizeDelta = new Vector2(520f, 76f);
        panelRect.anchoredPosition = buttonRect != null
            ? buttonRect.anchoredPosition + new Vector2(290f, 34f)
            : Vector2.zero;

        Image background = panelObject.GetComponent<Image>();
        background.color = new Color(0.02f, 0.04f, 0.07f, 0.9f);
        background.raycastTarget = true;

        lobbyIdInput = CreateLobbyIdInput(panelRect);
        CreateDevelopmentButton(panelRect, "CopyLobbyIdButton", "Copy ID", new Vector2(100f, 0f), CopyCurrentLobbyId);
        CreateDevelopmentButton(panelRect, "JoinLobbyIdButton", "Join ID", new Vector2(195f, 0f), JoinLobbyByIdInput);
    }

    private static TMP_InputField CreateLobbyIdInput(RectTransform parent)
    {
        GameObject inputObject = new GameObject("LobbyIdInput", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        RectTransform inputRect = inputObject.GetComponent<RectTransform>();
        inputRect.SetParent(parent, false);
        inputRect.anchorMin = new Vector2(0f, 0.5f);
        inputRect.anchorMax = new Vector2(0f, 0.5f);
        inputRect.pivot = new Vector2(0f, 0.5f);
        inputRect.sizeDelta = new Vector2(288f, 46f);
        inputRect.anchoredPosition = new Vector2(14f, 0f);

        Image background = inputObject.GetComponent<Image>();
        background.color = new Color(0.08f, 0.11f, 0.16f, 1f);

        GameObject textAreaObject = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        RectTransform textAreaRect = textAreaObject.GetComponent<RectTransform>();
        textAreaRect.SetParent(inputRect, false);
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(10f, 4f);
        textAreaRect.offsetMax = new Vector2(-10f, -4f);

        TMP_Text placeholder = CreateInputText(textAreaRect, "Placeholder", "Lobby ID", new Color(1f, 1f, 1f, 0.45f));
        TMP_Text valueText = CreateInputText(textAreaRect, "Text", "", Color.white);

        TMP_InputField input = inputObject.GetComponent<TMP_InputField>();
        input.textViewport = textAreaRect;
        input.textComponent = valueText;
        input.placeholder = placeholder;
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.lineType = TMP_InputField.LineType.SingleLine;
        return input;
    }

    private static TMP_Text CreateInputText(RectTransform parent, string objectName, string value, Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = 18;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
        text.text = value;
        return text;
    }

    private static void CreateDevelopmentButton(
        RectTransform parent,
        string objectName,
        string label,
        Vector2 anchoredPosition,
        UnityEngine.Events.UnityAction clicked)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(86f, 46f);
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.14f, 0.32f, 0.5f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(clicked);

        TMP_Text text = CreateInputText(rect, "Label", label, Color.white);
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 17;
    }
#endif

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

    public static string GetExpectedSteamAppIdPath()
    {
        string root = Directory.GetParent(Application.dataPath)?.FullName;
        return Path.Combine(root ?? "", "steam_appid.txt");
    }

    public static string BuildSteamInitFailureMessage(
        bool isSteamRunning,
        string expectedAppIdPath,
        bool appIdFileExists)
    {
        return "Steam API init failed. " +
               "Steam running: " + isSteamRunning +
               "; App ID file exists: " + appIdFileExists +
               "; Expected App ID path: " + expectedAppIdPath;
    }

#if STEAMWORKS_NET
    private static bool TryInitializeSteamApi(out string failure)
    {
        if (steamApiInitialized)
        {
            failure = "";
            return true;
        }

        bool isSteamRunning = SteamAPI.IsSteamRunning();

        if (!isSteamRunning)
        {
            failure = "Steam client is not running.";
            return false;
        }

        try
        {
            steamApiInitialized = SteamAPI.Init();
        }
        catch (System.Exception exception)
        {
            steamApiInitialized = false;
            failure = "SteamAPI.Init threw an exception: " + exception;
            return false;
        }

        if (!steamApiInitialized)
        {
            string expectedAppIdPath = GetExpectedSteamAppIdPath();
            failure = BuildSteamInitFailureMessage(
                isSteamRunning,
                expectedAppIdPath,
                File.Exists(expectedAppIdPath));
            return false;
        }

        ownsSteamApi = true;
        RegisterSteamShutdown();
        failure = "";
        return true;
    }

    private static void RegisterSteamShutdown()
    {
        if (steamShutdownRegistered)
            return;

        Application.quitting += ShutdownSteamOnApplicationQuit;
        steamShutdownRegistered = true;
    }
#endif

    private static void ShutdownSteamOnApplicationQuit()
    {
#if STEAMWORKS_NET
        if (!ownsSteamApi || !steamApiInitialized)
            return;

        Debug.Log("[SteamLobbyInviteController] Shutting down Steam API on application quit.");
        SteamAPI.Shutdown();
#endif

        steamApiInitialized = false;
        ownsSteamApi = false;
        steamShutdownRegistered = false;
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
