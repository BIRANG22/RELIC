using NUnit.Framework;
using UnityEngine;

public class BattleCharacterSelectionReadyAnimationTests
{
    [Test]
    public void SelectionReadyAnimation_UsesBattleReadyStateName()
    {
        Assert.That(
            BattleCharacter.SelectionReadyAnimationStateName,
            Is.EqualTo("battle_ready"));
    }

    [Test]
    public void SelectionReadyAnimation_FindsAnimatorUnderSpriteRootFirst()
    {
        GameObject characterObject = new("Character");
        GameObject spriteRootObject = new("SpriteRoot");
        GameObject nestedObject = new("NestedAnimator");

        try
        {
            spriteRootObject.transform.SetParent(characterObject.transform);
            nestedObject.transform.SetParent(spriteRootObject.transform);

            Animator nestedAnimator = nestedObject.AddComponent<Animator>();
            characterObject.AddComponent<Animator>();

            Assert.That(
                BattleCharacter.FindSelectionReadyAnimator(characterObject.transform),
                Is.EqualTo(nestedAnimator));
        }
        finally
        {
            Object.DestroyImmediate(characterObject);
        }
    }

    [Test]
    public void SelectionReadyAnimation_UsesIdleStateNameWhenDeselected()
    {
        Assert.That(
            BattleCharacter.SelectionIdleAnimationStateName,
            Is.EqualTo("Idle"));
    }
}
