using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;

/// <summary>
/// 에디터 자동화 중 TMP가 Dynamic 폰트 아틀라스를 변경하지 못하게 하고 원래 상태를 복원합니다.
/// </summary>
public sealed class LocalizationEditorFontMutationGuard : IDisposable
{
    private readonly FontState[] states;
    private bool disposed;

    private LocalizationEditorFontMutationGuard(IEnumerable<TMP_FontAsset> fonts)
    {
        states = (fonts ?? Array.Empty<TMP_FontAsset>())
            .Where(font => font != null)
            .Distinct()
            .Select(font => new FontState(
                font,
                font.atlasPopulationMode,
                EditorUtility.IsDirty(font)))
            .ToArray();

        foreach (FontState state in states)
        {
            if (state.Font.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                state.Font.atlasPopulationMode = AtlasPopulationMode.Static;
        }
    }

    public static LocalizationEditorFontMutationGuard ProtectAllProjectFonts()
    {
        TMP_FontAsset[] fonts = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>)
            .Where(font => font != null)
            .ToArray();
        return Protect(fonts);
    }

    public static LocalizationEditorFontMutationGuard Protect(IEnumerable<TMP_FontAsset> fonts) =>
        new LocalizationEditorFontMutationGuard(fonts);

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;

        foreach (FontState state in states)
        {
            if (state.Font == null)
                continue;

            state.Font.atlasPopulationMode = state.PopulationMode;
            if (state.WasDirty)
                EditorUtility.SetDirty(state.Font);
            else
                EditorUtility.ClearDirty(state.Font);
        }
    }

    private readonly struct FontState
    {
        public TMP_FontAsset Font { get; }
        public AtlasPopulationMode PopulationMode { get; }
        public bool WasDirty { get; }

        public FontState(TMP_FontAsset font, AtlasPopulationMode populationMode, bool wasDirty)
        {
            Font = font;
            PopulationMode = populationMode;
            WasDirty = wasDirty;
        }
    }
}
