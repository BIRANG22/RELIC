using UnityEngine;

public class BattleMapPanelAutoReturnWatcher : MonoBehaviour
{
    private BattleSceneController battleSceneController;
    private bool isInitialized;

    public void Initialize(BattleSceneController controller)
    {
        battleSceneController = controller;
        isInitialized = battleSceneController != null;
    }

    private void OnEnable()
    {
        if (!isInitialized)
            return;

        if (battleSceneController == null)
            return;

        battleSceneController.OnBattleMapPanelEnabledExternally(gameObject);
    }
}
