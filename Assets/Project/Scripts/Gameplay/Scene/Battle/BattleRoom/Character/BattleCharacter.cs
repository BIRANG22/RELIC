using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.EventSystems;

public class BattleCharacter : MonoBehaviour
{
    private const string TimelineHoverHighlightIdleBackName = "Idle_Back";
    private const string TimelineHoverHighlightShadowName = "Shadow";

    [Header("Timeline Hover Highlight")]
    [SerializeField] private GameObject timelineHoverHighlightObject;

    private SpriteRenderer[] timelineHoverHighlightRenderers;
    private float[] timelineHoverHighlightOriginalAlphas;
    private SpriteRenderer timelineHoverHighlightSourceRenderer;
    private SpriteRenderer[] timelineHoverHighlightForegroundRenderers;
    private SpriteRenderer[] timelineHoverHighlightIdleBackRenderers;
    private SpriteRenderer[] timelineHoverHighlightShadowRenderers;
    private Animator timelineHoverHighlightSourceAnimator;
    private Animator timelineHoverHighlightIdleBackAnimator;
    private bool timelineHoverHighlightVisible;

    public CharacterRuntimeData RuntimeData { get; private set; }

    private readonly List<SkillMasterData> equippedSkills = new();
    public IReadOnlyList<SkillMasterData> EquippedSkills => equippedSkills;

    public string CharacterId => RuntimeData != null ? RuntimeData.CharacterId : null;

    public int CurrentGridIndex { get; private set; } = -1;


    private void Awake()
    {
        SetTimelineHoverHighlight(false);
    }

    public void Initialize(CharacterRuntimeData runtimeData)
    {
        RuntimeData = runtimeData;
        LoadEquippedSkills();

        SetTimelineHoverHighlight(false);
    }

    public void SetGridIndex(int gridIndex)
    {
        CurrentGridIndex = gridIndex;
    }

    public void SetTimelineHoverHighlight(bool active)
    {
        SetTimelineHoverHighlightAlpha(active);
    }


    public void SetSelectionScaleFeedback(bool selected)
    {
        SetTimelineHoverHighlight(selected);
    }

    private void LateUpdate()
    {
        SyncTimelineHoverHighlightAnimation();
    }

    private void OnDisable()
    {
        SetTimelineHoverHighlight(false);
    }

    private void OnDestroy()
    {
        SetTimelineHoverHighlight(false);
    }

    private void OnMouseDown()
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        if (RuntimeData == null)
            return;

        if (RuntimeData.IsDead)
            return;

        if (!SteamBattleStateSynchronizer.CanLocalPlayerControlCharacter(RuntimeData.CharacterId))
        {
            BattleWarningUI.ShowMessage("다른 플레이어의 캐릭터입니다.");
            return;
        }

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

    private void SetTimelineHoverHighlightAlpha(bool active)
    {
        if (IsTimelineHoverHighlightBlockedByDeath())
        {
            ForceTimelineHoverHighlightOff();
            return;
        }

        timelineHoverHighlightVisible = active;

        if (timelineHoverHighlightObject == null)
            return;

        if (!timelineHoverHighlightObject.activeSelf)
            timelineHoverHighlightObject.SetActive(true);

        SyncTimelineHoverHighlightAnimation();
        ApplyTimelineHoverHighlightAlpha();
    }

    private void SyncTimelineHoverHighlightAnimation()
    {
        if (IsTimelineHoverHighlightBlockedByDeath())
        {
            ForceTimelineHoverHighlightOff();
            return;
        }

        if (timelineHoverHighlightObject == null)
            return;

        if (!timelineHoverHighlightObject.activeSelf)
            timelineHoverHighlightObject.SetActive(true);

        CacheTimelineHoverHighlightRenderers();
        CacheTimelineHoverHighlightSourceRenderer();
        CacheTimelineHoverHighlightSourceAnimator();
        CacheTimelineHoverHighlightIdleBackAnimator();
        CacheTimelineHoverHighlightForegroundRenderers();
        CacheTimelineHoverHighlightIdleBackRenderers();
        CacheTimelineHoverHighlightShadowRenderers();
        SyncTimelineHoverHighlightSorting();

        if (timelineHoverHighlightSourceAnimator == null ||
            timelineHoverHighlightIdleBackAnimator == null)
        {
            ApplyTimelineHoverHighlightAlpha();
            return;
        }

        AnimatorStateInfo sourceState = timelineHoverHighlightSourceAnimator.GetCurrentAnimatorStateInfo(0);
        int idleBackStateHash = GetTimelineHoverHighlightIdleBackStateHash();

        if (sourceState.shortNameHash == 0 || idleBackStateHash == 0)
        {
            ApplyTimelineHoverHighlightAlpha();
            return;
        }

        float normalizedTime = sourceState.normalizedTime % 1f;
        if (normalizedTime < 0f)
            normalizedTime += 1f;

        PauseTimelineHoverHighlightIdleBackAnimator();
        timelineHoverHighlightIdleBackAnimator.Play(idleBackStateHash, 0, normalizedTime);
        timelineHoverHighlightIdleBackAnimator.Update(0f);
        ApplyTimelineHoverHighlightAlpha();
    }

