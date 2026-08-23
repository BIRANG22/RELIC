using System.Collections;
using Relic.Gameplay.Data;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class QuestManagerHost : Singleton<QuestManagerHost>
{
    [SerializeField] private QuestPanelPresenter questPanel;

    public QuestManager Manager { get; private set; } = new();

    protected override void Awake()
    {
        base.Awake();

        if (IsDuplicateInstance)
            return;

        Manager ??= new QuestManager();
    }

    private IEnumerator Start()
    {
        while (DataManager.Instance == null || DataManager.Instance.LobbyRuntimeStore == null)
            yield return null;

        EnsureQuestPanel();
        Manager.Initialize(DataManager.Instance.LobbyRuntimeStore.GetOrCreate());
        RefreshPanel();
    }

    public QuestActionGateResult CanPerformAction(QuestActionId actionId)
    {
        return Manager.CanPerformAction(actionId);
    }

    public void MarkActionCompleted(QuestActionId actionId, bool saveImmediately = true)
    {
        Manager.MarkActionCompleted(actionId);
        RefreshPanel();

        if (saveImmediately && SaveSystem.Instance != null)
            SaveSystem.Instance.SaveCurrentProgress();
    }

    public void RefreshPanel()
    {
        if (questPanel == null)
            return;

        QuestDisplayState state = Manager.GetCurrentDisplayState();
        questPanel.Show(state.Text, state.Visible);
    }

    private void EnsureQuestPanel()
    {
        if (questPanel != null)
            return;

        Canvas bootstrapCanvas = FindFirstObjectByType<Canvas>();
        Transform parent = bootstrapCanvas != null ? bootstrapCanvas.transform : transform;
        questPanel = QuestPanelPresenter.CreateDefault(parent);
    }
}
