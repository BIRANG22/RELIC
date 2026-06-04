using UnityEngine;
using Relic.Gameplay.Data;

public class RelicInventoryPanelUI : MonoBehaviour
{
    [SerializeField] private RelicIconSlotUI relicIconSlotPrefab;
    [SerializeField] private Transform relicIconRoot;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        Clear();

        BattleRuntimeData runtime =
            DataManager.Instance.BattleRuntimeStore.GetOrCreate();

        if (runtime == null || runtime.OwnedRelicIds == null)
            return;

        for (int i = 0; i < runtime.OwnedRelicIds.Count; i++)
        {
            string relicId = runtime.OwnedRelicIds[i];

            RelicIconSlotUI slot =
                Instantiate(relicIconSlotPrefab, relicIconRoot);

            slot.Setup(relicId);
        }
    }

    private void Clear()
    {
        if (relicIconRoot == null)
            return;

        for (int i = relicIconRoot.childCount - 1; i >= 0; i--)
            Destroy(relicIconRoot.GetChild(i).gameObject);
    }
}