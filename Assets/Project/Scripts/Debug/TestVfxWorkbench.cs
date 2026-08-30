using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

public sealed class TestVfxWorkbench : MonoBehaviour
{
    private enum SpawnTarget
    {
        Player,
        Monster,
        Midpoint,
        WorldPoint
    }

    [Header("Scene Setup")]
    [SerializeField] private bool setupOnAwake = true;
    [SerializeField] private bool frameCameraOnAwake = true;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private string playerPrefabPath = "Assets/Project/PrefabsR/Character/A/A_BattlePrefab.prefab";
    [SerializeField] private string monsterPrefabPath = "Assets/Project/PrefabsR/Monster/Muck/Slime.prefab";
    [SerializeField] private Vector3 playerPosition = new Vector3(-2.4f, -0.6f, 0f);
    [SerializeField] private Vector3 monsterPosition = new Vector3(2.4f, -0.6f, 0f);
    [SerializeField] private Vector3 worldPoint = Vector3.zero;

    [Header("VFX Discovery")]
    [SerializeField] private GameObject[] manualVfxPrefabs = Array.Empty<GameObject>();
    [SerializeField] private string[] editorVfxSearchFolders = { "Assets/Project/Art/VFX" };
    [SerializeField] private int maxVisibleVfxSearchResults = 120;

    [Header("Workbench")]
    [SerializeField] private TestVfxSpawnSettings settings = new TestVfxSpawnSettings();
    [SerializeField] private SpawnTarget target = SpawnTarget.Player;
    [SerializeField] private bool followTarget = true;
    [SerializeField] private bool repeat;
    [SerializeField] private float repeatInterval = 1f;
    [SerializeField] private string statusEffectId = "E_Poison";

    [Header("Shortcuts")]
    [SerializeField] private KeyCode togglePanelKey = KeyCode.F2;
    [SerializeField] private KeyCode toggleHelpKey = KeyCode.F1;
    [SerializeField] private KeyCode playOnceKey = KeyCode.Space;
    [SerializeField] private KeyCode clearKey = KeyCode.Delete;
    [SerializeField] private KeyCode alternateClearKey = KeyCode.C;
    [SerializeField] private KeyCode toggleRepeatKey = KeyCode.R;
    [SerializeField] private KeyCode previousVfxKey = KeyCode.LeftBracket;
    [SerializeField] private KeyCode nextVfxKey = KeyCode.RightBracket;
    [SerializeField] private KeyCode targetPlayerKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode targetMonsterKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode targetMidpointKey = KeyCode.Alpha3;
    [SerializeField] private KeyCode targetWorldPointKey = KeyCode.Alpha4;
    [SerializeField] private KeyCode frameCameraKey = KeyCode.F;
    [SerializeField] private KeyCode respawnUnitsKey = KeyCode.U;
    [SerializeField] private KeyCode cycleRenderModeKey = KeyCode.Tab;
    [SerializeField] private KeyCode cycleFlipTypeKey = KeyCode.G;
    [SerializeField] private bool shortcutHelpVisible = true;

    private readonly List<GameObject> vfxPrefabs = new List<GameObject>();
    private readonly List<string> vfxLabels = new List<string>();
    private readonly List<int> filteredVfxIndexes = new List<int>();
    private readonly List<GameObject> spawnedObjects = new List<GameObject>();

    private Rect windowRect = new Rect(16f, 16f, 460f, 780f);
    private Vector2 windowScroll;
    private Vector2 vfxListScroll;
    private string vfxSearch = string.Empty;
    private int filteredVfxTotalCount;
    private bool filteredVfxDirty = true;
    private int selectedVfxIndex;
    private bool panelVisible = true;
    private float repeatTimer;
    private Transform unitsRoot;
    private Transform spawnedRoot;
    private GameObject playerInstance;
    private GameObject monsterInstance;
    private string[] objectLayerNames;
    private string[] sortingLayerNames;

    private void Awake()
    {
        if (settings == null)
            settings = new TestVfxSpawnSettings();

        if (!setupOnAwake)
            return;

        LoadDefaultAssetsIfNeeded();
        RefreshVfxList();
        EnsureRoots();
        SpawnUnitsIfNeeded();

        if (frameCameraOnAwake)
            FrameCamera();
    }

