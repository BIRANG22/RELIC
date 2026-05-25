using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Data;
using Relic.Gameplay.Battle;

//캐릭터 프리팹 스킬 선택 스크립트
public class SkillSelectButtonUI : MonoBehaviour
{
    [SerializeField] private bool useTestSkillId = false;
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

    private void Awake()
    {
        if (playerActionPlanner == null)
        {
            playerActionPlanner = Object.FindFirstObjectByType<PlayerActionPlanner>();
        }
    }
    private void Start()
    {
        if (useTestSkillId && !string.IsNullOrEmpty(testSkillId))
        {
            SetSkill(testSkillId);
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

    public void SetSkill(SkillMasterData data)
    {
        if (data == null)
        {
            skillId = null;
            skillData = null;

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            return;
        }

        skillData = data;
        skillId = data.SkillId;

        if (iconImage != null)
        {
            iconImage.sprite = data.Icon;
            iconImage.enabled = data.Icon != null;
        }
    }

    public void OnClickSkill()
    {
        if (skillData == null)
        {
            Debug.LogWarning($"[SkillSelectButtonUI] skillData 없음: {skillId}");
            return;
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

    public void ClearSkill()
    {
        skillId = null;
        skillData = null;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
    }
}