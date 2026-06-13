using System.Collections;
using UnityEngine;

public class ReturnToPartyLobbyButton : MonoBehaviour
{
    [SerializeField] private GameObject[] panelsToClose;
    [SerializeField] private GameObject partyLobbyPanel;
    [SerializeField] private bool playClickSound = true;

    [Header("Delay")]
    [SerializeField] private float clickActionDelay = 0.2f;

    private Coroutine executeCoroutine;
    private bool isProcessing;

    private void OnDisable()
    {
        if (executeCoroutine != null)
        {
            StopCoroutine(executeCoroutine);
            executeCoroutine = null;
        }

        isProcessing = false;
    }

    public void Execute()
    {
        if (isProcessing)
            return;

        if (playClickSound && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(SfxType.NormalButtonClick);

        if (clickActionDelay <= 0f)
        {
            ExecuteNow();
            return;
        }

        isProcessing = true;
        executeCoroutine = StartCoroutine(ExecuteAfterDelay());
    }

    private IEnumerator ExecuteAfterDelay()
    {
        yield return new WaitForSecondsRealtime(clickActionDelay);

        ExecuteNow();

        isProcessing = false;
        executeCoroutine = null;
    }

    private void ExecuteNow()
    {
        if (panelsToClose != null)
        {
            foreach (var panel in panelsToClose)
            {
                if (panel != null)
                    panel.SetActive(false);
            }
        }

        if (partyLobbyPanel != null)
            partyLobbyPanel.SetActive(true);
    }
}
