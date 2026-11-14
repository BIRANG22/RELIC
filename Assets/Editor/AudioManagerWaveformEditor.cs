#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AudioManager))]
public class AudioManagerWaveformInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(8);
        if (GUILayout.Button("Open BGM Waveform Region Editor", GUILayout.Height(28)))
            AudioManagerWaveformEditor.Open((AudioManager)target);
    }
}

public class AudioManagerWaveformEditor : EditorWindow
{
    private AudioManager manager;
    private SerializedObject serialized;
    private int selected;
    private int editMode; // 0 = playback boundaries, 1 = loop boundaries
    private float[] peaks;
    private AudioClip cachedClip;
    private double previewEndTime;
    private bool previewing;
    private string waveformWarning;
    private static readonly Color WaveColor = new Color(.32f, .75f, .95f);
    private static readonly Color RegionColor = new Color(.3f, .9f, .5f, .2f);
    private static readonly Type AudioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
    private int draggedEdge = -1;

    [MenuItem("Tools/Audio/BGM Waveform Region Editor")]
    public static void ShowWindow() => GetWindow<AudioManagerWaveformEditor>("BGM Regions");

    public static void Open(AudioManager audioManager)
    {
        var window = GetWindow<AudioManagerWaveformEditor>("BGM Regions");
        window.manager = audioManager;
        window.selected = 0;
        window.Refresh();
        window.Show();
    }

    private void OnEnable() => EditorApplication.update += UpdatePreview;
    private void OnDisable()
    {
        EditorApplication.update -= UpdatePreview;
        StopPreview();
    }

