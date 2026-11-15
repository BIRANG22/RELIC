using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

public class FontQuestionMarkFallbackTests
{
    private static readonly string[] DungGeunMoFontPaths =
    {
        "Assets/Fonts/TMP/DungGeunMo SDF.asset",
        "Assets/Fonts/TMP/DungGeunMo Legacy SDF.asset"
    };

    [TestCaseSource(nameof(DungGeunMoFontPaths))]
    public void QuestionMark_UsesReadableFallbackGlyph(string assetPath)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);

        Assert.That(font, Is.Not.Null, assetPath);
        Assert.That(font.characterTable.Any(character => character.unicode == '?'), Is.False,
            "잘못된 직접 ? 글리프가 남아 있습니다.");
        Assert.That(font.fallbackFontAssetTable, Is.Not.Null.And.Not.Empty);
        Assert.That(font.HasCharacter('?', searchFallbacks: false, tryAddCharacter: true), Is.False,
            "원본 TTF에서 잘못된 ? 글리프가 동적으로 다시 추가됩니다.");
        Assert.That(font.fallbackFontAssetTable.Any(fallback => fallback != null && fallback.HasCharacter('?')),
            Is.True, "? 문자를 제공하는 폴백 폰트가 필요합니다.");
        Assert.That(font.HasCharacter('?', searchFallbacks: true, tryAddCharacter: true), Is.True,
            "폴백 검색을 포함하면 정상 ? 문자를 찾을 수 있어야 합니다.");
    }

    [Test]
    public void CompoundRecipePrefab_QuestionObjectsUseAuthoredQuestionSprite()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Project/PrefabsR/CompoundRecipeSlot.prefab");
        Sprite questionSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Project/Art/Image/FontImage/question.png");

        Assert.That(prefab, Is.Not.Null);
        UnityEngine.UI.Image[] questionImages = prefab.GetComponentsInChildren<UnityEngine.UI.Image>(true)
            .Where(image => image.gameObject.name == "qus")
            .ToArray();
        Assert.That(questionImages, Has.Length.EqualTo(4));
        Assert.That(questionImages.All(image => image.sprite == questionSprite), Is.True);
        Assert.That(prefab.GetComponentsInChildren<TMP_Text>(true)
            .Any(text => text.gameObject.name == "qus"), Is.False);
    }

    [Test]
    public void CompoundRecipePrefab_ValueUsesQuestionMarkFallback()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Project/PrefabsR/CompoundRecipeSlot.prefab");
        TMP_FontAsset legacyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Fonts/TMP/DungGeunMo Legacy SDF.asset");

        Assert.That(prefab, Is.Not.Null);
        Assert.That(legacyFont, Is.Not.Null);

        TMP_Text valueText = prefab.GetComponentsInChildren<TMP_Text>(true)
            .Single(text => text.gameObject.name == "Value");

        Assert.That(valueText.font, Is.SameAs(legacyFont),
            "미발견 레시피의 ???를 표시하는 Value가 검증 대상 Legacy 폰트를 사용해야 합니다.");
        Assert.That(valueText.text, Does.Contain("?"));
        Assert.That(legacyFont.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Static),
            "원본 TTF에서 잘못된 ? 글리프가 동적으로 다시 추가되지 않도록 해야 합니다.");
        Assert.That(legacyFont.characterTable.Any(character => character.unicode == '?'), Is.False,
            "Value가 잘못된 직접 ? 글리프를 사용하게 됩니다.");
        Assert.That(legacyFont.fallbackFontAssetTable.Any(
                fallback => fallback != null && fallback.HasCharacter('?')),
            Is.True, "Value의 ?를 제공하는 정상 폴백 폰트가 필요합니다.");
    }
}
