using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;

public class SkillListPanel : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform panelRect;

    [Header("Content")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private SkillListSlotUI skillSlotPrefab;

    [Header("Detail")]
    [SerializeField] private GameObject detailsBackground;
    [SerializeField] private TMP_Text detailsText;

    [Header("Timeline")]
    [SerializeField] private BattleTimelineController battleTimelineController;

    [Header("Position")]
    [SerializeField] private Vector2 offsetFromHud = new Vector2(220f, 0f);

    private CharacterRuntimeData currentRuntime;

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        if (panelRect == null)
            panelRect = GetComponent<RectTransform>();

        if (battleTimelineController == null)
            battleTimelineController = FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);

        HideSkillDetail();
        Close();
    }

    public void Open(CharacterRuntimeData runtimeData)
    {
        Open(runtimeData, null);
    }

    public void Open(CharacterRuntimeData runtimeData, RectTransform hudRect)
    {
        currentRuntime = runtimeData;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (battleTimelineController == null)
            battleTimelineController = FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);

        if (battleTimelineController != null)
            battleTimelineController.SelectCharacter(currentRuntime);

        PositionToHud(hudRect);
        Refresh();
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        currentRuntime = null;
        Clear();
        HideSkillDetail();
    }

    public void Refresh()
    {
        Clear();

        if (currentRuntime == null)
            return;

        AddSkillSlot(currentRuntime.PassiveSkillId, false);
        AddSkillSlot(currentRuntime.MoveSkillId, true);
        AddSkillSlot(currentRuntime.AbilitySkillId1, true);
        AddSkillSlot(currentRuntime.AbilitySkillId2, true);
        AddSkillSlot(currentRuntime.UniqueSkillId, true);
    }

    private void AddSkillSlot(string skillId, bool interactable)
    {
        SkillListSlotUI slot = Instantiate(skillSlotPrefab, contentRoot);
        slot.Setup(this, skillId, interactable);
    }

    public void SelectSkill(string skillId)
    {
        if (currentRuntime == null)
        {
            Debug.LogWarning("[SkillListPanel] 선택된 캐릭터가 없습니다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(skillId))
            return;

        if (DataManager.Instance == null || DataManager.Instance.SkillDatabase == null)
        {
            Debug.LogWarning("[SkillListPanel] SkillDatabase가 없습니다.");
            return;
        }

        SkillMasterData skillData = DataManager.Instance.SkillDatabase.Get(skillId);

        if (skillData == null)
        {
            Debug.LogWarning($"[SkillListPanel] SkillData 없음: {skillId}");
            return;
        }

        if (battleTimelineController == null)
            battleTimelineController = FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);

        if (battleTimelineController == null)
        {
            Debug.LogWarning("[SkillListPanel] BattleTimelineController가 없습니다.");
            return;
        }

        battleTimelineController.SelectCharacter(currentRuntime);
        battleTimelineController.SelectSkill(skillData);

        Debug.Log($"[SkillListPanel] Skill Selected: {currentRuntime.CharacterId} / {skillId}");
    }

    private void PositionToHud(RectTransform hudRect)
    {
        if (panelRect == null || hudRect == null)
            return;

        RectTransform parentRect = panelRect.parent as RectTransform;

        if (parentRect == null)
            return;

        Canvas canvas = panelRect.GetComponentInParent<Canvas>();
        Camera uiCamera = null;

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, hudRect.position);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                screenPoint,
                uiCamera,
                out Vector2 localPoint))
        {
            return;
        }

        panelRect.anchoredPosition = localPoint + offsetFromHud;
    }

    public void ShowSkillDetail(string text)
    {
        if (detailsBackground != null)
            detailsBackground.SetActive(true);

        if (detailsText != null)
            detailsText.text = text;
    }

    public void HideSkillDetail()
    {
        if (detailsBackground != null)
            detailsBackground.SetActive(false);

        if (detailsText != null)
            detailsText.text = "";
    }

    private void Clear()
    {
        if (contentRoot == null)
            return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);
    }
}