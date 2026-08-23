using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class QuestPanelVisibilityManager : MonoBehaviour
{
    [Serializable]
    public sealed class SceneVisibilityRule
    {
        [SerializeField] private string sceneName;
        [SerializeField] private bool visible;

        public SceneVisibilityRule()
        {
        }

        public SceneVisibilityRule(string sceneName, bool visible)
        {
            this.sceneName = sceneName;
            this.visible = visible;
        }

        public string SceneName => sceneName;
        public bool Visible => visible;
    }

    [SerializeField] private QuestManagerHost questManagerHost;
    [SerializeField] private bool defaultVisible;
    [SerializeField] private List<SceneVisibilityRule> sceneVisibilityRules = new()
    {
        new SceneVisibilityRule("Lobby", true),
        new SceneVisibilityRule("Battle", true)
    };

    public bool DefaultVisible => defaultVisible;

    private void Awake()
    {
        ResolveQuestManagerHost();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ApplyActiveSceneVisibility();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public void Configure(bool defaultVisible, params SceneVisibilityRule[] rules)
    {
        this.defaultVisible = defaultVisible;
        sceneVisibilityRules.Clear();

        if (rules == null)
            return;

        for (int i = 0; i < rules.Length; i++)
        {
            if (rules[i] != null)
                sceneVisibilityRules.Add(rules[i]);
        }
    }

    public bool GetSceneVisible(string sceneName)
    {
        string normalizedSceneName = NormalizeSceneName(sceneName);
        if (string.IsNullOrEmpty(normalizedSceneName))
            return defaultVisible;

        for (int i = sceneVisibilityRules.Count - 1; i >= 0; i--)
        {
            SceneVisibilityRule rule = sceneVisibilityRules[i];
            if (rule == null)
                continue;

            if (string.Equals(
                    NormalizeSceneName(rule.SceneName),
                    normalizedSceneName,
                    StringComparison.Ordinal))
            {
                return rule.Visible;
            }
        }

        return defaultVisible;
    }

    public void ApplyActiveSceneVisibility()
    {
        ApplySceneVisibility(SceneManager.GetActiveScene().name);
    }

    public void ApplySceneVisibility(string sceneName)
    {
        QuestManagerHost host = ResolveQuestManagerHost();
        if (host == null)
            return;

        host.SetQuestPanelSceneVisible(GetSceneVisible(sceneName));
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySceneVisibility(scene.name);
    }

    private QuestManagerHost ResolveQuestManagerHost()
    {
        if (questManagerHost != null)
            return questManagerHost;

        questManagerHost = GetComponent<QuestManagerHost>();
        if (questManagerHost == null)
            questManagerHost = QuestManagerHost.Instance;

        return questManagerHost;
    }

    private static string NormalizeSceneName(string sceneName)
    {
        return string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName.Trim();
    }
}
