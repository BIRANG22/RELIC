using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonsterPatternInfoItemUI : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text patternText;
    [SerializeField] private Text legacyPatternText;
    [SerializeField] private TMP_Text orderText;
    [SerializeField] private Text legacyOrderText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Text legacyDescriptionText;

    [Header("Auto Find Names")]
    [SerializeField] private string patternTextObjectName = "PatternText";
    [SerializeField] private string orderTextObjectName = "OrderText";
    [SerializeField] private string descriptionTextObjectName = "DescriptionText";

    private void Awake()
    {
        ResolveReferencesIfNeeded();
    }

    public void Bind(int order, string description)
    {
        ResolveReferencesIfNeeded();

        string safeDescription = string.IsNullOrWhiteSpace(description) ? string.Empty : description.Trim();
        string orderString = order > 0 ? order.ToString() : string.Empty;
        string fullText = order > 0 ? $"{order}. {safeDescription}" : safeDescription;

        if (patternText != null)
            patternText.text = fullText;

        if (legacyPatternText != null)
            legacyPatternText.text = fullText;

        if (orderText != null)
            orderText.text = orderString;

        if (legacyOrderText != null)
            legacyOrderText.text = orderString;

        if (descriptionText != null)
            descriptionText.text = safeDescription;

        if (legacyDescriptionText != null)
            legacyDescriptionText.text = safeDescription;
    }

    private void ResolveReferencesIfNeeded()
    {
        if (patternText == null && legacyPatternText == null)
            ResolveTextReference(patternTextObjectName, ref patternText, ref legacyPatternText);

        if (orderText == null && legacyOrderText == null)
            ResolveTextReference(orderTextObjectName, ref orderText, ref legacyOrderText);

        if (descriptionText == null && legacyDescriptionText == null)
            ResolveTextReference(descriptionTextObjectName, ref descriptionText, ref legacyDescriptionText);

        if (patternText == null && legacyPatternText == null && orderText == null && legacyOrderText == null && descriptionText == null && legacyDescriptionText == null)
        {
            patternText = GetComponent<TMP_Text>();
            if (patternText == null)
                patternText = GetComponentInChildren<TMP_Text>(true);

            legacyPatternText = GetComponent<Text>();
            if (legacyPatternText == null)
                legacyPatternText = GetComponentInChildren<Text>(true);
        }
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
