using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Relic.Gameplay.Data;

public class RelicChoiceAreaUI : MonoBehaviour
{
    [Header("Slots In Scene")]
    [SerializeField] private RelicChoiceSlotUI[] choiceSlots;

    [Header("Hover Info Panel")]
    [SerializeField] private GameObject relicHoverInfoPanel;
    [SerializeField] private TMP_Text relicHoverNameText;
    [SerializeField] private TMP_Text relicHoverDescText;

    [Header("Choice Setting")]
    [SerializeField, Min(1)] private int choiceCount = 3;
    [SerializeField] private bool useRelicNumberRange = true;

    [Header("Relic Id Range")]
    [SerializeField] private int minRelicNumber = 1;
    [SerializeField] private int maxRelicNumber = 20;
    [SerializeField] private string relicIdPrefix = "Relic_";

    [Header("Complete")]
    [SerializeField] private BattleMapController battleMapController;
    [SerializeField] private StartRoomController startRoomController;

    private bool isOpen;
    private bool isSelectionCompleted;

    private void Awake()
    {
        if (startRoomController == null)
            startRoomController = GetComponentInParent<StartRoomController>(true);

        if (battleMapController == null)
            battleMapController = Object.FindFirstObjectByType<BattleMapController>(FindObjectsInactive.Include);

        HideRelicHoverInfo();
    }

    private void OnDisable()
    {
        HideRelicHoverInfo();
    }

    public void Open()
    {
        isOpen = true;
        isSelectionCompleted = false;

        gameObject.SetActive(true);
        HideRelicHoverInfo();
        SetupChoices();
    }

    public void Close()
    {
        isOpen = false;
        HideRelicHoverInfo();
        ClearSlots();
        gameObject.SetActive(false);
    }

    private void SetupChoices()
    {
        ClearSlots();

        List<RelicChoiceSlotUI> validSlots = GetValidSlots();
        if (validSlots.Count == 0)
        {
            Debug.LogWarning("[RelicChoiceAreaUI] Choice Slots are empty. Put RelicChoiceSlot_1, RelicChoiceSlot_2, RelicChoiceSlot_3 into Choice Slots in the Inspector.");
            return;
        }

        List<string> relicIds = PickRandomRelicIds();
        if (relicIds.Count == 0)
        {
            Debug.LogWarning("[RelicChoiceAreaUI] No selectable relic ids were found.");
            return;
        }

        int count = Mathf.Min(choiceCount, relicIds.Count, validSlots.Count);
        for (int i = 0; i < validSlots.Count; i++)
        {
            RelicChoiceSlotUI slot = validSlots[i];
            if (i < count)
            {
                slot.gameObject.SetActive(true);
                slot.Setup(relicIds[i], this);
            }
            else
            {
                slot.ClearSlot();
                slot.gameObject.SetActive(false);
            }
        }

        if (relicHoverInfoPanel != null)
            relicHoverInfoPanel.transform.SetAsLastSibling();
    }

    private List<RelicChoiceSlotUI> GetValidSlots()
    {
        List<RelicChoiceSlotUI> validSlots = new();

        if (choiceSlots == null)
            return validSlots;

        for (int i = 0; i < choiceSlots.Length; i++)
        {
            if (choiceSlots[i] != null && !validSlots.Contains(choiceSlots[i]))
                validSlots.Add(choiceSlots[i]);
        }

        return validSlots;
    }

    private List<string> PickRandomRelicIds()
    {
        List<string> candidates = new();

        if (DataManager.Instance == null || DataManager.Instance.RelicDatabase == null)
        {
            Debug.LogWarning("[RelicChoiceAreaUI] DataManager or RelicDatabase is null.");
            return candidates;
        }

        if (useRelicNumberRange)
            AddRelicsFromNumberRange(candidates);
        else
            AddAllRelics(candidates);

        RemoveAlreadyOwnedRelics(candidates);
        Shuffle(candidates);

        int count = Mathf.Min(choiceCount, candidates.Count);
        if (count <= 0)
            return new List<string>();

        return candidates.GetRange(0, count);
    }

    private void AddRelicsFromNumberRange(List<string> candidates)
    {
        int start = Mathf.Min(minRelicNumber, maxRelicNumber);
        int end = Mathf.Max(minRelicNumber, maxRelicNumber);
        string prefix = string.IsNullOrWhiteSpace(relicIdPrefix) ? "Relic_" : relicIdPrefix;

        for (int i = start; i <= end; i++)
        {
            string id = prefix + i.ToString("00");
            if (DataManager.Instance.RelicDatabase.TryGet(id, out _))
                candidates.Add(id);
        }
    }

