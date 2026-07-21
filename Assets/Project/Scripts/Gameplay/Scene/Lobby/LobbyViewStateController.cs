using UnityEngine;

public enum LobbyViewState
{
    Lobby,
    CharacterSelection,
    Position
}

public sealed class LobbyViewStateController : MonoBehaviour
{
    [Header("Lobby Objects")]
    [SerializeField] private GameObject backMain;
    [SerializeField] private GameObject effectLobby;
    [SerializeField] private GameObject effectCharacter;
    [SerializeField] private GameObject lobbyMainPanel;
    [SerializeField] private GameObject characterSettingPanel;
    [SerializeField] private GameObject characterPreviewSpawnRoot;

    [Header("Position Objects")]
    [SerializeField] private GameObject position;
    [SerializeField] private GameObject positionPanel;

    [Header("Lighting")]
    [SerializeField] private GameObject lobbyDirectionalLight;
    [SerializeField] private GameObject positionDirectionalLight;

    [Header("Position Camera")]
    [SerializeField] private HorizontalHubCameraDrag hubCameraDrag;

    public LobbyViewState CurrentState { get; private set; }

    private void Start()
    {
        ShowPosition();
    }

    public void ShowLobby()
    {
        ApplyState(LobbyViewState.Lobby);
    }

    public void ShowCharacterSelection()
    {
        ApplyState(LobbyViewState.CharacterSelection);
    }

    public void ShowPosition()
    {
        ApplyState(LobbyViewState.Position);
    }

    public void TogglePosition()
    {
        ApplyState(CurrentState == LobbyViewState.Position
            ? LobbyViewState.Lobby
            : LobbyViewState.Position);
    }

    private void ApplyState(LobbyViewState state)
    {
        CurrentState = state;

        bool isLobby = state == LobbyViewState.Lobby;
        bool isCharacterSelection = state == LobbyViewState.CharacterSelection;
        bool isPosition = state == LobbyViewState.Position;

        SetActive(backMain, !isPosition);
        SetActive(effectLobby, isLobby);
        SetActive(effectCharacter, isCharacterSelection);
        SetActive(lobbyMainPanel, isLobby);
        SetActive(characterSettingPanel, isCharacterSelection);
        SetActive(characterPreviewSpawnRoot, isCharacterSelection);
        SetActive(position, isPosition);
        SetActive(positionPanel, isPosition);
        SetActive(lobbyDirectionalLight, !isPosition);
        SetActive(positionDirectionalLight, isPosition);

        if (hubCameraDrag != null)
            hubCameraDrag.enabled = isPosition;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}
