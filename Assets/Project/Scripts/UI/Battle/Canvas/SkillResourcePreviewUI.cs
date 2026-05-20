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
}
