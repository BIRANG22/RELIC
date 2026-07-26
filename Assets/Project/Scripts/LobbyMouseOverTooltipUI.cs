using TMPro;
using UnityEngine;

/// <summary>
/// 로비 월드 오브젝트에 마우스를 올렸을 때 표시되는 공용 툴팁입니다.
/// Canvas 안의 MouseOver_Tooltip 오브젝트에 부착합니다.
/// </summary>
public sealed class LobbyMouseOverTooltipUI : MonoBehaviour
{
    private static readonly string[] DefaultBlockingPanelNames =
    {
        "CharacterSettingPanel",
        "RelicShopPanel",
        "CultureTankPanel",
        "ErosionSelectPanel"
    };

    private static LobbyMouseOverTooltipUI instance;

    [Header("UI 연결")]
    [Tooltip("툴팁 전체 RectTransform입니다. 비워두면 이 오브젝트의 RectTransform을 사용합니다.")]
    [SerializeField] private RectTransform tooltipRoot;

    [Tooltip("문구를 표시하는 TextMeshProUGUI입니다.")]
    [SerializeField] private TMP_Text tooltipText;

    [Tooltip("툴팁이 배치된 Canvas입니다. 비워두면 부모에서 자동으로 찾습니다.")]
    [SerializeField] private Canvas targetCanvas;

    [Header("패널 표시 중 차단")]
    [Tooltip("이 패널 중 하나라도 활성화되어 있으면 월드 오브젝트 툴팁을 표시하지 않습니다. 비워두면 이름으로 자동 탐색합니다.")]
    [SerializeField] private GameObject[] blockingPanels;

    [Header("마우스 위치")]
    [Tooltip("마우스 커서의 우측 아래에서 떨어질 거리입니다.")]
    [SerializeField] private Vector2 cursorOffset = new Vector2(24f, -24f);

    [Tooltip("화면 가장자리에서 유지할 여백입니다.")]
    [SerializeField] private Vector2 screenPadding = new Vector2(12f, 12f);

    private RectTransform canvasRectTransform;
    private CanvasGroup canvasGroup;
    private Object currentOwner;
    private bool isVisible;

    public static LobbyMouseOverTooltipUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<LobbyMouseOverTooltipUI>(FindObjectsInactive.Include);
            }

            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("[LobbyMouseOverTooltipUI] 씬에 공용 툴팁이 두 개 이상 있습니다.", this);
        }

        instance = this;
        ResolveReferences();
        ResolveBlockingPanels();
        SetVisible(false);
    }

    private void OnEnable()
    {
        ResolveReferences();
        ResolveBlockingPanels();
        SetVisible(false);
    }

    private void OnDisable()
    {
        currentOwner = null;
        isVisible = false;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void LateUpdate()
    {
        if (IsBlockedByOpenPanel())
        {
            if (isVisible || currentOwner != null)
            {
                HideImmediately();
            }

            return;
        }

        if (!isVisible)
        {
            return;
        }

        UpdateTooltipPosition(Input.mousePosition);
    }

    public void Show(Object owner, string message)
    {
        if (IsBlockedByOpenPanel())
        {
            HideImmediately();
            return;
        }

        if (owner == null || string.IsNullOrWhiteSpace(message))
        {
            Hide(owner);
            return;
        }

        ResolveReferences();

        if (tooltipRoot == null || tooltipText == null || canvasRectTransform == null)
        {
            Debug.LogWarning("[LobbyMouseOverTooltipUI] Tooltip Root, Tooltip Text, Canvas 연결을 확인해주세요.", this);
            return;
        }

        currentOwner = owner;
        tooltipText.text = message;
        SetVisible(true);

        Canvas.ForceUpdateCanvases();
        UpdateTooltipPosition(Input.mousePosition);
    }

    public void Hide(Object owner)
    {
        if (owner != null && currentOwner != null && owner != currentOwner)
        {
            return;
        }

        currentOwner = null;
        SetVisible(false);
    }

    public void HideImmediately()
    {
        currentOwner = null;
        SetVisible(false);
    }

    private bool IsBlockedByOpenPanel()
    {
        ResolveBlockingPanels();

        if (blockingPanels == null)
        {
            return false;
        }

        for (int i = 0; i < blockingPanels.Length; i++)
        {
            GameObject panel = blockingPanels[i];
            if (panel != null && panel.activeInHierarchy)
            {
                return true;
            }
        }

        return false;
    }

    private void ResolveBlockingPanels()
    {
        if (blockingPanels != null && blockingPanels.Length > 0)
        {
            return;
        }

        GameObject[] sceneObjects = FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        blockingPanels = new GameObject[DefaultBlockingPanelNames.Length];

        for (int nameIndex = 0; nameIndex < DefaultBlockingPanelNames.Length; nameIndex++)
        {
            string targetName = DefaultBlockingPanelNames[nameIndex];

            for (int objectIndex = 0; objectIndex < sceneObjects.Length; objectIndex++)
            {
                GameObject candidate = sceneObjects[objectIndex];
                if (candidate != null && candidate.name == targetName)
                {
                    blockingPanels[nameIndex] = candidate;
                    break;
                }
            }
        }
    }

    private void ResolveReferences()
    {
        if (tooltipRoot == null)
        {
            tooltipRoot = transform as RectTransform;
        }

        if (tooltipText == null)
        {
            Transform tooltipChild = transform.Find("Tooltip");
            if (tooltipChild != null)
            {
                tooltipText = tooltipChild.GetComponent<TMP_Text>();
            }

            if (tooltipText == null)
            {
                tooltipText = GetComponentInChildren<TMP_Text>(true);
            }
        }

        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInParent<Canvas>();
        }

        if (targetCanvas != null)
        {
            canvasRectTransform = targetCanvas.transform as RectTransform;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (tooltipRoot != null)
        {
            tooltipRoot.pivot = new Vector2(0f, 1f);
        }
    }

    private void SetVisible(bool visible)
    {
        isVisible = visible;

        if (canvasGroup == null)
        {
            ResolveReferences();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void UpdateTooltipPosition(Vector2 screenPosition)
    {
        if (tooltipRoot == null || canvasRectTransform == null || targetCanvas == null)
        {
            return;
        }

        Camera eventCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : targetCanvas.worldCamera;

        Vector2 desiredScreenPosition = screenPosition + cursorOffset;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform,
                desiredScreenPosition,
                eventCamera,
                out Vector2 localPoint))
        {
            return;
        }

        tooltipRoot.anchoredPosition = ClampToCanvas(localPoint);
    }

    private Vector2 ClampToCanvas(Vector2 desiredPosition)
    {
        Rect canvasRect = canvasRectTransform.rect;
        Rect tooltipRect = tooltipRoot.rect;

        float minX = canvasRect.xMin + screenPadding.x;
        float maxX = canvasRect.xMax - tooltipRect.width - screenPadding.x;

        float minY = canvasRect.yMin + tooltipRect.height + screenPadding.y;
        float maxY = canvasRect.yMax - screenPadding.y;

        desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
        desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);

        return desiredPosition;
    }
}
