using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class BattleActionRegressionTests
{
    [Test]
    public void PartyRuntimeStore_AllowsFullBattleGridCurrentIndex()
    {
        PartyRuntimeStore store = new();

        Assert.That(store.SetCharacter(0, "Char_Test"), Is.True);
        Assert.That(store.SetCurrentGridIndex(0, 34), Is.True);
        Assert.That(store.GetCurrentGridIndex(0), Is.EqualTo(34));
    }

    [Test]
    public void PlayerMoveConflictInfo_UsesSimulatedMoveResult()
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Test",
            CurrentMoveLevel = 3
        };

        SkillMasterData moveSkill = new()
        {
            SkillId = "S_Move_Test",
            RangeType = RangeType.Selection
        };

        PlayerReservedCommand command = new(runtime, moveSkill);
        command.SetSelectionResult(BattleDirection.Right, 10, new List<int> { 10 }, Vector2Int.right);
        command.SetSimulatedMoveResult(false, 12, new Vector2Int(2, 0));

        BattleActionBatchBuilder builder = new(null);
        MethodInfo method = typeof(BattleActionBatchBuilder).GetMethod(
            "CreatePlayerActionInfo",
            BindingFlags.Instance | BindingFlags.NonPublic);

        object actionInfo = method.Invoke(builder, new object[] { command });
        FieldInfo moveTargetCellsField = actionInfo.GetType().GetField("MoveTargetCells");
        List<int> moveTargetCells = (List<int>)moveTargetCellsField.GetValue(actionInfo);

        Assert.That(moveTargetCells, Is.EquivalentTo(new[] { 12 }));
    }

    [Test]
    public void SelfMoveSelection_FlipsCurrentDirection()
    {
        GameObject gameObject = new("ReservationController");
        PlayerSkillReservationController controller =
            gameObject.AddComponent<PlayerSkillReservationController>();

        GridManager gridManager = new GameObject("GridManager").AddComponent<GridManager>();
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Test",
            Direction = BattleDirection.Right
        };

        typeof(PlayerSkillReservationController)
            .GetField("gridManager", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, gridManager);

        typeof(PlayerSkillReservationController)
            .GetField("currentUserRuntime", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, runtime);

        MethodInfo method = typeof(PlayerSkillReservationController).GetMethod(
            "GetDirectionFromMove",
            BindingFlags.Instance | BindingFlags.NonPublic);

        BattleDirection direction =
            (BattleDirection)method.Invoke(controller, new object[] { 10, 10 });

        Object.DestroyImmediate(gameObject);
        Object.DestroyImmediate(gridManager.gameObject);

        Assert.That(direction, Is.EqualTo(BattleDirection.Left));
    }

    [Test]
    public void SelfMoveSelection_FlipsPreviewDirectionWithoutMutatingRuntimeDirection()
    {
        GameObject gameObject = new("ReservationControllerPreview");
        PlayerSkillReservationController controller =
            gameObject.AddComponent<PlayerSkillReservationController>();

        GridManager gridManager = new GameObject("GridManagerPreview").AddComponent<GridManager>();
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Test",
            Direction = BattleDirection.Right
        };

        typeof(PlayerSkillReservationController)
            .GetField("gridManager", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, gridManager);

        typeof(PlayerSkillReservationController)
            .GetField("currentUserRuntime", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, runtime);

        FieldInfo previewDirectionField = typeof(PlayerSkillReservationController)
            .GetField("currentCasterDirection", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(previewDirectionField, Is.Not.Null);
        previewDirectionField.SetValue(controller, BattleDirection.Left);

        MethodInfo method = typeof(PlayerSkillReservationController).GetMethod(
            "GetDirectionFromMove",
            BindingFlags.Instance | BindingFlags.NonPublic);

        BattleDirection direction =
            (BattleDirection)method.Invoke(controller, new object[] { 10, 10 });

        Object.DestroyImmediate(gameObject);
        Object.DestroyImmediate(gridManager.gameObject);

        Assert.That(direction, Is.EqualTo(BattleDirection.Right));
        Assert.That(runtime.Direction, Is.EqualTo(BattleDirection.Right));
    }

    [Test]
    public void PlayerReservedCommand_SetMoveDirection_UpdatesDirectionOnly()
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Test"
        };

        SkillMasterData moveSkill = new()
        {
            SkillId = "S_Move_Test",
            RangeType = RangeType.Selection
        };

        PlayerReservedCommand command = new(runtime, moveSkill);
        command.SetSelectionResult(BattleDirection.Right, 10, new List<int> { 10 }, Vector2Int.zero);

        MethodInfo method = typeof(PlayerReservedCommand).GetMethod(
            "SetMoveDirection",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.That(method, Is.Not.Null);
        method.Invoke(command, new object[] { BattleDirection.Left });

        Assert.That(command.Direction, Is.EqualTo(BattleDirection.Left));
        Assert.That(command.MoveOffset, Is.EqualTo(Vector2Int.zero));
        Assert.That(command.ReservedMoveGridIndex, Is.EqualTo(10));
    }

    [Test]
    public void TimelinePreviewDirection_RecomputesAfterSelfFlipIsRemoved()
    {
        GameObject timelineObject = new("TimelinePreviewDirection");
        BattleTimelineController timeline =
            timelineObject.AddComponent<BattleTimelineController>();

        ReserveTurnSlotUI slot0 = new GameObject("Slot0").AddComponent<ReserveTurnSlotUI>();
        ReserveTurnSlotUI slot1 = new GameObject("Slot1").AddComponent<ReserveTurnSlotUI>();
        ReserveTurnSlotUI[] slots = { slot0, slot1 };

        typeof(BattleTimelineController)
            .GetField("reserveSlots", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(timeline, slots);

        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Test",
            Direction = BattleDirection.Right
        };

        SkillMasterData moveSkill = new()
        {
            SkillId = "S_Move_Test",
            RangeType = RangeType.Selection
        };

        PlayerReservedCommand selfFlip = new(runtime, moveSkill);
        selfFlip.SetSelectionResult(
            BattleDirection.Left,
            10,
            new List<int> { 10 },
            Vector2Int.zero
        );

        Assert.That(slot0.AddCommand(selfFlip), Is.True);
        Assert.That(timeline.GetPreviewDirection(runtime, 1), Is.EqualTo(BattleDirection.Left));

        Assert.That(slot0.RemoveCommandAt(0, out _), Is.True);

        PlayerReservedCommand verticalMove = new(runtime, moveSkill);
        verticalMove.SetSelectionResult(
            BattleDirection.Left,
            15,
            new List<int> { 15 },
            Vector2Int.up
        );

        Assert.That(slot0.AddCommand(verticalMove), Is.True);
        Assert.That(timeline.GetPreviewDirection(runtime, 1), Is.EqualTo(BattleDirection.Right));

        Object.DestroyImmediate(timelineObject);
        Object.DestroyImmediate(slot0.gameObject);
        Object.DestroyImmediate(slot1.gameObject);
    }

    [Test]
    public void MoveRangePreview_UsesRemainingSlotCapacityAsManhattanDistance()
    {
        GridManager gridManager = new GameObject("GridManagerMoveRange").AddComponent<GridManager>();

        List<int> range = PlayerSkillReservationController.GetMoveRangeIndices(
            12,
            3,
            gridManager
        );

        Object.DestroyImmediate(gridManager.gameObject);

        Assert.That(range, Does.Contain(12));
        Assert.That(range, Does.Contain(27));
        Assert.That(range, Does.Contain(15));
        Assert.That(range, Has.No.Member(28));
    }

    [Test]
    public void MoveReservationCount_TreatsDiagonalAsHorizontalPlusVertical()
    {
        int count = PlayerSkillReservationController.GetRequiredMoveReservationCount(
            new Vector2Int(1, 1),
            1
        );

        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public void MoveReservationOffsets_SplitsDiagonalIntoCardinalSteps()
    {
        List<Vector2Int> offsets = PlayerSkillReservationController.BuildMoveReservationOffsets(
            new Vector2Int(1, 1),
            1
        );

        Assert.That(offsets, Is.EqualTo(new List<Vector2Int>
        {
            Vector2Int.right,
            Vector2Int.up
        }));
    }

    [Test]
    public void MoveReservationOffsets_UsesMoveDistanceForStraightSteps()
    {
        List<Vector2Int> offsets = PlayerSkillReservationController.BuildMoveReservationOffsets(
            new Vector2Int(2, 0),
            2
        );

        Assert.That(offsets, Is.EqualTo(new List<Vector2Int>
        {
            new Vector2Int(2, 0)
        }));
    }

    [Test]
    public void MoveRangePreview_LevelTwoMoveAllowsStraightTwoButNotDiagonalInOneReservation()
    {
        GridManager gridManager = new GameObject("GridManagerLevelTwoMoveRange").AddComponent<GridManager>();

        List<int> range = PlayerSkillReservationController.GetMoveRangeIndices(
            12,
            1,
            2,
            gridManager
        );

        Object.DestroyImmediate(gridManager.gameObject);

        Assert.That(range, Does.Contain(22));
        Assert.That(range, Does.Contain(14));
        Assert.That(range, Has.No.Member(18));
    }

    [Test]
    public void TimelineRemainingPlayerCommandCapacity_SubtractsExistingCommands()
    {
        GameObject timelineObject = new("TimelineCapacity");
        BattleTimelineController timeline =
            timelineObject.AddComponent<BattleTimelineController>();

        ReserveTurnSlotUI slot0 = new GameObject("SlotCapacity0").AddComponent<ReserveTurnSlotUI>();
        ReserveTurnSlotUI[] slots = { slot0 };

        typeof(BattleTimelineController)
            .GetField("reserveSlots", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(timeline, slots);

        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Test"
        };

        SkillMasterData skill = new()
        {
            SkillId = "S_Test"
        };

        Assert.That(slot0.AddCommand(new PlayerReservedCommand(runtime, skill)), Is.True);

        int remainingCapacity = timeline.GetRemainingPlayerCommandCapacity(0);

        Object.DestroyImmediate(timelineObject);
        Object.DestroyImmediate(slot0.gameObject);

        Assert.That(remainingCapacity, Is.EqualTo(2));
    }
}
