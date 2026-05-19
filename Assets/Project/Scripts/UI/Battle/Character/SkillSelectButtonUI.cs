using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Data;
using Relic.Gameplay.Battle;

//캐릭터 프리팹 스킬 선택 스크립트
public class SkillSelectButtonUI : MonoBehaviour
{
    [Header("Test")]
    [SerializeField] private string testSkillId = "S_Move_1";

    [Header("UI")]
    [SerializeField] private Image iconImage;

    [Header("Battle")]
    [SerializeField] private PlayerActionPlanner playerActionPlanner;

    [Header("Mode")]
    [SerializeField] private bool useEquipUI = false;
    [SerializeField] private bool useBattleTimeline = true;

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
            Debug.LogWarning($"[SkillSelectButtonUI] testSkillId가 비어있습니다.");
        }
    }

    public void SetSkill(string id)
    {
        skillId = id;

        var dm = DataManager.Instance;

        if (dm == null)
        {
            Debug.LogError("[SkillSelectButtonUI] DataManager.Instance가 없습니다.");
            return;
        }

        skillData = dm.SkillDatabase.Get(skillId);

        if (skillData == null)
        {
            Debug.LogWarning($"[SkillSelectButtonUI] SkillData 없음: {skillId}");
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = skillData.Icon;
            iconImage.enabled = skillData.Icon != null;
        }
    }

    public void OnClickSkill()
    {
        if (skillData == null)
        {
            Debug.LogWarning($"[SkillSelectButtonUI] skillData 없음: {skillId}");
            return;
        }

        if (useEquipUI)
        {
            if (SkillEquipUIController.Instance != null)
            {
                SkillEquipUIController.Instance.SelectSkill(skillData);
            }
            else
            {
                Debug.LogWarning("[SkillSelectButtonUI] SkillEquipUIController.Instance가 없습니다.");
            }
        }

        if (useBattleTimeline)
        {
            if (playerActionPlanner == null)
            {
                Debug.LogWarning("[SkillSelectButtonUI] PlayerActionPlanner가 연결되지 않았습니다.");
                return;
            }

            playerActionPlanner.SelectSkill(skillData);
        }
    }
}