using UnityEngine;
using UnityEngine.UI;

public class LobbyPositionToggleButton : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button toggleButton;

    [Header("Lobby Objects")]
    [SerializeField] private GameObject backMain;
    [SerializeField] private GameObject effectLobby;
    [SerializeField] private GameObject effectCharacter;
    [SerializeField] private GameObject lobbyMainPanel;
    [SerializeField] private GameObject characterSettingPanel;

    [Header("Position Objects")]
    [SerializeField] private GameObject position;
    [SerializeField] private GameObject positionPanel;

    [Header("Position Lighting")]
    [SerializeField] private GameObject lobbyDirectionalLight;
    [SerializeField] private GameObject positionDirectionalLight;

    private bool positionModeActive;
    private bool statesCaptured;
    private bool backMainWasActive;
    private bool effectLobbyWasActive;
    private bool effectCharacterWasActive;
    private bool lobbyMainPanelWasActive;
    private bool characterSettingPanelWasActive;
    private bool lobbyDirectionalLightWasActive;
    private bool positionDirectionalLightWasActive;

    private void Awake()
    {
        if (toggleButton == null)
            toggleButton = GetComponent<Button>();

        ResolveMissingReferences();
        toggleButton?.onClick.AddListener(TogglePositionMode);
    }

    private void OnDestroy()
    {
        toggleButton?.onClick.RemoveListener(TogglePositionMode);
    }

    public void TogglePositionMode()
    {
        ResolveMissingReferences();

        if (positionModeActive)
            ExitPositionMode();
        else
            EnterPositionMode();
    }

    private void EnterPositionMode()
    {
        CaptureLobbyStates();

        SetActive(backMain, false);
        SetActive(effectLobby, false);
        SetActive(effectCharacter, false);
        SetActive(lobbyMainPanel, false);
        SetActive(characterSettingPanel, false);
        SetActive(position, true);
        SetActive(positionPanel, true);
        SetActive(lobbyDirectionalLight, false);
        SetActive(positionDirectionalLight, true);

        positionModeActive = true;
    }

    private void ExitPositionMode()
    {
        if (statesCaptured)
        {
            SetActive(backMain, backMainWasActive);
            SetActive(effectLobby, effectLobbyWasActive);
            SetActive(effectCharacter, effectCharacterWasActive);
            SetActive(lobbyMainPanel, lobbyMainPanelWasActive);
            SetActive(characterSettingPanel, characterSettingPanelWasActive);
            SetActive(lobbyDirectionalLight, lobbyDirectionalLightWasActive);
            SetActive(positionDirectionalLight, positionDirectionalLightWasActive);
        }

        SetActive(position, false);
        SetActive(positionPanel, false);

        positionModeActive = false;
        statesCaptured = false;
    }

    private void CaptureLobbyStates()
    {
        backMainWasActive = IsActive(backMain);
        effectLobbyWasActive = IsActive(effectLobby);
        effectCharacterWasActive = IsActive(effectCharacter);
        lobbyMainPanelWasActive = IsActive(lobbyMainPanel);
        characterSettingPanelWasActive = IsActive(characterSettingPanel);
        lobbyDirectionalLightWasActive = IsActive(lobbyDirectionalLight);
        positionDirectionalLightWasActive = IsActive(positionDirectionalLight);
        statesCaptured = true;
    }

    private void ResolveMissingReferences()
    {
        backMain ??= FindSceneObject("Back_Main");
        effectLobby ??= FindSceneObject("Effect_Lobby");
        effectCharacter ??= FindSceneObject("Effect_Char");
        lobbyMainPanel ??= FindSceneObject("LobbyMainPanel");
        characterSettingPanel ??= FindSceneObject("CharacterSettingPanel");
        position ??= FindSceneObject("Position");
        positionPanel ??= FindSceneObject("PositionPanel");
        lobbyDirectionalLight ??= FindSceneObject("Directional Light");
        positionDirectionalLight ??= FindSceneObject("Postion Directional Light");
    }

    private GameObject FindSceneObject(string objectName)
    {
        if (!gameObject.scene.IsValid())
            return null;

        GameObject[] roots = gameObject.scene.GetRootGameObjects();

        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            Transform[] transforms = roots[rootIndex].GetComponentsInChildren<Transform>(true);

            for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
            {
                Transform candidate = transforms[transformIndex];
                if (candidate != null && candidate.name == objectName)
                    return candidate.gameObject;
            }
        }

        return null;
    }

    private static bool IsActive(GameObject target)
    {
        return target != null && target.activeSelf;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}
