using UnityEngine;

/// <summary>
/// 더 이상 사용하지 않는 구형 전투 튜토리얼 컴포넌트입니다.
/// 기존 씬/프리팹에 남아 있는 컴포넌트가 Missing Script가 되지 않도록 호환용으로만 유지합니다.
/// 새 튜토리얼 전투 시스템에서는 이 컴포넌트를 참조하거나 실행하지 않습니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class BattleFirstTutorialController : MonoBehaviour
{
    public static BattleFirstTutorialController Instance => null;

    public bool IsRunning => false;
    public bool IsTutorialRootActive => false;
    public int CurrentStepIndex => -1;
    public bool IsConfigured => false;

    private void Awake()
    {
        // 구형 컴포넌트가 씬/프리팹에 남아 있어도 아무 동작도 하지 않습니다.
        enabled = false;
    }

    public bool TryStartTutorialIfNeeded() => false;
    public bool StartTutorial() => false;
    public bool StartPreviewTutorial() => false;
    public void AdvanceStep() { }
    public void GoToPreviousPage() { }
    public void GoToNextPage() { }
    public void CloseTutorial() { }

    public static void ResetAutoTutorialRunState() { }
    public static bool TryHandleEscapeIfOpen() => false;
}
