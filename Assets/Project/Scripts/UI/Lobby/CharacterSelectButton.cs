using UnityEngine;

public class CharacterSelectButton : MonoBehaviour
{
    [SerializeField] private CharacterType characterType;
    [SerializeField] private bool playClickSound = true;

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
    }
}