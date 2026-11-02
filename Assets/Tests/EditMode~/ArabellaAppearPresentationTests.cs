using NUnit.Framework;
using UnityEngine;

public sealed class ArabellaAppearPresentationTests
{
    [Test]
    public void BossBackgroundAppear_UsesSequenceWithoutAnimatorController()
    {
        const string path = "Assets/Project/PrefabsR/Battle/BackGround/St1_boss.prefab";
        GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.That(prefab, Is.Not.Null);

        SpriteTransformSequence[] sequences = prefab.GetComponentsInChildren<SpriteTransformSequence>(true);
        SpriteTransformSequence appear = null;
        foreach (SpriteTransformSequence sequence in sequences)
            if (sequence.name == "appear") appear = sequence;

        Assert.That(appear, Is.Not.Null);
        Animator animator = appear.GetComponent<Animator>();
        Assert.That(animator, Is.Not.Null);
        Assert.That(animator.runtimeAnimatorController, Is.Null);

        var serialized = new UnityEditor.SerializedObject(appear);
        UnityEditor.SerializedProperty steps = serialized.FindProperty("steps");
        AnimationClip appear01 = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(
            "Assets/Project/PrefabsR/Monster/Boss/01.Arabella/Clip/appear01.anim");
        Assert.That(steps.arraySize, Is.GreaterThanOrEqualTo(5));
        for (int i = 0; i < 5; i++)
        {
            Object clip = steps.GetArrayElementAtIndex(i)
                .FindPropertyRelative("animationClip").objectReferenceValue;
            Assert.That(clip, Is.SameAs(appear01), $"appear step {i}");
        }
    }
}
