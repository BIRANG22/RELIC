using UnityEngine;

public class OptionCancelButton : MonoBehaviour
{
    public async void OnClickCancel()
    {
        AudioManager.Instance.PlaySfx(SfxType.Click);

        GameStateType returnState = GameManager.Instance.Context.OptionReturnStateType;

        if (returnState == GameStateType.None || returnState == GameStateType.Option)
        {
            Debug.LogWarning("[OptionCancelButton] Invalid return state. Fallback to Title.");
            returnState = GameStateType.Title;
        }

        await GameManager.Instance.StateMachine.ChangeState(returnState);
    }
}