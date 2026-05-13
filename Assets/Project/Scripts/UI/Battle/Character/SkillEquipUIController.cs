using Relic.Gameplay.Data;
using UnityEngine;

public class SkillEquipUIController : MonoBehaviour
{
    public static SkillEquipUIController Instance;

    [Header("Timeline")]
    [SerializeField] private SkillSlotUI[] skillSlots;
    [SerializeField] private TimelineSlotUI[] playerTimelineSlots;

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

        if (currentSlot != null)
            currentSlot.SetSelected(false);

        currentSlot = slot;
        currentSlot.SetSelected(true);

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

    public void SelectSkill(SkillMasterData skillData)
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

        if (skillData == null)
        {
            Debug.LogWarning("선택된 스킬 데이터가 없습니다.");
            return;
        }

        bool added = currentSlot.AddSkill(skillData.Icon);

        if (!added)
        {
            Debug.Log($"슬롯 [{currentSlot.name}] 에 스킬을 추가할 수 없습니다.");
            return;
        }

        UpdatePlayerTimeline(skillData);

        Debug.Log($"슬롯 [{currentSlot.name}] 에 스킬 [{skillData.Name}] 장착");
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
        if (currentSlot != null)
            currentSlot.SetSelected(false);

        currentSlot = null;
        currentCharacter = null;

        CloseCurrentSkillList();
        HideAllCharacterHighlights();

        if (defaultCharacterHighlightObject != null)
            defaultCharacterHighlightObject.SetActive(false);
    }

    private int GetSlotIndex(SkillSlotUI slot)
    {
        if (slot == null || skillSlots == null)
            return -1;

        for (int i = 0; i < skillSlots.Length; i++)
        {
            if (skillSlots[i] == slot)
                return i;
        }

        return -1;
    }

    private void UpdatePlayerTimeline(SkillMasterData skillData)
    {
        int slotIndex = GetSlotIndex(currentSlot);

        if (slotIndex < 0)
        {
            Debug.LogWarning("현재 슬롯 인덱스를 찾을 수 없습니다.");
            return;
        }

        if (playerTimelineSlots == null || slotIndex >= playerTimelineSlots.Length)
        {
            Debug.LogWarning($"PlayerTimelineSlot 연결 안 됨: index={slotIndex}");
            return;
        }

        if (DataManager.Instance.ActionTypeIconDatabase == null)
        {
            Debug.LogWarning("ActionTypeIconDatabase가 없습니다.");
            return;
        }

        string actionType = skillData.TimelineNotation.ToString();

        if (!DataManager.Instance.ActionTypeIconDatabase.TryGetIcon(actionType, out Sprite actionTypeIcon))
        {
            Debug.LogWarning($"TimelineNotation 아이콘 없음: {actionType}");
            return;
        }

        Sprite characterIcon = currentCharacter.CharacterIcon;

        playerTimelineSlots[slotIndex].SetOwnerIcon(characterIcon);
        playerTimelineSlots[slotIndex].AddActionTypeIcon(actionTypeIcon);
    }
}