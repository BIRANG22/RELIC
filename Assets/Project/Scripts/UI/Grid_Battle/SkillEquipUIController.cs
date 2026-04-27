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
        {
            currentOpenSkillList.SetActive(false);
            currentOpenSkillList = null;
        }

        HideAllCharacterHighlights();
    }

    public void SelectSlot(SkillSlotUI slot)
    {
        if (slot == null)
            return;

        currentSlot = slot;

        CloseCurrentSkillList();
        HideAllCharacterHighlights();

        if (currentSlot.HasOwnerCharacter)
        {
            currentCharacter = currentSlot.OwnerCharacter;
            currentCharacter.ShowHighlight(true);

            if (currentCharacter.skillListObject != null)
            {
                currentOpenSkillList = currentCharacter.skillListObject;
                currentOpenSkillList.SetActive(true);
            }
        }
        else
        {
            currentCharacter = null;

            if (defaultCharacterHighlightObject != null)
                defaultCharacterHighlightObject.SetActive(true);
        }

        Debug.Log("현재 슬롯 선택: " + slot.name);
    }

    public void SelectCharacter(CharacterSelectButtonUI character)
    {
        if (currentSlot == null)
        {
            Debug.Log("먼저 슬롯을 선택해야 합니다.");
            return;
        }

        if (currentSlot.IsFull)
        {
            Debug.Log("현재 슬롯은 이미 가득 찼습니다.");
            return;
        }

        if (!currentSlot.CanAcceptCharacter(character))
        {
            Debug.Log("이 슬롯에는 이미 다른 캐릭터가 지정되어 있습니다.");
            return;
        }

        currentCharacter = character;

        if (!currentSlot.HasOwnerCharacter)
            currentSlot.SetOwnerCharacter(character);

        HideAllCharacterHighlights();
        character.ShowHighlight(true);

        CloseCurrentSkillList();

        if (character.skillListObject != null)
        {
            currentOpenSkillList = character.skillListObject;
            currentOpenSkillList.SetActive(true);
        }

        if (defaultCharacterHighlightObject != null)
            defaultCharacterHighlightObject.SetActive(false);

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

        if (skillButton == null)
        {
            Debug.LogWarning("선택된 스킬 버튼이 없습니다.");
            return;
        }

        bool added = currentSlot.AddSkill(skillButton.skillIcon);

        if (!added)
        {
            Debug.Log($"슬롯 [{currentSlot.name}] 에 스킬을 추가할 수 없습니다.");
            return;
        }

        Debug.Log($"슬롯 [{currentSlot.name}] 에 스킬 [{skillButton.skillName}] 장착");

        if (currentSlot.IsFull)
        {
            Debug.Log($"슬롯 [{currentSlot.name}] 이 가득 찼습니다.");

            CloseCurrentSkillList();
            HideAllCharacterHighlights();

            currentSlot = null;
            currentCharacter = null;

            if (defaultCharacterHighlightObject != null)
                defaultCharacterHighlightObject.SetActive(false);
        }
        else
        {
            HideAllCharacterHighlights();
            currentCharacter.ShowHighlight(true);

            if (currentCharacter.skillListObject != null)
            {
                if (currentOpenSkillList != currentCharacter.skillListObject)
                {
                    CloseCurrentSkillList();
                    currentOpenSkillList = currentCharacter.skillListObject;
                }

                currentOpenSkillList.SetActive(true);
            }
        }
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
        CharacterSelectButtonUI[] characters = FindObjectsByType<CharacterSelectButtonUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);

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