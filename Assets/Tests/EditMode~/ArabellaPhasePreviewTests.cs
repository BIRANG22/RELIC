using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class ArabellaPhasePreviewTests
{
    private const string DatabasePath = "Assets/DB/MonsterPrefabDatabase.asset";

    [Test]
    public void PhaseTwoEntry_UsesDistinctPhaseTwoIdlePreviewSprites()
    {
        MonsterPrefabDatabase database =
            AssetDatabase.LoadAssetAtPath<MonsterPrefabDatabase>(DatabasePath);

        Assert.That(database, Is.Not.Null);
        Assert.That(database.TryGetEntry("Mon_12_1", out MonsterPrefabDatabase.Entry phaseOne), Is.True);
        Assert.That(database.TryGetEntry("Mon_12_2", out MonsterPrefabDatabase.Entry phaseTwo), Is.True);
        Assert.That(phaseOne.idlePreviewSprites, Has.Length.EqualTo(6));
        Assert.That(phaseTwo.idlePreviewSprites, Has.Length.EqualTo(6));

        for (int index = 0; index < phaseTwo.idlePreviewSprites.Length; index++)
        {
            Sprite phaseTwoSprite = phaseTwo.idlePreviewSprites[index];
            Assert.That(phaseTwoSprite, Is.Not.Null, $"2페이즈 프리뷰 {index}번 프레임이 비어 있습니다.");
            Assert.That(
                phaseTwoSprite.name,
                Is.EqualTo($"Mon_B_01_b_idle_{index}"),
                $"2페이즈 프리뷰 {index}번 프레임이 2페이즈 Idle 리소스를 사용해야 합니다.");
            Assert.That(
                phaseTwoSprite,
                Is.Not.SameAs(phaseOne.idlePreviewSprites[index]),
                $"{index}번 프레임이 1페이즈 프리뷰와 동일합니다.");
        }
    }
}
