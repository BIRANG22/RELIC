using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LobbyCultureTankPanelPresenter : MonoBehaviour
{
    private const float RefreshIntervalSeconds = 0.25f;

    [SerializeField] private Canvas ownerCanvas;
    [SerializeField] private string tankNamePrefix = "CultureTank";
    [SerializeField] private Vector2 panelSize = new(560f, 420f);
    [SerializeField] private int panelSortingOrder = 1100;

    private readonly List<TankRow> rows = new();
    private GameObject panelRoot;
    private RectTransform contentRoot;
    private TMP_Text emptyText;
    private float nextRefreshAt;
    private bool missingCanvasWarned;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        AutoBind();
        CreatePanelIfNeeded();
        Close();
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
        AutoBind();
        CreatePanelIfNeeded();

        if (panelRoot == null)
            return;

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
        List<LobbyCultureTankController> tanks = FindCultureTanks();
        EnsureRowCount(tanks.Count);

        bool hasTanks = tanks.Count > 0;
        if (emptyText != null)
            emptyText.gameObject.SetActive(!hasTanks);

        for (int i = 0; i < rows.Count; i++)
        {
            TankRow row = rows[i];
            LobbyCultureTankController tank = tanks[i];

            row.Controller = tank;
            row.Root.SetActive(true);
            row.Label.text = tank.GetPanelLabel();
            row.Background.color = GetRowColor(tank.GetPanelState());
            row.Button.onClick.RemoveAllListeners();

            LobbyCultureTankController clickedTank = tank;
            row.Button.onClick.AddListener(() => InteractWithTank(clickedTank));
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

    private void EnsureRowCount(int count)
    {
        while (rows.Count < count)
            rows.Add(CreateRow(rows.Count));

        while (rows.Count > count)
        {
            int lastIndex = rows.Count - 1;
            TankRow row = rows[lastIndex];
            rows.RemoveAt(lastIndex);

            if (row.Root != null)
                Destroy(row.Root);
        }
    }

    private TankRow CreateRow(int index)
    {
        const float rowHeight = 78f;
        const float rowSpacing = 12f;

        GameObject rowObject = new(
            $"CultureTankRow_{index + 1}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        RectTransform rowRect = (RectTransform)rowObject.transform;
        rowRect.SetParent(contentRoot, false);
        rowRect.anchorMin = new Vector2(0.5f, 1f);
        rowRect.anchorMax = new Vector2(0.5f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.sizeDelta = new Vector2(460f, rowHeight);
        rowRect.anchoredPosition = new Vector2(0f, -index * (rowHeight + rowSpacing));

        Image background = rowObject.GetComponent<Image>();
        background.color = new Color(0.16f, 0.18f, 0.22f, 0.96f);

        Button button = rowObject.GetComponent<Button>();
        button.targetGraphic = background;

        TMP_Text label = CreateText(rowRect, "Label", 24f, TextAlignmentOptions.Center);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(16f, 6f);
        label.rectTransform.offsetMax = new Vector2(-16f, -6f);

        return new TankRow
        {
            Root = rowObject,
            Background = background,
            Button = button,
            Label = label
        };
    }

    private void CreatePanelIfNeeded()
    {
        if (panelRoot != null)
            return;

        if (ownerCanvas == null)
        {
            if (!missingCanvasWarned)
            {
                Debug.LogWarning("[LobbyCultureTankPanelPresenter] Canvas not found.");
                missingCanvasWarned = true;
            }

            return;
        }

        if (ownerCanvas.GetComponent<GraphicRaycaster>() == null)
            ownerCanvas.gameObject.AddComponent<GraphicRaycaster>();

        panelRoot = new GameObject(
            "CultureTankPanel",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(GraphicRaycaster),
            typeof(CanvasRenderer),
            typeof(Image));
        RectTransform panelRect = (RectTransform)panelRoot.transform;
        panelRect.SetParent(ownerCanvas.transform, false);
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = panelSize;

        Canvas panelCanvas = panelRoot.GetComponent<Canvas>();
        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = panelSortingOrder;

        Image panelImage = panelRoot.GetComponent<Image>();
        panelImage.color = new Color(0.035f, 0.045f, 0.06f, 0.95f);

        TMP_Text title = CreateText(panelRect, "Title", 32f, TextAlignmentOptions.Center);
        title.text = "배양 연구";
        title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -26f);
        title.rectTransform.sizeDelta = new Vector2(360f, 54f);

        CreateCloseButton(panelRect);
        CreateContentRoot(panelRect);

        emptyText = CreateText(panelRect, "EmptyText", 24f, TextAlignmentOptions.Center);
        emptyText.text = "배양조를 찾을 수 없습니다.";
        emptyText.rectTransform.anchorMin = emptyText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        emptyText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        emptyText.rectTransform.anchoredPosition = new Vector2(0f, -10f);
        emptyText.rectTransform.sizeDelta = new Vector2(440f, 80f);
    }

    private void CreateCloseButton(RectTransform panelRect)
    {
        GameObject closeObject = new(
            "CloseButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        RectTransform closeRect = (RectTransform)closeObject.transform;
        closeRect.SetParent(panelRect, false);
        closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = Vector2.one;
        closeRect.anchoredPosition = new Vector2(-18f, -18f);
        closeRect.sizeDelta = new Vector2(48f, 48f);

        Image closeImage = closeObject.GetComponent<Image>();
        closeImage.color = Color.white;

        Button closeButton = closeObject.GetComponent<Button>();
        closeButton.onClick.AddListener(Close);

        TMP_Text label = CreateText(closeRect, "Label", 28f, TextAlignmentOptions.Center);
        label.text = "X";
        label.color = Color.black;
        label.raycastTarget = false;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
    }

    private void CreateContentRoot(RectTransform panelRect)
    {
        GameObject contentObject = new("Content", typeof(RectTransform));
        contentRoot = (RectTransform)contentObject.transform;
        contentRoot.SetParent(panelRect, false);
        contentRoot.anchorMin = contentRoot.anchorMax = new Vector2(0.5f, 1f);
        contentRoot.pivot = new Vector2(0.5f, 1f);
        contentRoot.anchoredPosition = new Vector2(0f, -108f);
        contentRoot.sizeDelta = new Vector2(480f, 280f);
    }

    private TMP_Text CreateText(
        RectTransform parent,
        string objectName,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.SetParent(parent, false);

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private void AutoBind()
    {
        if (ownerCanvas != null)
            return;

        ownerCanvas = GetComponentInParent<Canvas>();
        if (ownerCanvas != null)
            return;

        Canvas[] canvases = FindObjectsByType<Canvas>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas != null && canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace)
            {
                ownerCanvas = canvas;
                return;
            }
        }

        if (canvases.Length > 0)
            ownerCanvas = canvases[0];
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

    private sealed class TankRow
    {
        public GameObject Root;
        public Image Background;
        public Button Button;
        public TMP_Text Label;
        public LobbyCultureTankController Controller;
    }
}
