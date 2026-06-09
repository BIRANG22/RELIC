using UnityEngine;

public class MoveGhostPreview : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private SpriteRenderer ghostPrefab;

    private SpriteRenderer currentGhost;

    public void Show(Sprite sprite, int gridIndex, BattleDirection direction)
    {
        Clear();

        if (gridManager == null || ghostPrefab == null || sprite == null)
            return;

        Vector3 position = gridManager.GetWorldPositionByIndex(gridIndex);

        currentGhost = Instantiate(ghostPrefab, position, Quaternion.identity);
        currentGhost.sprite = sprite;

        currentGhost.flipX = direction == BattleDirection.Left;

        Color color = currentGhost.color;
        color.a = 0.4f;
        currentGhost.color = color;
    }

    public void Clear()
    {
        if (currentGhost != null)
            Destroy(currentGhost.gameObject);

        currentGhost = null;
    }
}