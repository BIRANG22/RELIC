using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimelineSkillHoverPopupView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text effectText;

    [Header("Fallback Text")]
    [SerializeField] private string emptyNameText = "";
    [TextArea]
    [SerializeField] private string emptyEffectText = "";

    private void Awake()
    {
        AutoBindReferences();
        DisableRaycastTargets();
    }

    private void OnValidate()
    {
        AutoBindReferences();
    }

    public void Set(string skillName, string effectDescription)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        AutoBindReferences();
        DisableRaycastTargets();

        if (nameText != null)
            nameText.text = string.IsNullOrWhiteSpace(skillName) ? emptyNameText : skillName;

        if (effectText != null)
            effectText.text = string.IsNullOrWhiteSpace(effectDescription) ? emptyEffectText : effectDescription;
    }

    private void AutoBindReferences()
    {
        if (backgroundImage == null)
            backgroundImage = FindImage("BackGround");

        if (backgroundImage == null)
            backgroundImage = FindImage("Background");

        if (backgroundImage == null)
            backgroundImage = FindImage("Image");

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (nameText == null)
            nameText = FindText("NameText");

        if (nameText == null)
            nameText = FindText("Name");

        if (effectText == null)
            effectText = FindText("EffectText");

        if (effectText == null)
            effectText = FindText("Effect");
    }

    private Image FindImage(string objectName)
    {
        Transform found = FindChildRecursive(transform, objectName);
        return found != null ? found.GetComponent<Image>() : null;
    }

    private TMP_Text FindText(string objectName)
    {
        Transform found = FindChildRecursive(transform, objectName);
        return found != null ? found.GetComponent<TMP_Text>() : null;
    }

    private Transform FindChildRecursive(Transform root, string objectName)
    {
        if (root == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child.name == objectName)
                return child;

            Transform found = FindChildRecursive(child, objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private void DisableRaycastTargets()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].raycastTarget = false;
        }
    }
}
