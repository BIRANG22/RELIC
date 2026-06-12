using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    [Header("Logo")]
    [SerializeField] private GameObject onLogo;
    [SerializeField] private GameObject offLogo;

    [Header("Logo Blink Option")]
    [SerializeField] private float blinkOffDuration = 0.08f;

    [Header("Warning")]
    [SerializeField] private TitleWarningUI warningUI;
    [SerializeField] private string unavailableMessage = "아직 준비되지 않았습니다.";
    [SerializeField] private Button[] unavailableButtons;

    private Coroutine blinkCoroutine;

    private void Awake()
    {
        AddUnavailableButtonListeners();
    }

    private void Start()
    {
        RefreshLogoDefaultState();
    }

    private void OnDestroy()
    {
        RemoveUnavailableButtonListeners();
    }

    public void OnClickLogoArea()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        blinkCoroutine = StartCoroutine(BlinkLogoCoroutine());
    }

    public void ShowUnavailableWarning()
    {
        TitleWarningUI targetWarningUI = GetWarningUI();

        if (targetWarningUI == null)
        {
            Debug.LogWarning("[TitleManager] TitleWarningUI is not assigned or found in the scene.");
            return;
        }

        targetWarningUI.Show(unavailableMessage);
    }

    private IEnumerator BlinkLogoCoroutine()
    {
        if (offLogo != null)
        {
            offLogo.SetActive(true);
        }

        if (onLogo != null)
        {
            onLogo.SetActive(false);
        }

        yield return new WaitForSeconds(blinkOffDuration);

        if (offLogo != null)
        {
            offLogo.SetActive(true);
        }

        if (onLogo != null)
        {
            onLogo.SetActive(true);
        }

        blinkCoroutine = null;
    }

    private void RefreshLogoDefaultState()
    {
        if (offLogo != null)
        {
            offLogo.SetActive(true);
        }

        if (onLogo != null)
        {
            onLogo.SetActive(true);
        }
    }

    private TitleWarningUI GetWarningUI()
    {
        if (warningUI != null)
        {
            return warningUI;
        }

        if (TitleWarningUI.Instance != null)
        {
            warningUI = TitleWarningUI.Instance;
            return warningUI;
        }

        warningUI = FindFirstObjectByType<TitleWarningUI>(FindObjectsInactive.Include);
        return warningUI;
    }

    private void AddUnavailableButtonListeners()
    {
        if (unavailableButtons == null)
        {
            return;
        }

        for (int i = 0; i < unavailableButtons.Length; i++)
        {
            if (unavailableButtons[i] == null)
            {
                continue;
            }

            unavailableButtons[i].onClick.RemoveListener(ShowUnavailableWarning);
            unavailableButtons[i].onClick.AddListener(ShowUnavailableWarning);
        }
    }

    private void RemoveUnavailableButtonListeners()
    {
        if (unavailableButtons == null)
        {
            return;
        }

        for (int i = 0; i < unavailableButtons.Length; i++)
        {
            if (unavailableButtons[i] == null)
            {
                continue;
            }

            unavailableButtons[i].onClick.RemoveListener(ShowUnavailableWarning);
        }
    }
}
