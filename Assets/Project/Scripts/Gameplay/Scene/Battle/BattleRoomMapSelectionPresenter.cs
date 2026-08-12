using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleRoomMapSelectionPresenter : MonoBehaviour
{
    [SerializeField] private Vector3 firstCharacterPosition = new(-5.5f, -0.25f, -2f);
    [SerializeField] private float characterSpacing = 2.1f;

    private readonly List<GameObject> hiddenRoomUiRoots = new();
    private readonly List<ColliderState> disabledCharacterColliders = new();

    private readonly struct ColliderState
    {
        public readonly Component Collider;
        public readonly bool WasEnabled;

        public ColliderState(Collider collider)
        {
            Collider = collider;
            WasEnabled = collider != null && collider.enabled;
        }

        public ColliderState(Collider2D collider)
        {
            Collider = collider;
            WasEnabled = collider != null && collider.enabled;
        }
    }

    public static Vector3 CalculateCharacterPosition(int index)
    {
        return new Vector3(-5.5f + Mathf.Max(0, index) * 2.1f, -0.25f, -2f);
    }

    public void Show(GameObject activeRoom)
    {
        if (activeRoom == null)
            return;

        List<Transform> characters = new();

        BattleMapSelectionCharacterMarker[] markedCharacters =
            activeRoom.GetComponentsInChildren<BattleMapSelectionCharacterMarker>(true);
        for (int i = 0; i < markedCharacters.Length; i++)
        {
            if (markedCharacters[i] != null && !characters.Contains(markedCharacters[i].transform))
                characters.Add(markedCharacters[i].transform);
        }

        characters.Sort((left, right) =>
            string.Compare(left.name, right.name, StringComparison.Ordinal));

        Canvas[] roomCanvases = activeRoom.GetComponentsInChildren<Canvas>(true);
        GameObject[] roomUiRoots = new GameObject[roomCanvases.Length];
        for (int i = 0; i < roomCanvases.Length; i++)
            roomUiRoots[i] = roomCanvases[i] != null ? roomCanvases[i].gameObject : null;

        Show(activeRoom, characters, roomUiRoots);
    }

    public void Show(
        GameObject activeRoom,
        IReadOnlyList<Transform> characters,
        IReadOnlyList<GameObject> roomUiRoots)
    {
        if (activeRoom == null)
            return;

        activeRoom.SetActive(true);
        hiddenRoomUiRoots.Clear();
        disabledCharacterColliders.Clear();
        ClearBattleGridPresentation(activeRoom);

        if (roomUiRoots != null)
        {
            for (int i = 0; i < roomUiRoots.Count; i++)
            {
                GameObject uiRoot = roomUiRoots[i];
                if (uiRoot == null || !uiRoot.activeSelf)
                    continue;

                uiRoot.SetActive(false);
                hiddenRoomUiRoots.Add(uiRoot);
            }
        }

        if (characters == null)
            return;

        for (int i = 0; i < characters.Count; i++)
        {
            Transform character = characters[i];
            if (character == null)
                continue;

            character.position = firstCharacterPosition + Vector3.right * (characterSpacing * i);

            BattleUnitFacing facing = character.GetComponent<BattleUnitFacing>();
            facing?.FaceRight(true);

            Collider[] colliders = character.GetComponentsInChildren<Collider>(true);
            for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
            {
                Collider collider = colliders[colliderIndex];
                disabledCharacterColliders.Add(new ColliderState(collider));
                if (collider != null)
                    collider.enabled = false;
            }

            Collider2D[] colliders2D = character.GetComponentsInChildren<Collider2D>(true);
            for (int colliderIndex = 0; colliderIndex < colliders2D.Length; colliderIndex++)
            {
                Collider2D collider = colliders2D[colliderIndex];
                disabledCharacterColliders.Add(new ColliderState(collider));
                if (collider != null)
                    collider.enabled = false;
            }
        }
    }

    public void Hide()
    {
        for (int i = 0; i < disabledCharacterColliders.Count; i++)
        {
            ColliderState state = disabledCharacterColliders[i];
            if (state.Collider != null)
            {
                if (state.Collider is Collider collider)
                    collider.enabled = state.WasEnabled;
                else if (state.Collider is Collider2D collider2D)
                    collider2D.enabled = state.WasEnabled;
            }
        }
        disabledCharacterColliders.Clear();

        for (int i = 0; i < hiddenRoomUiRoots.Count; i++)
        {
            if (hiddenRoomUiRoots[i] != null)
                hiddenRoomUiRoots[i].SetActive(true);
        }

        hiddenRoomUiRoots.Clear();
    }

    private static void ClearBattleGridPresentation(GameObject activeRoom)
    {
        BattleGridEffectController[] effectControllers =
            activeRoom.GetComponentsInChildren<BattleGridEffectController>(true);
        for (int i = 0; i < effectControllers.Length; i++)
            effectControllers[i]?.ClearAll();

        GridCell[] cells = activeRoom.GetComponentsInChildren<GridCell>(true);
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] == null)
                continue;
            cells[i].SetNormal();
            cells[i].ClearExecutionRangeTint();
        }
    }
}

public sealed class BattleMapSelectionCharacterMarker : MonoBehaviour
{
}
