using UnityEngine;

public class BattlePlacementGridCell : MonoBehaviour
{
    [SerializeField] private BattlePlacementController placementController;

    private int gridIndex = -1;

    private void Awake()
    {
        gridIndex = ParseGridIndexFromName();
    }

    private void OnMouseDown()
    {
        if (placementController == null)
        {
            Debug.LogWarning($"[BattlePlacementGridCell] PlacementController missing. Grid: {name}");
            return;
        }

        if (gridIndex < 0)
        {
            Debug.LogWarning($"[BattlePlacementGridCell] Invalid grid name: {name}");
            return;
        }

        placementController.SelectGrid(gridIndex);
    }

    private int ParseGridIndexFromName()
    {
        string prefix = "Grid_";

        if (!name.StartsWith(prefix))
            return -1;

        string numberText = name.Substring(prefix.Length);

        if (!int.TryParse(numberText, out int result))
            return -1;

        return result;
    }
}