    private void Refresh()
    {
        serialized = manager == null ? null : new SerializedObject(manager);
        cachedClip = null;
        peaks = null;
    }

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        var next = (AudioManager)EditorGUILayout.ObjectField("Audio Manager", manager, typeof(AudioManager), true);
        if (EditorGUI.EndChangeCheck()) { manager = next; selected = 0; Refresh(); }
        if (serialized == null)
        {
            EditorGUILayout.HelpBox("Assign an AudioManager from the scene, or open from its Inspector.", MessageType.Info);
            return;
        }
        serialized.Update();
        SerializedProperty list = serialized.FindProperty("bgmClipRegions");
        if (list == null) { EditorGUILayout.HelpBox("AudioManager has no region list.", MessageType.Error); return; }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("BGM Clip Regions", EditorStyles.boldLabel);
        if (GUILayout.Button("+ Add", GUILayout.Width(70)))
        {
            list.InsertArrayElementAtIndex(list.arraySize);
            selected = list.arraySize - 1;
            var newItem = list.GetArrayElementAtIndex(selected);
            newItem.FindPropertyRelative("clip").objectReferenceValue = null;
            newItem.FindPropertyRelative("enabled").boolValue = true;
            newItem.FindPropertyRelative("playbackStart").floatValue = 0f;
            newItem.FindPropertyRelative("playbackEnd").floatValue = 0f;
            newItem.FindPropertyRelative("loopStart").floatValue = 0f;
            newItem.FindPropertyRelative("loopEnd").floatValue = 0f;
            newItem.FindPropertyRelative("loop").boolValue = true;
            cachedClip = null;
        }
        GUI.enabled = list.arraySize > 0;
        if (GUILayout.Button("- Remove", GUILayout.Width(85)))
        {
            list.DeleteArrayElementAtIndex(Mathf.Clamp(selected, 0, list.arraySize - 1));
            selected = Mathf.Clamp(selected, 0, list.arraySize - 1);
            cachedClip = null;
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        if (list.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Add a BGM clip to edit its playback and loop boundaries. These settings override BgmClipData loop points for the matching clip.", MessageType.Info);
            serialized.ApplyModifiedProperties();
            return;
        }
        string[] names = new string[list.arraySize];
        for (int i = 0; i < list.arraySize; i++)
        {
            var item = list.GetArrayElementAtIndex(i);
            var clipProp = item.FindPropertyRelative("clip");
            names[i] = clipProp.objectReferenceValue != null ? clipProp.objectReferenceValue.name : "(Select AudioClip) " + (i + 1);
        }
        selected = EditorGUILayout.Popup("Clip", Mathf.Clamp(selected, 0, list.arraySize - 1), names);
        SerializedProperty region = list.GetArrayElementAtIndex(selected);
        EditorGUILayout.PropertyField(region.FindPropertyRelative("clip"));
        EditorGUILayout.PropertyField(region.FindPropertyRelative("enabled"));
        EditorGUILayout.PropertyField(region.FindPropertyRelative("loop"), new GUIContent("Repeat loop region"));
        var clip = region.FindPropertyRelative("clip").objectReferenceValue as AudioClip;
        if (clip == null)
        {
            EditorGUILayout.HelpBox("Choose an AudioClip.", MessageType.Info);
            serialized.ApplyModifiedProperties();
            return;
        }
        if (cachedClip != clip) BuildWaveform(clip);
        float length = clip.length;
        var start = region.FindPropertyRelative("playbackStart");
        var end = region.FindPropertyRelative("playbackEnd");
        var loopStart = region.FindPropertyRelative("loopStart");
        var loopEnd = region.FindPropertyRelative("loopEnd");
        float pStart = Mathf.Clamp(start.floatValue, 0, Mathf.Max(0, length - .001f));
        float pEnd = end.floatValue <= 0 ? length : Mathf.Clamp(end.floatValue, pStart + .001f, length);
        float lStart = Mathf.Clamp(loopStart.floatValue, pStart, pEnd - .001f);
        float lEnd = loopEnd.floatValue <= 0 ? pEnd : Mathf.Clamp(loopEnd.floatValue, lStart + .001f, pEnd);

        GUILayout.Space(6);
        editMode = GUILayout.Toolbar(editMode, new[] { "Edit Playback Start/End", "Edit Loop Start/End" });
        Rect waveform = GUILayoutUtility.GetRect(10, 145, GUILayout.ExpandWidth(true));
        DrawWaveform(waveform, length, pStart, pEnd, lStart, lEnd);
        HandleWaveformInput(waveform, length, region, pStart, pEnd, lStart, lEnd);
        if (!string.IsNullOrEmpty(waveformWarning)) EditorGUILayout.HelpBox(waveformWarning, MessageType.Warning);

        EditorGUILayout.LabelField("Playback region", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        float newPStart = EditorGUILayout.FloatField("Start (seconds)", pStart);
        float newPEnd = EditorGUILayout.FloatField("End (seconds)", pEnd);
        if (EditorGUI.EndChangeCheck())
        {
            start.floatValue = Mathf.Clamp(newPStart, 0f, length - .001f);
            end.floatValue = Mathf.Clamp(newPEnd, start.floatValue + .001f, length);
        }
        EditorGUILayout.LabelField("Loop region (after first playback reaches Loop End)", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        float newLStart = EditorGUILayout.FloatField("Loop Start (seconds)", lStart);
        float newLEnd = EditorGUILayout.FloatField("Loop End (seconds)", lEnd);
        if (EditorGUI.EndChangeCheck())
        {
            loopStart.floatValue = Mathf.Clamp(newLStart, pStart, pEnd - .001f);
            loopEnd.floatValue = Mathf.Clamp(newLEnd, loopStart.floatValue + .001f, pEnd);
        }
        EditorGUILayout.LabelField($"Duration: {length:F3}s   |   Rate: {clip.frequency} Hz   |   Channels: {clip.channels}");
        GUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Preview Playback")) StartPreview(clip, pStart, pEnd);
        if (GUILayout.Button("Preview Loop")) StartPreview(clip, lStart, lEnd);
        if (GUILayout.Button("Stop")) StopPreview();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox("Drag near a boundary line to move it. Click elsewhere to place the nearest boundary. Green = playback region; yellow lines = loop region. Original audio files are not modified. Preview is one pass of the selected region.", MessageType.None);
        if (serialized.ApplyModifiedProperties()) EditorUtility.SetDirty(manager);
    }

    private void BuildWaveform(AudioClip clip)
    {
        StopPreview();
        cachedClip = clip;
        waveformWarning = null;
        peaks = new float[768];
        if (clip.samples <= 0 || clip.channels <= 0) return;
        // Each column samples a short block distributed across the entire file.
        // This limits memory use even for multi-minute music tracks.
        int block = Mathf.Min(1024, Mathf.Max(16, clip.samples / peaks.Length));
        float[] buffer = new float[block * clip.channels];
        bool readable = true;
        for (int i = 0; i < peaks.Length; i++)
        {
            int pos = Mathf.Min(Mathf.RoundToInt(i * (clip.samples - block) / (float)Mathf.Max(1, peaks.Length - 1)), Mathf.Max(0, clip.samples - block));
            if (!clip.GetData(buffer, Mathf.Max(0, pos))) { readable = false; break; }
            float peak = 0f;
            for (int k = 0; k < buffer.Length; k++) peak = Mathf.Max(peak, Mathf.Abs(buffer[k]));
            peaks[i] = peak;
        }
        if (!readable)
        {
            peaks = null;
            waveformWarning = "Waveform data is unavailable. Try AudioClip Import Settings > Load Type: Decompress On Load. The region fields still work.";
        }
    }

    private void DrawWaveform(Rect rect, float duration, float ps, float pe, float ls, float le)
    {
        EditorGUI.DrawRect(rect, new Color(.12f, .14f, .17f));
        Rect green = new Rect(rect.x + rect.width * ps / duration, rect.y, rect.width * (pe - ps) / duration, rect.height);
        EditorGUI.DrawRect(green, RegionColor);
        if (peaks != null)
        {
            Handles.BeginGUI();
            Handles.color = WaveColor;
            float mid = rect.center.y;
            for (int x = 0; x < Mathf.RoundToInt(rect.width); x++)
            {
                int idx = Mathf.Clamp(Mathf.FloorToInt(x / rect.width * peaks.Length), 0, peaks.Length - 1);
                float h = peaks[idx] * rect.height * .43f;
                Handles.DrawLine(new Vector3(rect.x + x, mid - h), new Vector3(rect.x + x, mid + h));
            }
            Handles.EndGUI();
        }
        DrawBoundary(rect, duration, ps, new Color(.2f, 1f, .4f), "P Start");
        DrawBoundary(rect, duration, pe, new Color(.2f, 1f, .4f), "P End");
        DrawBoundary(rect, duration, ls, new Color(1f, .76f, .18f), "L Start");
        DrawBoundary(rect, duration, le, new Color(1f, .76f, .18f), "L End");
    }

    private static void DrawBoundary(Rect rect, float length, float seconds, Color color, string title)
    {
        float x = rect.x + rect.width * seconds / length;
        EditorGUI.DrawRect(new Rect(x - 1f, rect.y, 2f, rect.height), color);
        GUI.Label(new Rect(Mathf.Clamp(x - 24, rect.x, rect.xMax - 58), rect.y + (title.EndsWith("End") ? 18 : 0), 62, 16), title, EditorStyles.miniLabel);
    }

    private void HandleWaveformInput(Rect rect, float length, SerializedProperty region, float ps, float pe, float ls, float le)
    {
        Event e = Event.current;
        if (!rect.Contains(e.mousePosition) && e.type == EventType.MouseDown) return;
        if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
        {
            float x1 = rect.x + rect.width * (editMode == 0 ? ps : ls) / length;
            float x2 = rect.x + rect.width * (editMode == 0 ? pe : le) / length;
            draggedEdge = Mathf.Abs(e.mousePosition.x - x1) <= Mathf.Abs(e.mousePosition.x - x2) ? 0 : 1;
            GUI.FocusControl(null);
            e.Use();
        }
        if (draggedEdge < 0) return;
        if (e.type == EventType.MouseDrag || e.type == EventType.MouseDown)
        {
            float value = Mathf.Clamp01((e.mousePosition.x - rect.x) / rect.width) * length;
            string field = editMode == 0 ? (draggedEdge == 0 ? "playbackStart" : "playbackEnd") :
                (draggedEdge == 0 ? "loopStart" : "loopEnd");
            float minimum = editMode == 0 ? 0 : ps;
            float maximum = editMode == 0 ? length : pe;
            if (draggedEdge == 0) maximum = Mathf.Min(maximum - .001f, editMode == 0 ? pe - .001f : le - .001f);
            else minimum = Mathf.Max(minimum + .001f, editMode == 0 ? ps + .001f : ls + .001f);
            region.FindPropertyRelative(field).floatValue = Mathf.Clamp(value, minimum, maximum);
            Repaint();
            e.Use();
        }
        if (e.type == EventType.MouseUp) { draggedEdge = -1; e.Use(); }
    }

    private static MethodInfo GetAudioMethod(string name) => AudioUtil?.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    private void StartPreview(AudioClip clip, float start, float end)
    {
        StopPreview();
        try
        {
            MethodInfo method = GetAudioMethod("PlayPreviewClip") ?? GetAudioMethod("PlayClip");
            if (method == null) { Debug.LogWarning("Audio preview API is unavailable in this Unity editor version."); return; }
            var args = method.GetParameters();
            if (args.Length == 3) method.Invoke(null, new object[] { clip, Mathf.RoundToInt(start * clip.frequency), false });
            else if (args.Length == 2) method.Invoke(null, new object[] { clip, Mathf.RoundToInt(start * clip.frequency) });
            else method.Invoke(null, new object[] { clip });
            previewEndTime = EditorApplication.timeSinceStartup + (end - start);
            previewing = true;
        }
        catch (Exception ex) { Debug.LogWarning("Audio preview failed: " + ex.Message); }
    }
    private void StopPreview()
    {
        if (!previewing) return;
        previewing = false;
        try { (GetAudioMethod("StopAllPreviewClips") ?? GetAudioMethod("StopAllClips"))?.Invoke(null, null); }
        catch { /* preview API may differ across Unity versions */ }
    }
    private void UpdatePreview()
    {
        if (previewing && EditorApplication.timeSinceStartup >= previewEndTime) StopPreview();
    }
}
#endif
