using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonsterInfoPanelUI : MonoBehaviour
{
    [Header("Monster Info")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Text legacyNameText;

    [Header("HP Bar")]
    [SerializeField] private Image hpFillImage;
    [SerializeField] private TMP_Text hpValueText;
    [SerializeField] private Text legacyHpValueText;

    [Header("Armor")]
    [SerializeField] private Image armorFillImage;
    [SerializeField] private TMP_Text armorValueText;
    [SerializeField] private Text legacyArmorValueText;

    [Header("Status Icons")]
    [SerializeField] private Transform statusContent;
    [SerializeField] private StatusEffectIcon statusIconPrefab;
    [SerializeField, Min(0.01f)] private float statusIconScale = 3f;

    [Header("Pattern Info")]
    [SerializeField] private Transform patternContent;
    [SerializeField] private MonsterPatternInfoItemUI patternInfoItemPrefab;
    [SerializeField, Min(0.01f)] private float patternItemScale = 1f;

    [Header("Skill Info")]
    [SerializeField] private Transform skillContent;
    [SerializeField] private MonsterSkillInfoItemUI skillInfoItemPrefab;
    [SerializeField, Min(0.01f)] private float skillItemScale = 1f;

    [Header("Auto Find Names")]
    [SerializeField] private string nameObjectName = "Name";
    [SerializeField] private string hpBarObjectName = "HPBar";
    [SerializeField] private string hpFillObjectName = "Fill";
    [SerializeField] private string hpValueObjectName = "ValueText";
    [SerializeField] private string armorFillObjectName = "ArmorFill";
    [SerializeField] private string armorValueObjectName = "ArmorValueText";
    [SerializeField] private string statusContentObjectName = "StatusContent";
    [SerializeField] private string patternContentObjectName = "PatternContent";
    [SerializeField] private string skillContentObjectName = "SkillContent";

    private readonly List<StatusEffectIcon> spawnedStatusIcons = new();
    private readonly List<MonsterPatternInfoItemUI> spawnedPatternItems = new();
    private readonly List<MonsterSkillInfoItemUI> spawnedSkillItems = new();

    private MonsterUnit boundMonster;
    private MonsterRuntimeData boundMonsterData;
    private int lastStatusSignature = int.MinValue;
    private string lastPatternMonsterId;
    private string lastSkillMonsterId;

    private void Awake()
    {
        ResolveReferencesIfNeeded();
    }

    private void OnDisable()
    {
        boundMonster = null;
        boundMonsterData = null;
        lastStatusSignature = int.MinValue;
        lastPatternMonsterId = null;
        lastSkillMonsterId = null;

        ClearStatusIcons();
        ClearPatternItems();
        ClearSkillItems();
    }

    private void Update()
    {
        RefreshBoundData(false);
    }

    public void Bind(MonsterUnit monster)
    {
        boundMonster = monster;
        boundMonsterData = monster != null ? monster.RuntimeData : null;
        RefreshBoundData(true);
    }

    public void Bind(MonsterRuntimeData monsterData)
    {
        boundMonster = null;
        boundMonsterData = monsterData;
        RefreshBoundData(true);
    }

    public void Refresh()
    {
        RefreshBoundData(true);
    }

    private void RefreshBoundData(bool forceRefresh)
    {
        ResolveReferencesIfNeeded();

        if (boundMonster != null)
            boundMonsterData = boundMonster.RuntimeData;

        if (boundMonsterData == null)
        {
            SetName(string.Empty);
            SetHP(0, 0);
            SetArmor(0);
            ClearStatusIcons();
            ClearPatternItems();
            ClearSkillItems();
            lastStatusSignature = int.MinValue;
            lastPatternMonsterId = null;
            lastSkillMonsterId = null;
            return;
        }

        SetName(boundMonsterData.GetDisplayName());
        SetHP(boundMonsterData.CurrentHP, boundMonsterData.MaxHP);
        SetArmor(boundMonsterData.CurrentShield);

        int statusSignature = GetStatusSignature(boundMonsterData.StatusEffects);
        if (forceRefresh || statusSignature != lastStatusSignature)
        {
            SetStatusIcons(boundMonsterData.StatusEffects);
            lastStatusSignature = statusSignature;
        }

        string monsterId = boundMonsterData.MonsterId;
        if (forceRefresh || lastPatternMonsterId != monsterId)
        {
            SetPatternItems(monsterId);
            lastPatternMonsterId = monsterId;
        }

        if (forceRefresh || lastSkillMonsterId != monsterId)
        {
            SetSkillItems(monsterId);
            lastSkillMonsterId = monsterId;
        }
    }

    private void SetName(string monsterName)
    {
        string safeName = string.IsNullOrWhiteSpace(monsterName) ? string.Empty : monsterName;

        if (nameText != null)
            nameText.text = safeName;

        if (legacyNameText != null)
            legacyNameText.text = safeName;
    }

    private void SetHP(int currentHP, int maxHP)
    {
        int safeMaxHP = Mathf.Max(0, maxHP);
        int safeCurrentHP = safeMaxHP > 0 ? Mathf.Clamp(currentHP, 0, safeMaxHP) : Mathf.Max(0, currentHP);
        float hpRatio = safeMaxHP > 0 ? (float)safeCurrentHP / safeMaxHP : 0f;

        if (hpFillImage != null)
            hpFillImage.fillAmount = hpRatio;

        string hpText = safeCurrentHP.ToString();

        if (hpValueText != null)
            hpValueText.text = hpText;

        if (legacyHpValueText != null)
            legacyHpValueText.text = hpText;
    }

    private void SetArmor(int armor)
    {
        int safeArmor = Mathf.Max(0, armor);
        bool hasArmor = safeArmor > 0;

        if (armorFillImage != null)
        {
            armorFillImage.gameObject.SetActive(hasArmor);
            armorFillImage.fillAmount = hasArmor ? 1f : 0f;
        }

        string armorText = safeArmor.ToString();

        if (armorValueText != null)
            armorValueText.text = armorText;

        if (legacyArmorValueText != null)
            legacyArmorValueText.text = armorText;
    }

    private void SetStatusIcons(List<StatusEffectRuntimeData> statusEffects)
    {
        ClearStatusIcons();

        if (statusContent == null)
            return;

        StatusEffectIcon template = ResolveStatusIconTemplate();
        if (template == null)
            return;

        if (template.transform.IsChildOf(statusContent))
            template.gameObject.SetActive(false);

        if (statusEffects == null)
            return;

        for (int i = 0; i < statusEffects.Count; i++)
        {
            StatusEffectRuntimeData statusEffect = statusEffects[i];
            if (statusEffect == null || !statusEffect.IsValid())
                continue;

            StatusEffectIcon icon = Instantiate(template, statusContent);
            icon.gameObject.name = template.gameObject.name + "_" + statusEffect.EffectId;
            icon.transform.localScale = Vector3.one * statusIconScale;
            icon.Set(statusEffect);
            spawnedStatusIcons.Add(icon);
        }
    }

    private void ClearStatusIcons()
    {
        for (int i = 0; i < spawnedStatusIcons.Count; i++)
        {
            StatusEffectIcon icon = spawnedStatusIcons[i];
            if (icon == null)
                continue;

            icon.gameObject.SetActive(false);
            Destroy(icon.gameObject);
        }

        spawnedStatusIcons.Clear();
    }

    private void SetPatternItems(string monsterId)
    {
        ClearPatternItems();

        if (patternContent == null)
            return;

        MonsterPatternInfoItemUI template = ResolvePatternItemTemplate();
        if (template == null)
            return;

        if (template.transform.IsChildOf(patternContent))
            template.gameObject.SetActive(false);

        IReadOnlyList<MonsterPatternInfoData> patternInfos = GetPatternInfos(monsterId);
        if (patternInfos == null)
            return;

        for (int i = 0; i < patternInfos.Count; i++)
        {
            MonsterPatternInfoData patternInfo = patternInfos[i];
            if (patternInfo == null || string.IsNullOrWhiteSpace(patternInfo.Description))
                continue;

            MonsterPatternInfoItemUI item = Instantiate(template, patternContent);
            item.gameObject.name = template.gameObject.name + "_" + GetPatternItemName(patternInfo);
            item.transform.localScale = Vector3.one * patternItemScale;
            item.gameObject.SetActive(true);
            item.Bind(patternInfo.Order, patternInfo.Description);
            spawnedPatternItems.Add(item);
        }
    }

    private void ClearPatternItems()
    {
        for (int i = 0; i < spawnedPatternItems.Count; i++)
        {
            MonsterPatternInfoItemUI item = spawnedPatternItems[i];
            if (item == null)
                continue;

            item.gameObject.SetActive(false);
            Destroy(item.gameObject);
        }

        spawnedPatternItems.Clear();
    }

    private void SetSkillItems(string monsterId)
    {
        ClearSkillItems();

        if (skillContent == null)
            return;

        MonsterSkillInfoItemUI template = ResolveSkillItemTemplate();
        if (template == null)
            return;

        if (template.transform.IsChildOf(skillContent))
            template.gameObject.SetActive(false);

        IReadOnlyList<MonsterPatternInfoData> patternInfos = GetPatternInfos(monsterId);
        if (patternInfos == null)
            return;

        HashSet<string> spawnedSkillIds = new();
        for (int i = 0; i < patternInfos.Count; i++)
        {
            MonsterPatternInfoData patternInfo = patternInfos[i];
            if (patternInfo == null || string.IsNullOrWhiteSpace(patternInfo.SkillId))
                continue;

            string skillId = patternInfo.SkillId.Trim();
            if (!spawnedSkillIds.Add(skillId))
                continue;

            MonsterSkillData skillData = GetMonsterSkillData(skillId);
            string skillName = GetSkillName(skillId, skillData);
            string description = GetSkillDescription(patternInfo, skillData);
            Sprite timelineIcon = GetTimelineActionIcon(skillData);
            Sprite rangeIcon = GetSkillRangeIcon(skillData);

            MonsterSkillInfoItemUI item = Instantiate(template, skillContent);
            item.gameObject.name = template.gameObject.name + "_" + skillId;
            item.transform.localScale = Vector3.one * skillItemScale;
            item.gameObject.SetActive(true);
            item.Bind(skillName, description, timelineIcon, rangeIcon);
            spawnedSkillItems.Add(item);
        }
    }

    private void ClearSkillItems()
    {
        for (int i = 0; i < spawnedSkillItems.Count; i++)
        {
            MonsterSkillInfoItemUI item = spawnedSkillItems[i];
            if (item == null)
                continue;

            item.gameObject.SetActive(false);
            Destroy(item.gameObject);
        }

        spawnedSkillItems.Clear();
    }

    private static IReadOnlyList<MonsterPatternInfoData> GetPatternInfos(string monsterId)
    {
        DataManager dataManager = DataManager.Instance;
        if (dataManager == null || dataManager.MonsterPatternInfoDatabase == null)
            return null;

        return dataManager.MonsterPatternInfoDatabase.GetByMonsterId(monsterId);
    }

    private static MonsterSkillData GetMonsterSkillData(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId) || DataManager.Instance == null || DataManager.Instance.MonsterSkillDatabase == null)
            return null;

        return DataManager.Instance.MonsterSkillDatabase.Get(skillId.Trim());
    }

    private static string GetSkillName(string skillId, MonsterSkillData skillData)
    {
        if (skillData != null && !string.IsNullOrWhiteSpace(skillData.Name))
            return skillData.Name.Trim();

        return string.IsNullOrWhiteSpace(skillId) ? string.Empty : skillId.Trim();
    }

    private static string GetSkillDescription(MonsterPatternInfoData patternInfo, MonsterSkillData skillData)
    {
        if (patternInfo != null && !string.IsNullOrWhiteSpace(patternInfo.SkillInfo))
            return patternInfo.SkillInfo.Trim();

        if (skillData != null && !string.IsNullOrWhiteSpace(skillData.EffectDesc))
            return skillData.EffectDesc.Trim();

        return string.Empty;
    }

    private static Sprite GetTimelineActionIcon(MonsterSkillData skillData)
    {
        if (skillData == null || DataManager.Instance == null || DataManager.Instance.ActionTypeIconDatabase == null)
            return null;

        if (DataManager.Instance.ActionTypeIconDatabase.TryGetIcon(skillData.TimelineNotation.ToString(), out Sprite icon))
            return icon;

        return null;
    }

    private static Sprite GetSkillRangeIcon(MonsterSkillData skillData)
    {
        if (skillData == null || string.IsNullOrWhiteSpace(skillData.RangeId))
            return null;

        if (DataManager.Instance == null || DataManager.Instance.SkillRangeIconDatabase == null)
            return null;

        if (DataManager.Instance.SkillRangeIconDatabase.TryGetIcon(skillData.RangeId, out Sprite icon))
            return icon;

        return null;
    }

    private static string GetPatternItemName(MonsterPatternInfoData patternInfo)
    {
        if (patternInfo != null && !string.IsNullOrWhiteSpace(patternInfo.PatternId))
            return patternInfo.PatternId.Trim();

        return "Pattern";
    }

    private static int GetStatusSignature(List<StatusEffectRuntimeData> statusEffects)
    {
        if (statusEffects == null || statusEffects.Count <= 0)
            return 0;

        unchecked
        {
            int hash = 17;
            hash = hash * 31 + statusEffects.Count;

            for (int i = 0; i < statusEffects.Count; i++)
            {
                StatusEffectRuntimeData statusEffect = statusEffects[i];
                if (statusEffect == null)
                {
                    hash = hash * 31;
                    continue;
                }

                hash = hash * 31 + (statusEffect.EffectId != null ? statusEffect.EffectId.GetHashCode() : 0);
                hash = hash * 31 + statusEffect.Stack;
                hash = hash * 31 + statusEffect.TurnCount;
            }

            return hash;
        }
    }

    private void ResolveReferencesIfNeeded()
    {
        if (nameText == null && legacyNameText == null)
        {
            Transform nameRoot = FindChildRecursive(transform, nameObjectName);
            if (nameRoot != null)
            {
                nameText = nameRoot.GetComponent<TMP_Text>();
                if (nameText == null)
                    nameText = nameRoot.GetComponentInChildren<TMP_Text>(true);

                legacyNameText = nameRoot.GetComponent<Text>();
                if (legacyNameText == null)
                    legacyNameText = nameRoot.GetComponentInChildren<Text>(true);
            }
        }

        ResolveHPReferencesIfNeeded();

        if (statusContent == null)
        {
            Transform statusRoot = FindChildRecursive(transform, statusContentObjectName);
            if (statusRoot != null)
                statusContent = statusRoot;
        }

        if (patternContent == null)
        {
            Transform patternRoot = FindChildRecursive(transform, patternContentObjectName);
            if (patternRoot != null)
                patternContent = patternRoot;
        }

        if (skillContent == null)
        {
            Transform skillRoot = FindChildRecursive(transform, skillContentObjectName);
            if (skillRoot != null)
                skillContent = skillRoot;
        }
    }

    private void ResolveHPReferencesIfNeeded()
    {
        Transform hpBarRoot = FindChildRecursive(transform, hpBarObjectName);
        if (hpBarRoot == null)
            return;

        if (hpFillImage == null)
        {
            Transform fillRoot = FindDirectChild(hpBarRoot, hpFillObjectName);
            if (fillRoot == null)
                fillRoot = FindChildRecursive(hpBarRoot, hpFillObjectName);

            if (fillRoot != null)
                hpFillImage = fillRoot.GetComponent<Image>();
        }

        if (hpValueText == null && legacyHpValueText == null)
        {
            Transform valueRoot = FindChildRecursive(hpBarRoot, hpValueObjectName);
            if (valueRoot != null)
            {
                hpValueText = valueRoot.GetComponent<TMP_Text>();
                if (hpValueText == null)
                    hpValueText = valueRoot.GetComponentInChildren<TMP_Text>(true);

                legacyHpValueText = valueRoot.GetComponent<Text>();
                if (legacyHpValueText == null)
                    legacyHpValueText = valueRoot.GetComponentInChildren<Text>(true);
            }
        }

        ResolveArmorReferencesIfNeeded(hpBarRoot);
    }

    private void ResolveArmorReferencesIfNeeded(Transform hpBarRoot)
    {
        if (hpBarRoot == null)
            return;

        if (armorFillImage == null)
        {
            Transform armorFillRoot = FindDirectChild(hpBarRoot, armorFillObjectName);
            if (armorFillRoot == null)
                armorFillRoot = FindChildRecursive(hpBarRoot, armorFillObjectName);

            if (armorFillRoot != null)
                armorFillImage = armorFillRoot.GetComponent<Image>();
        }

        if (armorValueText == null && legacyArmorValueText == null)
        {
            Transform armorValueRoot = FindChildRecursive(hpBarRoot, armorValueObjectName);
            if (armorValueRoot != null)
            {
                armorValueText = armorValueRoot.GetComponent<TMP_Text>();
                if (armorValueText == null)
                    armorValueText = armorValueRoot.GetComponentInChildren<TMP_Text>(true);

                legacyArmorValueText = armorValueRoot.GetComponent<Text>();
                if (legacyArmorValueText == null)
                    legacyArmorValueText = armorValueRoot.GetComponentInChildren<Text>(true);
            }
        }
    }

    private StatusEffectIcon ResolveStatusIconTemplate()
    {
        if (statusIconPrefab != null)
            return statusIconPrefab;

        if (statusContent == null)
            return null;

        statusIconPrefab = statusContent.GetComponentInChildren<StatusEffectIcon>(true);
        return statusIconPrefab;
    }

    private MonsterPatternInfoItemUI ResolvePatternItemTemplate()
    {
        if (patternInfoItemPrefab != null)
            return patternInfoItemPrefab;

        if (patternContent == null)
            return null;

        patternInfoItemPrefab = patternContent.GetComponentInChildren<MonsterPatternInfoItemUI>(true);
        return patternInfoItemPrefab;
    }

    private MonsterSkillInfoItemUI ResolveSkillItemTemplate()
    {
        if (skillInfoItemPrefab != null)
            return skillInfoItemPrefab;

        if (skillContent == null)
            return null;

        skillInfoItemPrefab = skillContent.GetComponentInChildren<MonsterSkillInfoItemUI>(true);
        return skillInfoItemPrefab;
    }

    private static Transform FindDirectChild(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child != null && child.name == childName)
                return child;
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child == null)
                continue;

            if (child.name == childName)
                return child;

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
