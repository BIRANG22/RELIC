using UnityEngine;
using UnityEngine.UI;

public class SkillResourcePreviewUI : MonoBehaviour
{
    [Header("Resource Fill")]
    [SerializeField] private Image previewFill;

    public void SetPreview(float normalizedValue)
    {
        if (previewFill == null)
            return;

        previewFill.fillAmount = Mathf.Clamp01(normalizedValue);
    }

    public void SetPreviewByValue(int currentValue, int maxValue)
    {
        if (maxValue <= 0)
        {
            SetPreview(0f);
            return;
        }

        SetPreview((float)Mathf.Clamp(currentValue, 0, maxValue) / maxValue);
    }
}
