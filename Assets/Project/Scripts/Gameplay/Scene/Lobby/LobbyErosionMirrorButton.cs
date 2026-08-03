using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LobbyErosionMirrorButton : MonoBehaviour
{
    private const string DefaultPanelObjectName = "ErosionSelectPanel";
    private const string DefaultCloseButtonObjectName = "CloseButton";

    [Header("Panel")]
    [SerializeField] private GameObject erosionSelectPanel;
    [SerializeField] private string erosionSelectPanelName = DefaultPanelObjectName;
    [SerializeField] private bool autoFindPanel = true;

    [Header("Close Button")]
    [SerializeField] private Button closeButton;
    [SerializeField] private string closeButtonObjectName = DefaultCloseButtonObjectName;
    [SerializeField] private bool autoFindCloseButton = true;

    [Header("Opened Panel Sorting")]
    [SerializeField] private bool bringPanelToFront = true;
    [SerializeField] private bool forcePanelCanvasSorting = true;
    [SerializeField] private int panelSortingOrder = 1000;
    [SerializeField] private bool addGraphicRaycasterToPanel = true;

    [Header("Input Block")]
    [SerializeField] private bool blockWhenLobbyMenuOpen = true;
    [SerializeField] private bool blockWhenSkillUpgradePanelOpen = true;

    [Header("Sound")]
    [SerializeField] private bool playClickSound = true;
    [SerializeField] private SfxType clickSfx = SfxType.NormalButtonClick;
    [SerializeField, Range(0f, 1f)] private float clickSfxVolume = 1f;

    private void Awake()
    {
        BindCloseButton();
    }

    private void OnEnable()
    {
        BindCloseButton();
    }

    private void LateUpdate()
    {
        // ESC 등 다른 스크립트가 패널만 비활성화했을 때도
        // 월드 오브젝트 입력 차단이 남지 않도록 자동으로 해제합니다.
        if (!LobbyPositionModalInputBlocker.IsBlockedBy(this))
            return;

        GameObject panel = ResolvePanel();
        if (panel == null || !panel.activeInHierarchy)
            LobbyPositionModalInputBlocker.Unblock(this);
    }

    private void OnDisable()
    {
        if (LobbyPositionModalInputBlocker.IsBlockedBy(this))
            CloseErosionSelectPanel();
    }

    private void OnDestroy()
    {
        UnbindCloseButton();
        LobbyPositionModalInputBlocker.Unblock(this);
    }

    private void OnMouseUpAsButton()
    {
        OpenErosionSelectPanel();
    }

    public void OpenErosionSelectPanel()
    {
        if (ShouldBlockClick())
            return;

        if (!ResolveReferences())
            return;

        BindCloseButton();
        PlayClickSfx();
        TitleManager.CloseTitleModePanelsExceptInScene(erosionSelectPanel);

        erosionSelectPanel.SetActive(true);

        if (bringPanelToFront)
            erosionSelectPanel.transform.SetAsLastSibling();

        ApplyOpenedPanelSorting(erosionSelectPanel);
        LobbyPositionModalInputBlocker.Block(this);
    }

    public void CloseErosionSelectPanel()
    {
        GameObject panel = ResolvePanel();
        if (panel != null)
            panel.SetActive(false);

        LobbyPositionModalInputBlocker.Unblock(this);
    }

    /// <summary>
    /// 이 버튼이 지정된 침식도 선택 패널을 관리하는지 확인합니다.
    /// ESC 입력에서 동일한 패널을 관리하는 정확한 인스턴스를 찾는 데 사용합니다.
    /// </summary>
    public bool ControlsPanel(GameObject panel)
    {
        if (panel == null)
            return false;

        GameObject resolvedPanel = ResolvePanel();
        return resolvedPanel == panel;
    }

    private bool ShouldBlockClick()
    {
        if (LobbyPositionModalInputBlocker.IsBlockedByAnother(this))
            return true;

        if (blockWhenSkillUpgradePanelOpen && SkillUpgradePanel.IsAnyPanelOpen)
            return true;

        return blockWhenLobbyMenuOpen && UIPanelButton.IsMenuPanelOpen;
    }

    private bool ResolveReferences()
    {
        GameObject panel = ResolvePanel();
        if (panel != null)
            return true;

        Debug.LogWarning("[LobbyErosionMirrorButton] ErosionSelectPanel is missing.", this);
        return false;
    }

    private GameObject ResolvePanel()
    {
        if (erosionSelectPanel != null)
            return erosionSelectPanel;

        if (!autoFindPanel)
            return null;

        erosionSelectPanel = FindSceneObject(erosionSelectPanelName);
        return erosionSelectPanel;
    }

    private void BindCloseButton()
    {
        Button resolvedButton = ResolveCloseButton();
        if (resolvedButton == null)
            return;

        resolvedButton.onClick.RemoveListener(CloseErosionSelectPanel);
        resolvedButton.onClick.AddListener(CloseErosionSelectPanel);
    }

    private void UnbindCloseButton()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseErosionSelectPanel);
    }

    private Button ResolveCloseButton()
    {
        if (closeButton != null)
            return closeButton;

        if (!autoFindCloseButton)
            return null;

        GameObject panel = ResolvePanel();
        if (panel == null)
            return null;

        Transform buttonTransform = FindChildRecursive(panel.transform, closeButtonObjectName);
        if (buttonTransform == null)
            return null;

        closeButton = buttonTransform.GetComponent<Button>();
        return closeButton;
    }

    private void ApplyOpenedPanelSorting(GameObject panel)
    {
        if (panel == null || !forcePanelCanvasSorting)
            return;

        Canvas canvas = panel.GetComponent<Canvas>();
        if (canvas == null)
            canvas = panel.AddComponent<Canvas>();

        canvas.overrideSorting = true;
        canvas.sortingOrder = panelSortingOrder;

        if (!addGraphicRaycasterToPanel)
            return;

        GraphicRaycaster raycaster = panel.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
            panel.AddComponent<GraphicRaycaster>();
    }

    private void PlayClickSfx()
    {
        if (!playClickSound || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(clickSfx, clickSfxVolume);
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

public static class LobbyPositionModalInputBlocker
{
    private static object owner;

    public static bool IsBlocked =>
        owner != null || PanelCameraMover.IsAnyTargetPanelOpen();

    public static void Block(object ownerToken)
    {
        if (ownerToken == null)
            return;

        owner = ownerToken;
    }

    public static void Unblock(object ownerToken)
    {
        if (ownerToken == null || ReferenceEquals(owner, ownerToken))
            owner = null;
    }

    public static bool IsBlockedBy(object ownerToken)
    {
        return ownerToken != null && ReferenceEquals(owner, ownerToken);
    }

    public static bool IsBlockedByAnother(object ownerToken)
    {
        return owner != null && !ReferenceEquals(owner, ownerToken);
    }
}
