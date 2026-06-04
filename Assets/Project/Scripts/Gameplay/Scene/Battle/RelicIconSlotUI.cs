using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class RelicIconSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;

    public void Setup(string relicId)
    {
        if (iconImage == null)
            return;

        if (DataManager.Instance.RelicIconDatabase != null &&
            DataManager.Instance.RelicIconDatabase.TryGetIcon(relicId, out Sprite icon))
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;
        }
        else
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
    }
}