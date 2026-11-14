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
}
