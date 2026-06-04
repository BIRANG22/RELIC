using Relic.Gameplay.Battle;
using UnityEngine;

public class BattleGridClickHandler : MonoBehaviour
{
    [SerializeField] private int gridIndex;
    [SerializeField] private PlayerActionPlanner actionPlanner;

    private bool isSkillTargetMode;

#if UNITY_EDITOR
    private void OnValidate()
    {
        string numberPart = gameObject.name.Replace("Grid_", "");

        if (int.TryParse(numberPart, out int index))
            gridIndex = index;
    }
#endif

    private void OnMouseDown()
    {
        if (!isSkillTargetMode)
            return;

        OnClickGrid();
    }

    public void SetSkillTargetMode(bool value)
    {
        isSkillTargetMode = value;
    }

    public void OnClickGrid()
    {
        if (!isSkillTargetMode)
            return;

        if (actionPlanner != null)
            actionPlanner.SelectTargetGrid(gridIndex);
    }
}