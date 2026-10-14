using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

/// <summary>
/// BattleSlot 오브젝트에 붙여서 타임라인의 TurnMark 5개와 Use_skill 25개를 한 번에 관리하는 UI 스프라이트 프레임 애니메이션 컨트롤러입니다.
/// 각 TurnMark/Use_skill 오브젝트에 개별 스크립트를 붙이지 않아도 됩니다.
/// </summary>
public class BattleTimelineSpriteAnimationController : MonoBehaviour
{
    private const int TimelineSlotCount = 5;
    private const int OrderCountPerSlot = 5;
    private const int TotalUseSkillCount = TimelineSlotCount * OrderCountPerSlot;

    [Header("자동 탐색")]
    [Tooltip("켜두면 Awake에서 BattleSlot 아래의 TimelineSlot01~05, TurnMark, Use_skill을 자동으로 찾습니다.")]
    [SerializeField] private bool autoFindOnAwake = true;

    [Tooltip("Play 함수 호출 시 비어 있는 참조가 있으면 다시 자동 탐색합니다.")]
    [SerializeField] private bool autoFindWhenMissing = true;

    [Header("TurnMark 대상 5개")]
    [Tooltip("TimelineSlot01~05의 TurnMark Image입니다. 비어 있으면 자동 탐색됩니다.")]
    [SerializeField] private Image[] turnMarkImages = new Image[TimelineSlotCount];

    [Header("Use_skill 대상 25개")]
    [Tooltip("TimelineSlot01~05 안의 Order01~05 Use_skill Image입니다. 순서는 Slot01 Order01~05, Slot02 Order01~05 순서입니다.")]
    [SerializeField] private Image[] useSkillImages = new Image[TotalUseSkillCount];

    [Header("오더 행동 애니메이션")]
    [SerializeField, Min(1f)] private float actionScale = 1.2f;
    [SerializeField, Min(0.01f)] private float scaleUpDuration = 0.12f;
    [SerializeField, Min(0.01f)] private float scaleDownDuration = 0.12f;
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.2f;
    [FormerlySerializedAs("moveLeftDistance")]
    [SerializeField, Min(0f)] private float moveUpDistance = 70f;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool playOnlyActiveImages = true;

    private readonly Dictionary<RectTransform, Vector3> originalScales = new Dictionary<RectTransform, Vector3>();
    private readonly Dictionary<RectTransform, Vector2> originalPositions = new Dictionary<RectTransform, Vector2>();
    private readonly Dictionary<RectTransform, CanvasGroup> fadeGroups = new Dictionary<RectTransform, CanvasGroup>();
    private readonly Dictionary<RectTransform, bool> addedFadeGroups = new Dictionary<RectTransform, bool>();
    private readonly Dictionary<Image, Coroutine> runningCoroutines = new Dictionary<Image, Coroutine>();
    private readonly Dictionary<Image, GameObject> imageHideRoots = new Dictionary<Image, GameObject>();
    private readonly Dictionary<Image, Sprite> originalSprites = new Dictionary<Image, Sprite>();
    private readonly Dictionary<GameObject, bool> hiddenChildOriginalActiveStates = new Dictionary<GameObject, bool>();

    private Transform animationRoot;

    private void Awake()
    {
        if (autoFindOnAwake)
            RefreshTargets();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (turnMarkImages == null || turnMarkImages.Length != TimelineSlotCount)
            System.Array.Resize(ref turnMarkImages, TimelineSlotCount);

        if (useSkillImages == null || useSkillImages.Length != TotalUseSkillCount)
            System.Array.Resize(ref useSkillImages, TotalUseSkillCount);

        if (moveUpDistance < 0f)
            moveUpDistance = 0f;

    }
#endif

    public void SetAnimationRoot(Transform root)
    {
        if (animationRoot == root)
            return;

        animationRoot = root;
        RefreshTargets();
    }

