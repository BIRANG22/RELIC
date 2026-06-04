using Relic.Gameplay.Data;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerHUDSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("Basic")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;

    [Header("HP")]
    [SerializeField] private Image hpFill;
    [SerializeField] private TMP_Text hpValueText;

    [Header("Stamina")]
    [SerializeField] private Image staminaFill;
    [SerializeField] private TMP_Text staminaValueText;

    [Header("Unique Resource")]
    [SerializeField] private GameObject[] resourceSlots;
    [SerializeField] private Image[] resourceFillImages;

    [Header("Shield")]
    [SerializeField] private Image shieldFill;
    [SerializeField] private TMP_Text shieldValueText;

    [Header("Status Effects")]
    [SerializeField] private Transform statusIconRoot;
    [SerializeField] private StatusEffectIcon statusIconPrefab;

    private CharacterRuntimeData boundRuntime;
    private CharacterMasterData boundMaster;

    public event Action<CharacterRuntimeData> OnClicked;

    public void Bind(CharacterRuntimeData runtimeData)
    {
        boundRuntime = runtimeData;
        boundMaster = null;

        if (boundRuntime == null)
        {
            Clear();
            return;
        }

        if (DataManager.Instance != null)
            DataManager.Instance.CharacterDatabase.TryGet(boundRuntime.CharacterId, out boundMaster);

        Refresh();
    }

    public void Refresh()
    {
        if (boundRuntime == null)
        {
            Clear();
            return;
        }

        if (portraitImage != null)
        {
            portraitImage.sprite = GetCharacterIcon(boundRuntime.CharacterId);
            portraitImage.enabled = portraitImage.sprite != null;
        }

        if (nameText != null)
            nameText.text = boundMaster != null ? boundMaster.Name : boundRuntime.CharacterId;

        int maxHp = boundMaster != null ? boundMaster.MaxHealth : Mathf.Max(1, boundRuntime.CurrentHealth);
        int maxStamina = boundMaster != null ? boundMaster.MaxStamina : Mathf.Max(1, boundRuntime.CurrentStamina);
        int maxResource = boundMaster != null ? boundMaster.MaxResource : Mathf.Max(1, boundRuntime.CurrentResource);

        RefreshBar(hpFill, hpValueText, boundRuntime.CurrentHealth, maxHp);
        RefreshBar(staminaFill, staminaValueText, boundRuntime.CurrentStamina, maxStamina);
        RefreshUniqueResource(boundRuntime.CurrentResource, maxResource);
        RefreshShield(boundRuntime.CurrentShield, maxHp);
        RefreshStatusEffects(boundRuntime.StatusEffects);
    }

    private void RefreshBar(Image fill, TMP_Text valueText, int current, int max)
    {
        current = Mathf.Clamp(current, 0, max);

        if (fill != null)
            fill.fillAmount = max > 0 ? (float)current / max : 0f;

        if (valueText != null)
            valueText.text = $"{current} / {max}";
    }

    private void RefreshUniqueResource(int currentResource, int maxResource)
    {
        currentResource = Mathf.Clamp(currentResource, 0, maxResource);

        int slotCount = resourceSlots != null ? resourceSlots.Length : 0;

        for (int i = 0; i < slotCount; i++)
        {
            bool useSlot = i < maxResource;

            if (resourceSlots[i] != null)
                resourceSlots[i].SetActive(useSlot);

            if (resourceFillImages == null || i >= resourceFillImages.Length)
                continue;

            Image fillImage = resourceFillImages[i];

            if (fillImage == null)
                continue;

            bool filled = useSlot && i < currentResource;

            fillImage.gameObject.SetActive(filled);
        }
    }

    private void RefreshShield(int shield, int maxHp)
    {
        shield = Mathf.Max(0, shield);

        if (shieldFill != null)
        {
            shieldFill.gameObject.SetActive(shield > 0);
            shieldFill.fillAmount = maxHp > 0 ? (float)shield / maxHp : 0f;
        }

        if (shieldValueText != null)
        {
            shieldValueText.gameObject.SetActive(shield > 0);
            shieldValueText.text = shield.ToString();
        }
    }

    private void RefreshStatusEffects(List<StatusEffectRuntimeData> statusEffects)
    {
        if (statusIconRoot == null || statusIconPrefab == null)
            return;

        for (int i = statusIconRoot.childCount - 1; i >= 0; i--)
            Destroy(statusIconRoot.GetChild(i).gameObject);

        if (statusEffects == null)
            return;

        for (int i = 0; i < statusEffects.Count; i++)
        {
            if (statusEffects[i] == null)
                continue;

            StatusEffectIcon icon = Instantiate(statusIconPrefab, statusIconRoot);
            icon.Set(statusEffects[i]);
        }
    }

    private Sprite GetCharacterIcon(string characterId)
    {
        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.CharacterIconDatabase == null)
            return null;

        if (DataManager.Instance.CharacterIconDatabase.TryGetIcon(characterId, out var icon))
            return icon;

        return null;
    }

    private void Clear()
    {
        if (portraitImage != null)
        {
            portraitImage.sprite = null;
            portraitImage.enabled = false;
        }

        if (nameText != null)
            nameText.text = "";

        RefreshBar(hpFill, hpValueText, 0, 1);
        RefreshBar(staminaFill, staminaValueText, 0, 1);
        RefreshUniqueResource(0, 0);
        RefreshShield(0, 1);
        RefreshStatusEffects(null);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (boundRuntime != null)
            OnClicked?.Invoke(boundRuntime);
    }
}