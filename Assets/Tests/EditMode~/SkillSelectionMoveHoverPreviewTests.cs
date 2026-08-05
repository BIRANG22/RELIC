using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using UnityEngine;

public class SkillSelectionMoveHoverPreviewTests
{
    [Test]
    public void MoveSkillHover_DoesNotCancelActiveGeneralSelectionReservation()
    {
        GameObject controllerObject = new("SkillSelectionMoveHoverController");

        try
        {
            PlayerSkillReservationController controller =
                controllerObject.AddComponent<PlayerSkillReservationController>();
            CharacterRuntimeData runtime = CreateRuntime();
            SkillMasterData selectionSkill = CreateSelectionSkill("S_Test_Selection");
            SkillMasterData moveSkill = CreateMoveSkill();

            SetPrivateField(controller, "currentUserRuntime", runtime);
            SetPrivateField(controller, "currentSkillData", selectionSkill);
            SetPrivateField(controller, "currentSlotIndex", 0);

            controller.CancelSelectionWhenHoveringDifferentSkill(runtime, moveSkill);

            Assert.That(controller.IsSkillSelectionActive(), Is.True);
            Assert.That(
                GetPrivateField<SkillMasterData>(controller, "currentSkillData"),
                Is.SameAs(selectionSkill));
        }
        finally
        {
            Object.DestroyImmediate(controllerObject);
        }
    }

    [Test]
    public void DifferentNonMoveSkillHover_CancelsActiveGeneralSelectionReservation()
    {
        GameObject controllerObject = new("SkillSelectionDifferentHoverController");

        try
        {
            PlayerSkillReservationController controller =
                controllerObject.AddComponent<PlayerSkillReservationController>();
            CharacterRuntimeData runtime = CreateRuntime();
            SkillMasterData selectionSkill = CreateSelectionSkill("S_Test_Selection");
            SkillMasterData otherSkill = CreateSelectionSkill("S_Test_Other");

            SetPrivateField(controller, "currentUserRuntime", runtime);
            SetPrivateField(controller, "currentSkillData", selectionSkill);
            SetPrivateField(controller, "currentSlotIndex", 0);

            controller.CancelSelectionWhenHoveringDifferentSkill(runtime, otherSkill);

            Assert.That(controller.IsSkillSelectionActive(), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(controllerObject);
        }
    }

    [Test]
    public void ClearSkillHoverRangePreview_RestoresActiveGeneralSelectionCells()
    {
        GameObject controllerObject = new("SkillSelectionRestoreController");
        GameObject gridObject = new("SkillSelectionRestoreGrid");
        GameObject rangePreviewObject = new("SkillSelectionRestoreRangePreview");
        List<GameObject> cellObjects = new();

        try
        {
            GridManager gridManager = gridObject.AddComponent<GridManager>();
            GridCell[] cells = CreateGridCells(gridObject.transform, cellObjects, out GameObject[] highlights);
            SetPrivateField(gridManager, "cells", cells);
            InvokePrivateMethod(gridManager, "Awake");

            RangePreview rangePreview = rangePreviewObject.AddComponent<RangePreview>();
            SetPrivateField(rangePreview, "gridManager", gridManager);

            PlayerSkillReservationController controller =
                controllerObject.AddComponent<PlayerSkillReservationController>();
            SkillMasterData selectionSkill = CreateSelectionSkill("S_Test_Selection");
            SetPrivateField(controller, "rangePreview", rangePreview);
            SetPrivateField(controller, "currentUserRuntime", CreateRuntime());
            SetPrivateField(controller, "currentSkillData", selectionSkill);
            SetPrivateField(controller, "currentSlotIndex", 0);
            GetPrivateField<List<int>>(controller, "currentGeneralSelectionSelectableIndices").Add(2);

            rangePreview.ShowDirectionCells(new List<int> { 2 });
            Assert.That(highlights[2].activeSelf, Is.True);

            rangePreview.ShowDirectionCells(new List<int> { 1 });
            Assert.That(highlights[2].activeSelf, Is.False);

            controller.ClearSkillHoverRangePreview();

            Assert.That(highlights[2].activeSelf, Is.True);
            Assert.That(highlights[1].activeSelf, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(rangePreviewObject);
            Object.DestroyImmediate(gridObject);
            Object.DestroyImmediate(controllerObject);

            for (int i = 0; i < cellObjects.Count; i++)
                Object.DestroyImmediate(cellObjects[i]);
        }
    }

    private static CharacterRuntimeData CreateRuntime()
    {
        return new CharacterRuntimeData
        {
            CharacterId = "Char_Test_Selection_Hover",
            CurrentCost = 1,
            MaxCost = 1,
            Direction = BattleDirection.Right
        };
    }

    private static SkillMasterData CreateSelectionSkill(string skillId)
    {
        return new SkillMasterData
        {
            SkillId = skillId,
            Category = Category.Ability,
            SkillType = SkillType.Attack,
            RangeType = RangeType.Selection,
            TimelineNotation = TimelineActionType.Skill
        };
    }

    private static SkillMasterData CreateMoveSkill()
    {
        return new SkillMasterData
        {
            SkillId = "S_Move_1",
            Category = Category.Move,
            SkillType = SkillType.None,
            RangeType = RangeType.Selection,
            TimelineNotation = TimelineActionType.Move,
            ReferenceResource = ReferenceResource.Cost,
            ResourceCostValue = 1,
            GridMove = 1
        };
    }

    private static GridCell[] CreateGridCells(
        Transform parent,
        List<GameObject> cellObjects,
        out GameObject[] highlights)
    {
        GridCell[] cells = new GridCell[35];
        highlights = new GameObject[cells.Length];

        for (int i = 0; i < cells.Length; i++)
        {
            GameObject cellObject = new($"Grid_{i}");
            cellObjects.Add(cellObject);
            cellObject.transform.SetParent(parent, false);
            cellObject.AddComponent<MeshRenderer>();
            cellObject.AddComponent<BoxCollider>();

            GameObject highlightObject = new("Highlight");
            highlightObject.transform.SetParent(cellObject.transform, false);
            highlightObject.AddComponent<MeshRenderer>();
            highlights[i] = highlightObject;

            cells[i] = cellObject.AddComponent<GridCell>();
        }

        return cells;
    }

    private static void SetPrivateField<TValue>(
        object target,
        string fieldName,
        TValue value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, $"{fieldName} field is missing.");
        field.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, $"{fieldName} field is missing.");
        return (T)field.GetValue(target);
    }

    private static void InvokePrivateMethod(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"{methodName} method is missing.");
        method.Invoke(target, null);
    }
}
