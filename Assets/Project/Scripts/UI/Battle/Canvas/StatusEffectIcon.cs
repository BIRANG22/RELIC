using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusEffectIcon : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text stackText;
    [SerializeField] private TMP_Text turnText;

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

        if (stackText != null)
            stackText.text = data.Stack > 1 ? data.Stack.ToString() : "";

        if (turnText != null)
            turnText.text = data.RemainingTurn > 0 ? data.RemainingTurn.ToString() : "";
    }

    private Sprite GetIcon(string effectId)
    {
        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.StatusEffectIconDatabase == null)
            return null;

        if (DataManager.Instance.StatusEffectIconDatabase.TryGetIcon(effectId, out var icon))
            return icon;

        return null;
    }
}