    private void OnValidate()
    {
        if (settings == null)
            settings = new TestVfxSpawnSettings();
        repeatInterval = Mathf.Max(0.05f, repeatInterval);
        maxVisibleVfxSearchResults = Mathf.Max(0, maxVisibleVfxSearchResults);
        filteredVfxDirty = true;
    }

    private void Update()
    {
        if (!IsTextInputFocused())
            HandleShortcutInput();

        if (!repeat)
            return;

        repeatTimer -= Time.deltaTime;
        if (repeatTimer > 0f)
            return;

        repeatTimer = Mathf.Max(0.05f, repeatInterval);
        PlaySelectedVfx();
    }

    private void OnGUI()
    {
        if (!panelVisible)
        {
            GUILayout.BeginArea(new Rect(16f, 16f, 260f, 44f), GUI.skin.box);
            GUILayout.Label($"Press {togglePanelKey} to show VFX Workbench");
            GUILayout.EndArea();
            return;
        }

        windowRect = GUILayout.Window(GetInstanceID(), windowRect, DrawWindow, "Test_VFX Workbench");
    }

    private void DrawWindow(int id)
    {
        windowScroll = GUILayout.BeginScrollView(windowScroll);

        DrawShortcutHelp();
        DrawSceneControls();
        DrawVfxPicker();
        DrawSpawnControls();
        DrawUnitActionControls();

        GUILayout.EndScrollView();
        GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
    }

    private void DrawSceneControls()
    {
        GUILayout.Label("Scene", EditorLikeHeaderStyle());

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Respawn Units"))
            RespawnUnits();
        if (GUILayout.Button("Frame Camera"))
            FrameCamera();
        GUILayout.EndHorizontal();

        target = (SpawnTarget)DrawEnumToolbar("Target", target);
        followTarget = GUILayout.Toggle(followTarget, "Follow target for Individual RenderTexture mode");

        GUILayout.Label("World Point");
        worldPoint = DrawVector3(worldPoint);
    }

    private void DrawShortcutHelp()
    {
        shortcutHelpVisible = GUILayout.Toggle(
            shortcutHelpVisible,
            $"Shortcuts ({toggleHelpKey})");

        if (!shortcutHelpVisible)
            return;

        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label(
            $"{togglePanelKey}: Panel   {playOnceKey}: Play   {previousVfxKey}/{nextVfxKey}: Prev/Next VFX");
        GUILayout.Label(
            $"{targetPlayerKey}/{targetMonsterKey}/{targetMidpointKey}/{targetWorldPointKey}: Target   {toggleRepeatKey}: Repeat");
        GUILayout.Label(
            $"{cycleRenderModeKey}: Render Mode   {cycleFlipTypeKey}: Flip Type   {clearKey}/{alternateClearKey}: Clear");
        GUILayout.Label(
            $"{frameCameraKey}: Frame Camera   {respawnUnitsKey}: Respawn Units");
        GUILayout.EndVertical();
    }

    private void DrawVfxPicker()
    {
        GUILayout.Space(8f);
        GUILayout.Label("VFX Prefab", EditorLikeHeaderStyle());

        GUILayout.BeginHorizontal();
        string nextSearch = LabeledTextField("Search", vfxSearch);
        if (!string.Equals(nextSearch, vfxSearch, StringComparison.Ordinal))
        {
            vfxSearch = nextSearch;
            filteredVfxDirty = true;
            vfxListScroll = Vector2.zero;
        }

        if (GUILayout.Button("Refresh", GUILayout.Width(78f)))
            RefreshVfxList();
        GUILayout.EndHorizontal();

        if (vfxPrefabs.Count == 0)
        {
            GUILayout.Label("No VFX prefabs found.");
            return;
        }

        selectedVfxIndex = Mathf.Clamp(selectedVfxIndex, 0, vfxPrefabs.Count - 1);
        GUILayout.Label($"Selected: {vfxLabels[selectedVfxIndex]}");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("<", GUILayout.Width(36f)))
            SelectPreviousVfx();
        if (GUILayout.Button(">", GUILayout.Width(36f)))
            SelectNextVfx();
        GUILayout.EndHorizontal();

        EnsureFilteredVfxIndexes();
        if (filteredVfxTotalCount <= 0)
        {
            GUILayout.Label("No matching VFX prefabs.");
            return;
        }

