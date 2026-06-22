//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Reflection;
//using NUnit.Framework;
//using Relic.Gameplay.Data;
//using Relic.Gameplay.Monster;
//using UnityEngine;

//public class BattleActionRegressionTests
//{
//    [Test]
//    public void MonsterClick_DoesNotSelectMonsterForHud()
//    {
//        MonsterUnit monster = CreateMonsterWithHud(
//            "ClickNoHudMonster",
//            "M_ClickNoHud",
//            12,
//            out CanvasGroup canvasGroup
//        );

//        FieldInfo selectedMonsterField = typeof(MonsterUnit).GetField(
//            "selectedMonster",
//            BindingFlags.Static | BindingFlags.NonPublic);
//        selectedMonsterField.SetValue(null, null);

//        MethodInfo mouseDownMethod = typeof(MonsterUnit).GetMethod(
//            "OnMouseDown",
//            BindingFlags.Instance | BindingFlags.NonPublic);

//        mouseDownMethod.Invoke(monster, null);

//        Assert.That(selectedMonsterField.GetValue(null), Is.Null);
//        Assert.That(canvasGroup.alpha, Is.EqualTo(0f));

//        Object.DestroyImmediate(canvasGroup.gameObject);
//        Object.DestroyImmediate(monster.gameObject);
//    }

//    [Test]
//    public void MonsterHover_ShowsAndHidesBoundHud()
//    {
//        MonsterUnit monster = CreateMonsterWithHud(
//            "HoverHudMonster",
//            "M_HoverHud",
//            12,
//            out CanvasGroup canvasGroup
//        );

//        MethodInfo mouseEnterMethod = typeof(MonsterUnit).GetMethod(
//            "OnMouseEnter",
//            BindingFlags.Instance | BindingFlags.NonPublic);
//        MethodInfo mouseExitMethod = typeof(MonsterUnit).GetMethod(
//            "OnMouseExit",
//            BindingFlags.Instance | BindingFlags.NonPublic);

//        Assert.That(mouseEnterMethod, Is.Not.Null);
//        Assert.That(mouseExitMethod, Is.Not.Null);
//        Assert.That(canvasGroup.alpha, Is.EqualTo(0f));

//        mouseEnterMethod.Invoke(monster, null);
//        Assert.That(canvasGroup.alpha, Is.EqualTo(1f));

//        mouseExitMethod.Invoke(monster, null);
//        Assert.That(canvasGroup.alpha, Is.EqualTo(0f));

//        Object.DestroyImmediate(canvasGroup.gameObject);
//        Object.DestroyImmediate(monster.gameObject);
//    }

//    [Test]
//    public void TemporaryMonsterHudRange_ShowsMatchingMonstersAndCanBeCleared()
//    {
//        MonsterUnit inRangeMonster = CreateMonsterWithHud(
//            "TemporaryHudInRange",
//            "M_TemporaryInRange",
//            12,
//            out CanvasGroup inRangeCanvasGroup
//        );
//        MonsterUnit outOfRangeMonster = CreateMonsterWithHud(
//            "TemporaryHudOutOfRange",
//            "M_TemporaryOutOfRange",
//            22,
//            out CanvasGroup outOfRangeCanvasGroup
//        );

//        MethodInfo showTemporaryMethod = typeof(MonsterUnit).GetMethod(
//            "ShowTemporaryHUDsInRange",
//            BindingFlags.Static | BindingFlags.Public);
//        MethodInfo hideTemporaryMethod = typeof(MonsterUnit).GetMethod(
//            "HideAllTemporaryHUDs",
//            BindingFlags.Static | BindingFlags.Public);

//        Assert.That(showTemporaryMethod, Is.Not.Null);
//        Assert.That(hideTemporaryMethod, Is.Not.Null);

//        showTemporaryMethod.Invoke(null, new object[] { new List<int> { 12 }, 1f });

//        Assert.That(inRangeCanvasGroup.alpha, Is.EqualTo(1f));
//        Assert.That(outOfRangeCanvasGroup.alpha, Is.EqualTo(0f));

//        hideTemporaryMethod.Invoke(null, null);

//        Assert.That(inRangeCanvasGroup.alpha, Is.EqualTo(0f));
//        Assert.That(outOfRangeCanvasGroup.alpha, Is.EqualTo(0f));

//        Object.DestroyImmediate(inRangeCanvasGroup.gameObject);
//        Object.DestroyImmediate(outOfRangeCanvasGroup.gameObject);
//        Object.DestroyImmediate(inRangeMonster.gameObject);
//        Object.DestroyImmediate(outOfRangeMonster.gameObject);
//    }

//    [Test]
//    public void AddStatusToMonster_ShowsBoundHud()
//    {
//        MonsterUnit monster = CreateMonsterWithHud(
//            "StatusHudMonster",
//            "M_StatusHud",
//            12,
//            out CanvasGroup canvasGroup
//        );

//        BattleEffectUtility.AddStatusToMonster(monster, "E_Weaken", 1, 1);

//        Assert.That(monster.RuntimeData.StatusEffects, Has.Count.EqualTo(1));
//        Assert.That(monster.RuntimeData.StatusEffects[0].EffectId, Is.EqualTo("E_Weaken"));
//        Assert.That(canvasGroup.alpha, Is.EqualTo(1f));

//        Object.DestroyImmediate(canvasGroup.gameObject);
//        Object.DestroyImmediate(monster.gameObject);
//    }

//    [Test]
//    public void BurnEffectOnMonster_ShowsBoundHud()
//    {
//        MonsterUnit monster = CreateMonsterWithHud(
//            "BurnStatusHudMonster",
//            "M_BurnStatusHud",
//            12,
//            out CanvasGroup canvasGroup
//        );

//        BurnEffect effect = new();
//        effect.Execute(new BattleEffectContext
//        {
//            MonsterTarget = monster,
//            Value = 2
//        });

//        Assert.That(monster.RuntimeData.StatusEffects, Has.Count.EqualTo(1));
//        Assert.That(monster.RuntimeData.StatusEffects[0].EffectId, Is.EqualTo("E_Burn"));
//        Assert.That(canvasGroup.alpha, Is.EqualTo(1f));

//        Object.DestroyImmediate(canvasGroup.gameObject);
//        Object.DestroyImmediate(monster.gameObject);
//    }

//    [Test]
//    public void StrikeEffect_ExecuteAppliesOnlyOneHitEvenWhenContextCountIsThree()
//    {
//        MonsterUnit monster = CreateDamageTargetMonster(
//            "StrikeSingleHitMonster",
//            "M_StrikeSingleHit",
//            30);

//        StrikeEffect effect = new();
//        effect.Execute(new BattleEffectContext
//        {
//            MonsterTarget = monster,
//            Value = 5,
//            Count = 3
//        });

//        Assert.That(monster.RuntimeData.CurrentHP, Is.EqualTo(25));

//        Object.DestroyImmediate(monster.gameObject);
//    }

//    [Test]
//    public void PierceEffect_ExecuteAppliesOnlyOneHitEvenWhenContextCountIsThree()
//    {
//        MonsterUnit monster = CreateDamageTargetMonster(
//            "PierceSingleHitMonster",
//            "M_PierceSingleHit",
//            30);

//        PierceEffect effect = new();
//        effect.Execute(new BattleEffectContext
//        {
//            MonsterTarget = monster,
//            Value = 5,
//            Count = 3
//        });

//        Assert.That(monster.RuntimeData.CurrentHP, Is.EqualTo(25));

//        Object.DestroyImmediate(monster.gameObject);
//    }

//    [Test]
//    public void TurnEndAddictedMonsterDamage_HidesHudBeforeNextReservation()
//    {
//        MonsterUnit monster = CreateMonsterWithHud(
//            "TurnEndAddictedMonster",
//            "M_TurnEndAddicted",
//            12,
//            out CanvasGroup canvasGroup
//        );
//        monster.RuntimeData.StatusEffects.Add(new StatusEffectRuntimeData
//        {
//            EffectId = "E_Addicted",
//            Stack = 1,
//            TurnCount = 1
//        });

//        BattleActionRunner runner = new(null);
//        MethodInfo method = typeof(BattleActionRunner).GetMethod(
//            "ApplyTurnEndEffectsRoutine",
//            BindingFlags.Instance | BindingFlags.Public);

//        Assert.That(method, Is.Not.Null);

//        IEnumerator routine = (IEnumerator)method.Invoke(runner, null);

//        Assert.That(routine.MoveNext(), Is.True);
//        Assert.That(routine.Current, Is.TypeOf<WaitForSeconds>());
//        Assert.That(monster.RuntimeData.CurrentHP, Is.EqualTo(monster.RuntimeData.MaxHP - 1));
//        Assert.That(canvasGroup.alpha, Is.EqualTo(1f));

//        Assert.That(routine.MoveNext(), Is.True);
//        Assert.That(routine.Current, Is.TypeOf<WaitForSeconds>());
//        Assert.That(canvasGroup.alpha, Is.EqualTo(0f));

//        Object.DestroyImmediate(canvasGroup.gameObject);
//        Object.DestroyImmediate(monster.gameObject);
//    }

//    [Test]
//    public void GridVisibility_TogglesCellRendererAndCollider()
//    {
//        GridManager gridManager = CreateOneCellGrid(
//            "GridVisibility",
//            out Renderer cellRenderer,
//            out Collider cellCollider,
//            out GameObject gridObject,
//            out GameObject cellObject
//        );

//        MethodInfo method = typeof(GridManager).GetMethod(
//            "SetGridVisible",
//            BindingFlags.Instance | BindingFlags.Public);

