using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInfoPanel : MonoBehaviour
{
    [Header("Value Texts")]
    [SerializeField] private TMP_Text hpValueText;
    [SerializeField] private TMP_Text costValueText;
    [SerializeField] private TMP_Text recoveryValueText;
    [SerializeField] private TMP_Text moveValueText;

    [Header("Character Mark")]
    [SerializeField] private Image characterMarkImage;
    [SerializeField] private bool autoBindCharacterMarkImage = true;
    [SerializeField] private bool hideMarkWhenMissing = true;

    [Header("Story")]
    [SerializeField] private TMP_Text storyText;

    private CharacterMasterData currentMasterData;
    private CharacterRuntimeData currentRuntimeData;

    private void Awake()
    {
        AutoBindCharacterMarkImageIfNeeded();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoBindCharacterMarkImageIfNeeded();
    }
#endif

    public void SetCharacter(CharacterMasterData masterData, CharacterRuntimeData runtimeData)
    {
        currentMasterData = masterData;
        currentRuntimeData = runtimeData;

        Refresh();
    }

    public void Refresh()
    {
        if (currentMasterData == null)
        {
            Clear();
            return;
        }

        if (hpValueText != null)
            hpValueText.text = currentMasterData.MaxHP.ToString();

        if (costValueText != null)
            costValueText.text = currentMasterData.MaxCost.ToString();

        if (recoveryValueText != null)
            recoveryValueText.text = currentMasterData.CostRecovery.ToString();

        if (moveValueText != null)
            moveValueText.text = currentMasterData.MoveValue.ToString();

        RefreshCharacterMark();

        if (storyText != null)
            storyText.text = FormatIntroduction(currentMasterData.Introduction);
    }

    public void Clear()
    {
        currentMasterData = null;
        currentRuntimeData = null;

        if (hpValueText != null)
            hpValueText.text = "";

        if (costValueText != null)
            costValueText.text = "";

        if (recoveryValueText != null)
            recoveryValueText.text = "";

        if (moveValueText != null)
            moveValueText.text = "";

        ClearCharacterMark();

        if (storyText != null)
            storyText.text = "";
    }

    private void RefreshCharacterMark()
    {
        AutoBindCharacterMarkImageIfNeeded();

        if (characterMarkImage == null)
            return;

        Sprite markSprite = GetCharacterMarkSprite();
        bool hasMark = markSprite != null;

        characterMarkImage.sprite = markSprite;
        characterMarkImage.enabled = hasMark || !hideMarkWhenMissing;
        characterMarkImage.gameObject.SetActive(hasMark || !hideMarkWhenMissing);
    }

    private void ClearCharacterMark()
    {
        if (characterMarkImage == null)
            return;

        characterMarkImage.sprite = null;

        if (hideMarkWhenMissing)
        {
            characterMarkImage.enabled = false;
            characterMarkImage.gameObject.SetActive(false);
        }
    }

    private Sprite GetCharacterMarkSprite()
    {
        if (currentMasterData == null)
            return null;

        if (DataManager.Instance == null)
            return null;

        if (DataManager.Instance.CharacterIconDatabase == null)
            return null;

        if (DataManager.Instance.CharacterIconDatabase.TryGetMark(currentMasterData.CharacterId, out Sprite mark))
            return mark;

        return null;
    }

    private void AutoBindCharacterMarkImageIfNeeded()
    {
        if (!autoBindCharacterMarkImage)
            return;

        if (characterMarkImage != null)
            return;

        Transform markTransform = FindChildByName(transform, "CharacterMark");

        if (markTransform == null)
            markTransform = FindChildByName(transform, "Mark");

        if (markTransform == null)
            markTransform = FindChildByName(transform, "MarkImage");

        if (markTransform == null)
            return;

        characterMarkImage = markTransform.GetComponent<Image>();

        if (characterMarkImage == null)
            characterMarkImage = markTransform.GetComponentInChildren<Image>(true);
    }

    private Transform FindChildByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
            return null;

        if (root.name == targetName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildByName(root.GetChild(i), targetName);

            if (result != null)
                return result;
        }

        return null;
    }

    private string FormatIntroduction(string introduction)
    {
        if (string.IsNullOrWhiteSpace(introduction))
            return "";

        string formattedIntroduction = introduction
            .Replace("\\r\\n", "\n")
            .Replace("\\n", "\n")
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");

        string[] lines = formattedIntroduction.Split('\n');

        if (lines.Length == 0)
            return "";

        lines[0] = $"<color=#4E66DF>{lines[0]}</color>";

        return string.Join("\n", lines);
    }
}
