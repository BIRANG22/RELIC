using System.Threading.Tasks;

public interface IGameState
{
    GameStateType StateType { get; }

    Task Enter(GameStateContext context);
    Task Exit();
}