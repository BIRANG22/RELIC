using UnityEngine;

public class DirectionUIPerspective : MonoBehaviour
{
    [Header("Grid Setting")]
    [SerializeField] private int gridWidth = 7;

    [Header("Arrow Buttons")]
    [SerializeField] private Transform forwardArrow;
    [SerializeField] private Transform backwardArrow;
    [SerializeField] private Transform leftArrow;
    [SerializeField] private Transform rightArrow;

    [Header("UI Distance")]
    [SerializeField] private float verticalDistance = 80f;
    [SerializeField] private float horizontalDistance = 80f;

    [Header("Perspective Offset")]
    [SerializeField] private float maxPerspectiveXOffset = 45f;

    public void RefreshByGridIndex(int gridIndex)
    {
        int x = gridIndex % gridWidth;
        RefreshByGridX(x);
    }

    public void RefreshByGridX(int gridX)
    {
        float centerX = (gridWidth - 1) * 0.5f;

        // x=0이면 +1, 중앙이면 0, x=6이면 -1
        float perspective = centerX <= 0f
            ? 0f
            : (centerX - gridX) / centerX;

        float forwardXOffset = perspective * maxPerspectiveXOffset;
        float backwardXOffset = -forwardXOffset;

        SetLocalPosition(forwardArrow, new Vector2(forwardXOffset, verticalDistance));
        SetLocalPosition(backwardArrow, new Vector2(backwardXOffset, -verticalDistance));

        SetLocalPosition(leftArrow, new Vector2(-horizontalDistance, 0f));
        SetLocalPosition(rightArrow, new Vector2(horizontalDistance, 0f));
    }

    private void SetLocalPosition(Transform target, Vector2 position)
    {
        if (target == null)
            return;

        target.localPosition = new Vector3(position.x, position.y, target.localPosition.z);
    }
}