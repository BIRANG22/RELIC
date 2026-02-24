using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public void Initialize()
    {
        Debug.Log("GameManager Initialized");
    }
}