using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RuneSlotButton : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image borderImage;
    [SerializeField] private GameObject lockObject;

    [Header("Border Color")]
    [SerializeField] private string equippedBorderColorHex = "#4E66DF";

    private RuneSettingPanel owner;
    private int slotIndex;
    private RuneData equippedRune;
    private bool isLocked;

    private Color normalBorderColor = Color.white;
    private bool isNormalBorderColorCached;

    public int SlotIndex => slotIndex;
    public RuneData EquippedRune => equippedRune;
    public bool IsLocked => isLocked;

    private void Awake()
    {
        CacheBorderImage();
        CacheNormalBorderColor();
        ApplyBorderVisualState();
    }

    private void OnEnable()
    {
        CacheBorderImage();
        CacheNormalBorderColor();
        ApplyBorderVisualState();
    }

    public void Init(RuneSettingPanel panel, int index)
    {
        owner = panel;
        slotIndex = index;

        CacheBorderImage();
        CacheNormalBorderColor();
        ApplyBorderVisualState();

        if (button != null)
        {
            button.onClick.RemoveListener(Execute);
            button.onClick.AddListener(Execute);
        }
    }

    public void Execute()
    {
        if (owner == null)
            return;

        owner.UnequipRuneFromSlot(this);
    }

    public void SetRune(RuneData runeData)
    {
        equippedRune = runeData;

        if (nameText != null)
            nameText.text = equippedRune != null ? equippedRune.Name : "";

        if (iconImage != null)
        {
            Sprite icon = GetRuneIcon(equippedRune);

            iconImage.enabled = icon != null;
            iconImage.sprite = icon;
            iconImage.color = Color.white;
        }

        ApplyBorderVisualState();
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;

        if (lockObject != null)
            lockObject.SetActive(isLocked);

        if (button != null)
            button.interactable = !isLocked;

        ApplyBorderVisualState();
    }

    private void CacheBorderImage()
    {
        if (borderImage != null)
            return;

        Transform borderTransform = FindDeepChild(transform, "Border");

        if (borderTransform == null)
            borderTransform = FindDeepChild(transform, "Frame");

        if (borderTransform == null)
            borderTransform = FindDeepChild(transform, "Outline");

        if (borderTransform != null)
            borderImage = borderTransform.GetComponent<Image>();

        if (borderImage == null)
        {
            Image selfImage = GetComponent<Image>();

            if (selfImage != null && selfImage != iconImage)
                borderImage = selfImage;
        }
    }

    private void CacheNormalBorderColor()
    {
        if (borderImage == null)
            return;

        if (isNormalBorderColorCached)
            return;

        normalBorderColor = borderImage.color;
        isNormalBorderColorCached = true;
    }

    private void ApplyBorderVisualState()
    {
        if (borderImage == null)
            return;

        CacheNormalBorderColor();

        if (equippedRune != null)
        {
            borderImage.color = GetEquippedBorderColor();
            return;
        }

        borderImage.color = normalBorderColor;
    }

    private Color GetEquippedBorderColor()
    {
        if (ColorUtility.TryParseHtmlString(equippedBorderColorHex, out Color color))
            return color;

        if (ColorUtility.TryParseHtmlString("#4E66DF", out color))
            return color;

        return normalBorderColor;
    }

    private Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child == null)
                continue;

            if (child.name == childName)
                return child;

            Transform result = FindDeepChild(child, childName);

            if (result != null)
                return result;
        }

        return null;
    }

    private Sprite GetRuneIcon(RuneData runeData)
    {
        if (runeData == null)
            return null;

        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.RuneIconDatabase == null)
            return null;

        if (DataManager.Instance.RuneIconDatabase.TryGetIcon(runeData.RuneId, out var icon))
            return icon;

        return null;
    }
}
