using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyCharacterSlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image backImage;
    [SerializeField] private TMP_Text nameText;

    public void Set(string characterName, Sprite icon)
    {
        gameObject.SetActive(true);

        if (backImage != null)
        {
            backImage.sprite = icon;
            backImage.enabled = icon != null;
        }

        if (nameText != null)
            nameText.text = characterName;
    }

    public void Clear()
    {
        if (backImage != null)
        {
            backImage.sprite = null;
            backImage.enabled = false;
        }

        if (nameText != null)
            nameText.text = "";
    }
}