        if (filteredVfxTotalCount > filteredVfxIndexes.Count)
        {
            GUILayout.Label(
                $"Showing {filteredVfxIndexes.Count} / {filteredVfxTotalCount} matches. Refine Search for more.");
        }

        vfxListScroll = GUILayout.BeginScrollView(vfxListScroll, GUILayout.Height(160f));
        for (int resultIndex = 0; resultIndex < filteredVfxIndexes.Count; resultIndex++)
        {
            int i = filteredVfxIndexes[resultIndex];
            if (i < 0 || i >= vfxPrefabs.Count)
                continue;

            GUIStyle style = i == selectedVfxIndex ? SelectedButtonStyle() : GUI.skin.button;
            if (GUILayout.Button(vfxLabels[i], style))
                selectedVfxIndex = i;
        }
        GUILayout.EndScrollView();
    }

    private void DrawSpawnControls()
    {
        GUILayout.Space(8f);
        GUILayout.Label("Spawn Data", EditorLikeHeaderStyle());

        settings.RenderMode = (BattleVfxRenderMode)DrawEnumToolbar("Render Mode", settings.RenderMode);
        settings.FlipType = (VfxFlipType)DrawEnumToolbar("Flip Type", settings.FlipType);

        settings.ObjectLayerName = DrawChoice("Object Layer", settings.ObjectLayerName, GetObjectLayerNames());
        settings.SortingLayerName = DrawChoice("Sorting Layer", settings.SortingLayerName, GetSortingLayerNames());
        settings.SortingOrderOffset = LabeledIntField("Order Offset", settings.SortingOrderOffset);
        settings.SortingWorldYOffset = LabeledFloatField("Sorting Y Offset", settings.SortingWorldYOffset);
        settings.YMultiplier = LabeledFloatField("Y Multiplier", settings.YMultiplier);

        GUILayout.Label("Spawn Offset");
        settings.SpawnPositionOffset = DrawVector3(settings.SpawnPositionOffset);
        GUILayout.Label("Rotation Euler");
        settings.RotationEuler = DrawVector3(settings.RotationEuler);
        GUILayout.Label("Scale Multiplier");
        settings.ScaleMultiplier = DrawVector3(settings.ScaleMultiplier);

        GUILayout.Label("Proxy Offset");
        settings.ProxyWorldOffset = DrawVector3(settings.ProxyWorldOffset);
        settings.ScaleDirectWorldRendererToProxyHeight =
            GUILayout.Toggle(settings.ScaleDirectWorldRendererToProxyHeight, "Scale Direct VFX To Proxy Height");
        settings.ProxyWorldHeight = LabeledFloatField("Proxy Height", settings.ProxyWorldHeight);
        settings.RenderTextureWidth = LabeledIntField("RT Width", settings.RenderTextureWidth);
        settings.RenderTextureHeight = LabeledIntField("RT Height", settings.RenderTextureHeight);
        settings.RenderCameraOrthographicSize =
            LabeledFloatField("RT Camera Size", settings.RenderCameraOrthographicSize);

        settings.LifeTime = LabeledFloatField("Lifetime", settings.LifeTime);
        settings.AutoDestroy = GUILayout.Toggle(settings.AutoDestroy, "Auto Destroy Direct VFX");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Play Once", GUILayout.Height(30f)))
            PlaySelectedVfx();
        if (GUILayout.Button("Clear", GUILayout.Height(30f)))
            ClearSpawnedVfx();
        GUILayout.EndHorizontal();

        repeat = GUILayout.Toggle(repeat, "Repeat");
        repeatInterval = LabeledFloatField("Repeat Interval", repeatInterval);
    }

    private void DrawUnitActionControls()
    {
        GUILayout.Space(8f);
        GUILayout.Label("Unit Action VFX", EditorLikeHeaderStyle());

        DrawUnitButtons("Player", playerInstance);
        DrawUnitButtons("Monster", monsterInstance);

        statusEffectId = LabeledTextField("Status Effect Id", statusEffectId);
    }

    private void DrawUnitButtons(string label, GameObject unit)
    {
        GUILayout.Label(label);
        GUILayout.BeginHorizontal();

        bool enabledBefore = GUI.enabled;
        GUI.enabled = unit != null && unit.GetComponentInChildren<BattleUnitAnimator>() != null;

        if (GUILayout.Button("Move"))
            unit.GetComponentInChildren<BattleUnitAnimator>().PlayMove();
        if (GUILayout.Button("Hit"))
            unit.GetComponentInChildren<BattleUnitAnimator>().PlayHit();
        if (GUILayout.Button("Guard"))
            unit.GetComponentInChildren<BattleUnitAnimator>().PlayGuard();
        if (GUILayout.Button("Atk1"))
            unit.GetComponentInChildren<BattleUnitAnimator>().PlayAttackAction(1);
        if (GUILayout.Button("Atk2"))
            unit.GetComponentInChildren<BattleUnitAnimator>().PlayAttackAction(2);
        if (GUILayout.Button("Atk3"))
            unit.GetComponentInChildren<BattleUnitAnimator>().PlayAttackAction(3);
        if (GUILayout.Button("Status"))
            unit.GetComponentInChildren<BattleUnitAnimator>().PlayStatusVfx(statusEffectId);

        GUI.enabled = enabledBefore;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUI.enabled = unit != null && unit.GetComponentInChildren<BattleUnitFacing>() != null;
        if (GUILayout.Button("Flip Facing"))
            unit.GetComponentInChildren<BattleUnitFacing>().FlipOnce();
        GUI.enabled = enabledBefore;
        GUILayout.EndHorizontal();
    }

    private void PlaySelectedVfx()
    {
        if (vfxPrefabs.Count == 0)
            RefreshVfxList();

        if (vfxPrefabs.Count == 0)
            return;

        selectedVfxIndex = Mathf.Clamp(selectedVfxIndex, 0, vfxPrefabs.Count - 1);
        GameObject prefab = vfxPrefabs[selectedVfxIndex];
        if (prefab == null)
            return;

        EnsureRoots();

        BattleVfxEntry entry = settings.ToEntry(prefab);
        int renderLayer = ResolveLayer(settings.ObjectLayerName);
        if (renderLayer < 0)
            renderLayer = 0;

        if (TrySpawnProxyVfx(entry, renderLayer))
            return;

        SpawnDirectVfx(entry, renderLayer);
    }

    private void HandleShortcutInput()
    {
        if (Input.GetKeyDown(togglePanelKey))
            panelVisible = !panelVisible;

        if (Input.GetKeyDown(toggleHelpKey))
        {
            shortcutHelpVisible = !shortcutHelpVisible;
            panelVisible = true;
        }

        if (Input.GetKeyDown(playOnceKey))
            PlaySelectedVfx();

        if (Input.GetKeyDown(clearKey) || Input.GetKeyDown(alternateClearKey))
            ClearSpawnedVfx();

        if (Input.GetKeyDown(toggleRepeatKey))
            ToggleRepeat();

        if (Input.GetKeyDown(previousVfxKey))
            SelectPreviousVfx();

        if (Input.GetKeyDown(nextVfxKey))
            SelectNextVfx();

        if (Input.GetKeyDown(targetPlayerKey))
            target = SpawnTarget.Player;

        if (Input.GetKeyDown(targetMonsterKey))
            target = SpawnTarget.Monster;

        if (Input.GetKeyDown(targetMidpointKey))
            target = SpawnTarget.Midpoint;

        if (Input.GetKeyDown(targetWorldPointKey))
            target = SpawnTarget.WorldPoint;

        if (Input.GetKeyDown(frameCameraKey))
            FrameCamera();

        if (Input.GetKeyDown(respawnUnitsKey))
            RespawnUnits();

        if (Input.GetKeyDown(cycleRenderModeKey))
            settings.RenderMode = TestVfxWorkbenchUtility.NextEnumValue(settings.RenderMode);

        if (Input.GetKeyDown(cycleFlipTypeKey))
            settings.FlipType = TestVfxWorkbenchUtility.NextEnumValue(settings.FlipType);
    }

    private static bool IsTextInputFocused()
    {
        return GUIUtility.keyboardControl != 0;
    }

    private void ToggleRepeat()
    {
        repeat = !repeat;
        repeatTimer = 0f;
    }

    private void SelectPreviousVfx()
    {
        selectedVfxIndex =
            TestVfxWorkbenchUtility.WrapIndex(selectedVfxIndex - 1, vfxPrefabs.Count);
    }

    private void SelectNextVfx()
    {
        selectedVfxIndex =
            TestVfxWorkbenchUtility.WrapIndex(selectedVfxIndex + 1, vfxPrefabs.Count);
    }

    private bool TrySpawnProxyVfx(BattleVfxEntry entry, int renderLayer)
    {
        if (entry == null || entry.prefab == null)
            return false;

        if (entry.renderMode != BattleVfxRenderMode.IndividualWorldRenderTexture)
            return false;

        Transform targetTransform = ResolveTargetTransform();
        float lifeTime = settings.SafeLifeTime();

        if (followTarget && targetTransform != null)
        {
            BattleVfxEntry followEntry = settings.ToEntry(entry.prefab);
            followEntry.proxyWorldOffset = settings.ProxyWorldOffset + settings.SpawnPositionOffset;

            bool spawned = BattleWorldVfxRenderer.TrySpawn(
                followEntry,
                targetTransform,
                renderLayer,
                lifeTime,
                vfx => ConfigureVfxInstance(vfx, followEntry, renderLayer),
                out BattleWorldVfxHandle handle);

            TrackHandle(handle);
            return spawned;
        }

        Vector3 position = ResolveTargetPosition() + settings.SpawnPositionOffset;
        int visibleLayer = ResolveVisibleLayer(renderLayer);
        bool detached = BattleWorldVfxRenderer.TrySpawnDetached(
            entry,
            position,
            renderLayer,
            visibleLayer,
            lifeTime,
            vfx => ConfigureVfxInstance(vfx, entry, renderLayer),
            out BattleWorldVfxHandle detachedHandle);

        TrackHandle(detachedHandle);
        return detached;
    }

    private void SpawnDirectVfx(BattleVfxEntry entry, int renderLayer)
    {
        if (entry == null || entry.prefab == null)
            return;

        Vector3 position = ResolveTargetPosition() + settings.SpawnPositionOffset;
        GameObject instance = Instantiate(entry.prefab, spawnedRoot);
        instance.name = $"{entry.prefab.name}_Workbench";
        instance.transform.position = position;

        ConfigureVfxInstance(instance, entry, renderLayer);

        TestVfxWorkbenchUtility.ApplyDirectRendererSorting(
            instance,
            entry.proxySortingLayerName,
            position.y + entry.proxySortingWorldYOffset,
            entry.proxyYMultiplier,
            entry.proxySortingOrderOffset);

        spawnedObjects.Add(instance);

        if (settings.AutoDestroy)
            StartCoroutine(DestroyAfter(instance, settings.SafeLifeTime()));
    }

    private void ConfigureVfxInstance(GameObject vfx, BattleVfxEntry entry, int renderLayer)
    {
        if (vfx == null || entry == null)
            return;

        vfx.SetActive(true);
        TestVfxWorkbenchUtility.SetLayerRecursively(vfx, renderLayer);
        TestVfxWorkbenchUtility.ApplyTransformOverrides(vfx, settings);
        TestVfxWorkbenchUtility.ApplyFlip(vfx, entry.flipType);
        TestVfxWorkbenchUtility.RestartParticles(vfx);
        BattleVfxAudioUtility.PlayAndStripEmbeddedAudioSources(vfx, entry.prefab, this);
    }

    private IEnumerator DestroyAfter(GameObject targetObject, float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, delay));

        if (targetObject != null)
        {
            spawnedObjects.Remove(targetObject);
            Destroy(targetObject);
        }
    }

    private void ClearSpawnedVfx()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
            DestroyUnityObject(spawnedObjects[i]);

        spawnedObjects.Clear();
    }

    private void TrackHandle(BattleWorldVfxHandle handle)
    {
        if (handle != null)
            spawnedObjects.Add(handle.gameObject);
    }

    private void RespawnUnits()
    {
        DestroyUnityObject(playerInstance);
        DestroyUnityObject(monsterInstance);
        playerInstance = null;
        monsterInstance = null;
        SpawnUnitsIfNeeded();
    }

    private void SpawnUnitsIfNeeded()
    {
        EnsureRoots();

        if (playerInstance == null)
            playerInstance = SpawnUnit(playerPrefab, "Player_TestUnit", playerPosition, true);

        if (monsterInstance == null)
            monsterInstance = SpawnUnit(monsterPrefab, "Monster_TestUnit", monsterPosition, false);
    }

    private GameObject SpawnUnit(GameObject prefab, string instanceName, Vector3 position, bool faceRight)
    {
        GameObject unit = prefab != null
            ? Instantiate(prefab, unitsRoot)
            : GameObject.CreatePrimitive(PrimitiveType.Capsule);

        unit.name = instanceName;
        unit.transform.position = position;

        BattleUnitFacing facing = unit.GetComponentInChildren<BattleUnitFacing>();
        if (facing != null)
            facing.FaceRight(faceRight);

        return unit;
    }

    private Transform ResolveTargetTransform()
    {
        switch (target)
        {
            case SpawnTarget.Player:
                return playerInstance != null ? playerInstance.transform : null;

            case SpawnTarget.Monster:
                return monsterInstance != null ? monsterInstance.transform : null;

            default:
                return null;
        }
    }

    private Vector3 ResolveTargetPosition()
    {
        switch (target)
        {
            case SpawnTarget.Player:
                return playerInstance != null ? playerInstance.transform.position : playerPosition;

            case SpawnTarget.Monster:
                return monsterInstance != null ? monsterInstance.transform.position : monsterPosition;

            case SpawnTarget.Midpoint:
                return ResolveMidpoint();

            case SpawnTarget.WorldPoint:
                return worldPoint;

            default:
                return Vector3.zero;
        }
    }

    private Vector3 ResolveMidpoint()
    {
        Vector3 player = playerInstance != null ? playerInstance.transform.position : playerPosition;
        Vector3 monster = monsterInstance != null ? monsterInstance.transform.position : monsterPosition;
        return (player + monster) * 0.5f;
    }

    private int ResolveVisibleLayer(int renderLayer)
    {
        Transform targetTransform = ResolveTargetTransform();
        int visibleLayer = targetTransform != null ? targetTransform.gameObject.layer : 0;
        return visibleLayer == renderLayer ? 0 : visibleLayer;
    }

    private int ResolveLayer(string layerName)
    {
        if (string.IsNullOrWhiteSpace(layerName))
            return 0;

        int layer = LayerMask.NameToLayer(layerName.Trim());
        return layer >= 0 ? layer : 0;
    }

    private void EnsureRoots()
    {
        if (unitsRoot == null)
            unitsRoot = EnsureChild("SampleUnits");

        if (spawnedRoot == null)
            spawnedRoot = EnsureChild("SpawnedVfx");
    }

    private Transform EnsureChild(string childName)
    {
        Transform child = transform.Find(childName);
        if (child != null)
            return child;

        GameObject childObject = new GameObject(childName);
        childObject.transform.SetParent(transform, false);
        return childObject.transform;
    }

    private void LoadDefaultAssetsIfNeeded()
    {
#if UNITY_EDITOR
        if (playerPrefab == null && !string.IsNullOrWhiteSpace(playerPrefabPath))
            playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);

        if (monsterPrefab == null && !string.IsNullOrWhiteSpace(monsterPrefabPath))
            monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(monsterPrefabPath);
