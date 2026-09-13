using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LobbyErosionMirrorButton : MonoBehaviour
{
    private const string DefaultPanelObjectName = "ErosionSelectPanel";

    [Header("Panel")]
    [SerializeField] private GameObject erosionSelectPanel;
    [SerializeField] private string erosionSelectPanelName = DefaultPanelObjectName;
    [SerializeField] private bool autoFindPanel = true;

    [Header("Opened Panel Sorting")]
    [SerializeField] private bool forcePanelCanvasSorting = true;
    [SerializeField] private int panelSortingOrder = 1000;
    [SerializeField] private bool addGraphicRaycasterToPanel = true;

    [Header("Input Block")]
    [SerializeField] private bool blockWhenLobbyMenuOpen = true;
    [SerializeField] private bool blockWhenSkillUpgradePanelOpen = true;

    [Header("Sound")]
    [SerializeField] private bool playClickSound = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string clickSfx = AudioIds.Sfx.NormalButtonClick;
    [SerializeField, Range(0f, 1f)] private float clickSfxVolume = 1f;

    private void Awake()
    {
    }

    private void OnEnable()
    {
    }

    private void LateUpdate()
    {
        // If ESC or another script disables the panel, release this input blocker automatically.
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
        LobbyPositionSharedModalBackground.HideForOwner(this);
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

        PlayClickSfx();
        TitleManager.CloseTitleModePanelsExceptInScene(erosionSelectPanel);

        LobbyPositionSharedModalBackground.ShowForPanel(
            erosionSelectPanel,
            this,
            CloseErosionSelectPanel);
        erosionSelectPanel.SetActive(true);
        RefreshPanelText(erosionSelectPanel);

        ApplyOpenedPanelSorting(erosionSelectPanel);
        LobbyPositionModalInputBlocker.Block(this);
    }

    public void CloseErosionSelectPanel()
    {
        GameObject panel = ResolvePanel();
        if (panel != null)
            panel.SetActive(false);

        LobbyPositionSharedModalBackground.HideForOwner(this);
        LobbyPositionModalInputBlocker.Unblock(this);
    }

    /// <summary>
    /// Returns whether this button controls the given erosion select panel.
    /// Used by ESC handling to find the exact panel owner.
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

    private void ApplyOpenedPanelSorting(GameObject panel)
    {
        if (panel == null || !forcePanelCanvasSorting)
            return;

        // 공용 BackgroundPanel의 Presentation Canvas로 표시 중일 때는
        // UIBlurBackgroundManager가 SharedBlurCanvas 위의 정렬 순서를 관리합니다.
        if (LobbyPositionSharedModalBackground.IsPanelPresented(panel))
            return;

        if (panel.GetComponent<UIBlurBackground>() != null)
            return;

        Canvas canvas = panel.GetComponent<Canvas>();
        if (canvas == null)
            return;

        canvas.overrideSorting = true;
        canvas.sortingOrder = panelSortingOrder;

        if (!addGraphicRaycasterToPanel)
            return;

        GraphicRaycaster raycaster = panel.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
            Debug.LogWarning("[LobbyErosionMirrorButton] Panel Canvas has no GraphicRaycaster.", panel);
    }

    private static void RefreshPanelText(GameObject panel)
    {
        MenuPanelTextRefresher refresher = EnsurePanelTextRefresher(panel);
        if (refresher == null)
            return;

        refresher.RefreshNow();
        refresher.RefreshNextFrame();
    }

    private static MenuPanelTextRefresher EnsurePanelTextRefresher(GameObject panel)
    {
        if (panel == null)
            return null;

        MenuPanelTextRefresher refresher = panel.GetComponent<MenuPanelTextRefresher>();
        return refresher != null ? refresher : panel.AddComponent<MenuPanelTextRefresher>();
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
