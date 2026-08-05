using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TitleDebugBattleLauncher : MonoBehaviour
{
    [SerializeField] private string debugBattleSceneName = "DebugBattle";

    private bool isLoading;

    public void LoadDebugBattle()
    {
        if (isLoading)
            return;

        isLoading = true;

        if (string.IsNullOrWhiteSpace(debugBattleSceneName))
        {
            Debug.LogError("[TitleDebugBattleLauncher] DebugBattle scene name is empty.");
            isLoading = false;
            return;
        }

        DataManager dataManager = DataManager.Instance;

        if (dataManager == null)
        {
            Debug.LogError("[TitleDebugBattleLauncher] DataManager is missing.");
            isLoading = false;
            return;
        }

        dataManager.Initialize();

        if (!DebugBattlePartySetup.TryCreateDefaultParty(dataManager))
        {
            Debug.LogError("[TitleDebugBattleLauncher] Failed to create the default debug party.");
            isLoading = false;
            return;
        }

        SceneManager.LoadScene(debugBattleSceneName, LoadSceneMode.Single);
    }
}
