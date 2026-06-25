using Relic.Gameplay.Data;
using System;
using UnityEngine;
using UnityEngine.UI;

public class SkillUpgradeIconItem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;

    private SkillUpgradeRequest request;
    private Action<SkillUpgradeRequest> onClicked;

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

    private void HandleClick()
    {
        onClicked?.Invoke(request);
    }

    private void RefreshIcon(string skillId)
    {
        if (iconImage == null)
            return;

        iconImage.sprite = null;

        if (DataManager.Instance == null)
            return;

        if (!DataManager.Instance.SkillDatabase.TryGet(skillId, out SkillMasterData skillData))
            return;

        iconImage.sprite = skillData.Icon;
        iconImage.enabled = iconImage.sprite != null;
    }
}