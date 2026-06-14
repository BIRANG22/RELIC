using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillSlotButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;

    [Header("Select Visual")]
    [SerializeField] private Image borderImage;
    [SerializeField] private Color selectedBorderColor = new Color32(0x4E, 0x66, 0xDF, 0xFF);
    [SerializeField] private float selectedScale = 1.1f;
    [SerializeField] private bool autoBindBorderImage = true;

    private SkillSettingPanel owner;
    private int slotIndex;
    private SkillMasterData equippedSkill;

    private Vector3 defaultScale;
    private Color defaultBorderColor = Color.white;
    private bool hasDefaultBorderColor;
    private bool isSelected;

    public int SlotIndex => slotIndex;
    public SkillMasterData EquippedSkill => equippedSkill;

    private void Awake()
    {
        CacheDefaultVisualState();
    }

    private void OnEnable()
    {
        CacheDefaultVisualState();
        ClearSelectionVisualState();
    }

    private void OnDisable()
    {
        ClearSelectionVisualState();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (selectedScale <= 0f)
            selectedScale = 1.1f;

        if (button == null)
            button = GetComponent<Button>();

        if (iconImage == null)
            iconImage = FindChildImageByName("Icon", "IconImage", "SkillIcon");

        if (autoBindBorderImage && borderImage == null)
            borderImage = FindBorderImage();
    }
#endif

    public void Init(SkillSettingPanel panel, int index)
    {
        owner = panel;
        slotIndex = index;

        CacheDefaultVisualState();

        if (button != null)
        {
            button.onClick.RemoveListener(Execute);
            button.onClick.AddListener(Execute);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (owner != null && equippedSkill != null)
            owner.ShowSkillInfo(equippedSkill);

        if (!isSelected)
            SetBorderColor(selectedBorderColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelected)
            RestoreDefaultBorderColor();
    }

    public void Execute()
    {
        if (owner == null)
        {
            Debug.LogWarning("[SkillSlotButton] owner is null.");
            return;
        }

        owner.ShowSkillInfo(equippedSkill);
        owner.OpenSkillSelectPanel(this);
    }

    public void SetSkill(SkillMasterData skill)
    {
        equippedSkill = skill;

        if (nameText != null)
            nameText.text = skill != null ? skill.Name : "";

        if (iconImage != null)
        {
            Sprite icon = null;

            if (skill != null)
                icon = SkillIconUtility.GetSkillIcon(skill.SkillId);

            iconImage.enabled = icon != null;
            iconImage.sprite = icon;
            iconImage.color = Color.white;
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        ApplyVisualState(selected);
    }

    private void ClearSelectionVisualState()
    {
        isSelected = false;
        ApplyVisualState(false);
    }

    private void ApplyVisualState(bool selected)
    {
        transform.localScale = selected ? defaultScale * selectedScale : defaultScale;

        if (selected)
            SetBorderColor(selectedBorderColor);
        else
            RestoreDefaultBorderColor();
    }

    private void CacheDefaultVisualState()
    {
        if (defaultScale == Vector3.zero)
            defaultScale = transform.localScale;

        if (button == null)
            button = GetComponent<Button>();

        if (autoBindBorderImage && borderImage == null)
            borderImage = FindBorderImage();

        if (borderImage != null && !hasDefaultBorderColor)
        {
            defaultBorderColor = borderImage.color;
            hasDefaultBorderColor = true;
        }
    }

    private void SetBorderColor(Color color)
    {
        if (borderImage != null)
            borderImage.color = color;
    }

    private void RestoreDefaultBorderColor()
    {
        if (borderImage != null && hasDefaultBorderColor)
            borderImage.color = defaultBorderColor;
    }

    private Image FindBorderImage()
    {
        Image image = FindChildImageByName("BorderImage", "Border", "Frame", "Outline", "SelectImage", "SelectedImage");

        if (image != null)
            return image;

        Image[] images = GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            Image candidate = images[i];

            if (candidate == null)
                continue;

            if (candidate == iconImage)
                continue;

            if (button != null && candidate == button.targetGraphic)
                continue;

            return candidate;
        }

        return null;
    }

    private Image FindChildImageByName(params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            Transform target = FindChildRecursive(transform, names[i]);

            if (target == null)
                continue;

            Image image = target.GetComponent<Image>();

            if (image != null)
                return image;
        }

        return null;
    }

    private Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
            return null;

        if (root.name == targetName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildRecursive(root.GetChild(i), targetName);

            if (result != null)
                return result;
        }

        return null;
    }
}
