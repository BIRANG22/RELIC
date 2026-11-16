using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

public sealed class LocaleFontGlyphAtlasConsistencyTests
{
    private const string DatabasePath = "Assets/Resources/Localization/LocaleFontDatabase.asset";

    [TestCase("Assets/Fonts/TMP/Pretendard-Bold SDF.asset")]
    [TestCase("Assets/Fonts/TMP/PretendardJP-Bold SDF.asset")]
    [TestCase("Assets/Fonts/TMP/ZiTiQuanWeiJunHei-W2-2 SDF.asset")]
    [TestCase("Assets/Fonts/TMP/ZhiyiSans-Bold SDF.asset")]
    public void LocaleFont_IsDynamicAndAllowsAdditionalAtlasTextures(string assetPath)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);

        Assert.That(font, Is.Not.Null, assetPath);
        Assert.That(font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Dynamic), assetPath);
        Assert.That(font.isMultiAtlasTexturesEnabled, Is.True, assetPath);
    }

    [TestCase("Assets/Fonts/TMP/Pretendard-Bold SDF.asset", "탐사진행 이름 버리기 스킬 휴식 정예 보스 사건")]
    [TestCase("Assets/Fonts/TMP/Pretendard-Bold SDF.asset", "Exploración Recompensa Selección")]
    [TestCase("Assets/Fonts/TMP/PretendardJP-Bold SDF.asset", "探索を続ける 報酬 選択 休息")]
    [TestCase("Assets/Fonts/TMP/ZiTiQuanWeiJunHei-W2-2 SDF.asset", "继续探索 奖励 选择 休息")]
    public void LocaleFont_SourceContainsRepresentativeTexts(string assetPath, string content)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);

        Assert.That(font, Is.Not.Null);
        Assert.That(font.sourceFontFile, Is.Not.Null);
        foreach (char character in content)
        {
            if (char.IsWhiteSpace(character))
                continue;
            Assert.That(font.sourceFontFile.HasCharacter(character), Is.True, $"Missing '{character}'");
        }
    }

    [Test]
    public void TraditionalChinese_UsesFontWhoseSourceContainsRepresentativeDatabaseText()
    {
        LocaleFontDatabase database = AssetDatabase.LoadAssetAtPath<LocaleFontDatabase>(DatabasePath);

        Assert.That(database, Is.Not.Null);
        Assert.That(database.TryResolve("zh-Hant", LocaleFontDatabase.BodyRoleId, out TMP_FontAsset font), Is.True);
        Assert.That(font.name, Is.EqualTo("ZhiyiSans-Bold SDF"));
        foreach (char character in "繼續探索獎勵選擇")
            Assert.That(font.sourceFontFile.HasCharacter(character), Is.True, $"Missing '{character}'");
    }

    [Test]
    public void ApplyResolvedFont_ReplacesOnlyIncompatibleMaterialAtlas()
    {
        TMP_FontAsset oldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Fonts/TMP/DungGeunMo SDF.asset");
        TMP_FontAsset localeFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Fonts/TMP/Pretendard-Bold SDF.asset");
        var gameObject = new GameObject("Locale Font Material Test", typeof(RectTransform));
        TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();

        try
        {
            text.font = oldFont;
            text.fontSharedMaterial = oldFont.material;

            bool changed = LocaleTMPFontGuard.ApplyResolvedFont(text, localeFont);

            Assert.That(changed, Is.True);
            Assert.That(text.font, Is.SameAs(localeFont));
            Assert.That(text.fontSharedMaterial.mainTexture, Is.SameAs(localeFont.material.mainTexture));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }
}
