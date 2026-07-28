using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LobbyCultureTankPanelPresenter : MonoBehaviour
{
    private const float RefreshIntervalSeconds = 0.25f;

    [Header("Panel")]
    [Tooltip("PositionPanel ?ˆì— ë¯¸ë¦¬ ë°°ì¹˜??CultureTankPanel?…ë‹ˆ?? ë¹„ì–´ ?ˆìœ¼ë©???ì»´í¬?ŒíŠ¸ê°€ ë¶™ì? ?¤ë¸Œ?íŠ¸ë¥??¬ìš©?©ë‹ˆ??")]
    [SerializeField] private GameObject panelRoot;

    [Tooltip("CultureTankPanel??BackButton?…ë‹ˆ??")]
    [SerializeField] private Button backButton;

    [Tooltip("ë°°ì–‘ì¡??‰ë“¤???¤ì–´ ?ˆëŠ” Content?…ë‹ˆ??")]
    [SerializeField] private RectTransform contentRoot;

    [Tooltip("ë°°ì–‘ì¡°ê? ?†ì„ ???œì‹œ???ìŠ¤?¸ìž…?ˆë‹¤. ?¬ìš©?˜ì? ?Šìœ¼ë©?ë¹„ì›Œ???©ë‹ˆ??")]
    [SerializeField] private TMP_Text emptyText;

    [Header("Rows")]
    [Tooltip("Content ?„ëž˜??ë¯¸ë¦¬ ë§Œë“¤????CultureTankRow_1~3???œì„œ?€ë¡??°ê²°?©ë‹ˆ??")]
    [SerializeField] private TankRow[] rows = new TankRow[3];

    [Header("Search")]
    [SerializeField] private string tankNamePrefix = "CultureTank";

    private float nextRefreshAt;
    private bool backButtonBound;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        BindSceneObjects();
        BindBackButton();
    }

    private void OnEnable()
    {
        BindSceneObjects();
        BindBackButton();
        RefreshRows();
        nextRefreshAt = Time.unscaledTime + RefreshIntervalSeconds;
    }

    private void Update()
    {
        if (!IsOpen || Time.unscaledTime < nextRefreshAt)
            return;

        RefreshRows();
        nextRefreshAt = Time.unscaledTime + RefreshIntervalSeconds;
    }

    public void Open()
    {
        BindSceneObjects();
        BindBackButton();

        if (panelRoot == null)
        {
            Debug.LogWarning("[LobbyCultureTankPanelPresenter] CultureTankPanel???°ê²°?˜ì? ?Šì•˜?µë‹ˆ??");
            return;
        }

        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();
        RefreshRows();
        nextRefreshAt = Time.unscaledTime + RefreshIntervalSeconds;
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
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
                row.Label.text = tank.GetPanelLabel();

            if (row.Background != null)
                row.Background.color = GetRowColor(tank.GetPanelState());

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

        tank.Interact();
        tank.RefreshNow();
        RefreshRows();
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

            Transform rowTransform = FindDirectOrNestedChild(contentRoot, $"CultureTankRow_{i + 1}");
            if (rowTransform == null)
                continue;

            rows[i].Root = rowTransform.gameObject;
            rows[i].BindMissingComponents();
        }
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

        public GameObject Root
        {
            get => root;
            set => root = value;
        }

        public Image Background => background;
        public Button Button => button;
        public TMP_Text Label => label;
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
                label = root.GetComponentInChildren<TMP_Text>(true);
        }
    }
}
