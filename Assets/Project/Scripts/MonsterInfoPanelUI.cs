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

    [Header("Auto Find Names")]
    [SerializeField] private string nameObjectName = "Name";
    [SerializeField] private string hpBarObjectName = "HPBar";
    [SerializeField] private string hpFillObjectName = "Fill";
    [SerializeField] private string hpValueObjectName = "ValueText";
    [SerializeField] private string armorFillObjectName = "ArmorFill";
    [SerializeField] private string armorValueObjectName = "ArmorValueText";
    [SerializeField] private string statusContentObjectName = "StatusContent";

    private readonly List<StatusEffectIcon> spawnedStatusIcons = new();

    private void Awake()
    {
        ResolveReferencesIfNeeded();
    }

    private void OnDisable()
    {
        ClearStatusIcons();
    }

    public void Bind(MonsterUnit monster)
    {
        Bind(monster != null ? monster.RuntimeData : null);
    }

    public void Bind(MonsterRuntimeData monsterData)
    {
        ResolveReferencesIfNeeded();

        if (monsterData == null)
        {
            SetName(string.Empty);
            SetHP(0, 0);
            SetArmor(0);
            ClearStatusIcons();
            return;
        }

        SetName(monsterData.Name);
        SetHP(monsterData.CurrentHP, monsterData.MaxHP);
        SetArmor(monsterData.CurrentShield);
        SetStatusIcons(monsterData.StatusEffects);
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
