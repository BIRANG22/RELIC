using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Relic.Gameplay.Data;

public class RelicChoiceSlotUI : MonoBehaviour
{
    [SerializeField] private Image relicIconImage;
    [SerializeField] private TMP_Text relicNameText;
    [SerializeField] private TMP_Text relicDescText;

    private string relicId;
    private RelicChoiceAreaUI owner;

    public void Setup(string id, RelicChoiceAreaUI choiceArea)
    {
        relicId = id;
        owner = choiceArea;

        RelicData relicData = DataManager.Instance.RelicDatabase.Get(relicId);

        if (relicData == null)
            return;

        if (relicNameText != null)
            relicNameText.text = relicData.Name;

        if (relicDescText != null)
            relicDescText.text = relicData.EffectDesc;

        if (relicIconImage != null &&
            DataManager.Instance.RelicIconDatabase.TryGetIcon(relicId, out Sprite icon))
        {
            relicIconImage.sprite = icon;
            relicIconImage.enabled = true;
        }
        else if (relicIconImage != null)
        {
            relicIconImage.sprite = null;
            relicIconImage.enabled = false;
        }
    }

    public void OnClick()
    {
        if (string.IsNullOrWhiteSpace(relicId))
            return;

        if (DataManager.Instance == null)
        {
            Debug.LogWarning("[RelicChoiceSlotUI] DataManager is null.");
            return;
        }

        BattleRuntimeData runtime =
            DataManager.Instance.BattleRuntimeStore.GetOrCreate();

        runtime.OwnedRelicIds ??= new System.Collections.Generic.List<string>();

        if (!runtime.OwnedRelicIds.Contains(relicId))
            runtime.OwnedRelicIds.Add(relicId);

        DataManager.Instance.BattleRuntimeStore.Set(runtime);

        RelicEquipPanelUI relicEquipPanel =
            Object.FindFirstObjectByType<RelicEquipPanelUI>(FindObjectsInactive.Include);

        if (relicEquipPanel != null)
            relicEquipPanel.Refresh();

        if (owner != null)
            owner.OnRelicSelected(relicId);
    }
}