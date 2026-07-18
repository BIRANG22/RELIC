using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SkillUpgradePanelMode
{
    None,
    Lobby,
    Battle
}

[DefaultExecutionOrder(-10000)]
public sealed class SkillUpgradePanelContextSelector : MonoBehaviour
{
    [SerializeField] private LobbySkillUpgradePanelUI lobbyController;
    [SerializeField] private SkillUpgradePanel battleController;

    private void Awake()
    {
        RefreshContext();
    }

    private void OnEnable()
    {
        RefreshContext();
    }

    public static SkillUpgradePanelMode ResolveMode(string sceneName)
    {
        if (string.Equals(sceneName, "Lobby", StringComparison.OrdinalIgnoreCase))
            return SkillUpgradePanelMode.Lobby;
        if (string.Equals(sceneName, "Battle", StringComparison.OrdinalIgnoreCase))
            return SkillUpgradePanelMode.Battle;
        return SkillUpgradePanelMode.None;
    }

    public void Configure(
        LobbySkillUpgradePanelUI lobby,
        SkillUpgradePanel battle)
    {
        lobbyController = lobby;
        battleController = battle;
    }

    public void RefreshContext()
    {
        SkillUpgradePanelMode mode = ResolveMode(gameObject.scene.name);
        ApplyMode(mode);
        Debug.Log($"[SkillUpgradePanelContextSelector] Scene:{gameObject.scene.name}, Mode:{mode}");
    }

    private void ApplyMode(SkillUpgradePanelMode mode)
    {
        if (lobbyController != null)
            lobbyController.enabled = mode == SkillUpgradePanelMode.Lobby;
        if (battleController != null)
            battleController.enabled = mode == SkillUpgradePanelMode.Battle;

        if (mode == SkillUpgradePanelMode.Lobby)
            lobbyController?.ActivateForContext();
        else if (mode == SkillUpgradePanelMode.Battle)
            battleController?.ActivateForContext();
    }
}
