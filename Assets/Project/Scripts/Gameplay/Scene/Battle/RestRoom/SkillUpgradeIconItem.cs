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
    private Action<SkillUpgradeRequest, Sprite> onClicked;
    private Action<SkillUpgradeRequest> onHovered;
    private Action<SkillUpgradeRequest> onHoverExited;
    private Color defaultIconColor = Color.white;
    private bool hasDefaultIconColor;

    public bool Matches(SkillUpgradeRequest compareRequest)
    {
        return string.Equals(request.CharacterId, compareRequest.CharacterId, StringComparison.Ordinal) &&
               string.Equals(request.CurrentSkillId, compareRequest.CurrentSkillId, StringComparison.Ordinal) &&
               string.Equals(request.UpgradeSkillId, compareRequest.UpgradeSkillId, StringComparison.Ordinal) &&
               request.SlotType == compareRequest.SlotType &&
               request.SlotIndex == compareRequest.SlotIndex;
    }

    public void SetIconColor(Color color)
    {
        if (iconImage == null)
            return;

        CacheDefaultIconColor();
        iconImage.color = color;
    }

    public void ResetIconColor()
    {
        if (iconImage == null)
            return;

        CacheDefaultIconColor();
        iconImage.color = defaultIconColor;
    }

    public void ShowUpgradeMark(string upgradedSkillId)
    {
        SkillUpgradeMarkStyle.ApplyShared(iconImage, upgradedSkillId);
    }

    private void OnDisable()
    {
        onHoverExited?.Invoke(request);
    }

    public void Initialize(
        string characterId,
        string currentSkillId,
        string upgradeSkillId,
        SkillSlotType slotType,
        int slotIndex,
        Action<SkillUpgradeRequest, Sprite> onClicked,
        Action<SkillUpgradeRequest> onHovered,
        Action<SkillUpgradeRequest> onHoverExited)
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
        this.onHovered = onHovered;
        this.onHoverExited = onHoverExited;

        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }

        RefreshIcon(currentSkillId);
        ResetIconColor();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        onHovered?.Invoke(request);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        onHoverExited?.Invoke(request);
    }

    private void HandleClick()
    {
        onClicked?.Invoke(request, iconImage != null ? iconImage.sprite : null);
    }

    private void CacheDefaultIconColor()
    {
        if (hasDefaultIconColor || iconImage == null)
            return;

        defaultIconColor = iconImage.color;
        hasDefaultIconColor = true;
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
        iconImage.color = defaultIconColor;
        SkillUpgradeMarkStyle.ApplyShared(iconImage, skillId);
    }
}
