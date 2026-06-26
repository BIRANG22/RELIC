using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimelineSkillHoverPopupView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private Image rangeIconImage;

    [Header("Font Override")]
    [SerializeField] private bool applyFontAssetToTexts = false;
    [SerializeField] private TMP_FontAsset textFontAsset;

    [Header("No Range Icon Layout")]
    [SerializeField] private bool expandEffectTextWhenNoRangeIcon = true;
    [SerializeField] private float noRangeEffectTextXOffset = -30f;
    [SerializeField] private float noRangeEffectTextWidth = 370f;

    [Header("Fallback Text")]
    [SerializeField] private string emptyNameText = "";
    [TextArea]
    [SerializeField] private string emptyEffectText = "";

    private RectTransform effectTextRect;
    private Vector2 originalEffectTextAnchoredPosition;
    private Vector2 originalEffectTextSizeDelta;
    private bool hasOriginalEffectTextLayout;

    private void Awake()
    {
        NormalizeNoRangeIconLayoutDefaults();
        AutoBindReferences();
        CacheOriginalEffectTextLayout();
        DisableRaycastTargets();
        ApplyFontAsset();
        HideRangeIcon();
        ApplyNoRangeIconLayout();
    }

    private void OnValidate()
    {
        NormalizeNoRangeIconLayoutDefaults();
        AutoBindReferences();
    }

    public void Set(string skillName, string effectDescription)
    {
        Set(skillName, effectDescription, null);
    }

    public void Set(string skillName, string effectDescription, Sprite rangeIcon)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        AutoBindReferences();
        CacheOriginalEffectTextLayout();
        DisableRaycastTargets();
        ApplyFontAsset();

        if (nameText != null)
            nameText.text = string.IsNullOrWhiteSpace(skillName) ? emptyNameText : skillName;

        if (effectText != null)
            effectText.text = string.IsNullOrWhiteSpace(effectDescription) ? emptyEffectText : effectDescription;

        SetRangeIcon(rangeIcon);
    }


    private void NormalizeNoRangeIconLayoutDefaults()
    {
        // 기존 프리팹/씬에 저장되어 있던 이전 기본값을 새 기본값으로 갱신합니다.
        if (Mathf.Approximately(noRangeEffectTextXOffset, -45f) || Mathf.Approximately(noRangeEffectTextXOffset, -50f) || Mathf.Approximately(noRangeEffectTextXOffset, -75f))
            noRangeEffectTextXOffset = -30f;

        if (Mathf.Approximately(noRangeEffectTextWidth, 300f) || Mathf.Approximately(noRangeEffectTextWidth, 350f))
            noRangeEffectTextWidth = 370f;
    }

    private void ApplyFontAsset()
    {
        if (!applyFontAssetToTexts)
            return;

        if (textFontAsset == null)
            return;

        if (nameText != null)
            nameText.font = textFontAsset;

        if (effectText != null)
            effectText.font = textFontAsset;
    }

    private void SetRangeIcon(Sprite rangeIcon)
    {
        if (rangeIconImage == null)
        {
            ApplyNoRangeIconLayout();
            return;
        }

        if (rangeIcon == null)
        {
            HideRangeIcon();
            ApplyNoRangeIconLayout();
            return;
        }

        rangeIconImage.sprite = rangeIcon;
        rangeIconImage.gameObject.SetActive(true);
        RestoreOriginalEffectTextLayout();
    }

    private void HideRangeIcon()
    {
        if (rangeIconImage != null)
            rangeIconImage.gameObject.SetActive(false);
    }

    private void CacheOriginalEffectTextLayout()
    {
        if (hasOriginalEffectTextLayout)
            return;

        if (effectText == null)
            return;

        effectTextRect = effectText.rectTransform;
        if (effectTextRect == null)
            return;

        originalEffectTextAnchoredPosition = effectTextRect.anchoredPosition;
        originalEffectTextSizeDelta = effectTextRect.sizeDelta;
        hasOriginalEffectTextLayout = true;
    }

    private void ApplyNoRangeIconLayout()
    {
        if (!expandEffectTextWhenNoRangeIcon)
            return;

        if (effectText == null)
            return;

        effectTextRect = effectText.rectTransform;
        if (effectTextRect == null)
            return;

        Vector2 anchoredPosition = effectTextRect.anchoredPosition;
        anchoredPosition.x = hasOriginalEffectTextLayout
            ? originalEffectTextAnchoredPosition.x + noRangeEffectTextXOffset
            : anchoredPosition.x + noRangeEffectTextXOffset;
        effectTextRect.anchoredPosition = anchoredPosition;

        Vector2 sizeDelta = effectTextRect.sizeDelta;
        sizeDelta.x = noRangeEffectTextWidth;
        effectTextRect.sizeDelta = sizeDelta;
    }

    private void RestoreOriginalEffectTextLayout()
    {
        if (!hasOriginalEffectTextLayout)
            return;

        if (effectText == null)
            return;

        effectTextRect = effectText.rectTransform;
        if (effectTextRect == null)
            return;

        effectTextRect.anchoredPosition = originalEffectTextAnchoredPosition;
        effectTextRect.sizeDelta = originalEffectTextSizeDelta;
    }

    private void AutoBindReferences()
    {
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

        if (effectText == null)
            effectText = FindText("EffectText");

        if (effectText == null)
            effectText = FindText("Effect");

        if (rangeIconImage == null)
            rangeIconImage = FindImage("RangeIcon");

        if (rangeIconImage == null)
            rangeIconImage = FindImage("RangeIconImage");

        if (rangeIconImage == null)
            rangeIconImage = FindImage("SkillRangeIcon");

        if (rangeIconImage == null)
            rangeIconImage = FindImage("RangeImage");
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
