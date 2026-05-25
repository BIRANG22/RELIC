using UnityEngine;

public class DirectionUIPerspectiveController : MonoBehaviour
{
    [SerializeField] private int gridWidth = 7;

    [Header("Arrow Skew Components")]
    [SerializeField] private UIPerspectiveSkew forward;
    [SerializeField] private UIPerspectiveSkew backward;
    [SerializeField] private UIPerspectiveSkew left;
    [SerializeField] private UIPerspectiveSkew right;

    [Header("Perspective Strength")]
    [SerializeField] private float maxSkew = 12f;
    [SerializeField] private float nearScale = 1.15f;
    [SerializeField] private float farScale = 0.85f;

    public void RefreshByGridIndex(int gridIndex)
    {
        int gridX = gridIndex % gridWidth;
        RefreshByGridX(gridX);
    }

    public void RefreshByGridX(int gridX)
    {
        float centerX = (gridWidth - 1) * 0.5f;
        float perspective = centerX <= 0f ? 0f : (gridX - centerX) / centerX;

        float skew = perspective * maxSkew;

        ApplyForwardBackward(skew);
        ApplySideArrows(skew);
    }

    private void ApplyForwardBackward(float skew)
    {
        if (forward != null)
        {
            forward.SetSkew(skew, -skew);
            forward.SetScale(farScale, nearScale);
        }

        if (backward != null)
        {
            backward.SetSkew(-skew, skew);
            backward.SetScale(nearScale, farScale);
        }
    }

    private void ApplySideArrows(float skew)
    {
        if (left != null)
        {
            left.SetSkew(skew * 0.5f, -skew * 0.5f);
            left.SetScale(1f, 1f);
        }

        if (right != null)
        {
            right.SetSkew(skew * 0.5f, -skew * 0.5f);
            right.SetScale(1f, 1f);
        }
    }
}