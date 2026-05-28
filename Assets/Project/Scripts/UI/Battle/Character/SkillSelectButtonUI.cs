using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Data;
using Relic.Gameplay.Battle;

public class SkillSelectButtonUI : MonoBehaviour
{
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
            playerActionPlanner = Object.FindFirstObjectByType<PlayerActionPlanner>();
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
            ClearSkill();
            return;
        }

        RefreshIcon();
    }

    public void SetSkill(SkillMasterData data)
    {
        if (data == null)
        {
            ClearSkill();
            return;
        }

        skillData = data;
        skillId = data.SkillId;

        RefreshIcon();
    }

    private void RefreshIcon()
    {
        if (iconImage == null)
            return;

        Sprite icon = SkillIconUtility.GetSkillIcon(skillId);

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
        iconImage.gameObject.SetActive(icon != null);

        if (icon == null)
            Debug.LogWarning($"[SkillSelectButtonUI] Skill icon not found: {skillId}");
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
            iconImage.gameObject.SetActive(false);
        }
    }
}