//        Assert.That(method, Is.Not.Null);

//        method.Invoke(gridManager, new object[] { false });

//        Assert.That(cellRenderer.enabled, Is.False);
//        Assert.That(cellCollider.enabled, Is.False);

//        method.Invoke(gridManager, new object[] { true });

//        Assert.That(cellRenderer.enabled, Is.True);
//        Assert.That(cellCollider.enabled, Is.True);

//        Object.DestroyImmediate(cellObject);
//        Object.DestroyImmediate(gridObject);
//    }

//    [Test]
//    public void MonsterReservationVisualState_MakesTransparentButKeepsCollider()
//    {
//        MonsterUnit monster = CreateMonsterWithSprite(
//            "ReservationVisualMonster",
//            "M_ReservationVisual",
//            out SpriteRenderer spriteRenderer,
//            out Collider2D clickCollider
//        );

//        MethodInfo method = typeof(MonsterUnit).GetMethod(
//            "SetReservationVisualState",
//            BindingFlags.Instance | BindingFlags.Public);

//        Assert.That(method, Is.Not.Null);

//        method.Invoke(monster, new object[] { true });

//        Assert.That(spriteRenderer.color.a, Is.LessThan(1f));
//        Assert.That(clickCollider.enabled, Is.True);

//        method.Invoke(monster, new object[] { false });

//        Assert.That(spriteRenderer.color.a, Is.EqualTo(1f).Within(0.001f));
//        Assert.That(clickCollider.enabled, Is.True);

//        Object.DestroyImmediate(monster.gameObject);
//    }

//    [Test]
//    public void BattleInputReady_TogglesGridAndMonsterPresentation()
//    {
//        GridManager gridManager = CreateOneCellGrid(
//            "BattleInputGrid",
//            out Renderer cellRenderer,
//            out Collider cellCollider,
//            out GameObject gridObject,
//            out GameObject cellObject
//        );
//        MonsterUnit monster = CreateMonsterWithSprite(
//            "BattleInputMonster",
//            "M_BattleInput",
//            out SpriteRenderer spriteRenderer,
//            out Collider2D _);

//        BattleTurnExecutor executor =
//            new GameObject("BattleInputExecutor").AddComponent<BattleTurnExecutor>();

//        typeof(BattleTurnExecutor)
//            .GetField("gridManager", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(executor, gridManager);

//        executor.SetBattleInputReady(true);

//        Assert.That(cellRenderer.enabled, Is.True);
//        Assert.That(cellCollider.enabled, Is.True);
//        Assert.That(spriteRenderer.color.a, Is.LessThan(1f));

//        executor.SetBattleInputReady(false);

//        Assert.That(cellRenderer.enabled, Is.False);
//        Assert.That(cellCollider.enabled, Is.False);
//        Assert.That(spriteRenderer.color.a, Is.EqualTo(1f).Within(0.001f));

//        Object.DestroyImmediate(executor.gameObject);
//        Object.DestroyImmediate(monster.gameObject);
//        Object.DestroyImmediate(cellObject);
//        Object.DestroyImmediate(gridObject);
//    }

//    [Test]
//    public void PartyRuntimeStore_AllowsFullBattleGridCurrentIndex()
//    {
//        PartyRuntimeStore store = new();

//        Assert.That(store.SetCharacter(0, "Char_Test"), Is.True);
//        Assert.That(store.SetCurrentGridIndex(0, 34), Is.True);
//        Assert.That(store.GetCurrentGridIndex(0), Is.EqualTo(34));
//    }

//    [Test]
//    public void PlayerMoveConflictInfo_UsesSimulatedMoveResult()
//    {
//        CharacterRuntimeData runtime = new()
//        {
//            CharacterId = "Char_Test",
//            CurrentMoveLevel = 3
//        };

//        SkillMasterData moveSkill = new()
//        {
//            SkillId = "S_Move_Test",
//            RangeType = RangeType.Selection
//        };

//        PlayerReservedCommand command = new(runtime, moveSkill);
//        command.SetSelectionResult(BattleDirection.Right, 10, new List<int> { 10 }, Vector2Int.right);
//        command.SetSimulatedMoveResult(false, 12, new Vector2Int(2, 0));

//        BattleActionBatchBuilder builder = new(null);
//        MethodInfo method = typeof(BattleActionBatchBuilder).GetMethod(
//            "CreatePlayerActionInfo",
//            BindingFlags.Instance | BindingFlags.NonPublic);

//        object actionInfo = method.Invoke(builder, new object[] { command });
//        FieldInfo moveTargetCellsField = actionInfo.GetType().GetField("MoveTargetCells");
//        List<int> moveTargetCells = (List<int>)moveTargetCellsField.GetValue(actionInfo);

//        Assert.That(moveTargetCells, Is.EquivalentTo(new[] { 12 }));
//    }

//    [Test]
//    public void SelfMoveSelection_FlipsCurrentDirection()
//    {
//        GameObject gameObject = new("ReservationController");
//        PlayerSkillReservationController controller =
//            gameObject.AddComponent<PlayerSkillReservationController>();

//        GridManager gridManager = new GameObject("GridManager").AddComponent<GridManager>();
//        CharacterRuntimeData runtime = new()
//        {
//            CharacterId = "Char_Test",
//            Direction = BattleDirection.Right
//        };

//        typeof(PlayerSkillReservationController)
//            .GetField("gridManager", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(controller, gridManager);

//        typeof(PlayerSkillReservationController)
//            .GetField("currentUserRuntime", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(controller, runtime);

//        MethodInfo method = typeof(PlayerSkillReservationController).GetMethod(
//            "GetDirectionFromMove",
//            BindingFlags.Instance | BindingFlags.NonPublic);

//        BattleDirection direction =
//            (BattleDirection)method.Invoke(controller, new object[] { 10, 10 });

//        Object.DestroyImmediate(gameObject);
//        Object.DestroyImmediate(gridManager.gameObject);

//        Assert.That(direction, Is.EqualTo(BattleDirection.Left));
//    }

//    [Test]
//    public void SelfMoveSelection_FlipsPreviewDirectionWithoutMutatingRuntimeDirection()
//    {
//        GameObject gameObject = new("ReservationControllerPreview");
//        PlayerSkillReservationController controller =
//            gameObject.AddComponent<PlayerSkillReservationController>();

//        GridManager gridManager = new GameObject("GridManagerPreview").AddComponent<GridManager>();
//        CharacterRuntimeData runtime = new()
//        {
//            CharacterId = "Char_Test",
//            Direction = BattleDirection.Right
//        };

//        typeof(PlayerSkillReservationController)
//            .GetField("gridManager", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(controller, gridManager);

//        typeof(PlayerSkillReservationController)
//            .GetField("currentUserRuntime", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(controller, runtime);

//        FieldInfo previewDirectionField = typeof(PlayerSkillReservationController)
//            .GetField("currentCasterDirection", BindingFlags.Instance | BindingFlags.NonPublic);

//        Assert.That(previewDirectionField, Is.Not.Null);
//        previewDirectionField.SetValue(controller, BattleDirection.Left);

//        MethodInfo method = typeof(PlayerSkillReservationController).GetMethod(
//            "GetDirectionFromMove",
//            BindingFlags.Instance | BindingFlags.NonPublic);

//        BattleDirection direction =
//            (BattleDirection)method.Invoke(controller, new object[] { 10, 10 });

//        Object.DestroyImmediate(gameObject);
//        Object.DestroyImmediate(gridManager.gameObject);

//        Assert.That(direction, Is.EqualTo(BattleDirection.Right));
//        Assert.That(runtime.Direction, Is.EqualTo(BattleDirection.Right));
//    }

//    [Test]
//    public void PlayerReservedCommand_SetMoveDirection_UpdatesDirectionOnly()
//    {
//        CharacterRuntimeData runtime = new()
//        {
//            CharacterId = "Char_Test"
//        };

//        SkillMasterData moveSkill = new()
//        {
//            SkillId = "S_Move_Test",
//            RangeType = RangeType.Selection
//        };

//        PlayerReservedCommand command = new(runtime, moveSkill);
//        command.SetSelectionResult(BattleDirection.Right, 10, new List<int> { 10 }, Vector2Int.zero);

//        MethodInfo method = typeof(PlayerReservedCommand).GetMethod(
//            "SetMoveDirection",
//            BindingFlags.Instance | BindingFlags.Public);

//        Assert.That(method, Is.Not.Null);
//        method.Invoke(command, new object[] { BattleDirection.Left });

//        Assert.That(command.Direction, Is.EqualTo(BattleDirection.Left));
//        Assert.That(command.MoveOffset, Is.EqualTo(Vector2Int.zero));
//        Assert.That(command.ReservedMoveGridIndex, Is.EqualTo(10));
//    }

//    [Test]
//    public void TimelinePreviewDirection_RecomputesAfterSelfFlipIsRemoved()
//    {
//        GameObject timelineObject = new("TimelinePreviewDirection");
//        BattleTimelineController timeline =
//            timelineObject.AddComponent<BattleTimelineController>();

//        ReserveTurnSlotUI slot0 = new GameObject("Slot0").AddComponent<ReserveTurnSlotUI>();
//        ReserveTurnSlotUI slot1 = new GameObject("Slot1").AddComponent<ReserveTurnSlotUI>();
//        ReserveTurnSlotUI[] slots = { slot0, slot1 };

//        typeof(BattleTimelineController)
//            .GetField("reserveSlots", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(timeline, slots);

//        CharacterRuntimeData runtime = new()
//        {
//            CharacterId = "Char_Test",
//            Direction = BattleDirection.Right
//        };

