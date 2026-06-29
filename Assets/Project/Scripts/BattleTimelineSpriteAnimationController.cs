using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("TurnMark 프레임")]
    [Tooltip("TurnMark가 톱니에 갈릴 때 순서대로 교체할 스프라이트 프레임입니다.")]
    [SerializeField] private Sprite[] turnMarkFrames;

    [Header("Use_skill 프레임")]
    [Tooltip("Use_skill이 톱니에 갈릴 때 순서대로 교체할 스프라이트 프레임입니다.")]
    [SerializeField] private Sprite[] useSkillFrames;

    [Header("재생 설정")]
    [Tooltip("프레임 하나가 유지되는 시간입니다.")]
    [SerializeField] private float secondsPerFrame = 0.08f;

    [Tooltip("이전 버전 호환용 값입니다. 현재 TurnMark/Use_skill 개별 이동은 하지 않고 TimelineBar 전체 이동은 BattleTimelineController가 처리합니다.")]
    [SerializeField] private float moveLeftDistance = 70f;

    [Tooltip("Time.timeScale의 영향을 받지 않고 UI 애니메이션을 재생합니다.")]
    [SerializeField] private bool useUnscaledTime = true;

    [Tooltip("비활성화된 Use_skill은 재생하지 않습니다. 실제 등록된 행동만 갈리는 연출을 줄 때 켜두는 것을 권장합니다.")]
    [SerializeField] private bool playOnlyActiveImages = true;

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

        if (secondsPerFrame < 0.001f)
            secondsPerFrame = 0.001f;

        if (moveLeftDistance < 0f)
            moveLeftDistance = 0f;

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
                Transform useSkillRoot = FindChildRecursive(orderRoot, "Use_skill");
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

    public IEnumerator PlayTurnMarkRoutine(int slotIndex)
    {
        EnsureTargetsIfNeeded();

        if (!IsValidSlotIndex(slotIndex))
            yield break;

        yield return PlayImageRoutineSafe(turnMarkImages[slotIndex], turnMarkFrames);
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


    public float GetTurnMarkAnimationDuration()
    {
        return GetFrameAnimationDuration(turnMarkFrames);
    }

    public float GetUseSkillAnimationDuration()
    {
        return GetFrameAnimationDuration(useSkillFrames);
    }

    public float GetFrameAnimationDuration(Sprite[] frames)
    {
        if (frames == null || frames.Length <= 0)
            return 0f;

        return Mathf.Max(0.001f, secondsPerFrame) * Mathf.Max(1, frames.Length);
    }

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
        // TurnMark와 Use_skill 개별 오브젝트는 더 이상 이동하지 않습니다.
        // 전체 라인 이동과 원위치 복구는 BattleTimelineController가 TimelineBar RectTransform으로 처리합니다.
    }

    public void ResetTurnMarksForNextTurn()
    {
        ResetTimelineSpritesForNextTurn();
    }

    public void ResetTimelineSpritesForNextTurn()
    {
        EnsureTargetsIfNeeded();
        ResetAllTargetPositions();
        RestoreAllHiddenChildren();

        ResetImageArrayToOriginalSprites(turnMarkImages);
        ResetImageArrayToOriginalSprites(useSkillImages);
    }

    private void ResetImageArrayToOriginalSprites(Image[] images)
    {
        if (images == null)
            return;

        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];

            if (image == null)
                continue;

            GameObject root = GetHideRoot(image);

            if (root != null)
                root.SetActive(true);

            RestoreHiddenChildren(root);
            RestoreOriginalSprite(image);
            image.enabled = true;
        }
    }

    private IEnumerator PlayUseSkillByLinearIndexRoutine(int index)
    {
        if (useSkillImages == null || index < 0 || index >= useSkillImages.Length)
            yield break;

        yield return PlayImageRoutineSafe(useSkillImages[index], useSkillFrames);
    }

    private IEnumerator PlayImageRoutineSafe(Image image, Sprite[] frames)
    {
        if (image == null || frames == null || frames.Length <= 0)
            yield break;

        GameObject hideRoot = GetHideRoot(image);

        if (playOnlyActiveImages && hideRoot != null && !hideRoot.activeInHierarchy)
            yield break;

        if (playOnlyActiveImages && hideRoot == null && !image.gameObject.activeInHierarchy)
            yield break;

        if (runningCoroutines.TryGetValue(image, out Coroutine oldCoroutine) && oldCoroutine != null)
            StopCoroutine(oldCoroutine);

        HideChildrenForAnimation(hideRoot);

        Coroutine newCoroutine = StartCoroutine(PlayImageRoutine(image, frames));
        runningCoroutines[image] = newCoroutine;
        yield return newCoroutine;
    }

    private IEnumerator PlayImageRoutine(Image image, Sprite[] frames)
    {
        if (image == null)
            yield break;

        int frameCount = Mathf.Max(1, frames.Length);
        float totalDuration = Mathf.Max(0.001f, secondsPerFrame * frameCount);
        float elapsed = 0f;
        int lastFrameIndex = -1;

        image.enabled = true;

        if (frames[0] != null)
            image.sprite = frames[0];

        while (elapsed < totalDuration)
        {
            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            elapsed += deltaTime;

            float t = Mathf.Clamp01(elapsed / totalDuration);
            int frameIndex = Mathf.Clamp(Mathf.FloorToInt(t * frameCount), 0, frameCount - 1);

            if (frameIndex != lastFrameIndex)
            {
                if (frames[frameIndex] != null)
                    image.sprite = frames[frameIndex];

                lastFrameIndex = frameIndex;
            }

            yield return null;
        }

        if (frames[frameCount - 1] != null)
            image.sprite = frames[frameCount - 1];

        if (image != null)
            runningCoroutines.Remove(image);
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

    private void RestoreOriginalSprite(Image image)
    {
        if (image == null)
            return;

        if (originalSprites.TryGetValue(image, out Sprite originalSprite) && originalSprite != null)
            image.sprite = originalSprite;
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
