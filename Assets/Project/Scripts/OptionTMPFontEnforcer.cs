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
            RefreshUnlessMultilingualDropdown(localizers[i]);

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
            RefreshUnlessMultilingualDropdown(localizers[i]);
    }
    private static void RefreshUnlessMultilingualDropdown(LocalizedTMPFont localizer)
    {
        if (localizer == null)
            return;

        TMP_Text target = localizer.Target != null
            ? localizer.Target : localizer.GetComponent<TMP_Text>();
        if (target != null)
        {
            DirectTemplateDropdown owner = target.GetComponentInParent<DirectTemplateDropdown>(true);
            if (owner != null && owner.OwnsFont(target))
                return;
        }

        localizer.RefreshForLocale(LocalizationSettings.SelectedLocale);
    }

}
