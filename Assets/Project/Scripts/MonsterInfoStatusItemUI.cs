using Relic.Gameplay.Data;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MonsterStatus 프리팹 한 칸의 표시를 담당합니다.
/// Icon/StatusIcon에는 효과 아이콘, Name에는 "효과명 수치", Detail에는 효과 설명을 표시합니다.
/// </summary>
public sealed class MonsterInfoStatusItemUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image statusIconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text detailText;

    private StatusEffectRuntimeData boundStatus;

    private void Awake()
    {
        ResolveReferences();
        DisableFixedLocalizationWriters();
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        DisableFixedLocalizationWriters();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale locale)
    {
        if (boundStatus != null)
            Bind(boundStatus);
    }

    private void DisableFixedLocalizationWriters()
    {
        DisableFixedWriter(nameText);
        DisableFixedWriter(detailText);
    }

    private static void DisableFixedWriter(TMP_Text text)
    {
        if (text == null) return;
        LocalizedTMPText writer = text.GetComponent<LocalizedTMPText>();
        if (writer != null) writer.enabled = false;
    }

    public void Bind(StatusEffectRuntimeData statusData)
    {
        ResolveReferences();
        DisableFixedLocalizationWriters();
        boundStatus = statusData;

        if (statusData == null || string.IsNullOrWhiteSpace(statusData.EffectId))
        {
            Clear();
            return;
        }

        EffectMasterData effectData = ResolveEffectData(statusData.EffectId);

        if (statusIconImage != null)
        {
            DataManager dataManager = DataManager.Instance;
            Color iconColor = ResolveIconColor(
                statusData.EffectId,
                statusData.IsMonsterInnateTrait,
                dataManager != null ? dataManager.StatusEffectIconDatabase : null,
                dataManager != null ? dataManager.EffectDatabase : null);
            statusIconImage.color = ComposeIconColor(iconColor, statusIconImage.color);
            SetImage(statusIconImage, ResolveStatusIcon(statusData.EffectId));
        }

        string effectName = MonsterInfoDataNameResolver.ResolveEffectName(
            effectData,
            statusData.EffectId);

        if (nameText != null)
            nameText.text = FormatDisplayName(
                effectName,
                statusData.EffectId,
                statusData.Stack,
                statusData.IsPassive);

        if (detailText != null)
        {
            detailText.richText = true;
            DataManager dataManager = DataManager.Instance;
            detailText.text = FormatDetailText(
                statusData,
                effectData,
                dataManager != null ? dataManager.EffectDatabase : null,
                dataManager != null ? dataManager.GridEffectDatabase : null);
        }
    }

    public static string FormatDetailText(
        StatusEffectRuntimeData statusData,
        EffectMasterData effectData,
        EffectDatabase effectDatabase,
        GridEffectDatabase gridEffectDatabase)
    {
        if (effectData == null)
            return string.Empty;

        string tooltip = GameDataLocalization.EffectTooltip(effectData);
        if (statusData == null)
            return tooltip;

        return MonsterInnateEffectDescriptionFormatter.Format(
            tooltip,
            statusData.EffectId,
            effectDatabase,
            gridEffectDatabase);
    }

    public static string FormatDisplayName(
        string effectName,
        string effectId,
        int stack,
        bool isPassive)
    {
        return isPassive
            ? $"{effectName} ∞"
            : stack > 0 ? $"{effectName} {stack}" : effectName;
    }

    private void Clear()
    {
        if (statusIconImage != null)
        {
            statusIconImage.color = ComposeIconColor(Color.white, statusIconImage.color);
            SetImage(statusIconImage, null);
        }
        if (nameText != null)
            nameText.text = string.Empty;
        if (detailText != null)
            detailText.text = string.Empty;
    }

    private static EffectMasterData ResolveEffectData(string effectId)
    {
        DataManager dataManager = DataManager.Instance;
        if (dataManager == null || dataManager.EffectDatabase == null || string.IsNullOrWhiteSpace(effectId))
            return null;

        dataManager.EffectDatabase.TryGet(effectId.Trim(), out EffectMasterData effectData);
        return effectData;
    }

    private static Sprite ResolveStatusIcon(string effectId)
    {
        DataManager dataManager = DataManager.Instance;
        if (dataManager == null || dataManager.StatusEffectIconDatabase == null || string.IsNullOrWhiteSpace(effectId))
            return null;

        return dataManager.StatusEffectIconDatabase.TryGetIcon(effectId.Trim(), out Sprite icon)
            ? icon
            : null;
    }

    public static Color ResolveIconColor(
        string effectId,
        bool isMonsterInnateTrait,
        StatusEffectIconDatabase iconDatabase,
        EffectDatabase effectDatabase)
    {
        if (iconDatabase == null)
            return Color.white;

        string normalizedEffectId = string.IsNullOrWhiteSpace(effectId)
            ? string.Empty
            : effectId.Trim();
        return iconDatabase.GetIconColor(normalizedEffectId, effectDatabase, isMonsterInnateTrait);
    }

    public static Color ComposeIconColor(Color effectColor, Color currentImageColor)
    {
        return new Color(effectColor.r, effectColor.g, effectColor.b, currentImageColor.a);
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
        if (statusIconImage == null)
        {
            Transform iconRoot = FindChildRecursive(transform, "Icon");
            Transform statusIcon = FindChildRecursive(iconRoot, "StatusIcon");
            if (statusIcon != null)
                statusIconImage = statusIcon.GetComponent<Image>();
        }

        if (nameText == null)
            nameText = ResolveText("Name");

        if (detailText == null)
            detailText = ResolveText("Detail");
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
