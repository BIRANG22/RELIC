using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OptionPanelUI : MonoBehaviour
{
    [Header("Contents")]
    [SerializeField] private GameObject soundContent;
    [SerializeField] private GameObject languageContent;
    [SerializeField] private GameObject resolutionContent;

    [Header("Resolution")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    private bool isResolutionDropdownReady;

    private void OnEnable()
    {
        SetupResolutionDropdown();
        ShowSound();
    }

    private void OnDestroy()
    {
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
    }

    public void ShowSound()
    {
        SetContentActive(soundContent, true);
        SetContentActive(languageContent, false);
        SetContentActive(resolutionContent, false);
    }

    public void ShowLanguage()
    {
        SetContentActive(soundContent, false);
        SetContentActive(languageContent, true);
        SetContentActive(resolutionContent, false);
    }

    public void ShowResolution()
    {
        SetupResolutionDropdown();

        SetContentActive(soundContent, false);
        SetContentActive(languageContent, false);
        SetContentActive(resolutionContent, true);
    }

    public void SaveProgress()
    {
        if (SaveSystem.Instance == null)
        {
            Debug.LogWarning("[OptionPanelUI] SaveSystem is not ready. Progress was not saved.");
            return;
        }

        SaveSystem.Instance.SaveCurrentProgress();
    }

    private void SetupResolutionDropdown()
    {
        TMP_Dropdown contentDropdown = resolutionContent != null
            ? resolutionContent.GetComponentInChildren<TMP_Dropdown>(true)
            : null;

        if (contentDropdown != null && resolutionDropdown != contentDropdown)
        {
            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);

            resolutionDropdown = contentDropdown;
        }

        if (resolutionDropdown == null)
            return;

        isResolutionDropdownReady = false;

        resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
        resolutionDropdown.ClearOptions();

        List<string> labels = ResolutionManager.GetSupportedResolutionLabels();
        var options = new List<TMP_Dropdown.OptionData>(labels.Count);

        for (int i = 0; i < labels.Count; i++)
            options.Add(new TMP_Dropdown.OptionData(labels[i]));

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.SetValueWithoutNotify(ResolutionManager.CurrentResolutionIndex);
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        isResolutionDropdownReady = true;
    }

    private void OnResolutionChanged(int index)
    {
        if (!isResolutionDropdownReady)
            return;

        ResolutionManager.ApplyResolution(index, true);
    }

    private static void SetContentActive(GameObject content, bool active)
    {
        if (content != null)
            content.SetActive(active);
    }
}
