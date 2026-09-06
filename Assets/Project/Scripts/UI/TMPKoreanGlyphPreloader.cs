using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class TMPKoreanGlyphPreloader : MonoBehaviour
{
    [Header("Target Font Assets")]
    [SerializeField] private TMP_FontAsset[] targetFonts;

    [Header("Resources TextAsset Paths")]
    [SerializeField] private string[] resourcesTextAssetPaths = { "Data/GameData" };

    [Header("Characters")]
    [SerializeField] private bool includeBasicAscii = true;
    [SerializeField] private bool includeKoreanJamo = true;
    [SerializeField] private bool includeAllModernHangul = false;
    [SerializeField] private string extraCharacters = "";

    [Header("Options")]
    [SerializeField] private bool preloadOnAwake = true;
    [SerializeField] private bool logResult = false;

    [Header("Editor Safety")]
    [Tooltip("에디터 플레이 중 프로젝트에 저장된 TMP FontAsset(.asset)을 직접 수정하지 않습니다. 켜두면 NativeFormatImporter inconsistent result 경고와 순간 렉을 줄일 수 있습니다.")]
    [SerializeField] private bool preventEditorAssetModification = true;

    [Tooltip("에디터 플레이 중 원본 FontAsset을 수정하지 않도록 독립된 런타임 복제본에서 글리프를 미리 준비합니다. 씬의 TMP_Text에는 복제본을 직접 연결하지 않습니다.")]
    [SerializeField] private bool useRuntimeFontClonesInEditor = true;

    [Tooltip("씬 로드 후 새로 등장한 TMP_Text가 사용하는 FontAsset도 런타임 복제본에 미리 준비합니다. TMP_Text의 실제 FontAsset 참조는 변경하지 않습니다.")]
    [SerializeField] private bool replaceTextFontsOnSceneLoaded = true;

    [Tooltip("스킬 툴팁/팝업처럼 플레이 중 생성되는 TMP_Text의 FontAsset도 주기적으로 확인해 런타임 복제본을 준비합니다. TMP_Text의 실제 FontAsset 참조는 변경하지 않습니다.")]
    [SerializeField] private bool scanRuntimeTextsInEditor = true;

    [SerializeField, Min(0.05f)] private float runtimeTextScanInterval = 0.25f;

#if UNITY_EDITOR
    private readonly Dictionary<TMP_FontAsset, TMP_FontAsset> runtimeFontClones = new Dictionary<TMP_FontAsset, TMP_FontAsset>();
    private float nextRuntimeTextScanTime;
#endif

    private bool hasPreloaded;

    private void Awake()
    {
#if UNITY_EDITOR
        if (Application.isPlaying && useRuntimeFontClonesInEditor)
        {
            CreateRuntimeFontClonesFromTargets();
            ReplaceLoadedTextFontsWithRuntimeClones();

            if (replaceTextFontsOnSceneLoaded)
                SceneManager.sceneLoaded += OnSceneLoaded;
        }
#endif

        if (preloadOnAwake)
            Preload();
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR
        if (replaceTextFontsOnSceneLoaded)
            SceneManager.sceneLoaded -= OnSceneLoaded;

        if (Application.isPlaying && useRuntimeFontClonesInEditor)
        {
            RestoreLoadedTextFontsToOriginals();
            DestroyRuntimeFontClones();
        }
#endif
    }

    private void LateUpdate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying || !useRuntimeFontClonesInEditor || !scanRuntimeTextsInEditor)
            return;

        if (Time.unscaledTime < nextRuntimeTextScanTime)
            return;

        nextRuntimeTextScanTime = Time.unscaledTime + runtimeTextScanInterval;
        ReplaceLoadedTextFontsWithRuntimeClones();
#endif
    }

#if UNITY_EDITOR
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!Application.isPlaying || !useRuntimeFontClonesInEditor)
            return;

        ReplaceLoadedTextFontsWithRuntimeClones();
    }
