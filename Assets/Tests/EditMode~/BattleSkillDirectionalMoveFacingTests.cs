using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleSkillDirectionalMoveFacingTests
{
    private GameObject characterObject;
    private BattleCharacter character;
    private BattleUnitFacing facing;
    private BattleActionRunner runner;
    private MethodInfo applyFacing;

    [SetUp]
    public void SetUp()
    {
        characterObject = new GameObject("SkillMoveFacingTestCharacter");
        character = characterObject.AddComponent<BattleCharacter>();
        facing = characterObject.AddComponent<BattleUnitFacing>();
        runner = new BattleActionRunner(null);
        applyFacing = typeof(BattleActionRunner).GetMethod(
            "ApplyPlayerMoveFacing",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(BattleCharacter), typeof(BattleDirection), typeof(Vector2Int), typeof(bool) },
            null);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(characterObject);
    }

    [Test]
    public void SkillRetreatMove_PreservesAttackFacing()
    {
        facing.FaceRight(true);

        Assert.That(applyFacing, Is.Not.Null);
        applyFacing.Invoke(runner, new object[]
        {
            character,
            BattleDirection.Right,
            Vector2Int.left,
            true
        });

        Assert.That(facing.IsFacingRight, Is.True);
    }

    [Test]
    public void NormalMove_StillFacesMovementDirection()
    {
        facing.FaceRight(true);

        Assert.That(applyFacing, Is.Not.Null);
        applyFacing.Invoke(runner, new object[]
        {
            character,
            BattleDirection.Right,
            Vector2Int.left,
            false
        });

        Assert.That(facing.IsFacingRight, Is.False);
    }
}
