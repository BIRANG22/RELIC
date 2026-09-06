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
    [SerializeField] private GameObject settingButton;

    [Header("Lighting")]
    [SerializeField] private GameObject lobbyDirectionalLight;
    [SerializeField] private GameObject positionDirectionalLight;

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
        // CharacterSettingPanel과 SettingButton이 PositionPanel의 자식이므로
        // 캐릭터 설정 화면에서도 PositionPanel 루트는 유지합니다.
        SetActive(positionPanel, isPosition || isCharacterSelection);

        // 캐릭터 설정 화면에서도 SettingButton은 계속 표시합니다.
        if (settingButton == null)
            settingButton = FindSceneObject("SettingButton");

        SetActive(settingButton, isPosition || isCharacterSelection);
        SetActive(lobbyDirectionalLight, !isPosition);
        SetActive(positionDirectionalLight, isPosition);
    }



    private void LateUpdate()
    {
        // 다른 전환 스크립트나 인스펙터 배열에서 PositionPanel/SettingButton을
        // 비활성화하더라도 캐릭터 설정 화면에서는 마지막 단계에서 다시 유지합니다.
        if (CurrentState != LobbyViewState.CharacterSelection)
            return;

        if (positionPanel == null)
            positionPanel = FindSceneObject("PositionPanel");

        if (settingButton == null)
            settingButton = FindSceneObject("SettingButton");

        SetActive(positionPanel, true);
        SetActive(settingButton, true);
    }

    private static GameObject FindSceneObject(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        GameObject[] objects = FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject candidate = objects[i];

            if (candidate != null && candidate.name == objectName)
                return candidate;
        }

        return null;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}
