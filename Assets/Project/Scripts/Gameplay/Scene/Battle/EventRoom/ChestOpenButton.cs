using System;
using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public class ChestOpenButton : MonoBehaviour
{
    public enum ChestGrade
    {
        Grade1 = 1,
        Grade2 = 2,
        Grade3 = 3,
        Grade4 = 4
    }

    public event Action Opened;
    public event Action<string> RewardPointerEntered;
    public event Action RewardPointerExited;
    public event Action<string> RewardClaimed;

    public bool IsOpened => isOpened;
    public bool IsRewardGranted => isRewardGranted;
    public bool IsAwaitingRewardSelection => isOpened && hasSelectedReward && !isRewardGranted;

    [Header("상자 등급")]
    [SerializeField] private ChestGrade chestGrade = ChestGrade.Grade1;

    [Header("보상 유물 스폰")]
    [Tooltip("체크하면 DataManager의 유물 데이터에서 Common~Unique 유물 하나를 첫 클릭 때 랜덤 선택합니다.")]
    [SerializeField] private bool useRandomRelicReward = true;

    [Tooltip("유물 아이콘을 생성할 부모입니다. 비워두면 상자 Transform을 사용합니다.")]
    [SerializeField] private Transform rewardItemSpawnRoot;

    [SerializeField] private Vector3 rewardItemLocalPosition = new(2.202f, 0.177f, 0f);
    [SerializeField] private Vector3 rewardItemLocalScale = new(0.6f, 0.6f, 0.6f);
    [SerializeField] private string rewardSortingLayerName = "Unit";
    [SerializeField] private int rewardSortingOrder = 1;

    [Header("상자 Y-sort 보정")]
    [SerializeField] private int chestUnderSortingOrderOffset = 0;
    [SerializeField] private int chestOpenSortingOrderOffset = 3;

    [Header("상자 스프라이트")]
    [SerializeField] private SpriteRenderer chestUnder;
    [SerializeField] private SpriteRenderer chestOpen;

    [Header("열림 애니메이션 16프레임")]
    [SerializeField] private Sprite[] chestOpenFrames;

    [Header("등급별 클릭 VFX")]
    [Tooltip("0번=Common, 1번=Uncommon, 2번=Rare, 3번=Unique 클릭 VFX 프리팹")]
    [SerializeField] private GameObject[] stepVfxList;

    [Header("완전 열림 VFX")]
    [Tooltip("상자가 완전히 열릴 때 1회 재생할 VFX 프리팹입니다.")]
    [SerializeField] private GameObject openCompleteVfx;

    [Tooltip("마지막 클릭에서 덜컹 VFX가 나온 뒤 Open Complete VFX가 나오기까지의 지연 시간입니다.")]
    [SerializeField] private float openCompleteVfxDelay = 0.12f;

    [Header("VFX 런타임 스폰")]
    [SerializeField] private Transform vfxSpawnRoot;
    [SerializeField] private Vector3 stepVfxLocalPosition = new(1.79f, 0f, 0f);
    [SerializeField] private Vector3 openCompleteVfxLocalPosition = new(2.247f, 0.148f, 0f);
    [SerializeField] private string vfxLayerName = "VFX";
    [SerializeField] private string vfxSortingLayerName = "Empty";
    [SerializeField] private int vfxSortingOrder = 2;
    [SerializeField] private float vfxAutoDestroyDelay = 3f;

    [Header("VFX 개별 월드 프록시")]
    [SerializeField] private bool useIndividualWorldVfxProxy = true;
    [Min(1)][SerializeField] private int vfxRenderTextureWidth = 512;
    [Min(1)][SerializeField] private int vfxRenderTextureHeight = 512;
    [Min(0.01f)][SerializeField] private float vfxRenderCameraOrthographicSize = 5f;
    [Min(0.01f)][SerializeField] private float vfxProxyWorldHeight = 10f;
    [SerializeField] private Vector3 vfxProxyWorldOffset = Vector3.zero;
    [Min(0.01f)][SerializeField] private float vfxProxyYMultiplier = 100f;

    [Header("보상 아이템 오브젝트")]
    [Tooltip("레거시용 수동 보상 아이템 오브젝트입니다. 비워두면 선택된 유물 아이콘으로 런타임 생성합니다.")]
    [SerializeField] private GameObject rewardItemObject;

    [Tooltip("체크하면 상자 열림 16프레임 애니메이션이 끝난 뒤 아이템이 나타납니다. 체크 해제하면 마지막 클릭 순간 바로 나타납니다.")]
    [SerializeField] private bool showRewardItemAfterOpenAnimation = true;

    [Header("보상 아이템 등장 애니메이션")]
    [Tooltip("아이템이 아래에서 올라오는 거리입니다.")]
    [SerializeField] private float rewardItemRiseDistance = 0.35f;

    [Tooltip("아이템 등장 애니메이션 시간입니다.")]
    [SerializeField] private float rewardItemAppearDuration = 0.35f;

    [Tooltip("등장 중 살짝 커지는 배율입니다.")]
    [SerializeField] private float rewardItemOvershootScale = 1.15f;

    [Header("덜컹 애니메이션 대상")]
    [Tooltip("덜컹거릴 대상입니다. 보통 Chest 오브젝트 또는 상자 전체 부모 오브젝트를 넣습니다.")]
    [SerializeField] private Transform clunkTarget;

    [Header("덜컹거림 설정")]
    [SerializeField] private float clunkDuration = 0.25f;
    [SerializeField] private int clunkCount = 3;
    [SerializeField] private float rotationPower = 4f;

    [Header("클릭 제한")]
    [Tooltip("클릭 후 다음 클릭을 받을 때까지의 대기 시간입니다.")]
    [SerializeField] private float clickCooldown = 0.3f;

    [Header("클릭 시 짧게 재생할 프레임")]
    [SerializeField] private int clickPreviewFrameCount = 4;
    [SerializeField] private float clickPreviewFrameInterval = 0.04f;

    [Header("완전 열림 애니메이션")]
    [SerializeField] private float frameInterval = 0.04f;

    private int currentClickCount;
    private int requiredClickCount;

    private bool isOpened;
    private bool isOpening;
    private bool isPreviewPlaying;
    private bool isClickCooling;
    private bool hasSelectedReward;
    private bool isRewardGranted;
    private bool rewardItemWasRuntimeCreated;

    private Quaternion originalRotation;
    private Coroutine clunkCoroutine;
    private Coroutine clickCooldownCoroutine;
    private Coroutine rewardItemAnimationCoroutine;
    private Coroutine openCompleteVfxDelayCoroutine;

    private Animator chestOpenAnimator;
    private Animation chestOpenAnimation;

    private Transform rewardItemTransform;
    private Vector3 rewardItemOriginalLocalPosition;
    private Vector3 rewardItemOriginalLocalScale;
    private ChestRelicReward selectedReward;
    private readonly List<GameObject> spawnedVfxObjects = new();
    private readonly List<BattleWorldVfxHandle> spawnedVfxHandles = new();

    private void Awake()
    {
        if (clunkTarget == null)
            clunkTarget = transform;

        if (rewardItemSpawnRoot == null)
            rewardItemSpawnRoot = transform;

        if (vfxSpawnRoot == null)
            vfxSpawnRoot = rewardItemSpawnRoot != null ? rewardItemSpawnRoot : transform;

        originalRotation = clunkTarget.localRotation;
        requiredClickCount = GetRequiredClickCount(chestGrade);

        InitializeChestSprites();
        InitializeLegacyRewardItem();

        StopAndHideAllStepVfx();
        StopAndHideVfx(openCompleteVfx);
    }

    private void OnEnable()
    {
        ResetForNewEventRoomEntry();
    }

    public void ResetForNewEventRoomEntry()
    {
        StopRunningCoroutinesForReset();

        currentClickCount = 0;
        requiredClickCount = GetRequiredClickCount(chestGrade);

        isOpened = false;
        isOpening = false;
        isPreviewPlaying = false;
        isClickCooling = false;
        hasSelectedReward = false;
        isRewardGranted = false;
        selectedReward = default;
        RewardPointerExited?.Invoke();

        if (clunkTarget != null)
        {
            originalRotation = clunkTarget.localRotation;
            clunkTarget.localRotation = originalRotation;
        }

        ResetRewardItemObjectForNewEntry();
        InitializeChestSprites();

        StopAndHideAllStepVfx();
        StopAndHideVfx(openCompleteVfx);
        CleanupSpawnedVfxObjects();
        CleanupSpawnedVfxHandles();
    }

    private void StopRunningCoroutinesForReset()
    {
        if (clunkCoroutine != null)
        {
            StopCoroutine(clunkCoroutine);
            clunkCoroutine = null;
        }

        if (clickCooldownCoroutine != null)
        {
            StopCoroutine(clickCooldownCoroutine);
            clickCooldownCoroutine = null;
        }

        if (rewardItemAnimationCoroutine != null)
        {
            StopCoroutine(rewardItemAnimationCoroutine);
            rewardItemAnimationCoroutine = null;
        }

        if (openCompleteVfxDelayCoroutine != null)
        {
            StopCoroutine(openCompleteVfxDelayCoroutine);
            openCompleteVfxDelayCoroutine = null;
        }
    }

    private void ResetRewardItemObjectForNewEntry()
    {
        if (rewardItemWasRuntimeCreated)
        {
            if (rewardItemObject != null)
                Destroy(rewardItemObject);

            rewardItemObject = null;
            rewardItemTransform = null;
            rewardItemWasRuntimeCreated = false;
            return;
        }

        if (rewardItemObject == null)
            return;

        rewardItemTransform = rewardItemObject.transform;
        rewardItemTransform.localPosition = rewardItemOriginalLocalPosition;
        rewardItemTransform.localScale = rewardItemOriginalLocalScale;
        SetRewardItemInteractable(false);
        rewardItemObject.SetActive(false);
    }

    private void InitializeChestSprites()
    {
        if (chestUnder != null)
        {
            chestUnder.gameObject.SetActive(true);
            ApplyYSortOffset(chestUnder, chestUnderSortingOrderOffset);
        }

        if (chestOpen == null)
            return;

        chestOpen.gameObject.SetActive(true);
        ApplyYSortOffset(chestOpen, chestOpenSortingOrderOffset);

        chestOpenAnimator = chestOpen.GetComponent<Animator>();
        if (chestOpenAnimator != null)
            chestOpenAnimator.enabled = false;

        chestOpenAnimation = chestOpen.GetComponent<Animation>();
        if (chestOpenAnimation != null)
        {
            chestOpenAnimation.Stop();
            chestOpenAnimation.enabled = false;
        }

        if (chestOpenFrames != null && chestOpenFrames.Length > 0)
            chestOpen.sprite = chestOpenFrames[0];
    }

    private void ApplyYSortOffset(SpriteRenderer spriteRenderer, int sortingOrderOffset)
    {
        if (spriteRenderer == null)
            return;

        YSortSprite ySort = spriteRenderer.GetComponent<YSortSprite>();
        if (ySort != null)
            ySort.sortingOrderOffset = sortingOrderOffset;
    }

    private void InitializeLegacyRewardItem()
    {
        if (rewardItemObject == null)
            return;

        rewardItemTransform = rewardItemObject.transform;
        rewardItemOriginalLocalPosition = rewardItemTransform.localPosition;
        rewardItemOriginalLocalScale = rewardItemTransform.localScale;

        rewardItemObject.SetActive(false);
    }

    private int GetRequiredClickCount(ChestGrade grade)
    {
        return GetRevealClickCount(grade) + 1;
    }

    private int GetRevealClickCount(ChestGrade grade)
    {
        return Mathf.Clamp((int)grade, 1, 4);
    }

    private void OnMouseDown()
    {
        OnClickChest();
    }

    private void OnClickChest()
    {
        if (isOpened || isOpening || isClickCooling)
            return;

        EnsureRewardSelected();
        StartClickCooldown();

        currentClickCount++;

        PlayClunk();

        // 일반 클릭과 마지막 열림 클릭 모두 Step VFX를 재생합니다.
        PlayStepVfxByClickCount();

        if (currentClickCount < requiredClickCount)
        {
            if (!isPreviewPlaying)
                StartCoroutine(ClickPreviewRoutine());

            return;
        }

        OpenChest();
    }

    private void EnsureRewardSelected()
    {
        if (!useRandomRelicReward || hasSelectedReward)
            return;

        if (!ChestRelicRewardService.TryRollReward(DataManager.Instance, out selectedReward))
        {
            requiredClickCount = GetRequiredClickCount(chestGrade);
            Debug.LogWarning("[ChestOpenButton] Common~Unique 유물 후보가 없어 상자 등급 클릭 수로 동작합니다.");
            return;
        }

        hasSelectedReward = true;
        requiredClickCount = ChestRelicRewardService.GetOpenClickCount(selectedReward.Rarity);

        Debug.Log($"[ChestOpenButton] 유물 보상 선택 / Relic:{selectedReward.RelicId} / Rarity:{selectedReward.Rarity}");
    }

    private void StartClickCooldown()
    {
        if (clickCooldownCoroutine != null)
            StopCoroutine(clickCooldownCoroutine);

        clickCooldownCoroutine = StartCoroutine(ClickCooldownRoutine());
    }

    private IEnumerator ClickCooldownRoutine()
    {
        isClickCooling = true;

        yield return new WaitForSecondsRealtime(clickCooldown);

        isClickCooling = false;
        clickCooldownCoroutine = null;
    }

    private void PlayClunk()
    {
        if (clunkTarget == null)
            return;

        originalRotation = clunkTarget.localRotation;

        if (clunkCoroutine != null)
            StopCoroutine(clunkCoroutine);

        clunkCoroutine = StartCoroutine(ClunkRoutine());
    }

    private IEnumerator ClunkRoutine()
    {
        float timer = 0f;

        while (timer < clunkDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(timer / clunkDuration);

            float shake = Mathf.Sin(t * Mathf.PI * clunkCount * 2f);
            float fade = 1f - t;

            float zRotation = shake * rotationPower * fade;

            if (clunkTarget != null)
                clunkTarget.localRotation = originalRotation * Quaternion.Euler(0f, 0f, zRotation);

            yield return null;
        }

        if (clunkTarget != null)
            clunkTarget.localRotation = originalRotation;

        clunkCoroutine = null;
    }

    private IEnumerator ClickPreviewRoutine()
    {
        isPreviewPlaying = true;

        if (chestOpen == null || chestOpenFrames == null || chestOpenFrames.Length == 0)
        {
            isPreviewPlaying = false;
            yield break;
        }

        chestOpen.gameObject.SetActive(true);
        DisableChestOpenAutoAnimation();

        int frameCount = Mathf.Min(clickPreviewFrameCount, chestOpenFrames.Length);

        for (int i = 0; i < frameCount; i++)
        {
            chestOpen.sprite = chestOpenFrames[i];
            yield return new WaitForSecondsRealtime(clickPreviewFrameInterval);
        }

        chestOpen.sprite = chestOpenFrames[0];

        isPreviewPlaying = false;
    }

    private void OpenChest()
    {
        if (isOpened || isOpening)
            return;

        isOpening = true;

        EnsureRewardItemObject();

        // 마지막 클릭에서 Step VFX가 먼저 나오고,
        // 그 뒤 약간의 딜레이 후 Open Complete VFX가 나오게 합니다.
        if (openCompleteVfxDelayCoroutine != null)
            StopCoroutine(openCompleteVfxDelayCoroutine);

        openCompleteVfxDelayCoroutine = StartCoroutine(PlayOpenCompleteVfxDelayedRoutine());

        if (!showRewardItemAfterOpenAnimation)
            ShowRewardItem();

        StartCoroutine(OpenAnimationRoutine());
    }

    private IEnumerator PlayOpenCompleteVfxDelayedRoutine()
    {
        float delay = Mathf.Max(0f, openCompleteVfxDelay);

        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        PlayOpenCompleteVfx();
        openCompleteVfxDelayCoroutine = null;
    }

    public void ClaimSelectedReward()
    {
        if (!isOpened || isOpening)
            return;

        if (!useRandomRelicReward || !hasSelectedReward || isRewardGranted || !selectedReward.IsValid)
            return;

        if (!ChestRelicRewardService.GrantReward(DataManager.Instance, selectedReward))
        {
            Debug.LogWarning($"[ChestOpenButton] 유물 보상 지급 실패 / Relic:{selectedReward.RelicId}");
            return;
        }

        isRewardGranted = true;
        SetRewardItemInteractable(false);
        RewardPointerExited?.Invoke();

        if (rewardItemObject != null)
            rewardItemObject.SetActive(false);

        RelicEquipPanelUI.RefreshAll();
        RewardClaimed?.Invoke(selectedReward.RelicId);
        Debug.Log($"[ChestOpenButton] 유물 보상 지급 / Relic:{selectedReward.RelicId}");
    }

    public void NotifyRewardPointerEnter()
    {
        if (!isOpened || isRewardGranted || !hasSelectedReward || !selectedReward.IsValid)
            return;

        RewardPointerEntered?.Invoke(selectedReward.RelicId);
    }

    public void NotifyRewardPointerExit()
    {
        RewardPointerExited?.Invoke();
    }

    public void NotifyRewardClicked()
    {
        ClaimSelectedReward();
    }

    private IEnumerator OpenAnimationRoutine()
    {
        if (chestOpen == null || chestOpenFrames == null || chestOpenFrames.Length == 0)
        {
            if (showRewardItemAfterOpenAnimation)
                ShowRewardItem();

            isOpened = true;
            isOpening = false;
            NotifyOpened();
            yield break;
        }

        if (chestUnder != null)
            chestUnder.gameObject.SetActive(true);

        chestOpen.gameObject.SetActive(true);
        DisableChestOpenAutoAnimation();

        for (int i = 0; i < chestOpenFrames.Length; i++)
        {
            chestOpen.sprite = chestOpenFrames[i];
            yield return new WaitForSecondsRealtime(frameInterval);
        }

        chestOpen.sprite = chestOpenFrames[chestOpenFrames.Length - 1];

        if (showRewardItemAfterOpenAnimation)
            ShowRewardItem();

        isOpened = true;
        isOpening = false;
        NotifyOpened();
    }

    private void NotifyOpened()
    {
        Opened?.Invoke();
    }

    private void ShowRewardItem()
    {
        if (!EnsureRewardItemObject())
            return;

        if (rewardItemAnimationCoroutine != null)
            StopCoroutine(rewardItemAnimationCoroutine);

        rewardItemAnimationCoroutine = StartCoroutine(RewardItemAppearRoutine());
    }

    private bool EnsureRewardItemObject()
    {
        if (rewardItemObject == null)
            CreateRewardItemObject();

        if (rewardItemObject == null)
            return false;

        if (rewardItemTransform == null)
            rewardItemTransform = rewardItemObject.transform;

        if (rewardItemWasRuntimeCreated)
        {
            rewardItemOriginalLocalPosition = rewardItemLocalPosition;
            rewardItemOriginalLocalScale = rewardItemLocalScale;
        }

        ConfigureRewardItemInteraction();
        return rewardItemTransform != null;
    }

    private void CreateRewardItemObject()
    {
        Sprite rewardSprite = GetSelectedRewardSprite();
        if (rewardSprite == null)
            return;

        string objectName = string.IsNullOrWhiteSpace(selectedReward.RelicId)
            ? "RelicReward"
            : $"RelicReward_{selectedReward.RelicId}";

        rewardItemObject = new GameObject(objectName);
        rewardItemWasRuntimeCreated = true;

        rewardItemTransform = rewardItemObject.transform;
        rewardItemTransform.SetParent(rewardItemSpawnRoot != null ? rewardItemSpawnRoot : transform, false);
        rewardItemTransform.localPosition = rewardItemLocalPosition;
        rewardItemTransform.localRotation = Quaternion.identity;
        rewardItemTransform.localScale = rewardItemLocalScale;

        SpriteRenderer spriteRenderer = rewardItemObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = rewardSprite;
        spriteRenderer.sortingLayerName = rewardSortingLayerName;
        spriteRenderer.sortingOrder = rewardSortingOrder;

        rewardItemOriginalLocalPosition = rewardItemLocalPosition;
        rewardItemOriginalLocalScale = rewardItemLocalScale;
        rewardItemObject.SetActive(false);
    }

    private void ConfigureRewardItemInteraction()
    {
        if (rewardItemObject == null)
            return;

        if (!hasSelectedReward || !selectedReward.IsValid)
            return;

        SpriteRenderer spriteRenderer = rewardItemObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            BoxCollider2D collider = rewardItemObject.GetComponent<BoxCollider2D>();
            if (collider == null)
                collider = rewardItemObject.AddComponent<BoxCollider2D>();

            Bounds bounds = spriteRenderer.sprite != null ? spriteRenderer.sprite.bounds : new Bounds(Vector3.zero, Vector3.one);
            collider.size = bounds.size;
            collider.offset = bounds.center;
            collider.isTrigger = true;
            collider.enabled = true;
        }

        EventRoomRelicRewardItem rewardView = rewardItemObject.GetComponent<EventRoomRelicRewardItem>();
        if (rewardView == null)
            rewardView = rewardItemObject.AddComponent<EventRoomRelicRewardItem>();

        rewardView.Setup(this, selectedReward.RelicId);
        SetRewardItemInteractable(true);
    }

    private void SetRewardItemInteractable(bool interactable)
    {
        if (rewardItemObject == null)
            return;

        EventRoomRelicRewardItem rewardView = rewardItemObject.GetComponent<EventRoomRelicRewardItem>();
        if (rewardView != null)
            rewardView.SetInteractable(interactable);

        Collider2D collider = rewardItemObject.GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = interactable;
    }

    private Sprite GetSelectedRewardSprite()
    {
        if (!hasSelectedReward || string.IsNullOrWhiteSpace(selectedReward.RelicId))
        {
            Debug.LogWarning("[ChestOpenButton] 선택된 유물 보상이 없어 유물 아이콘을 생성할 수 없습니다.");
            return null;
        }

        if (DataManager.Instance == null || DataManager.Instance.RelicIconDatabase == null)
        {
            Debug.LogWarning("[ChestOpenButton] DataManager 또는 RelicIconDatabase가 없어 유물 아이콘을 생성할 수 없습니다.");
            return null;
        }

        if (!DataManager.Instance.RelicIconDatabase.TryGetIcon(selectedReward.RelicId, out Sprite icon))
        {
            Debug.LogWarning($"[ChestOpenButton] 유물 아이콘을 찾을 수 없습니다. Relic:{selectedReward.RelicId}");
            return null;
        }

        return icon;
    }

    private IEnumerator RewardItemAppearRoutine()
    {
        rewardItemObject.SetActive(true);

        Vector3 startPosition = rewardItemOriginalLocalPosition + Vector3.down * rewardItemRiseDistance;
        Vector3 endPosition = rewardItemOriginalLocalPosition;

        Vector3 startScale = Vector3.zero;
        Vector3 overshootScale = rewardItemOriginalLocalScale * rewardItemOvershootScale;
        Vector3 endScale = rewardItemOriginalLocalScale;

        rewardItemTransform.localPosition = startPosition;
        rewardItemTransform.localScale = startScale;

        float timer = 0f;
        float safeDuration = Mathf.Max(0.01f, rewardItemAppearDuration);

        while (timer < safeDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(timer / safeDuration);
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);

            rewardItemTransform.localPosition = Vector3.LerpUnclamped(startPosition, endPosition, smoothT);

            if (t < 0.7f)
            {
                float scaleT = t / 0.7f;
                scaleT = 1f - Mathf.Pow(1f - scaleT, 3f);
                rewardItemTransform.localScale = Vector3.LerpUnclamped(startScale, overshootScale, scaleT);
            }
            else
            {
                float scaleT = (t - 0.7f) / 0.3f;
                scaleT = 1f - Mathf.Pow(1f - scaleT, 2f);
                rewardItemTransform.localScale = Vector3.LerpUnclamped(overshootScale, endScale, scaleT);
            }

            yield return null;
        }

        rewardItemTransform.localPosition = endPosition;
        rewardItemTransform.localScale = endScale;

        rewardItemAnimationCoroutine = null;
    }

    private void DisableChestOpenAutoAnimation()
    {
        if (chestOpenAnimator != null)
            chestOpenAnimator.enabled = false;

        if (chestOpenAnimation != null)
        {
            chestOpenAnimation.Stop();
            chestOpenAnimation.enabled = false;
        }
    }

    private void PlayStepVfxByClickCount()
    {
        GameObject selectedVfx = GetStepVfxByClickCount();

        if (selectedVfx == null)
            return;

        PlayVfx(selectedVfx, stepVfxLocalPosition);
    }

    private GameObject GetStepVfxByClickCount()
    {
        if (stepVfxList == null || stepVfxList.Length == 0)
            return null;

        int revealCount = GetActiveRevealCount();

        // 마지막 열림 클릭은 revealCount + 1번째 클릭이므로,
        // 이때도 마지막 단계 Step VFX가 나오도록 마지막 인덱스로 고정합니다.
        int index = Mathf.Min(currentClickCount - 1, revealCount - 1);

        if (index < 0 || index >= stepVfxList.Length)
            return null;

        return stepVfxList[index];
    }

    private int GetActiveRevealCount()
    {
        if (hasSelectedReward)
            return ChestRelicRewardService.GetRevealClickCount(selectedReward.Rarity);

        return GetRevealClickCount(chestGrade);
    }

    private void PlayOpenCompleteVfx()
    {
        if (openCompleteVfx == null)
            return;

        PlayVfx(openCompleteVfx, openCompleteVfxLocalPosition);
    }

    private void PlayVfx(GameObject vfxObject, Vector3 localPosition)
    {
        if (vfxObject == null)
            return;

        try
        {
            PlayVfxUnchecked(vfxObject, localPosition);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"[ChestOpenButton] VFX playback failed; chest flow will continue. VFX:{vfxObject.name}\n{exception}");
        }
    }

    private void PlayVfxUnchecked(GameObject vfxObject, Vector3 localPosition)
    {
        if (TryPlayWorldProxyVfx(vfxObject, localPosition))
            return;

        GameObject playTarget = PrepareDirectVfxInstance(vfxObject, localPosition);
        if (playTarget == null)
            return;

        playTarget.SetActive(true);
        RestartParticles(playTarget);

        if (!IsSceneObject(vfxObject) && vfxAutoDestroyDelay > 0f)
            Destroy(playTarget, vfxAutoDestroyDelay);
    }

    private bool TryPlayWorldProxyVfx(GameObject vfxObject, Vector3 localPosition)
    {
        if (!useIndividualWorldVfxProxy || vfxObject == null)
            return false;

        int renderLayer = ResolveVfxLayer();
        if (renderLayer < 0)
            return false;

        BattleVfxEntry entry = CreateWorldVfxEntry(vfxObject, localPosition);
        bool spawned = BattleWorldVfxRenderer.TrySpawnDetached(
            entry,
            GetVfxWorldPosition(localPosition),
            renderLayer,
            ResolveVisibleVfxLayer(renderLayer),
            GetSafeVfxLifeTime(),
            ConfigureWorldProxyVfxInstance,
            out BattleWorldVfxHandle handle);

        if (!spawned)
            return false;

        if (handle != null)
            spawnedVfxHandles.Add(handle);

        return true;
    }

    private BattleVfxEntry CreateWorldVfxEntry(GameObject vfxObject, Vector3 localPosition)
    {
        return new BattleVfxEntry
        {
            prefab = vfxObject,
            renderMode = BattleVfxRenderMode.IndividualWorldRenderTexture,
            renderTextureWidth = Mathf.Max(1, vfxRenderTextureWidth),
            renderTextureHeight = Mathf.Max(1, vfxRenderTextureHeight),
            renderCameraOrthographicSize = Mathf.Max(0.01f, vfxRenderCameraOrthographicSize),
            proxyWorldHeight = Mathf.Max(0.01f, vfxProxyWorldHeight),
            proxyWorldOffset = vfxProxyWorldOffset,
            proxySortingLayerName = GetVfxSortingLayerName(),
            proxySortingOrderOffset = vfxSortingOrder,
            proxySortingWorldYOffset = GetVfxSortingWorldY(localPosition) - GetVfxWorldPosition(localPosition).y,
            proxyYMultiplier = Mathf.Max(0.01f, vfxProxyYMultiplier)
        };
    }

    private void ConfigureWorldProxyVfxInstance(GameObject vfxObject)
    {
        if (vfxObject == null)
            return;

        vfxObject.SetActive(true);

        int layer = ResolveVfxLayer();
        if (layer >= 0)
            SetLayerRecursively(vfxObject, layer);

        RestartParticles(vfxObject);
    }

    private GameObject PrepareDirectVfxInstance(GameObject vfxObject, Vector3 localPosition)
    {
        if (IsSceneObject(vfxObject))
        {
            ApplyVfxLayerAndDirectSorting(vfxObject, GetVfxSortingWorldY(localPosition));
            return vfxObject;
        }

        Transform parent = vfxSpawnRoot != null ? vfxSpawnRoot : transform;
        GameObject instance = Instantiate(vfxObject, parent);
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = vfxObject.transform.localScale;

        ApplyVfxLayerAndDirectSorting(instance, GetVfxSortingWorldY(localPosition));
        spawnedVfxObjects.Add(instance);
        return instance;
    }

    private void ApplyVfxLayerAndDirectSorting(GameObject vfxObject, float sortingWorldY)
    {
        if (vfxObject == null)
            return;

        int layer = ResolveVfxLayer();
        if (layer >= 0)
            SetLayerRecursively(vfxObject, layer);

        ApplyDirectWorldVfxSorting(vfxObject, sortingWorldY);
    }

    private void ApplyDirectWorldVfxSorting(GameObject vfxObject, float sortingWorldY)
    {
        Renderer[] renderers = vfxObject.GetComponentsInChildren<Renderer>(true);
        int baseOrder = BattleWorldVfxSortUtility.CalculateSortingOrder(
            sortingWorldY,
            Mathf.Max(0.01f, vfxProxyYMultiplier),
            vfxSortingOrder);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            int prefabOrderOffset = renderers[i].sortingOrder;
            renderers[i].sortingLayerName = GetVfxSortingLayerName();
            renderers[i].sortingOrder = baseOrder + prefabOrderOffset;
        }
    }

    private void RestartParticles(GameObject vfxObject)
    {
        ParticleSystem[] particles = vfxObject.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] == null)
                continue;

            ParticleSystem.MainModule main = particles[i].main;
            main.playOnAwake = false;

            particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles[i].Play(true);
        }
    }

    private Vector3 GetVfxWorldPosition(Vector3 localPosition)
    {
        Transform parent = vfxSpawnRoot != null ? vfxSpawnRoot : transform;
        return parent.TransformPoint(localPosition);
    }

    private float GetVfxSortingWorldY(Vector3 localPosition)
    {
        if (chestUnder != null)
            return chestUnder.transform.position.y;

        if (chestOpen != null)
            return chestOpen.transform.position.y;

        return GetVfxWorldPosition(localPosition).y;
    }

    private string GetVfxSortingLayerName()
    {
        if (TryGetChestSortingLayerName(out string chestSortingLayerName))
            return chestSortingLayerName;

        if (string.IsNullOrWhiteSpace(vfxSortingLayerName) ||
            string.Equals(vfxSortingLayerName, "Empty", System.StringComparison.OrdinalIgnoreCase))
        {
            return "Unit";
        }

        return vfxSortingLayerName;
    }

    private bool TryGetChestSortingLayerName(out string sortingLayerName)
    {
        sortingLayerName = null;

        if (chestUnder != null && !string.IsNullOrWhiteSpace(chestUnder.sortingLayerName))
        {
            sortingLayerName = chestUnder.sortingLayerName;
            return true;
        }

        if (chestOpen != null && !string.IsNullOrWhiteSpace(chestOpen.sortingLayerName))
        {
            sortingLayerName = chestOpen.sortingLayerName;
            return true;
        }

        return false;
    }

    private int ResolveVfxLayer()
    {
        return LayerMask.NameToLayer(vfxLayerName);
    }

    private int ResolveVisibleVfxLayer(int renderLayer)
    {
        Transform parent = vfxSpawnRoot != null ? vfxSpawnRoot : transform;
        int visibleLayer = parent != null ? parent.gameObject.layer : 0;
        return visibleLayer == renderLayer ? 0 : visibleLayer;
    }

    private float GetSafeVfxLifeTime()
    {
        return Mathf.Max(0.01f, vfxAutoDestroyDelay);
    }

    private void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null)
            return;

        target.layer = layer;

        Transform targetTransform = target.transform;
        for (int i = 0; i < targetTransform.childCount; i++)
            SetLayerRecursively(targetTransform.GetChild(i).gameObject, layer);
    }

    private void StopAndHideAllStepVfx()
    {
        if (stepVfxList == null)
            return;

        for (int i = 0; i < stepVfxList.Length; i++)
        {
            StopAndHideVfx(stepVfxList[i]);
        }
    }

    private void StopAndHideVfx(GameObject vfxObject)
    {
        if (vfxObject == null || !IsSceneObject(vfxObject))
            return;

        ParticleSystem[] particles = vfxObject.GetComponentsInChildren<ParticleSystem>(true);

        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] == null)
                continue;

            ParticleSystem.MainModule main = particles[i].main;
            main.playOnAwake = false;

            particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles[i].Clear(true);
        }

        vfxObject.SetActive(false);
    }

    private bool IsSceneObject(GameObject target)
    {
        return target != null && target.scene.IsValid() && target.scene.isLoaded;
    }

    private void OnDisable()
    {
        if (clunkCoroutine != null)
        {
            StopCoroutine(clunkCoroutine);
            clunkCoroutine = null;
        }

        if (clickCooldownCoroutine != null)
        {
            StopCoroutine(clickCooldownCoroutine);
            clickCooldownCoroutine = null;
        }

        if (rewardItemAnimationCoroutine != null)
        {
            StopCoroutine(rewardItemAnimationCoroutine);
            rewardItemAnimationCoroutine = null;
        }

        if (openCompleteVfxDelayCoroutine != null)
        {
            StopCoroutine(openCompleteVfxDelayCoroutine);
            openCompleteVfxDelayCoroutine = null;
        }

        isClickCooling = false;
        RewardPointerExited?.Invoke();

        if (clunkTarget != null)
            clunkTarget.localRotation = originalRotation;

        if (rewardItemTransform != null)
        {
            rewardItemTransform.localPosition = rewardItemOriginalLocalPosition;
            rewardItemTransform.localScale = rewardItemOriginalLocalScale;
        }

        StopAndHideVfx(openCompleteVfx);
        CleanupSpawnedVfxObjects();
        CleanupSpawnedVfxHandles();
    }

    private void CleanupSpawnedVfxObjects()
    {
        for (int i = spawnedVfxObjects.Count - 1; i >= 0; i--)
        {
            GameObject spawned = spawnedVfxObjects[i];
            if (spawned != null)
                Destroy(spawned);
        }

        spawnedVfxObjects.Clear();
    }

    private void CleanupSpawnedVfxHandles()
    {
        for (int i = spawnedVfxHandles.Count - 1; i >= 0; i--)
        {
            BattleWorldVfxHandle handle = spawnedVfxHandles[i];
            if (handle != null)
                Destroy(handle.gameObject);
        }

        spawnedVfxHandles.Clear();
    }
}