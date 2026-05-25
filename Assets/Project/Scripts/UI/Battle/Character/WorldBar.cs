using UnityEngine;

public class WorldBar : MonoBehaviour
{
    [SerializeField] private Transform fillTransform;

    public void SetValue(float current, float max)
    {
        float ratio = max <= 0 ? 0 : current / max;

        Vector3 scale = fillTransform.localScale;
        scale.x = ratio;
        fillTransform.localScale = scale;
    }
}