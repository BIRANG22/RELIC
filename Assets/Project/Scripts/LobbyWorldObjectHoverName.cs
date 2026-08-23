using TMPro;
using UnityEngine;

public sealed class LobbyWorldObjectHoverName : MonoBehaviour
{
    public enum HoverNameType
    {
        Research,
        Exploration,
        Resonance,
        Npc
    }

    [Header("Hover Name")]
    [SerializeField] private HoverNameType hoverNameType = HoverNameType.Research;

    [Header("Position")]
    [Tooltip("콜라이더 하단에서 이름 UI까지의 화면 픽셀 간격입니다. 음수값이면 콜라이더 아래쪽으로 내려갑니다.")]
    [SerializeField] private float screenYOffset = -18f;

    [Header("Blocking Panels")]
    [Tooltip("등록된 패널 중 하나라도 활성화되어 있으면 월드 오브젝트 이름을 숨깁니다.")]
    [SerializeField] private GameObject[] blockingPanels;

    [Tooltip("활성화하면 로비에서 자주 사용하는 모달 패널을 이름으로 자동 감지합니다.")]
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

    private static LobbyWorldObjectHoverName currentOwner;

    private GameObject worldObjectNamePanel;
    private RectTransform worldObjectNamePanelRect;
    private Canvas targetCanvas;
    private RectTransform positionRoot;
    private Camera worldCamera;
    private Collider2D targetCollider;
    private GameObject[] commonBlockingPanels;
    private bool isHovered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        currentOwner = null;
    }

    private void Awake()
    {
        AutoBindIfNeeded();
        isHovered = false;

        if (currentOwner == null)
            SetNameVisible(false);
    }

    private void OnEnable()
    {
        AutoBindIfNeeded();
        isHovered = false;
    }

    private void OnDisable()
    {
        isHovered = false;

        if (currentOwner == this)
        {
            SetNameVisible(false);
            currentOwner = null;
        }
    }

    private void OnMouseEnter()
    {
        AutoBindIfNeeded();

        if (targetCollider == null || objectNameText == null || worldObjectNamePanel == null)
            return;

        if (IsAnyBlockingPanelActive())
            return;

        currentOwner = this;
        isHovered = true;
        objectNameText.text = GetDisplayName();
        UpdateNamePosition();
        SetNameVisible(true);
    }

    private void OnMouseExit()
    {
        isHovered = false;

        if (currentOwner != this)
            return;

        SetNameVisible(false);
        currentOwner = null;
    }

    private void LateUpdate()
    {
        if (!isHovered || currentOwner != this)
            return;

        if (IsAnyBlockingPanelActive())
        {
            isHovered = false;
            SetNameVisible(false);
            currentOwner = null;
            return;
        }

        UpdateNamePosition();
    }

    private void UpdateNamePosition()
    {
        AutoBindIfNeeded();

        if (worldObjectNamePanelRect == null || targetCollider == null || targetCanvas == null || positionRoot == null)
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
            if (currentOwner == this)
            {
                SetNameVisible(false);
                currentOwner = null;
                isHovered = false;
            }

            return;
        }

        screenPoint.y += screenYOffset;

        Camera uiCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : targetCanvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(
                positionRoot,
                screenPoint,
                uiCamera,
                out Vector3 worldPoint))
        {
            return;
        }

        // ObjectName의 로컬 X/Y는 건드리지 않고 부모 패널만 콜라이더 하단으로 이동합니다.
        worldObjectNamePanelRect.position = worldPoint;
    }

    private void SetNameVisible(bool visible)
    {
        if (worldObjectNamePanel != null && worldObjectNamePanel.activeSelf != visible)
            worldObjectNamePanel.SetActive(visible);
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

            case HoverNameType.Npc:
                return "연구원 엘릭";

            default:
                return string.Empty;
        }
    }

    private bool IsAnyBlockingPanelActive()
    {
        // 실제로 로비 모달 입력 차단기가 활성화된 경우에만 월드 호버를 차단합니다.
        // 패널 오브젝트 이름만으로 상태를 추정하지 않아, 닫힌 Storage/Equip 때문에 호버가 막히지 않습니다.
        if (LobbyPositionModalInputBlocker.IsBlocked)
            return true;

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

        if (worldObjectNamePanel == null)
            worldObjectNamePanel = FindSceneObjectByName("WorldObjectNamePanel");

        if (worldObjectNamePanelRect == null && worldObjectNamePanel != null)
            worldObjectNamePanelRect = worldObjectNamePanel.GetComponent<RectTransform>();

        if (objectNameRect == null && worldObjectNamePanel != null)
        {
            GameObject objectName = FindChildByName(worldObjectNamePanel.transform, "ObjectName");

            if (objectName != null)
                objectNameRect = objectName.GetComponent<RectTransform>();
        }

        if (objectNameText == null && objectNameRect != null)
            objectNameText = objectNameRect.GetComponent<TMP_Text>() ?? objectNameRect.GetComponentInChildren<TMP_Text>(true);

        if (worldObjectNamePanelRect != null)
        {
            if (targetCanvas == null)
                targetCanvas = worldObjectNamePanelRect.GetComponentInParent<Canvas>();

            if (positionRoot == null)
                positionRoot = worldObjectNamePanelRect.parent as RectTransform;
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
