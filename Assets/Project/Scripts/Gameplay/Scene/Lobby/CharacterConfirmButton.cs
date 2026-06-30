using UnityEngine;

public class CharacterConfirmButton : MonoBehaviour
{
    [SerializeField] private CharPick charPick;

    [Header("Sound")]
    [Tooltip("확정 버튼 자체의 클릭 효과음과 중복되지 않도록, 기본값은 꺼둡니다.")]
    [SerializeField] private bool playCurrentCharacterClickSound = false;

    public void Execute()
    {
        if (charPick == null)
        {
            Debug.LogWarning("[CharacterConfirmButton] CharPick is missing.");
            return;
        }

        charPick.ConfirmCurrentCharacter(playCurrentCharacterClickSound);
    }
}