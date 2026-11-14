using UnityEngine;

[DefaultExecutionOrder(10000)]
[DisallowMultipleComponent]
public sealed class TutorialForegroundTooltipCanvas : MonoBehaviour
{
    private TutorialFocusGuideRunner registeredRunner;

    public int TutorialSortingOrder => TutorialFocusGuideRunner.ForegroundTooltipSortingOrder;

    private void OnEnable()
    {
        TryRegisterWithActiveGuide();
    }

    private void LateUpdate()
    {
        if (registeredRunner != TutorialFocusGuideRunner.Active)
            TryRegisterWithActiveGuide();
    }

    private void OnDisable()
    {
        registeredRunner = null;
    }

    private void TryRegisterWithActiveGuide()
    {
        TutorialFocusGuideRunner runner = TutorialFocusGuideRunner.Active;
        if (runner == null || !TutorialFocusGuideRunner.IsRunning)
        {
            registeredRunner = null;
            return;
        }

        runner.RegisterForegroundTooltip(transform);
        registeredRunner = runner;
    }
}
