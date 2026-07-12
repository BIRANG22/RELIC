using UnityEngine;

public class BackToTitleButton : MonoBehaviour
{
    public async void OnClickBackToTitle()
    {
        // 게임에서 타이틀로 돌아올 때는 PRESS ANY KEY 시작 연출을 건너뜁니다.
        PressAnyKeyIntro.SkipIntroOnNextTitleLoad();

        await GameManager.Instance.StateMachine.ChangeState(GameStateType.Title);
    }
}