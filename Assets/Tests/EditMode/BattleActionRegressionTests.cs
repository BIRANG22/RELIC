using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class BattleActionRegressionTests
{
    [Test]
    public void MonsterClick_DoesNotSelectMonsterForHud()
    {
        MonsterUnit monster = CreateMonsterWithHud(
            "ClickNoHudMonster",
            "M_ClickNoHud",
            12,
            out CanvasGroup canvasGroup
        );

        FieldInfo selectedMonsterField = typeof(MonsterUnit).GetField(
            "selectedMonster",
            BindingFlags.Static | BindingFlags.NonPublic);
        selectedMonsterField.SetValue(null, null);

        MethodInfo mouseDownMethod = typeof(MonsterUnit).GetMethod(
            "OnMouseDown",
            BindingFlags.Instance | BindingFlags.NonPublic);

        mouseDownMethod.Invoke(monster, null);

        Assert.That(selectedMonsterField.GetValue(null), Is.Null);
        Assert.That(canvasGroup.alpha, Is.EqualTo(0f));

        Object.DestroyImmediate(canvasGroup.gameObject);
        Object.DestroyImmediate(monster.gameObject);
    }

    [Test]
    public void MonsterHover_ShowsAndHidesBoundHud()
    {
        MonsterUnit monster = CreateMonsterWithHud(
            "HoverHudMonster",
            "M_HoverHud",
            12,
            out CanvasGroup canvasGroup
        );

        MethodInfo mouseEnterMethod = typeof(MonsterUnit).GetMethod(
            "OnMouseEnter",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo mouseExitMethod = typeof(MonsterUnit).GetMethod(
            "OnMouseExit",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(mouseEnterMethod, Is.Not.Null);
        Assert.That(mouseExitMethod, Is.Not.Null);
        Assert.That(canvasGroup.alpha, Is.EqualTo(0f));

        mouseEnterMethod.Invoke(monster, null);
        Assert.That(canvasGroup.alpha, Is.EqualTo(1f));

        mouseExitMethod.Invoke(monster, null);
        Assert.That(canvasGroup.alpha, Is.EqualTo(0f));

        Object.DestroyImmediate(canvasGroup.gameObject);
        Object.DestroyImmediate(monster.gameObject);
    }

    [Test]
    public void TemporaryMonsterHudRange_ShowsMatchingMonstersAndCanBeCleared()
    {
        MonsterUnit inRangeMonster = CreateMonsterWithHud(
            "TemporaryHudInRange",
            "M_TemporaryInRange",
            12,
            out CanvasGroup inRangeCanvasGroup
        );
        MonsterUnit outOfRangeMonster = CreateMonsterWithHud(
            "TemporaryHudOutOfRange",
            "M_TemporaryOutOfRange",
            22,
            out CanvasGroup outOfRangeCanvasGroup
        );

        MethodInfo showTemporaryMethod = typeof(MonsterUnit).GetMethod(
            "ShowTemporaryHUDsInRange",
            BindingFlags.Static | BindingFlags.Public);
        MethodInfo hideTemporaryMethod = typeof(MonsterUnit).GetMethod(
            "HideAllTemporaryHUDs",
            BindingFlags.Static | BindingFlags.Public);

        Assert.That(showTemporaryMethod, Is.Not.Null);
        Assert.That(hideTemporaryMethod, Is.Not.Null);

        showTemporaryMethod.Invoke(null, new object[] { new List<int> { 12 }, 1f });

        Assert.That(inRangeCanvasGroup.alpha, Is.EqualTo(1f));
        Assert.That(outOfRangeCanvasGroup.alpha, Is.EqualTo(0f));

        hideTemporaryMethod.Invoke(null, null);

        Assert.That(inRangeCanvasGroup.alpha, Is.EqualTo(0f));
        Assert.That(outOfRangeCanvasGroup.alpha, Is.EqualTo(0f));

        Object.DestroyImmediate(inRangeCanvasGroup.gameObject);
        Object.DestroyImmediate(outOfRangeCanvasGroup.gameObject);
        Object.DestroyImmediate(inRangeMonster.gameObject);
        Object.DestroyImmediate(outOfRangeMonster.gameObject);
    }

    [Test]
    public void AddStatusToMonster_ShowsBoundHud()
    {
        MonsterUnit monster = CreateMonsterWithHud(
            "StatusHudMonster",
            "M_StatusHud",
            12,
            out CanvasGroup canvasGroup
        );

        BattleEffectUtility.AddStatusToMonster(monster, "E_Weaken", 1, 1);

        Assert.That(monster.RuntimeData.StatusEffects, Has.Count.EqualTo(1));
        Assert.That(monster.RuntimeData.StatusEffects[0].EffectId, Is.EqualTo("E_Weaken"));
        Assert.That(canvasGroup.alpha, Is.EqualTo(1f));

        Object.DestroyImmediate(canvasGroup.gameObject);
        Object.DestroyImmediate(monster.gameObject);
    }

    [Test]
    public void BurnEffectOnMonster_ShowsBoundHud()
    {
        MonsterUnit monster = CreateMonsterWithHud(
            "BurnStatusHudMonster",
            "M_BurnStatusHud",
            12,
            out CanvasGroup canvasGroup
        );

        BurnEffect effect = new();
        effect.Execute(new BattleEffectContext
        {
            MonsterTarget = monster,
            Value = 2
        });

        Assert.That(monster.RuntimeData.StatusEffects, Has.Count.EqualTo(1));
        Assert.That(monster.RuntimeData.StatusEffects[0].EffectId, Is.EqualTo("E_Burn"));
        Assert.That(canvasGroup.alpha, Is.EqualTo(1f));

        Object.DestroyImmediate(canvasGroup.gameObject);
        Object.DestroyImmediate(monster.gameObject);
    }

    [Test]
    public void TurnEndAddictedMonsterDamage_HidesHudBeforeNextReservation()
    {
        MonsterUnit monster = CreateMonsterWithHud(
            "TurnEndAddictedMonster",
            "M_TurnEndAddicted",
            12,
            out CanvasGroup canvasGroup
        );
        monster.RuntimeData.StatusEffects.Add(new StatusEffectRuntimeData
        {
            EffectId = "E_Addicted",
            Stack = 1,
            TurnCount = 1
        });

        BattleActionRunner runner = new(null);
        MethodInfo method = typeof(BattleActionRunner).GetMethod(
            "ApplyTurnEndEffectsRoutine",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.That(method, Is.Not.Null);

        IEnumerator routine = (IEnumerator)method.Invoke(runner, null);

        Assert.That(routine.MoveNext(), Is.True);
        Assert.That(routine.Current, Is.TypeOf<WaitForSeconds>());
        Assert.That(monster.RuntimeData.CurrentHp, Is.EqualTo(monster.RuntimeData.MaxHp - 1));
        Assert.That(canvasGroup.alpha, Is.EqualTo(1f));

        Assert.That(routine.MoveNext(), Is.True);
        Assert.That(routine.Current, Is.TypeOf<WaitForSeconds>());
        Assert.That(canvasGroup.alpha, Is.EqualTo(0f));

        Object.DestroyImmediate(canvasGroup.gameObject);
        Object.DestroyImmediate(monster.gameObject);
    }

    [Test]
    public void GridVisibility_TogglesCellRendererAndCollider()
    {
        GridManager gridManager = CreateOneCellGrid(
            "GridVisibility",
            out Renderer cellRenderer,
            out Collider cellCollider,
            out GameObject gridObject,
            out GameObject cellObject
        );

        MethodInfo method = typeof(GridManager).GetMethod(
            "SetGridVisible",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.That(method, Is.Not.Null);

        method.Invoke(gridManager, new object[] { false });

        Assert.That(cellRenderer.enabled, Is.False);
        Assert.That(cellCollider.enabled, Is.False);

        method.Invoke(gridManager, new object[] { true });

        Assert.That(cellRenderer.enabled, Is.True);
        Assert.That(cellCollider.enabled, Is.True);

        Object.DestroyImmediate(cellObject);
        Object.DestroyImmediate(gridObject);
    }

    [Test]
    public void MonsterReservationVisualState_MakesTransparentButKeepsCollider()
    {
        MonsterUnit monster = CreateMonsterWithSprite(
            "ReservationVisualMonster",
            "M_ReservationVisual",
            out SpriteRenderer spriteRenderer,
            out Collider2D clickCollider
        );

        MethodInfo method = typeof(MonsterUnit).GetMethod(
            "SetReservationVisualState",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.That(method, Is.Not.Null);

        method.Invoke(monster, new object[] { true });

        Assert.That(spriteRenderer.color.a, Is.LessThan(1f));
        Assert.That(clickCollider.enabled, Is.True);

        method.Invoke(monster, new object[] { false });

        Assert.That(spriteRenderer.color.a, Is.EqualTo(1f).Within(0.001f));
        Assert.That(clickCollider.enabled, Is.True);

        Object.DestroyImmediate(monster.gameObject);
    }

    [Test]
    public void BattleInputReady_TogglesGridAndMonsterPresentation()
    {
        GridManager gridManager = CreateOneCellGrid(
            "BattleInputGrid",
            out Renderer cellRenderer,
            out Collider cellCollider,
            out GameObject gridObject,
            out GameObject cellObject
        );
        MonsterUnit monster = CreateMonsterWithSprite(
            "BattleInputMonster",
            "M_BattleInput",
            out SpriteRenderer spriteRenderer,
            out Collider2D _);

        BattleTurnExecutor executor =
            new GameObject("BattleInputExecutor").AddComponent<BattleTurnExecutor>();

        typeof(BattleTurnExecutor)
            .GetField("gridManager", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(executor, gridManager);

        executor.SetBattleInputReady(true);

        Assert.That(cellRenderer.enabled, Is.True);
        Assert.That(cellCollider.enabled, Is.True);
        Assert.That(spriteRenderer.color.a, Is.LessThan(1f));

        executor.SetBattleInputReady(false);

        Assert.That(cellRenderer.enabled, Is.False);
        Assert.That(cellCollider.enabled, Is.False);
        Assert.That(spriteRenderer.color.a, Is.EqualTo(1f).Within(0.001f));

        Object.DestroyImmediate(executor.gameObject);
        Object.DestroyImmediate(monster.gameObject);
        Object.DestroyImmediate(cellObject);
        Object.DestroyImmediate(gridObject);
    }

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

    [Test]
    public void MoveReservationPathCandidates_UsesOpenAxisWhenDiagonalFirstAxisIsBlocked()
    {
        GridManager gridManager = new GameObject("GridManagerBlockedDiagonalPath").AddComponent<GridManager>();
        HashSet<int> blockedGridIndices = new()
        {
            17
        };

        List<List<Vector2Int>> paths =
            PlayerSkillReservationController.GetReservableMovePathCandidates(
                12,
                16,
                1,
                2,
                gridManager,
                blockedGridIndices
            );

        Object.DestroyImmediate(gridManager.gameObject);

        Assert.That(paths, Has.Count.EqualTo(1));
        Assert.That(paths[0], Is.EqualTo(new List<Vector2Int>
        {
            Vector2Int.down,
            Vector2Int.right
        }));
    }

    [Test]
    public void MoveReservationCommands_AddsDiagonalVisualWithoutChangingDataSteps()
    {
        GameObject controllerObject = new("ReservationControllerNoDiagonalJump");
        PlayerSkillReservationController controller =
            controllerObject.AddComponent<PlayerSkillReservationController>();

        GridManager gridManager = new GameObject("GridManagerNoDiagonalJump").AddComponent<GridManager>();
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

        typeof(PlayerSkillReservationController)
            .GetField("gridManager", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, gridManager);

        typeof(PlayerSkillReservationController)
            .GetField("currentUserRuntime", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, runtime);

        typeof(PlayerSkillReservationController)
            .GetField("currentSkillData", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, moveSkill);

        typeof(PlayerSkillReservationController)
            .GetField("currentCasterGridIndex", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, 12);

        typeof(PlayerSkillReservationController)
            .GetField("currentCasterDirection", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, BattleDirection.Right);

        MethodInfo method = typeof(PlayerSkillReservationController).GetMethod(
            "BuildMoveReservationCommands",
            BindingFlags.Instance | BindingFlags.NonPublic);

        List<PlayerReservedCommand> commands =
            (List<PlayerReservedCommand>)method.Invoke(
                controller,
                new object[]
                {
                    new List<Vector2Int>
                    {
                        Vector2Int.right,
                        Vector2Int.down
                    }
                });

        Object.DestroyImmediate(controllerObject);
        Object.DestroyImmediate(gridManager.gameObject);

        Assert.That(commands, Has.Count.EqualTo(2));
        Assert.That(commands[0].MoveOffset, Is.EqualTo(Vector2Int.right));
        Assert.That(commands[0].ReservedMoveGridIndex, Is.EqualTo(17));
        Assert.That(commands[0].EffectiveVisualMoveOffset, Is.EqualTo(Vector2Int.right + Vector2Int.down));
        Assert.That(commands[0].EffectiveVisualMoveGridIndex, Is.EqualTo(16));
        Assert.That(commands[0].VisualMoveSteps, Is.EqualTo(new List<Vector2Int>
        {
            Vector2Int.right,
            Vector2Int.down
        }));
        Assert.That(commands[0].SkipMoveVisual, Is.False);

        Assert.That(commands[1].MoveOffset, Is.EqualTo(Vector2Int.down));
        Assert.That(commands[1].ReservedMoveGridIndex, Is.EqualTo(16));
        Assert.That(commands[1].SkipMoveVisual, Is.True);
    }

    [Test]
    public void TimelinePreviewGridIndexAtSlotEnd_IgnoresFutureSlots()
    {
        GameObject timelineObject = new("TimelineSlotEndPreview");
        BattleTimelineController timeline =
            timelineObject.AddComponent<BattleTimelineController>();

        ReserveTurnSlotUI slot0 = new GameObject("SlotEnd0").AddComponent<ReserveTurnSlotUI>();
        ReserveTurnSlotUI slot1 = new GameObject("SlotEnd1").AddComponent<ReserveTurnSlotUI>();
        ReserveTurnSlotUI slot2 = new GameObject("SlotEnd2").AddComponent<ReserveTurnSlotUI>();
        ReserveTurnSlotUI[] slots = { slot0, slot1, slot2 };

        typeof(BattleTimelineController)
            .GetField("reserveSlots", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(timeline, slots);

        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Test"
        };

        BattleCharacter character =
            new GameObject("TimelineSlotEndCharacter").AddComponent<BattleCharacter>();
        character.Initialize(runtime);
        character.SetGridIndex(12);

        SkillMasterData moveSkill = new()
        {
            SkillId = "S_Move_Test",
            RangeType = RangeType.Selection
        };

        PlayerReservedCommand move1 = new(runtime, moveSkill);
        move1.SetSelectionResult(BattleDirection.Right, 17, new List<int> { 17 }, Vector2Int.right);
        PlayerReservedCommand move2 = new(runtime, moveSkill);
        move2.SetSelectionResult(BattleDirection.Right, 22, new List<int> { 22 }, Vector2Int.right);
        PlayerReservedCommand move3 = new(runtime, moveSkill);
        move3.SetSelectionResult(BattleDirection.Right, 27, new List<int> { 27 }, Vector2Int.right);

        PlayerReservedCommand futureMove = new(runtime, moveSkill);
        futureMove.SetSelectionResult(BattleDirection.Left, 22, new List<int> { 22 }, Vector2Int.left);

        Assert.That(slot0.AddCommand(move1), Is.True);
        Assert.That(slot0.AddCommand(move2), Is.True);
        Assert.That(slot0.AddCommand(move3), Is.True);
        Assert.That(slot2.AddCommand(futureMove), Is.True);

        int slot1StartGrid = timeline.GetPreviewGridIndexAtSlotEnd(runtime, 1);

        Object.DestroyImmediate(character.gameObject);
        Object.DestroyImmediate(timelineObject);
        Object.DestroyImmediate(slot0.gameObject);
        Object.DestroyImmediate(slot1.gameObject);
        Object.DestroyImmediate(slot2.gameObject);

        Assert.That(slot1StartGrid, Is.EqualTo(27));
    }

    [Test]
    public void TimelineLastMoveGhostPreviewResult_UsesSlotOrderNotReservationOrder()
    {
        GameObject timelineObject = new("TimelineGhostOrder");
        BattleTimelineController timeline =
            timelineObject.AddComponent<BattleTimelineController>();

        ReserveTurnSlotUI slot0 = new GameObject("GhostSlot0").AddComponent<ReserveTurnSlotUI>();
        ReserveTurnSlotUI slot1 = new GameObject("GhostSlot1").AddComponent<ReserveTurnSlotUI>();
        ReserveTurnSlotUI slot2 = new GameObject("GhostSlot2").AddComponent<ReserveTurnSlotUI>();
        ReserveTurnSlotUI[] slots = { slot0, slot1, slot2 };

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

        PlayerReservedCommand slot0Move = new(runtime, moveSkill);
        slot0Move.SetSelectionResult(BattleDirection.Right, 17, new List<int> { 17 }, Vector2Int.right);

        PlayerReservedCommand slot2Move = new(runtime, moveSkill);
        slot2Move.SetSelectionResult(BattleDirection.Right, 22, new List<int> { 22 }, Vector2Int.right);

        PlayerReservedCommand slot1Move = new(runtime, moveSkill);
        slot1Move.SetSelectionResult(BattleDirection.Left, 12, new List<int> { 12 }, Vector2Int.left);

        Assert.That(slot0.AddCommand(slot0Move), Is.True);
        Assert.That(slot2.AddCommand(slot2Move), Is.True);
        Assert.That(slot1.AddCommand(slot1Move), Is.True);

        bool found = timeline.TryGetLastMoveGhostPreviewResult(
            runtime,
            out int ghostGridIndex,
            out BattleDirection ghostDirection
        );

        Object.DestroyImmediate(timelineObject);
        Object.DestroyImmediate(slot0.gameObject);
        Object.DestroyImmediate(slot1.gameObject);
        Object.DestroyImmediate(slot2.gameObject);

        Assert.That(found, Is.True);
        Assert.That(ghostGridIndex, Is.EqualTo(22));
        Assert.That(ghostDirection, Is.EqualTo(BattleDirection.Right));
    }

    [Test]
    public void VisualSkipMove_IsConsumedOnlyAtReservedTarget()
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
        command.SetSelectionResult(
            BattleDirection.Right,
            16,
            new List<int> { 16 },
            Vector2Int.right
        );
        command.SetSkipMoveVisual(true);
        command.SetSimulatedMoveResult(true, 11, Vector2Int.zero);

        Assert.That(command.EffectiveMoveGridIndex, Is.EqualTo(11));
        Assert.That(command.IsVisualSkipConsumedAtGrid(11), Is.False);
        Assert.That(command.IsVisualSkipConsumedAtGrid(16), Is.True);
        Assert.That(command.ExecutionMoveOffset, Is.EqualTo(Vector2Int.right));
        Assert.That(command.PreviewMoveGridIndex, Is.EqualTo(16));
    }

    [Test]
    public void TimelineLastMoveGhostPreviewResult_UsesReservedTargetForVisualSkipMove()
    {
        GameObject timelineObject = new("TimelineGhostVisualSkip");
        BattleTimelineController timeline =
            timelineObject.AddComponent<BattleTimelineController>();

        ReserveTurnSlotUI slot0 = new GameObject("GhostVisualSkipSlot0").AddComponent<ReserveTurnSlotUI>();
        ReserveTurnSlotUI[] slots = { slot0 };

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

        PlayerReservedCommand skippedAxisMove = new(runtime, moveSkill);
        skippedAxisMove.SetSelectionResult(
            BattleDirection.Right,
            16,
            new List<int> { 16 },
            Vector2Int.right
        );
        skippedAxisMove.SetSkipMoveVisual(true);
        skippedAxisMove.SetSimulatedMoveResult(true, 11, Vector2Int.zero);

        Assert.That(slot0.AddCommand(skippedAxisMove), Is.True);

        bool found = timeline.TryGetLastMoveGhostPreviewResult(
            runtime,
            out int ghostGridIndex,
            out BattleDirection ghostDirection
        );

        Object.DestroyImmediate(timelineObject);
        Object.DestroyImmediate(slot0.gameObject);

        Assert.That(found, Is.True);
        Assert.That(ghostGridIndex, Is.EqualTo(16));
        Assert.That(ghostDirection, Is.EqualTo(BattleDirection.Right));
    }

    [Test]
    public void BattleOccupancyService_IgnoresDeadMonsters()
    {
        MonsterMasterData masterData = new()
        {
            MonsterId = "Mon_Test",
            Name = "DeadMonster",
            Health = 1
        };

        MonsterRuntimeData runtime = new("M_Dead", masterData);
        runtime.TakeDamage(1);

        MonsterUnit monster =
            new GameObject("DeadMonster").AddComponent<MonsterUnit>();
        monster.Initialize(runtime);
        monster.SetOccupiedCells(new List<int> { 16 });

        bool occupied = BattleOccupancyService.IsOccupiedByAnyUnit(16);

        Object.DestroyImmediate(monster.gameObject);

        Assert.That(runtime.IsDead, Is.True);
        Assert.That(occupied, Is.False);
    }

    [Test]
    public void BattleActionSimulation_IgnoresMonsterFutureMoveTargetForPlayerReservation()
    {
        GameObject timelineObject = new("TimelineFutureMonsterMove");
        BattleTimelineController timeline =
            timelineObject.AddComponent<BattleTimelineController>();
        ReserveTurnSlotUI slot0 = new GameObject("FutureMonsterMoveSlot0").AddComponent<ReserveTurnSlotUI>();
        ReserveTurnSlotUI[] slots = { slot0 };

        typeof(BattleTimelineController)
            .GetField("reserveSlots", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(timeline, slots);

        MethodInfo initializeMonsterSlots = typeof(BattleTimelineController).GetMethod(
            "InitializeMonsterCommandSlots",
            BindingFlags.Instance | BindingFlags.NonPublic);
        initializeMonsterSlots.Invoke(timeline, null);

        CharacterRuntimeData playerRuntime = new()
        {
            CharacterId = "Char_Test",
            Direction = BattleDirection.Right
        };

        BattleCharacter player =
            new GameObject("FutureMonsterMovePlayer").AddComponent<BattleCharacter>();
        player.Initialize(playerRuntime);
        player.SetGridIndex(10);

        SkillMasterData moveSkill = new()
        {
            SkillId = "S_Move_Test",
            RangeType = RangeType.Selection
        };

        PlayerReservedCommand playerMove = new(playerRuntime, moveSkill);
        playerMove.SetSelectionResult(
            BattleDirection.Right,
            20,
            new List<int> { 20 },
            new Vector2Int(2, 0)
        );
        Assert.That(slot0.AddCommand(playerMove), Is.True);

        MonsterMasterData monsterMasterData = new()
        {
            MonsterId = "Mon_Test",
            Name = "MovingMonster",
            Health = 10
        };
        MonsterRuntimeData monsterRuntime = new("M_Move", monsterMasterData);
        MonsterUnit monster =
            new GameObject("FutureMonsterMoveMonster").AddComponent<MonsterUnit>();
        monster.Initialize(monsterRuntime);
        monster.SetOccupiedCells(new List<int> { 21 });

        MonsterSkillData monsterMoveSkill = new()
        {
            SkillId = "S_Monster_Move_Test",
            TimelineNotation = TimelineActionType.Move
        };
        MonsterReservedCommand monsterMove = new(monsterRuntime, monsterMoveSkill);
        monsterMove.SetMoveOffset(Vector2Int.down);

        List<MonsterReservedCommand>[] monsterCommandsBySlot =
            (List<MonsterReservedCommand>[])typeof(BattleTimelineController)
                .GetField("monsterCommandsBySlot", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(timeline);
        monsterCommandsBySlot[0].Add(monsterMove);

        GridManager gridManager =
            new GameObject("FutureMonsterMoveGrid").AddComponent<GridManager>();
        BattleActionSimulationService simulator = new(gridManager);
        simulator.Simulate(timeline);

        Object.DestroyImmediate(player.gameObject);
        Object.DestroyImmediate(monster.gameObject);
        Object.DestroyImmediate(gridManager.gameObject);
        Object.DestroyImmediate(timelineObject);
        Object.DestroyImmediate(slot0.gameObject);

        Assert.That(monsterMove.IsSimulatedMoveBlocked, Is.False);
        Assert.That(playerMove.IsSimulatedMoveBlocked, Is.False);
        Assert.That(playerMove.EffectiveMoveGridIndex, Is.EqualTo(20));
        Assert.That(playerMove.EffectiveMoveOffset, Is.EqualTo(new Vector2Int(2, 0)));
    }

    [Test]
    public void BattleActionRunner_PlayerMoveTargetStopsBeforeRuntimeBlocker()
    {
        GridManager gridManager =
            new GameObject("RuntimeBlockGrid").AddComponent<GridManager>();

        MonsterMasterData monsterMasterData = new()
        {
            MonsterId = "Mon_Test",
            Name = "RuntimeBlockMonster",
            Health = 10
        };
        MonsterRuntimeData monsterRuntime = new("M_Block", monsterMasterData);
        MonsterUnit monster =
            new GameObject("RuntimeBlockMonster").AddComponent<MonsterUnit>();
        monster.Initialize(monsterRuntime);
        monster.SetOccupiedCells(new List<int> { 20 });

        BattleActionRunner runner = new(gridManager);
        MethodInfo method = typeof(BattleActionRunner).GetMethod(
            "TryGetPlayerMoveTargetGridIndex",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[]
            {
                typeof(int),
                typeof(Vector2Int),
                typeof(string),
                typeof(int).MakeByRefType()
            },
            null);

        object[] args =
        {
            10,
            new Vector2Int(2, 0),
            "Char_Test",
            0
        };

        bool found = (bool)method.Invoke(runner, args);
        int targetGridIndex = (int)args[3];

        Object.DestroyImmediate(monster.gameObject);
        Object.DestroyImmediate(gridManager.gameObject);

        Assert.That(found, Is.True);
        Assert.That(targetGridIndex, Is.EqualTo(15));
    }

    [Test]
    public void BattleActionRunner_UsesFastMovementDuration()
    {
        Assert.That(BattleActionRunner.MoveAnimationDuration, Is.GreaterThan(0f));
        Assert.That(BattleActionRunner.MoveAnimationDuration, Is.LessThan(0.25f));
    }

    [Test]
    public void EquipmentBattleStart_AppliesRuneAndRelicStatBonuses()
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_01",
            MaxHealth = 114,
            CurrentHealth = 114,
            EquippedRuneIds = new[] { "Rune_01", "Rune_16", "Rune_20", "Rune_25" },
            EquippedRelicIds = new[] { "Relic_01", "Relic_08", "Relic_09", "Relic_10" }
        };

        CharacterMasterData master = new()
        {
            CharacterId = "Char_01",
            MaxHealth = 114,
            MaxStamina = 8,
            MaxResource = 3,
            MoveValue = 12
        };

        BattleEquipmentEffectService.ApplyBattleStartEffects(runtime, master);

        Assert.That(runtime.MaxHealth, Is.EqualTo(125));
        Assert.That(runtime.CurrentHealth, Is.EqualTo(125));
        Assert.That(runtime.MaxStamina, Is.EqualTo(11));
        Assert.That(runtime.CurrentStamina, Is.EqualTo(13));
        Assert.That(runtime.CurrentResource, Is.EqualTo(3));
        Assert.That(runtime.CurrentMoveLevel, Is.EqualTo(23));
    }

    [Test]
    public void AllCurrentUniqueResourceRune_LowersMinimumCostToOne()
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_01",
            CurrentResource = 1,
            EquippedRuneIds = new[] { "Rune_05" }
        };

        SkillMasterData skill = new()
        {
            SkillId = "S_AllCurrent_Test",
            ReferenceResource = ReferenceResource.UniqueResource,
            ResourceCostType = ResourceCostType.AllCurrent,
            ResourceCostValue = 2
        };

        bool canPay = SkillCostCalculator.TryGetPreviewPayAmount(
            runtime,
            skill,
            out int payAmount);

        Assert.That(canPay, Is.True);
        Assert.That(payAmount, Is.EqualTo(1));
    }

    [Test]
    public void EquipmentEffectValue_AppliesDamageArmorAndSlotBonuses()
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_02",
            EquippedRuneIds = new[] { "Rune_09" },
            EquippedRelicIds = new[] { "Relic_03" }
        };

        SkillMasterData attackSkill = new()
        {
            SkillId = "S_Attack_Test",
            SkillType = SkillType.Attack
        };

        PlayerReservedCommand command = new(runtime, attackSkill);
        command.SetTimelineSlotIndex(4);

        SkillEffectEntry strike = new()
        {
            EffectId = "E_Strike",
            ValueAmount = 5,
            CountAmount = 1
        };

        int value = BattleEquipmentEffectService.ModifyPlayerEffectValue(
            runtime,
            command,
            strike,
            5);

        Assert.That(value, Is.EqualTo(8));
    }

    [Test]
    public void EquipmentRangeRune_ExpandsChar03FrontFourToFrontSix()
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_03",
            EquippedRuneIds = new[] { "Rune_14" }
        };

        SkillMasterData skill = new()
        {
            RangeId = "Range_21"
        };

        string rangeId = BattleEquipmentEffectService.GetEffectiveRangeId(runtime, skill);

        Assert.That(rangeId, Is.EqualTo("Range_18"));
    }

    [Test]
    public void PlayerReservedCommand_CostModifiersCanResetToBaseCost()
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Test",
            CurrentStamina = 3,
            EquippedRuneIds = new[] { "Rune_24" }
        };

        SkillMasterData moveSkill = new()
        {
            SkillId = "S_Move_Test",
            Category = Category.Move,
            ReferenceResource = ReferenceResource.Stamina,
            ResourceCostType = ResourceCostType.Fixed,
            ResourceCostValue = 1
        };

        PlayerReservedCommand command = new(runtime, moveSkill);

        BattleEquipmentEffectService.ApplyReservationCostModifiers(
            command,
            0,
            true,
            false);

        Assert.That(command.StaminaCost, Is.EqualTo(0));

        command.ResetCostsToBase();

        Assert.That(command.StaminaCost, Is.EqualTo(1));
    }

    [Test]
    public void Relic06_AppliesArmorOnlyAtSecondPlayerTurnStart()
    {
        CharacterRuntimeData runtime = new()
        {
            CharacterId = "Char_Test",
            CurrentShield = 0,
            EquippedRelicIds = new[] { "Relic_06" }
        };

        BattleEquipmentEffectService.ApplyPlayerTurnStartEffects(runtime, 1);
        Assert.That(runtime.CurrentShield, Is.EqualTo(0));

        BattleEquipmentEffectService.ApplyPlayerTurnStartEffects(runtime, 2);
        Assert.That(runtime.CurrentShield, Is.EqualTo(10));

        BattleEquipmentEffectService.ApplyPlayerTurnStartEffects(runtime, 3);
        Assert.That(runtime.CurrentShield, Is.EqualTo(10));
    }

    private static MonsterUnit CreateMonsterWithHud(
        string name,
        string runtimeId,
        int occupiedGridIndex,
        out CanvasGroup canvasGroup)
    {
        MonsterMasterData masterData = new()
        {
            MonsterId = name,
            Name = name,
            Health = 10
        };

        MonsterRuntimeData runtimeData = new(runtimeId, masterData);
        MonsterUnit monster = new GameObject(name).AddComponent<MonsterUnit>();
        monster.Initialize(runtimeData);
        monster.SetOccupiedCells(new List<int> { occupiedGridIndex });

        GameObject hudObject = new(name + "HUD");
        canvasGroup = hudObject.AddComponent<CanvasGroup>();
        MonsterHUDSlot hud = hudObject.AddComponent<MonsterHUDSlot>();
        monster.BindHUD(hud);

        return monster;
    }

    private static GridManager CreateOneCellGrid(
        string name,
        out Renderer cellRenderer,
        out Collider cellCollider,
        out GameObject gridObject,
        out GameObject cellObject)
    {
        gridObject = new GameObject(name);
        GridManager gridManager = gridObject.AddComponent<GridManager>();

        cellObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cellObject.name = name + "_Cell";
        cellObject.transform.SetParent(gridObject.transform);

        GridCell cell = cellObject.AddComponent<GridCell>();
        cell.Initialize(gridManager, 0, 0, 0);

        typeof(GridManager)
            .GetField("width", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(gridManager, 1);
        typeof(GridManager)
            .GetField("height", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(gridManager, 1);
        typeof(GridManager)
            .GetField("cells", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(gridManager, new[] { cell });

        cellRenderer = cellObject.GetComponent<Renderer>();
        cellCollider = cellObject.GetComponent<Collider>();

        return gridManager;
    }

    private static MonsterUnit CreateMonsterWithSprite(
        string name,
        string runtimeId,
        out SpriteRenderer spriteRenderer,
        out Collider2D clickCollider)
    {
        MonsterMasterData masterData = new()
        {
            MonsterId = name,
            Name = name,
            Health = 10
        };

        MonsterRuntimeData runtimeData = new(runtimeId, masterData);
        MonsterUnit monster = new GameObject(name).AddComponent<MonsterUnit>();
        spriteRenderer = monster.gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.color = Color.white;
        clickCollider = monster.gameObject.AddComponent<BoxCollider2D>();
        monster.Initialize(runtimeData);

        return monster;
    }
}
