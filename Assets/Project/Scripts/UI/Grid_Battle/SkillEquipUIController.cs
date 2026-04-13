using UnityEngine;

public class SkillEquipUIController : MonoBehaviour
{
    public static SkillEquipUIController Instance;

    [Header("Selection")]
    public SkillSlotUI currentSlot;
    public CharacterSelectButtonUI currentCharacter;

    [Header("UI References")]
    public GameObject defaultCharacterHighlightObject;
    public GameObject currentOpenSkillList;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (defaultCharacterHighlightObject != null)
            defaultCharacterHighlightObject.SetActive(false);

        if (currentOpenSkillList != null)
            currentOpenSkillList.SetActive(false);

        HideAllCharacterHighlights();
    }

    public void SelectSlot(SkillSlotUI slot)
    {
        currentSlot = slot;
        currentCharacter = null;

        CloseCurrentSkillList();
        HideAllCharacterHighlights();

        if (defaultCharacterHighlightObject != null)
            defaultCharacterHighlightObject.SetActive(true);

        Debug.Log("현재 슬롯 선택: " + slot.name);
    }

    public void SelectCharacter(CharacterSelectButtonUI character)
    {
        if (currentSlot == null)
        {
            Debug.Log("먼저 슬롯을 선택해야 합니다.");
            return;
        }

        currentCharacter = character;

        HideAllCharacterHighlights();
        character.ShowHighlight(true);

        CloseCurrentSkillList();

        if (character.skillListObject != null)
        {
            currentOpenSkillList = character.skillListObject;
            currentOpenSkillList.SetActive(true);
        }

        Debug.Log("현재 캐릭터 선택: " + character.name);
    }

    public void SelectSkill(SkillSelectButtonUI skillButton)
    {
        if (currentSlot == null)
        {
            Debug.Log("슬롯이 선택되지 않았습니다.");
            return;
        }

        if (currentCharacter == null)
        {
            Debug.Log("캐릭터가 선택되지 않았습니다.");
            return;
        }

        currentSlot.SetSkill(skillButton.skillIcon);

        Debug.Log($"슬롯 [{currentSlot.name}] 에 스킬 [{skillButton.skillName}] 장착");

        CloseCurrentSkillList();
        HideAllCharacterHighlights();

        currentSlot = null;
        currentCharacter = null;

        if (defaultCharacterHighlightObject != null)
            defaultCharacterHighlightObject.SetActive(false);
    }

    public void CloseCurrentSkillList()
    {
        if (currentOpenSkillList != null)
        {
            currentOpenSkillList.SetActive(false);
            currentOpenSkillList = null;
        }
    }

    public void HideAllCharacterHighlights()
    {
        CharacterSelectButtonUI[] characters = FindObjectsOfType<CharacterSelectButtonUI>(true);

        foreach (CharacterSelectButtonUI character in characters)
        {
            character.ShowHighlight(false);
        }
    }

    public void ResetSelectionState()
    {
        currentSlot = null;
        currentCharacter = null;

        CloseCurrentSkillList();
        HideAllCharacterHighlights();

        if (defaultCharacterHighlightObject != null)
            defaultCharacterHighlightObject.SetActive(false);
    }
}