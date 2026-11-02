using NUnit.Framework;
using UnityEngine;

public sealed class BattleRenderSortingTests
{
    [Test]
    public void SortingReferenceYOverride_IgnoresVisualArcHeightUntilCleared()
    {
        GameObject go = new GameObject("sorted", typeof(SpriteRenderer), typeof(YSortSprite));
        try
        {
            YSortSprite sorter = go.GetComponent<YSortSprite>();
            go.transform.position = new Vector3(0f, 5f, 0f);
            sorter.SetSortingReferenceY(1.25f);

            Assert.That(sorter.CalculateCurrentSortingOrder(), Is.EqualTo(-125));

            go.transform.position = new Vector3(0f, 9f, 0f);
            Assert.That(sorter.CalculateCurrentSortingOrder(), Is.EqualTo(-125));

            sorter.ClearSortingReferenceY();
            Assert.That(sorter.CalculateCurrentSortingOrder(), Is.EqualTo(-900));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
