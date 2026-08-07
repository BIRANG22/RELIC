using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LobbyCultureTankPanelPresenter : MonoBehaviour
{
    private const float RefreshIntervalSeconds = 0.25f;

    [Header("Panel")]
    [Tooltip("PositionPanel 안에 미리 배치한 CultureTankPanel입니다. 비어 있으면 이 컴포넌트가 붙은 오브젝트를 사용합니다.")]
    [SerializeField] private GameObject panelRoot;

    [Tooltip("CultureTankPanel의 BackButton입니다.")]
    [SerializeField] private Button backButton;

    [Tooltip("배양조 행들이 들어 있는 Content입니다.")]
    [SerializeField] private RectTransform contentRoot;

    [Tooltip("배양조가 없을 때 표시할 텍스트입니다. 사용하지 않으면 비워 둬도 됩니다.")]
    [SerializeField] private TMP_Text emptyText;

    [Header("Inventory")]
    [SerializeField] private Transform inventoryItemRoot;

    [Header("Rows")]
    [Tooltip("Content 아래에 미리 만든 CultureTankRow_1~3을 순서대로 연결합니다.")]
    [SerializeField] private TankRow[] rows = new TankRow[3];

    [Header("Search")]
    [SerializeField] private string tankNamePrefix = "CultureTank";

    private float nextRefreshAt;
    private bool backButtonBound;
    private readonly List<InventorySlot> inventorySlots = new();
    private LobbyCultureTankController selectedTank;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        BindSceneObjects();
        BindBackButton();
        BindInventorySlots();
    }

    private void OnEnable()
    {
        BindSceneObjects();
        BindBackButton();
        BindInventorySlots();
        RefreshRows();
        RefreshInventory();
        nextRefreshAt = Time.unscaledTime + RefreshIntervalSeconds;
    }

    private void Update()
    {
        // ESC나 다른 스크립트에서 패널을 직접 비활성화한 경우에도
        // 월드 오브젝트 입력 차단 상태가 남지 않도록 자동으로 해제합니다.
        if (!IsOpen)
        {
            if (LobbyPositionModalInputBlocker.IsBlockedBy(this))
                LobbyPositionModalInputBlocker.Unblock(this);

            return;
        }

        if (Time.unscaledTime < nextRefreshAt)
            return;

        RefreshRows();
        RefreshInventory();
        nextRefreshAt = Time.unscaledTime + RefreshIntervalSeconds;
    }

    public void Open()
    {
        // 침식도 선택창이나 유물 상점처럼 다른 위치 모달이 열려 있으면
        // 배양조 패널을 중복으로 열지 않습니다.
        if (LobbyPositionModalInputBlocker.IsBlockedByAnother(this))
            return;

        BindSceneObjects();
        BindBackButton();

        if (panelRoot == null)
        {
            Debug.LogWarning("[LobbyCultureTankPanelPresenter] CultureTankPanel이 연결되지 않았습니다.");
            return;
        }

        // CultureTankPanel이 열려 있는 동안 Statue, relic_stone, Researcher 등
        // 뒤쪽 월드 오브젝트가 클릭되지 않도록 입력을 차단합니다.
        LobbyPositionModalInputBlocker.Block(this);

        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();
        RefreshRows();
        RefreshInventory();
        nextRefreshAt = Time.unscaledTime + RefreshIntervalSeconds;
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        selectedTank = null;
        LobbyPositionModalInputBlocker.Unblock(this);
    }

    private void OnDisable()
    {
        LobbyPositionModalInputBlocker.Unblock(this);
    }

    private void OnDestroy()
    {
        LobbyPositionModalInputBlocker.Unblock(this);
    }

    private void RefreshRows()
    {
        BindSceneObjects();

        List<LobbyCultureTankController> tanks = FindCultureTanks();
        bool hasTanks = tanks.Count > 0;

        if (emptyText != null)
            emptyText.gameObject.SetActive(!hasTanks);

        if (rows == null)
            return;

        for (int i = 0; i < rows.Length; i++)
        {
            TankRow row = rows[i];
            if (row == null || row.Root == null)
                continue;

            bool hasTank = i < tanks.Count;
            row.Root.SetActive(hasTank);

            if (!hasTank)
            {
                row.Controller = null;
                if (row.Button != null)
                    row.Button.onClick.RemoveAllListeners();
                continue;
            }

            LobbyCultureTankController tank = tanks[i];
            row.Controller = tank;

            if (row.Label != null)
                row.Label.text = tank.GetPanelName();

            if (row.StateLabel != null)
                row.StateLabel.text = tank.GetPanelStateText();

            LobbyCultureTankPanelState state = tank.GetPanelState();
            UpdateRowItemIcon(row, tank, state);

            if (row.Background != null)
                row.Background.color = GetRowColor(state);

            if (row.Button != null)
            {
                row.Button.onClick.RemoveAllListeners();
                row.Button.interactable = CanLocalPlayerMutateHostOnlyState();
                LobbyCultureTankController clickedTank = tank;
                row.Button.onClick.AddListener(() => InteractWithTank(clickedTank));
            }
        }
    }

    private void InteractWithTank(LobbyCultureTankController tank)
    {
        if (tank == null)
            return;

        if (tank.GetPanelState() == LobbyCultureTankPanelState.Empty)
        {
            selectedTank = tank;
            RefreshInventory();
            return;
        }

        selectedTank = null;
        tank.Interact();
        tank.RefreshNow();
        RefreshRows();
        RefreshInventory();
    }

    private List<LobbyCultureTankController> FindCultureTanks()
    {
        var result = new List<LobbyCultureTankController>();
        LobbyCultureTankController[] tanks = FindObjectsByType<LobbyCultureTankController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < tanks.Length; i++)
        {
            LobbyCultureTankController tank = tanks[i];
            if (tank == null)
                continue;

            string id = tank.TankId;
            string objectName = tank.name;
            if (!StartsWithTankPrefix(id) && !StartsWithTankPrefix(objectName))
                continue;

            result.Add(tank);
        }

        result.Sort((left, right) => string.Compare(left.TankId, right.TankId, StringComparison.Ordinal));
        return result;
    }

    private bool StartsWithTankPrefix(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string prefix = string.IsNullOrWhiteSpace(tankNamePrefix)
            ? "CultureTank"
            : tankNamePrefix.Trim();

        return value.Trim().StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private void BindSceneObjects()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        Transform panelTransform = panelRoot.transform;

        if (backButton == null)
        {
            Transform target = FindDirectOrNestedChild(panelTransform, "BackButton");
            if (target != null)
                backButton = target.GetComponent<Button>();
        }

        if (contentRoot == null)
        {
            Transform target = FindDirectOrNestedChild(panelTransform, "Content");
            if (target != null)
                contentRoot = target as RectTransform;
        }

        if (inventoryItemRoot == null)
        {
            Transform inventory = FindDirectOrNestedChild(panelTransform, "inventory");
            inventoryItemRoot = FindDirectOrNestedChild(inventory, "item");
        }

        if (rows == null || rows.Length != 3)
            rows = new TankRow[3];

        if (contentRoot == null)
            return;

        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] ??= new TankRow();
            if (rows[i].Root != null)
            {
                rows[i].BindMissingComponents();
                continue;
            }

            Transform rowTransform = FindInteractiveRow(contentRoot, $"CultureTankRow_{i + 1}");
            if (rowTransform == null)
                continue;

            rows[i].Root = rowTransform.gameObject;
            rows[i].BindMissingComponents();
        }
    }

    private static Transform FindInteractiveRow(Transform root, string rowName)
    {
        if (root == null)
            return null;

        Transform fallback = null;
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == null || !string.Equals(child.name, rowName, StringComparison.Ordinal))
                continue;

            fallback ??= child;
            if (child.GetComponent<Button>() != null)
                return child;
        }

        return fallback;
    }

    private static void UpdateRowItemIcon(
        TankRow row,
        LobbyCultureTankController tank,
        LobbyCultureTankPanelState state)
    {
        if (row?.ItemIcon == null)
            return;

        Sprite icon = null;
        string itemId = string.Empty;
        bool shouldShow = ShouldShowRowItemIcon(state) &&
                          tank != null &&
                          tank.TryGetPanelItemId(out itemId);

        ItemIconDatabase iconDatabase = DataManager.Instance?.ItemIconDatabase;
        if (shouldShow && iconDatabase != null)
        {
            if (state == LobbyCultureTankPanelState.Completed)
                shouldShow = iconDatabase.TryGetResearchResultIcon(itemId, out icon) ||
                             iconDatabase.TryGetIcon(itemId, out icon);
            else
                shouldShow = iconDatabase.TryGetIcon(itemId, out icon);
        }
        else
        {
            shouldShow = false;
        }

        row.ItemIcon.sprite = shouldShow ? icon : null;
        row.ItemIcon.enabled = shouldShow;
        row.ItemIcon.preserveAspect = true;
    }

    public static bool ShouldShowRowItemIcon(LobbyCultureTankPanelState state)
    {
        return state == LobbyCultureTankPanelState.Running ||
               state == LobbyCultureTankPanelState.Completed;
    }

    private void BindInventorySlots()
    {
        BindSceneObjects();
        if (inventoryItemRoot == null || inventorySlots.Count == inventoryItemRoot.childCount)
            return;

        inventorySlots.Clear();
        for (int i = 0; i < inventoryItemRoot.childCount; i++)
        {
            Transform child = inventoryItemRoot.GetChild(i);
            Image image = child.GetComponent<Image>();
            if (image == null)
                continue;

            Button button = child.GetComponent<Button>();
            if (button == null)
                button = child.gameObject.AddComponent<Button>();

            button.targetGraphic = image;
            inventorySlots.Add(new InventorySlot(image, button));
        }
    }

    private void RefreshInventory()
    {
        BindInventorySlots();
        LobbyRuntimeData lobby = DataManager.Instance != null
            ? DataManager.Instance.LobbyRuntimeStore?.GetOrCreate()
            : null;
        IReadOnlyList<string> itemIds = lobby?.BagItemIds;
        LobbyCultureTankController availableTank = selectedTank ?? FindFirstEmptyTank();
        bool canSelect = availableTank != null &&
                         availableTank.GetPanelState() == LobbyCultureTankPanelState.Empty &&
                         CanLocalPlayerMutateHostOnlyState();

        for (int i = 0; i < inventorySlots.Count; i++)
        {
            InventorySlot slot = inventorySlots[i];
            string itemId = itemIds != null && i < itemIds.Count ? itemIds[i]?.Trim() : string.Empty;
            Sprite icon = null;
            if (!string.IsNullOrEmpty(itemId) && DataManager.Instance?.ItemIconDatabase != null)
                DataManager.Instance.ItemIconDatabase.TryGetIcon(itemId, out icon);

            slot.SetItem(itemId, icon, canSelect && !string.IsNullOrEmpty(itemId), SelectInventoryItem);
        }
    }

    private void SelectInventoryItem(string itemId)
    {
        LobbyCultureTankController tank = selectedTank ?? FindFirstEmptyTank();
        if (tank == null || string.IsNullOrWhiteSpace(itemId))
            return;

        if (!tank.TryStartResearchFromPanel(itemId))
            return;

        selectedTank = null;
        RefreshRows();
        RefreshInventory();
    }

    private LobbyCultureTankController FindFirstEmptyTank()
    {
        List<LobbyCultureTankController> tanks = FindCultureTanks();
        for (int i = 0; i < tanks.Count; i++)
        {
            LobbyCultureTankController tank = tanks[i];
            if (tank != null && tank.GetPanelState() == LobbyCultureTankPanelState.Empty)
                return tank;
        }

        return null;
    }

    private void BindBackButton()
    {
        if (backButton == null || backButtonBound)
            return;

        backButton.onClick.RemoveListener(Close);
        backButton.onClick.AddListener(Close);
        backButtonBound = true;
    }

    private static Transform FindDirectOrNestedChild(Transform root, string objectName)
    {
        if (root == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && string.Equals(child.name, objectName, StringComparison.Ordinal))
                return child;
        }

        return null;
    }

    private static Color GetRowColor(LobbyCultureTankPanelState state)
    {
        return state switch
        {
            LobbyCultureTankPanelState.Completed => new Color(0.08f, 0.38f, 0.24f, 0.96f),
            LobbyCultureTankPanelState.Running => new Color(0.48f, 0.24f, 0.08f, 0.96f),
            LobbyCultureTankPanelState.MissingData => new Color(0.33f, 0.11f, 0.11f, 0.96f),
            _ => new Color(0.16f, 0.18f, 0.22f, 0.96f)
        };
    }

    private static bool CanLocalPlayerMutateHostOnlyState()
    {
        SteamLobbySharedStateSynchronizer synchronizer =
            SteamLobbySharedStateSynchronizer.Instance;
        return synchronizer == null ||
               synchronizer.CanLocalPlayerMutateHostOnlyState();
    }

    [Serializable]
    private sealed class TankRow
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image background;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text stateLabel;
        [SerializeField] private Image itemIcon;

        public GameObject Root
        {
            get => root;
            set => root = value;
        }

        public Image Background => background;
        public Button Button => button;
        public TMP_Text Label => label;
        public TMP_Text StateLabel => stateLabel;
        public Image ItemIcon => itemIcon;
        public LobbyCultureTankController Controller { get; set; }

        public void BindMissingComponents()
        {
            if (root == null)
                return;

            if (background == null)
                background = root.GetComponent<Image>();

            if (button == null)
                button = root.GetComponent<Button>();

            if (label == null)
            {
                Transform labelTransform = FindDirectOrNestedChild(root.transform, "Label");
                label = labelTransform != null
                    ? labelTransform.GetComponent<TMP_Text>()
                    : root.GetComponentInChildren<TMP_Text>(true);
            }

            if (stateLabel == null)
            {
                Transform stateTransform = FindDirectOrNestedChild(root.transform, "StateLabel");
                if (stateTransform != null)
                    stateLabel = stateTransform.GetComponent<TMP_Text>();
            }

            if (stateLabel == null && label != null)
            {
                GameObject stateObject = Instantiate(label.gameObject, root.transform);
                stateObject.name = "StateLabel";
                stateLabel = stateObject.GetComponent<TMP_Text>();

                if (label.transform is RectTransform nameRect &&
                    stateLabel.transform is RectTransform stateRect)
                {
                    Vector2 origin = nameRect.anchoredPosition;
                    nameRect.anchoredPosition = origin + new Vector2(0f, 24f);
                    stateRect.anchoredPosition = origin - new Vector2(0f, 24f);
                }
            }

            if (itemIcon == null)
            {
                Transform iconTransform = FindDirectOrNestedChild(root.transform, "ItemIcon");
                if (iconTransform != null)
                    itemIcon = iconTransform.GetComponent<Image>();
            }

            if (itemIcon == null)
            {
                GameObject iconObject = new("ItemIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.layer = root.layer;
                iconObject.transform.SetParent(root.transform, false);
                itemIcon = iconObject.GetComponent<Image>();
                itemIcon.raycastTarget = false;
                itemIcon.enabled = false;
                itemIcon.preserveAspect = true;

                RectTransform iconRect = (RectTransform)iconObject.transform;
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = new Vector2(0f, 12f);
                iconRect.sizeDelta = new Vector2(88f, 88f);
                iconRect.SetAsLastSibling();
            }
        }
    }

    private sealed class InventorySlot
    {
        private readonly Image image;
        private readonly Button button;
        private readonly Sprite emptySprite;
        private readonly Color emptyColor;
        private string itemId;
        private Action<string> selected;

        public InventorySlot(Image image, Button button)
        {
            this.image = image;
            this.button = button;
            emptySprite = image.sprite;
            emptyColor = image.color;
        }

        public void SetItem(string newItemId, Sprite icon, bool interactable, Action<string> callback)
        {
            itemId = newItemId;
            selected = callback;
            bool showsIcon = !string.IsNullOrEmpty(itemId) && icon != null;

            image.sprite = showsIcon ? icon : emptySprite;
            image.color = showsIcon ? Color.white : emptyColor;
            image.preserveAspect = showsIcon;

            button.onClick.RemoveListener(InvokeSelection);
            button.onClick.AddListener(InvokeSelection);
            button.interactable = interactable;
        }

        private void InvokeSelection()
        {
            if (button.interactable && !string.IsNullOrEmpty(itemId))
                selected?.Invoke(itemId);
        }
    }
}
