using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SoundDatabaseVfxOwnershipTests
{
    private const string DatabasePath = "Assets/DB/SoundDatabase.asset";

    [TestCase(0.5f, 2f, 1f)]
    [TestCase(1f, 4f, 3f)]
    [TestCase(2f, 1.5f, 3f)]
    public void VfxCuePlaybackPitch_CombinesCuePitchAndPresentationSpeed(
        float cuePitch,
        float presentationSpeed,
        float expected)
    {
        Assert.That(
            BattleVfxAudioUtility.CalculatePlaybackPitch(cuePitch, presentationSpeed),
            Is.EqualTo(expected).Within(0.0001f));
    }

    [Test]
    public void ArabellaAttack02PrepareSound_PlaysOncePerAction()
    {
        SoundDatabase database = AssetDatabase.LoadAssetAtPath<SoundDatabase>(DatabasePath);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Project/Art/VFX/Mon/Vfx_Mon_B_01_attack_02_pre.prefab");
        database.Initialize();

        Assert.That(database.TryGetSkillVfxSfx(prefab, out VfxSoundData data), Is.True);
        Assert.That(data.Cues, Has.Count.EqualTo(1));
        Assert.That(data.Cues[0].playOncePerAction, Is.True);
    }

    private static readonly string[] DatabaseOwnedVfxPrefabPaths =
    {
        "Assets/Project/Art/VFX/Cha/Vfx_core_03.prefab",
        "Assets/Project/Art/VFX/Cha/Vfx_core_12.prefab",
        "Assets/Project/Art/VFX/Cha/Vfx_core_13.prefab",
        "Assets/Project/Art/VFX/Cha/Vfx_core_19_r.prefab",
        "Assets/Project/Art/VFX/Cha/Vfx_core_20_r.prefab",
        "Assets/Project/Art/VFX/Cha/Vfx_core_26_r.prefab",
        "Assets/Project/Art/VFX/Cha/Vfx_core_28_r.prefab",
        "Assets/Project/Art/VFX/Cha/Vfx_core_31_r.prefab",
        "Assets/Project/Art/VFX/Cha/Vfx_core_33.prefab",
        "Assets/Project/Art/VFX/Cha/Vfx_S_Ability_07_r.prefab",
        "Assets/Project/Art/VFX/Grid_object/GR_Residue.prefab",
        "Assets/Project/Art/VFX/Grid_object/Vfx_Gr_poisson.prefab",
        "Assets/Project/Art/VFX/Mon/Vfx_Mon_B_01_attack_02_pre.prefab",
    };

    [Test]
    public void DatabaseOwnedVfxPrefabs_DoNotContainEmbeddedAudioSources()
    {
        SoundDatabase database = AssetDatabase.LoadAssetAtPath<SoundDatabase>(DatabasePath);
        database.Initialize();

        foreach (string path in DatabaseOwnedVfxPrefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            Assert.That(prefab, Is.Not.Null, path);
            Assert.That(
                prefab.GetComponentsInChildren<AudioSource>(true),
                Is.Empty,
                path);
            Assert.That(database.TryGetSkillVfxSfx(prefab, out VfxSoundData data), Is.True, path);
            Assert.That(data.HasPlayableCue, Is.True, path);
        }
    }

    [Test]
    public void ProjectOwnedPrefabs_DoNotDependDirectlyOnAudioClips()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Project" }))
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            if (prefabPath.Contains("/Download/"))
                continue;

            string[] audioDependencies = AssetDatabase.GetDependencies(prefabPath, false)
                .Where(IsAudioAssetPath)
                .ToArray();

            Assert.That(audioDependencies, Is.Empty, prefabPath);
        }
    }

    [Test]
    public void PreviouslyUnregisteredVfxPrefabs_AreResolvedBySoundDatabase()
    {
        SoundDatabase database = AssetDatabase.LoadAssetAtPath<SoundDatabase>(DatabasePath);
        Assert.That(database, Is.Not.Null);
        database.Initialize();

        var expectedClipPathsByPrefabPath = new Dictionary<string, string>
        {
            ["Assets/Project/Art/VFX/Grid_object/GR_Residue.prefab"] =
                "Assets/Project/Download/vfx/Vefects/Pixel Craft VFX URP/Audio/WAV/Sergi/SFX_Vefects_Bubbles_Loop_02.wav",
            ["Assets/Project/Art/VFX/Grid_object/Vfx_Gr_poisson.prefab"] =
                "Assets/Project/Download/vfx/Vefects/Pixel Craft VFX URP/Audio/WAV/Sergi/SFX_Vefects_Bubbles_Loop_02.wav",
            ["Assets/Project/Art/VFX/Mon/Vfx_Mon_B_01_attack_02_pre.prefab"] =
                "Assets/Project/Download/vfx/Vefects/Stylized VFX URP/Sounds/Magic Attacks/WAV/SFX_Magic_Attack_Electric_Cast.wav",
        };

        foreach (KeyValuePair<string, string> entry in expectedClipPathsByPrefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.Key);
            AudioClip expectedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(entry.Value);

            Assert.That(prefab, Is.Not.Null, entry.Key);
            Assert.That(expectedClip, Is.Not.Null, entry.Value);
            Assert.That(database.TryGetSkillVfxSfx(prefab, out VfxSoundData data), Is.True, entry.Key);
            Assert.That(data, Is.Not.Null, entry.Key);
            Assert.That(data.HasPlayableCue, Is.True, entry.Key);
            Assert.That(data.Cues[0].clip, Is.SameAs(expectedClip), entry.Key);
            Assert.That(data.Cues[0].volume, Is.EqualTo(1f), entry.Key);
            Assert.That(data.Cues[0].pitch, Is.EqualTo(1f), entry.Key);
            Assert.That(data.Cues[0].loop, Is.False, entry.Key);
            Assert.That(data.Cues[0].overrideSpatialBlend, Is.True, entry.Key);
            Assert.That(data.Cues[0].spatialBlend, Is.EqualTo(1f), entry.Key);
        }
    }

    [Test]
    public void GridEffectVfxSounds_HaveTheirOwnInspectorList()
    {
        SoundDatabase database = AssetDatabase.LoadAssetAtPath<SoundDatabase>(DatabasePath);
        SerializedProperty list = new SerializedObject(database).FindProperty("gridEffectVfxSfxList");

        Assert.That(list, Is.Not.Null);
        Assert.That(list.arraySize, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void SoundUsageScanner_ReportsGridEffectVfxSoundEntries()
    {
        SoundDatabase database = AssetDatabase.LoadAssetAtPath<SoundDatabase>(DatabasePath);
        SoundUsageReport report = SoundUsageScanner.Scan(new SoundUsageScanOptions
        {
            Database = database,
            Prefabs = System.Array.Empty<GameObject>(),
            SkillVfxEntries = System.Array.Empty<Relic.Gameplay.Data.SkillVfxEntry>(),
            PrefabSearchRoots = System.Array.Empty<string>(),
            SourceSearchRoots = System.Array.Empty<string>(),
        });

        string[] gridPaths = report.VfxSoundEntries
            .Where(entry => entry.Group == "Grid Effect")
            .Select(entry => entry.VfxPath)
            .ToArray();

        Assert.That(gridPaths, Has.Length.EqualTo(2));
        Assert.That(gridPaths, Does.Contain("Assets/Project/Art/VFX/Grid_object/GR_Residue.prefab"));
        Assert.That(gridPaths, Does.Contain("Assets/Project/Art/VFX/Grid_object/Vfx_Gr_poisson.prefab"));
    }

    [Test]
    public void VfxSoundCue_SpatialBlendOverrideWinsOverAudioManagerTemplate()
    {
        AudioClip clip = AudioClip.Create("spatial-test", 1, 1, 44100, false);
        GameObject templateObject = new GameObject("AudioTemplate");
        try
        {
            AudioSource template = templateObject.AddComponent<AudioSource>();
            template.spatialBlend = 0f;
            var cue = new VfxSoundCue
            {
                clip = clip,
                overrideSpatialBlend = true,
                spatialBlend = 1f,
            };

            AudioSourcePlaybackSettings settings = AudioSourcePlaybackSettings.From(
                cue,
                Vector3.zero,
                Quaternion.identity,
                template);

            Assert.That(settings.SpatialBlend, Is.EqualTo(1f));
        }
        finally
        {
            Object.DestroyImmediate(templateObject);
            Object.DestroyImmediate(clip);
        }
    }

    private static bool IsAudioAssetPath(string path)
    {
        string extension = System.IO.Path.GetExtension(path);
        return extension.Equals(".wav", System.StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".mp3", System.StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".ogg", System.StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".aif", System.StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".aiff", System.StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".flac", System.StringComparison.OrdinalIgnoreCase);
    }
}
