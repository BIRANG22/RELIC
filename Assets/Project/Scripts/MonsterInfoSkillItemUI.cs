using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MonsterSkill 프리팹 한 칸의 표시를 담당합니다.
/// 프리팹 구조의 Icon/SkillIcon, Name, Detail, Range, Type/TypeText를 이름으로 자동 연결합니다.
/// </summary>
public sealed class MonsterInfoSkillItemUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image skillIconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text detailText;
    [SerializeField] private Image rangeImage;
    [SerializeField] private TMP_Text rangeText;
    [SerializeField] private TMP_Text typeText;

    private void Awake()
    {
        ResolveReferences();
    }

    public void Bind(MonsterSkillData skillData)
    {
        ResolveReferences();
        ProtectDynamicTexts();

        if (skillData == null)
        {
            Clear();
            return;
        }

        if (skillIconImage != null)
            SetImage(skillIconImage, ResolveSkillIcon(skillData));

        if (nameText != null)
            nameText.text = MonsterInfoDataNameResolver.ResolveSkillName(skillData);

        if (detailText != null)
        {
            string localizedDescription = GameDataLocalization.MonsterSkillDescription(skillData);
            detailText.text = MonsterSkillDescriptionFormatter.Format(localizedDescription, skillData);
        }

        Sprite rangeSprite = ResolveRangeIcon(skillData.RangeId);
        if (rangeImage != null)
            SetImage(rangeImage, rangeSprite);

        if (rangeText != null)
            rangeText.text = rangeSprite == null ? (skillData.RangeId ?? string.Empty) : string.Empty;

        if (typeText != null)
            typeText.text = GameDataLocalization.MonsterSkillType(skillData);
    }

    // These texts are filled from the selected monster skill, not a fixed UI key.
    // Keep their existing localization components from replacing runtime values.
    private void ProtectDynamicTexts()
    {
        ProtectDynamicText(detailText);
        ProtectDynamicText(typeText);
    }

    private static void ProtectDynamicText(TMP_Text text)
    {
        if (text == null)
            return;

        if (text.GetComponent<LocalizationIgnore>() == null)
            text.gameObject.AddComponent<LocalizationIgnore>();

        LocalizedTMPText localized = text.GetComponent<LocalizedTMPText>();
        if (localized != null)
            localized.enabled = false;
    }

    private void Clear()
    {
        if (skillIconImage != null)
            SetImage(skillIconImage, null);
        if (rangeImage != null)
            SetImage(rangeImage, null);
        if (nameText != null)
            nameText.text = string.Empty;
        if (detailText != null)
            detailText.text = string.Empty;
        if (rangeText != null)
            rangeText.text = string.Empty;
        if (typeText != null)
            typeText.text = string.Empty;
    }

    private static Sprite ResolveSkillIcon(MonsterSkillData skillData)
    {
        if (skillData == null || string.IsNullOrWhiteSpace(skillData.SkillIcon))
            return null;

        DataManager dataManager = DataManager.Instance;
        if (dataManager == null || dataManager.MonsterSkillIconDatabase == null)
            return null;

        return dataManager.MonsterSkillIconDatabase.TryGetIcon(skillData.SkillIcon.Trim(), out Sprite icon)
            ? icon
            : null;
    }

    private static Sprite ResolveRangeIcon(string rangeId)
    {
        if (string.IsNullOrWhiteSpace(rangeId))
            return null;

        DataManager dataManager = DataManager.Instance;
        if (dataManager == null || dataManager.SkillRangeIconDatabase == null)
            return null;

        return dataManager.SkillRangeIconDatabase.TryGetIcon(rangeId.Trim(), out Sprite icon)
            ? icon
            : null;
    }

    private static void SetImage(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    private void ResolveReferences()
    {
        if (skillIconImage == null)
        {
            Transform iconRoot = FindChildRecursive(transform, "Icon");
            Transform skillIcon = FindChildRecursive(iconRoot, "SkillIcon");
            if (skillIcon != null)
                skillIconImage = skillIcon.GetComponent<Image>();
        }

        if (nameText == null)
            nameText = ResolveText("Name");

        if (detailText == null)
            detailText = ResolveText("Detail");

        Transform rangeRoot = FindChildRecursive(transform, "Range");
        if (rangeImage == null && rangeRoot != null)
            rangeImage = rangeRoot.GetComponent<Image>();
        if (rangeText == null && rangeRoot != null)
            rangeText = rangeRoot.GetComponent<TMP_Text>() ?? rangeRoot.GetComponentInChildren<TMP_Text>(true);

        if (typeText == null)
        {
            Transform typeRoot = FindChildRecursive(transform, "Type");
            Transform typeTextRoot = FindChildRecursive(typeRoot, "TypeText");
            if (typeTextRoot != null)
                typeText = typeTextRoot.GetComponent<TMP_Text>() ?? typeTextRoot.GetComponentInChildren<TMP_Text>(true);
        }
    }

    private TMP_Text ResolveText(string objectName)
    {
        Transform target = FindChildRecursive(transform, objectName);
        if (target == null)
            return null;

        return target.GetComponent<TMP_Text>() ?? target.GetComponentInChildren<TMP_Text>(true);
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
