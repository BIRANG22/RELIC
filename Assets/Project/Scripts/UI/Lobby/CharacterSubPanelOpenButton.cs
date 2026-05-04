using System.Collections.Generic;
using UnityEngine;

public enum CharacterSubPanelType
{
    Skill,
    Rune
}

[System.Serializable]
public class CharacterSubPanelEntry
{
    public CharacterType characterType;
    public CharacterSubPanelType panelType;
    public GameObject targetPanel;
}

public class CharacterSubPanelOpenButton : MonoBehaviour
{
    [Header("Button Type")]
    [SerializeField] private CharacterSubPanelType panelType;

    [Header("Character Panels")]
    [SerializeField] private List<CharacterSubPanelEntry> panels = new();

    [Header("Option")]
    [SerializeField] private bool playClickSound = true;

    private static GameObject currentOpenedSubPanel;

    public void Execute()
    {
        if (playClickSound && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(SfxType.Click);

        if (CharacterSelectionState.Instance == null)
        {
            Debug.LogWarning("[CharacterSubPanelOpenButton] CharacterSelectionState instance is missing.");
            return;
        }

        CharacterType currentCharacter = CharacterSelectionState.Instance.CurrentCharacter;

        if (currentCharacter == CharacterType.None)
        {
            Debug.LogWarning("[CharacterSubPanelOpenButton] No character selected.");
            return;
        }

        GameObject targetPanel = FindTargetPanel(currentCharacter, panelType);

        if (targetPanel == null)
        {
            Debug.LogWarning($"[CharacterSubPanelOpenButton] No panel found. Character:{currentCharacter}, Type:{panelType}");
            return;
        }

        if (currentOpenedSubPanel != null && currentOpenedSubPanel != targetPanel)
            currentOpenedSubPanel.SetActive(false);

        targetPanel.SetActive(true);
        currentOpenedSubPanel = targetPanel;

        Debug.Log($"[CharacterSubPanelOpenButton] Open: {currentCharacter} / {panelType} / {targetPanel.name}");
    }

    private GameObject FindTargetPanel(CharacterType characterType, CharacterSubPanelType type)
    {
        foreach (var entry in panels)
        {
            if (entry.characterType == characterType && entry.panelType == type)
                return entry.targetPanel;
        }

        return null;
    }
}