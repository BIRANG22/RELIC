using System.Collections.Generic;
using UnityEngine;

public class MoveGhostPreview : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private SpriteRenderer ghostPrefab;

    private readonly Dictionary<string, SpriteRenderer> ghostsByCharacterId = new();

    public void Show(string characterId, Sprite sprite, int gridIndex, BattleDirection direction)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return;

        if (gridManager == null || ghostPrefab == null || sprite == null)
            return;

        Clear(characterId);

        Vector3 position = gridManager.GetWorldPositionByIndex(gridIndex);

        SpriteRenderer ghost = Instantiate(ghostPrefab, position, Quaternion.identity);
        ghost.sprite = sprite;
        ghost.flipX = direction == BattleDirection.Left;

        Color color = ghost.color;
        color.a = 0.4f;
        ghost.color = color;

        ghostsByCharacterId[characterId] = ghost;
    }

    public void Clear(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return;

        if (!ghostsByCharacterId.TryGetValue(characterId, out SpriteRenderer ghost))
            return;

        if (ghost != null)
            Destroy(ghost.gameObject);

        ghostsByCharacterId.Remove(characterId);
    }

    public void ClearAll()
    {
        foreach (var pair in ghostsByCharacterId)
        {
            if (pair.Value != null)
                Destroy(pair.Value.gameObject);
        }

        ghostsByCharacterId.Clear();
    }
}