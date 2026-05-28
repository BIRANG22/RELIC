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
    [SerializeField] private GameObject lockObject;

    private RuneSettingPanel owner;
    private int slotIndex;
    private RuneData equippedRune;
    private bool isLocked;

    public int SlotIndex => slotIndex;
    public RuneData EquippedRune => equippedRune;
    public bool IsLocked => isLocked;

    public void Init(RuneSettingPanel panel, int index)
    {
        owner = panel;
        slotIndex = index;

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
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;

        if (lockObject != null)
            lockObject.SetActive(isLocked);

        if (button != null)
            button.interactable = !isLocked;
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