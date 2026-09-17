using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public sealed class LobbyWorldObjectHoverName : MonoBehaviour
{
    public enum HoverNameType
    {
        Research,
        Exploration,
        Resonance,
        Npc,
        Storage
    }

    [Header("Hover Name")]
    [SerializeField] private HoverNameType hoverNameType = HoverNameType.Research;

    [Header("Storage Interaction")]
    [Tooltip("Storage 월드 오브젝트 클릭 시 BackgroundPanel과 함께 활성화할 StoragePanel입니다. 비워두면 씬의 StoragePanel을 자동으로 찾습니다.")]
    [SerializeField] private GameObject storagePanel;

    [Header("Position")]
    [Tooltip("콜라이더 하단에서 이름 UI까지의 화면 픽셀 간격입니다. 음수값이면 콜라이더 아래쪽으로 내려갑니다.")]
    [SerializeField] private float screenYOffset = -18f;

    [Header("Name Background Size")]
    [Tooltip("WorldObjectNamePanel/Image의 가로 크기를 이름 길이에 맞춰 자동 조절합니다.")]
    [SerializeField] private RectTransform backgroundRect;

    [Tooltip("텍스트 좌우에 추가할 여백입니다. 실제 좌우 여백은 이 값씩 적용됩니다.")]
    [SerializeField] private float backgroundSidePadding = 20f;

    [Tooltip("이름이 짧아도 유지할 배경의 최소 가로 크기입니다.")]
    [SerializeField] private float backgroundMinWidth = 100f;

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
        EnsureDynamicTextOwnership();
        DisablePanelRaycasts();
        isHovered = false;

        if (currentOwner == null)
            SetNameVisible(false);
    }

    private void OnEnable()
    {
        AutoBindIfNeeded();
        EnsureDynamicTextOwnership();
        DisablePanelRaycasts();
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

    private void OnDestroy()
    {
        LobbyPositionSharedModalBackground.HideForOwner(this);
        LobbyPositionModalInputBlocker.Unblock(this);
    }

    private void OnMouseEnter()
    {
        AutoBindIfNeeded();

        if (targetCollider == null || objectNameText == null || worldObjectNamePanel == null)
            return;

        if (IsAnyBlockingPanelActive())
            return;

        ShowCurrentName();
    }

    private void OnMouseOver()
    {
        if (IsAnyBlockingPanelActive())
            return;

        AutoBindIfNeeded();
        if (targetCollider == null || objectNameText == null || worldObjectNamePanel == null)
            return;

        ShowCurrentName();
    }


    private void OnMouseUpAsButton()
    {
        if (hoverNameType != HoverNameType.Storage)
            return;

        // CharacterSettingPanel 같은 전면 모달이 열려 있으면 Storage 월드 오브젝트 클릭을 무시합니다.
        // 공용 모달 차단 상태와 실제 활성 패널 상태를 함께 확인해, 활성화 프레임 순서와 관계없이
        // 뒤쪽 월드 오브젝트가 눌리지 않도록 합니다.
        if (LobbyPositionModalInputBlocker.IsBlocked || IsAnyBlockingPanelActive())
            return;

        OpenStoragePanel();
    }

    private void OpenStoragePanel()
    {
        if (storagePanel == null)
            storagePanel = FindSceneObjectByName("StoragePanel");

        if (storagePanel == null)
        {
            Debug.LogWarning(
                "[LobbyWorldObjectHoverName] StoragePanel을 찾을 수 없습니다. Storage 월드 오브젝트의 Storage Panel을 연결해 주세요.",
                this);
            return;
        }

        if (LobbyPositionSharedModalBackground.IsPanelPresented(storagePanel))
            return;

        LobbyPositionModalInputBlocker.Block(this);

        // StoragePanel은 이전 이동형 패널과 달리 공용 PositionPanel 모달로 사용합니다.
        // 먼저 패널 Canvas를 활성화한 뒤 BackgroundPanel의 Blur/Presentation 정렬을 요청해야
        // 첫 진입에서도 SharedBlurCanvas 위에 정상적으로 표시됩니다.
        storagePanel.SetActive(true);
        Canvas.ForceUpdateCanvases();
        LobbyPositionSharedModalBackground.ShowForPanel(storagePanel, this, CloseStoragePanel);

        if (UIBlurBackgroundManager.HasInstance)
            UIBlurBackgroundManager.Instance.RefreshPresentation();
    }

    private void CloseStoragePanel()
    {
        if (storagePanel != null)
            storagePanel.SetActive(false);

        LobbyPositionSharedModalBackground.HideForOwner(this);
        LobbyPositionModalInputBlocker.Unblock(this);
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

        ShowCurrentName();
    }

    private void ShowCurrentName()
    {
        currentOwner = this;
        isHovered = true;
        objectNameText.text = GetDisplayName();
        RefreshBackgroundWidth();
        UpdateNamePosition();
        SetNameVisible(true);
    }

    private void RefreshBackgroundWidth()
    {
        AutoBindIfNeeded();

        if (backgroundRect == null || objectNameText == null)
            return;

        objectNameText.ForceMeshUpdate();
        float preferredWidth = objectNameText.GetPreferredValues(objectNameText.text).x;
        float targetWidth = Mathf.Max(backgroundMinWidth, preferredWidth + backgroundSidePadding * 2f);

        // 기존 Middle Center 앵커/피벗과 높이는 유지하고 가로 크기만 변경합니다.
        backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
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
        string key = GetDisplayNameKey(hoverNameType);
        return GameLocalization.Get(key, GetKoreanDisplayName(hoverNameType));
    }

    public static string GetDisplayNameKey(HoverNameType type)
    {
        return type switch
        {
            HoverNameType.Research => "lobby.world_object.workbench",
            HoverNameType.Exploration => "lobby.world_object.statue",
            HoverNameType.Resonance => "lobby.world_object.stela",
            HoverNameType.Npc => "lobby.world_object.researcher_elric",
            HoverNameType.Storage => "lobby.world_object.storage",
            _ => string.Empty
        };
    }

    private static string GetKoreanDisplayName(HoverNameType type)
    {
        return type switch
        {
            HoverNameType.Research => "\uC791\uC5C5\uB300",
            HoverNameType.Exploration => "\uC870\uAC01\uC0C1",
            HoverNameType.Resonance => "\uBE44\uC11D",
            HoverNameType.Npc => "\uC5F0\uAD6C\uC6D0 \uC5D8\uB9AD",
            HoverNameType.Storage => "\uBCF4\uAD00\uD568",
            _ => string.Empty
        };
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

        if (backgroundRect == null && worldObjectNamePanel != null)
        {
            GameObject background = FindChildByName(worldObjectNamePanel.transform, "Image");

            if (background != null)
                backgroundRect = background.GetComponent<RectTransform>();
        }

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

    private void EnsureDynamicTextOwnership()
    {
        if (objectNameText == null)
            return;

        if (objectNameText.GetComponent<LocalizationIgnore>() == null)
            objectNameText.gameObject.AddComponent<LocalizationIgnore>();

        LocalizedTMPText localizer = objectNameText.GetComponent<LocalizedTMPText>();
        if (localizer != null)
        {
            localizer.enabled = false;
            Destroy(localizer);
        }

        LocalizeStringEvent legacyLocalizer = objectNameText.GetComponent<LocalizeStringEvent>();
        if (legacyLocalizer != null)
        {
            legacyLocalizer.enabled = false;
            Destroy(legacyLocalizer);
        }
    }

    private void DisablePanelRaycasts()
    {
        if (worldObjectNamePanel == null)
            return;

        foreach (Graphic graphic in worldObjectNamePanel.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
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
