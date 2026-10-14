using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

/// <summary>Legacy scene hook retained for serialized compatibility. Font ownership belongs to LocalizedTMPFont.</summary>
public sealed class OptionTMPFontEnforcer : MonoBehaviour
{
    private void OnEnable() => ApplyFonts();

    public void ApplyFonts()
    {
        LocalizedTMPFont[] localizers = GetComponentsInChildren<LocalizedTMPFont>(true);
        for (int i = 0; i < localizers.Length; i++)
            localizers[i]?.RefreshForLocale(LocalizationSettings.SelectedLocale);

        TMP_Dropdown[] dropdowns = GetComponentsInChildren<TMP_Dropdown>(true);
        for (int i = 0; i < dropdowns.Length; i++)
            RefreshDropdownTemplate(dropdowns[i]);
    }

    private static void RefreshDropdownTemplate(TMP_Dropdown dropdown)
    {
        if (dropdown == null || dropdown.template == null)
            return;

        LocalizedTMPFont[] localizers = dropdown.template.GetComponentsInChildren<LocalizedTMPFont>(true);
        for (int i = 0; i < localizers.Length; i++)
            localizers[i]?.RefreshForLocale(LocalizationSettings.SelectedLocale);
    }
}
