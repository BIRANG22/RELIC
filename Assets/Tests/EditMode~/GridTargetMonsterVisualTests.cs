using System.Reflection;
using NUnit.Framework;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using UnityEngine;

public class GridTargetMonsterVisualTests
{
    [Test]
    public void SelectionSkillReservation_DimsAndDisablesMonsterUntilPreviewClears()
    {
        GameObject controllerObject = new("SelectionSkillReservationController");
        MonsterUnit monster = CreateMonster("SelectionSkillTargetMonster", out SpriteRenderer spriteRenderer, out Collider2D collider);

        try
        {
            PlayerSkillReservationController controller =
                controllerObject.AddComponent<PlayerSkillReservationController>();
            CharacterRuntimeData runtime = new()
            {
                CharacterId = "Char_Selection",
                Direction = BattleDirection.Right
            };
            SkillMasterData selectionSkill = new()
            {
                SkillId = "S_Test_Selection",
                Category = Category.Attack,
                SkillType = SkillType.Attack,
                RangeType = RangeType.Selection,
                TimelineNotation = TimelineActionType.Skill
            };

            controller.StartReservation(runtime, selectionSkill, 0, 0);

            Assert.That(collider.enabled, Is.False);
            Assert.That(spriteRenderer.color.a, Is.LessThan(1f));

            controller.ClearPreview();

            Assert.That(collider.enabled, Is.True);
            Assert.That(spriteRenderer.color.a, Is.EqualTo(1f).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(monster.gameObject);
            Object.DestroyImmediate(controllerObject);
        }
    }

    [Test]
    public void ActiveRelicTargeting_DimsAndDisablesMonsterUntilTargetingCancels()
    {
        GameObject controllerObject = new("ActiveRelicTargetingController");
        GameObject gridObject = new("ActiveRelicTargetingGrid");
        MonsterUnit monster = CreateMonster("ActiveRelicTargetMonster", out SpriteRenderer spriteRenderer, out Collider2D collider);

        try
        {
            ActiveRelicTargetingController controller =
                controllerObject.AddComponent<ActiveRelicTargetingController>();
            GridManager gridManager = gridObject.AddComponent<GridManager>();
            ActiveRelicService service = new(new RelicDatabase());
            CharacterRuntimeData runtime = new()
            {
                CharacterId = "Char_ActiveRelic"
            };
            ActiveRelicAvailability availability = new()
            {
                CanUse = true,
                TargetMode = ActiveRelicTargetMode.Grid
            };

            SetPrivateField(controller, "gridManager", gridManager);

            bool started = controller.BeginTargeting(null, service, runtime, availability);

            Assert.That(started, Is.True);
            Assert.That(collider.enabled, Is.False);
            Assert.That(spriteRenderer.color.a, Is.LessThan(1f));

            controller.CancelTargeting();

            Assert.That(collider.enabled, Is.True);
            Assert.That(spriteRenderer.color.a, Is.EqualTo(1f).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(monster.gameObject);
            Object.DestroyImmediate(controllerObject);
            Object.DestroyImmediate(gridObject);
        }
    }

    private static MonsterUnit CreateMonster(
        string name,
        out SpriteRenderer spriteRenderer,
        out Collider2D collider)
    {
        GameObject monsterObject = new(name);
        spriteRenderer = monsterObject.AddComponent<SpriteRenderer>();
        collider = monsterObject.AddComponent<BoxCollider2D>();
        return monsterObject.AddComponent<MonsterUnit>();
    }

    private static void SetPrivateField<TTarget, TValue>(
        TTarget target,
        string fieldName,
        TValue value)
    {
        FieldInfo field = typeof(TTarget).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, $"{fieldName} field is missing.");
        field.SetValue(target, value);
    }
}
