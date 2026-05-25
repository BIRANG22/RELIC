using Relic.Gameplay.Battle;
using UnityEngine;

public enum SkillDirection
{
    None,
    Forward,
    Backward,
    Left,
    Right
}

public class DirectionSelectUI : MonoBehaviour
{
    [SerializeField] private GameObject forwardButton;
    [SerializeField] private GameObject backwardButton;
    [SerializeField] private GameObject leftButton;
    [SerializeField] private GameObject rightButton;

    [SerializeField] private DirectionUIPerspectiveController perspectiveController;

    private PlayerActionPlanner actionPlanner;

    private void Awake()
    {
        Hide();
    }

    public void Bind(PlayerActionPlanner planner)
    {
        actionPlanner = planner;
    }

    public void Show(int gridIndex)
    {
        gameObject.SetActive(true);

        if (perspectiveController != null)
            perspectiveController.RefreshByGridIndex(gridIndex);

        if (forwardButton != null) forwardButton.SetActive(true);
        if (backwardButton != null) backwardButton.SetActive(true);
        if (leftButton != null) leftButton.SetActive(true);
        if (rightButton != null) rightButton.SetActive(true);
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SelectForward()
    {
        actionPlanner?.SelectTargetDirection(SkillDirection.Forward);
    }

    public void SelectBackward()
    {
        actionPlanner?.SelectTargetDirection(SkillDirection.Backward);
    }

    public void SelectLeft()
    {
        actionPlanner?.SelectTargetDirection(SkillDirection.Left);
    }

    public void SelectRight()
    {
        actionPlanner?.SelectTargetDirection(SkillDirection.Right);
    }
}