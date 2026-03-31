using System;

[Serializable]
public class SceneTransitionData
{
    public string FromScene;
    public string ToScene;
    public GameStateType TargetState;
}