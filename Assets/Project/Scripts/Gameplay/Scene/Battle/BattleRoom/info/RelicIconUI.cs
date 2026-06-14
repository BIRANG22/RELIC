using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class RelicIconUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image iconImage;

    private string relicId;
    private RelicEquipPanelUI owner;

    public string RelicId => relicId;

    public void Setup(string relicId)
    {
        this.relicId = relicId;

        RefreshIcon();
    }

    public void Setup(string relicId, RelicEquipPanelUI owner)
    {
        this.relicId = relicId;
        this.owner = owner;

        RefreshIcon();
    }

    private void RefreshIcon()
    {
        if (iconImage == null)
            return;

        if (!string.IsNullOrWhiteSpace(relicId) &&
            DataManager.Instance.RelicIconDatabase != null &&
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

    public void Clear()
    {
        relicId = null;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[RelicIconUI] Click / Relic:{relicId} / Owner:{owner}");

        if (string.IsNullOrWhiteSpace(relicId))
            return;

        if (owner == null)
        {
            Debug.LogWarning($"[RelicIconUI] owner ¾øÀ½ / Relic:{relicId}");
            return;
        }

        owner.SelectRelic(relicId);
    }
}