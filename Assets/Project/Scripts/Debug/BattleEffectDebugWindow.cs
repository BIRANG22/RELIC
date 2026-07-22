using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class BattleEffectDebugWindow : MonoBehaviour
{
    [SerializeField] private KeyCode toggleKey = KeyCode.F9;
    [SerializeField] private bool visible = true;
    [SerializeField] private int selectedPartyIndex;
    [SerializeField] private int selectedPresetIndex;
    [SerializeField] private int gridIndex = 12;
    [SerializeField] private string customRelicIds = "";
    [SerializeField] private string customRuneIds = "";
    [SerializeField] private string customGridEffectId = "GR_Poisson";

    private Vector2 scrollPosition;
    private Rect windowRect = new(16f, 16f, 430f, 720f);
    private GUIStyle smallLabelStyle;

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            visible = !visible;
    }

    private void OnGUI()
    {
        if (!visible)
            return;

        EnsureStyles();
        windowRect = GUILayout.Window(
            GetInstanceID(),
            windowRect,
            DrawWindow,
            "Battle Effect Debug");
    }

    private void DrawWindow(int id)
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(660f));

        DrawSelectedCharacter();
        GUILayout.Space(8f);
        DrawPresets();
        GUILayout.Space(8f);
        DrawRuntimeControls();
        GUILayout.Space(8f);
        DrawStatusControls();
        GUILayout.Space(8f);
        DrawGridEffectControls();
        GUILayout.Space(8f);
        DrawBattleControls();

        GUILayout.EndScrollView();
        GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
    }

    private void DrawSelectedCharacter()
    {
        GUILayout.Label("Character", EditorLikeHeaderStyle());

        int maxPartyCount = DataManager.Instance != null && DataManager.Instance.PartyRuntimeStore != null
            ? DataManager.Instance.PartyRuntimeStore.MaxPartyCountValue
            : 3;

        GUILayout.BeginHorizontal();
        for (int i = 0; i < maxPartyCount; i++)
        {
            CharacterRuntimeData runtime = BattleEffectDebugTool.GetPartyRuntime(i);
            string label = runtime != null ? runtime.CharacterId : $"Slot {i + 1}";

            if (GUILayout.Toggle(selectedPartyIndex == i, label, "Button"))
                selectedPartyIndex = i;
        }
        GUILayout.EndHorizontal();

        CharacterRuntimeData selected = GetSelectedRuntime();

        if (selected == null)
        {
            GUILayout.Label("선택된 캐릭터 런타임이 없습니다.", smallLabelStyle);
            return;
        }

        GUILayout.Label(
            $"HP {selected.CurrentHP}/{selected.MaxHP}  Cost {selected.CurrentCost}/{selected.MaxCost}  Resource {selected.CurrentResource}/{BattleEffectDebugTool.GetMaxResource(selected)}",
            smallLabelStyle);

        GUILayout.Label(
            $"Relics: {JoinIds(selected.EquippedRelicIds)}",
            smallLabelStyle);
    }

    private void DrawPresets()
    {
        GUILayout.Label("Relic Presets", EditorLikeHeaderStyle());

        IReadOnlyList<BattleEffectDebugPreset> presets = BattleEffectDebugTool.GetDefaultPresets();

        if (presets == null || presets.Count == 0)
        {
            GUILayout.Label("프리셋이 없습니다.", smallLabelStyle);
            return;
        }

        selectedPresetIndex = Mathf.Clamp(selectedPresetIndex, 0, presets.Count - 1);

        string[] labels = new string[presets.Count];
        for (int i = 0; i < presets.Count; i++)
            labels[i] = presets[i].Label;

        selectedPresetIndex = GUILayout.SelectionGrid(selectedPresetIndex, labels, 1);

        if (GUILayout.Button("Apply Selected Preset"))
        {
            BattleEffectDebugTool.ApplyPreset(GetSelectedRuntime(), presets[selectedPresetIndex]);
            BattleEffectDebugTool.RefreshBattle();
            Debug.Log($"[BattleEffectDebug] ApplyPreset:{presets[selectedPresetIndex].Key}");
        }

        GUILayout.Label("Custom Relics (; separated)", smallLabelStyle);
        customRelicIds = GUILayout.TextField(customRelicIds);

        GUILayout.Label("Custom Runes (; separated)", smallLabelStyle);
        customRuneIds = GUILayout.TextField(customRuneIds);

        if (GUILayout.Button("Apply Custom Loadout"))
        {
            CharacterRuntimeData runtime = GetSelectedRuntime();

            if (runtime != null)
            {
                BattleEffectDebugTool.EquipOnlyRelics(runtime, SplitIds(customRelicIds));
                BattleEffectDebugTool.EquipOnlyRunes(runtime, SplitIds(customRuneIds));
                BattleEffectDebugTool.RefreshBattle();
                Debug.Log("[BattleEffectDebug] ApplyCustomLoadout");
            }
        }
    }

    private void DrawRuntimeControls()
    {
        GUILayout.Label("Runtime", EditorLikeHeaderStyle());

        CharacterRuntimeData runtime = GetSelectedRuntime();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("HP 30%"))
            ApplyHp(runtime, 0.3f);
        if (GUILayout.Button("HP 90%"))
            ApplyHp(runtime, 0.9f);
        if (GUILayout.Button("HP Max"))
            ApplyHp(runtime, 1f);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Cost 0"))
            ApplyCost(runtime, 0);
        if (GUILayout.Button("Cost Max"))
            ApplyCost(runtime, runtime != null ? runtime.MaxCost : 0);
        if (GUILayout.Button("Resource Max"))
            ApplyResource(runtime, BattleEffectDebugTool.GetMaxResource(runtime));
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Cost/Resource Max"))
        {
            BattleEffectDebugTool.SetFullResources(runtime);
            BattleEffectDebugTool.RefreshBattle();
        }
    }

    private void DrawStatusControls()
    {
        GUILayout.Label("Status", EditorLikeHeaderStyle());

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Enemies Poison"))
            AddEnemyStatus("E_Poison");
        if (GUILayout.Button("Enemies Bleed"))
            AddEnemyStatus("E_Bleed");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Enemies Vulnerable"))
            AddEnemyStatus("E_Vulnerable");
        if (GUILayout.Button("Enemies Weaken"))
            AddEnemyStatus("E_Weaken");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Player Charge"))
            AddPlayerStatus("E_Charge");
        if (GUILayout.Button("Player Focus"))
            AddPlayerStatus("E_Focus");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Player Swift"))
            AddPlayerStatus("E_Swift");
        if (GUILayout.Button("Player Boost"))
            AddPlayerStatus("E_Boost");
        GUILayout.EndHorizontal();
    }

    private void DrawGridEffectControls()
    {
        GUILayout.Label("Grid Effect", EditorLikeHeaderStyle());
        GUILayout.BeginHorizontal();
        GUILayout.Label("Grid", GUILayout.Width(38f));
        string gridText = GUILayout.TextField(gridIndex.ToString(), GUILayout.Width(56f));
        if (int.TryParse(gridText, out int parsedGridIndex))
            gridIndex = Mathf.Clamp(parsedGridIndex, 0, 34);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Poison"))
            PlaceGrid("GR_Poisson");
        if (GUILayout.Button("Thorn"))
            PlaceGrid("GR_thorn");
        if (GUILayout.Button("Obstacle"))
            PlaceGrid("GR_debris");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Dummy"))
            PlaceGrid("GR_dummy");
        if (GUILayout.Button("Explosive Doll"))
            PlaceGrid("GR_explosive_doll");
        if (GUILayout.Button("Remove"))
            RemoveGrid();
        GUILayout.EndHorizontal();

        GUILayout.Label("Custom GridEffectID", smallLabelStyle);
        customGridEffectId = GUILayout.TextField(customGridEffectId);
        if (GUILayout.Button("Place Custom GridEffect"))
            PlaceGrid(customGridEffectId);
    }

    private void DrawBattleControls()
    {
        GUILayout.Label("Battle", EditorLikeHeaderStyle());

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Reload Battle"))
            BattleEffectDebugTool.ReloadBattleRoom();

        if (GUILayout.Button("Kill All Monsters"))
        {
            BattleDebugKillAllMonsters killer = FindFirstObjectByType<BattleDebugKillAllMonsters>(
                FindObjectsInactive.Include);

            if (killer != null)
                killer.KillAllMonstersForDebug();
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Print Runtime"))
            PrintRuntimeSummary();

        GUILayout.Label("F9: Show/Hide", smallLabelStyle);
    }

    private CharacterRuntimeData GetSelectedRuntime()
    {
        return BattleEffectDebugTool.GetPartyRuntime(selectedPartyIndex);
    }

    private void ApplyHp(CharacterRuntimeData runtime, float percent)
    {
        BattleEffectDebugTool.SetHpPercent(runtime, percent);
        BattleEffectDebugTool.RefreshBattle();
    }

    private void ApplyCost(CharacterRuntimeData runtime, int cost)
    {
        BattleEffectDebugTool.SetCurrentCost(runtime, cost);
        BattleEffectDebugTool.RefreshBattle();
    }

    private void ApplyResource(CharacterRuntimeData runtime, int resource)
    {
        BattleEffectDebugTool.SetCurrentResource(
            runtime,
            resource,
            BattleEffectDebugTool.GetMaxResource(runtime));
        BattleEffectDebugTool.RefreshBattle();
    }

    private void AddEnemyStatus(string effectId)
    {
        BattleEffectDebugTool.AddStatusToAllMonsters(effectId, 1, 1);
        BattleEffectDebugTool.RefreshBattle();
    }

    private void AddPlayerStatus(string effectId)
    {
        BattleEffectDebugTool.AddStatusToPlayer(GetSelectedRuntime(), effectId, 1, 1);
        BattleEffectDebugTool.RefreshBattle();
    }

    private void PlaceGrid(string gridEffectId)
    {
        if (BattleEffectDebugTool.TryPlaceGridEffect(gridIndex, gridEffectId))
            Debug.Log($"[BattleEffectDebug] PlaceGridEffect:{gridEffectId} Grid:{gridIndex}");
        else
            Debug.LogWarning($"[BattleEffectDebug] Failed PlaceGridEffect:{gridEffectId} Grid:{gridIndex}");
    }

    private void RemoveGrid()
    {
        if (BattleEffectDebugTool.TryRemoveGridEffect(gridIndex))
            Debug.Log($"[BattleEffectDebug] RemoveGridEffect Grid:{gridIndex}");
        else
            Debug.LogWarning($"[BattleEffectDebug] Failed RemoveGridEffect Grid:{gridIndex}");
    }

    private void PrintRuntimeSummary()
    {
        List<CharacterRuntimeData> runtimes = BattleEffectDebugTool.GetPartyRuntimes();

        for (int i = 0; i < runtimes.Count; i++)
        {
            CharacterRuntimeData runtime = runtimes[i];

            Debug.Log(
                $"[BattleEffectDebug] {runtime.CharacterId} " +
                $"HP:{runtime.CurrentHP}/{runtime.MaxHP} " +
                $"Cost:{runtime.CurrentCost}/{runtime.MaxCost} " +
                $"Resource:{runtime.CurrentResource}/{BattleEffectDebugTool.GetMaxResource(runtime)} " +
                $"Relics:{JoinIds(runtime.EquippedRelicIds)} " +
                $"Runes:{JoinIds(runtime.EquippedRuneIds)}");
        }
    }

    private void EnsureStyles()
    {
        if (smallLabelStyle != null)
            return;

        smallLabelStyle = new GUIStyle(GUI.skin.label)
        {
            wordWrap = true,
            fontSize = 11
        };
    }

    private GUIStyle EditorLikeHeaderStyle()
    {
        GUIStyle style = GUI.skin.box;
        style.alignment = TextAnchor.MiddleLeft;
        return style;
    }

    private static string JoinIds(IReadOnlyList<string> ids)
    {
        if (ids == null)
            return "";

        List<string> filled = new();

        for (int i = 0; i < ids.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(ids[i]))
                filled.Add(ids[i].Trim());
        }

        return filled.Count > 0 ? string.Join(", ", filled) : "None";
    }

    private static string[] SplitIds(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return System.Array.Empty<string>();

        string[] parts = raw.Split(';');
        List<string> ids = new();

        for (int i = 0; i < parts.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(parts[i]))
                ids.Add(parts[i].Trim());
        }

        return ids.ToArray();
    }
}
