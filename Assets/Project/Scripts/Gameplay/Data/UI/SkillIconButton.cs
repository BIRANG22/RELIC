using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillIconButton : MonoBehaviour, IPointerEnterHandler
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text lockText;
    [SerializeField] private GameObject lockObject;

    private SkillSettingPanel owner;
    private SkillMasterData currentSkillData;

    private bool isLocked;
    private int requiredLevel;

    public SkillMasterData CurrentSkillData => currentSkillData;

    public void Init(SkillSettingPanel panel)
    {
        owner = panel;

        if (button != null)
        {
            button.onClick.RemoveListener(Execute);
            button.onClick.AddListener(Execute);
        }
    }

    public void SetSkillData(
        SkillMasterData skillData,
        bool locked,
        int requiredLv
    )
    {
        currentSkillData = skillData;
        isLocked = locked;
        requiredLevel = requiredLv;

        bool hasSkill = currentSkillData != null;

        gameObject.SetActive(hasSkill);

        if (!hasSkill)
            return;

        if (nameText != null)
            nameText.text = currentSkillData.Name;

        if (iconImage != null)
        {
            Sprite icon = SkillIconUtility.GetSkillIcon(currentSkillData.SkillId);

            iconImage.enabled = icon != null;
            iconImage.sprite = icon;
            iconImage.color = Color.white;
        }

        if (lockObject != null)
            lockObject.SetActive(isLocked);

        if (lockText != null)
            lockText.text = isLocked
                ? $"LV.{requiredLevel}"
                : "";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (owner != null && currentSkillData != null)
            owner.ShowSkillInfo(currentSkillData);
    }

    public void Execute()
    {
        if (owner == null)
            return;

        if (currentSkillData == null)
            return;

        owner.ShowSkillInfo(currentSkillData);

        if (isLocked)
        {
            Debug.Log(
                $"[SkillIconButton] Locked. Required Level: {requiredLevel}"
            );
            return;
        }

        owner.SelectSkill(currentSkillData);
    }
}
