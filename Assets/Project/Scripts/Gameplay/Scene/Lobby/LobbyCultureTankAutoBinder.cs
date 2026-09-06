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
        BindResearchers(roots);
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

    /// <summary>
    /// 씬 안의 모든 Researcher를 확인합니다.
    /// LobbyPanelTransitionButton이 있는 월드 Researcher는 전환 버튼만 사용하고,
    /// 전환 버튼이 없는 기존 Researcher에는 기존 상호작용 스크립트를 유지합니다.
    /// </summary>
    private static void BindResearchers(GameObject[] roots)
    {
        if (roots == null)
            return;

        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            GameObject rootObject = roots[rootIndex];
            if (rootObject == null)
                continue;

            Transform[] transforms = rootObject.GetComponentsInChildren<Transform>(true);

            for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
            {
                Transform researcher = transforms[transformIndex];
                if (researcher == null ||
                    !string.Equals(researcher.name, ResearcherObjectName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                BindSingleResearcher(researcher);
            }
        }
    }

    private static void BindSingleResearcher(Transform researcher)
    {
        Transform clickTarget = ResolveClickableTarget(researcher);
        if (clickTarget == null)
            return;

        LobbyResearcherCultureTankInteraction legacyInteraction =
            clickTarget.GetComponent<LobbyResearcherCultureTankInteraction>();

        bool usesPanelTransition = HasLobbyPanelTransitionButton(researcher, clickTarget);

        if (usesPanelTransition)
        {
            // 패널과 배경 변경은 LobbyPanelTransitionButton이 화면을 가린 중간 시점에 처리합니다.
            // 기존 상호작용이 동시에 presenter.Open()을 호출하지 않도록 비활성화합니다.
            if (legacyInteraction != null)
                legacyInteraction.enabled = false;

            return;
        }

        // 전환 버튼을 사용하지 않는 기존 Researcher는 원래 상호작용을 유지합니다.
        if (legacyInteraction == null)
            legacyInteraction = clickTarget.gameObject.AddComponent<LobbyResearcherCultureTankInteraction>();

        legacyInteraction.enabled = true;
    }

    private static bool HasLobbyPanelTransitionButton(Transform researcher, Transform clickTarget)
    {
        if (clickTarget != null && clickTarget.GetComponent<LobbyPanelTransitionButton>() != null)
            return true;

        if (researcher == null)
            return false;

        if (researcher.GetComponent<LobbyPanelTransitionButton>() != null)
            return true;

        if (researcher.GetComponentInChildren<LobbyPanelTransitionButton>(true) != null)
            return true;

        return researcher.GetComponentInParent<LobbyPanelTransitionButton>(true) != null;
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
