using NUnit.Framework;
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
}
