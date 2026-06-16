using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitStatusEffectTooltipItemUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    public void BindFallbackReferences(Image icon, TMP_Text title, TMP_Text description)
    {
        iconImage = icon;
        titleText = title;
        descriptionText = description;
    }

    public void BindFallbackReferences(Image background, Image icon, TMP_Text title, TMP_Text description)
    {
        backgroundImage = background;
        iconImage = icon;
        titleText = title;
        descriptionText = description;
    }

    private void Awake()
    {
        DisableRaycastTargets();
    }

    public void Set(StatusEffectRuntimeData data)
    {
        if (data == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        DisableRaycastTargets();

        EffectMasterData masterData = GetEffectMasterData(data.EffectId);

        if (iconImage != null)
        {
            iconImage.sprite = GetIcon(data.EffectId);
            iconImage.enabled = iconImage.sprite != null;
            iconImage.raycastTarget = false;
        }

        if (titleText != null)
        {
            if (masterData != null && !string.IsNullOrWhiteSpace(masterData.Name))
                titleText.text = masterData.Name;
            else
                titleText.text = data.EffectId;
        }

        if (descriptionText != null)
        {
            if (masterData != null && !string.IsNullOrWhiteSpace(masterData.ToolTip))
                descriptionText.text = masterData.ToolTip;
            else
                descriptionText.text = string.Empty;
        }
    }

    private void DisableRaycastTargets()
    {
        if (backgroundImage != null)
            backgroundImage.raycastTarget = false;

        if (iconImage != null)
            iconImage.raycastTarget = false;

        if (titleText != null)
            titleText.raycastTarget = false;

        if (descriptionText != null)
            descriptionText.raycastTarget = false;
    }

    private EffectMasterData GetEffectMasterData(string effectId)
    {
        if (DataManager.Instance == null || DataManager.Instance.EffectDatabase == null)
            return null;

        DataManager.Instance.EffectDatabase.TryGet(effectId, out EffectMasterData masterData);
        return masterData;
    }

    private Sprite GetIcon(string effectId)
    {
        if (DataManager.Instance == null || DataManager.Instance.StatusEffectIconDatabase == null)
            return null;

        if (DataManager.Instance.StatusEffectIconDatabase.TryGetIcon(effectId, out Sprite icon))
            return icon;

        return null;
    }
}
