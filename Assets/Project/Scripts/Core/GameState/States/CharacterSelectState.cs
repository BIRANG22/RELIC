using System.Threading.Tasks;
using UnityEngine;

public class CharacterSelectState : BaseGameState
{
    public override GameStateType StateType => GameStateType.CharacterSelect;

    public CharacterSelectState(SceneFlowManager sceneFlow) : base(sceneFlow) { }

    public override async Task Enter(GameStateContext context)
    {
        Debug.Log("[CharacterSelectState] Enter");
        await sceneFlow.LoadSceneAsync(SceneName.CharacterSelect);
    }

    public override Task Exit()
    {
        Debug.Log("[CharacterSelectState] Exit");
        return Task.CompletedTask;
    }
}