#endif
    }

    private void RefreshVfxList()
    {
        vfxPrefabs.Clear();
        vfxLabels.Clear();

        if (manualVfxPrefabs != null)
        {
            for (int i = 0; i < manualVfxPrefabs.Length; i++)
                AddVfxPrefab(manualVfxPrefabs[i], manualVfxPrefabs[i] != null ? manualVfxPrefabs[i].name : null);
        }

#if UNITY_EDITOR
        if (editorVfxSearchFolders != null && editorVfxSearchFolders.Length > 0)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", editorVfxSearchFolders);
            Array.Sort(guids, StringComparer.Ordinal);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                AddVfxPrefab(prefab, path);
            }
        }
#endif

        selectedVfxIndex = Mathf.Clamp(selectedVfxIndex, 0, Mathf.Max(0, vfxPrefabs.Count - 1));
        filteredVfxDirty = true;
    }

    private void EnsureFilteredVfxIndexes()
    {
        if (!filteredVfxDirty)
            return;

        filteredVfxTotalCount = TestVfxWorkbenchUtility.RebuildFilteredLabelIndexes(
            vfxLabels,
            vfxSearch,
            filteredVfxIndexes,
            maxVisibleVfxSearchResults);
        filteredVfxDirty = false;
    }

    private void AddVfxPrefab(GameObject prefab, string label)
    {
        if (prefab == null || vfxPrefabs.Contains(prefab))
            return;

        vfxPrefabs.Add(prefab);
        vfxLabels.Add(string.IsNullOrWhiteSpace(label) ? prefab.name : BuildDisplayLabel(label));
    }

    private static string BuildDisplayLabel(string label)
    {
        string normalized = label.Replace('\\', '/');
        const string root = "Assets/Project/Art/VFX/";

        if (normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            return normalized.Substring(root.Length);

        return normalized;
    }

    private void FrameCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
            camera = FindFirstObjectByType<Camera>();

        if (camera == null)
            return;

        camera.orthographic = true;
        camera.orthographicSize = 3.5f;
        camera.transform.position = new Vector3(0f, 0f, -20f);
        camera.transform.rotation = Quaternion.identity;
    }

    private string[] GetObjectLayerNames()
    {
        if (objectLayerNames != null)
            return objectLayerNames;

#if UNITY_EDITOR
        objectLayerNames = InternalEditorUtility.layers;
#else
        objectLayerNames = new[] { "Default", "VFX", "UI" };
#endif
        return objectLayerNames;
    }

    private string[] GetSortingLayerNames()
    {
        if (sortingLayerNames != null)
            return sortingLayerNames;

        SortingLayer[] layers = SortingLayer.layers;
        sortingLayerNames = new string[layers.Length];

        for (int i = 0; i < layers.Length; i++)
            sortingLayerNames[i] = layers[i].name;

        return sortingLayerNames;
    }

    private static Enum DrawEnumToolbar(string label, Enum value)
    {
        GUILayout.Label(label);
        Type enumType = value.GetType();
        string[] names = Enum.GetNames(enumType);
        int current = Array.IndexOf(names, value.ToString());
        int selected = GUILayout.Toolbar(Mathf.Max(0, current), names);
        return (Enum)Enum.Parse(enumType, names[Mathf.Clamp(selected, 0, names.Length - 1)]);
    }

    private static string DrawChoice(string label, string value, string[] choices)
    {
        GUILayout.Label(label);

        if (choices == null || choices.Length == 0)
            return LabeledTextField(label, value);

        int current = Array.IndexOf(choices, value);
        int selected = GUILayout.SelectionGrid(Mathf.Max(0, current), choices, 3);
        return choices[Mathf.Clamp(selected, 0, choices.Length - 1)];
    }

    private static Vector3 DrawVector3(Vector3 value)
    {
        GUILayout.BeginHorizontal();
        value.x = CompactFloatField("X", value.x);
        value.y = CompactFloatField("Y", value.y);
        value.z = CompactFloatField("Z", value.z);
        GUILayout.EndHorizontal();
        return value;
    }

    private static string LabeledTextField(string label, string value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(110f));
        value = GUILayout.TextField(value ?? string.Empty);
        GUILayout.EndHorizontal();
        return value;
    }

    private static int LabeledIntField(string label, int value)
    {
        string text = LabeledTextField(label, value.ToString());
        int parsed;
        return int.TryParse(text, out parsed) ? parsed : value;
    }

    private static float LabeledFloatField(string label, float value)
    {
        string text = LabeledTextField(label, value.ToString("0.###"));
        float parsed;
        return float.TryParse(text, out parsed) ? parsed : value;
    }

    private static float CompactFloatField(string label, float value)
    {
        GUILayout.Label(label, GUILayout.Width(14f));
        string text = GUILayout.TextField(value.ToString("0.###"), GUILayout.Width(78f));
        float parsed;
        return float.TryParse(text, out parsed) ? parsed : value;
    }

    private static GUIStyle EditorLikeHeaderStyle()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold
        };
        return style;
    }

    private static GUIStyle SelectedButtonStyle()
    {
        GUIStyle style = new GUIStyle(GUI.skin.button)
        {
            fontStyle = FontStyle.Bold
        };
        return style;
    }

    private static void DestroyUnityObject(UnityEngine.Object targetObject)
    {
        if (targetObject == null)
            return;

        if (Application.isPlaying)
            Destroy(targetObject);
        else
            DestroyImmediate(targetObject);
    }
}
