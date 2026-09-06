using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PositionStageSelectController : MonoBehaviour
{
    [Header("Legacy Stage Selection")]
    [Tooltip("기존 StageSelectPanel을 다시 사용할 때만 켭니다. 현재는 PlayButton 직행 구조이므로 기본값은 false입니다.")]
    [SerializeField] private bool stageSelectionEnabled = false;

    [Header("Overlay")]
    [SerializeField] private RectTransform positionPanel;
    [SerializeField] private GameObject stageSelectPanel;
    [SerializeField] private Color blockerColor = new(0f, 0f, 0f, 0.35f);

    [Header("Close Button")]
    [SerializeField] private string closeButtonText = "X";
    [SerializeField] private Vector2 closeButtonSize = new(60f, 60f);
    [SerializeField] private Vector2 closeButtonOffset = new(-20f, -20f);

    private GameObject overlayRoot;

    private void Awake()
    {
        EnsureWorldSpriteCollider();
    }

    private void OnMouseUpAsButton()
    {
        if (!stageSelectionEnabled || LobbyPositionModalInputBlocker.IsBlocked)
            return;

        OpenStageSelect();
    }

    public void OpenStageSelect()
    {
        if (!stageSelectionEnabled || LobbyPositionModalInputBlocker.IsBlocked)
            return;

        EnsureWorldSpriteCollider();

        if (!ResolveReferences())
            return;

        EnsureOverlay();

        if (overlayRoot == null)
            return;

        overlayRoot.SetActive(true);
        overlayRoot.transform.SetAsLastSibling();
        stageSelectPanel.SetActive(true);
        stageSelectPanel.transform.SetAsLastSibling();
    }

    public void CloseStageSelect()
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(false);
    }

    private bool ResolveReferences()
    {
        if (positionPanel == null)
        {
            GameObject found = FindSceneObject("PositionPanel");

            if (found != null)
                positionPanel = found.GetComponent<RectTransform>();
        }

        if (stageSelectPanel == null)
            stageSelectPanel = FindSceneObject("StageSelectPanel");

        if (positionPanel != null && stageSelectPanel != null)
            return true;

        Debug.LogWarning(
            "[PositionStageSelectController] PositionPanel or StageSelectPanel is missing.",
            this);
        return false;
    }

    private void EnsureOverlay()
    {
        if (overlayRoot != null)
            return;

        overlayRoot = new GameObject(
            "PositionStageSelectOverlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
        overlayRect.SetParent(positionPanel, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlayRect.localScale = Vector3.one;

        Image blocker = overlayRoot.GetComponent<Image>();
        blocker.color = blockerColor;
        blocker.raycastTarget = true;

        RectTransform panelRect = stageSelectPanel.transform as RectTransform;
        stageSelectPanel.transform.SetParent(overlayRect, false);

        if (panelRect != null)
            panelRect.localScale = Vector3.one;

        CreateCloseButton(panelRect);
        overlayRoot.SetActive(false);
    }

    private void CreateCloseButton(RectTransform panelRect)
    {
        if (panelRect == null || panelRect.Find("CloseButton") != null)
            return;

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
        button.onClick.AddListener(CloseStageSelect);

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

    private void EnsureWorldSpriteCollider()
    {
        if (GetComponent<Collider2D>() != null)
            return;

        if (GetComponent<SpriteRenderer>() == null)
            return;

        gameObject.AddComponent<PolygonCollider2D>();
    }

    private static GameObject FindSceneObject(string objectName)
    {
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
