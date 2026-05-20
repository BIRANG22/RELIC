using UnityEngine;

public class SkillResourcePreviewUI : MonoBehaviour
{
    [Header("Preview Fill")]
    [SerializeField] private Transform previewFill;

    private Vector3 defaultScale;

    private void Awake()
    {
        if (previewFill != null)
            defaultScale = previewFill.localScale;
    }

    public void SetPreview(float normalizedValue)
    {
        if (previewFill == null)
            return;

        normalizedValue = Mathf.Clamp01(normalizedValue);

        Vector3 scale = defaultScale;
        scale.x *= normalizedValue;

        previewFill.localScale = scale;
    }

    public void SetPreviewByValue(int currentValue, int maxValue)
    {
        if (maxValue <= 0)
        {
            SetPreview(0f);
            return;
        }

        float normalized =
            (float)Mathf.Clamp(currentValue, 0, maxValue) / maxValue;

        SetPreview(normalized);
    }
}