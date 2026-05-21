using Relic.Gameplay.Battle;
using UnityEngine;

public class BattleGridClickHandler : MonoBehaviour
{
    [SerializeField] private int gridIndex;
    [SerializeField] private BattlePlacementController placementController;
    [SerializeField] private PlayerActionPlanner actionPlanner;

    private bool isPlacementMode;
    private bool isSkillTargetMode;

#if UNITY_EDITOR
    private void OnValidate()
    {
        string objectName = gameObject.name;

        string numberPart = objectName.Replace("Grid_", "");

        if (int.TryParse(numberPart, out int index))
        {
            gridIndex = index;
        }
    }
#endif

    private void OnMouseDown()
    {
        OnClickGrid();
    }

    public void SetPlacementMode(bool value)
    {
        isPlacementMode = value;

        if (value)
            isSkillTargetMode = false;
    }

    public void SetSkillTargetMode(bool value)
    {
        isSkillTargetMode = value;

        if (value)
            isPlacementMode = false;
    }

    public void OnClickGrid()
    {
        if (isPlacementMode)
        {
            placementController.SelectGrid(gridIndex);
            return;
        }

        if (isSkillTargetMode)
        {
            actionPlanner.SelectTargetGrid(gridIndex);
            return;
        }

        Debug.Log($"[BattleGridClickHandler] 클릭 모드 없음 Grid:{gridIndex}");
    }
}