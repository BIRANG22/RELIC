using System;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

public sealed class LocalizationEditorFontMutationGuardTests
{
    [Test]
    public void Dispose_RestoresDynamicPopulationMode()
    {
        TMP_FontAsset font = ScriptableObject.CreateInstance<TMP_FontAsset>();
        font.atlasPopulationMode = AtlasPopulationMode.Dynamic;

        try
        {
            using (LocalizationEditorFontMutationGuard.Protect(new[] { font }))
                Assert.That(font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Static));

            Assert.That(font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Dynamic));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(font);
        }
    }

    [Test]
    public void Dispose_RestoresPopulationModeWhenOperationThrows()
    {
        TMP_FontAsset font = ScriptableObject.CreateInstance<TMP_FontAsset>();
        font.atlasPopulationMode = AtlasPopulationMode.Dynamic;

        try
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                using (LocalizationEditorFontMutationGuard.Protect(new[] { font }))
                    throw new InvalidOperationException("expected");
            });

            Assert.That(font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Dynamic));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(font);
        }
    }

    [Test]
    public void Protect_LeavesStaticFontStatic()
    {
        TMP_FontAsset font = ScriptableObject.CreateInstance<TMP_FontAsset>();
        font.atlasPopulationMode = AtlasPopulationMode.Static;

        try
        {
            using (LocalizationEditorFontMutationGuard.Protect(new[] { font }))
                Assert.That(font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Static));

            Assert.That(font.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Static));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(font);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Dispose_RestoresOriginalDirtyState(bool initiallyDirty)
    {
        TMP_FontAsset font = ScriptableObject.CreateInstance<TMP_FontAsset>();
        font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        if (initiallyDirty)
            EditorUtility.SetDirty(font);
        else
            EditorUtility.ClearDirty(font);

        try
        {
            using (LocalizationEditorFontMutationGuard.Protect(new[] { font }))
            {
            }

            Assert.That(EditorUtility.IsDirty(font), Is.EqualTo(initiallyDirty));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(font);
        }
    }
}
