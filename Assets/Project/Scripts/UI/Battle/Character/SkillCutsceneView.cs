using UnityEngine;
using UnityEngine.UI;

public class SkillCutsceneView : MonoBehaviour
{
    [SerializeField] private Image cutsceneImage;

    public void SetImage(Sprite image)
    {
        if (cutsceneImage != null)
            cutsceneImage.sprite = image;
    }
}
