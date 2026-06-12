using System.Collections.Generic;
using UnityEngine;
using Relic.Gameplay.Data;

public class RelicChoiceAreaUI : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private RelicChoiceSlotUI relicChoiceSlotPrefab;

    [Header("Root")]
    [SerializeField] private Transform slotRoot;

    [Header("Choice Setting")]
    [SerializeField] private int choiceCount = 3;

    [Header("Relic Id Range")]
    [SerializeField] private int minRelicNumber = 1;
    [SerializeField] private int maxRelicNumber = 20;
    [SerializeField] private string relicIdPrefix = "Relic_";

    [Header("Complete")]
    [SerializeField] private BattleMapController battleMapController;
    [SerializeField] private StartRoomController startRoomController;
    public void Open()
    {
        gameObject.SetActive(true);
        SpawnChoices();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        Clear();
    }

    private void SpawnChoices()
    {
        Clear();

        List<string> relicIds = PickRandomRelicIds();

        for (int i = 0; i < relicIds.Count; i++)
        {
            RelicChoiceSlotUI slot = Instantiate(relicChoiceSlotPrefab, slotRoot);
            slot.Setup(relicIds[i], this);
        }
    }

    private List<string> PickRandomRelicIds()
    {
        List<string> candidates = new();

        for (int i = minRelicNumber; i <= maxRelicNumber; i++)
        {
            string id = relicIdPrefix + i.ToString("00");

            if (DataManager.Instance.RelicDatabase.TryGet(id, out _))
                candidates.Add(id);
        }

        Shuffle(candidates);

        int count = Mathf.Min(choiceCount, candidates.Count);

        return candidates.GetRange(0, count);
    }

    public void OnRelicSelected(string relicId)
    {
        Close();

        if (startRoomController != null)
            startRoomController.OnRelicChoiceFinished();

        if (battleMapController != null)
            battleMapController.OpenMap();
        else
            Debug.LogWarning("[RelicChoiceAreaUI] BattleMapController가 연결되지 않았습니다.");
    }

    private void Clear()
    {
        Transform root = slotRoot != null ? slotRoot : transform;

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }

    private void Shuffle(List<string> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);

            string temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}