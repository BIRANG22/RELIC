using System;
using System.IO;
using NUnit.Framework;

public sealed class LocalizationMissingTranslationPolicyTests
{
    [Test]
    public void RuntimeLookup_DisablesCrossLocaleFallback()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Core/Localization/GameLocalization.cs");

        int occurrences = source.Split(
            new[] { "FallbackBehavior.DontUseFallback" },
            StringSplitOptions.None).Length - 1;

        Assert.That(occurrences, Is.GreaterThanOrEqualTo(2),
            "선택 로케일의 빈 셀이 한국어 로케일로 fallback되면 안 됩니다.");
        Assert.That(GameLocalization.ResolveMissingTranslation("en", "한국어 원문"),
            Is.EqualTo("Untranslated"));
    }

    [Test]
    public void EditingLock_BypassIsLimitedToNonPlayingEditorState()
    {
        string source = File.ReadAllText(
            "Assets/Project/Scripts/Core/Localization/GameLocalization.cs");

        Assert.That(source, Does.Contain("!Application.isPlaying && !LocalizationEditingLockState.IsEnabledForEditor"),
            "편집 잠금이 꺼져 있어도 Play Mode에서는 선택 언어를 조회해야 합니다.");
    }

    [Test]
    public void EditingLockOff_DoesNotDisableRuntimeLocalizationComponent()
    {
        Assert.That(
            LocalizationEditingLockPolicy.ShouldEnableRuntimeLocalizer(editingLockEnabled: false),
            Is.True,
            "편집 중 한국어 원문을 표시하더라도 Play Mode에서 실행될 로컬라이저 컴포넌트는 꺼 저장하면 안 됩니다.");
    }

    [TestCase(false, null)]
    [TestCase(true, "")]
    [TestCase(true, "   ")]
    public void BlankSelectedLocaleEntry_IsNeverAcceptedAsLocalizedText(bool entryExists, string rawValue)
    {
        Assert.That(
            GameLocalization.ShouldUseSelectedLocaleEntry(entryExists, rawValue),
            Is.False,
            "선택 언어 표에 값이 없으면 다른 로케일의 문자열을 표시 대상으로 인정하면 안 됩니다.");
    }

    [TestCase("미번역")]
    [TestCase("Untranslated")]
    [TestCase("未翻訳")]
    [TestCase("未翻译")]
    [TestCase("Sin traducir")]
    public void MissingTranslationMarkers_AreNeverLocalizationSources(string marker)
    {
        Assert.That(LocalizationTextRules.IsKoreanPlayerText(marker), Is.False);
    }

    [Test]
    public void ExistingRegisteredKey_RemainsAuthoritativeWhenDisplayedTextChanges()
    {
        Assert.That(
            LocalizationBindingSourcePolicy.SelectExistingRegisteredKey(
                "ui.sample.label",
                new[] { "ui.sample.label" }),
            Is.EqualTo("ui.sample.label"));
    }
}
