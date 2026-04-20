using UnityEngine;

public class OptionPanelUI : MonoBehaviour
{
    [Header("Contents")]
    [SerializeField] private GameObject soundContent;
    [SerializeField] private GameObject languageContent;

    private void OnEnable()
    {
        ShowSound();
    }

    public void ShowSound()
    {
        soundContent.SetActive(true);
        languageContent.SetActive(false);
    }

    public void ShowLanguage()
    {
        soundContent.SetActive(false);
        languageContent.SetActive(true);
    }
}