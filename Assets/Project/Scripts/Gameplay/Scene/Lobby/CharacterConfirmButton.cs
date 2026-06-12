using UnityEngine;

public class CharacterConfirmButton : MonoBehaviour
{
    [SerializeField] private CharPick charPick;

    public void Execute()
    {
        if (charPick == null)
        {
            Debug.LogWarning("[CharacterConfirmButton] CharPick is missing.");
            return;
        }

        charPick.ConfirmCurrentCharacter();
    }
}