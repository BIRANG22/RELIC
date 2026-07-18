using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class LobbyRelicOfferButtonUITests
{
    [Test]
    public void Create_UsesTransparentBackgroundImage()
    {
        var parent = new GameObject("Parent", typeof(RectTransform));

        try
        {
            LobbyRelicOfferButtonUI button = LobbyRelicOfferButtonUI.Create(parent.transform, "RelicOffer");

            Assert.That(button.GetComponent<Image>().color.a, Is.Zero);
        }
        finally
        {
            Object.DestroyImmediate(parent);
        }
    }
}
