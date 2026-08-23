using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using TMPro;
using UnityEngine;

public sealed class QuestManagerBootstrapTests
{
    [Test]
    public void QuestManagerHost_ExposesInitializedManager()
    {
        GameObject gameObject = new("QuestManagerHost");
        QuestManagerHost host = gameObject.AddComponent<QuestManagerHost>();

        Assert.That(host.Manager, Is.Not.Null);

        Object.DestroyImmediate(gameObject);
    }

    [Test]
    public void QuestPanelPresenter_ShowUpdatesTextAndVisibility()
    {
        GameObject root = new("QuestPanel");
        GameObject textObject = new("QuestText");
        textObject.transform.SetParent(root.transform);
        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        QuestPanelPresenter presenter = root.AddComponent<QuestPanelPresenter>();
        presenter.Bind(text);

        presenter.Show("퀘스트 내용", true);

        Assert.That(root.activeSelf, Is.True);
        Assert.That(text.text, Is.EqualTo("퀘스트 내용"));

        Object.DestroyImmediate(root);
    }

    [Test]
    public void QuestManagerHost_HidesPanelWhenSceneVisibilityDisallows()
    {
        GameObject panelRoot = new("QuestPanel");
        GameObject textObject = new("QuestText");
        textObject.transform.SetParent(panelRoot.transform);
        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        QuestPanelPresenter presenter = panelRoot.AddComponent<QuestPanelPresenter>();
        presenter.Bind(text);

        GameObject hostObject = new("QuestManagerHost");
        QuestManagerHost host = hostObject.AddComponent<QuestManagerHost>();
        BindQuestPanel(host, presenter);
        host.Manager.Initialize(new LobbyRuntimeData
        {
            ActiveQuestId = QuestManager.DefaultTutorialQuestId
        });

        host.RefreshPanel();
        Assert.That(panelRoot.activeSelf, Is.True);

        host.SetQuestPanelSceneVisible(false);
        Assert.That(panelRoot.activeSelf, Is.False);

        host.SetQuestPanelSceneVisible(true);
        Assert.That(panelRoot.activeSelf, Is.True);

        Object.DestroyImmediate(hostObject);
        Object.DestroyImmediate(panelRoot);
    }

    [Test]
    public void QuestPanelVisibilityManager_UsesSceneRulesAndDefaultVisibility()
    {
        QuestPanelVisibilityManager manager = new GameObject("QuestPanelVisibilityManager")
            .AddComponent<QuestPanelVisibilityManager>();

        manager.Configure(
            false,
            new QuestPanelVisibilityManager.SceneVisibilityRule("Lobby", true),
            new QuestPanelVisibilityManager.SceneVisibilityRule("Battle", true),
            new QuestPanelVisibilityManager.SceneVisibilityRule("Title", false));

        Assert.That(manager.GetSceneVisible("Lobby"), Is.True);
        Assert.That(manager.GetSceneVisible("Battle"), Is.True);
        Assert.That(manager.GetSceneVisible("Title"), Is.False);
        Assert.That(manager.GetSceneVisible("UnknownScene"), Is.False);

        Object.DestroyImmediate(manager.gameObject);
    }

    private static void BindQuestPanel(QuestManagerHost host, QuestPanelPresenter presenter)
    {
        FieldInfo field = typeof(QuestManagerHost).GetField(
            "questPanel",
            BindingFlags.Instance | BindingFlags.NonPublic);
        field!.SetValue(host, presenter);
    }
}
