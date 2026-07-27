using UnityEngine;

/// <summary>
/// Shows the first-battle tutorial overlay once when the tutorial setting is enabled.
/// </summary>
public class BattleFirstTutorialController : MonoBehaviour
{
    [Header("Tutorial UI")]
    [SerializeField] private GameObject tutorialRoot;
    [SerializeField] private GameObject[] tutorialSteps;

    [Header("Completion")]
    [SerializeField] private bool markTutorialShownOnComplete = true;

    private int currentStepIndex = -1;
    private bool isRunning;

    public bool IsRunning => isRunning;

    private void Awake()
    {
        HideTutorialImmediate();
    }

    private void Update()
    {
        if (!isRunning)
            return;

        if (WasAdvanceInputPressed())
            AdvanceStep();
    }

    private void OnDisable()
    {
        isRunning = false;
        currentStepIndex = -1;
    }

    public bool TryStartTutorialIfNeeded()
    {
        if (!TutorialSettings.ShouldShowTutorial)
        {
            HideTutorialImmediate();
            return false;
        }

        return StartTutorial();
    }

    public bool StartTutorial()
    {
        if (isRunning || tutorialSteps == null || tutorialSteps.Length == 0)
            return false;

        isRunning = true;
        currentStepIndex = 0;

        if (tutorialRoot != null)
            tutorialRoot.SetActive(true);

        ShowStep(currentStepIndex);
        return true;
    }

    public void AdvanceStep()
    {
        if (!isRunning)
            return;

        int nextStepIndex = currentStepIndex + 1;

        if (nextStepIndex >= tutorialSteps.Length)
        {
            CompleteTutorial();
            return;
        }

        currentStepIndex = nextStepIndex;
        ShowStep(currentStepIndex);
    }

    private void CompleteTutorial()
    {
        HideTutorialImmediate();

        if (markTutorialShownOnComplete)
            TutorialSettings.MarkTutorialShown();
    }

    private void HideTutorialImmediate()
    {
        isRunning = false;
        currentStepIndex = -1;
        ShowStep(-1);

        if (tutorialRoot != null)
            tutorialRoot.SetActive(false);
    }

    private void ShowStep(int stepIndex)
    {
        if (tutorialSteps == null)
            return;

        for (int i = 0; i < tutorialSteps.Length; i++)
        {
            GameObject step = tutorialSteps[i];

            if (step != null)
                step.SetActive(i == stepIndex);
        }
    }

    private static bool WasAdvanceInputPressed()
    {
        if (Input.GetMouseButtonDown(0))
            return true;

        for (int i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).phase == TouchPhase.Began)
                return true;
        }

        return false;
    }
}
