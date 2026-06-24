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

    [Header("Range Icon")]
    [SerializeField] private Vector2 rangeIconSize = new Vector2(60f, 60f);
    [SerializeField] private float rangeIconGap = 4f;

    [Header("Fallback Text")]
    [SerializeField] private string emptyNameText = "";
    [TextArea]
    [SerializeField] private string emptyEffectText = "";

    private void Awake()
    {
        AutoBindReferences();
        DisableRaycastTargets();
    }

    private void OnValidate()
    {
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
        DisableRaycastTargets();

        if (nameText != null)
            nameText.text = string.IsNullOrWhiteSpace(skillName) ? emptyNameText : skillName;

        if (effectText != null)
            effectText.text = string.IsNullOrWhiteSpace(effectDescription) ? emptyEffectText : effectDescription;

        SetRangeIcon(rangeIcon);
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
            rangeIconImage = FindImage("SkillRangeIcon");

        if (rangeIconImage == null)
            rangeIconImage = FindImage("RangeImage");
    }

    private void SetRangeIcon(Sprite rangeIcon)
    {
        if (rangeIcon == null)
        {
            if (rangeIconImage != null)
                rangeIconImage.gameObject.SetActive(false);

            return;
        }

        if (rangeIconImage == null)
            rangeIconImage = CreateRangeIconImage();

        if (rangeIconImage == null)
            return;

        rangeIconImage.sprite = rangeIcon;
        rangeIconImage.preserveAspect = true;
        rangeIconImage.raycastTarget = false;
        rangeIconImage.gameObject.SetActive(true);
    }

    private Image CreateRangeIconImage()
    {
        RectTransform parent = effectText != null && effectText.transform.parent != null
            ? effectText.transform.parent as RectTransform
            : transform as RectTransform;

        if (parent == null)
            return null;

        GameObject iconObject = new GameObject("RangeIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.layer = gameObject.layer;
        iconObject.transform.SetParent(parent, false);

        RectTransform iconRect = iconObject.transform as RectTransform;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = rangeIconSize;

        RectTransform effectRect = effectText != null ? effectText.transform as RectTransform : null;
        if (effectRect != null)
        {
            float iconX = effectRect.anchoredPosition.x - effectRect.sizeDelta.x * 0.5f - rangeIconSize.x * 0.5f - rangeIconGap;
            iconRect.anchoredPosition = new Vector2(iconX, effectRect.anchoredPosition.y);
        }
        else
        {
            iconRect.anchoredPosition = Vector2.zero;
        }

        Image image = iconObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
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
