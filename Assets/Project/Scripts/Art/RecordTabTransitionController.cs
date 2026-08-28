using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 도감 메인 탭을 전환할 때 page 오브젝트의 Animator 애니메이션을 재생한 뒤
/// 선택한 RecordPanelUI 탭을 표시합니다.
/// </summary>
public class RecordTabTransitionController : MonoBehaviour
{
    private enum RecordTab
    {
        Unique,
        Skill,
        Fragment,
        Relic,
        Compound,
        Item
    }

    [Header("References")]
    [SerializeField] private RecordPanelUI recordPanelUI;

    [Header("Page Transition")]
    [Tooltip("전환 애니메이션이 들어 있는 page 오브젝트를 연결합니다. Animator와 Image는 자동으로 찾습니다.")]
    [SerializeField] private GameObject pageObject;
    [Tooltip("1→6 방향(낮은 번호에서 높은 번호)으로 이동할 때 재생할 Animator State 이름입니다. 비워두면 Animator의 첫 번째 State를 재생합니다.")]
    [SerializeField] private string forwardAnimationStateName = "UI_book";
    [Tooltip("6→1 방향(높은 번호에서 낮은 번호)으로 이동할 때 재생할 Animator State 이름입니다. 비워두면 Animator의 첫 번째 State를 재생합니다.")]
    [SerializeField] private string backwardAnimationStateName = "UI_book_r";
    [Tooltip("전환 State가 들어 있는 Animator Layer입니다.")]
    [SerializeField, Min(0)] private int animatorLayer = 0;
    [Tooltip("Time.timeScale이 0이어도 page 애니메이션을 재생합니다.")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Fade")]
    [Tooltip("도감의 Info 루트 오브젝트를 연결합니다. 비워두면 Record 아래의 Info를 자동으로 찾습니다.")]
    [SerializeField] private GameObject infoObject;
    [Tooltip("현재 Content와 Info가 사라지는 시간입니다.")]
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.15f;
    [Tooltip("새 Content와 Info가 나타나는 시간입니다.")]
    [SerializeField, Min(0f)] private float fadeInDuration = 0.2f;

    [Header("Contents")]
    [Tooltip("전환 애니메이션 중 숨길 도감 Content들입니다.")]
    [SerializeField] private GameObject uniqueContent;
    [SerializeField] private GameObject skillContent;
    [SerializeField] private GameObject fragmentContent;
    [SerializeField] private GameObject relicContent;
    [SerializeField] private GameObject compoundContent;
    [SerializeField] private GameObject itemContent;

    [Header("Input")]
    [Tooltip("page 애니메이션 재생 중 다른 탭 버튼 입력을 무시합니다.")]
    [SerializeField] private bool blockInputWhilePlaying = true;

    private Animator pageAnimator;
    private Image pageImage;
    private Coroutine transitionCoroutine;
    private bool isPlaying;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        ResolveReferences();
        HidePage();
    }

    private void OnEnable()
    {
        ResolveReferences();
        RestoreFadeState();
        HidePage();
    }

