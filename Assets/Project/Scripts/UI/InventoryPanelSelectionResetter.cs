using UnityEngine;

public class InventoryPanelSelectionResetter : MonoBehaviour
{
    [SerializeField] private EquippedSkillPanelUI equippedSkillPanel;
    [SerializeField] private RelicEquipPanelUI relicEquipPanel;
    [SerializeField] private SkillListPanel[] skillListPanels;

    private void Awake()
    {
        ResolveTargets();
    }

    private void OnEnable()
    {
        ResetSelectionState();
    }

    private void OnDisable()
    {
        ResetSelectionState();
    }

    public void ResetSelectionState()
    {
        ResetSelectionStateExcept(null);
    }

    public void ResetSelectionStateExcept(Object exceptOwner)
    {
        ResolveTargets();

        if (equippedSkillPanel != null && equippedSkillPanel != exceptOwner)
            equippedSkillPanel.ResetSelectionState();

        if (relicEquipPanel != null && relicEquipPanel != exceptOwner)
            relicEquipPanel.ResetSelectionState();

        if (skillListPanels != null)
        {
            for (int i = 0; i < skillListPanels.Length; i++)
            {
                if (skillListPanels[i] != null && skillListPanels[i] != exceptOwner)
                    skillListPanels[i].ResetSelectionState();
            }
        }
    }

    public static void ResetAllSelectionsExcept(Object exceptOwner)
    {
        InventoryPanelSelectionResetter[] resetters = FindObjectsByType<InventoryPanelSelectionResetter>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        if (resetters == null || resetters.Length == 0)
            return;

        for (int i = 0; i < resetters.Length; i++)
        {
            if (resetters[i] != null)
                resetters[i].ResetSelectionStateExcept(exceptOwner);
        }
    }

    private void ResolveTargets()
    {
        if (equippedSkillPanel == null)
            equippedSkillPanel = GetComponentInChildren<EquippedSkillPanelUI>(true);

        if (relicEquipPanel == null)
            relicEquipPanel = GetComponentInChildren<RelicEquipPanelUI>(true);

        if (skillListPanels == null || skillListPanels.Length == 0)
            skillListPanels = GetComponentsInChildren<SkillListPanel>(true);
    }
}
