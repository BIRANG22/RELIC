using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonsterSkillInfoItemUI : MonoBehaviour
{
    [Header("Icon")]
    [SerializeField] private Image timelineIconImage;
    [SerializeField] private Image rangeIconImage;

    [Header("Text")]
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private Text legacySkillNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Text legacyDescriptionText;

    [Header("Auto Find Names")]
    [SerializeField] private string timelineIconObjectName = "TimelineIcon";
    [SerializeField] private string rangeIconObjectName = "RangeIcon";
    [SerializeField] private string skillNameTextObjectName = "SkillNameText";
    [SerializeField] private string fallbackSkillNameTextObjectName = "Name";
    [SerializeField] private string descriptionTextObjectName = "DescriptionText";

    private void Awake()
    {
        ResolveReferencesIfNeeded();
    }

    public void Bind(string skillName, string description, Sprite timelineIcon, Sprite rangeIcon)
    {
        ResolveReferencesIfNeeded();

        string safeName = string.IsNullOrWhiteSpace(skillName) ? string.Empty : skillName.Trim();
        string safeDescription = string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim();

        SetIcon(timelineIconImage, timelineIcon);
        SetIcon(rangeIconImage, rangeIcon);

        if (skillNameText != null)
            skillNameText.text = safeName;

        if (legacySkillNameText != null)
            legacySkillNameText.text = safeName;

        if (descriptionText != null)
            descriptionText.text = safeDescription;

        if (legacyDescriptionText != null)
            legacyDescriptionText.text = safeDescription;
    }

    private static void SetIcon(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
        image.gameObject.SetActive(sprite != null);
    }

    private void ResolveReferencesIfNeeded()
    {
        if (timelineIconImage == null)
            timelineIconImage = ResolveImageReference(timelineIconObjectName);

        if (rangeIconImage == null)
            rangeIconImage = ResolveImageReference(rangeIconObjectName);

        if (skillNameText == null && legacySkillNameText == null)
        {
            ResolveTextReference(skillNameTextObjectName, ref skillNameText, ref legacySkillNameText);

            if (skillNameText == null && legacySkillNameText == null)
                ResolveTextReference(fallbackSkillNameTextObjectName, ref skillNameText, ref legacySkillNameText);
        }

        if (descriptionText == null && legacyDescriptionText == null)
            ResolveTextReference(descriptionTextObjectName, ref descriptionText, ref legacyDescriptionText);
    }

    private Image ResolveImageReference(string objectName)
    {
        Transform root = FindChildRecursive(transform, objectName);
        if (root == null)
            return null;

        Image image = root.GetComponent<Image>();
        if (image == null)
            image = root.GetComponentInChildren<Image>(true);

        return image;
    }

    private void ResolveTextReference(string objectName, ref TMP_Text tmpText, ref Text legacyText)
    {
        Transform root = FindChildRecursive(transform, objectName);
        if (root == null)
            return;

        tmpText = root.GetComponent<TMP_Text>();
        if (tmpText == null)
            tmpText = root.GetComponentInChildren<TMP_Text>(true);

        legacyText = root.GetComponent<Text>();
        if (legacyText == null)
            legacyText = root.GetComponentInChildren<Text>(true);
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
