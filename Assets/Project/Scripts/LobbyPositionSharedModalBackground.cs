using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PositionPanel 아래의 공용 BackgroundPanel을 ErosionSelectPanel, RelicShopPanel,
/// CultureTankPanel이 함께 사용하도록 관리합니다. Blur는 BackgroundPanel 자체가 담당합니다.
/// 공용 BackButton은 현재 열린 패널의 정상 Close 경로만 호출합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class LobbyPositionSharedModalBackground : MonoBehaviour
{
    private const string PositionPanelName = "PositionPanel";
    private const string BackgroundPanelName = "BackgroundPanel";
    private const string BackButtonName = "BackButton";

    [Header("Shared Background")]
    [SerializeField] private GameObject backgroundRoot;
    [SerializeField] private Button backButton;

    private object activeOwner;
    private GameObject activePanel;
    private Action activeCloseAction;
    private bool backButtonBound;
    private bool keepBackgroundActiveDuringSwitch;

    public bool IsShowing => backgroundRoot != null && backgroundRoot.activeSelf;
    public GameObject ActivePanel => activePanel;

    public static bool IsPanelPresented(GameObject panel)
    {
        if (panel == null)
            return false;

        LobbyPositionSharedModalBackground controller = FindController();
        return controller != null && controller.IsShowing && controller.activePanel == panel;
    }

    private void Awake()
    {
        ResolveReferences();
        BindBackButton();
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindBackButton();
    }

    private void OnDestroy()
    {
        UnbindBackButton();
    }

    public static void ShowForPanel(GameObject panel, object owner, Action closeAction)
    {
        if (panel == null || owner == null)
            return;

        LobbyPositionSharedModalBackground controller = ResolveController();
        if (controller == null)
        {
            Debug.LogWarning(
                "[LobbyPositionSharedModalBackground] PositionPanel/BackgroundPanel을 찾지 못했습니다. " +
                "공용 Background 하이어라키를 확인해 주세요.",
                panel);
            return;
        }

        controller.Show(panel, owner, closeAction);
    }

    public static void HideForOwner(object owner)
    {
        if (owner == null)
            return;

        LobbyPositionSharedModalBackground controller = FindController();
        controller?.Hide(owner);
    }

    /// <summary>
    /// ESC 등 공용 입력에서 현재 PositionPanel 모달을 패널별 Close 경로로 닫습니다.
    /// 공용 BackButton과 동일한 closeAction을 사용합니다.
    /// </summary>
    public static bool TryCloseActivePanel()
    {
        LobbyPositionSharedModalBackground controller = FindController();
        if (controller == null || controller.activePanel == null)
            return false;

        controller.HandleBackButtonClicked();
        return true;
    }

    public static void HideForPanel(GameObject panel)
    {
        if (panel == null)
            return;

        LobbyPositionSharedModalBackground controller = FindController();
        controller?.HidePanel(panel);
    }

    /// <summary>
    /// Lobby_Icon shortcut에서 다른 PositionPanel 모달로 전환하기 전에
    /// 현재 패널의 정상 Close 경로를 실행합니다. 전환 중에는 BackgroundPanel을 유지합니다.
    /// 현재 패널이 닫기를 거부하면 false를 반환합니다.
    /// </summary>
    public static bool PrepareForPanelSwitch(GameObject targetPanel)
    {
        if (targetPanel == null)
            return false;

        LobbyPositionSharedModalBackground controller = FindController();
        if (controller == null || controller.activePanel == null)
            return true;

        if (controller.activePanel == targetPanel)
            return false;

        GameObject previousPanel = controller.activePanel;
        Action closeAction = controller.activeCloseAction;

        controller.keepBackgroundActiveDuringSwitch = true;
        try
        {
            if (closeAction != null)
                closeAction.Invoke();
            else
            {
                if (previousPanel != null)
                    previousPanel.SetActive(false);

                controller.HideInternal();
            }
        }
        finally
        {
            controller.keepBackgroundActiveDuringSwitch = false;
        }

        // Close가 실제로 완료되었다면 activePanel이 비워집니다.
        // 구매 연출 등으로 Close가 거부된 경우에는 기존 패널이 그대로 남습니다.
        return controller.activePanel == null;
    }

    private void Show(GameObject panel, object owner, Action closeAction)
    {
        ResolveReferences();
        BindBackButton();

        activeOwner = owner;
        activePanel = panel;
        activeCloseAction = closeAction;

        if (backgroundRoot == null)
            return;

        UIBlurBackground sharedBlur = backgroundRoot.GetComponent<UIBlurBackground>();
        if (sharedBlur != null)
        {
            // BackgroundPanel/Background에 공용 Blur가 구성되어 있으면 기존 각 패널의 Blur 요청은 끕니다.
            UIBlurBackground legacyPanelBlur = panel.GetComponent<UIBlurBackground>();
            if (legacyPanelBlur != null && legacyPanelBlur.enabled)
                legacyPanelBlur.enabled = false;

            LobbyQuestManager.Instance?.ConfigureQuestPanelBlur(sharedBlur);
        }
        else
        {
            Debug.LogWarning(
                "[LobbyPositionSharedModalBackground] BackgroundPanel에 UIBlurBackground가 없습니다. " +
                "공용 Blur를 사용하려면 BackgroundPanel 오브젝트에 UIBlurBackground를 설정해 주세요.",
                backgroundRoot);
        }

        if (!backgroundRoot.activeSelf)
            backgroundRoot.SetActive(true);

        // 패널을 연 경로와 관계없이 현재 Shortcut의 Inspector Display Name/Icon으로
        // BackgroundPanel의 Mainicon 표시를 동기화합니다.
        LobbyPositionPanelShortcutUI.RefreshForPanel(panel);
    }

    private void Hide(object owner)
    {
        if (!ReferenceEquals(activeOwner, owner))
            return;

        HideInternal();
    }

    private void HidePanel(GameObject panel)
    {
        if (activePanel != panel)
            return;

        HideInternal();
    }

    private void HideInternal()
    {
        activeOwner = null;
        activePanel = null;
        activeCloseAction = null;

        if (!keepBackgroundActiveDuringSwitch &&
            backgroundRoot != null &&
            backgroundRoot.activeSelf)
        {
            backgroundRoot.SetActive(false);
        }
    }

    private void HandleBackButtonClicked()
    {
        Action closeAction = activeCloseAction;
        GameObject panelToClose = activePanel;

        // 패널별 Close에는 구매 연출 중 닫기 금지 같은 고유 조건이 있을 수 있으므로
        // Close가 등록되어 있으면 결과를 강제로 덮어쓰지 않고 해당 경로만 실행합니다.
        if (closeAction != null)
        {
            closeAction.Invoke();
            return;
        }

        if (panelToClose != null)
            panelToClose.SetActive(false);

        HideInternal();
    }

    private void ResolveReferences()
    {
        if (backgroundRoot == null)
        {
            GameObject positionPanel = FindSceneObject(PositionPanelName);
            if (positionPanel != null)
            {
                Transform backgroundPanel = positionPanel.transform.Find(BackgroundPanelName);
                if (backgroundPanel == null)
                    backgroundPanel = FindChildRecursive(positionPanel.transform, BackgroundPanelName);

                if (backgroundPanel != null)
                    backgroundRoot = backgroundPanel.gameObject;
            }
        }

        if (backButton == null && backgroundRoot != null)
        {
            Transform buttonTransform = FindChildRecursive(backgroundRoot.transform, BackButtonName);
            if (buttonTransform != null)
                backButton = buttonTransform.GetComponent<Button>();
        }
    }

    private void BindBackButton()
    {
        if (backButton == null)
            return;

        backButton.onClick.RemoveListener(HandleBackButtonClicked);
        backButton.onClick.AddListener(HandleBackButtonClicked);
        backButtonBound = true;
    }

    private void UnbindBackButton()
    {
        if (!backButtonBound || backButton == null)
            return;

        backButton.onClick.RemoveListener(HandleBackButtonClicked);
        backButtonBound = false;
    }

    private static LobbyPositionSharedModalBackground ResolveController()
    {
        LobbyPositionSharedModalBackground controller = FindController();
        if (controller != null)
            return controller;

        GameObject positionPanel = FindSceneObject(PositionPanelName);
        if (positionPanel == null)
            return null;

        controller = positionPanel.GetComponent<LobbyPositionSharedModalBackground>();
        if (controller == null)
            controller = positionPanel.AddComponent<LobbyPositionSharedModalBackground>();

        return controller;
    }

    private static LobbyPositionSharedModalBackground FindController()
    {
        return FindFirstObjectByType<LobbyPositionSharedModalBackground>(FindObjectsInactive.Include);
    }

    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
            return null;

        if (root.name == targetName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        GameObject[] objects = FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject candidate = objects[i];
            if (candidate != null && candidate.name == objectName)
                return candidate;
        }

        return null;
    }
}
