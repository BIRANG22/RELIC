using UnityEngine;

public class CharacterSelectionState : MonoBehaviour
{
    public static CharacterSelectionState Instance { get; private set; }

    public CharacterType CurrentCharacter { get; private set; } = CharacterType.None;
    public GameObject CurrentOpenedPanel { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SelectCharacter(CharacterType characterType)
    {
        CurrentCharacter = characterType;
        //Debug.LogWarning(CurrentCharacter);
    }

    public void OpenPanel(GameObject panel)
    {
        if (CurrentOpenedPanel != null && CurrentOpenedPanel != panel)
        {
            CurrentOpenedPanel.SetActive(false);
        }

        CurrentOpenedPanel = panel;

        if (CurrentOpenedPanel != null && !CurrentOpenedPanel.activeSelf)
        {
            CurrentOpenedPanel.SetActive(true);
        }
    }

    public void CloseCurrentPanel()
    {
        if (CurrentOpenedPanel != null)
        {
            CurrentOpenedPanel.SetActive(false);
            CurrentOpenedPanel = null;
        }
    }
}