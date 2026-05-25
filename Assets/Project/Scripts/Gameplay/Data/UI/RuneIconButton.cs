using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RuneIconButton : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject equippedObject;

    private RuneSettingPanel owner;
    private RuneData currentRuneData;

    public RuneData CurrentRuneData => currentRuneData;

    public void Init(RuneSettingPanel panel)
    {
        owner = panel;

        if (button != null)
        {
            button.onClick.RemoveListener(Execute);
            button.onClick.AddListener(Execute);
        }
    }

    public void SetRuneData(RuneData runeData)
    {
        currentRuneData = runeData;

        bool hasRune = currentRuneData != null;
        gameObject.SetActive(hasRune);

        if (!hasRune)
            return;

        if (nameText != null)
            nameText.text = currentRuneData.Name;

        if (iconImage != null)
        {
            Sprite icon = GetRuneIcon(currentRuneData);

            iconImage.enabled = icon != null;
            iconImage.sprite = icon;
            iconImage.color = Color.white;
        }
    }

    public void SetEquippedState(bool equipped)
    {
        if (equippedObject != null)
            equippedObject.SetActive(equipped);
    }

    public void Execute()
    {
        Debug.Log(currentRuneData != null
            ? $"[RuneIconButton] Click: {currentRuneData.RuneId}"
            : "[RuneIconButton] Click: null");

        if (owner == null)
        {
            Debug.LogWarning("[RuneIconButton] owner is null.");
            return;
        }

        if (currentRuneData == null)
            return;

        owner.TryEquipRuneToFirstEmptySlot(currentRuneData);
    }

    private Sprite GetRuneIcon(RuneData runeData)
    {
        if (runeData == null)
        {
            Debug.LogWarning("[RuneIconButton] RuneData is null.");
            return null;
        }

        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[RuneIconButton] DataManager is null.");
            return null;
        }

        if (DataManager.Instance.RuneIconDatabase == null)
        {
            Debug.LogWarning("[RuneIconButton] RuneIconDatabase is null.");
            return null;
        }

        if (DataManager.Instance.RuneIconDatabase.TryGetIcon(runeData.RuneId, out var icon))
        {
            Debug.Log($"[RuneIconButton] Icon Found: {runeData.RuneId}");
            return icon;
        }

        Debug.LogWarning($"[RuneIconButton] Icon Missing: {runeData.RuneId}");
        return null;
    }
}