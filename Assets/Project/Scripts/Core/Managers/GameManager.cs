using System.Threading.Tasks;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public GameStateMachine StateMachine { get; private set; }
    public GameStateContext Context { get; private set; }

    [SerializeField] private SceneFlowManager sceneFlowManager;

    public void Initialize()
    {
        Debug.Log("GameManager Initialized");
    }

    protected override async void Awake()
    {
        base.Awake();

        if (IsDuplicateInstance)
            return;

        if (sceneFlowManager == null)
        {
            sceneFlowManager = SceneFlowManager.Instance;
        }

        if (sceneFlowManager == null)
        {
            Debug.LogError("[GameManager] SceneFlowManager not found in scene.");
            return;
        }

        Context = new GameStateContext();
        StateMachine = new GameStateMachine(Context);

        RegisterStates();

        await StartGame();
    }

    private void RegisterStates()
    {
        StateMachine.RegisterState(new BootstrapState(sceneFlowManager));
        StateMachine.RegisterState(new LobbyState(sceneFlowManager));
        StateMachine.RegisterState(new CharacterSelectState(sceneFlowManager));
        StateMachine.RegisterState(new BattleSelectState(sceneFlowManager));
        StateMachine.RegisterState(new BattleState(sceneFlowManager));
    }

    private async Task StartGame()
    {
        await StateMachine.ChangeState(GameStateType.Bootstrap);
    }
}