using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 첫 전투에서 한 번 표시되는 기본 전투 튜토리얼 패널을 제어합니다.
/// Bootstrap의 Tutorial_Panel에 배치하고 Left / Right / Close 버튼으로 페이지를 이동합니다.
/// </summary>
public class BattleFirstTutorialController : MonoBehaviour
{
    public static BattleFirstTutorialController Instance { get; private set; }

    [Header("Tutorial UI")]
    [SerializeField] private GameObject tutorialRoot;
    [SerializeField] private GameObject[] tutorialSteps;
    [SerializeField] private TMP_Text pageText;

    [Header("Buttons")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button closeButton;

    [Header("Transition")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.2f;

    private CanvasGroup[] stepCanvasGroups;
    private Coroutine transitionRoutine;
    private int currentStepIndex = -1;
    private bool isRunning;
    private bool isTransitioning;
    private static int lastClosedByEscapeFrame = -1;

    public bool IsRunning => isRunning;
    public static bool WasClosedByEscapeThisFrame => lastClosedByEscapeFrame == Time.frameCount;
    public bool IsTutorialRootActive => tutorialRoot != null && tutorialRoot.activeSelf;
    public int CurrentStepIndex => currentStepIndex;
    public bool IsConfigured => tutorialRoot != null && StepCount > 0;

    private int StepCount => tutorialSteps != null ? tutorialSteps.Length : 0;

    private void Awake()
    {
        RegisterInstanceIfConfigured();
        CacheStepCanvasGroups();
        BindButtons();
        HideTutorialImmediate();
    }

    private void RegisterInstanceIfConfigured()
    {
        if (!IsConfigured)
        {
            Debug.LogWarning(
                $"[BattleFirstTutorialController] Instance 등록 제외 - " +
                $"Root: {(tutorialRoot != null ? tutorialRoot.name : "null")}, StepCount: {StepCount}",
                this);
            return;
        }

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                $"[BattleFirstTutorialController] 이미 정상 등록된 Instance가 있어 현재 컨트롤러는 사용하지 않습니다. " +
                $"Current: {name}, Instance: {Instance.name}",
                this);
            return;
        }

        Instance = this;
        Debug.Log(
            $"[BattleFirstTutorialController] Bootstrap tutorial Instance registered - " +
            $"Root: {tutorialRoot.name}, StepCount: {StepCount}",
            this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        UnbindButtons();
    }

    private void OnDisable()
    {
        StopTransition();
        isRunning = false;
        isTransitioning = false;
        currentStepIndex = -1;
    }

    public bool TryStartTutorialIfNeeded()
    {
        if (!TutorialSettings.ShouldShowTutorial)
        {
            HideTutorialImmediate();
            return false;
        }

        bool started = StartTutorial();
        if (started)
        {
            // TutorialToggle1은 "다음 탐사의 첫 전투에서 1회 표시" 예약값입니다.
            // 실제 자동 튜토리얼이 열린 순간 예약을 소비하여 OFF로 저장합니다.
            TutorialSettings.SetShouldShowTutorial(false);
        }

        return started;
    }

    public bool StartTutorial()
    {
        return StartTutorialInternal();
    }

    /// <summary>
    /// 옵션의 TutorialToggle2에서 즉시 확인할 때 사용합니다.
    /// 이 경로로 연 튜토리얼은 닫아도 첫 전투 자동 튜토리얼 설정을 변경하지 않습니다.
    /// </summary>
    public bool StartPreviewTutorial()
    {
        return StartTutorialInternal();
    }

    private bool StartTutorialInternal()
    {

        if (isRunning || tutorialRoot == null || StepCount <= 0)
        {
            Debug.LogWarning(
                $"[BattleFirstTutorialController] Tutorial start failed - " +
                $"Running: {isRunning}, Root: {(tutorialRoot != null ? tutorialRoot.name : "null")}, " +
                $"StepCount: {StepCount}",
                this);
            return false;
        }

        CacheStepCanvasGroups();

        isRunning = true;
        isTransitioning = false;
        currentStepIndex = 0;

        if (tutorialRoot != null)
            tutorialRoot.SetActive(true);

        ShowStepImmediate(currentStepIndex);
        RefreshPageUI();
        return true;
    }

    /// <summary>
    /// 기존 외부 호출 호환용입니다. 다음 페이지로 이동합니다.
    /// 마지막 페이지에서는 자동 종료하지 않고 Close 버튼으로 종료합니다.
    /// </summary>
    public void AdvanceStep()
    {
        GoToNextPage();
    }

    public void GoToPreviousPage()
    {
        if (!isRunning || isTransitioning || currentStepIndex <= 0)
            return;

        ChangeStep(currentStepIndex - 1);
    }

    public void GoToNextPage()
    {
        if (!isRunning || isTransitioning || currentStepIndex < 0 || currentStepIndex >= StepCount - 1)
            return;

        ChangeStep(currentStepIndex + 1);
    }

    public void CloseTutorial()
    {
        if (!isRunning)
        {
            HideTutorialImmediate();
            return;
        }

        CompleteTutorial();
    }

    /// <summary>
    /// ESC 입력을 튜토리얼이 최우선으로 소비합니다.
    /// 같은 프레임에 UIManager나 배틀 메뉴가 동일한 ESC를 다시 처리하지 않도록 기록합니다.
    /// </summary>
    public static bool TryHandleEscapeIfOpen()
    {
        if (WasClosedByEscapeThisFrame)
            return true;

        BattleFirstTutorialController controller = Instance;
        if (controller == null || !controller.IsRunning)
            return false;

        controller.CloseTutorial();
        lastClosedByEscapeFrame = Time.frameCount;
        return true;
    }

    private void ChangeStep(int nextStepIndex)
    {
        if (nextStepIndex < 0 || nextStepIndex >= StepCount || nextStepIndex == currentStepIndex)
            return;

        StopTransition();
        transitionRoutine = StartCoroutine(FadeToStep(currentStepIndex, nextStepIndex));
    }

    private IEnumerator FadeToStep(int previousStepIndex, int nextStepIndex)
    {
        isTransitioning = true;
        SetNavigationInteractable(false);

        CanvasGroup previousGroup = GetStepCanvasGroup(previousStepIndex);
        CanvasGroup nextGroup = GetStepCanvasGroup(nextStepIndex);

        // 1) 현재 페이지가 완전히 사라진 뒤 비활성화합니다.
        if (fadeDuration > 0f && previousGroup != null)
        {
            float elapsed = 0f;
            float startAlpha = previousGroup.alpha;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                previousGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }
        }

        ApplyStepHidden(previousStepIndex);

        // 2) 이전 페이지 비활성화가 끝난 다음에만 다음 페이지를 활성화합니다.
        GameObject nextStep = GetStep(nextStepIndex);
        if (nextStep != null)
            nextStep.SetActive(true);

        if (nextGroup != null)
        {
            nextGroup.alpha = 0f;
            nextGroup.interactable = false;
            nextGroup.blocksRaycasts = false;
        }

        currentStepIndex = nextStepIndex;
        RefreshPageText();

        // 3) 새 페이지를 0 -> 1로 순차 페이드 인합니다.
        if (fadeDuration > 0f && nextGroup != null)
        {
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                nextGroup.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }
        }

