using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LobbyQuestPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text questText;

    public void Apply(LobbyQuestState state)
    {
        gameObject.SetActive(state.IsVisible);

        if (questText != null)
            questText.text = state.Text;
    }
}