#endif

    public void Preload()
    {
        if (hasPreloaded)
            return;

        hasPreloaded = true;

        if (targetFonts == null || targetFonts.Length == 0)
            return;

        string characters = BuildCharacters();
        if (string.IsNullOrEmpty(characters))
            return;

        for (int i = 0; i < targetFonts.Length; i++)
        {
            TMP_FontAsset font = targetFonts[i];
            if (font == null)
                continue;

            TryPreloadFont(font, characters);
        }
    }

    private void TryPreloadFont(TMP_FontAsset font, string characters)
    {
        TMP_FontAsset fontToUse = GetRuntimeFontIfAvailable(font);

        if (fontToUse == null)
            return;

        if (fontToUse.atlasPopulationMode == AtlasPopulationMode.Static)
        {
            if (logResult)
                Debug.Log($"[TMPKoreanGlyphPreloader] '{fontToUse.name}'은 Static Font Asset이라 런타임 글리프 추가를 건너뜁니다.", fontToUse);

            return;
        }

#if UNITY_EDITOR
        if (preventEditorAssetModification && EditorApplication.isPlaying && AssetDatabase.Contains(fontToUse))
        {
            if (logResult)
            {
                Debug.Log(
                    $"[TMPKoreanGlyphPreloader] 에디터 플레이 중 프로젝트 원본 FontAsset '{fontToUse.name}' 수정을 건너뜁니다. " +
                    "원본 TMP FontAsset을 동적으로 수정하면 NativeFormatImporter inconsistent result 경고와 순간 렉이 발생할 수 있습니다.",
                    fontToUse);
            }

            return;
        }
#endif

        try
        {
            fontToUse.TryAddCharacters(characters, out string missingCharacters);

            if (logResult)
            {
                int missingCount = string.IsNullOrEmpty(missingCharacters) ? 0 : missingCharacters.Length;
                Debug.Log($"[TMPKoreanGlyphPreloader] '{fontToUse.name}' 글리프 프리로드 완료. Missing Count: {missingCount}", fontToUse);
            }
        }
        catch (System.Exception exception)
        {
            if (logResult)
                Debug.LogWarning($"[TMPKoreanGlyphPreloader] '{fontToUse.name}' 글리프 프리로드 중 예외가 발생했습니다. {exception.Message}", fontToUse);
        }
    }

    private TMP_FontAsset GetRuntimeFontIfAvailable(TMP_FontAsset font)
    {
#if UNITY_EDITOR
        if (Application.isPlaying && useRuntimeFontClonesInEditor && font != null)
            return GetOrCreateRuntimeClone(font);
#endif

        return font;
    }