        ApplyStepVisible(nextStepIndex);

        isTransitioning = false;
        transitionRoutine = null;
        RefreshNavigationButtons();
    }

    private void CompleteTutorial()
    {
        HideTutorialImmediate();
    }

    private void HideTutorialImmediate()
    {
        StopTransition();

        isRunning = false;
        isTransitioning = false;
        currentStepIndex = -1;

        HideAllStepsImmediate();
        RefreshPageUI();

        if (tutorialRoot != null)
            tutorialRoot.SetActive(false);
    }

    private void ShowStepImmediate(int stepIndex)
    {
        for (int i = 0; i < StepCount; i++)
        {
            if (i == stepIndex)
                ApplyStepVisible(i);
            else
                ApplyStepHidden(i);
        }
    }

    private void HideAllStepsImmediate()
    {
        for (int i = 0; i < StepCount; i++)
            ApplyStepHidden(i);
    }

    private void ApplyStepVisible(int stepIndex)
    {
        GameObject step = GetStep(stepIndex);
        if (step == null)
            return;

        step.SetActive(true);

        CanvasGroup group = GetStepCanvasGroup(stepIndex);
        if (group != null)
        {
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
        }
    }

    private void ApplyStepHidden(int stepIndex)
    {
        GameObject step = GetStep(stepIndex);
        if (step == null)
            return;

        CanvasGroup group = GetStepCanvasGroup(stepIndex);
        if (group != null)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        step.SetActive(false);
    }

    private void RefreshPageUI()
    {
        RefreshPageText();
        RefreshNavigationButtons();
    }

    private void RefreshPageText()
    {
        if (pageText == null)
            return;

        if (!isRunning || currentStepIndex < 0 || StepCount <= 0)
        {
            pageText.text = StepCount > 0 ? $"1/{StepCount}" : "0/0";
            return;
        }

        pageText.text = $"{currentStepIndex + 1}/{StepCount}";
    }

    private void RefreshNavigationButtons()
    {
        if (!isRunning || isTransitioning)
        {
            SetNavigationInteractable(false);
            return;
        }

        if (leftButton != null)
            leftButton.interactable = currentStepIndex > 0;

        if (rightButton != null)
            rightButton.interactable = currentStepIndex >= 0 && currentStepIndex < StepCount - 1;

        if (closeButton != null)
            closeButton.interactable = true;
    }

    private void SetNavigationInteractable(bool interactable)
    {
        if (leftButton != null)
            leftButton.interactable = interactable;

        if (rightButton != null)
            rightButton.interactable = interactable;

        if (closeButton != null)
            closeButton.interactable = interactable;
    }

    private void CacheStepCanvasGroups()
    {
        stepCanvasGroups = new CanvasGroup[StepCount];

        for (int i = 0; i < StepCount; i++)
        {
            GameObject step = GetStep(i);
            if (step == null)
                continue;

            CanvasGroup group = step.GetComponent<CanvasGroup>();
            if (group == null)
                group = step.AddComponent<CanvasGroup>();

            stepCanvasGroups[i] = group;
        }
    }

    private CanvasGroup GetStepCanvasGroup(int index)
    {
        if (stepCanvasGroups == null || index < 0 || index >= stepCanvasGroups.Length)
            return null;

        return stepCanvasGroups[index];
    }

    private GameObject GetStep(int index)
    {
        if (tutorialSteps == null || index < 0 || index >= tutorialSteps.Length)
            return null;

        return tutorialSteps[index];
    }

    private void BindButtons()
    {
        if (leftButton != null)
            leftButton.onClick.AddListener(GoToPreviousPage);

        if (rightButton != null)
            rightButton.onClick.AddListener(GoToNextPage);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseTutorial);
    }

    private void UnbindButtons()
    {
        if (leftButton != null)
            leftButton.onClick.RemoveListener(GoToPreviousPage);

        if (rightButton != null)
            rightButton.onClick.RemoveListener(GoToNextPage);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseTutorial);
    }

    private void StopTransition()
    {
        if (transitionRoutine == null)
            return;

        StopCoroutine(transitionRoutine);
        transitionRoutine = null;
        isTransitioning = false;
    }
}
