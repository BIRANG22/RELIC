using UnityEngine;

public class BackToTitleButton : MonoBehaviour
{
    public async void OnClickBackToTitle()
    {
        await GameManager.Instance.StateMachine.ChangeState(GameStateType.Title);
    }
}