    private void ApplyTimelineHoverHighlightAlpha()
    {
        if (timelineHoverHighlightRenderers == null)
            return;

        for (int i = 0; i < timelineHoverHighlightRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = timelineHoverHighlightRenderers[i];

            if (spriteRenderer == null)
                continue;

            Color color = spriteRenderer.color;
            color.a = timelineHoverHighlightVisible ? timelineHoverHighlightOriginalAlphas[i] : 0f;
            spriteRenderer.color = color;
        }
    }

    private bool IsTimelineHoverHighlightBlockedByDeath()
    {
        return RuntimeData != null && RuntimeData.IsDead;
    }

    private void ForceTimelineHoverHighlightOff()
    {
        timelineHoverHighlightVisible = false;

        if (timelineHoverHighlightObject == null)
            return;

        CacheTimelineHoverHighlightRenderers();
        ApplyTimelineHoverHighlightAlpha();

        if (timelineHoverHighlightObject.activeSelf)
            timelineHoverHighlightObject.SetActive(false);
    }

    private void CacheTimelineHoverHighlightRenderers()
    {
        if (timelineHoverHighlightRenderers != null)
            return;

        timelineHoverHighlightRenderers =
            timelineHoverHighlightObject.GetComponentsInChildren<SpriteRenderer>(true);
        timelineHoverHighlightOriginalAlphas = new float[timelineHoverHighlightRenderers.Length];

        for (int i = 0; i < timelineHoverHighlightRenderers.Length; i++)
        {
            timelineHoverHighlightOriginalAlphas[i] =
                timelineHoverHighlightRenderers[i] != null
                    ? timelineHoverHighlightRenderers[i].color.a
                    : 1f;
        }
    }

    private void PauseTimelineHoverHighlightIdleBackAnimator()
    {
        if (timelineHoverHighlightIdleBackAnimator == null)
            return;

        if (!timelineHoverHighlightIdleBackAnimator.enabled)
            timelineHoverHighlightIdleBackAnimator.enabled = true;

        timelineHoverHighlightIdleBackAnimator.speed = 0f;
    }

    private void SyncTimelineHoverHighlightSorting()
    {
        if (timelineHoverHighlightSourceRenderer == null)
            return;

        int sortingLayerId = timelineHoverHighlightSourceRenderer.sortingLayerID;
        int sourceSortingOrder = GetTimelineHoverHighlightSourceSortingOrder();

        ApplyTimelineHoverHighlightSorting(
            timelineHoverHighlightForegroundRenderers,
            sortingLayerId,
            sourceSortingOrder - 1);

        ApplyTimelineHoverHighlightSorting(
            timelineHoverHighlightIdleBackRenderers,
            sortingLayerId,
            sourceSortingOrder - 1);

        ApplyTimelineHoverHighlightSorting(
            timelineHoverHighlightShadowRenderers,
            sortingLayerId,
            sourceSortingOrder - 2);
    }