//        SkillMasterData moveSkill = new()
//        {
//            SkillId = "S_Move_Test",
//            RangeType = RangeType.Selection
//        };

//        PlayerReservedCommand selfFlip = new(runtime, moveSkill);
//        selfFlip.SetSelectionResult(
//            BattleDirection.Left,
//            10,
//            new List<int> { 10 },
//            Vector2Int.zero
//        );

//        Assert.That(slot0.AddCommand(selfFlip), Is.True);
//        Assert.That(timeline.GetPreviewDirection(runtime, 1), Is.EqualTo(BattleDirection.Left));

//        Assert.That(slot0.RemoveCommandAt(0, out _), Is.True);

//        PlayerReservedCommand verticalMove = new(runtime, moveSkill);
//        verticalMove.SetSelectionResult(
//            BattleDirection.Left,
//            15,
//            new List<int> { 15 },
//            Vector2Int.up
//        );

//        Assert.That(slot0.AddCommand(verticalMove), Is.True);
//        Assert.That(timeline.GetPreviewDirection(runtime, 1), Is.EqualTo(BattleDirection.Right));

//        Object.DestroyImmediate(timelineObject);
//        Object.DestroyImmediate(slot0.gameObject);
//        Object.DestroyImmediate(slot1.gameObject);
//    }

//    [Test]
//    public void MoveRangePreview_UsesCostBudgetAsManhattanCost()
//    {
//        GridManager gridManager = new GameObject("GridManagerMoveRange").AddComponent<GridManager>();

//        List<int> range = PlayerSkillReservationController.GetMoveRangeIndices(
//            12,
//            3,
//            gridManager
//        );

//        Object.DestroyImmediate(gridManager.gameObject);

//        Assert.That(range, Does.Contain(12));
//        Assert.That(range, Does.Contain(27));
//        Assert.That(range, Does.Contain(15));
//        Assert.That(range, Has.No.Member(28));
//    }

//    [Test]
//    public void MoveCost_TreatsDiagonalAsHorizontalPlusVertical()
//    {
//        int count = PlayerSkillReservationController.GetRequiredMoveReservationCount(
//            new Vector2Int(1, 1),
//            1
//        );

//        Assert.That(count, Is.EqualTo(2));
//    }

//    [Test]
//    public void MoveReservationOffsets_SplitsDiagonalIntoCardinalSteps()
//    {
//        List<Vector2Int> offsets = PlayerSkillReservationController.BuildMoveReservationOffsets(
//            new Vector2Int(1, 1),
//            1
//        );

//        Assert.That(offsets, Is.EqualTo(new List<Vector2Int>
//        {
//            Vector2Int.right,
//            Vector2Int.up
//        }));
//    }

//    [Test]
//    public void MoveReservationOffsets_UsesMoveDistanceForStraightSteps()
//    {
//        List<Vector2Int> offsets = PlayerSkillReservationController.BuildMoveReservationOffsets(
//            new Vector2Int(2, 0),
//            2
//        );

//        Assert.That(offsets, Is.EqualTo(new List<Vector2Int>
//        {
//            new Vector2Int(2, 0)
//        }));
//    }

//    [Test]
//    public void MoveRangePreview_LevelTwoMoveAllowsStraightTwoButNotDiagonalInOneReservation()
//    {
//        GridManager gridManager = new GameObject("GridManagerLevelTwoMoveRange").AddComponent<GridManager>();

//        List<int> range = PlayerSkillReservationController.GetMoveRangeIndices(
//            12,
//            1,
//            2,
//            gridManager
//        );

//        Object.DestroyImmediate(gridManager.gameObject);

//        Assert.That(range, Does.Contain(22));
//        Assert.That(range, Does.Contain(14));
//        Assert.That(range, Has.No.Member(18));
//    }

//    [Test]
//    public void TimelineRemainingPlayerCommandCapacity_SubtractsExistingPlayerCommandsFromFiveSlotCapacity()
//    {
//        GameObject timelineObject = new("TimelineCapacity");
//        BattleTimelineController timeline =
//            timelineObject.AddComponent<BattleTimelineController>();

//        ReserveTurnSlotUI slot0 = new GameObject("SlotCapacity0").AddComponent<ReserveTurnSlotUI>();
//        ReserveTurnSlotUI[] slots = { slot0 };

//        typeof(BattleTimelineController)
//            .GetField("reserveSlots", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(timeline, slots);

//        CharacterRuntimeData runtime = new()
//        {
//            CharacterId = "Char_Test"
//        };

//        SkillMasterData skill = new()
//        {
//            SkillId = "S_Test"
//        };

//        Assert.That(slot0.AddCommand(new PlayerReservedCommand(runtime, skill)), Is.True);

//        int remainingCapacity = timeline.GetRemainingPlayerCommandCapacity(0);

//        Object.DestroyImmediate(timelineObject);
//        Object.DestroyImmediate(slot0.gameObject);

//        Assert.That(remainingCapacity, Is.EqualTo(4));
//    }

//    [Test]
//    public void TimelineCombinedSlotCapacity_AllowsFivePlayerCommandsWhenNoMonsterActions()
//    {
//        GameObject timelineObject = new("TimelineFivePlayerCapacity");
//        BattleTimelineController timeline =
//            timelineObject.AddComponent<BattleTimelineController>();

//        ReserveTurnSlotUI slot0 = new GameObject("SlotFivePlayerCapacity0").AddComponent<ReserveTurnSlotUI>();
//        ReserveTurnSlotUI[] slots = { slot0 };

//        typeof(BattleTimelineController)
//            .GetField("reserveSlots", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(timeline, slots);

//        CharacterRuntimeData runtime = new()
//        {
//            CharacterId = "Char_Test",
//            CurrentCost = 100,
//            MaxCost = 100
//        };

//        SkillMasterData skill = new()
//        {
//            SkillId = "S_Test"
//        };

//        for (int i = 0; i < 5; i++)
//            Assert.That(timeline.ConfirmPlayerCommand(0, new PlayerReservedCommand(runtime, skill)), Is.True);

//        Assert.That(timeline.GetRemainingPlayerCommandCapacity(0), Is.EqualTo(0));
//        Assert.That(timeline.ConfirmPlayerCommand(0, new PlayerReservedCommand(runtime, skill)), Is.False);

//        Object.DestroyImmediate(timelineObject);
//        Object.DestroyImmediate(slot0.gameObject);
//    }

//    [Test]
//    public void TimelineCombinedSlotCapacity_SubtractsMonsterCommandsFromPlayerCapacity()
//    {
//        GameObject timelineObject = new("TimelineMonsterPlayerCapacity");
//        BattleTimelineController timeline =
//            timelineObject.AddComponent<BattleTimelineController>();

//        ReserveTurnSlotUI slot0 = new GameObject("SlotMonsterPlayerCapacity0").AddComponent<ReserveTurnSlotUI>();
//        ReserveTurnSlotUI[] slots = { slot0 };

//        typeof(BattleTimelineController)
//            .GetField("reserveSlots", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(timeline, slots);

//        timeline.AddMonsterCommand(0, CreateMonsterReservedCommand("Monster_Runtime_01"));
//        timeline.AddMonsterCommand(0, CreateMonsterReservedCommand("Monster_Runtime_01"));

//        Assert.That(timeline.GetMonsterCommands(0), Has.Count.EqualTo(2));
//        Assert.That(timeline.GetRemainingPlayerCommandCapacity(0), Is.EqualTo(3));

//        CharacterRuntimeData runtime = new()
//        {
//            CharacterId = "Char_Test",
//            CurrentCost = 100,
//            MaxCost = 100
//        };

//        SkillMasterData skill = new()
//        {
//            SkillId = "S_Test"
//        };

//        for (int i = 0; i < 3; i++)
//            Assert.That(timeline.ConfirmPlayerCommand(0, new PlayerReservedCommand(runtime, skill)), Is.True);

//        Assert.That(timeline.ConfirmPlayerCommand(0, new PlayerReservedCommand(runtime, skill)), Is.False);

//        Object.DestroyImmediate(timelineObject);
//        Object.DestroyImmediate(slot0.gameObject);
//    }

//    [Test]
//    public void TimelineMonsterSlot_AllowsOnlyOneMonsterRuntimePerSlot()
//    {
//        GameObject timelineObject = new("TimelineSingleMonsterPerSlot");
//        BattleTimelineController timeline =
//            timelineObject.AddComponent<BattleTimelineController>();

//        ReserveTurnSlotUI slot0 = new GameObject("SingleMonsterSlot0").AddComponent<ReserveTurnSlotUI>();
//        ReserveTurnSlotUI slot1 = new GameObject("SingleMonsterSlot1").AddComponent<ReserveTurnSlotUI>();
//        ReserveTurnSlotUI[] slots = { slot0, slot1 };

//        typeof(BattleTimelineController)
//            .GetField("reserveSlots", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(timeline, slots);

//        timeline.AddMonsterCommand(0, CreateMonsterReservedCommand("Monster_Runtime_01"));
//        timeline.AddMonsterCommand(0, CreateMonsterReservedCommand("Monster_Runtime_01"));
//        timeline.AddMonsterCommand(0, CreateMonsterReservedCommand("Monster_Runtime_02"));

//        Assert.That(timeline.GetMonsterCommands(0), Has.Count.EqualTo(2));
//        Assert.That(timeline.GetMonsterCommands(0)[0].RuntimeId, Is.EqualTo("Monster_Runtime_01"));
//        Assert.That(timeline.GetMonsterCommands(0)[1].RuntimeId, Is.EqualTo("Monster_Runtime_01"));
//        Assert.That(timeline.GetMonsterCommands(1), Has.Count.EqualTo(1));
//        Assert.That(timeline.GetMonsterCommands(1)[0].RuntimeId, Is.EqualTo("Monster_Runtime_02"));

