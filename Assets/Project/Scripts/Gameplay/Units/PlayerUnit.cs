using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerUnit : MonoBehaviour
{
    public GridManager currentGrid;
    public float moveSpeed = 5f;

    private bool isMoving = false;
    private GridTile currentTile;
    private Animator anim;

    [SerializeField] private GameObject selectionRing;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();

        if (selectionRing != null)
            selectionRing.SetActive(false);
    }

    public void SetSelected(bool value)
    {
        if (selectionRing != null)
            selectionRing.SetActive(value);
    }

    private void OnMouseDown()
    {
        BattleManager.Instance.SelectUnit(this);
    }

    public void SetStartTile(GridTile tile)
    {
        currentTile = tile;
        tile.SetOccupant(this);
    }

    public void MoveTo(GridTile tile)
    {
        if (isMoving) return;
        if (tile.GetGrid() != currentGrid) return;
        if (tile.IsOccupied()) return;

        List<GridTile> path =
            BattleManager.Instance.FindPath(currentTile, tile);

        if (path == null) return;

        StartCoroutine(MovePathRoutine(path));
    }

    IEnumerator MovePathRoutine(List<GridTile> path)
    {
        isMoving = true;
        if (anim) anim.SetBool("IsMoving", true);

        currentTile.ClearOccupant();

        foreach (var step in path)
        {
            Vector3 targetPos = new Vector3(
                step.transform.position.x,
                transform.position.y,
                step.transform.position.z
            );

            while (Vector3.Distance(transform.position, targetPos) > 0.01f)
            {
                Vector3 dir = (targetPos - transform.position).normalized;
                dir.y = 0;

                if (dir != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir);

                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRot,
                        10f * Time.deltaTime   // 회전 속도
                    );
                }

                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPos,
                    moveSpeed * Time.deltaTime
                );

                yield return null;
            }
        }

        currentTile = path[path.Count - 1];
        currentTile.SetOccupant(this);

        if (anim) anim.SetBool("IsMoving", false);
        isMoving = false;
    }
}