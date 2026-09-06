using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimelineSkillHoverPopupView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private RectTransform popupRect;

    [Header("Auto Size")]
    [SerializeField] private bool autoSizeToName = true;
    [SerializeField] private Vector2 textPadding = new Vector2(36f, 16f);
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
        // OnValidate/Awake 단계에서는 RectTransform 크기를 변경하지 않습니다.
        // 팝업 크기는 실제 스킬 이름이 설정되는 Set()에서만 갱신합니다.
        AutoBindReferences();
    }

    public void Set(string skillName, string effectDescription)
    {
        Set(skillName);
    }

    public void Set(string skillName, string effectDescription, Sprite rangeIcon)
    {
        Set(skillName);
    }

    public void Set(string skillName)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        AutoBindReferences();
        DisableRaycastTargets();
        ApplyFontAsset();

        if (nameText != null)
            nameText.text = string.IsNullOrWhiteSpace(skillName) ? emptyNameText : skillName;

        RefreshSize();
    }

    private void RefreshSize()
    {
        if (!autoSizeToName || nameText == null)
            return;

        if (popupRect == null)
            popupRect = transform as RectTransform;

        if (popupRect == null)
            return;

        nameText.ForceMeshUpdate();

        string displayText = nameText.text ?? string.Empty;
        Vector2 preferredSize = nameText.GetPreferredValues(displayText);

        float targetWidth = Mathf.Max(minWidth, preferredSize.x + Mathf.Max(0f, textPadding.x));
        float targetHeight = Mathf.Max(minHeight, preferredSize.y + Mathf.Max(0f, textPadding.y));

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

        if (!resizeBackgrounds)
            return;

        CacheBackgroundRects();

        for (int i = 0; i < backgroundRects.Count; i++)
        {
            RectTransform backgroundRect = backgroundRects[i];
            if (backgroundRect == null)
                continue;

            // Stretch 앵커는 부모 크기를 자동으로 따라가므로 별도 크기 지정이 필요 없습니다.
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
