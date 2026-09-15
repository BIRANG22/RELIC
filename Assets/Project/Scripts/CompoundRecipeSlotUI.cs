using System;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// CompoundRecipeSlot 한 개의 결과물과 재료 3개 표시를 담당합니다.
/// 발견되지 않은 대상은 Icon을 끄고 qus를 켭니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class CompoundRecipeSlotUI : MonoBehaviour
{
    [Header("레시피 번호")]
    [Tooltip("Background/Value에 표시할 레시피 번호입니다. 비워두면 자동으로 찾습니다.")]
    [SerializeField] private TMP_Text recipeNumberText;

    [Header("결과 연성제")]
    [SerializeField] private Image compoundIcon;
    [SerializeField] private GameObject compoundQuestion;

    [Header("재료 1")]
    [SerializeField] private Image material1Icon;
    [SerializeField] private GameObject material1Question;

    [Header("재료 2")]
    [SerializeField] private Image material2Icon;
    [SerializeField] private GameObject material2Question;

    [Header("재료 3")]
    [SerializeField] private Image material3Icon;
    [SerializeField] private GameObject material3Question;

    private LobbyCultureTankPanelPresenter cultureTankPresenter;

    public void Bind(DataManager dataManager, CompoundData compound, CompoundReferenceNameTooltip tooltip)
    {
        if (dataManager == null || compound == null)
            return;

        ResolveReferences();
        ApplyRecipeNumber(compound.CompoundId);

        bool compoundDiscovered = RecordDiscoveryService.IsCompoundDiscovered(dataManager, compound.CompoundId);
        Sprite compoundSprite = null;
        if (compoundDiscovered)
            TryGetCompoundIcon(dataManager, compound.CompoundId, out compoundSprite);

        ApplyEntry(
            compoundIcon,
            compoundQuestion,
            compoundDiscovered,
            compoundSprite,
            compoundDiscovered ? () => GameDataLocalization.CompoundName(compound) : null,
            tooltip);
        bool material1Discovered = ApplyMaterial(dataManager, compound.MaterialId1, material1Icon, material1Question, tooltip);
        bool material2Discovered = ApplyMaterial(dataManager, compound.MaterialId2, material2Icon, material2Question, tooltip);
        bool material3Discovered = ApplyMaterial(dataManager, compound.MaterialId3, material3Icon, material3Question, tooltip);

        Transform materials = transform.Find("Materials");
        ConfigureMaterialInteraction(materials?.Find("Material1"), compound.MaterialId1, material1Discovered);
        ConfigureMaterialInteraction(materials?.Find("Material2"), compound.MaterialId2, material2Discovered);
        ConfigureMaterialInteraction(materials?.Find("Material3"), compound.MaterialId3, material3Discovered);
    }

    private static bool ApplyMaterial(
        DataManager dataManager,
        string itemId,
        Image icon,
        GameObject question,
        CompoundReferenceNameTooltip tooltip)
    {
        bool discovered = !string.IsNullOrWhiteSpace(itemId)
            && RecordDiscoveryService.IsItemDiscovered(dataManager, itemId.Trim());

        Sprite sprite = null;
        if (discovered)
        {
            if (dataManager.ItemIconDatabase != null)
                dataManager.ItemIconDatabase.TryGetIcon(itemId.Trim(), out sprite);

        }

        string normalizedItemId = itemId.Trim();
        ApplyEntry(
            icon,
            question,
            discovered,
            sprite,
            discovered ? () => GetItemDisplayName(dataManager, normalizedItemId) : null,
            tooltip);

        return discovered;
    }

    private static void ApplyEntry(
        Image icon,
        GameObject question,
        bool discovered,
        Sprite sprite,
        Func<string> displayNameProvider,
        CompoundReferenceNameTooltip tooltip)
    {
        if (question != null)
            question.SetActive(!discovered);

        if (icon == null)
            return;

        icon.sprite = discovered ? sprite : null;
        icon.gameObject.SetActive(discovered);
        icon.raycastTarget = discovered;
        icon.preserveAspect = true;

        CompoundReferenceIconHover hover = icon.GetComponent<CompoundReferenceIconHover>();
        if (!discovered)
        {
            if (hover != null)
                hover.Clear();
            return;
        }

        if (hover == null)
            hover = icon.gameObject.AddComponent<CompoundReferenceIconHover>();

        hover.Initialize(tooltip, icon.rectTransform, displayNameProvider);
    }


    private void ConfigureMaterialInteraction(Transform materialRoot, string itemId, bool discovered)
    {
        if (materialRoot == null)
            return;

        Image back = materialRoot.Find("Back")?.GetComponent<Image>();
        if (back == null)
            back = materialRoot.Find("back")?.GetComponent<Image>();

        if (back != null)
            back.raycastTarget = true;

        CompoundRecipeMaterialInteraction interaction = materialRoot.GetComponent<CompoundRecipeMaterialInteraction>();
        if (interaction == null)
            interaction = materialRoot.gameObject.AddComponent<CompoundRecipeMaterialInteraction>();

        string normalizedItemId = string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim();
        interaction.Configure(
            back,
            discovered && !string.IsNullOrEmpty(normalizedItemId),
            () => TryRegisterRecipeMaterial(normalizedItemId));
    }

    private void TryRegisterRecipeMaterial(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return;

        if (cultureTankPresenter == null)
            cultureTankPresenter = FindFirstObjectByType<LobbyCultureTankPanelPresenter>(FindObjectsInactive.Include);

        cultureTankPresenter?.TryRegisterRecipeMaterial(itemId);
    }

    private static string GetItemDisplayName(DataManager dataManager, string itemId)
    {
        ItemData item = dataManager?.ItemDatabase?.Get(itemId);
        return item != null && !string.IsNullOrWhiteSpace(item.Name)
            ? GameDataLocalization.ItemName(item)
            : itemId;
    }

    private void ApplyRecipeNumber(string compoundId)
    {
        if (recipeNumberText == null)
            return;

        int recipeNumber = GetTrailingNumber(compoundId);
        recipeNumberText.text = recipeNumber == int.MaxValue ? string.Empty : recipeNumber.ToString();
    }

    private void ResolveReferences()
    {
        if (recipeNumberText == null)
        {
            Transform value = transform.Find("Background/Value");
            if (value != null)
                recipeNumberText = value.GetComponent<TMP_Text>();
        }

        Transform compound = transform.Find("Compound");
        Transform materials = transform.Find("Materials");

        if (compound != null)
        {
            compoundIcon ??= FindImage(compound, "Icon");
            compoundQuestion ??= FindObject(compound, "qus");
        }

        if (materials != null)
        {
            BindMaterialReferences(materials.Find("Material1"), ref material1Icon, ref material1Question);
            BindMaterialReferences(materials.Find("Material2"), ref material2Icon, ref material2Question);
            BindMaterialReferences(materials.Find("Material3"), ref material3Icon, ref material3Question);
        }
    }

    private static void BindMaterialReferences(Transform root, ref Image icon, ref GameObject question)
    {
        if (root == null)
            return;

        icon ??= FindImage(root, "Icon");
        question ??= FindObject(root, "qus");
    }

    private static Image FindImage(Transform root, string name)
    {
        Transform target = root.Find(name);
        return target != null ? target.GetComponent<Image>() : null;
    }

    private static GameObject FindObject(Transform root, string name)
    {
        Transform target = root.Find(name);
        return target != null ? target.gameObject : null;
    }

    private static int GetTrailingNumber(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return int.MaxValue;

        int index = id.Length - 1;
        while (index >= 0 && char.IsDigit(id[index]))
            index--;

        string number = id.Substring(index + 1);
        return int.TryParse(number, out int parsed) ? parsed : int.MaxValue;
    }

    private static bool TryGetCompoundIcon(DataManager dataManager, string compoundId, out Sprite icon)
    {
        icon = null;

        if (dataManager == null || dataManager.RelicIconDatabase == null || string.IsNullOrWhiteSpace(compoundId))
            return false;

        string id = compoundId.Trim();
        if (dataManager.RelicIconDatabase.TryGetIcon(id, out icon) && icon != null)
            return true;

        const string prefix = "Compound_";
        if (!id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        string suffix = id.Substring(prefix.Length);
        return dataManager.RelicIconDatabase.TryGetIcon($"Relic_A_{suffix}", out icon) && icon != null;
    }
}


[DisallowMultipleComponent]
internal sealed class CompoundRecipeMaterialInteraction : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private static readonly Color32 NormalColor = new Color32(0x22, 0x22, 0x22, 0xFF);
    private static readonly Color32 HoverColor = new Color32(0x33, 0x33, 0x33, 0xFF);
    private static readonly Color32 PressedColor = new Color32(0x11, 0x11, 0x11, 0xFF);

    private Image back;
    private bool interactionEnabled;
    private bool pointerInside;
    private bool pointerPressed;
    private bool clickCanceledByExit;
    private Action onClick;

    public void Configure(Image targetBack, bool enabled, Action clickAction)
    {
        back = targetBack;
        interactionEnabled = enabled;
        onClick = clickAction;
        pointerInside = false;
        pointerPressed = false;
        clickCanceledByExit = false;
        ApplyRgb(NormalColor);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!interactionEnabled)
            return;

        pointerInside = true;
        ApplyRgb(HoverColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;

        // 누른 채 슬롯 밖으로 나간 클릭은 취소합니다.
        // 이후 다시 슬롯 안으로 들어와 버튼을 놓아도 등록/제거가 실행되지 않습니다.
        if (pointerPressed)
            clickCanceledByExit = true;

        ApplyRgb(NormalColor);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!interactionEnabled || eventData.button != PointerEventData.InputButton.Left)
            return;

        pointerPressed = true;
        clickCanceledByExit = false;
        ApplyRgb(PressedColor);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!interactionEnabled || eventData.button != PointerEventData.InputButton.Left)
            return;

        bool shouldInvoke = pointerPressed && pointerInside && !clickCanceledByExit;
        pointerPressed = false;
        clickCanceledByExit = false;

        ApplyRgb(pointerInside ? HoverColor : NormalColor);
        if (shouldInvoke)
            onClick?.Invoke();
    }

    private void OnDisable()
    {
        pointerInside = false;
        pointerPressed = false;
        clickCanceledByExit = false;
        ApplyRgb(NormalColor);
    }

    private void ApplyRgb(Color32 rgb)
    {
        if (back == null)
            return;

        float alpha = back.color.a;
        back.color = new Color(rgb.r / 255f, rgb.g / 255f, rgb.b / 255f, alpha);
    }
}
