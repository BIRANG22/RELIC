using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TitleManager : MonoBehaviour
{
    [Header("Logo")]
    [SerializeField] private GameObject onLogo;
    [SerializeField] private GameObject offLogo;

    [Header("Warning")]
    [SerializeField] private TitleWarningUI warningUI;
    [SerializeField] private string unavailableMessage = "아직 준비되지 않았습니다.";
    [SerializeField] private Button[] unavailableButtons;

    [Header("Exit")]
    [SerializeField] private Button exitButton;
    [SerializeField] private bool autoFindExitButton = true;
    [SerializeField] private bool playExitClickSound = true;
    [SerializeField] private SfxType exitClickSfx = SfxType.NormalButtonClick;

    private bool isOnLogoActive = true;

    private void Awake()
    {
        ResolveExitButton();
        AddExitButtonListener();
        AddUnavailableButtonListeners();
    }

    private void Start()
    {
        RefreshLogoDefaultState();
    }

    private void OnDestroy()
    {
        RemoveExitButtonListener();
        RemoveUnavailableButtonListeners();
    }

    public void OnClickLogoArea()
    {
        isOnLogoActive = !isOnLogoActive;
        ApplyLogoState();
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

    public void OnClickExitGame()
    {
        PlayExitClickSound();

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void RefreshLogoDefaultState()
    {
        isOnLogoActive = true;
        ApplyLogoState();
    }

    private void ApplyLogoState()
    {
        if (onLogo != null)
        {
            onLogo.SetActive(isOnLogoActive);
        }

        if (offLogo != null)
        {
            offLogo.SetActive(!isOnLogoActive);
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

    private void ResolveExitButton()
    {
        if (exitButton != null || !autoFindExitButton)
        {
            return;
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            string buttonName = button.gameObject.name;
            if (string.IsNullOrWhiteSpace(buttonName))
            {
                continue;
            }

            string lowerName = buttonName.ToLowerInvariant();
            if (lowerName.Contains("exit") || lowerName.Contains("quit"))
            {
                exitButton = button;
                return;
            }
        }
    }

    private void AddExitButtonListener()
    {
        if (exitButton == null)
        {
            return;
        }

        exitButton.onClick.RemoveListener(OnClickExitGame);
        exitButton.onClick.AddListener(OnClickExitGame);
    }

    private void RemoveExitButtonListener()
    {
        if (exitButton == null)
        {
            return;
        }

        exitButton.onClick.RemoveListener(OnClickExitGame);
    }

    private void AddUnavailableButtonListeners()
    {
        if (unavailableButtons == null)
        {
            return;
        }

        for (int i = 0; i < unavailableButtons.Length; i++)
        {
            Button unavailableButton = unavailableButtons[i];
            if (unavailableButton == null || unavailableButton == exitButton)
            {
                continue;
            }

            unavailableButton.onClick.RemoveListener(ShowUnavailableWarning);
            unavailableButton.onClick.AddListener(ShowUnavailableWarning);
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
            Button unavailableButton = unavailableButtons[i];
            if (unavailableButton == null)
            {
                continue;
            }

            unavailableButton.onClick.RemoveListener(ShowUnavailableWarning);
        }
    }

    private void PlayExitClickSound()
    {
        if (!playExitClickSound)
        {
            return;
        }

        if (AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.PlaySfx(exitClickSfx);
    }
}
