using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class LobbyQuestPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text questText;

    private void Awake()
    {
        ConfigureDynamicQuestText();
    }

    private void ConfigureDynamicQuestText()
    {
        if (questText == null)
            return;

        // LobbyQuestState.Build() resolves the quest key through GameLocalization.
        // Keep that per-language result as the only text writer for this dynamic label.
        // LocalizedTMPFont is not disabled, so locale-specific fonts still work.
        LocalizedTMPText.ProtectDynamicOwnership(questText);

        DynamicLocalizedTMPText redundantWriter = questText.GetComponent<DynamicLocalizedTMPText>();
        if (redundantWriter != null)
            redundantWriter.enabled = false;
    }

    public void Apply(LobbyQuestState state)
    {
        ConfigureDynamicQuestText();
        if (gameObject.activeSelf != state.IsVisible)
            gameObject.SetActive(state.IsVisible);

        // LobbyQuestManager refreshes the localized state every LateUpdate.
        // Writing after activation avoids an empty initial label.
        if (questText != null && state.IsVisible && questText.text != state.Text)
            questText.text = state.Text;
    }
}