//        Object.DestroyImmediate(timelineObject);
//        Object.DestroyImmediate(slot0.gameObject);
//        Object.DestroyImmediate(slot1.gameObject);
//    }

//    [Test]
//    public void MoveReservationPathCandidates_UsesOpenAxisWhenDiagonalFirstAxisIsBlocked()
//    {
//        GridManager gridManager = new GameObject("GridManagerBlockedDiagonalPath").AddComponent<GridManager>();
//        HashSet<int> blockedGridIndices = new()
//        {
//            17
//        };

//        List<List<Vector2Int>> paths =
//            PlayerSkillReservationController.GetReservableMovePathCandidates(
//                12,
//                16,
//                1,
//                2,
//                gridManager,
//                blockedGridIndices
//            );

//        Object.DestroyImmediate(gridManager.gameObject);

//        Assert.That(paths, Has.Count.EqualTo(1));
//        Assert.That(paths[0], Is.EqualTo(new List<Vector2Int>
//        {
//            Vector2Int.down,
//            Vector2Int.right
//        }));
//    }

//    [Test]
//    public void MoveReservationPathCandidates_KeepsCostPathWhenRuntimeBlockerExists()
//    {
//        GridManager gridManager = new GameObject("GridManagerMoveRuntimeBlockerPath").AddComponent<GridManager>();
//        HashSet<int> blockedGridIndices = new()
//        {
//            17
//        };

//        List<List<Vector2Int>> paths =
//            PlayerSkillReservationController.GetReservableMovePathCandidates(
//                12,
//                22,
//                2,
//                1,
//                gridManager,
//                blockedGridIndices
//            );

//        Object.DestroyImmediate(gridManager.gameObject);

//        Assert.That(paths, Has.Count.EqualTo(1));
//        Assert.That(paths[0], Is.EqualTo(new List<Vector2Int>
//        {
//            new Vector2Int(2, 0)
//        }));
//    }

//    [Test]
//    public void MoveDestinationBlockers_UseOtherPlayerReservedGridAtSelectedSlot()
//    {
//        GameObject controllerObject = new("ReservationControllerPlayerDestinationBlock");
//        PlayerSkillReservationController controller =
//            controllerObject.AddComponent<PlayerSkillReservationController>();

//        GridManager gridManager = new GameObject("GridManagerPlayerDestinationBlock").AddComponent<GridManager>();
//        GameObject timelineObject = new("TimelinePlayerDestinationBlock");
//        BattleTimelineController timeline =
//            timelineObject.AddComponent<BattleTimelineController>();

//        ReserveTurnSlotUI slot0 = new GameObject("DestinationBlockSlot0").AddComponent<ReserveTurnSlotUI>();
//        ReserveTurnSlotUI slot1 = new GameObject("DestinationBlockSlot1").AddComponent<ReserveTurnSlotUI>();
//        ReserveTurnSlotUI[] slots = { slot0, slot1 };

//        typeof(BattleTimelineController)
//            .GetField("reserveSlots", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(timeline, slots);

//        CharacterRuntimeData currentRuntime = new()
//        {
//            CharacterId = "Char_Current"
//        };

//        CharacterRuntimeData otherRuntime = new()
//        {
//            CharacterId = "Char_Other"
//        };

//        BattleCharacter otherCharacter =
//            new GameObject("OtherDestinationBlockCharacter").AddComponent<BattleCharacter>();
//        otherCharacter.Initialize(otherRuntime);
//        otherCharacter.SetGridIndex(12);

//        SkillMasterData moveSkill = new()
//        {
//            SkillId = "S_Move_Test",
//            RangeType = RangeType.Selection
//        };

//        PlayerReservedCommand otherMove = new(otherRuntime, moveSkill);
//        otherMove.SetSelectionResult(
//            BattleDirection.Right,
//            22,
//            new List<int> { 22 },
//            new Vector2Int(2, 0)
//        );

//        Assert.That(slot0.AddCommand(otherMove), Is.True);

//        typeof(PlayerSkillReservationController)
//            .GetField("gridManager", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(controller, gridManager);

//        typeof(PlayerSkillReservationController)
//            .GetField("timelineController", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(controller, timeline);

//        typeof(PlayerSkillReservationController)
//            .GetField("currentUserRuntime", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(controller, currentRuntime);

//        typeof(PlayerSkillReservationController)
//            .GetField("currentSlotIndex", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(controller, 1);

//        MethodInfo method = typeof(PlayerSkillReservationController).GetMethod(
//            "BuildKnownOtherPlayerDestinationGridIndices",
//            BindingFlags.Instance | BindingFlags.NonPublic);

//        HashSet<int> blockedDestinations =
//            (HashSet<int>)method.Invoke(controller, null);

//        Object.DestroyImmediate(otherCharacter.gameObject);
//        Object.DestroyImmediate(controllerObject);
//        Object.DestroyImmediate(gridManager.gameObject);
//        Object.DestroyImmediate(timelineObject);
//        Object.DestroyImmediate(slot0.gameObject);
//        Object.DestroyImmediate(slot1.gameObject);

//        Assert.That(blockedDestinations, Does.Contain(22));
//        Assert.That(blockedDestinations, Has.No.Member(12));
//    }

//    [Test]
//    public void MoveReservationCommands_CreatesSingleDistanceCostedCommandWithPath()
//    {
//        GameObject controllerObject = new("ReservationControllerNoDiagonalJump");
//        PlayerSkillReservationController controller =
//            controllerObject.AddComponent<PlayerSkillReservationController>();

//        GridManager gridManager = new GameObject("GridManagerNoDiagonalJump").AddComponent<GridManager>();
//        CharacterRuntimeData runtime = new()
//        {
//            CharacterId = "Char_Test",
//            Direction = BattleDirection.Right
//        };

//        SkillMasterData moveSkill = new()
//        {
//            SkillId = "S_Move_Test",
//            RangeType = RangeType.Selection,
//            ReferenceResource = ReferenceResource.Cost,
//            ResourceCostType = ResourceCostType.Fixed,
//            ResourceCostValue = 1
//        };

//        typeof(PlayerSkillReservationController)
//            .GetField("gridManager", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(controller, gridManager);

//        typeof(PlayerSkillReservationController)
//            .GetField("currentUserRuntime", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(controller, runtime);

//        typeof(PlayerSkillReservationController)
//            .GetField("currentSkillData", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(controller, moveSkill);

//        typeof(PlayerSkillReservationController)
//            .GetField("currentCasterGridIndex", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(controller, 12);

//        typeof(PlayerSkillReservationController)
//            .GetField("currentCasterDirection", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(controller, BattleDirection.Right);

//        typeof(PlayerSkillReservationController)
//            .GetField("currentMoveDistancePerCommand", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(controller, 1);

//        MethodInfo method = typeof(PlayerSkillReservationController).GetMethod(
//            "BuildMoveReservationCommands",
//            BindingFlags.Instance | BindingFlags.NonPublic);

//        List<PlayerReservedCommand> commands =
//            (List<PlayerReservedCommand>)method.Invoke(
//                controller,
//                new object[]
//                {
//                    16,
//                    new List<Vector2Int>
//                    {
//                        Vector2Int.right,
//                        Vector2Int.down
//                    }
//                });

//        Object.DestroyImmediate(controllerObject);
//        Object.DestroyImmediate(gridManager.gameObject);

//        Assert.That(commands, Has.Count.EqualTo(1));
//        Assert.That(commands[0].MoveOffset, Is.EqualTo(Vector2Int.right + Vector2Int.down));
//        Assert.That(commands[0].ReservedMoveGridIndex, Is.EqualTo(16));
//        Assert.That(commands[0].EffectiveVisualMoveGridIndex, Is.EqualTo(16));
//        Assert.That(commands[0].VisualMoveSteps, Is.EqualTo(new List<Vector2Int>
//        {
//            Vector2Int.right,
//            Vector2Int.down
//        }));
//        Assert.That(commands[0].SkipMoveVisual, Is.False);
//        Assert.That(commands[0].BaseCost, Is.EqualTo(2));
//        Assert.That(commands[0].Cost, Is.EqualTo(2));
//        Assert.That(commands[0].PlannedMoveDistance, Is.EqualTo(2));
//        Assert.That(commands[0].MoveDistancePerCost, Is.EqualTo(1));
//    }

//    [Test]
//    public void MoveBlockedRefund_FloorsHalfOfBlockedCost()
//    {
//        CharacterRuntimeData runtime = new()
//        {
//            CharacterId = "Char_Test",
//            CurrentCost = 10,
//            MaxCost = 10
//        };

//        SkillMasterData moveSkill = new()
//        {
//            SkillId = "S_Move_2",
//            RangeType = RangeType.Selection,
//            ReferenceResource = ReferenceResource.Cost,
//            ResourceCostType = ResourceCostType.Fixed,
//            ResourceCostValue = 1
//        };

//        PlayerReservedCommand command = new(runtime, moveSkill);
//        command.SetSelectionResult(
//            BattleDirection.Right,
//            0,
//            new List<int> { 0 },
//            new Vector2Int(7, 0)
//        );
//        command.SetMoveReservationCost(7, 2);
//        command.SetExecutedMoveDistance(4);

//        Assert.That(command.BaseCost, Is.EqualTo(4));
//        Assert.That(command.Cost, Is.EqualTo(4));
//        Assert.That(command.GetBlockedMoveCostRefund(), Is.EqualTo(1));

