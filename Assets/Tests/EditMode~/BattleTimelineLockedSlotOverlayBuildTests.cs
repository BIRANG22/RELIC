using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class BattleTimelineLockedSlotOverlayBuildTests
{
    [Test]
    public void SetPlayerLockedSlot_UsesSerializedOverlaySpriteForRuntimeCreatedOverlay()
    {
        GameObject barObject = new("TimelineBar");
        GameObject groupObject = new("TimelineGroup", typeof(RectTransform));
        Texture2D texture = new(2, 2);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));

        try
        {
            BattleTimelineBarUI bar = barObject.AddComponent<BattleTimelineBarUI>();
            BattleTimelineGroupUI group = groupObject.AddComponent<BattleTimelineGroupUI>();

            SetPrivateField(bar, "timelineGroups", new[] { group });
            SetPrivateField(bar, "lockedSlotOverlaySprite", sprite);

            bar.SetPlayerLockedSlot(0);

            Image overlayImage = group.GetComponentInChildren<Image>(true);
            Assert.That(overlayImage, Is.Not.Null);
            Assert.That(overlayImage.sprite, Is.SameAs(sprite));
            Assert.That(overlayImage.gameObject.activeSelf, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(groupObject);
            Object.DestroyImmediate(barObject);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"{fieldName} field is missing.");
        field.SetValue(target, value);
    }
}
