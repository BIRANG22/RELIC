using System;

[Serializable]
public class BattleStartPayload
{
    public string stageId;
    public string enemyGroupId;
    public bool isInitialized;

    public void Clear()
    {
        stageId = string.Empty;
        enemyGroupId = string.Empty;
        isInitialized = false;
    }

    public bool IsValid()
    {
        return isInitialized &&
               !string.IsNullOrEmpty(stageId) &&
               !string.IsNullOrEmpty(enemyGroupId);
    }
}