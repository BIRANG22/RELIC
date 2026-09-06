using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class CultureTankInventorySlotClickRelayTests
{
    [Test]
    public void ButtonClick_ForwardsConfiguredItemIdExactlyOnce()
    {
        GameObject slotObject = new("CultureTankInventorySlot", typeof(RectTransform), typeof(Button));
        try
        {
            Button button = slotObject.GetComponent<Button>();
            CultureTankInventorySlotClickRelay relay = slotObject.AddComponent<CultureTankInventorySlotClickRelay>();
            string selectedItemId = null;
            int clickCount = 0;

            relay.Configure(button, "Item_A", true, itemId =>
            {
                selectedItemId = itemId;
                clickCount++;
            });
            button.onClick.Invoke();

            Assert.That(selectedItemId, Is.EqualTo("Item_A"));
            Assert.That(clickCount, Is.EqualTo(1));
        }
        finally { Object.DestroyImmediate(slotObject); }
    }

    [Test]
    public void ButtonClick_DoesNotForwardWhenSelectionIsDisabled()
    {
        GameObject slotObject = new("CultureTankInventorySlot", typeof(RectTransform), typeof(Button));
        try
        {
            Button button = slotObject.GetComponent<Button>();
            CultureTankInventorySlotClickRelay relay = slotObject.AddComponent<CultureTankInventorySlotClickRelay>();
            int clickCount = 0;

            relay.Configure(button, "Item_A", false, _ => clickCount++);
            button.onClick.Invoke();

            Assert.That(clickCount, Is.Zero);
            Assert.That(button.interactable, Is.False);
        }
        finally { Object.DestroyImmediate(slotObject); }
    }
}
