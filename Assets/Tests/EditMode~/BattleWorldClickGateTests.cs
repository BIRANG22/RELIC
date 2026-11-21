using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class BattleWorldClickGateTests
{
    [Test]
    public void CanProcessWorldClick_BlocksWhileBattleActionIsExecuting()
    {
        Assert.That(BattleWorldClickGate.CanProcessWorldClick(true, true), Is.False);
    }

    [Test]
    public void CanProcessWorldClick_AllowsReservationPhase()
    {
        Assert.That(BattleWorldClickGate.CanProcessWorldClick(true, false), Is.True);
    }

    [Test]
    public void CanProcessWorldClick_AllowsObjectsOutsideBattleExecutorLifetime()
    {
        Assert.That(BattleWorldClickGate.CanProcessWorldClick(false, false), Is.True);
    }

    [TestCase(true, true, true)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, false, false)]
    public void CanOpenMonsterInfo_RequiresExecutorInputAndTutorialPermission(
        bool canAcceptPlayerInput,
        bool tutorialAllowsMonsterClick,
        bool expected)
    {
        Assert.That(
            BattleWorldClickGate.CanOpenMonsterInfo(canAcceptPlayerInput, tutorialAllowsMonsterClick),
            Is.EqualTo(expected));
    }

    [Test]
    public void GridCell_WorldClickIsBlockedDuringExecution_ButHoverRemainsActive()
    {
        GameObject executorObject = new("BattleTurnExecutor_Test");
        GameObject gridObject = new("GridManager_Test");
        GameObject cellObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        cellObject.name = "Grid_0";
        cellObject.transform.SetParent(gridObject.transform);

        try
        {
            GridCell cell = cellObject.AddComponent<GridCell>();
            GridManager grid = gridObject.AddComponent<GridManager>();
            BattleTurnExecutor executor = executorObject.AddComponent<BattleTurnExecutor>();
            SetPrivateField(executor, "isExecuting", true);

            int clickCount = 0;
            int hoverCount = 0;
            grid.OnCellClicked += _ => clickCount++;
            grid.OnCellHovered += _ => hoverCount++;

            InvokePrivate(cell, "OnMouseDown");
            InvokePrivate(cell, "OnMouseEnter");

            Assert.That(clickCount, Is.Zero,
                "전투 실행 중 GridCell의 월드 클릭 이벤트가 전달되면 안 됩니다.");
            Assert.That(hoverCount, Is.EqualTo(1),
                "클릭 차단 중에도 GridCell 호버 이벤트는 유지되어야 합니다.");
        }
        finally
        {
            Object.DestroyImmediate(gridObject);
            Object.DestroyImmediate(executorObject);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"필드 '{fieldName}'를 찾지 못했습니다.");
        field.SetValue(target, value);
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, $"메서드 '{methodName}'를 찾지 못했습니다.");
        method.Invoke(target, null);
    }
}
