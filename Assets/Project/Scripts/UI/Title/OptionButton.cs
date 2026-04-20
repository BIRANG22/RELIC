using UnityEngine;

public class OptionButton : MonoBehaviour
{
    public async void OnClickOption()
    {
        AudioManager.Instance.PlaySfx(SfxType.Click);

        var stateMachine = GameManager.Instance.StateMachine;
        var context = GameManager.Instance.Context;

        context.OptionReturnStateType = stateMachine.CurrentStateType;

        await stateMachine.ChangeState(GameStateType.Option);
    }
}