using TMPro;
using UnityEngine;

public sealed class LobbyWorldObjectHoverName : MonoBehaviour
{
    public enum HoverNameType
    {
        Research,
        Exploration,
        Resonance
    }

    [Header("Hover Name")]
    [SerializeField] private HoverNameType hoverNameType = HoverNameType.Research;

    [Header("Position")]
    [Tooltip("콜라이더 하단에서 이름 UI까지의 화면 픽셀 간격입니다. 음수값이면 콜라이더 아래쪽으로 내려갑니다.")]
    [SerializeField] private float screenYOffset = -18f;

    [Header("Blocking Panels")]
    [Tooltip("If any registered panel is active, the world object hover name is hidden.")]
    [SerializeField] private GameObject[] blockingPanels;

    [Tooltip("When enabled, common lobby modal panels are also detected automatically by name.")]
    [SerializeField] private bool autoDetectCommonBlockingPanels = true;

    private static readonly string[] CommonBlockingPanelNames =
    {
        "CharacterSettingPanel",
        "ErosionSelectPanel",
        "CultureTankPanel",
        "RelicShopPanel",
        "DialoguePanel",
        "MenuPanel"
    };

    [Header("Optional Direct References")]
    [Tooltip("비워두면 WorldObjectNamePanel 아래의 ObjectName을 자동으로 찾습니다.")]
    [SerializeField] private RectTransform objectNameRect;

    [Tooltip("비워두면 ObjectName의 TMP_Text를 자동으로 찾습니다.")]
    [SerializeField] private TMP_Text objectNameText;

    [Tooltip("비워두면 이 오브젝트의 Collider2D를 자동으로 찾습니다.")]
    [SerializeField] private Collider2D targetCollider;

    private Canvas targetCanvas;
    private RectTransform canvasRect;
    private Camera worldCamera;
    private GameObject[] commonBlockingPanels;
    private bool isHovered;

    private void Awake()
    {
        AutoBindIfNeeded();
        SetNameVisible(false);
    }

    private void OnEnable()
    {
        AutoBindIfNeeded();
        isHovered = false;
        SetNameVisible(false);
    }

    private void OnDisable()
    {
        isHovered = false;
        SetNameVisible(false);
    }

    private void OnMouseEnter()
    {
        AutoBindIfNeeded();

        if (objectNameRect == null || objectNameText == null || targetCollider == null)
            return;

        if (IsAnyBlockingPanelActive())
        {
            isHovered = false;
            SetNameVisible(false);
            return;
        }

        isHovered = true;
        objectNameText.text = GetDisplayName();
        SetNameVisible(true);
        UpdateNamePosition();
    }

    private void OnMouseExit()
    {
        isHovered = false;
        SetNameVisible(false);
    }

    private void LateUpdate()
    {
        if (!isHovered)
            return;

        if (IsAnyBlockingPanelActive())
        {
            // Require the pointer to leave and enter again after a modal closes.
            isHovered = false;
            SetNameVisible(false);
            return;
        }

        UpdateNamePosition();
    }

    private void UpdateNamePosition()
    {
        AutoBindIfNeeded();

        if (objectNameRect == null || targetCollider == null || targetCanvas == null || canvasRect == null)
            return;

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (worldCamera == null)
            return;

        Bounds bounds = targetCollider.bounds;
        Vector3 worldBottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        Vector3 screenPoint = worldCamera.WorldToScreenPoint(worldBottomCenter);

        if (screenPoint.z < 0f)
        {
            SetNameVisible(false);
            return;
        }

        screenPoint.y += screenYOffset;

        Camera uiCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : targetCanvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPoint,
                uiCamera,
                out Vector2 localPoint))
        {
            return;
        }

        objectNameRect.anchoredPosition = localPoint;

        if (!objectNameRect.gameObject.activeSelf)
            SetNameVisible(true);
    }

    private void SetNameVisible(bool visible)
    {
        if (objectNameRect != null && objectNameRect.gameObject.activeSelf != visible)
            objectNameRect.gameObject.SetActive(visible);
    }

    private string GetDisplayName()
    {
        switch (hoverNameType)
        {
            case HoverNameType.Research:
                return "작업대";

            case HoverNameType.Exploration:
                return "조각상";

            case HoverNameType.Resonance:
                return "비석";

            default:
                return string.Empty;
        }
    }

    private bool IsAnyBlockingPanelActive()
    {
        if (blockingPanels != null)
        {
            for (int i = 0; i < blockingPanels.Length; i++)
            {
                GameObject panel = blockingPanels[i];

                if (panel != null && panel.activeInHierarchy)
                    return true;
            }
        }

        if (!autoDetectCommonBlockingPanels)
            return false;

        AutoBindCommonBlockingPanelsIfNeeded();

        if (commonBlockingPanels != null)
        {
            for (int i = 0; i < commonBlockingPanels.Length; i++)
            {
                GameObject panel = commonBlockingPanels[i];

                if (panel != null && panel.activeInHierarchy)
                    return true;
            }
        }

        return false;
    }

    private void AutoBindCommonBlockingPanelsIfNeeded()
    {
        if (commonBlockingPanels != null && commonBlockingPanels.Length == CommonBlockingPanelNames.Length)
            return;

        commonBlockingPanels = new GameObject[CommonBlockingPanelNames.Length];

        for (int i = 0; i < CommonBlockingPanelNames.Length; i++)
            commonBlockingPanels[i] = FindSceneObjectByName(CommonBlockingPanelNames[i]);
    }

    private void AutoBindIfNeeded()
    {
        if (targetCollider == null)
            targetCollider = GetComponent<Collider2D>();

        if (objectNameRect == null)
        {
            GameObject panel = FindSceneObjectByName("WorldObjectNamePanel");

            if (panel != null)
            {
                GameObject objectName = FindChildByName(panel.transform, "ObjectName");

                if (objectName != null)
                    objectNameRect = objectName.GetComponent<RectTransform>();
            }
        }

        if (objectNameText == null && objectNameRect != null)
            objectNameText = objectNameRect.GetComponent<TMP_Text>() ?? objectNameRect.GetComponentInChildren<TMP_Text>(true);

        if (objectNameRect != null)
        {
            if (targetCanvas == null)
                targetCanvas = objectNameRect.GetComponentInParent<Canvas>();

            if (targetCanvas != null && canvasRect == null)
                canvasRect = targetCanvas.transform as RectTransform;
        }

        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    private static GameObject FindSceneObjectByName(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform item = transforms[i];

            if (item == null)
                continue;

            if (!item.gameObject.scene.IsValid())
                continue;

            if (item.name == objectName)
                return item.gameObject;
        }

        return null;
    }

    private static GameObject FindChildByName(Transform root, string childName)
    {
        if (root == null)
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];

            if (child != null && child.name == childName)
                return child.gameObject;
        }

        return null;
    }
}