    [ContextMenu("Refresh Timeline Animation Targets")]
    public void RefreshTargets()
    {
        if (turnMarkImages == null || turnMarkImages.Length != TimelineSlotCount)
            turnMarkImages = new Image[TimelineSlotCount];

        if (useSkillImages == null || useSkillImages.Length != TotalUseSkillCount)
            useSkillImages = new Image[TotalUseSkillCount];


        imageHideRoots.Clear();

        for (int slot = 0; slot < TimelineSlotCount; slot++)
        {
            Transform timelineSlot = FindTimelineSlot(slot);

            if (timelineSlot == null)
                continue;

            Transform turnMark = FindChildRecursive(timelineSlot, "TurnMark");
            Image turnMarkImage = FindBestImage(turnMark);
            turnMarkImages[slot] = turnMarkImage;
            RegisterTarget(turnMarkImage, turnMark != null ? turnMark.gameObject : null);

            for (int order = 0; order < OrderCountPerSlot; order++)
            {
                Transform orderRoot = FindOrderRoot(timelineSlot, order);
                Transform useSkillRoot = FindChildRecursive(orderRoot, "Use_skill1");
                if (useSkillRoot == null) useSkillRoot = FindChildRecursive(orderRoot, "Use_skill");
                Image useSkillImage = FindBestUseSkillImage(useSkillRoot);
                useSkillImages[GetUseSkillIndex(slot, order)] = useSkillImage;
                RegisterTarget(useSkillImage, useSkillRoot != null ? useSkillRoot.gameObject : null);
            }
        }
    }

    public void PlayTurnMark(int slotIndex)
    {
        StartCoroutine(PlayTurnMarkRoutine(slotIndex));
    }

    [SerializeField, Min(0f)] private float turnMarkFadeDuration = 0.2f;
    private readonly Dictionary<int, Coroutine> turnMarkFadeCoroutines = new Dictionary<int, Coroutine>();
    private readonly HashSet<int> passedTurnMarks = new HashSet<int>();

    public void BeginPassedTurnMarkFade(int slotIndex)
    {
        EnsureTargetsIfNeeded();
        if (!IsValidSlotIndex(slotIndex) || !passedTurnMarks.Add(slotIndex))
            return;

        if (turnMarkFadeCoroutines.TryGetValue(slotIndex, out Coroutine old) && old != null)
            StopCoroutine(old);
        turnMarkFadeCoroutines[slotIndex] = StartCoroutine(FadePassedTurnMark(slotIndex));
    }

    public void CompletePassedTurnMarkFade(int slotIndex)
    {
        EnsureTargetsIfNeeded();
        if (!IsValidSlotIndex(slotIndex))
            return;
        passedTurnMarks.Add(slotIndex);
        if (turnMarkFadeCoroutines.TryGetValue(slotIndex, out Coroutine old) && old != null)
            StopCoroutine(old);
        turnMarkFadeCoroutines.Remove(slotIndex);
        CanvasGroup group = GetTurnMarkCanvasGroup(slotIndex);
        if (group != null)
            group.alpha = 0f;
    }

