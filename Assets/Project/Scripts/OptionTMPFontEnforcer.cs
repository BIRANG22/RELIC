using System.Collections;
using TMPro;
using UnityEngine;

public sealed class OptionTMPFontEnforcer : MonoBehaviour
{
    [Header("기본 옵션 폰트")]
    [Tooltip("Sound, Language, Master, BGM, SFX 같은 일반 옵션 텍스트에 적용할 TMP 폰트입니다.")]
    [SerializeField] private TMP_FontAsset defaultFontAsset;

    [Header("언어 드롭다운 폰트")]
    [Tooltip("Language Dropdown의 Label, Item Label, 런타임 Dropdown List에 적용할 TMP 폰트입니다.")]
    [SerializeField] private TMP_FontAsset dropdownFontAsset;

    [Header("적용 타이밍")]
    [SerializeField] private bool applyOnAwake = true;
    [SerializeField] private bool applyOnEnable = true;
    [SerializeField] private bool applyAfterOneFrame = true;
    [SerializeField] private bool applyForDropdownRuntimeList = true;

    private Coroutine applyRoutine;

    private void Awake()
    {
        if (applyOnAwake)
            ApplyFonts();
    }

    private void OnEnable()
    {
        if (applyOnEnable)
            ApplyFonts();

        if (applyAfterOneFrame)
            RestartApplyRoutine();
    }

    private void OnDisable()
    {
        if (applyRoutine != null)
        {
            StopCoroutine(applyRoutine);
            applyRoutine = null;
        }
    }

    private void LateUpdate()
    {
        if (!applyForDropdownRuntimeList)
            return;

        ApplyDropdownRuntimeListFont();
    }

    public void ApplyFonts()
    {
        ApplyDefaultTextFonts();
        ApplyDropdownFonts();
    }

    private void ApplyDefaultTextFonts()
    {
        if (defaultFontAsset == null)
            return;

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
            ApplyTextFont(texts[i], defaultFontAsset);
    }

    private void ApplyDropdownFonts()
    {
        if (dropdownFontAsset == null)
            return;

        TMP_Dropdown[] dropdowns = GetComponentsInChildren<TMP_Dropdown>(true);
        for (int i = 0; i < dropdowns.Length; i++)
            ApplyDropdownFont(dropdowns[i]);
    }

    private void RestartApplyRoutine()
    {
        if (applyRoutine != null)
            StopCoroutine(applyRoutine);

        applyRoutine = StartCoroutine(ApplyFontsDelayed());
    }

    private IEnumerator ApplyFontsDelayed()
    {
        yield return null;
        ApplyFonts();
        yield return null;
        ApplyDropdownRuntimeListFont();
        applyRoutine = null;
    }

    private void ApplyDropdownFont(TMP_Dropdown dropdown)
    {
        if (dropdown == null || dropdownFontAsset == null)
            return;

        ApplyTextFont(dropdown.captionText, dropdownFontAsset);
        ApplyTextFont(dropdown.itemText, dropdownFontAsset);

        if (dropdown.template != null)
        {
            TMP_Text[] templateTexts = dropdown.template.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < templateTexts.Length; i++)
                ApplyTextFont(templateTexts[i], dropdownFontAsset);
        }
    }

    private void ApplyDropdownRuntimeListFont()
    {
        if (dropdownFontAsset == null)
            return;

        TMP_Dropdown[] dropdowns = GetComponentsInChildren<TMP_Dropdown>(true);
        for (int i = 0; i < dropdowns.Length; i++)
        {
            TMP_Dropdown dropdown = dropdowns[i];
            if (dropdown == null)
                continue;

            Transform dropdownList = dropdown.transform.Find("Dropdown List");
            if (dropdownList == null)
                continue;

            TMP_Text[] texts = dropdownList.GetComponentsInChildren<TMP_Text>(true);
            for (int t = 0; t < texts.Length; t++)
                ApplyTextFont(texts[t], dropdownFontAsset);
        }
    }

    private void ApplyTextFont(TMP_Text text, TMP_FontAsset fontAsset)
    {
        if (text == null || fontAsset == null)
            return;

        if (text.font == fontAsset)
            return;

        text.font = fontAsset;
        text.SetAllDirty();
    }
}
