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

        Vector3 position = gridManager.GetWorldPositionByIndex(gridIndex);

        if (!ghostsByCharacterId.TryGetValue(characterId, out SpriteRenderer ghost) ||
            ghost == null)
        {
            ghost = Instantiate(ghostPrefab, position, Quaternion.identity);
            ghostsByCharacterId[characterId] = ghost;
        }

        ghost.transform.position = position;
        ghost.sprite = sprite;
        ApplyDirection(ghost, direction);

        Color color = ghost.color;
        color.a = 0.4f;
        ghost.color = color;
    }

    public void ClearExcept(ICollection<string> characterIdsToKeep)
    {
        if (characterIdsToKeep == null)
        {
            ClearAll();
            return;
        }

        List<string> removeTargets = new();

        foreach (var pair in ghostsByCharacterId)
        {
            if (!characterIdsToKeep.Contains(pair.Key))
                removeTargets.Add(pair.Key);
        }

        for (int i = 0; i < removeTargets.Count; i++)
            Clear(removeTargets[i]);
    }

    public void Clear(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return;

        if (!ghostsByCharacterId.TryGetValue(characterId, out SpriteRenderer ghost))
            return;

        DestroyGhost(ghost);

        ghostsByCharacterId.Remove(characterId);
    }

    public void ClearAll()
    {
        foreach (var pair in ghostsByCharacterId)
        {
            DestroyGhost(pair.Value);
        }

        ghostsByCharacterId.Clear();
    }

    private static void ApplyDirection(SpriteRenderer ghost, BattleDirection direction)
    {
        if (ghost == null)
            return;

        BattleUnitFacing facing = ghost.GetComponentInParent<BattleUnitFacing>();

        if (facing == null)
            facing = ghost.GetComponentInChildren<BattleUnitFacing>();

        if (facing != null)
        {
            facing.FaceRight(direction == BattleDirection.Right);
            return;
        }

        ghost.flipX = direction == BattleDirection.Left;
    }

    private static void DestroyGhost(SpriteRenderer ghost)
    {
        if (ghost == null)
            return;

        if (Application.isPlaying)
            Destroy(ghost.gameObject);
        else
            DestroyImmediate(ghost.gameObject);
    }
}
