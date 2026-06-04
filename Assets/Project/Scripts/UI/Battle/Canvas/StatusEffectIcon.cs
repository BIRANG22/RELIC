using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusEffectIcon : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text valueText;

    public void Set(StatusEffectRuntimeData data)
    {
        if (data == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (iconImage != null)
        {
            iconImage.sprite = GetIcon(data.EffectId);
            iconImage.enabled = iconImage.sprite != null;
        }

        if (valueText != null)
        {
            valueText.text = data.Stack > 0
                ? data.Stack.ToString()
                : "";
        }
    }

    private Sprite GetIcon(string effectId)
    {
        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.StatusEffectIconDatabase == null)
            return null;

        if (DataManager.Instance.StatusEffectIconDatabase.TryGetIcon(effectId, out Sprite icon))
            return icon;

        return null;
    }
}