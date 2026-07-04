using System.Collections;
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

    [Header("상자 등급")]
    [SerializeField] private ChestGrade chestGrade = ChestGrade.Grade1;

    [Header("상자 스프라이트")]
    [SerializeField] private SpriteRenderer chestUnder;
    [SerializeField] private SpriteRenderer chestOpen;

    [Header("열림 애니메이션 16프레임")]
    [SerializeField] private Sprite[] chestOpenFrames;

    [Header("등급별 클릭 VFX")]
    [Tooltip("0번=1단계 클릭 VFX, 1번=2단계 클릭 VFX, 2번=3단계 클릭 VFX, 3번=4단계 클릭 VFX")]
    [SerializeField] private GameObject[] stepVfxList;

    [Header("완전 열림 VFX")]
    [Tooltip("상자가 완전히 열릴 때 1회 재생할 새로운 VFX입니다.")]
    [SerializeField] private GameObject openCompleteVfx;

    [Header("보상 아이템 오브젝트")]
    [Tooltip("상자가 완전히 열릴 때 나타날 아이템 스프라이트 오브젝트입니다.")]
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

    private Quaternion originalRotation;
    private Coroutine clunkCoroutine;
    private Coroutine clickCooldownCoroutine;
    private Coroutine rewardItemAnimationCoroutine;

    private Animator chestOpenAnimator;
    private Animation chestOpenAnimation;

    private Transform rewardItemTransform;
    private Vector3 rewardItemOriginalLocalPosition;
    private Vector3 rewardItemOriginalLocalScale;

    private void Awake()
    {
        if (clunkTarget == null)
            clunkTarget = transform;

        originalRotation = clunkTarget.localRotation;

        requiredClickCount = GetRequiredClickCount(chestGrade);

        if (chestUnder != null)
            chestUnder.gameObject.SetActive(true);

        if (chestOpen != null)
        {
            chestOpen.gameObject.SetActive(true);

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

        if (rewardItemObject != null)
        {
            rewardItemTransform = rewardItemObject.transform;
            rewardItemOriginalLocalPosition = rewardItemTransform.localPosition;
            rewardItemOriginalLocalScale = rewardItemTransform.localScale;

            rewardItemObject.SetActive(false);
        }

        StopAndHideAllStepVfx();
        StopAndHideVfx(openCompleteVfx);
    }

    private int GetRequiredClickCount(ChestGrade grade)
    {
        switch (grade)
        {
            case ChestGrade.Grade1:
                return 2;

            case ChestGrade.Grade2:
                return 2;

            case ChestGrade.Grade3:
                return 3;

            case ChestGrade.Grade4:
                return 4;

            default:
                return 2;
        }
    }

    private void OnMouseDown()
    {
        OnClickChest();
    }

    private void OnClickChest()
    {
        if (isOpened || isOpening || isClickCooling)
            return;

        StartClickCooldown();

        currentClickCount++;

        PlayClunk();
        PlayStepVfxByClickCount();

        if (currentClickCount < requiredClickCount)
        {
            if (!isPreviewPlaying)
                StartCoroutine(ClickPreviewRoutine());

            return;
        }

        OpenChest();
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

        PlayOpenCompleteVfx();

        if (!showRewardItemAfterOpenAnimation)
            ShowRewardItem();

        StartCoroutine(OpenAnimationRoutine());

        GiveRewardByGrade();
    }

    private IEnumerator OpenAnimationRoutine()
    {
        if (chestOpen == null || chestOpenFrames == null || chestOpenFrames.Length == 0)
        {
            if (showRewardItemAfterOpenAnimation)
                ShowRewardItem();

            isOpened = true;
            isOpening = false;
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
    }

    private void ShowRewardItem()
    {
        if (rewardItemObject == null || rewardItemTransform == null)
            return;

        if (rewardItemAnimationCoroutine != null)
            StopCoroutine(rewardItemAnimationCoroutine);

        rewardItemAnimationCoroutine = StartCoroutine(RewardItemAppearRoutine());
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

        while (timer < rewardItemAppearDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(timer / rewardItemAppearDuration);
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

        PlayVfx(selectedVfx);
    }

    private GameObject GetStepVfxByClickCount()
    {
        if (stepVfxList == null || stepVfxList.Length == 0)
            return null;

        int index = currentClickCount - 1;

        int maxGradeIndex = (int)chestGrade - 1;
        index = Mathf.Min(index, maxGradeIndex);

        if (index < 0 || index >= stepVfxList.Length)
            return null;

        return stepVfxList[index];
    }

    private void PlayOpenCompleteVfx()
    {
        if (openCompleteVfx == null)
            return;

        PlayVfx(openCompleteVfx);
    }

    private void PlayVfx(GameObject vfxObject)
    {
        if (vfxObject == null)
            return;

        vfxObject.SetActive(true);

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
        if (vfxObject == null)
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

    private void GiveRewardByGrade()
    {
        switch (chestGrade)
        {
            case ChestGrade.Grade1:
                Debug.Log("1단계 상자 보상 지급");
                break;

            case ChestGrade.Grade2:
                Debug.Log("2단계 상자 보상 지급");
                break;

            case ChestGrade.Grade3:
                Debug.Log("3단계 상자 보상 지급");
                break;

            case ChestGrade.Grade4:
                Debug.Log("4단계 상자 보상 지급");
                break;
        }
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

        isClickCooling = false;

        if (clunkTarget != null)
            clunkTarget.localRotation = originalRotation;

        if (rewardItemTransform != null)
        {
            rewardItemTransform.localPosition = rewardItemOriginalLocalPosition;
            rewardItemTransform.localScale = rewardItemOriginalLocalScale;
        }

        StopAndHideVfx(openCompleteVfx);
    }
}