    private static void ApplyTimelineHoverHighlightSorting(
        SpriteRenderer[] renderers,
        int sortingLayerId,
        int sortingOrder)
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];

            if (renderer == null)
                continue;

            renderer.sortingLayerID = sortingLayerId;
            renderer.sortingOrder = sortingOrder;
        }
    }

    private int GetTimelineHoverHighlightSourceSortingOrder()
    {
        YSortSprite ySortSprite = timelineHoverHighlightSourceRenderer.GetComponent<YSortSprite>();

        if (ySortSprite != null)
            return (int)(-ySortSprite.transform.position.y * ySortSprite.yMultiplier) +
                   ySortSprite.sortingOrderOffset;

        return timelineHoverHighlightSourceRenderer.sortingOrder;
    }

    private void CacheTimelineHoverHighlightSourceRenderer()
    {
        if (timelineHoverHighlightSourceRenderer != null)
            return;

        Transform spriteRoot = transform.Find("SpriteRoot");

        if (spriteRoot != null)
            timelineHoverHighlightSourceRenderer = spriteRoot.GetComponentInChildren<SpriteRenderer>(true);

        if (timelineHoverHighlightSourceRenderer != null)
            return;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];

            if (renderer == null)
                continue;

            if (IsTimelineHoverHighlightChild(renderer.transform))
                continue;

            timelineHoverHighlightSourceRenderer = renderer;
            return;
        }
    }

    private void CacheTimelineHoverHighlightSourceAnimator()
    {
        if (timelineHoverHighlightSourceAnimator != null)
            return;

        Transform spriteRoot = transform.Find("SpriteRoot");

        if (spriteRoot != null)
            timelineHoverHighlightSourceAnimator = spriteRoot.GetComponentInChildren<Animator>(true);

        if (timelineHoverHighlightSourceAnimator != null)
            return;

        Animator[] animators = GetComponentsInChildren<Animator>(true);

        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];

            if (animator == null)
                continue;

            if (IsTimelineHoverHighlightChild(animator.transform))
                continue;

            timelineHoverHighlightSourceAnimator = animator;
            return;
        }
    }

    private void CacheTimelineHoverHighlightIdleBackAnimator()
    {
        if (timelineHoverHighlightIdleBackAnimator != null)
            return;

        Transform idleBack = FindTimelineHoverHighlightIdleBackTransform();

        if (idleBack == null)
            return;

        timelineHoverHighlightIdleBackAnimator = idleBack.GetComponent<Animator>();

        if (timelineHoverHighlightIdleBackAnimator == null)
            timelineHoverHighlightIdleBackAnimator = idleBack.GetComponentInChildren<Animator>(true);
    }

    private void CacheTimelineHoverHighlightForegroundRenderers()
    {
        if (timelineHoverHighlightForegroundRenderers != null)
            return;

        Transform idleBack = FindTimelineHoverHighlightIdleBackTransform();
        List<SpriteRenderer> foregroundRenderers = new();
        SpriteRenderer[] renderers = timelineHoverHighlightObject.GetComponentsInChildren<SpriteRenderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];

            if (renderer == null)
                continue;

            if (idleBack != null && IsTransformChildOf(renderer.transform, idleBack))
                continue;

            foregroundRenderers.Add(renderer);
        }

        timelineHoverHighlightForegroundRenderers = foregroundRenderers.ToArray();
    }

    private void CacheTimelineHoverHighlightIdleBackRenderers()
    {
        if (timelineHoverHighlightIdleBackRenderers != null)
            return;

        Transform idleBack = FindTimelineHoverHighlightIdleBackTransform();

        if (idleBack == null)
            return;

        timelineHoverHighlightIdleBackRenderers = idleBack.GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void CacheTimelineHoverHighlightShadowRenderers()
    {
        if (timelineHoverHighlightShadowRenderers != null)
            return;

        Transform shadow = transform.Find(TimelineHoverHighlightShadowName);

        if (shadow == null)
            shadow = FindChildRecursiveExcludingTimelineHoverHighlight(
                transform,
                TimelineHoverHighlightShadowName);

        timelineHoverHighlightShadowRenderers =
            shadow != null
                ? shadow.GetComponentsInChildren<SpriteRenderer>(true)
                : System.Array.Empty<SpriteRenderer>();
    }

    private int GetTimelineHoverHighlightIdleBackStateHash()
    {
        if (timelineHoverHighlightIdleBackAnimator == null)
            return 0;

        AnimatorStateInfo currentState =
            timelineHoverHighlightIdleBackAnimator.GetCurrentAnimatorStateInfo(0);

        if (currentState.shortNameHash != 0)
            return currentState.shortNameHash;

        return Animator.StringToHash(TimelineHoverHighlightIdleBackName);
    }

    private Transform FindTimelineHoverHighlightIdleBackTransform()
    {
        Transform idleBack = FindChildRecursive(
            timelineHoverHighlightObject.transform,
            TimelineHoverHighlightIdleBackName);

        if (idleBack != null)
            return idleBack;

        return FindChildRecursive(
            timelineHoverHighlightObject.transform,
            "Idle_back");
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (string.Equals(child.name, childName, System.StringComparison.OrdinalIgnoreCase))
                return child;

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private Transform FindChildRecursiveExcludingTimelineHoverHighlight(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (IsTimelineHoverHighlightChild(child))
                continue;

            if (string.Equals(child.name, childName, System.StringComparison.OrdinalIgnoreCase))
                return child;

            Transform nested = FindChildRecursiveExcludingTimelineHoverHighlight(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private static bool IsTransformChildOf(Transform target, Transform root)
    {
        if (target == null || root == null)
            return false;

        Transform current = target;

        while (current != null)
        {
            if (current == root)
                return true;

            current = current.parent;
        }

        return false;
    }

    private bool IsTimelineHoverHighlightChild(Transform target)
    {
        if (target == null || timelineHoverHighlightObject == null)
            return false;

        Transform current = target;

        while (current != null)
        {
            if (current.gameObject == timelineHoverHighlightObject)
                return true;

            current = current.parent;
        }

        return false;
    }
}
