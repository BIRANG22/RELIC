using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlotButton : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;

    private SkillSettingPanel owner;
    private int slotIndex;
    private SkillMasterData equippedSkill;

    public int SlotIndex => slotIndex;
    public SkillMasterData EquippedSkill => equippedSkill;

    public void Init(SkillSettingPanel panel, int index)
    {
        owner = panel;
        slotIndex = index;

        if (button != null)
        {
            button.onClick.RemoveListener(Execute);
            button.onClick.AddListener(Execute);
        }
    }

    public void Execute()
    {
        Debug.Log($"[SkillSlotButton] Clicked Slot: {slotIndex}");

        if (owner == null)
        {
            Debug.LogWarning("[SkillSlotButton] owner is null.");
            return;
        }

        owner.OpenSkillSelectPanel(this);
    }

    public void SetSkill(SkillMasterData skill)
    {
        equippedSkill = skill;

        Debug.Log(skill != null
            ? $"[SkillSlotButton] SetSkill: {skill.SkillId} / {skill.Name}"
            : "[SkillSlotButton] SetSkill: null");

        if (nameText != null)
            nameText.text = skill != null ? skill.Name : "";

        if (iconImage != null)
        {
            Sprite icon = null;

            if (skill != null)
                icon = SkillIconUtility.GetSkillIcon(skill.SkillId);

            Debug.Log(icon != null
                ? $"[SkillSlotButton] Icon Found: {skill.SkillId}"
                : $"[SkillSlotButton] Icon Missing: {(skill != null ? skill.SkillId : "null")}");

            iconImage.enabled = icon != null;
            iconImage.sprite = icon;
            iconImage.color = Color.white;
        }
    }
}