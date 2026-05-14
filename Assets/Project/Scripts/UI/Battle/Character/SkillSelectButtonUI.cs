using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class SkillSelectButtonUI : MonoBehaviour
{
    [Header("Test")]
    [SerializeField] private string testSkillId = "S_Move_1";

    [Header("UI")]
    [SerializeField] private Image iconImage;

    private string skillId;
    private SkillMasterData skillData;

    private void Start()
    {
        if (!string.IsNullOrEmpty(testSkillId))
        {
            SetSkill(testSkillId);
        }
        else
        {
            Debug.LogWarning($"스킬 아이콘 없음: {skillId}");
        }
    }

    public void SetSkill(string id)
    {
        skillId = id;

        var dm = DataManager.Instance;

        skillData = dm.SkillDatabase.Get(skillId);

        if (skillData == null)
        {
            Debug.LogWarning($"[SkillSelectButtonUI] SkillData 없음: {skillId}");
            return;
        }

        // 아이콘 설정
        iconImage.sprite = skillData.Icon;
    }

    public void OnClickSkill()
    {
        SkillEquipUIController.Instance.SelectSkill(skillData);
    }
}