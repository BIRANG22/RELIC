using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

public sealed class RecordIconSlotUnconfirmedFontTests
{
    private const string PrefabPath = "Assets/Project/PrefabsR/RecordIconSlot.prefab";
    private const string SafeFontPath = "Assets/Fonts/TMP/DungGeunMo SDF.asset";

    [Test]
    public void UnconfirmedLabel_UsesFontWithCorrectQuestionMarkGlyph()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        TMP_FontAsset safeFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SafeFontPath);

        Assert.That(prefab, Is.Not.Null);
        Assert.That(safeFont, Is.Not.Null);

        TMP_Text label = prefab.GetComponentsInChildren<TMP_Text>(true)
            .SingleOrDefault(text => text.name == "Unconfirmed");

        Assert.That(label, Is.Not.Null);
        Assert.That(label.text, Is.EqualTo("?"));
        Assert.That(label.font, Is.SameAs(safeFont));
        Assert.That(label.font.HasCharacter('?'), Is.True);
    }
}
