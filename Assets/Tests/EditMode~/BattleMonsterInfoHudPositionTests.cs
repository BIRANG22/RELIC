using System;
using System.IO;
using NUnit.Framework;

public sealed class BattleMonsterInfoHudPositionTests
{
    [Test]
    public void MonsterInfoSelection_KeepsBattleHudPositionWhenSelectionIsCleared()
    {
        string source = File.ReadAllText("Assets/Project/Scripts/BattleCharacterPanelUI.cs");

        int handlerStart = source.IndexOf(
            "private void HandleMonsterInfoSelectionChanged(MonsterUnit monster)",
            StringComparison.Ordinal);
        int handlerEnd = source.IndexOf(
            "private void ScheduleSelectionPanelPositionRefresh()",
            handlerStart,
            StringComparison.Ordinal);
        string handler = source.Substring(handlerStart, handlerEnd - handlerStart);

        Assert.That(handler, Does.Contain("isMonsterInfoSelectionActive = monster != null"));
        Assert.That(handler, Does.Contain("StopSelectionPanelPositionRefresh"));
        Assert.That(handler, Does.Not.Contain("ScheduleSelectionPanelPositionRefresh"));

        int refreshStart = source.IndexOf(
            "private void RefreshSelectionPanelPosition()",
            StringComparison.Ordinal);
        int refreshEnd = source.IndexOf(
            "private bool HasAnyInfoSelection()",
            refreshStart,
            StringComparison.Ordinal);
        string refresh = source.Substring(refreshStart, refreshEnd - refreshStart);

        Assert.That(refresh, Does.Contain("if (isMonsterInfoSelectionActive)"));
    }

    [Test]
    public void MonsterInfoPanel_ShowsWithoutWaitingForBattleHudToMoveDown()
    {
        string source = File.ReadAllText("Assets/Project/Scripts/BattleMonsterInfoCanvasUI.cs");

        int methodStart = source.IndexOf(
            "private IEnumerator RevealAfterBattleUiMovementRoutine()",
            StringComparison.Ordinal);
        int methodEnd = source.IndexOf(
            "private void RefreshBlurReplica()",
            methodStart,
            StringComparison.Ordinal);
        string method = source.Substring(methodStart, methodEnd - methodStart);

        Assert.That(method, Does.Not.Contain("IsAtDefaultPosition"));
        Assert.That(method, Does.Not.Contain("maxWaitForBattleUi"));
        Assert.That(method, Does.Contain("monsterInfoPanel.SetActive(true)"));
    }
}
