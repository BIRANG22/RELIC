using System.Collections;
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

    public IEnumerator PlayAndWait(Sprite image)
    {
        gameObject.SetActive(false);
        SetImage(image);
        gameObject.SetActive(true);

        yield return new WaitUntil(() => !gameObject.activeInHierarchy);
    }
}
