using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class ExecutionRangeGridTests
{
    [Test]
    public void ShowExecutionRange_EnablesAndTintsOnlyRequestedBaseGridCells()
    {
        GridManager gridManager = CreateTwoCellGrid(
            out GameObject root,
            out Renderer firstRenderer,
            out Renderer secondRenderer);

        try
        {
            gridManager.SetGridVisible(false);

            gridManager.ShowExecutionRange(new List<int> { 0 }, Color.red);

            Assert.That(firstRenderer.enabled, Is.True);
            Assert.That(secondRenderer.enabled, Is.False);
            AssertRendererTint(firstRenderer, Color.red);

            gridManager.ClearExecutionRange();

            Assert.That(firstRenderer.enabled, Is.False);
            Assert.That(secondRenderer.enabled, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void GridCellInitialization_ForcesBaseAndHighlightRenderersBehindBattleObjects()
    {
        GameObject cellObject = new("BackSortedGridCell");
        GameObject highlightObject = new("Highlight");
        highlightObject.transform.SetParent(cellObject.transform);

        try
        {
            MeshRenderer baseRenderer = cellObject.AddComponent<MeshRenderer>();
            baseRenderer.sortingLayerName = "Unit";
            baseRenderer.sortingOrder = 500;
            cellObject.AddComponent<BoxCollider>();

            MeshRenderer highlightRenderer = highlightObject.AddComponent<MeshRenderer>();
            highlightRenderer.sortingLayerName = "Unit";
            highlightRenderer.sortingOrder = 500;

            GridCell cell = cellObject.AddComponent<GridCell>();
            cell.Initialize(null, 0, 0, 0);
            cell.SetPreview(Color.green);

            Assert.That(baseRenderer.sortingLayerName, Is.EqualTo("Empty"));
            Assert.That(baseRenderer.sortingOrder, Is.EqualTo(-1000));
            Assert.That(highlightRenderer.sortingLayerName, Is.EqualTo("Empty"));
            Assert.That(highlightRenderer.sortingOrder, Is.EqualTo(-1000));
        }
        finally
        {
            Object.DestroyImmediate(cellObject);
        }
    }

    [Test]
    public void BuildPlayerExecutionRange_UsesReservedMoveRangeForMoveCommand()
    {
        PlayerReservedCommand command = CreatePlayerMoveCommand(
            selectedGridIndex: 12,
            rangeGridIndices: new List<int> { 8, 12 });

        BattleActionRunner runner = new(null);

        List<int> range = InvokePrivateListMethod<PlayerReservedCommand>(
            runner,
            "BuildPlayerExecutionRange",
            command);

        Assert.That(range, Is.EqualTo(new List<int> { 8, 12 }));
    }

    [Test]
    public void BuildMonsterMoveExecutionRange_UsesMovedOccupiedCells()
    {
        GridManager gridManager = new GameObject("GridManagerMonsterMoveRange").AddComponent<GridManager>();
        MonsterUnit monster = new GameObject("MonsterMoveRange").AddComponent<MonsterUnit>();

        try
        {
            monster.SetOccupiedCells(new List<int> { 0, 1 });

            MonsterReservedCommand command = CreateMonsterMoveCommand(Vector2Int.right);
            BattleActionRunner runner = new(gridManager);

            List<int> range = InvokePrivateListMethod<MonsterUnit, MonsterReservedCommand>(
                runner,
                "BuildMonsterMoveExecutionRange",
                monster,
                command);

            Assert.That(range, Is.EqualTo(new List<int> { 5, 6 }));
        }
        finally
        {
            Object.DestroyImmediate(monster.gameObject);
            Object.DestroyImmediate(gridManager.gameObject);
        }
    }

    private static GridManager CreateTwoCellGrid(
        out GameObject root,
        out Renderer firstRenderer,
        out Renderer secondRenderer)
    {
        root = new GameObject("ExecutionRangeGrid");
        firstRenderer = CreateGridCell(root.transform, "Cell_0");
        secondRenderer = CreateGridCell(root.transform, "Cell_1");

        return root.AddComponent<GridManager>();
    }

    private static Renderer CreateGridCell(Transform parent, string name)
    {
        GameObject cellObject = new(name);
        cellObject.transform.SetParent(parent);

        MeshRenderer renderer = cellObject.AddComponent<MeshRenderer>();
        cellObject.AddComponent<BoxCollider>();
        cellObject.AddComponent<GridCell>();

        return renderer;
    }

    private static List<int> InvokePrivateListMethod<TArg>(
        BattleActionRunner runner,
        string methodName,
        TArg arg)
    {
        MethodInfo method = typeof(BattleActionRunner).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"{methodName} method is missing.");
        return (List<int>)method.Invoke(runner, new object[] { arg });
    }

    private static List<int> InvokePrivateListMethod<TArg1, TArg2>(
        BattleActionRunner runner,
        string methodName,
        TArg1 arg1,
        TArg2 arg2)
    {
        MethodInfo method = typeof(BattleActionRunner).GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"{methodName} method is missing.");
        return (List<int>)method.Invoke(runner, new object[] { arg1, arg2 });
    }

    private static void AssertRendererTint(Renderer renderer, Color expected)
    {
        MaterialPropertyBlock block = new();
        renderer.GetPropertyBlock(block);
        Color actual = block.GetColor("_BaseColor");

        Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.001f));
        Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.001f));
        Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.001f));
        Assert.That(actual.a, Is.EqualTo(1f).Within(0.001f));
    }

    private static PlayerReservedCommand CreatePlayerMoveCommand(
        int selectedGridIndex,
        List<int> rangeGridIndices)
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Move_Range_Test"
        };

        SkillMasterData skill = new()
        {
            SkillId = "S_Player_Move_Range_Test",
            Category = Category.Move,
            RangeType = RangeType.Selection
        };

        PlayerReservedCommand command = new(runtime, skill);
        command.SetSelectionResult(
            BattleDirection.Right,
            selectedGridIndex,
            rangeGridIndices,
            Vector2Int.right);

        return command;
    }

    private static MonsterReservedCommand CreateMonsterMoveCommand(Vector2Int moveOffset)
    {
        MonsterMasterData masterData = new()
        {
            MonsterId = "Monster_Move_Range_Test",
            Name = "Monster Move Range Test",
            HP = 10
        };

        MonsterRuntimeData runtime = new("Monster_Move_Range_Runtime", masterData);
        MonsterSkillData skill = new()
        {
            SkillId = "S_Monster_Move_Range_Test",
            TimelineNotation = TimelineActionType.Move
        };

        MonsterReservedCommand command = new(runtime, skill);
        command.SetMoveOffset(moveOffset);
        return command;
    }
}
