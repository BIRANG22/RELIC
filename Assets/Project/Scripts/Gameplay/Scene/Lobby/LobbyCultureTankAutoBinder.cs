using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LobbyCultureTankAutoBinder
{
    private const string LobbySceneName = "Lobby";
    private const string CultureTankNamePrefix = "CultureTank";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Bind(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Bind(scene);
    }

    private static void Bind(Scene scene)
    {
        if (!scene.IsValid() ||
            !string.Equals(scene.name, LobbySceneName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        GameObject[] roots = scene.GetRootGameObjects();

        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            Transform[] transforms = roots[rootIndex].GetComponentsInChildren<Transform>(true);

            for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
            {
                Transform candidate = transforms[transformIndex];
                if (candidate == null ||
                    !candidate.name.StartsWith(CultureTankNamePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (candidate.GetComponent<SpriteRenderer>() == null)
                    continue;

                if (candidate.GetComponent<LobbyCultureTankController>() == null)
                    candidate.gameObject.AddComponent<LobbyCultureTankController>();
            }
        }
    }
}
