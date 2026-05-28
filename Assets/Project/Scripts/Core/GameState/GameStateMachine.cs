using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class GameStateMachine
{
    private readonly Dictionary<GameStateType, IGameState> states = new();
    private readonly GameStateContext context;

    public IGameState CurrentState { get; private set; }
    public GameStateType CurrentStateType => CurrentState?.StateType ?? GameStateType.None;
    public GameStateContext Context => context;

    private bool isChangingState;

    public GameStateMachine(GameStateContext context)
    {
        this.context = context;
    }

    public void RegisterState(IGameState state)
    {
        if (state == null)
        {
            Debug.LogError("[GameStateMachine] RegisterState failed: state is null.");
            return;
        }

        if (states.ContainsKey(state.StateType))
        {
            Debug.LogWarning($"[GameStateMachine] State already registered. Overwriting: {state.StateType}");
        }

        states[state.StateType] = state;
    }

    public async Task ChangeState(GameStateType newStateType)
    {
        if (isChangingState)
        {
            Debug.LogWarning($"[GameStateMachine] ChangeState ignored. Already changing to another state. Requested: {newStateType}");
            return;
        }

        if (!states.TryGetValue(newStateType, out IGameState nextState))
        {
            Debug.LogError($"[GameStateMachine] State not registered: {newStateType}");
            return;
        }

        if (CurrentStateType == newStateType)
        {
            Debug.Log($"[GameStateMachine] Already in state: {newStateType}");
            return;
        }

        isChangingState = true;

        try
        {
            if (CurrentState != null)
            {
                await CurrentState.Exit();
            }

            CurrentState = nextState;
            await CurrentState.Enter(context);
        }
        finally
        {
            isChangingState = false;
        }
    }
}