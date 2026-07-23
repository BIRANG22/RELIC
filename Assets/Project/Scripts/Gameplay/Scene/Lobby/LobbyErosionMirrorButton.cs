using TMPro;
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

    [Header("Close Button")]
    [SerializeField] private string closeButtonText = "X";
    [SerializeField] private Vector2 closeButtonSize = new(60f, 60f);
    [SerializeField] private Vector2 closeButtonOffset = new(-20f, -20f);

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
        EnsureWorldSpriteCollider();
    }

    private void OnDisable()
    {
        if (LobbyPositionModalInputBlocker.IsBlockedBy(this))
            CloseErosionSelectPanel();
    }

    private void OnMouseUpAsButton()
    {
        OpenErosionSelectPanel();
    }

    public void OpenErosionSelectPanel()
    {
        if (ShouldBlockClick())
            return;

        EnsureWorldSpriteCollider();

        if (!ResolveReferences())
        {
            return;
        }

        PlayClickSfx();
        TitleManager.CloseTitleModePanelsExceptInScene(erosionSelectPanel);

        RectTransform panelRect = erosionSelectPanel.transform as RectTransform;
        CreateCloseButton(panelRect);
        erosionSelectPanel.SetActive(true);

        if (bringPanelToFront)
            erosionSelectPanel.transform.SetAsLastSibling();

        ApplyOpenedPanelSorting(erosionSelectPanel);
        LobbyPositionModalInputBlocker.Block(this);
    }

    public void CloseErosionSelectPanel()
    {
        if (erosionSelectPanel != null)
            erosionSelectPanel.SetActive(false);

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

    private void CreateCloseButton(RectTransform panelRect)
    {
        if (panelRect == null)
            return;

        Transform existing = panelRect.Find("CloseButton");
        if (existing != null)
        {
            Button existingButton = existing.GetComponent<Button>();
            if (existingButton != null)
            {
                existingButton.onClick.RemoveListener(CloseErosionSelectPanel);
                existingButton.onClick.AddListener(CloseErosionSelectPanel);
            }

            return;
        }

        GameObject buttonObject = new(
            "CloseButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.SetParent(panelRect, false);
        buttonRect.anchorMin = Vector2.one;
        buttonRect.anchorMax = Vector2.one;
        buttonRect.pivot = Vector2.one;
        buttonRect.anchoredPosition = closeButtonOffset;
        buttonRect.sizeDelta = closeButtonSize;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(CloseErosionSelectPanel);

        GameObject labelObject = new(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(buttonRect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = closeButtonText;
        label.fontSize = 34f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
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

    private void EnsureWorldSpriteCollider()
    {
        if (GetComponent<Collider2D>() != null)
            return;

        if (GetComponent<SpriteRenderer>() == null)
            return;

        gameObject.AddComponent<PolygonCollider2D>();
    }

    private void PlayClickSfx()
    {
        if (!playClickSound || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(clickSfx, clickSfxVolume);
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

    public static bool IsBlocked => owner != null;

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