    private IEnumerator FadePassedTurnMark(int slotIndex)
    {
        CanvasGroup group = GetTurnMarkCanvasGroup(slotIndex);
        if (group == null)
            yield break;
        float startAlpha = group.alpha;
        float duration = Mathf.Max(0.001f, turnMarkFadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        group.alpha = 0f;
        turnMarkFadeCoroutines.Remove(slotIndex);
    }

    private CanvasGroup GetTurnMarkCanvasGroup(int slotIndex)
    {
        Transform slot = FindTimelineSlot(slotIndex);
        Transform mark = slot != null ? FindChildRecursive(slot, "TurnMark") : null;
        if (mark == null)
            return null;
        CanvasGroup group = mark.GetComponent<CanvasGroup>();
        if (group == null)
            group = mark.gameObject.AddComponent<CanvasGroup>();
        return group;
    }

    public IEnumerator PlayTurnMarkRoutine(int slotIndex)
    {
        BeginPassedTurnMarkFade(slotIndex);
        float duration = Mathf.Max(0f, turnMarkFadeDuration);
        if (duration > 0f)
        {
            if (useUnscaledTime) yield return new WaitForSecondsRealtime(duration);
            else yield return new WaitForSeconds(duration);
        }
        CompletePassedTurnMarkFade(slotIndex);
    }

    public void PlayUseSkill(int slotIndex, int orderIndex)
    {
        StartCoroutine(PlayUseSkillRoutine(slotIndex, orderIndex));
    }

    public IEnumerator PlayUseSkillRoutine(int slotIndex, int orderIndex)
    {
        EnsureTargetsIfNeeded();

        if (!IsValidSlotIndex(slotIndex) || !IsValidOrderIndex(orderIndex))
            yield break;

        yield return PlayUseSkillByLinearIndexRoutine(GetUseSkillIndex(slotIndex, orderIndex));
    }

    public IEnumerator PlayUseSkillsRoutine(int slotIndex, int startOrderIndex, int count)
    {
        EnsureTargetsIfNeeded();

        if (!IsValidSlotIndex(slotIndex))
            yield break;

        int safeStart = Mathf.Clamp(startOrderIndex, 0, OrderCountPerSlot);
        int safeCount = Mathf.Max(0, count);

        for (int i = 0; i < safeCount; i++)
        {
            int orderIndex = safeStart + i;

            if (!IsValidOrderIndex(orderIndex))
                yield break;

            yield return PlayUseSkillRoutine(slotIndex, orderIndex);
        }
    }

    public void PlayTimelineSlot(int slotIndex)
    {
        StartCoroutine(PlayTimelineSlotRoutine(slotIndex));
    }

    public IEnumerator PlayTimelineSlotRoutine(int slotIndex)
    {
        EnsureTargetsIfNeeded();

        if (!IsValidSlotIndex(slotIndex))
            yield break;

        yield return PlayTurnMarkRoutine(slotIndex);

        for (int order = 0; order < OrderCountPerSlot; order++)
            yield return PlayUseSkillRoutine(slotIndex, order);
    }

    [ContextMenu("Play All Timeline Sprite Animations")]
    public void PlayAll()
    {
        StartCoroutine(PlayAllRoutine());
    }

    public IEnumerator PlayAllRoutine()
    {
        EnsureTargetsIfNeeded();

        for (int slot = 0; slot < TimelineSlotCount; slot++)
            yield return PlayTurnMarkRoutine(slot);

        for (int i = 0; i < TotalUseSkillCount; i++)
            yield return PlayUseSkillByLinearIndexRoutine(i);
    }


    public float GetTurnMarkAnimationDuration() => turnMarkFadeDuration;
    public float GetUseSkillAnimationDuration() => scaleUpDuration + scaleDownDuration + fadeOutDuration;
    public float GetFrameAnimationDuration(Sprite[] frames) => 0f;

    public void StopAllAnimations(bool resetPositions)
    {
        List<Image> keys = new List<Image>(runningCoroutines.Keys);

        for (int i = 0; i < keys.Count; i++)
        {
            Image image = keys[i];

            if (image == null)
                continue;

            Coroutine coroutine = runningCoroutines[image];

            if (coroutine != null)
                StopCoroutine(coroutine);
        }

        runningCoroutines.Clear();

        if (resetPositions)
        {
            ResetAllTargetPositions();
            RestoreAllHiddenChildren();
        }
    }

    public void ResetAllTargetPositions()
    {
        foreach (var entry in originalScales)
            if (entry.Key != null) entry.Key.localScale = entry.Value;
        foreach (var entry in originalPositions)
            if (entry.Key != null) entry.Key.anchoredPosition = entry.Value;
        foreach (var entry in fadeGroups)
            if (entry.Value != null) entry.Value.alpha = 1f;
    }

    public void ResetTurnMarksForNextTurn() => ResetTimelineSpritesForNextTurn();

    public void ResetTimelineSpritesForNextTurn()
    {
        StopAllAnimations(true);
        foreach (Coroutine routine in turnMarkFadeCoroutines.Values)
            if (routine != null) StopCoroutine(routine);
        turnMarkFadeCoroutines.Clear();
        passedTurnMarks.Clear();
        for (int i = 0; i < TimelineSlotCount; i++)
        {
            CanvasGroup marker = GetTurnMarkCanvasGroup(i);
            if (marker != null) marker.alpha = 1f;
        }
    }

    private IEnumerator PlayUseSkillByLinearIndexRoutine(int index)
    {
        if (useSkillImages == null || index < 0 || index >= useSkillImages.Length)
            yield break;

        Image target = useSkillImages[index];
        int slot = index / OrderCountPerSlot;
        int order = index % OrderCountPerSlot;
        Transform slotRoot = FindTimelineSlot(slot);
        Transform orderRoot = FindOrderRoot(slotRoot, order);
        if (orderRoot != null)
        {
            for (int size = 1; size <= 3; size++)
            {
                Transform root = orderRoot.Find("Use_skill" + size);
                if (root == null || !root.gameObject.activeInHierarchy) continue;
                Image candidate = FindBestUseSkillImage(root);
                if (candidate != null)
                {
                    target = candidate;
                    RegisterTarget(target, root.gameObject);
                    break;
                }
            }
        }
        yield return PlayImageRoutineSafe(target);
    }

    private IEnumerator PlayImageRoutineSafe(Image image)
    {
        if (image == null) yield break;
        GameObject root = GetHideRoot(image);
        if (root == null || (playOnlyActiveImages && !root.activeInHierarchy)) yield break;
        RectTransform rect = root.GetComponent<RectTransform>();
        if (rect == null) yield break;

        if (runningCoroutines.TryGetValue(image, out Coroutine previous) && previous != null)
            StopCoroutine(previous);

        if (!originalScales.ContainsKey(rect)) originalScales[rect] = rect.localScale;
        if (!originalPositions.ContainsKey(rect)) originalPositions[rect] = rect.anchoredPosition;
        if (!fadeGroups.TryGetValue(rect, out CanvasGroup group) || group == null)
        {
            group = root.GetComponent<CanvasGroup>();
            bool added = group == null;
            if (added) group = root.AddComponent<CanvasGroup>();
            fadeGroups[rect] = group;
            addedFadeGroups[rect] = added;
        }
        rect.localScale = originalScales[rect];
        rect.anchoredPosition = originalPositions[rect];
        group.alpha = 1f;
        Coroutine running = StartCoroutine(PlayOrderAnimation(rect, group));
        runningCoroutines[image] = running;
        yield return running;
        runningCoroutines.Remove(image);
    }

    private IEnumerator PlayOrderAnimation(RectTransform rect, CanvasGroup group)
    {
        Vector3 baseScale = originalScales[rect];
        Vector2 basePosition = originalPositions[rect];
        yield return AnimateScale(rect, baseScale, baseScale * actionScale, scaleUpDuration);
        yield return AnimateScale(rect, baseScale * actionScale, baseScale, scaleDownDuration);
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, fadeOutDuration));
            rect.anchoredPosition = basePosition + Vector2.up * (moveUpDistance * t);
            group.alpha = 1f - t;
            yield return null;
        }
        rect.anchoredPosition = basePosition + Vector2.up * moveUpDistance;
        group.alpha = 0f;
    }

    private IEnumerator AnimateScale(RectTransform rect, Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            rect.localScale = Vector3.LerpUnclamped(from, to, t);
            yield return null;
        }
        rect.localScale = to;
    }

    private void HideChildrenForAnimation(GameObject root)
    {
        if (root == null)
            return;

        Transform rootTransform = root.transform;

        for (int i = 0; i < rootTransform.childCount; i++)
            HideChildRecursive(rootTransform.GetChild(i));
    }

    private void HideChildRecursive(Transform child)
    {
        if (child == null)
            return;

        GameObject childObject = child.gameObject;

        if (!hiddenChildOriginalActiveStates.ContainsKey(childObject))
            hiddenChildOriginalActiveStates.Add(childObject, childObject.activeSelf);

        childObject.SetActive(false);
    }

    private void RestoreHiddenChildren(GameObject root)
    {
        if (root == null || hiddenChildOriginalActiveStates.Count <= 0)
            return;

        List<GameObject> restoredObjects = new List<GameObject>();

        foreach (KeyValuePair<GameObject, bool> pair in hiddenChildOriginalActiveStates)
        {
            GameObject childObject = pair.Key;

            if (childObject == null)
            {
                restoredObjects.Add(childObject);
                continue;
            }

            if (!childObject.transform.IsChildOf(root.transform))
                continue;

            childObject.SetActive(pair.Value);
            restoredObjects.Add(childObject);
        }

        for (int i = 0; i < restoredObjects.Count; i++)
            hiddenChildOriginalActiveStates.Remove(restoredObjects[i]);
    }

    private void RestoreAllHiddenChildren()
    {
        if (hiddenChildOriginalActiveStates.Count <= 0)
            return;

        List<GameObject> keys = new List<GameObject>(hiddenChildOriginalActiveStates.Keys);

        for (int i = 0; i < keys.Count; i++)
        {
            GameObject childObject = keys[i];

            if (childObject != null)
                childObject.SetActive(hiddenChildOriginalActiveStates[childObject]);
        }

        hiddenChildOriginalActiveStates.Clear();
    }

    private void RegisterTarget(Image image, GameObject hideRoot)
    {
        if (image == null)
            return;

        GameObject safeRoot = hideRoot != null ? hideRoot : image.gameObject;
        imageHideRoots[image] = safeRoot;

        if (!originalSprites.ContainsKey(image))
            originalSprites.Add(image, image.sprite);
    }

    private GameObject GetHideRoot(Image image)
    {
        if (image == null)
            return null;

        if (imageHideRoots.TryGetValue(image, out GameObject root) && root != null)
            return root;

        RegisterTarget(image, image.gameObject);
        return imageHideRoots.TryGetValue(image, out root) ? root : image.gameObject;
    }

    private void EnsureTargetsIfNeeded()
    {
        if (!autoFindWhenMissing)
            return;

        if (HasMissingTargets())
            RefreshTargets();
    }

    private bool HasMissingTargets()
    {
        if (turnMarkImages == null || turnMarkImages.Length != TimelineSlotCount)
            return true;

        if (useSkillImages == null || useSkillImages.Length != TotalUseSkillCount)
            return true;

        for (int i = 0; i < turnMarkImages.Length; i++)
        {
            if (turnMarkImages[i] == null)
                return true;
        }

        for (int i = 0; i < useSkillImages.Length; i++)
        {
            if (useSkillImages[i] == null)
                return true;
        }

        return false;
    }

    private Transform FindTimelineSlot(int slotIndex)
    {
        int slotNumber = slotIndex + 1;
        Transform searchRoot = animationRoot != null ? animationRoot : transform;
        Transform found = FindChildRecursive(searchRoot, "TimelineSlot" + slotNumber.ToString("00"));

        if (found == null)
            found = FindChildRecursive(searchRoot, "TimelineSlot" + slotNumber);

        return found;
    }

    private Transform FindOrderRoot(Transform timelineSlot, int orderIndex)
    {
        if (timelineSlot == null)
            return null;

        int orderNumber = orderIndex + 1;
        Transform found = FindChildRecursive(timelineSlot, "Order" + orderNumber.ToString("00"));

        if (found == null)
            found = FindChildRecursive(timelineSlot, "Order" + orderNumber);

        return found;
    }

    private Image FindBestUseSkillImage(Transform useSkillRoot)
    {
        if (useSkillRoot == null)
            return null;

        // Use_skill 자식의 Skill_Image는 실제 스킬 아이콘 표시용입니다.
        // 갈리는 프레임 애니메이션은 Use_skill 본인이 가진 Image 컴포넌트에 적용해야 합니다.
        Image rootImage = useSkillRoot.GetComponent<Image>();

        if (rootImage != null)
            return rootImage;

        return FindBestImage(useSkillRoot);
    }

    private Image FindBestImage(Transform root)
    {
        if (root == null)
            return null;

        Image image = root.GetComponent<Image>();

        if (image != null)
            return image;

        return root.GetComponentInChildren<Image>(true);
    }

    private Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child.name == childName)
                return child;

            Transform found = FindChildRecursive(child, childName);

            if (found != null)
                return found;
        }

        return null;
    }

    private int GetUseSkillIndex(int slotIndex, int orderIndex)
    {
        return (slotIndex * OrderCountPerSlot) + orderIndex;
    }

    private bool IsValidSlotIndex(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < TimelineSlotCount;
    }

    private bool IsValidOrderIndex(int orderIndex)
    {
        return orderIndex >= 0 && orderIndex < OrderCountPerSlot;
    }
}