#if UNITY_EDITOR
    private void CreateRuntimeFontClonesFromTargets()
    {
        if (targetFonts == null)
            return;

        for (int i = 0; i < targetFonts.Length; i++)
        {
            TMP_FontAsset source = targetFonts[i];
            if (source == null)
                continue;

            GetOrCreateRuntimeClone(source);
        }
    }

    private TMP_FontAsset GetOrCreateRuntimeClone(TMP_FontAsset source)
    {
        if (source == null)
            return null;

        if (runtimeFontClones.TryGetValue(source, out TMP_FontAsset cachedClone) && cachedClone != null)
            return cachedClone;

        Font sourceFontFile = source.sourceFontFile;
        if (sourceFontFile == null)
        {
            if (logResult)
            {
                Debug.LogWarning(
                    $"[TMPKoreanGlyphPreloader] '{source.name}'의 Source Font File을 찾을 수 없어 에디터 런타임 FontAsset 생성을 건너뜁니다.",
                    source);
            }

            return null;
        }

        // 기존 TMP_FontAsset을 Instantiate하면 atlas/material 서브 에셋 참조까지 복제되어
        // 파괴 시 프로젝트 에셋을 제거하려는 문제가 생길 수 있습니다.
        // 원본 Font 파일에서 독립된 런타임 FontAsset을 새로 만들어 사용합니다.
        TMP_FontAsset clone = TMP_FontAsset.CreateFontAsset(sourceFontFile);
        if (clone == null)
        {
            if (logResult)
                Debug.LogWarning($"[TMPKoreanGlyphPreloader] '{source.name}'의 런타임 FontAsset 생성에 실패했습니다.", source);

            return null;
        }

        clone.name = source.name + " Runtime Clone";
        clone.hideFlags = HideFlags.DontSaveInBuild;

        if (clone.material != null)
            clone.material.hideFlags = HideFlags.DontSaveInBuild;

        Texture2D[] runtimeAtlases = clone.atlasTextures;
        if (runtimeAtlases != null)
        {
            for (int i = 0; i < runtimeAtlases.Length; i++)
            {
                if (runtimeAtlases[i] != null)
                    runtimeAtlases[i].hideFlags = HideFlags.DontSaveInBuild;
            }
        }

        runtimeFontClones[source] = clone;

        CloneFallbackFontList(source, clone);
        return clone;
    }

    private void CloneFallbackFontList(TMP_FontAsset source, TMP_FontAsset clone)
    {
        if (source == null || clone == null || source.fallbackFontAssetTable == null)
            return;

        List<TMP_FontAsset> clonedFallbacks = new List<TMP_FontAsset>(source.fallbackFontAssetTable.Count);

        for (int i = 0; i < source.fallbackFontAssetTable.Count; i++)
        {
            TMP_FontAsset fallback = source.fallbackFontAssetTable[i];
            if (fallback == null)
                continue;

            if (fallback == source)
                clonedFallbacks.Add(clone);
            else
                clonedFallbacks.Add(GetOrCreateRuntimeClone(fallback));
        }

        clone.fallbackFontAssetTable = clonedFallbacks;
    }

    private void ReplaceLoadedTextFontsWithRuntimeClones()
    {
        // CreateFontAsset()으로 만든 런타임 FontAsset을 씬 TMP_Text.font에 직접 연결하면
        // Inspector 직렬화 과정에서 kDontSaveInEditor Assertion이 발생할 수 있습니다.
        // 기존 검색/복제 구조는 유지하되 TMP_Text의 실제 FontAsset 참조는 변경하지 않습니다.
        TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        int preparedCount = 0;

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null || text.font == null)
                continue;

            if (EditorUtility.IsPersistent(text))
                continue;

            if (!text.gameObject.scene.IsValid())
                continue;

            TMP_FontAsset sourceFont = GetOriginalFontFromRuntimeClone(text.font) ?? text.font;
            TMP_FontAsset clone = GetOrCreateRuntimeClone(sourceFont);
            if (clone != null)
                preparedCount++;
        }

        if (logResult && preparedCount > 0)
        {
            Debug.Log(
                $"[TMPKoreanGlyphPreloader] TMP_Text {preparedCount}개의 FontAsset에 대응하는 에디터 런타임 복제본을 준비했습니다. " +
                "씬 TMP_Text의 FontAsset 참조는 변경하지 않습니다.",
                this);
        }
    }

    private void RestoreLoadedTextFontsToOriginals()
    {
        TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null || text.font == null)
                continue;

            if (EditorUtility.IsPersistent(text))
                continue;

            if (!text.gameObject.scene.IsValid())
                continue;

            TMP_FontAsset original = GetOriginalFontFromRuntimeClone(text.font);
            if (original == null)
                continue;

            text.font = original;
            text.SetAllDirty();
        }
    }

    private void DestroyRuntimeFontClones()
    {
        foreach (KeyValuePair<TMP_FontAsset, TMP_FontAsset> pair in runtimeFontClones)
        {
            TMP_FontAsset clone = pair.Value;
            if (clone == null)
                continue;

            Destroy(clone);
        }

        runtimeFontClones.Clear();
    }

    private bool IsRuntimeFontClone(TMP_FontAsset font)
    {
        return GetOriginalFontFromRuntimeClone(font) != null;
    }

    private TMP_FontAsset GetOriginalFontFromRuntimeClone(TMP_FontAsset possibleClone)
    {
        if (possibleClone == null)
            return null;

        foreach (KeyValuePair<TMP_FontAsset, TMP_FontAsset> pair in runtimeFontClones)
        {
            if (pair.Value == possibleClone)
                return pair.Key;
        }

        return null;
    }
#endif

    private string BuildCharacters()
    {
        HashSet<char> set = new HashSet<char>();

        AddTextAssetCharacters(set);

        if (includeBasicAscii)
            AddRange(set, 0x20, 0x7E);

        if (includeKoreanJamo)
        {
            AddRange(set, 0x3131, 0x318E);
            AddRange(set, 0x1100, 0x11FF);
        }

        if (includeAllModernHangul)
            AddRange(set, 0xAC00, 0xD7A3);

        if (!string.IsNullOrEmpty(extraCharacters))
        {
            for (int i = 0; i < extraCharacters.Length; i++)
                set.Add(extraCharacters[i]);
        }

        StringBuilder builder = new StringBuilder(set.Count);
        foreach (char c in set)
            builder.Append(c);

        return builder.ToString();
    }

    private void AddTextAssetCharacters(HashSet<char> set)
    {
        if (resourcesTextAssetPaths == null)
            return;

        for (int i = 0; i < resourcesTextAssetPaths.Length; i++)
        {
            string path = resourcesTextAssetPaths[i];
            if (string.IsNullOrWhiteSpace(path))
                continue;

            TextAsset asset = Resources.Load<TextAsset>(path);
            if (asset == null || string.IsNullOrEmpty(asset.text))
                continue;

            string text = asset.text;
            for (int c = 0; c < text.Length; c++)
                set.Add(text[c]);
        }
    }

    private static void AddRange(HashSet<char> set, int startInclusive, int endInclusive)
    {
        for (int code = startInclusive; code <= endInclusive; code++)
            set.Add((char)code);
    }
}
