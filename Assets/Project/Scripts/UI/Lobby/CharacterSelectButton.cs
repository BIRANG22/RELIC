using UnityEngine;

public class CharacterSelectButton : MonoBehaviour
{
    [SerializeField] private CharacterType characterType;
    [SerializeField] private bool playClickSound = true;
    [SerializeField] private GameObject targetPanel;

    private static GameObject currentActivePanel;

    public void Execute()
    {
        if (playClickSound)
            AudioManager.Instance.PlaySfx(SfxType.Click);

        if (CharacterSelectionState.Instance == null)
        {
            Debug.LogWarning("[CharacterSelectButton] CharacterSelectionState instance is missing.");
            return;
        }

        CharacterSelectionState.Instance.SelectCharacter(characterType);

        if (targetPanel == null)
        {
            Debug.LogWarning($"[CharacterSelectButton] Target panel is not assigned on {gameObject.name}.");
            return;
        }

        if (currentActivePanel != null && currentActivePanel != targetPanel)
        {
            currentActivePanel.SetActive(false);
        }

        targetPanel.SetActive(true);
        currentActivePanel = targetPanel;
    }
}