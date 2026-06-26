using Relic.Gameplay.Data;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillUpgradeIconItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;

    private SkillUpgradeRequest request;
    private Action<SkillUpgradeRequest> onClicked;
    private RectTransform rectTransform;

    private const string CurrentTooltipHeader = "\uD604\uC7AC";
    private const string UpgradeTooltipHeader = "\uAC15\uD654 \uD6C4";

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnDisable()
    {
        TimelineSkillHoverPopupUI.Instance?.Hide(this);
    }

    public void Initialize(
        string characterId,
        string currentSkillId,
        string upgradeSkillId,
        SkillSlotType slotType,
        int slotIndex,
        Action<SkillUpgradeRequest> onClicked)
    {
        request = new SkillUpgradeRequest
        {
            CharacterId = characterId,
            CurrentSkillId = currentSkillId,
            UpgradeSkillId = upgradeSkillId,
            SlotType = slotType,
            SlotIndex = slotIndex
        };

        this.onClicked = onClicked;

        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }

        RefreshIcon(currentSkillId);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TimelineSkillHoverPopupUI.Instance?.Hide(this);
    }

    private void HandleClick()
    {
        onClicked?.Invoke(request);
    }

    private void ShowTooltip()
    {
        if (DataManager.Instance == null || DataManager.Instance.SkillDatabase == null)
            return;

        if (!DataManager.Instance.SkillDatabase.TryGet(request.CurrentSkillId, out SkillMasterData currentSkill))
            return;

        if (!DataManager.Instance.SkillDatabase.TryGet(request.UpgradeSkillId, out SkillMasterData upgradeSkill))
            return;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        CharacterRuntimeData runtime = ResolveCharacterRuntime();
        string title = $"{GetSkillName(currentSkill)} -> {GetSkillName(upgradeSkill)}";
        string description =
            $"{CurrentTooltipHeader}\n{SkillTooltipFormatter.BuildSkillDescription(currentSkill, runtime)}\n\n" +
            $"{UpgradeTooltipHeader}\n{SkillTooltipFormatter.BuildSkillDescription(upgradeSkill, runtime)}";

        TimelineSkillHoverPopupUI.Instance?.Show(title, description, null, rectTransform, this);
    }

    private CharacterRuntimeData ResolveCharacterRuntime()
    {
        if (string.IsNullOrWhiteSpace(request.CharacterId))
            return null;

        if (DataManager.Instance == null || DataManager.Instance.CharacterRuntimeStore == null)
            return null;

        return DataManager.Instance.CharacterRuntimeStore.TryGet(
            request.CharacterId,
            out CharacterRuntimeData runtime)
            ? runtime
            : null;
    }

    private string GetSkillName(SkillMasterData skillData)
    {
        if (skillData == null)
            return "";

        if (!string.IsNullOrWhiteSpace(skillData.Name))
            return skillData.Name;

        return skillData.SkillId;
    }

    private void RefreshIcon(string skillId)
    {
        if (iconImage == null)
            return;

        iconImage.sprite = null;

        if (DataManager.Instance == null)
            return;

        Sprite icon = null;

        if (DataManager.Instance.SkillDatabase != null &&
            DataManager.Instance.SkillDatabase.TryGet(skillId, out SkillMasterData skillData))
        {
            icon = skillData.Icon;
        }

        if (icon == null &&
            DataManager.Instance.SkillIconDatabase != null &&
            DataManager.Instance.SkillIconDatabase.TryGetIcon(skillId, out Sprite databaseIcon))
        {
            icon = databaseIcon;
        }

        iconImage.sprite = icon;
        iconImage.enabled = iconImage.sprite != null;
    }
}
