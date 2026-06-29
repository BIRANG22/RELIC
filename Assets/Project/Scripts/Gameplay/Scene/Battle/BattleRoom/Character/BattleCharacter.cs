using System.Collections;
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

    [Header("Selection Scale Feedback")]
    [SerializeField] private string selectionScaleTargetName = "SpriteRoot";
    [SerializeField] private Transform selectionScaleTarget;
    [Tooltip("켜두면 선택 확대값을 절대 스케일이 아니라 현재 SpriteRoot 기본 스케일에 곱해서 계산합니다. 예: 기본 1.2에 1.125를 곱하면 1.35가 됩니다.")]
    [SerializeField] private bool useSelectedScaleMultiplier = true;
    [Tooltip("기본 SpriteRoot 스케일에 곱할 선택 확대 배율입니다. SpriteRoot 기본값이 1.2일 때 1.125면 선택 시 1.35가 됩니다.")]
    [SerializeField] private float selectedScaleMultiplier = 1.125f;
    [Tooltip("Use Selected Scale Multiplier를 끈 경우 사용할 절대 선택 스케일입니다.")]
    [SerializeField] private Vector3 selectedScale = new Vector3(1.35f, 1.35f, 1f);
    [SerializeField] private float selectionScaleDuration = 0.12f;

    private Vector3 originalSelectionScale = Vector3.one;
    private bool hasOriginalSelectionScale;
    private Coroutine selectionScaleRoutine;

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


    public void SetSelectionScaleFeedback(bool selected)
    {
        EnsureSelectionScaleTarget();

        if (selectionScaleTarget == null)
            return;

        Vector3 targetScale = selected ? GetSelectedScaleTarget() : originalSelectionScale;

        if (selectionScaleRoutine != null)
            StopCoroutine(selectionScaleRoutine);

        if (!isActiveAndEnabled || selectionScaleDuration <= 0f)
        {
            selectionScaleTarget.localScale = targetScale;
            selectionScaleRoutine = null;
            return;
        }

        selectionScaleRoutine = StartCoroutine(AnimateSelectionScale(targetScale));
    }


    private Vector3 GetSelectedScaleTarget()
    {
        if (!useSelectedScaleMultiplier)
            return selectedScale;

        float multiplier = selectedScaleMultiplier;

        if (multiplier <= 0f)
            multiplier = 1f;

        return new Vector3(
            originalSelectionScale.x * multiplier,
            originalSelectionScale.y * multiplier,
            originalSelectionScale.z
        );
    }

    private IEnumerator AnimateSelectionScale(Vector3 targetScale)
    {
        if (selectionScaleTarget == null)
        {
            selectionScaleRoutine = null;
            yield break;
        }

        Vector3 startScale = selectionScaleTarget.localScale;
        float elapsed = 0f;

        while (elapsed < selectionScaleDuration)
        {
            if (selectionScaleTarget == null)
            {
                selectionScaleRoutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / selectionScaleDuration);
            selectionScaleTarget.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        if (selectionScaleTarget != null)
            selectionScaleTarget.localScale = targetScale;

        selectionScaleRoutine = null;
    }

    private void ResetSelectionScaleImmediate()
    {
        if (selectionScaleRoutine != null)
        {
            StopCoroutine(selectionScaleRoutine);
            selectionScaleRoutine = null;
        }

        if (selectionScaleTarget != null && hasOriginalSelectionScale)
            selectionScaleTarget.localScale = originalSelectionScale;
    }

    private void EnsureSelectionScaleTarget()
    {
        if (selectionScaleTarget == null)
            selectionScaleTarget = FindChildByName(transform, selectionScaleTargetName);

        if (selectionScaleTarget == null)
            return;

        if (hasOriginalSelectionScale)
            return;

        originalSelectionScale = selectionScaleTarget.localScale;
        hasOriginalSelectionScale = true;
    }

    private Transform FindChildByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
            return null;

        if (root.name == targetName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), targetName);

            if (found != null)
                return found;
        }

        return null;
    }

    private void OnDisable()
    {
        SetTimelineHoverHighlight(false);
        ResetSelectionScaleImmediate();
    }

    private void OnDestroy()
    {
        ResetSelectionScaleImmediate();
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
