using System;
using System.Collections;
using System.Collections.Generic;
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
    private const string InfoPanelName = "Info_Panel";
    private const string BackButtonName = "BackButton";
    private const string BackName = "Back";
    private const string LobbyIconName = "Lobby_Icon";
    private const string MainIconName = "Mainicon";

    private static readonly string[] ReadyConflictingPanelNames =
    {
        "ErosionSelectPanel",
        "RelicShopPanel",
        "CultureTankPanel",
        "StoragePanel",
        "CharacterSettingPanel"
    };

    [Header("Shared Background")]
    [SerializeField] private GameObject backgroundRoot;
    [SerializeField] private GameObject infoRoot;
    [SerializeField] private Button backButton;

    [Header("Ready Presentation")]
    [SerializeField] private GameObject readyBackRoot;
    [SerializeField] private GameObject readyLobbyIconRoot;
    [SerializeField] private GameObject readyMainIconRoot;

    [Header("Ready Transition")]
    [SerializeField, Min(0f)] private float readyPanelFadeDuration = 0.15f;

    private object activeOwner;
    private GameObject activePanel;
    private Action activeCloseAction;
    private bool backButtonBound;
    private bool keepBackgroundActiveDuringSwitch;
    private bool readyPresentationActive;
    private bool readyBackWasActive;
    private bool readyLobbyIconWasActive;
    private bool readyMainIconWasActive;
    private Coroutine readyTransitionCoroutine;

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

    /// <summary>
    /// 탐사 준비 화면을 열기 전에 기존 PositionPanel 모달을 부드럽게 정리합니다.
    /// BackgroundPanel 자체는 유지하고 Back / Lobby_Icon / Mainicon만 숨깁니다.
    /// 기존 패널의 페이드아웃이 끝난 뒤 onPrepared를 호출합니다.
    /// </summary>
    public static void PrepareForReadyPanel(GameObject readyPanel, Action onPrepared = null)
    {
        LobbyPositionSharedModalBackground controller = ResolveController();
        if (controller != null)
        {
            controller.PrepareForReadyPanelInternal(readyPanel, onPrepared);
            return;
        }

        DisableReadyConflictingPanels(readyPanel);
        onPrepared?.Invoke();
    }

    /// <summary>
    /// 탐사 준비 화면을 닫을 때 숨겨둔 공용 UI 상태를 복구한 뒤 BackgroundPanel 전체를 끕니다.
    /// </summary>
    public static void HideAfterReadyPanel()
    {
        LobbyPositionSharedModalBackground controller = FindController();
        controller?.HideAfterReadyPanelInternal();
    }

    /// <summary>
    /// 이전 호출부 호환용입니다. 탐사 준비가 아닌 일반 복구가 필요할 때만 사용합니다.
    /// </summary>
    public static void RestoreAfterReadyPanel()
    {
        LobbyPositionSharedModalBackground controller = FindController();
        controller?.RestoreReadyPresentationInternal();
    }

    private void PrepareForReadyPanelInternal(GameObject readyPanel, Action onPrepared)
    {
        ResolveReferences();

        if (readyTransitionCoroutine != null)
        {
            StopCoroutine(readyTransitionCoroutine);
            readyTransitionCoroutine = null;
        }

        readyTransitionCoroutine = StartCoroutine(PrepareForReadyPanelRoutine(readyPanel, onPrepared));
    }

    private IEnumerator PrepareForReadyPanelRoutine(GameObject readyPanel, Action onPrepared)
    {
        GameObject previousPanel = activePanel;
        Action closeAction = activeCloseAction;

        List<GameObject> panelsToFade = CollectReadyConflictingPanels(readyPanel);
        if (previousPanel != null && previousPanel != readyPanel && previousPanel.activeSelf && !panelsToFade.Contains(previousPanel))
            panelsToFade.Add(previousPanel);

        yield return FadeOutPanels(panelsToFade);

        // 패널별 정리 로직은 페이드가 끝난 뒤 실행합니다.
        // 이 동안 BackgroundPanel은 유지해야 하므로 전환 플래그를 사용합니다.
        keepBackgroundActiveDuringSwitch = true;
        try
        {
            if (closeAction != null)
                closeAction.Invoke();
        }
        finally
        {
            keepBackgroundActiveDuringSwitch = false;
        }

        for (int i = 0; i < panelsToFade.Count; i++)
        {
            GameObject panel = panelsToFade[i];
            if (panel != null && panel != readyPanel && panel.activeSelf)
                panel.SetActive(false);
        }

        activeOwner = null;
        activePanel = null;
        activeCloseAction = null;

        ApplyReadyPresentationInternal();
        readyTransitionCoroutine = null;
        onPrepared?.Invoke();
    }

    private IEnumerator FadeOutPanels(List<GameObject> panels)
    {
        if (panels == null || panels.Count == 0)
            yield break;

        List<CanvasGroup> groups = new List<CanvasGroup>();
        List<float> originalAlpha = new List<float>();
        List<bool> originalInteractable = new List<bool>();
        List<bool> originalBlocksRaycasts = new List<bool>();

        for (int i = 0; i < panels.Count; i++)
        {
            GameObject panel = panels[i];
            if (panel == null || !panel.activeSelf)
                continue;

            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            if (group == null)
                group = panel.AddComponent<CanvasGroup>();

            groups.Add(group);
            originalAlpha.Add(group.alpha);
            originalInteractable.Add(group.interactable);
            originalBlocksRaycasts.Add(group.blocksRaycasts);
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        float duration = Mathf.Max(0f, readyPanelFadeDuration);
        if (duration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                for (int i = 0; i < groups.Count; i++)
                {
                    CanvasGroup group = groups[i];
                    if (group != null)
                        group.alpha = Mathf.Lerp(originalAlpha[i], 0f, t);
                }
                yield return null;
            }
        }

        for (int i = 0; i < groups.Count; i++)
        {
            CanvasGroup group = groups[i];
            if (group == null)
                continue;

            group.alpha = originalAlpha[i];
            group.interactable = originalInteractable[i];
            group.blocksRaycasts = originalBlocksRaycasts[i];
        }
    }

    private static List<GameObject> CollectReadyConflictingPanels(GameObject readyPanel)
    {
        List<GameObject> result = new List<GameObject>();
        for (int i = 0; i < ReadyConflictingPanelNames.Length; i++)
        {
            GameObject panel = FindSceneObject(ReadyConflictingPanelNames[i]);
            if (panel == null || panel == readyPanel || !panel.activeSelf || result.Contains(panel))
                continue;

            result.Add(panel);
        }
        return result;
    }

    private void HideAfterReadyPanelInternal()
    {
        RestoreReadyPresentationInternal();
        if (backgroundRoot != null && backgroundRoot.activeSelf)
            backgroundRoot.SetActive(false);
    }

    private void ApplyReadyPresentationInternal()
    {
        ResolveReferences();
        ResolveReadyPresentationRoots();

        if (backgroundRoot != null && !backgroundRoot.activeSelf)
            backgroundRoot.SetActive(true);

        if (!readyPresentationActive)
        {
            readyBackWasActive = readyBackRoot != null && readyBackRoot.activeSelf;
            readyLobbyIconWasActive = readyLobbyIconRoot != null && readyLobbyIconRoot.activeSelf;
            readyMainIconWasActive = readyMainIconRoot != null && readyMainIconRoot.activeSelf;
            readyPresentationActive = true;
        }

        SetActiveIfNeeded(readyBackRoot, false);
        SetActiveIfNeeded(readyLobbyIconRoot, false);
        SetActiveIfNeeded(readyMainIconRoot, false);
    }

    private void RestoreReadyPresentationInternal()
    {
        if (!readyPresentationActive)
            return;

        ResolveReferences();
        ResolveReadyPresentationRoots();

        if (backgroundRoot != null && !backgroundRoot.activeSelf)
            backgroundRoot.SetActive(true);

        SetActiveIfNeeded(readyBackRoot, readyBackWasActive);
        SetActiveIfNeeded(readyLobbyIconRoot, readyLobbyIconWasActive);
        SetActiveIfNeeded(readyMainIconRoot, readyMainIconWasActive);
        readyPresentationActive = false;
    }

    private void ResolveReadyPresentationRoots()
    {
        if (backgroundRoot == null)
            return;

        if (readyBackRoot == null)
        {
            Transform found = FindChildRecursive(backgroundRoot.transform, BackName);
            if (found != null)
                readyBackRoot = found.gameObject;
        }

        if (readyLobbyIconRoot == null)
        {
            Transform found = FindChildRecursive(backgroundRoot.transform, LobbyIconName);
            if (found != null)
                readyLobbyIconRoot = found.gameObject;
        }

        if (readyMainIconRoot == null)
        {
            Transform found = FindChildRecursive(backgroundRoot.transform, MainIconName);
            if (found != null)
                readyMainIconRoot = found.gameObject;
        }
    }

    private static void SetActiveIfNeeded(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
            target.SetActive(active);
    }

    private static void DisableReadyConflictingPanels(GameObject readyPanel)
    {
        for (int i = 0; i < ReadyConflictingPanelNames.Length; i++)
        {
            GameObject panel = FindSceneObject(ReadyConflictingPanelNames[i]);
            if (panel == null || panel == readyPanel || !panel.activeSelf)
                continue;

            panel.SetActive(false);
        }
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


        LobbyInfoPanelUI.RefreshAll();

        // 패널을 연 경로와 관계없이 현재 Shortcut의 Inspector Display Name/Icon으로
        // BackgroundPanel의 Mainicon 표시를 동기화합니다.
        LobbyPositionPanelShortcutUI.RefreshForPanel(panel);

        // 패널 진입 시 한 번만 초기화합니다.
        // 다음 프레임에 다시 초기화하면 사용자가 스크롤한 직후 위치가 되돌아가
        // 클릭을 방해할 수 있으므로 지연 초기화는 사용하지 않습니다.
        ResetPanelScrolls(panel);
    }

    private static void ResetPanelScrolls(GameObject panel)
    {
        if (panel == null)
            return;

        ScrollRect[] scrollRects = panel.GetComponentsInChildren<ScrollRect>(true);
        for (int i = 0; i < scrollRects.Length; i++)
        {
            ScrollRect scrollRect = scrollRects[i];
            if (scrollRect == null)
                continue;

            if (scrollRect.vertical)
                scrollRect.verticalNormalizedPosition = 1f;

            if (scrollRect.horizontal)
                scrollRect.horizontalNormalizedPosition = 0f;

            scrollRect.StopMovement();
        }
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

        if (!keepBackgroundActiveDuringSwitch)
        {
            if (backgroundRoot != null && backgroundRoot.activeSelf)
                backgroundRoot.SetActive(false);

        }
    }

    private void HandleBackButtonClicked()
    {
        // 탐사 준비 화면에서는 BackgroundPanel을 바로 닫지 않고
        // LobbyEquipPanelUI의 정상 Close 경로를 사용해 Info/Ready 퇴장 슬라이드를 재생합니다.
        if (readyPresentationActive && LobbyEquipPanelUI.TryCloseOpenReadyPanel())
            return;

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

        if (infoRoot == null)
        {
            GameObject positionPanel = FindSceneObject(PositionPanelName);
            if (positionPanel != null)
            {
                Transform infoPanel = positionPanel.transform.Find(InfoPanelName);
                if (infoPanel == null)
                    infoPanel = FindChildRecursive(positionPanel.transform, InfoPanelName);

                if (infoPanel != null)
                    infoRoot = infoPanel.gameObject;
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
