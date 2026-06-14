using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillSlotButton : MonoBehaviour, IPointerEnterHandler
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (owner != null && equippedSkill != null)
            owner.ShowSkillInfo(equippedSkill);
    }

    public void Execute()
    {
        if (owner == null)
        {
            Debug.LogWarning("[SkillSlotButton] owner is null.");
            return;
        }

        owner.ShowSkillInfo(equippedSkill);
        owner.OpenSkillSelectPanel(this);
    }

    public void SetSkill(SkillMasterData skill)
    {
        equippedSkill = skill;

        if (nameText != null)
            nameText.text = skill != null ? skill.Name : "";

        if (iconImage != null)
        {
            Sprite icon = null;

            if (skill != null)
                icon = SkillIconUtility.GetSkillIcon(skill.SkillId);

            iconImage.enabled = icon != null;
            iconImage.sprite = icon;
            iconImage.color = Color.white;
        }
    }
}
