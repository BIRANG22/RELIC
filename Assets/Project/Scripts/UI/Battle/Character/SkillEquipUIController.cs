using Relic.Gameplay.Battle;
using Relic.Gameplay.Data;
using UnityEngine;

public class SkillEquipUIController : MonoBehaviour
{
    public static SkillEquipUIController Instance;

    [Header("Selection")]
    public CharacterSelectButtonUI currentCharacter;

    [Header("UI References")]
    public GameObject defaultCharacterHighlightObject;

    [Header("Scene Skill UI")]
    [SerializeField] private GameObject skillListObject;
    [SerializeField] private SkillSelectButtonUI[] skillButtons;

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

        CloseSkillList();
        HideAllCharacterHighlights();
    }

    public void SelectCharacter(CharacterSelectButtonUI character)
    {
        if (character == null)
            return;

        currentCharacter = character;

        HideAllCharacterHighlights();
        character.ShowHighlight(true);

        RefreshSkillList(character.BattleCharacter);

        if (defaultCharacterHighlightObject != null)
            defaultCharacterHighlightObject.SetActive(false);

        if (playerActionPlanner == null)
            playerActionPlanner = Object.FindFirstObjectByType<PlayerActionPlanner>();

        if (playerActionPlanner != null)
            playerActionPlanner.SelectPlayer(character);
        else
            Debug.LogWarning("[SkillEquipUIController] PlayerActionPlanner를 찾지 못했습니다.");

        Debug.Log($"현재 캐릭터 선택: {character.CharacterId}");
    }

    private void RefreshSkillList(BattleCharacter battleCharacter)
    {
        if (skillListObject != null)
            skillListObject.SetActive(true);

        ClearSkillButtons();

        if (battleCharacter == null || battleCharacter.RuntimeData == null)
            return;

        CharacterRuntimeData runtime = battleCharacter.RuntimeData;

        SetSkillButton(0, runtime.AbilitySkillId1);
        SetSkillButton(1, runtime.AbilitySkillId2);
        SetSkillButton(2, runtime.UniqueSkillId);
    }

    private void SetSkillButton(int index, string skillId)
    {
        Debug.Log($"[SkillEquipUIController] SetSkillButton index:{index}, skillId:{skillId}");

        if (skillButtons == null || index < 0 || index >= skillButtons.Length)
        {
            Debug.LogWarning("[SkillEquipUIController] skillButtons index invalid");
            return;
        }

        SkillSelectButtonUI button = skillButtons[index];

        if (button == null)
        {
            Debug.LogWarning($"[SkillEquipUIController] button null: {index}");
            return;
        }

        if (string.IsNullOrWhiteSpace(skillId))
        {
            Debug.LogWarning($"[SkillEquipUIController] skillId empty: {index}");
            button.ClearSkill();
            button.gameObject.SetActive(false);
            return;
        }

        SkillMasterData skillData = DataManager.Instance.SkillDatabase.Get(skillId);

        if (skillData == null)
        {
            Debug.LogWarning($"[SkillEquipUIController] SkillData 없음: {skillId}");
            button.ClearSkill();
            button.gameObject.SetActive(false);
            return;
        }

        Debug.Log($"[SkillEquipUIController] SkillData found: {skillData.SkillId}");

        button.gameObject.SetActive(true);
        button.SetSkill(skillData);
    }

    private void ClearSkillButtons()
    {
        if (skillButtons == null)
            return;

        for (int i = 0; i < skillButtons.Length; i++)
        {
            if (skillButtons[i] == null)
                continue;

            skillButtons[i].ClearSkill();
            skillButtons[i].gameObject.SetActive(false);
        }
    }

    public void CloseSkillList()
    {
        if (skillListObject != null)
            skillListObject.SetActive(false);

        ClearSkillButtons();
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

        CloseSkillList();
        HideAllCharacterHighlights();

        if (defaultCharacterHighlightObject != null)
            defaultCharacterHighlightObject.SetActive(false);
    }
}