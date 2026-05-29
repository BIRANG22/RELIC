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

    [Header("Locked UI")]
    [SerializeField] private GameObject lockedObject;
    [SerializeField] private TMP_Text requiredLevelText;

    private RuneSettingPanel owner;
    private RuneData currentRuneData;

    private bool isLocked;
    private int requiredLevel;

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
        SetRuneData(runeData, false, 0);
    }

    public void SetRuneData(RuneData runeData, bool locked, int requiredLevel)
    {
        currentRuneData = runeData;
        isLocked = locked;
        this.requiredLevel = requiredLevel;

        bool hasRune = currentRuneData != null;
        gameObject.SetActive(hasRune);

        if (!hasRune)
        {
            SetEquippedState(false);
            SetLockedState(false, 0);
            return;
        }

        if (nameText != null)
            nameText.text = currentRuneData.Name;

        if (iconImage != null)
        {
            Sprite icon = GetRuneIcon(currentRuneData);

            iconImage.enabled = icon != null;
            iconImage.sprite = icon;
            iconImage.color = isLocked ? new Color(0.35f, 0.35f, 0.35f, 1f) : Color.white;
        }

        SetLockedState(isLocked, this.requiredLevel);
    }

    public void SetEquippedState(bool equipped)
    {
        if (equippedObject != null)
            equippedObject.SetActive(equipped);
    }

    private void SetLockedState(bool locked, int level)
    {
        if (lockedObject != null)
            lockedObject.SetActive(locked);

        if (requiredLevelText != null)
        {
            requiredLevelText.gameObject.SetActive(locked);
            requiredLevelText.text = locked ? "LV. " + level : "";
        }

        if (button != null)
            button.interactable = currentRuneData != null;
    }

    public void Execute()
    {
        if (owner == null)
        {
            Debug.LogWarning("[RuneIconButton] owner is null.");
            return;
        }

        if (currentRuneData == null)
            return;

        owner.TrySelectRuneIcon(currentRuneData, isLocked, requiredLevel);
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
            return icon;

        Debug.LogWarning($"[RuneIconButton] Icon Missing: {runeData.RuneId}");
        return null;
    }
}