using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

public class MapNodeHoverRelayTests
{
    [Test]
    public void ChildHitArea_ForwardsHoverCallbacksAndScalesSiblingIcon()
    {
        GameObject node = new("Node", typeof(RectTransform));
        try
        {
            GameObject iconObject = new("Icon", typeof(RectTransform));
            iconObject.transform.SetParent(node.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.localScale = Vector3.one * 0.4f;

            GameObject hitArea = new("HoverHitArea", typeof(RectTransform));
            hitArea.transform.SetParent(node.transform, false);
            MapNodeHoverRelay relay = hitArea.AddComponent<MapNodeHoverRelay>();
            int enteredCount = 0;
            int exitedCount = 0;
            relay.Configure(null, null, (_, _) => enteredCount++, () => exitedCount++);

            relay.OnPointerEnter((PointerEventData)null);
            relay.AdvanceHoverScale(1f);

            Assert.That(enteredCount, Is.EqualTo(1));
            Assert.That(iconRect.localScale.x, Is.EqualTo(0.44f).Within(0.001f));

            relay.OnPointerExit((PointerEventData)null);
            relay.AdvanceHoverScale(1f);

            Assert.That(exitedCount, Is.EqualTo(1));
            Assert.That(iconRect.localScale.x, Is.EqualTo(0.4f).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(node);
        }
    }
}
