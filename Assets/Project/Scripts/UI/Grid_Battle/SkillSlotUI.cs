using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour
{
    [Header("Skill Icons (Max 3)")]
    [SerializeField] private Image[] skillIconImages;

    [Header("Slot Setting")]
    [SerializeField] private int maxSkillCount = 3;

    private readonly List<Sprite> equippedSkillIcons = new List<Sprite>();
    private CharacterSelectButtonUI ownerCharacter;

    public int SkillCount => equippedSkillIcons.Count;
    public bool IsFull => equippedSkillIcons.Count >= maxSkillCount;
    public IReadOnlyList<Sprite> EquippedSkillIcons => equippedSkillIcons;
    public CharacterSelectButtonUI OwnerCharacter => ownerCharacter;
    public bool HasOwnerCharacter => ownerCharacter != null;

    public void OnClickSlot()
    {
        if (SkillEquipUIController.Instance == null)
        {
            Debug.LogError("SkillEquipUIController.Instance 가 없습니다.");
            return;
        }

        SkillEquipUIController.Instance.SelectSlot(this);
    }

    public void SetOwnerCharacter(CharacterSelectButtonUI character)
    {
        ownerCharacter = character;
    }

    public bool CanAcceptCharacter(CharacterSelectButtonUI character)
    {
        if (character == null)
            return false;

        if (ownerCharacter == null)
            return true;

        return ownerCharacter == character;
    }

    public bool AddSkill(Sprite icon)
    {
        if (icon == null)
        {
            Debug.LogWarning($"{name}: 추가할 스킬 아이콘이 없습니다.");
            return false;
        }

        if (IsFull)
        {
            Debug.Log($"{name}: 이미 스킬이 가득 찼습니다.");
            return false;
        }

        equippedSkillIcons.Add(icon);
        RefreshUI();
        return true;
    }

    public void ClearSkills()
    {
        equippedSkillIcons.Clear();
        ownerCharacter = null;
        RefreshUI();
    }

    public void RemoveSkillAt(int index)
    {
        if (index < 0 || index >= equippedSkillIcons.Count)
        {
            Debug.LogWarning($"{name}: RemoveSkillAt 인덱스가 범위를 벗어났습니다.");
            return;
        }

        equippedSkillIcons.RemoveAt(index);

        if (equippedSkillIcons.Count == 0)
            ownerCharacter = null;

        RefreshUI();
    }

    private void Awake()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (skillIconImages == null || skillIconImages.Length == 0)
        {
            Debug.LogWarning($"{name}: skillIconImages가 연결되지 않았습니다.");
            return;
        }

        for (int i = 0; i < skillIconImages.Length; i++)
        {
            if (skillIconImages[i] == null)
                continue;

            skillIconImages[i].sprite = null;
            skillIconImages[i].enabled = false;
        }

        for (int i = 0; i < equippedSkillIcons.Count && i < skillIconImages.Length; i++)
        {
            if (skillIconImages[i] == null)
                continue;

            skillIconImages[i].sprite = equippedSkillIcons[i];
            skillIconImages[i].enabled = true;
        }

        UpdateIconLayout();
    }

    private void UpdateIconLayout()
    {
        int count = equippedSkillIcons.Count;

        ResetAllIconTransforms();

        if (count == 1)
        {
            SetIconTransform(0, Vector2.zero, new Vector2(72f, 72f));
        }
        else if (count == 2)
        {
            SetIconTransform(0, new Vector2(-16f, 12f), new Vector2(48f, 48f));
            SetIconTransform(1, new Vector2(16f, -12f), new Vector2(48f, 48f));
        }
        else if (count >= 3)
        {
            SetIconTransform(0, new Vector2(0f, 18f), new Vector2(40f, 40f));
            SetIconTransform(1, new Vector2(-18f, -10f), new Vector2(40f, 40f));
            SetIconTransform(2, new Vector2(18f, -10f), new Vector2(40f, 40f));
        }
    }

    private void ResetAllIconTransforms()
    {
        for (int i = 0; i < skillIconImages.Length; i++)
        {
            if (skillIconImages[i] == null)
                continue;

            RectTransform rt = skillIconImages[i].rectTransform;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }
    }

    private void SetIconTransform(int index, Vector2 anchoredPos, Vector2 size)
    {
        if (index < 0 || index >= skillIconImages.Length)
            return;

        if (skillIconImages[index] == null)
            return;

        RectTransform rt = skillIconImages[index].rectTransform;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
    }
}