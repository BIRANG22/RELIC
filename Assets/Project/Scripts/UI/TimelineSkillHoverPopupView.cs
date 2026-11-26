using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimelineSkillHoverPopupView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image rangeIconImage;
    [SerializeField] private RectTransform popupRect;

    [Header("Auto Size")]
    [SerializeField] private bool autoSizeToName = true;
    [SerializeField] private Vector2 textPadding = new Vector2(36f, 16f);
    [SerializeField, Min(0f)] private float contentSpacing = 8f;
    [SerializeField] private float minWidth = 80f;
    [SerializeField] private float minHeight = 40f;
    [SerializeField] private float maxWidth = 0f;
    [SerializeField] private bool resizeBackgrounds = true;

    [Header("Font Override")]
    [SerializeField] private bool applyFontAssetToTexts = false;
    [SerializeField] private TMP_FontAsset textFontAsset;

    [Header("Fallback Text")]
    [SerializeField] private string emptyNameText = "";

    private readonly List<RectTransform> backgroundRects = new List<RectTransform>();

    private void Awake()
    {
        AutoBindReferences();
        DisableRaycastTargets();
        ApplyFontAsset();
    }

    private void OnValidate()
    {
        // Do not resize RectTransforms during OnValidate or Awake.
        // Resize only after Set supplies the displayed action content.
        AutoBindReferences();
    }

    public void Set(string skillName, string effectDescription)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        AutoBindReferences();
        DisableRaycastTargets();
        ApplyFontAsset();

        if (nameText != null)
        {
            string displayName = string.IsNullOrWhiteSpace(skillName) ? emptyNameText : skillName;
            nameText.text = descriptionText == null && !string.IsNullOrWhiteSpace(effectDescription)
                ? $"{displayName}\n{effectDescription}"
                : displayName;
        }

        bool showDescription = !string.IsNullOrWhiteSpace(effectDescription);
        if (descriptionText != null)
        {
            descriptionText.text = showDescription ? effectDescription : string.Empty;
            descriptionText.gameObject.SetActive(showDescription);
        }

        if (rangeIconImage != null)
        {
            rangeIconImage.sprite = null;
            rangeIconImage.gameObject.SetActive(false);
        }

        RefreshSize();
    }

    public void Set(string skillName)
    {
        Set(skillName, string.Empty);
    }

    private void RefreshSize()
    {
        if (!autoSizeToName || nameText == null)
            return;

        if (popupRect == null)
            popupRect = transform as RectTransform;

        if (popupRect == null)
            return;

        LocaleTMPFontGuard.Prepare(nameText);
        nameText.ForceMeshUpdate();
        if (descriptionText != null && descriptionText.gameObject.activeSelf)
        {
            LocaleTMPFontGuard.Prepare(descriptionText);
            descriptionText.ForceMeshUpdate();
        }

        string displayText = nameText.text ?? string.Empty;
        Vector2 preferredSize = nameText.GetPreferredValues(displayText);
        Vector2 descriptionSize = descriptionText != null && descriptionText.gameObject.activeSelf
            ? descriptionText.GetPreferredValues(descriptionText.text ?? string.Empty)
            : Vector2.zero;
        bool hasDescription = descriptionSize.sqrMagnitude > 0f;

        float textWidth = Mathf.Max(preferredSize.x, descriptionSize.x);
        float descriptionSpace = hasDescription ? contentSpacing + descriptionSize.y : 0f;
        float targetWidth = Mathf.Max(minWidth, textWidth + Mathf.Max(0f, textPadding.x));
        float targetHeight = Mathf.Max(minHeight, preferredSize.y + descriptionSpace + Mathf.Max(0f, textPadding.y));

        if (maxWidth > 0f)
            targetWidth = Mathf.Min(targetWidth, maxWidth);

        popupRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
        popupRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);

        RectTransform nameRect = nameText.rectTransform;
        if (nameRect != null && IsFixedAnchor(nameRect))
        {
            nameRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, preferredSize.x);
            nameRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredSize.y);
        }

        if (descriptionText != null && descriptionText.gameObject.activeSelf && IsFixedAnchor(descriptionText.rectTransform))
        {
            descriptionText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, descriptionSize.x);
            descriptionText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, descriptionSize.y);
        }

        if (!resizeBackgrounds)
            return;

        CacheBackgroundRects();

        for (int i = 0; i < backgroundRects.Count; i++)
        {
            RectTransform backgroundRect = backgroundRects[i];
            if (backgroundRect == null)
                continue;

            // Stretch anchors already follow the parent size.
            if (!IsFixedAnchor(backgroundRect))
                continue;

            backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
            backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        }
    }

    private static bool IsFixedAnchor(RectTransform rectTransform)
    {
        if (rectTransform == null)
            return false;

        return Mathf.Approximately(rectTransform.anchorMin.x, rectTransform.anchorMax.x)
            && Mathf.Approximately(rectTransform.anchorMin.y, rectTransform.anchorMax.y);
    }

    private void ApplyFontAsset()
    {
        if (!applyFontAssetToTexts || textFontAsset == null)
            return;

        if (nameText != null)
            nameText.font = textFontAsset;

        if (descriptionText != null)
            descriptionText.font = textFontAsset;
    }

    private void AutoBindReferences()
    {
        if (popupRect == null)
            popupRect = transform as RectTransform;

        if (backgroundImage == null)
            backgroundImage = FindImage("BackGround");

        if (backgroundImage == null)
            backgroundImage = FindImage("Background");

        if (backgroundImage == null)
            backgroundImage = FindImage("Image");

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (nameText == null)
            nameText = FindText("NameText");

        if (nameText == null)
            nameText = FindText("Name");

        if (descriptionText == null)
            descriptionText = FindText("DescriptionText");

        if (descriptionText == null)
            descriptionText = FindText("Description");

        if (rangeIconImage == null)
            rangeIconImage = FindImage("RangeIcon");

        CacheBackgroundRects();
    }

    private void CacheBackgroundRects()
    {
        backgroundRects.Clear();

        Image[] images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null)
                continue;

            string objectName = image.gameObject.name;
            bool isBackground = objectName.StartsWith("BackGround", System.StringComparison.OrdinalIgnoreCase)
                || objectName.StartsWith("Background", System.StringComparison.OrdinalIgnoreCase);

            if (!isBackground && image != backgroundImage)
                continue;

            RectTransform rect = image.rectTransform;
            if (rect != null && !backgroundRects.Contains(rect))
                backgroundRects.Add(rect);
        }
    }

    private Image FindImage(string objectName)
    {
        Transform found = FindChildRecursive(transform, objectName);
        return found != null ? found.GetComponent<Image>() : null;
    }

    private TMP_Text FindText(string objectName)
    {
        Transform found = FindChildRecursive(transform, objectName);
        return found != null ? found.GetComponent<TMP_Text>() : null;
    }

    private Transform FindChildRecursive(Transform root, string objectName)
    {
        if (root == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child.name == objectName)
                return child;

            Transform found = FindChildRecursive(child, objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private void DisableRaycastTargets()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].raycastTarget = false;
        }
    }
}
