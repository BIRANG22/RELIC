using Relic.Gameplay.Battle;
using UnityEngine;

public class SkillEquipUIController : MonoBehaviour
{
    public static SkillEquipUIController Instance;

    [Header("Selection")]
    public CharacterSelectButtonUI currentCharacter;

    [Header("UI References")]
    public GameObject defaultCharacterHighlightObject;
    public GameObject currentOpenSkillList;

    private PlayerActionPlanner playerActionPlanner;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        playerActionPlanner = Object.FindFirstObjectByType<PlayerActionPlanner>();
    }

    private void Start()
    {
        if (defaultCharacterHighlightObject != null)
            defaultCharacterHighlightObject.SetActive(false);

        CloseCurrentSkillList();
        HideAllCharacterHighlights();
    }

    public void SelectCharacter(CharacterSelectButtonUI character)
    {
        if (character == null)
            return;

        currentCharacter = character;

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

        if (playerActionPlanner == null)
            playerActionPlanner = Object.FindFirstObjectByType<PlayerActionPlanner>();

        if (playerActionPlanner != null)
            playerActionPlanner.SelectPlayer(character);
        else
            Debug.LogWarning("[SkillEquipUIController] PlayerActionPlanner를 찾지 못했습니다.");

        Debug.Log("현재 캐릭터 선택: " + character.name);
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
        CharacterSelectButtonUI[] characters =
            FindObjectsByType<CharacterSelectButtonUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (CharacterSelectButtonUI character in characters)
        {
            character.ShowHighlight(false);
        }
    }

    public void ResetSelectionState()
    {
        currentCharacter = null;

        CloseCurrentSkillList();
        HideAllCharacterHighlights();

        if (defaultCharacterHighlightObject != null)
            defaultCharacterHighlightObject.SetActive(false);
    }
}