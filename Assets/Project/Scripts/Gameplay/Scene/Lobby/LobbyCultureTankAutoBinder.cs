using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LobbyCultureTankAutoBinder
{
    private const string LobbySceneName = "Lobby";
    private const string CultureTankNamePrefix = "CultureTank";
    private const string ResearcherObjectName = "Researcher";

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
        BindCultureTanks(roots);
        BindResearcher(roots);
    }

    private static void BindCultureTanks(GameObject[] roots)
    {
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

    private static void BindResearcher(GameObject[] roots)
    {
        Transform researcher = FindTransformByName(roots, ResearcherObjectName);
        if (researcher == null)
            return;

        Transform clickTarget = ResolveClickableTarget(researcher);
        if (clickTarget == null)
            return;

        if (clickTarget.GetComponent<LobbyResearcherCultureTankInteraction>() == null)
            clickTarget.gameObject.AddComponent<LobbyResearcherCultureTankInteraction>();
    }

    private static Transform FindTransformByName(GameObject[] roots, string objectName)
    {
        if (roots == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            if (roots[rootIndex] == null)
                continue;

            Transform[] transforms = roots[rootIndex].GetComponentsInChildren<Transform>(true);

            for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
            {
                Transform candidate = transforms[transformIndex];
                if (candidate != null &&
                    string.Equals(candidate.name, objectName, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static Transform ResolveClickableTarget(Transform root)
    {
        if (root == null)
            return null;

        if (root.GetComponent<SpriteRenderer>() != null || root.GetComponent<Collider2D>() != null)
            return root;

        SpriteRenderer childRenderer = root.GetComponentInChildren<SpriteRenderer>(true);
        return childRenderer != null ? childRenderer.transform : root;
    }
}