//        command.ResetCostsToBase();

//        Assert.That(command.Cost, Is.EqualTo(4));
//    }

//    [Test]
//    public void BattleStartMoveValue50_UpgradesMoveSkillToLevelTwo()
//    {
//        CharacterRuntimeData runtime = new()
//        {
//            CharacterId = "Char_Test",
//            MoveSkillId = "S_Move_1"
//        };

//        CharacterMasterData masterData = new()
//        {
//            MaxHP = 10,
//            MaxCost = 5,
//            MoveValue = 50
//        };

//        BattleEquipmentEffectService.ApplyBattleStartEffects(runtime, masterData);

//        Assert.That(runtime.MoveSkillId, Is.EqualTo("S_Move_2"));
//    }

//    [Test]
//    public void TimelinePreviewGridIndexAtSlotEnd_IgnoresFutureSlots()
//    {
//        GameObject timelineObject = new("TimelineSlotEndPreview");
//        BattleTimelineController timeline =
//            timelineObject.AddComponent<BattleTimelineController>();

//        ReserveTurnSlotUI slot0 = new GameObject("SlotEnd0").AddComponent<ReserveTurnSlotUI>();
//        ReserveTurnSlotUI slot1 = new GameObject("SlotEnd1").AddComponent<ReserveTurnSlotUI>();
//        ReserveTurnSlotUI slot2 = new GameObject("SlotEnd2").AddComponent<ReserveTurnSlotUI>();
//        ReserveTurnSlotUI[] slots = { slot0, slot1, slot2 };

//        typeof(BattleTimelineController)
//            .GetField("reserveSlots", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(timeline, slots);

//        CharacterRuntimeData runtime = new()
//        {
//            CharacterId = "Char_Test"
//        };

//        BattleCharacter character =
//            new GameObject("TimelineSlotEndCharacter").AddComponent<BattleCharacter>();
//        character.Initialize(runtime);
//        character.SetGridIndex(12);

//        SkillMasterData moveSkill = new()
//        {
//            SkillId = "S_Move_Test",
//            RangeType = RangeType.Selection
//        };

//        PlayerReservedCommand move1 = new(runtime, moveSkill);
//        move1.SetSelectionResult(BattleDirection.Right, 17, new List<int> { 17 }, Vector2Int.right);
//        PlayerReservedCommand move2 = new(runtime, moveSkill);
//        move2.SetSelectionResult(BattleDirection.Right, 22, new List<int> { 22 }, Vector2Int.right);
//        PlayerReservedCommand move3 = new(runtime, moveSkill);
//        move3.SetSelectionResult(BattleDirection.Right, 27, new List<int> { 27 }, Vector2Int.right);

//        PlayerReservedCommand futureMove = new(runtime, moveSkill);
//        futureMove.SetSelectionResult(BattleDirection.Left, 22, new List<int> { 22 }, Vector2Int.left);

//        Assert.That(slot0.AddCommand(move1), Is.True);
//        Assert.That(slot0.AddCommand(move2), Is.True);
//        Assert.That(slot0.AddCommand(move3), Is.True);
//        Assert.That(slot2.AddCommand(futureMove), Is.True);

//        int slot1StartGrid = timeline.GetPreviewGridIndexAtSlotEnd(runtime, 1);

//        Object.DestroyImmediate(character.gameObject);
//        Object.DestroyImmediate(timelineObject);
//        Object.DestroyImmediate(slot0.gameObject);
//        Object.DestroyImmediate(slot1.gameObject);
//        Object.DestroyImmediate(slot2.gameObject);

//        Assert.That(slot1StartGrid, Is.EqualTo(27));
//    }

//    [Test]
//    public void TimelineLastMoveGhostPreviewResult_UsesSlotOrderNotReservationOrder()
//    {
//        GameObject timelineObject = new("TimelineGhostOrder");
//        BattleTimelineController timeline =
//            timelineObject.AddComponent<BattleTimelineController>();

//        ReserveTurnSlotUI slot0 = new GameObject("GhostSlot0").AddComponent<ReserveTurnSlotUI>();
//        ReserveTurnSlotUI slot1 = new GameObject("GhostSlot1").AddComponent<ReserveTurnSlotUI>();
//        ReserveTurnSlotUI slot2 = new GameObject("GhostSlot2").AddComponent<ReserveTurnSlotUI>();
//        ReserveTurnSlotUI[] slots = { slot0, slot1, slot2 };

//        typeof(BattleTimelineController)
//            .GetField("reserveSlots", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(timeline, slots);

//        CharacterRuntimeData runtime = new()
//        {
//            CharacterId = "Char_Test",
//            Direction = BattleDirection.Right
//        };

//        SkillMasterData moveSkill = new()
//        {
//            SkillId = "S_Move_Test",
//            RangeType = RangeType.Selection
//        };

//        PlayerReservedCommand slot0Move = new(runtime, moveSkill);
//        slot0Move.SetSelectionResult(BattleDirection.Right, 17, new List<int> { 17 }, Vector2Int.right);

//        PlayerReservedCommand slot2Move = new(runtime, moveSkill);
//        slot2Move.SetSelectionResult(BattleDirection.Right, 22, new List<int> { 22 }, Vector2Int.right);

//        PlayerReservedCommand slot1Move = new(runtime, moveSkill);
//        slot1Move.SetSelectionResult(BattleDirection.Left, 12, new List<int> { 12 }, Vector2Int.left);

//        Assert.That(slot0.AddCommand(slot0Move), Is.True);
//        Assert.That(slot2.AddCommand(slot2Move), Is.True);
//        Assert.That(slot1.AddCommand(slot1Move), Is.True);

//        bool found = timeline.TryGetLastMoveGhostPreviewResult(
//            runtime,
//            out int ghostGridIndex,
//            out BattleDirection ghostDirection
//        );

//        Object.DestroyImmediate(timelineObject);
//        Object.DestroyImmediate(slot0.gameObject);
//        Object.DestroyImmediate(slot1.gameObject);
//        Object.DestroyImmediate(slot2.gameObject);

//        Assert.That(found, Is.True);
//        Assert.That(ghostGridIndex, Is.EqualTo(22));
//        Assert.That(ghostDirection, Is.EqualTo(BattleDirection.Right));
//    }

//    [Test]
//    public void VisualSkipMove_IsConsumedOnlyAtReservedTarget()
//    {
//        CharacterRuntimeData runtime = new()
//        {
//            CharacterId = "Char_Test"
//        };

//        SkillMasterData moveSkill = new()
//        {
//            SkillId = "S_Move_Test",
//            RangeType = RangeType.Selection
//        };

//        PlayerReservedCommand command = new(runtime, moveSkill);
//        command.SetSelectionResult(
//            BattleDirection.Right,
//            16,
//            new List<int> { 16 },
//            Vector2Int.right
//        );
//        command.SetSkipMoveVisual(true);
//        command.SetSimulatedMoveResult(true, 11, Vector2Int.zero);

//        Assert.That(command.EffectiveMoveGridIndex, Is.EqualTo(11));
//        Assert.That(command.IsVisualSkipConsumedAtGrid(11), Is.False);
//        Assert.That(command.IsVisualSkipConsumedAtGrid(16), Is.True);
//        Assert.That(command.ExecutionMoveOffset, Is.EqualTo(Vector2Int.right));
//        Assert.That(command.PreviewMoveGridIndex, Is.EqualTo(16));
//    }

//    [Test]
//    public void TimelineLastMoveGhostPreviewResult_UsesReservedTargetForVisualSkipMove()
//    {
//        GameObject timelineObject = new("TimelineGhostVisualSkip");
//        BattleTimelineController timeline =
//            timelineObject.AddComponent<BattleTimelineController>();

//        ReserveTurnSlotUI slot0 = new GameObject("GhostVisualSkipSlot0").AddComponent<ReserveTurnSlotUI>();
//        ReserveTurnSlotUI[] slots = { slot0 };

//        typeof(BattleTimelineController)
//            .GetField("reserveSlots", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(timeline, slots);

//        CharacterRuntimeData runtime = new()
//        {
//            CharacterId = "Char_Test",
//            Direction = BattleDirection.Right
//        };

//        SkillMasterData moveSkill = new()
//        {
//            SkillId = "S_Move_Test",
//            RangeType = RangeType.Selection
//        };

//        PlayerReservedCommand skippedAxisMove = new(runtime, moveSkill);
//        skippedAxisMove.SetSelectionResult(
//            BattleDirection.Right,
//            16,
//            new List<int> { 16 },
//            Vector2Int.right
//        );
//        skippedAxisMove.SetSkipMoveVisual(true);
//        skippedAxisMove.SetSimulatedMoveResult(true, 11, Vector2Int.zero);

//        Assert.That(slot0.AddCommand(skippedAxisMove), Is.True);

//        bool found = timeline.TryGetLastMoveGhostPreviewResult(
//            runtime,
//            out int ghostGridIndex,
//            out BattleDirection ghostDirection
//        );

//        Object.DestroyImmediate(timelineObject);
//        Object.DestroyImmediate(slot0.gameObject);

//        Assert.That(found, Is.True);
//        Assert.That(ghostGridIndex, Is.EqualTo(16));
//        Assert.That(ghostDirection, Is.EqualTo(BattleDirection.Right));
//    }

//    [Test]
//    public void BattleOccupancyService_IgnoresDeadMonsters()
//    {
//        MonsterMasterData masterData = new()
//        {
//            MonsterId = "Mon_Test",
//            Name = "DeadMonster",
//            HP = 1
//        };

//        MonsterRuntimeData runtime = new("M_Dead", masterData);
//        runtime.TakeDamage(1);

