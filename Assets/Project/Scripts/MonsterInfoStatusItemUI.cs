using Relic.Gameplay.Data;
using TMPro;
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

    private void Awake()
    {
        ResolveReferences();
    }

    public void Bind(StatusEffectRuntimeData statusData)
    {
        ResolveReferences();

        if (statusData == null || string.IsNullOrWhiteSpace(statusData.EffectId))
        {
            Clear();
            return;
        }

        EffectMasterData effectData = ResolveEffectData(statusData.EffectId);

        if (statusIconImage != null)
            SetImage(statusIconImage, ResolveStatusIcon(statusData.EffectId));

        string effectName = effectData != null
            ? GameDataLocalization.EffectName(effectData)
            : statusData.EffectId.Trim();

        if (nameText != null)
        {
            nameText.text = statusData.Stack > 0
                ? $"{effectName} {statusData.Stack}"
                : effectName;
        }

        if (detailText != null)
        {
            detailText.text = effectData != null
                ? GameDataLocalization.EffectTooltip(effectData)
                : string.Empty;
        }
    }

    private void Clear()
    {
        if (statusIconImage != null)
            SetImage(statusIconImage, null);
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
