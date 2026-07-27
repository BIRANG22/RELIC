using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class SteamLobbyInviteControllerDevUiTests
{
    [Test]
    public void Controller_ExposesLobbyIdCopyAndJoinCommands()
    {
        Assert.That(
            typeof(SteamLobbyInviteController).GetMethod(nameof(SteamLobbyInviteController.CopyCurrentLobbyId)),
            Is.Not.Null);
        Assert.That(
            typeof(SteamLobbyInviteController).GetMethod(nameof(SteamLobbyInviteController.JoinLobbyByIdInput)),
            Is.Not.Null);
    }

    [Test]
    public void CreateStatusPanelIfNeeded_CreatesDevelopmentLobbyIdControls()
    {
        var root = new GameObject("Root", typeof(RectTransform));
        var button = new GameObject("Invite", typeof(RectTransform));
        button.transform.SetParent(root.transform, false);
        SteamLobbyInviteController controller = button.AddComponent<SteamLobbyInviteController>();

        MethodInfo createPanel = typeof(SteamLobbyInviteController).GetMethod(
            "CreateStatusPanelIfNeeded",
            BindingFlags.Instance | BindingFlags.NonPublic);
        createPanel.Invoke(controller, null);

        Assert.That(FindDescendant(root.transform, "LobbyIdInput"), Is.Not.Null);
        Assert.That(FindDescendant(root.transform, "CopyLobbyIdButton"), Is.Not.Null);
        Assert.That(FindDescendant(root.transform, "JoinLobbyIdButton"), Is.Not.Null);

        Object.DestroyImmediate(root);
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        Transform[] descendants = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < descendants.Length; i++)
        {
            if (descendants[i].name == objectName)
                return descendants[i];
        }

        return null;
    }
}