//        MonsterUnit monster =
//            new GameObject("DeadMonster").AddComponent<MonsterUnit>();
//        monster.Initialize(runtime);
//        monster.SetOccupiedCells(new List<int> { 16 });

//        bool occupied = BattleOccupancyService.IsOccupiedByAnyUnit(16);

//        Object.DestroyImmediate(monster.gameObject);

//        Assert.That(runtime.IsDead, Is.True);
//        Assert.That(occupied, Is.False);
//    }

//    [Test]
//    public void BattleActionSimulation_IgnoresMonsterFutureMoveTargetForPlayerReservation()
//    {
//        GameObject timelineObject = new("TimelineFutureMonsterMove");
//        BattleTimelineController timeline =
//            timelineObject.AddComponent<BattleTimelineController>();
//        ReserveTurnSlotUI slot0 = new GameObject("FutureMonsterMoveSlot0").AddComponent<ReserveTurnSlotUI>();
//        ReserveTurnSlotUI[] slots = { slot0 };

//        typeof(BattleTimelineController)
//            .GetField("reserveSlots", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(timeline, slots);

//        MethodInfo initializeMonsterSlots = typeof(BattleTimelineController).GetMethod(
//            "InitializeMonsterCommandSlots",
//            BindingFlags.Instance | BindingFlags.NonPublic);
//        initializeMonsterSlots.Invoke(timeline, null);

//        CharacterRuntimeData playerRuntime = new()
//        {
//            CharacterId = "Char_Test",
//            Direction = BattleDirection.Right
//        };

//        BattleCharacter player =
//            new GameObject("FutureMonsterMovePlayer").AddComponent<BattleCharacter>();
//        player.Initialize(playerRuntime);
//        player.SetGridIndex(10);

//        SkillMasterData moveSkill = new()
//        {
//            SkillId = "S_Move_Test",
//            RangeType = RangeType.Selection
//        };

//        PlayerReservedCommand playerMove = new(playerRuntime, moveSkill);
//        playerMove.SetSelectionResult(
//            BattleDirection.Right,
//            20,
//            new List<int> { 20 },
//            new Vector2Int(2, 0)
//        );
//        Assert.That(slot0.AddCommand(playerMove), Is.True);

//        MonsterMasterData monsterMasterData = new()
//        {
//            MonsterId = "Mon_Test",
//            Name = "MovingMonster",
//            HP = 10
//        };
//        MonsterRuntimeData monsterRuntime = new("M_Move", monsterMasterData);
//        MonsterUnit monster =
//            new GameObject("FutureMonsterMoveMonster").AddComponent<MonsterUnit>();
//        monster.Initialize(monsterRuntime);
//        monster.SetOccupiedCells(new List<int> { 21 });

//        MonsterSkillData monsterMoveSkill = new()
//        {
//            SkillId = "S_Monster_Move_Test",
//            TimelineNotation = TimelineActionType.Move
//        };
//        MonsterReservedCommand monsterMove = new(monsterRuntime, monsterMoveSkill);
//        monsterMove.SetMoveOffset(Vector2Int.down);

//        List<MonsterReservedCommand>[] monsterCommandsBySlot =
//            (List<MonsterReservedCommand>[])typeof(BattleTimelineController)
//                .GetField("monsterCommandsBySlot", BindingFlags.Instance | BindingFlags.NonPublic)
//                .GetValue(timeline);
//        monsterCommandsBySlot[0].Add(monsterMove);

//        GridManager gridManager =
//            new GameObject("FutureMonsterMoveGrid").AddComponent<GridManager>();
//        BattleActionSimulationService simulator = new(gridManager);
//        simulator.Simulate(timeline);

//        Object.DestroyImmediate(player.gameObject);
//        Object.DestroyImmediate(monster.gameObject);
//        Object.DestroyImmediate(gridManager.gameObject);
//        Object.DestroyImmediate(timelineObject);
//        Object.DestroyImmediate(slot0.gameObject);

//        Assert.That(monsterMove.IsSimulatedMoveBlocked, Is.False);
//        Assert.That(playerMove.IsSimulatedMoveBlocked, Is.False);
//        Assert.That(playerMove.EffectiveMoveGridIndex, Is.EqualTo(20));
//        Assert.That(playerMove.EffectiveMoveOffset, Is.EqualTo(new Vector2Int(2, 0)));
//    }

//    [Test]
//    public void BattleActionRunner_PlayerMoveTargetStopsBeforeRuntimeBlocker()
//    {
//        GridManager gridManager =
//            new GameObject("RuntimeBlockGrid").AddComponent<GridManager>();

//        MonsterMasterData monsterMasterData = new()
//        {
//            MonsterId = "Mon_Test",
//            Name = "RuntimeBlockMonster",
//            HP = 10
//        };
//        MonsterRuntimeData monsterRuntime = new("M_Block", monsterMasterData);
//        MonsterUnit monster =
//            new GameObject("RuntimeBlockMonster").AddComponent<MonsterUnit>();
//        monster.Initialize(monsterRuntime);
//        monster.SetOccupiedCells(new List<int> { 20 });

//        BattleActionRunner runner = new(gridManager);
//        MethodInfo method = typeof(BattleActionRunner).GetMethod(
//            "TryGetPlayerMoveTargetGridIndex",
//            BindingFlags.Instance | BindingFlags.NonPublic,
//            null,
//            new[]
//            {
//                typeof(int),
//                typeof(Vector2Int),
//                typeof(string),
//                typeof(int).MakeByRefType()
//            },
//            null);

//        object[] args =
//        {
//            10,
//            new Vector2Int(2, 0),
//            "Char_Test",
//            0
//        };

//        bool found = (bool)method.Invoke(runner, args);
//        int targetGridIndex = (int)args[3];

//        Object.DestroyImmediate(monster.gameObject);
//        Object.DestroyImmediate(gridManager.gameObject);

//        Assert.That(found, Is.True);
//        Assert.That(targetGridIndex, Is.EqualTo(15));
//    }

//    [Test]
//    public void BattleActionRunner_UsesFastMovementDuration()
//    {
//        Assert.That(BattleActionRunner.MoveAnimationDuration, Is.GreaterThan(0f));
//        Assert.That(BattleActionRunner.MoveAnimationDuration, Is.LessThan(0.25f));
//    }

//    [Test]
//    public void EquipmentBattleStart_AppliesRuneAndRelicStatBonuses()
//    {
//        CharacterRuntimeData runtime = new()
//        {
//            CharacterId = "Char_01",
//            MaxHP = 114,
//            CurrentHP = 114,
//            EquippedRuneIds = new[] { "Rune_01", "Rune_16", "Rune_20", "Rune_25" },
//            EquippedRelicIds = new[] { "Relic_01", "Relic_08", "Relic_09", "Relic_10" }
//        };

//        CharacterMasterData master = new()
//        {
//            CharacterId = "Char_01",
//            MaxHP = 114,
//            MaxCost = 8,
//            MaxResource = 3,
//            MoveValue = 12
//        };

//        BattleEquipmentEffectService.ApplyBattleStartEffects(runtime, master);

//        Assert.That(runtime.MaxHP, Is.EqualTo(125));
//        Assert.That(runtime.CurrentHP, Is.EqualTo(125));
//        Assert.That(runtime.MaxCost, Is.EqualTo(11));
//        Assert.That(runtime.CurrentCost, Is.EqualTo(13));
//        Assert.That(runtime.CurrentResource, Is.EqualTo(3));
//        Assert.That(runtime.CurrentMoveLevel, Is.EqualTo(23));
//    }

//    [Test]
//    public void AllCurrentUniqueResourceRune_LowersMinimumCostToOne()
//    {
//        CharacterRuntimeData runtime = new()
//        {
//            CharacterId = "Char_01",
//            CurrentResource = 1,
//            EquippedRuneIds = new[] { "Rune_05" }
//        };

//        SkillMasterData skill = new()
//        {
//            SkillId = "S_AllCurrent_Test",
//            ReferenceResource = ReferenceResource.UniqueResource,
//            ResourceCostType = ResourceCostType.AllCurrent,
//            ResourceCostValue = 2
//        };

//        bool canPay = SkillCostCalculator.TryGetPreviewPayAmount(
//            runtime,
//            skill,
//            out int payAmount);

//        Assert.That(canPay, Is.True);
//        Assert.That(payAmount, Is.EqualTo(1));
//    }

//    [Test]
//    public void EquipmentEffectValue_AppliesDamageArmorAndSlotBonuses()
//    {
//        CharacterRuntimeData runtime = new()
//        {
//            CharacterId = "Char_02",
//            EquippedRuneIds = new[] { "Rune_09" },
//            EquippedRelicIds = new[] { "Relic_03" }
//        };

//        SkillMasterData attackSkill = new()
//        {
//            SkillId = "S_Attack_Test",
//            SkillType = SkillType.Attack
//        };

//        PlayerReservedCommand command = new(runtime, attackSkill);
//        command.SetTimelineSlotIndex(4);

//        SkillEffectEntry strike = new()
//        {
//            EffectId = "E_Strike",
//            ValueAmount = 5,
//            CountAmount = 1
//        };

//        int value = BattleEquipmentEffectService.ModifyPlayerEffectValue(
//            runtime,
//            command,
//            strike,
//            5);

//        Assert.That(value, Is.EqualTo(8));
//    }

//    [Test]
//    public void EquipmentRangeRune_ExpandsChar03FrontFourToFrontSix()
//    {
//        CharacterRuntimeData runtime = new()
//        {
//            CharacterId = "Char_03",
//            EquippedRuneIds = new[] { "Rune_14" }
//        };

