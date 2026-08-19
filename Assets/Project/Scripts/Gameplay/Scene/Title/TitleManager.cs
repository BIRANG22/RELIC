using System.Collections;
using System.Collections.Generic;
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

    [Header("BGM")]
    [SerializeField] private bool playTitleBgmOnStart = true;
    [SerializeField] private int titleBgmRetryFrameCount = 2;

    [Header("Warning")]
    [SerializeField] private TitleWarningUI warningUI;
    [SerializeField] private string unavailableMessage = "아직 준비되지 않았습니다.";
    [SerializeField] private Button[] unavailableButtons;

    [Header("Run Buttons")]
    [SerializeField] private GameObject startButtonObject;
    [SerializeField] private GameObject continueButtonObject;
    [SerializeField] private GameObject abandonBattleButtonObject;
    [SerializeField] private bool autoFindRunButtons = true;
    [SerializeField] private string startButtonName = "StartButton";
    [SerializeField] private string continueButtonName = "ContinueButton";
    [SerializeField] private string abandonBattleButtonName = "AbandonBattleButton";

    [Header("Title Mode Panels")]
    [SerializeField] private GameObject[] titleModePanels;
    [SerializeField] private bool autoFindTitleModePanels = true;
    [SerializeField]
    private string[] autoFindTitleModePanelNames =
    {
        "SingleModePanel",
        "MultiModePanel"
    };

    [Header("Exit")]
    [SerializeField] private Button exitButton;
    [SerializeField] private bool autoFindExitButton = true;
    [SerializeField] private bool playExitClickSound = true;
    [SerializeField] private SfxType exitClickSfx = SfxType.NormalButtonClick;

    private bool isOnLogoActive = true;

    private void Awake()
    {
        ResolveTitleModePanels();
        ResolveRunButtons();
        ResolveExitButton();
        AddExitButtonListener();
        AddUnavailableButtonListeners();
    }

    private void OnEnable()
    {
        RefreshRunButtons();
    }

    private void Start()
    {
        RefreshLogoDefaultState();
        RefreshRunButtons();
        StartTitleBgmRetry();
    }

    private void StartTitleBgmRetry()
    {
        if (!playTitleBgmOnStart)
        {
            return;
        }

        StartCoroutine(PlayTitleBgmRetryRoutine());
    }

    private IEnumerator PlayTitleBgmRetryRoutine()
    {
        int waitFrameCount = Mathf.Max(0, titleBgmRetryFrameCount);

        for (int i = 0; i < waitFrameCount; i++)
        {
            yield return null;
        }

        if (AudioManager.Instance == null)
        {
            yield break;
        }

        AudioManager.Instance.PlayBgm(BgmType.Title);
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
        CloseTitleModePanels();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowQuitConfirm();
            return;
        }

        Debug.LogWarning("[TitleManager] UIManager is not found. Quit confirm dialog cannot be opened. Quitting directly instead.");
        QuitGameImmediately();
    }

    public void RefreshRunButtons()
    {
        ResolveRunButtons();

        bool hasBattleContinueSave = HasBattleContinueSave();

        SetActiveSafely(startButtonObject, !hasBattleContinueSave);
        SetActiveSafely(continueButtonObject, hasBattleContinueSave);
        SetActiveSafely(abandonBattleButtonObject, hasBattleContinueSave);

        if (continueButtonObject != null)
        {
            TitleContinueButton continueButton = continueButtonObject.GetComponent<TitleContinueButton>();
            if (continueButton != null)
            {
                continueButton.RefreshLockState();
            }
        }

        if (abandonBattleButtonObject != null)
        {
            TitleAbandonBattleButton abandonButton = abandonBattleButtonObject.GetComponent<TitleAbandonBattleButton>();
            if (abandonButton != null)
            {
                abandonButton.RefreshInteractable();
            }
        }
    }

    public bool HasBattleContinueSave()
    {
        return SaveSystem.Instance != null && SaveSystem.Instance.HasBattleContinueSave();
    }

    private void QuitGameImmediately()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void CloseTitleModePanels()
    {
        CloseTitleModePanelsExcept(null);
    }

    public void CloseTitleModePanelsExcept(GameObject panelToKeep)
    {
        ResolveTitleModePanels();

        if (titleModePanels == null)
        {
            return;
        }

        for (int i = 0; i < titleModePanels.Length; i++)
        {
            GameObject panel = titleModePanels[i];

            if (panel == null || panel == panelToKeep)
            {
                continue;
            }

            if (panel.activeSelf)
            {
                TitleModePanelSpreadAnimator animator = panel.GetComponent<TitleModePanelSpreadAnimator>();

                if (animator != null)
                {
                    animator.Close();
                }
                else
                {
                    panel.SetActive(false);
                }
            }
        }
    }

    public static void CloseTitleModePanelsInScene()
    {
        TitleManager manager = FindFirstObjectByType<TitleManager>(FindObjectsInactive.Include);

        if (manager == null)
        {
            return;
        }

        manager.CloseTitleModePanels();
    }

    public static void CloseTitleModePanelsExceptInScene(GameObject panelToKeep)
    {
        TitleManager manager = FindFirstObjectByType<TitleManager>(FindObjectsInactive.Include);

        if (manager == null)
        {
            return;
        }

        manager.CloseTitleModePanelsExcept(panelToKeep);
    }

    public static void RefreshRunButtonsInScene()
    {
        TitleManager manager = FindFirstObjectByType<TitleManager>(FindObjectsInactive.Include);

        if (manager == null)
        {
            return;
        }

        manager.RefreshRunButtons();
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

    private void ResolveTitleModePanels()
    {
        if (!autoFindTitleModePanels)
        {
            return;
        }

        List<GameObject> resolvedPanels = new List<GameObject>();

        if (titleModePanels != null)
        {
            for (int i = 0; i < titleModePanels.Length; i++)
            {
                AddPanelIfValid(resolvedPanels, titleModePanels[i]);
            }
        }

        if (autoFindTitleModePanelNames != null)
        {
            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < autoFindTitleModePanelNames.Length; i++)
            {
                string panelName = autoFindTitleModePanelNames[i];

                if (string.IsNullOrWhiteSpace(panelName))
                {
                    continue;
                }

                for (int j = 0; j < transforms.Length; j++)
                {
                    Transform target = transforms[j];

                    if (target == null || target.gameObject == null)
                    {
                        continue;
                    }

                    if (target.name == panelName)
                    {
                        AddPanelIfValid(resolvedPanels, target.gameObject);
                        break;
                    }
                }
            }
        }

        titleModePanels = resolvedPanels.ToArray();
    }

    private void ResolveRunButtons()
    {
        if (!autoFindRunButtons)
        {
            return;
        }

        if (startButtonObject == null)
        {
            startButtonObject = FindObjectByName(startButtonName);
        }

        if (continueButtonObject == null)
        {
            continueButtonObject = FindObjectByName(continueButtonName);
        }

        if (abandonBattleButtonObject == null)
        {
            abandonBattleButtonObject = FindObjectByName(abandonBattleButtonName);
        }
    }

    private static void SetActiveSafely(GameObject target, bool shouldBeActive)
    {
        if (target == null || target.activeSelf == shouldBeActive)
        {
            return;
        }

        target.SetActive(shouldBeActive);
    }

    private static GameObject FindObjectByName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];
            if (target == null || target.gameObject == null)
            {
                continue;
            }

            if (target.name == objectName)
            {
                return target.gameObject;
            }
        }

        return null;
    }

    private void AddPanelIfValid(List<GameObject> panels, GameObject panel)
    {
        if (panels == null || panel == null)
        {
            return;
        }

        if (panels.Contains(panel))
        {
            return;
        }

        panels.Add(panel);
    }

    private void ResolveExitButton()
    {
        if (exitButton != null || !autoFindExitButton)
        {
            return;
        }

        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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
