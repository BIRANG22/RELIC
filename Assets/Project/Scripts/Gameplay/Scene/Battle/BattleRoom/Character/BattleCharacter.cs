using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.EventSystems;

public class BattleCharacter : MonoBehaviour
{
    [Header("Timeline Hover Highlight")]
    [SerializeField] private GameObject timelineHoverHighlightObject;
    [SerializeField] private bool autoFindTimelineHoverHighlightObject = true;

    public CharacterRuntimeData RuntimeData { get; private set; }

    private readonly List<SkillMasterData> equippedSkills = new();
    public IReadOnlyList<SkillMasterData> EquippedSkills => equippedSkills;

    public string CharacterId => RuntimeData != null ? RuntimeData.CharacterId : null;

    public int CurrentGridIndex { get; private set; } = -1;

    private void Awake()
    {
        EnsureTimelineHoverHighlightObject();
        SetTimelineHoverHighlight(false);
    }

    public void Initialize(CharacterRuntimeData runtimeData)
    {
        RuntimeData = runtimeData;
        LoadEquippedSkills();

        EnsureTimelineHoverHighlightObject();
        SetTimelineHoverHighlight(false);
    }

    public void SetGridIndex(int gridIndex)
    {
        CurrentGridIndex = gridIndex;
    }

    public void SetTimelineHoverHighlight(bool active)
    {
        EnsureTimelineHoverHighlightObject();

        if (timelineHoverHighlightObject != null)
            timelineHoverHighlightObject.SetActive(active);
    }

    private void OnDisable()
    {
        SetTimelineHoverHighlight(false);
    }

    private void OnMouseDown()
    {
        if (RuntimeData == null)
            return;

        if (IsPointerOverUI())
            return;

        BattleRoomLoader roomLoader = FindFirstObjectByType<BattleRoomLoader>(FindObjectsInactive.Include);

        if (roomLoader == null)
        {
            Debug.LogWarning("[BattleCharacter] BattleRoomLoader가 없습니다.");
            return;
        }

        roomLoader.OnPlayerCharacterClicked(RuntimeData);
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        if (EventSystem.current.IsPointerOverGameObject())
            return true;

        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);

        return false;
    }

    private void LoadEquippedSkills()
    {
        equippedSkills.Clear();

        if (RuntimeData == null)
            return;

        AddSkill(RuntimeData.MoveSkillId);

        if (RuntimeData.EquippedSkillIds == null ||
            RuntimeData.EquippedSkillIds.Length != 4)
        {
            RuntimeData.EquippedSkillIds = new string[4]
            {
                RuntimeData.UniqueSkillId,
                RuntimeData.AbilitySkillId,
                "",
                ""
            };
        }

        for (int i = 0; i < RuntimeData.EquippedSkillIds.Length; i++)
        {
            AddSkill(RuntimeData.EquippedSkillIds[i]);
        }
    }

    private void AddSkill(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return;

        if (DataManager.Instance == null)
            return;

        for (int i = 0; i < equippedSkills.Count; i++)
        {
            if (equippedSkills[i] != null && equippedSkills[i].SkillId == skillId)
                return;
        }

        SkillMasterData skillData = DataManager.Instance.SkillDatabase.Get(skillId);

        if (skillData == null)
        {
            Debug.LogWarning($"[BattleCharacter] SkillData 없음: {skillId}");
            return;
        }

        equippedSkills.Add(skillData);
    }

    private void EnsureTimelineHoverHighlightObject()
    {
        if (timelineHoverHighlightObject != null)
            return;

        if (!autoFindTimelineHoverHighlightObject)
            return;

        timelineHoverHighlightObject = FindTimelineHoverHighlightObject(transform);
    }

    private GameObject FindTimelineHoverHighlightObject(Transform root)
    {
        if (root == null)
            return null;

        string objectName = root.name;

        if (objectName == "TimelineHoverHighlight" ||
            objectName == "Timeline_HoverHighlight" ||
            objectName == "Timeline_Hover_Highlight" ||
            objectName == "TimelineHighlight" ||
            objectName == "Timeline_Highlight")
        {
            return root.gameObject;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            GameObject found = FindTimelineHoverHighlightObject(root.GetChild(i));

            if (found != null)
                return found;
        }

        return null;
    }
}