//        SkillMasterData skill = new()
//        {
//            RangeId = "Range_21"
//        };

//        string rangeId = BattleEquipmentEffectService.GetEffectiveRangeId(runtime, skill);

//        Assert.That(rangeId, Is.EqualTo("Range_18"));
//    }

//    [Test]
//    public void DuplicateSkillReservations_IncreaseCostOnlyWithinSameSlot()
//    {
//        GameObject timelineObject = new("DuplicateSkillCostTimeline");
//        BattleTimelineController timeline =
//            timelineObject.AddComponent<BattleTimelineController>();

//        ReserveTurnSlotUI slot0 = new GameObject("DuplicateSkillCostSlot0")
//            .AddComponent<ReserveTurnSlotUI>();
//        ReserveTurnSlotUI slot1 = new GameObject("DuplicateSkillCostSlot1")
//            .AddComponent<ReserveTurnSlotUI>();
//        ReserveTurnSlotUI[] slots = { slot0, slot1 };

//        typeof(BattleTimelineController)
//            .GetField("reserveSlots", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(timeline, slots);

//        CharacterRuntimeData runtime = new()
//        {
//            CharacterId = "Char_Test",
//            MaxCost = 10,
//            CurrentCost = 10
//        };

//        SkillMasterData skill = new()
//        {
//            SkillId = "S_Duplicate_Test",
//            ReferenceResource = ReferenceResource.Cost,
//            ResourceCostType = ResourceCostType.Fixed,
//            ResourceCostValue = 1
//        };

//        PlayerReservedCommand firstSlotFirst = new(runtime, skill);
//        PlayerReservedCommand firstSlotSecond = new(runtime, skill);
//        PlayerReservedCommand firstSlotThird = new(runtime, skill);
//        PlayerReservedCommand nextSlotFirst = new(runtime, skill);

//        Assert.That(slot0.AddCommand(firstSlotFirst), Is.True);
//        Assert.That(slot0.AddCommand(firstSlotSecond), Is.True);
//        Assert.That(slot0.AddCommand(firstSlotThird), Is.True);
//        Assert.That(slot1.AddCommand(nextSlotFirst), Is.True);

//        MethodInfo recalculateMethod = typeof(BattleTimelineController).GetMethod(
//            "RecalculateAllReservedCosts",
//            BindingFlags.Instance | BindingFlags.NonPublic);

//        Assert.That(recalculateMethod, Is.Not.Null);
//        recalculateMethod.Invoke(timeline, null);

//        Assert.That(firstSlotFirst.Cost, Is.EqualTo(1));
//        Assert.That(firstSlotSecond.Cost, Is.EqualTo(2));
//        Assert.That(firstSlotThird.Cost, Is.EqualTo(3));
//        Assert.That(nextSlotFirst.Cost, Is.EqualTo(1));
//        Assert.That(runtime.ReservedCost, Is.EqualTo(7));

//        Object.DestroyImmediate(timelineObject);
//        Object.DestroyImmediate(slot0.gameObject);
//        Object.DestroyImmediate(slot1.gameObject);
//    }

//    [Test]
//    public void PreviewReservationCostValue_IncludesDuplicateSkillCostSurchargeForActiveSlotOnly()
//    {
//        GameObject timelineObject = new("DuplicateSkillPreviewCostTimeline");
//        BattleTimelineController timeline =
//            timelineObject.AddComponent<BattleTimelineController>();

//        ReserveTurnSlotUI slot0 = new GameObject("DuplicateSkillPreviewCostSlot0")
//            .AddComponent<ReserveTurnSlotUI>();
//        ReserveTurnSlotUI slot1 = new GameObject("DuplicateSkillPreviewCostSlot1")
//            .AddComponent<ReserveTurnSlotUI>();
//        ReserveTurnSlotUI[] slots = { slot0, slot1 };

//        typeof(BattleTimelineController)
//            .GetField("reserveSlots", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(timeline, slots);

//        CharacterRuntimeData runtime = new()
//        {
//            CharacterId = "Char_Test",
//            MaxCost = 10,
//            CurrentCost = 10
//        };

//        SkillMasterData skill = new()
//        {
//            SkillId = "S_Duplicate_Preview_Test",
//            ReferenceResource = ReferenceResource.Cost,
//            ResourceCostType = ResourceCostType.Fixed,
//            ResourceCostValue = 1
//        };

//        Assert.That(slot0.AddCommand(new PlayerReservedCommand(runtime, skill)), Is.True);

//        typeof(BattleTimelineController)
//            .GetField("activeSlotIndex", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(timeline, 0);

//        int slot0PreviewCost = timeline.GetPreviewReservationCostValue(runtime, skill);

//        typeof(BattleTimelineController)
//            .GetField("activeSlotIndex", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(timeline, 1);

//        int slot1PreviewCost = timeline.GetPreviewReservationCostValue(runtime, skill);

//        Assert.That(slot0PreviewCost, Is.EqualTo(2));
//        Assert.That(slot1PreviewCost, Is.EqualTo(1));

//        Object.DestroyImmediate(timelineObject);
//        Object.DestroyImmediate(slot0.gameObject);
//        Object.DestroyImmediate(slot1.gameObject);
//    }

//    [Test]
//    public void PlayerReservedCommand_CostModifiersCanResetToBaseCost()
//    {
//        CharacterRuntimeData runtime = new()
//        {
//            CharacterId = "Char_Test",
//            CurrentCost = 3,
//            EquippedRuneIds = new[] { "Rune_24" }
//        };

//        SkillMasterData moveSkill = new()
//        {
//            SkillId = "S_Move_Test",
//            Category = Category.Move,
//            ReferenceResource = ReferenceResource.Cost,
//            ResourceCostType = ResourceCostType.Fixed,
//            ResourceCostValue = 1
//        };

//        PlayerReservedCommand command = new(runtime, moveSkill);

//        BattleEquipmentEffectService.ApplyReservationCostModifiers(
//            command,
//            0,
//            true,
//            false);

//        Assert.That(command.Cost, Is.EqualTo(0));

//        command.ResetCostsToBase();

//        Assert.That(command.Cost, Is.EqualTo(1));
//    }

//    [Test]
//    public void Relic06_AppliesArmorOnlyAtSecondPlayerTurnStart()
//    {
//        CharacterRuntimeData runtime = new()
//        {
//            CharacterId = "Char_Test",
//            CurrentShield = 0,
//            EquippedRelicIds = new[] { "Relic_06" }
//        };

//        BattleEquipmentEffectService.ApplyPlayerTurnStartEffects(runtime, 1);
//        Assert.That(runtime.CurrentShield, Is.EqualTo(0));

//        BattleEquipmentEffectService.ApplyPlayerTurnStartEffects(runtime, 2);
//        Assert.That(runtime.CurrentShield, Is.EqualTo(10));

//        BattleEquipmentEffectService.ApplyPlayerTurnStartEffects(runtime, 3);
//        Assert.That(runtime.CurrentShield, Is.EqualTo(10));
//    }

//    [Test]
//    public void CharacterLoader_MapsLegacyHealthAndStaminaColumnsToHPAndCostFields()
//    {
//        Dictionary<string, List<Dictionary<string, string>>> workbook = new()
//        {
//            ["Character"] = new List<Dictionary<string, string>>
//            {
//                new()
//                {
//                    ["CharacterId"] = "Char_HP_Cost",
//                    ["Name"] = "HP Cost",
//                    ["MaxHP"] = "12",
//                    ["MaxCost"] = "7",
//                    ["CostRecovery"] = "3"
//                }
//            }
//        };

//        List<CharacterMasterData> characters = CharacterCsvLoader.Load(workbook);

//        Assert.That(characters, Has.Count.EqualTo(1));
//        AssertIntFieldValue(characters[0], "MaxHP", 12);
//        AssertIntFieldValue(characters[0], "MaxCost", 7);
//        AssertIntFieldValue(characters[0], "CostRecovery", 3);
//    }

//    [Test]
//    public void MonsterLoader_MapsLegacyHealthColumnToHPField()
//    {
//        Dictionary<string, List<Dictionary<string, string>>> workbook = new()
//        {
//            ["Monster"] = new List<Dictionary<string, string>>
//            {
//                new()
//                {
//                    ["MonsterId"] = "Mon_HP",
//                    ["Name"] = "HP Monster",
//                    ["Health"] = "24"
//                }
//            }
//        };

//        List<MonsterMasterData> monsters = MonsterCsvLoader.Load(workbook);

//        Assert.That(monsters, Has.Count.EqualTo(1));
//        AssertIntFieldValue(monsters[0], "HP", 24);
//    }

//    [Test]
//    public void MonsterSkillLoader_MapsKoreanRandomValueColumnToValueRandomRange()
//    {
//        Dictionary<string, List<Dictionary<string, string>>> workbook = new()
//        {
//            ["MonsterSkill"] = new List<Dictionary<string, string>>
//            {
//                new()
//                {
//                    ["SkillId"] = "S_Monster_Random",
//                    ["Name"] = "Attack",
//                    ["Target"] = "PlayerParty",
//                    ["EffectIds"] = "E_Strike",
//                    ["ValueCalcTypes"] = "Fixed",
//                    ["ValueRate"] = "6",
//                    ["CountRate"] = "1",
//                    ["RangeId"] = "Range_17",
//                    ["TimelineNotation"] = "Attack",
//                    ["Effectdesc"] = "\"Range_17\" attacks \"\uC218\uCE58\" damage.",
//                    ["\uC218\uCE58\uAC12\uBCC0\uC218"] = "2"
//                }
//            }
//        };

