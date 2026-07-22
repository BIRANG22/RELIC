using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DebugBattleSceneBootstrap
{
    private const string DebugBattleSceneName = "DebugBattle";
    private const string DebugWindowObjectName = "BattleEffectDebugWindow";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureDebugWindow(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureDebugWindow(scene);
    }

    private static void EnsureDebugWindow(Scene scene)
    {
        if (!scene.IsValid() || scene.name != DebugBattleSceneName)
            return;

        BattleEffectDebugWindow existingWindow = Object.FindFirstObjectByType<BattleEffectDebugWindow>(
            FindObjectsInactive.Include);

        if (existingWindow != null)
            return;

        GameObject windowObject = new(DebugWindowObjectName);
        windowObject.AddComponent<BattleEffectDebugWindow>();
        windowObject.AddComponent<BattleDebugKillAllMonsters>();
        windowObject.AddComponent<DebugBattleSceneRunner>();

        Debug.Log("[DebugBattleSceneBootstrap] Debug battle window created.");
    }
}

public sealed class DebugBattleSceneRunner : MonoBehaviour
{
    private IEnumerator Start()
    {
        yield return null;
        yield return null;
        OpenBattleRoom();
    }

    private void OpenBattleRoom()
    {
        BattleRoomLoader loader = Object.FindFirstObjectByType<BattleRoomLoader>(
            FindObjectsInactive.Include);

        if (loader == null)
        {
            Debug.LogWarning("[DebugBattleSceneRunner] BattleRoomLoader not found.");
            return;
        }

        BattleSceneController sceneController = Object.FindFirstObjectByType<BattleSceneController>(
            FindObjectsInactive.Include);

        if (sceneController != null)
            sceneController.enabled = false;

        BattleMapPanel mapPanel = Object.FindFirstObjectByType<BattleMapPanel>(
            FindObjectsInactive.Include);

        if (mapPanel != null)
            mapPanel.Close();

        GameObject battleRoomRoot = ResolveBattleRoomRoot(loader);

        if (battleRoomRoot != null && !battleRoomRoot.activeSelf)
            battleRoomRoot.SetActive(true);

        loader.ResetLoadedStateForNextBattle(true);
        loader.RequestLoadBattle();
    }

    private static GameObject ResolveBattleRoomRoot(BattleRoomLoader loader)
    {
        if (loader == null)
            return null;

        Transform current = loader.transform;

        while (current.parent != null &&
               !string.Equals(current.name, "BattleRoom", System.StringComparison.Ordinal))
        {
            current = current.parent;
        }

        return current.gameObject;
    }
}
