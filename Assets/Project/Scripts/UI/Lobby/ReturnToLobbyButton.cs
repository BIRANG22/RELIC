using UnityEngine;

public class ReturnToPartyLobbyButton : MonoBehaviour
{
    [SerializeField] private GameObject[] panelsToClose;
    [SerializeField] private GameObject partyLobbyPanel;
    [SerializeField] private bool playClickSound = true;

    public void Execute()
    {
        if (playClickSound && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(SfxType.Click);

        foreach (var panel in panelsToClose)
        {
            if (panel != null)
                panel.SetActive(false);
        }

        if (partyLobbyPanel != null)
            partyLobbyPanel.SetActive(true);
    }
}