    private void OnDisable()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        isPlaying = false;
        RestoreFadeState();
        HidePage();
    }

    public void ShowUniqueTab() => RequestTab(RecordTab.Unique);
    public void ShowSkillTab() => RequestTab(RecordTab.Skill);
    public void ShowFragmentTab() => RequestTab(RecordTab.Fragment);
    public void ShowRelicTab() => RequestTab(RecordTab.Relic);
    public void ShowCompoundTab() => RequestTab(RecordTab.Compound);
    public void ShowItemTab() => RequestTab(RecordTab.Item);

    private void RequestTab(RecordTab tab)
    {
        ResolveReferences();

        if (recordPanelUI == null)
        {
            Debug.LogWarning("[RecordTabTransitionController] RecordPanelUI가 연결되어 있지 않습니다.", this);
            return;
        }

        if (isPlaying && blockInputWhilePlaying)
            return;

        RecordTab currentTab = GetCurrentTab();
        int currentIndex = GetTabIndex(currentTab);
        int targetIndex = GetTabIndex(tab);

        if (targetIndex == currentIndex)
            return;

        bool forward = targetIndex > currentIndex;
        string stateName = forward ? forwardAnimationStateName : backwardAnimationStateName;

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        transitionCoroutine = StartCoroutine(PlayTransition(tab, stateName));
    }

    private IEnumerator PlayTransition(RecordTab tab, string stateName)
    {
        isPlaying = true;

        yield return FadeOutCurrentUI();
        SetAllContentsActive(false);
        SetActive(infoObject, false);

        if (CanPlayPageAnimation())
            yield return PlayPageAnimation(stateName);

        HidePage();

        PrepareAllContentAlpha(0f);
        SetCanvasGroupAlpha(infoObject, 0f);
        OpenTab(tab);
        SetActive(infoObject, true);
        yield return FadeInCurrentUI();

        isPlaying = false;
        transitionCoroutine = null;
    }

    private void ResolveReferences()
    {
        if (recordPanelUI == null)
            recordPanelUI = GetComponent<RecordPanelUI>();

        if (recordPanelUI == null)
            recordPanelUI = GetComponentInParent<RecordPanelUI>(true);

        if (pageObject == null)
        {
            Transform page = transform.Find("page");
            if (page == null)
            {
                Transform[] children = GetComponentsInChildren<Transform>(true);
                foreach (Transform child in children)
                {
                    if (child != null && child.name == "page")
                    {
                        page = child;
                        break;
                    }
                }
            }

            if (page != null)
                pageObject = page.gameObject;
        }

        if (infoObject == null)
        {
            Transform info = transform.Find("Info");
            if (info != null)
                infoObject = info.gameObject;
        }

        ResolveContentReferences();
        EnsureFadeCanvasGroups();

        if (pageObject == null)
        {
            pageAnimator = null;
            pageImage = null;
            return;
        }

        pageAnimator = pageObject.GetComponent<Animator>();
        if (pageAnimator == null)
            pageAnimator = pageObject.GetComponentInChildren<Animator>(true);

        pageImage = pageObject.GetComponent<Image>();
        if (pageImage == null)
            pageImage = pageObject.GetComponentInChildren<Image>(true);
    }

    private bool CanPlayPageAnimation()
    {
        if (pageObject == null || pageAnimator == null)
        {
            Debug.LogWarning(
                "[RecordTabTransitionController] page 오브젝트 또는 Animator가 연결되어 있지 않아 전환 애니메이션을 생략합니다.",
                this);
            return false;
        }

        return true;
    }

    private IEnumerator PlayPageAnimation(string stateName)
    {
        pageObject.SetActive(true);

        if (pageImage != null)
            pageImage.enabled = true;

        pageAnimator.enabled = true;

        AnimatorUpdateMode previousUpdateMode = pageAnimator.updateMode;
        if (useUnscaledTime)
            pageAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;

        if (string.IsNullOrWhiteSpace(stateName))
            pageAnimator.Play(0, animatorLayer, 0f);
        else
            pageAnimator.Play(stateName, animatorLayer, 0f);

        pageAnimator.Update(0f);

        AnimatorStateInfo startState = pageAnimator.GetCurrentAnimatorStateInfo(animatorLayer);
        int playedStateHash = startState.fullPathHash;

        float effectiveSpeed = Mathf.Abs(pageAnimator.speed * startState.speed * startState.speedMultiplier);
        if (effectiveSpeed < 0.01f)
            effectiveSpeed = 1f;

        float maxWaitTime = Mathf.Max(0.1f, startState.length / effectiveSpeed + 0.5f);
        float elapsed = 0f;

        while (elapsed < maxWaitTime)
        {
            AnimatorStateInfo currentState = pageAnimator.GetCurrentAnimatorStateInfo(animatorLayer);
            bool inTransition = pageAnimator.IsInTransition(animatorLayer);
            bool sameState = currentState.fullPathHash == playedStateHash;

            if (!inTransition && sameState && currentState.normalizedTime >= 1f)
                break;

            if (!inTransition && !sameState)
                break;

            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        pageAnimator.updateMode = previousUpdateMode;
    }


    private RecordTab GetCurrentTab()
    {
        if (uniqueContent != null && uniqueContent.activeInHierarchy)
            return RecordTab.Unique;
        if (skillContent != null && skillContent.activeInHierarchy)
            return RecordTab.Skill;
        if (fragmentContent != null && fragmentContent.activeInHierarchy)
            return RecordTab.Fragment;
        if (relicContent != null && relicContent.activeInHierarchy)
            return RecordTab.Relic;
        if (compoundContent != null && compoundContent.activeInHierarchy)
            return RecordTab.Compound;
        if (itemContent != null && itemContent.activeInHierarchy)
            return RecordTab.Item;

        return RecordTab.Unique;
    }

    private static int GetTabIndex(RecordTab tab)
    {
        return tab switch
        {
            RecordTab.Unique => 1,
            RecordTab.Skill => 2,
            RecordTab.Fragment => 3,
            RecordTab.Relic => 4,
            RecordTab.Compound => 5,
            RecordTab.Item => 6,
            _ => 1
        };
    }

    private void OpenTab(RecordTab tab)
    {
        switch (tab)
        {
            case RecordTab.Unique:
                recordPanelUI.ShowUniqueTab();
                break;
            case RecordTab.Skill:
                recordPanelUI.ShowSkillTab();
                break;
            case RecordTab.Fragment:
                recordPanelUI.ShowFragmentTab();
                break;
            case RecordTab.Relic:
                recordPanelUI.ShowRelicTab();
                break;
            case RecordTab.Compound:
                recordPanelUI.ShowCompoundTab();
                break;
            case RecordTab.Item:
                recordPanelUI.ShowItemTab();
                break;
        }
    }

    private void SetAllContentsActive(bool active)
    {
        SetActive(uniqueContent, active);
        SetActive(skillContent, active);
        SetActive(fragmentContent, active);
        SetActive(relicContent, active);
        SetActive(compoundContent, active);
        SetActive(itemContent, active);
    }

    private IEnumerator FadeOutCurrentUI()
    {
        CanvasGroup[] groups = GetVisibleFadeGroups();
        yield return FadeGroups(groups, 1f, 0f, fadeOutDuration);
    }

    private IEnumerator FadeInCurrentUI()
    {
        CanvasGroup[] groups = GetVisibleFadeGroups();
        yield return FadeGroups(groups, 0f, 1f, fadeInDuration);
    }

    private IEnumerator FadeGroups(CanvasGroup[] groups, float from, float to, float duration)
    {
        if (groups == null || groups.Length == 0)
            yield break;

        foreach (CanvasGroup group in groups)
        {
            if (group != null)
            {
                group.alpha = from;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
        }

        if (duration <= 0f)
        {
            foreach (CanvasGroup group in groups)
            {
                if (group != null)
                    group.alpha = to;
            }
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = Mathf.Lerp(from, to, t);

                foreach (CanvasGroup group in groups)
                {
                    if (group != null)
                        group.alpha = alpha;
                }

                yield return null;
            }

            foreach (CanvasGroup group in groups)
            {
                if (group != null)
                    group.alpha = to;
            }
        }

        bool enableInput = to > 0.999f;
        foreach (CanvasGroup group in groups)
        {
            if (group != null)
            {
                group.interactable = enableInput;
                group.blocksRaycasts = enableInput;
            }
        }
    }

    private CanvasGroup[] GetVisibleFadeGroups()
    {
        System.Collections.Generic.List<CanvasGroup> groups = new System.Collections.Generic.List<CanvasGroup>(2);

        GameObject activeContent = GetActiveContent();
        CanvasGroup contentGroup = GetOrAddCanvasGroup(activeContent);
        if (contentGroup != null)
            groups.Add(contentGroup);

        if (infoObject != null && infoObject.activeInHierarchy)
        {
            CanvasGroup infoGroup = GetOrAddCanvasGroup(infoObject);
            if (infoGroup != null && !groups.Contains(infoGroup))
                groups.Add(infoGroup);
        }

        return groups.ToArray();
    }

    private GameObject GetActiveContent()
    {
        GameObject[] contents =
        {
            uniqueContent,
            skillContent,
            fragmentContent,
            relicContent,
            compoundContent,
            itemContent
        };

        foreach (GameObject content in contents)
        {
            if (content != null && content.activeInHierarchy)
                return content;
        }

        return null;
    }

    private void PrepareAllContentAlpha(float alpha)
    {
        SetCanvasGroupAlpha(uniqueContent, alpha);
        SetCanvasGroupAlpha(skillContent, alpha);
        SetCanvasGroupAlpha(fragmentContent, alpha);
        SetCanvasGroupAlpha(relicContent, alpha);
        SetCanvasGroupAlpha(compoundContent, alpha);
        SetCanvasGroupAlpha(itemContent, alpha);
    }

    private void EnsureFadeCanvasGroups()
    {
        GetOrAddCanvasGroup(infoObject);
        GetOrAddCanvasGroup(uniqueContent);
        GetOrAddCanvasGroup(skillContent);
        GetOrAddCanvasGroup(fragmentContent);
        GetOrAddCanvasGroup(relicContent);
        GetOrAddCanvasGroup(compoundContent);
        GetOrAddCanvasGroup(itemContent);
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        if (target == null)
            return null;

        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null)
            group = target.AddComponent<CanvasGroup>();

        return group;
    }

    private static void SetCanvasGroupAlpha(GameObject target, float alpha)
    {
        CanvasGroup group = GetOrAddCanvasGroup(target);
        if (group == null)
            return;

        group.alpha = alpha;
        bool visible = alpha > 0.999f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }


    private void RestoreFadeState()
    {
        SetCanvasGroupAlpha(infoObject, 1f);
        SetCanvasGroupAlpha(uniqueContent, 1f);
        SetCanvasGroupAlpha(skillContent, 1f);
        SetCanvasGroupAlpha(fragmentContent, 1f);
        SetCanvasGroupAlpha(relicContent, 1f);
        SetCanvasGroupAlpha(compoundContent, 1f);
        SetCanvasGroupAlpha(itemContent, 1f);
    }

    private void ResolveContentReferences()
    {
        if (uniqueContent == null)
            uniqueContent = FindRelativeGameObject("Content/UniqueContent", "UniqueContent");
        if (skillContent == null)
            skillContent = FindRelativeGameObject("Content/SkillContent", "SkillContent");
        if (fragmentContent == null)
            fragmentContent = FindRelativeGameObject("Content/FragmentContent", "FragmentContent");
        if (relicContent == null)
            relicContent = FindRelativeGameObject("Content/RelicContent", "RelicContent");
        if (compoundContent == null)
            compoundContent = FindRelativeGameObject("Content/CompoundContent", "CompoundContent");
        if (itemContent == null)
            itemContent = FindRelativeGameObject("Content/ItemContent", "ItemContent");
    }

    private GameObject FindRelativeGameObject(string path, string objectName)
    {
        Transform found = transform.Find(path);
        if (found != null)
            return found.gameObject;

        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child != null && child.name == objectName)
                return child.gameObject;
        }

        return null;
    }

    private void HidePage()
    {
        if (pageImage != null)
            pageImage.enabled = false;

        if (pageAnimator != null)
            pageAnimator.enabled = false;

        if (pageObject != null && pageObject != gameObject)
            pageObject.SetActive(false);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
            target.SetActive(active);
    }
}
