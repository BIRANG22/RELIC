using Relic.Gameplay.Monster;
using UnityEngine;

/// <summary>
/// Compatibility component for existing BattleRoom prefabs. The previous
/// step-based first-battle tutorial was removed. New sheet-driven tutorial
/// behavior is owned by TutorialBattleSequence.
/// </summary>
public sealed class BattleFirstTutorialController : MonoBehaviour
{
    public static BattleFirstTutorialController Instance { get; private set; }
    public bool IsRunning => false;
    public bool IsTutorialRootActive => gameObject.activeInHierarchy;
    public int CurrentStepIndex => -1;
    public bool IsConfigured => true;
    public static bool IsInputRestricted => false;
    public static bool CanSelectMove => true;
    public static bool CanSelectSkill01 => true;
    public static bool CanExecuteEndTurn => true;
    public static bool IsTimelineRegisteredSkillPreviewLocked => false;
    public static bool CanClickMonster(MonsterUnit monster) => true;
    public static bool CanClickGrid(int gridIndex) => true;
    public static void NotifyMoveSelected() { TutorialFocusGuideRunner.NotifyMoveSelected(); }
    public static void NotifyGridClicked(int gridIndex) { }
    public static void NotifySkill01Selected() { TutorialFocusGuideRunner.NotifySkillSelected(); }
    public static void NotifyEndTurnRequested() { }
    public bool TryStartTutorialIfNeeded() => false;
    public bool StartTutorial() => false;
    public bool StartPreviewTutorial() => false;
    public void CloseTutorial() { }
    private void Awake() => Instance = this;
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
