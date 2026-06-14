using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class EquippedSkillPanelUI : MonoBehaviour
{
    [Header("Character Rows")]
    [SerializeField] private EquippedSkillCharacterRowUI[] characterRows;

    [Header("Tooltip")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipNameText;
    [SerializeField] private TMP_Text tooltipDescriptionText;
    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private bool moveTooltipToHoveredSlot = false;
    [SerializeField] private Vector2 tooltipOffset = new Vector2(0f, 80f);
    [SerializeField] private bool keepTooltipPanelVisible = true;
    [SerializeField] private string emptyTooltipName = string.Empty;
    [TextArea]
    [SerializeField] private string emptyTooltipDescription = string.Empty;

    [Header("Front Sorting")]
    [SerializeField] private bool bringToFrontOnEnable = true;
    [SerializeField] private bool forceCanvasSorting = true;
    [SerializeField] private int sortingOrder = 1000;
    [SerializeField] private bool addGraphicRaycaster = true;

    private Canvas cachedCanvas;
    private GraphicRaycaster cachedGraphicRaycaster;

    private void Awake()
    {
        if (tooltipPanel != null && tooltipRect == null)
            tooltipRect = tooltipPanel.GetComponent<RectTransform>();

        ApplyFrontSorting();
        HideSkillTooltip();
    }

    private void OnEnable()
    {
        ApplyFrontSorting();
        Refresh();
        HideSkillTooltip();
    }

    private void OnDisable()
    {
        HideSkillTooltip();
    }

    public void Refresh()
    {
        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[EquippedSkillPanelUI] DataManager가 없습니다.");
            return;
        }

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        CharacterRuntimeStore characterStore = DataManager.Instance.CharacterRuntimeStore;

        if (partyStore == null || characterStore == null)
        {
            Debug.LogWarning("[EquippedSkillPanelUI] PartyRuntimeStore 또는 CharacterRuntimeStore가 없습니다.");
            ClearRows();
            return;
        }

        for (int i = 0; i < characterRows.Length; i++)
        {
            if (characterRows[i] == null)
                continue;

            string characterId = partyStore.GetCharacterId(i);

            if (string.IsNullOrWhiteSpace(characterId))
            {
                characterRows[i].Clear();
                continue;
            }

            if (characterStore.TryGet(characterId, out CharacterRuntimeData characterData))
            {
                characterRows[i].Setup(this, characterData);
            }
            else
            {
                Debug.LogWarning($"[EquippedSkillPanelUI] CharacterRuntimeData가 없습니다: {characterId}");
                characterRows[i].Clear();
            }
        }
    }

    public void ShowSkillTooltip(SkillMasterData skillData, RectTransform hoveredSlotRect)
    {
        if (skillData == null)
        {
            HideSkillTooltip();
            return;
        }

        if (tooltipPanel != null && !tooltipPanel.activeSelf)
            tooltipPanel.SetActive(true);

        if (tooltipNameText != null)
            tooltipNameText.text = string.IsNullOrWhiteSpace(skillData.Name) ? skillData.SkillId : skillData.Name;

        if (tooltipDescriptionText != null)
            tooltipDescriptionText.text = BuildDescription(skillData);

        if (moveTooltipToHoveredSlot)
            MoveTooltipToSlot(hoveredSlotRect);
    }

    public void HideSkillTooltip()
    {
        if (tooltipPanel != null && keepTooltipPanelVisible && !tooltipPanel.activeSelf)
            tooltipPanel.SetActive(true);
        else if (tooltipPanel != null && !keepTooltipPanelVisible)
            tooltipPanel.SetActive(false);

        if (tooltipNameText != null)
            tooltipNameText.text = emptyTooltipName;

        if (tooltipDescriptionText != null)
            tooltipDescriptionText.text = emptyTooltipDescription;
    }

    private void ClearRows()
    {
        if (characterRows == null)
            return;

        for (int i = 0; i < characterRows.Length; i++)
        {
            if (characterRows[i] != null)
                characterRows[i].Clear();
        }
    }

    private string BuildDescription(SkillMasterData skillData)
    {
        if (skillData == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(skillData.ToolTip))
            return skillData.ToolTip;

        if (!string.IsNullOrWhiteSpace(skillData.Details))
            return skillData.Details;

        return "효과 설명이 없습니다.";
    }

    private void MoveTooltipToSlot(RectTransform hoveredSlotRect)
    {
        if (tooltipRect == null || hoveredSlotRect == null)
            return;

        RectTransform tooltipParentRect = tooltipRect.parent as RectTransform;
        if (tooltipParentRect == null)
            return;

        Canvas canvas = tooltipRect.GetComponentInParent<Canvas>();
        Camera uiCamera = GetCanvasCamera(canvas);

        Vector3[] corners = new Vector3[4];
        hoveredSlotRect.GetWorldCorners(corners);
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCenter);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                tooltipParentRect,
                screenPoint,
                uiCamera,
                out Vector2 localPoint))
        {
            return;
        }

        tooltipRect.anchoredPosition = localPoint + tooltipOffset;
    }

    private Camera GetCanvasCamera(Canvas canvas)
    {
        if (canvas == null)
            return null;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private void ApplyFrontSorting()
    {
        if (bringToFrontOnEnable)
            transform.SetAsLastSibling();

        if (!forceCanvasSorting)
            return;

        if (cachedCanvas == null)
        {
            cachedCanvas = GetComponent<Canvas>();
            if (cachedCanvas == null)
                cachedCanvas = gameObject.AddComponent<Canvas>();
        }

        cachedCanvas.overrideSorting = true;
        cachedCanvas.sortingOrder = sortingOrder;

        if (addGraphicRaycaster)
        {
            if (cachedGraphicRaycaster == null)
            {
                cachedGraphicRaycaster = GetComponent<GraphicRaycaster>();
                if (cachedGraphicRaycaster == null)
                    cachedGraphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
            }
        }
    }
}