//        List<MonsterSkillData> skills = MonsterSkillCsvLoader.Load(workbook);

//        Assert.That(skills, Has.Count.EqualTo(1));
//        AssertIntFieldValue(skills[0], "ValueRandomRange", 2);
//    }

//    [Test]
//    public void MonsterTimelineDescription_ReplacesValueTokenWithDamageRange()
//    {
//        MonsterReservedCommand command = CreateMonsterReservedCommand("M_RandomDesc");
//        command.SkillData.ValueRate = "6";
//        command.SkillData.TimelineNotation = TimelineActionType.Attack;
//        command.SkillData.EffectDesc = "\"Range_17\" attacks \"\uC218\uCE58\" damage.";
//        SetIntFieldValue(command.SkillData, "ValueRandomRange", 2);

//        BattleTimelinePreviewEntry entry =
//            BattleTimelinePreviewEntry.CreateMonster(0, 0, command);

//        Assert.That(entry.SkillEffectDescription, Does.Contain("4-8"));
//        Assert.That(entry.SkillEffectDescription, Does.Not.Contain("\uC218\uCE58"));
//    }

//    [Test]
//    public void MonsterDamageRange_UsesValueRatePlusMinusRandomRange()
//    {
//        MonsterReservedCommand command = CreateMonsterReservedCommand("M_RandomDamage");
//        command.SkillData.ValueRate = "6";
//        SetIntFieldValue(command.SkillData, "ValueRandomRange", 2);

//        BattleDamageService service = new(null);
//        MethodInfo method = typeof(BattleDamageService).GetMethod(
//            "TryGetMonsterDamageRange",
//            BindingFlags.Instance | BindingFlags.Public,
//            null,
//            new[]
//            {
//                typeof(MonsterReservedCommand),
//                typeof(int).MakeByRefType(),
//                typeof(int).MakeByRefType()
//            },
//            null);

//        Assert.That(method, Is.Not.Null);

//        object[] args = { command, 0, 0 };
//        bool found = (bool)method.Invoke(service, args);

//        Assert.That(found, Is.True);
//        Assert.That((int)args[1], Is.EqualTo(4));
//        Assert.That((int)args[2], Is.EqualTo(8));
//    }

//    [Test]
//    public void SkillTooltipFormatter_ReplacesUniqueSkillFormulaWithCalculatedValue()
//    {
//        Type formatterType = FindType("Relic.Gameplay.Data.SkillTooltipFormatter");
//        Assert.That(formatterType, Is.Not.Null);

//        MethodInfo formatMethod = formatterType.GetMethod(
//            "Format",
//            BindingFlags.Static | BindingFlags.Public,
//            null,
//            new[]
//            {
//                typeof(SkillMasterData),
//                typeof(string),
//                typeof(CharacterRuntimeData),
//                typeof(int)
//            },
//            null);

//        Assert.That(formatMethod, Is.Not.Null);

//        SkillMasterData skill = new()
//        {
//            SkillId = "S_Unique_Tooltip",
//            ToolTip = "{(3+\uC9D1\uC911)x\uC18C\uBAA8\uB7C9}\uC758 \uBC29\uC5B4\uB3C4\uB97C \uC5BB\uB294\uB2E4.",
//            EffectEntries = new List<SkillEffectEntry>
//            {
//                new()
//                {
//                    EffectId = "E_Armor",
//                    ValueCalcType = ValueCalcType.Fixed,
//                    ValueAmount = 3,
//                    CountAmount = 1
//                }
//            }
//        };

//        CharacterRuntimeData runtime = new()
//        {
//            CharacterId = "Char_Tooltip"
//        };
//        runtime.StatusEffects.Add(new StatusEffectRuntimeData
//        {
//            EffectId = "E_Focus",
//            Stack = 1
//        });

//        string result = (string)formatMethod.Invoke(
//            null,
//            new object[] { skill, skill.ToolTip, runtime, 2 });

//        Assert.That(result, Does.Contain("8\uC758 \uBC29\uC5B4\uB3C4"));
//        Assert.That(result, Does.Not.Contain("{"));

//        runtime.StatusEffects.Add(new StatusEffectRuntimeData
//        {
//            EffectId = "E_Power",
//            Stack = 2
//        });

//        string powerResult = (string)formatMethod.Invoke(
//            null,
//            new object[]
//            {
//                skill,
//                "{(5+\uC9D1\uC911)x\uC18C\uBAA8\uB7C9+\uD798}\uC758 \uD53C\uD574\uB97C \uC900\uB2E4.",
//                runtime,
//                3
//            });

//        Assert.That(powerResult, Does.Contain("20\uC758 \uD53C\uD574"));
//        Assert.That(powerResult, Does.Not.Contain("{"));
//    }

//    private static MonsterUnit CreateMonsterWithHud(
//        string name,
//        string runtimeId,
//        int occupiedGridIndex,
//        out CanvasGroup canvasGroup)
//    {
//        MonsterMasterData masterData = new()
//        {
//            MonsterId = name,
//            Name = name,
//            HP = 10
//        };

//        MonsterRuntimeData runtimeData = new(runtimeId, masterData);
//        MonsterUnit monster = new GameObject(name).AddComponent<MonsterUnit>();
//        monster.Initialize(runtimeData);
//        monster.SetOccupiedCells(new List<int> { occupiedGridIndex });

//        GameObject hudObject = new(name + "HUD");
//        canvasGroup = hudObject.AddComponent<CanvasGroup>();
//        MonsterHUDSlot hud = hudObject.AddComponent<MonsterHUDSlot>();
//        monster.BindHUD(hud);

//        return monster;
//    }

//    private static MonsterReservedCommand CreateMonsterReservedCommand(string runtimeId)
//    {
//        MonsterMasterData monsterData = new()
//        {
//            MonsterId = "Monster_Test",
//            Name = "Monster_Test",
//            HP = 10
//        };

//        MonsterRuntimeData runtime = new(runtimeId, monsterData);
//        MonsterSkillData skill = new()
//        {
//            SkillId = "MS_Test"
//        };

//        return new MonsterReservedCommand(runtime, skill);
//    }

//    private static void AssertIntFieldValue(object target, string fieldName, int expected)
//    {
//        Assert.That(target, Is.Not.Null);

//        FieldInfo field = target.GetType().GetField(
//            fieldName,
//            BindingFlags.Instance | BindingFlags.Public);

//        Assert.That(field, Is.Not.Null, $"{target.GetType().Name}.{fieldName} field is missing.");
//        Assert.That((int)field.GetValue(target), Is.EqualTo(expected));
//    }

//    private static void SetIntFieldValue(object target, string fieldName, int value)
//    {
//        Assert.That(target, Is.Not.Null);

//        FieldInfo field = target.GetType().GetField(
//            fieldName,
//            BindingFlags.Instance | BindingFlags.Public);

//        Assert.That(field, Is.Not.Null, $"{target.GetType().Name}.{fieldName} field is missing.");
//        field.SetValue(target, value);
//    }

//    private static Type FindType(string fullName)
//    {
//        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

//        for (int i = 0; i < assemblies.Length; i++)
//        {
//            Type type = assemblies[i].GetType(fullName);

//            if (type != null)
//                return type;
//        }

//        return null;
//    }

//    private static GridManager CreateOneCellGrid(
//        string name,
//        out Renderer cellRenderer,
//        out Collider cellCollider,
//        out GameObject gridObject,
//        out GameObject cellObject)
//    {
//        gridObject = new GameObject(name);
//        GridManager gridManager = gridObject.AddComponent<GridManager>();

//        cellObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
//        cellObject.name = name + "_Cell";
//        cellObject.transform.SetParent(gridObject.transform);

//        GridCell cell = cellObject.AddComponent<GridCell>();
//        cell.Initialize(gridManager, 0, 0, 0);

//        typeof(GridManager)
//            .GetField("width", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(gridManager, 1);
//        typeof(GridManager)
//            .GetField("height", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(gridManager, 1);
//        typeof(GridManager)
//            .GetField("cells", BindingFlags.Instance | BindingFlags.NonPublic)
//            .SetValue(gridManager, new[] { cell });

//        cellRenderer = cellObject.GetComponent<Renderer>();
//        cellCollider = cellObject.GetComponent<Collider>();

//        return gridManager;
//    }

//    private static MonsterUnit CreateMonsterWithSprite(
//        string name,
//        string runtimeId,
//        out SpriteRenderer spriteRenderer,
//        out Collider2D clickCollider)
//    {
//        MonsterMasterData masterData = new()
//        {
//            MonsterId = name,
//            Name = name,
//            HP = 10
//        };

//        MonsterRuntimeData runtimeData = new(runtimeId, masterData);
//        MonsterUnit monster = new GameObject(name).AddComponent<MonsterUnit>();
//        spriteRenderer = monster.gameObject.AddComponent<SpriteRenderer>();
//        spriteRenderer.color = Color.white;
//        clickCollider = monster.gameObject.AddComponent<BoxCollider2D>();
//        monster.Initialize(runtimeData);

//        return monster;
//    }

//    private static MonsterUnit CreateDamageTargetMonster(
//        string name,
//        string runtimeId,
//        int hp)
//    {
//        MonsterMasterData masterData = new()
//        {
//            MonsterId = name,
//            Name = name,
//            HP = hp
//        };

//        MonsterRuntimeData runtimeData = new(runtimeId, masterData);
//        MonsterUnit monster = new GameObject(name).AddComponent<MonsterUnit>();
//        monster.Initialize(runtimeData);
//        monster.SetOccupiedCells(new List<int> { 12 });

//        return monster;
//    }
//}