    private void AddAllRelics(List<string> candidates)
    {
        IReadOnlyList<RelicData> allRelics = DataManager.Instance.RelicDatabase.GetAll();
        if (allRelics == null)
            return;

        for (int i = 0; i < allRelics.Count; i++)
        {
            RelicData relicData = allRelics[i];
            if (relicData == null || string.IsNullOrWhiteSpace(relicData.FragmentId))
                continue;

            candidates.Add(relicData.FragmentId.Trim());
        }
    }

    private void RemoveAlreadyOwnedRelics(List<string> candidates)
    {
        if (DataManager.Instance == null || DataManager.Instance.BattleRuntimeStore == null)
            return;

        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore.GetOrCreate();
        if (runtime.OwnedRelicIds == null || runtime.OwnedRelicIds.Count == 0)
            return;

        candidates.RemoveAll(id => runtime.OwnedRelicIds.Contains(id));
    }

    public void ShowRelicHoverInfo(string relicId)
    {
        if (!isOpen || string.IsNullOrWhiteSpace(relicId))
            return;

        if (DataManager.Instance == null || DataManager.Instance.RelicDatabase == null)
        {
            Debug.LogWarning("[RelicChoiceAreaUI] DataManager or RelicDatabase is null.");
            return;
        }

        if (!DataManager.Instance.RelicDatabase.TryGet(relicId, out RelicData relicData) || relicData == null)
            return;

        if (relicHoverNameText != null)
            relicHoverNameText.text = relicData.Name;

        if (relicHoverDescText != null)
            relicHoverDescText.text = relicData.EffectDesc;

        if (relicHoverInfoPanel != null)
        {
            relicHoverInfoPanel.transform.SetAsLastSibling();
            relicHoverInfoPanel.SetActive(true);
        }
    }

    public void HideRelicHoverInfo()
    {
        if (relicHoverInfoPanel != null)
            relicHoverInfoPanel.SetActive(false);
    }

    public void SelectRelic(string relicId)
    {
        if (!isOpen || isSelectionCompleted)
            return;

        if (string.IsNullOrWhiteSpace(relicId))
            return;

        if (DataManager.Instance == null || DataManager.Instance.BattleRuntimeStore == null || DataManager.Instance.RelicDatabase == null)
        {
            Debug.LogWarning("[RelicChoiceAreaUI] DataManager, BattleRuntimeStore, or RelicDatabase is null.");
            return;
        }

        if (!DataManager.Instance.RelicDatabase.TryGet(relicId, out _))
        {
            Debug.LogWarning($"[RelicChoiceAreaUI] Unknown relic id: {relicId}");
            return;
        }

        isSelectionCompleted = true;
        GrantRelic(relicId);
        RefreshRelicEquipPanel();
        CompleteChoiceEvent();
    }

    private void GrantRelic(string relicId)
    {
        BattleRuntimeData runtime = DataManager.Instance.BattleRuntimeStore.GetOrCreate();
        runtime.OwnedRelicIds ??= new List<string>();

        if (!runtime.OwnedRelicIds.Contains(relicId))
            runtime.OwnedRelicIds.Add(relicId);

        DataManager.Instance.BattleRuntimeStore.Set(runtime);
    }

    private void RefreshRelicEquipPanel()
    {
        RelicEquipPanelUI relicEquipPanel = Object.FindFirstObjectByType<RelicEquipPanelUI>(FindObjectsInactive.Include);
        if (relicEquipPanel != null)
            relicEquipPanel.Refresh();
    }

    private void CompleteChoiceEvent()
    {
        Close();

        if (startRoomController != null)
            startRoomController.OnRelicChoiceFinished();

        if (battleMapController == null)
            battleMapController = Object.FindFirstObjectByType<BattleMapController>(FindObjectsInactive.Include);

        if (battleMapController != null)
            battleMapController.OpenMap();
        else
            Debug.LogWarning("[RelicChoiceAreaUI] BattleMapController is not connected.");
    }

    private void ClearSlots()
    {
        List<RelicChoiceSlotUI> validSlots = GetValidSlots();
        for (int i = 0; i < validSlots.Count; i++)
            validSlots[i].ClearSlot();
    }

    private void Shuffle(List<string> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }
}
