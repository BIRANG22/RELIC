using System;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;

public sealed class BattleEffectDebugWindow : MonoBehaviour
{
    [SerializeField] private KeyCode toggleKey = KeyCode.F9;
    [SerializeField] private bool visible = true;
    [SerializeField] private int selectedPartyIndex;
    [SerializeField] private string idSearch = "";
    [SerializeField] private string selectedCharacterId = "";
    [SerializeField] private string selectedSkillId = "";
    [SerializeField] private string selectedRelicId = "";
    [SerializeField] private string selectedRuneId = "";
    [SerializeField] private string selectedCompoundId = "";
    [SerializeField] private string selectedGridEffectId = "";
    [SerializeField] private string selectedMonsterId = "";
    [SerializeField] private int monsterSpawnGridIndex = 27;
    [SerializeField] private string selectedMonsterRuntimeId = "";
    [SerializeField] private string selectedMonsterSkillId = "";
    [SerializeField] private int monsterSkillSlotIndex;
    [SerializeField] private int characterGridIndex = 12;
    [SerializeField, Range(MinUiScale, MaxUiScale)] private float uiScale = 1.35f;

    private const float MinWindowWidth = 560f;
    private const float MinWindowHeight = 460f;
    private const float MinUiScale = 1f;
    private const float MaxUiScale = 2f;
    private const float BaseControlHeight = 24f;
    private const int MaxDropdownRows = 12;
    public const float ResizeHandleSize = 44f;

    private static readonly string[] SkillSlotNames =
    {
        "전용",
        "스킬 1",
        "스킬 2",
        "고유",
        "본능"
    };

    private static readonly int[] StatDeltas = { -10, -1, 1, 10 };

    private Vector2 scrollPosition;
    private Vector2 skillOptionScroll;
    private Vector2 relicOptionScroll;
    private Vector2 runeOptionScroll;
    private Vector2 compoundOptionScroll;
    private Rect windowRect = new(16f, 16f, 700f, 820f);
    private GUIStyle smallLabelStyle;
    private GUIStyle headerStyle;
    private GUIStyle buttonStyle;
    private GUIStyle toggleStyle;
    private GUIStyle textFieldStyle;
    private GUIStyle handleStyle;
    private bool characterDropdownOpen;
    private bool skillDropdownOpen;
    private bool relicDropdownOpen;
    private bool runeDropdownOpen;
    private bool compoundDropdownOpen;
    private bool gridEffectDropdownOpen;
    private bool monsterMasterDropdownOpen;
    private bool monsterDropdownOpen;
    private bool monsterSkillDropdownOpen;
    private bool isResizing;

    private struct DebugOption
    {
        public DebugOption(string id, string name)
        {
            Id = string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
            Name = string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
            Label = string.IsNullOrWhiteSpace(Name) ? Id : $"{Id} | {Name}";
        }

        public string Id { get; }
        public string Name { get; }
        public string Label { get; }
    }

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
        uiScale = ClampUiScale(uiScale);
        windowRect = GUILayout.Window(
            GetInstanceID(),
            windowRect,
            DrawWindow,
            "Battle Effect Debug");
        windowRect = ClampWindowRect(windowRect, new Vector2(MinWindowWidth, MinWindowHeight));
    }

    private void DrawWindow(int id)
    {
        float scrollHeight = Mathf.Max(120f, windowRect.height - 58f);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(scrollHeight));

        DrawSearchAndViewControls();
        DrawCharacterSection();
        DrawMonsterSpawnSection();
        DrawMonsterSkillSection();
        DrawSkillSection();
        DrawRelicSection();
        DrawRuneSection();
        DrawCompoundSection();
        DrawGridEffectSection();
        DrawStatusControls();
        DrawStatControls();
        DrawBattleControls();

        GUILayout.EndScrollView();
        DrawResizeHandle();
        GUI.DragWindow(new Rect(0f, 0f, Mathf.Max(0f, windowRect.width - ResizeHandleSize), 24f));
    }

    private void DrawSearchAndViewControls()
    {
        GUILayout.Label("ID 검색", EditorLikeHeaderStyle());
        idSearch = GUILayout.TextField(idSearch ?? string.Empty, textFieldStyle, ControlHeightOption());

        GUILayout.BeginHorizontal();
        GUILayout.Label($"UI Scale {uiScale:0.00}", smallLabelStyle, GUILayout.Width(120f * uiScale));
        uiScale = ClampUiScale(GUILayout.HorizontalSlider(uiScale, MinUiScale, MaxUiScale));
        GUILayout.EndHorizontal();
        GUILayout.Space(SectionSpacing());
    }

    private void DrawCharacterSection()
    {
        GUILayout.Label("캐릭터", EditorLikeHeaderStyle());
        DrawPartySelector();

        List<DebugOption> options = BuildCharacterOptions();
        selectedCharacterId = DrawOptionDropdown(
            "캐릭터 선택",
            options,
            selectedCharacterId,
            ref characterDropdownOpen);

        DrawIntStepper("배치 그리드", ref characterGridIndex, 0, BattleEffectDebugTool.BattleGridCellCount - 1);

        if (GUILayout.Button($"아군 {selectedPartyIndex + 1}번 슬롯 배치/교체", buttonStyle, ControlHeightOption()))
        {
            bool applied = BattleEffectDebugTool.TryApplyPartyCharacter(selectedPartyIndex, selectedCharacterId, characterGridIndex);
            if (applied)
            {
                Debug.Log($"[BattleEffectDebug] PartySlot:{selectedPartyIndex + 1} Character:{selectedCharacterId} Grid:{characterGridIndex}");
            }
            else
            {
                Debug.LogWarning($"[BattleEffectDebug] Failed Character:{selectedCharacterId}");
            }
        }

        DrawSelectedCharacterSummary();
        GUILayout.Space(SectionSpacing());
    }

    private void DrawMonsterSpawnSection()
    {
        GUILayout.Label("몬스터 소환", EditorLikeHeaderStyle());

        List<MonsterMasterData> monsterMasters = BattleEffectDebugTool.GetMonsterMasters();
        List<DebugOption> monsterOptions = new();

        for (int i = 0; i < monsterMasters.Count; i++)
        {
            MonsterMasterData monster = monsterMasters[i];
            if (monster != null)
                AddOptionIfMatches(monsterOptions, monster.MonsterId, monster.Name);
        }
        SortOptions(monsterOptions);

        selectedMonsterId = DrawOptionDropdown(
            "소환 몬스터 선택",
            monsterOptions,
            selectedMonsterId,
            ref monsterMasterDropdownOpen);

        DrawIntStepper(
            "소환 그리드",
            ref monsterSpawnGridIndex,
            0,
            BattleEffectDebugTool.BattleGridCellCount - 1);

        if (GUILayout.Button("선택 몬스터 소환", buttonStyle, ControlHeightOption()))
        {
            if (BattleEffectDebugTool.TrySpawnMonster(
                    selectedMonsterId,
                    monsterSpawnGridIndex,
                    out string runtimeId))
            {
                selectedMonsterRuntimeId = runtimeId;
                selectedMonsterSkillId = string.Empty;
                Debug.Log(
                    $"[BattleEffectDebug] Spawn Monster:{selectedMonsterId} " +
                    $"Runtime:{runtimeId} Grid:{monsterSpawnGridIndex}");
            }
            else
            {
                Debug.LogWarning(
                    $"[BattleEffectDebug] Failed Spawn Monster:{selectedMonsterId} " +
                    $"Grid:{monsterSpawnGridIndex}");
            }
        }

        GUILayout.Label(
            "빈 그리드에 원하는 몬스터를 즉시 소환합니다. 소환 후 아래 몬스터 선택에 자동 지정됩니다.",
            smallLabelStyle);
        GUILayout.Space(SectionSpacing());
    }

    private void DrawMonsterSkillSection()
    {
        GUILayout.Label("몬스터 스킬 강제 등록", EditorLikeHeaderStyle());

        List<BattleDebugMonsterEntry> monsters = BattleEffectDebugTool.GetLiveMonsters();
        List<DebugOption> monsterOptions = new();
        for (int i = 0; i < monsters.Count; i++)
        {
            BattleDebugMonsterEntry monster = monsters[i];
            monsterOptions.Add(new DebugOption(
                monster.RuntimeId,
                $"{monster.Name} ({monster.MonsterId}) Grid {monster.GridIndex}"));
        }

        selectedMonsterRuntimeId = DrawOptionDropdown(
            "몬스터 선택",
            monsterOptions,
            selectedMonsterRuntimeId,
            ref monsterDropdownOpen);

        List<MonsterSkillData> monsterSkills = BattleEffectDebugTool.GetMonsterSkills(selectedMonsterRuntimeId);
        List<DebugOption> skillOptions = new();
        for (int i = 0; i < monsterSkills.Count; i++)
        {
            MonsterSkillData skill = monsterSkills[i];
            if (skill != null)
                skillOptions.Add(new DebugOption(skill.SkillId, skill.Name));
        }

        selectedMonsterSkillId = DrawOptionDropdown(
            "몬스터 스킬 선택",
            skillOptions,
            selectedMonsterSkillId,
            ref monsterSkillDropdownOpen);

        DrawIntStepper("등록 슬롯 (0=1번)", ref monsterSkillSlotIndex, 0, 4);

        if (GUILayout.Button("선택 몬스터 스킬 등록", buttonStyle, ControlHeightOption()))
        {
            if (BattleEffectDebugTool.TryQueueMonsterSkill(
                    selectedMonsterRuntimeId,
                    selectedMonsterSkillId,
                    monsterSkillSlotIndex))
            {
                Debug.Log(
                    $"[BattleEffectDebug] Monster:{selectedMonsterRuntimeId} " +
                    $"Skill:{selectedMonsterSkillId} Slot:{monsterSkillSlotIndex + 1}");
            }
            else
            {
                Debug.LogWarning(
                    $"[BattleEffectDebug] Failed MonsterSkill Monster:{selectedMonsterRuntimeId} " +
                    $"Skill:{selectedMonsterSkillId}");
            }
        }

        GUILayout.Label(
            "공격/버프/디버프 스킬은 실제 타임라인에 등록됩니다. 대상은 현재 몬스터 방향과 실제 스킬 범위로 계산됩니다.",
            smallLabelStyle);
        GUILayout.Space(SectionSpacing());
    }

    private void DrawSkillSection()
    {
        GUILayout.Label("스킬", EditorLikeHeaderStyle());

        List<DebugOption> options = BuildSkillOptions();
        selectedSkillId = DrawScrollableOptionDropdown(
            "스킬 선택",
            options,
            selectedSkillId,
            ref skillDropdownOpen,
            ref skillOptionScroll);
        DrawClearSelectionButton(ref selectedSkillId, "스킬 선택 비우기");

        CharacterRuntimeData runtime = GetSelectedRuntime();
        DrawSlotGrid(
            BattleEffectDebugTool.SkillDisplaySlotCount,
            2,
            index =>
            {
                string current = BattleEffectDebugTool.GetSkillDisplaySlotId(runtime, index);
                return $"{SkillSlotNames[index]}\n{ShortId(current)}";
            },
            index =>
            {
                if (!BattleEffectDebugTool.SetSkillDisplaySlot(runtime, index, selectedSkillId))
                    return;

                BattleEffectDebugTool.RefreshBattle();
                Debug.Log($"[BattleEffectDebug] SkillSlot:{index} Skill:{selectedSkillId}");
            });

        GUILayout.Space(SectionSpacing());
    }

    private void DrawRelicSection()
    {
        GUILayout.Label("유물", EditorLikeHeaderStyle());

        List<DebugOption> options = BuildRelicOptions();
        selectedRelicId = DrawScrollableOptionDropdown(
            "유물 선택",
            options,
            selectedRelicId,
            ref relicDropdownOpen,
            ref relicOptionScroll);
        DrawClearSelectionButton(ref selectedRelicId, "유물 선택 비우기");

        CharacterRuntimeData runtime = GetSelectedRuntime();
        DrawSlotGrid(
            BattleEffectDebugTool.PassiveRelicSlotCount,
            3,
            index =>
            {
                string current = BattleEffectDebugTool.GetPassiveRelicSlotId(runtime, index);
                return $"유물 {index + 1}\n{ShortId(current)}";
            },
            index =>
            {
                if (!BattleEffectDebugTool.SetPassiveRelicSlot(runtime, index, selectedRelicId))
                    return;

                BattleEffectDebugTool.RefreshBattle();
                Debug.Log($"[BattleEffectDebug] RelicSlot:{index + 1} Relic:{selectedRelicId}");
            });

        GUILayout.Space(SectionSpacing());
    }

    private void DrawRuneSection()
    {
        GUILayout.Label("룬", EditorLikeHeaderStyle());

        List<DebugOption> options = BuildRuneOptions();
        selectedRuneId = DrawScrollableOptionDropdown(
            "룬 선택",
            options,
            selectedRuneId,
            ref runeDropdownOpen,
            ref runeOptionScroll);
        DrawClearSelectionButton(ref selectedRuneId, "룬 선택 비우기");

        CharacterRuntimeData runtime = GetSelectedRuntime();
        DrawSlotGrid(
            BattleEffectDebugTool.RuneSlotCount,
            3,
            index =>
            {
                string current = BattleEffectDebugTool.GetRuneSlotId(runtime, index);
                string hud = index < 6 ? "HUD" : "런타임";
                return $"룬 {index + 1} {hud}\n{ShortId(current)}";
            },
            index =>
            {
                if (!BattleEffectDebugTool.SetRuneSlot(runtime, index, selectedRuneId))
                    return;

                BattleEffectDebugTool.RefreshBattle();
                Debug.Log($"[BattleEffectDebug] RuneSlot:{index + 1} Rune:{selectedRuneId}");
            });

        GUILayout.Space(SectionSpacing());
    }

    private void DrawCompoundSection()
    {
        GUILayout.Label("Compound", EditorLikeHeaderStyle());

        List<DebugOption> options = BuildCompoundOptions();
        selectedCompoundId = DrawScrollableOptionDropdown(
            "Compound 선택",
            options,
            selectedCompoundId,
            ref compoundDropdownOpen,
            ref compoundOptionScroll);
        DrawClearSelectionButton(ref selectedCompoundId, "Compound 선택 비우기");

        CharacterRuntimeData runtime = GetSelectedRuntime();
        string current = BattleEffectDebugTool.GetCompoundSlotId(runtime);
        if (GUILayout.Button($"Compound 슬롯\n{ShortId(current)}", buttonStyle, SlotHeightOption()))
        {
            if (BattleEffectDebugTool.SetCompoundSlot(runtime, selectedCompoundId))
            {
                BattleEffectDebugTool.RefreshBattle();
                Debug.Log($"[BattleEffectDebug] Compound:{selectedCompoundId}");
            }
        }

        GUILayout.Space(SectionSpacing());
    }

    private void DrawGridEffectSection()
    {
        GUILayout.Label("그리드효과", EditorLikeHeaderStyle());

        List<DebugOption> options = BuildGridEffectOptions();
        selectedGridEffectId = DrawOptionDropdown(
            "그리드효과 선택",
            options,
            selectedGridEffectId,
            ref gridEffectDropdownOpen);
        DrawClearSelectionButton(ref selectedGridEffectId, "그리드 제거 선택");

        DrawSlotGrid(
            BattleEffectDebugTool.BattleGridCellCount,
            5,
            index => index.ToString("00"),
            index =>
            {
                if (string.IsNullOrWhiteSpace(selectedGridEffectId))
                    RemoveGrid(index);
                else
                    PlaceGrid(index, selectedGridEffectId);
            },
            ControlHeightOption());

        GUILayout.Space(SectionSpacing());
    }

    private void DrawStatusControls()
    {
        GUILayout.Label("상태이상 테스트", EditorLikeHeaderStyle());

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("적 중독", buttonStyle, ControlHeightOption()))
            AddEnemyStatus("E_Poison");
        if (GUILayout.Button("적 출혈", buttonStyle, ControlHeightOption()))
            AddEnemyStatus("E_Bleed");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("적 취약", buttonStyle, ControlHeightOption()))
            AddEnemyStatus("E_Vulnerable");
        if (GUILayout.Button("적 약화", buttonStyle, ControlHeightOption()))
            AddEnemyStatus("E_Weaken");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("플레이어 충전", buttonStyle, ControlHeightOption()))
            AddPlayerStatus("E_Charge");
        if (GUILayout.Button("플레이어 집중", buttonStyle, ControlHeightOption()))
            AddPlayerStatus("E_Focus");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("플레이어 신속", buttonStyle, ControlHeightOption()))
            AddPlayerStatus("E_Swift");
        if (GUILayout.Button("플레이어 증폭", buttonStyle, ControlHeightOption()))
            AddPlayerStatus("E_Boost");
        GUILayout.EndHorizontal();

        GUILayout.Space(SectionSpacing());
    }

    private void DrawStatControls()
    {
        GUILayout.Label("스탯 테스트", EditorLikeHeaderStyle());

        CharacterRuntimeData runtime = GetSelectedRuntime();

        if (runtime == null)
        {
            GUILayout.Label("선택된 캐릭터 런타임이 없습니다.", smallLabelStyle);
            GUILayout.Space(SectionSpacing());
            return;
        }

        DrawStatStepper(
            "HP",
            $"{runtime.CurrentHP}/{runtime.MaxHP}",
            delta => BattleEffectDebugTool.AdjustCurrentHP(runtime, delta));
        DrawStatStepper(
            "스테미나",
            $"{runtime.CurrentCost}/{runtime.MaxCost}",
            delta => BattleEffectDebugTool.AdjustCurrentCost(runtime, delta));
        DrawStatStepper(
            "아머",
            runtime.CurrentShield.ToString(),
            delta => BattleEffectDebugTool.AdjustCurrentShield(runtime, delta));
        DrawStatStepper(
            "회복량",
            runtime.CostRecovery.ToString(),
            delta => BattleEffectDebugTool.AdjustCostRecovery(runtime, delta));

        GUILayout.Space(SectionSpacing());
    }

    private void DrawBattleControls()
    {
        GUILayout.Label("배틀", EditorLikeHeaderStyle());

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("배틀 리로드", buttonStyle, ControlHeightOption()))
            BattleEffectDebugTool.ReloadBattleRoom();

        if (GUILayout.Button("모든 몬스터 처치", buttonStyle, ControlHeightOption()))
        {
            BattleDebugKillAllMonsters killer = FindFirstObjectByType<BattleDebugKillAllMonsters>(
                FindObjectsInactive.Include);

            if (killer != null)
                killer.KillAllMonstersForDebug();
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("런타임 출력", buttonStyle, ControlHeightOption()))
            PrintRuntimeSummary();

        GUILayout.Label("F9: Show/Hide", smallLabelStyle);
    }

    public static Rect ClampWindowRect(Rect rect, Vector2 minSize)
    {
        rect.width = Mathf.Max(rect.width, minSize.x);
        rect.height = Mathf.Max(rect.height, minSize.y);
        return rect;
    }

    public static float ClampUiScale(float value)
    {
        return Mathf.Clamp(value, MinUiScale, MaxUiScale);
    }

    public static float GetScaledControlHeight(float scale)
    {
        return BaseControlHeight * ClampUiScale(scale);
    }

    private void DrawPartySelector()
    {
        int maxPartyCount = DataManager.Instance != null && DataManager.Instance.PartyRuntimeStore != null
            ? DataManager.Instance.PartyRuntimeStore.MaxPartyCountValue
            : 3;

        GUILayout.BeginHorizontal();
        for (int i = 0; i < maxPartyCount; i++)
        {
            CharacterRuntimeData runtime = BattleEffectDebugTool.GetPartyRuntime(i);
            string label = runtime != null ? runtime.CharacterId : $"Slot {i + 1}";

            if (GUILayout.Toggle(selectedPartyIndex == i, label, buttonStyle, ControlHeightOption()))
                selectedPartyIndex = i;
        }
        GUILayout.EndHorizontal();
    }

    private void DrawSelectedCharacterSummary()
    {
        CharacterRuntimeData selected = GetSelectedRuntime();

        if (selected == null)
        {
            GUILayout.Label("선택된 캐릭터 런타임이 없습니다.", smallLabelStyle);
            return;
        }

        GUILayout.Label(
            $"HP {selected.CurrentHP}/{selected.MaxHP}  Cost {selected.CurrentCost}/{selected.MaxCost}  Armor {selected.CurrentShield}  Recovery {selected.CostRecovery}",
            smallLabelStyle);
        GUILayout.Label($"Skills: {JoinDisplaySkills(selected)}", smallLabelStyle);
        GUILayout.Label($"Relics: {JoinPassiveRelics(selected)}", smallLabelStyle);
        GUILayout.Label($"Runes: {JoinIds(selected.EquippedRuneIds)}", smallLabelStyle);
        GUILayout.Label($"Compound: {ShortId(BattleEffectDebugTool.GetCompoundSlotId(selected))}", smallLabelStyle);
    }

    private string DrawOptionDropdown(
        string title,
        IReadOnlyList<DebugOption> options,
        string selectedId,
        ref bool isOpen)
    {
        GUILayout.Label($"{title} ({options.Count})", smallLabelStyle);

        string buttonLabel = ResolveSelectedLabel(options, selectedId);
        if (GUILayout.Button(buttonLabel, buttonStyle, ControlHeightOption()))
            isOpen = !isOpen;

        if (!isOpen)
            return selectedId;

        if (options.Count == 0)
        {
            GUILayout.Label("검색 결과가 없습니다.", smallLabelStyle);
            return selectedId;
        }

        int drawCount = Mathf.Min(options.Count, MaxDropdownRows);
        for (int i = 0; i < drawCount; i++)
        {
            DebugOption option = options[i];
            if (!GUILayout.Button(option.Label, buttonStyle, ControlHeightOption()))
                continue;

            selectedId = option.Id;
            isOpen = false;
        }

        if (options.Count > drawCount)
            GUILayout.Label($"+ {options.Count - drawCount} more. ID 검색으로 좁혀주세요.", smallLabelStyle);

        return selectedId;
    }

    private string DrawScrollableOptionDropdown(
        string title,
        IReadOnlyList<DebugOption> options,
        string selectedId,
        ref bool isOpen,
        ref Vector2 scrollPosition)
    {
        GUILayout.Label($"{title} ({options.Count})", smallLabelStyle);

        string buttonLabel = ResolveSelectedLabel(options, selectedId);
        if (GUILayout.Button(buttonLabel, buttonStyle, ControlHeightOption()))
            isOpen = !isOpen;

        if (!isOpen)
            return selectedId;

        if (options.Count == 0)
        {
            GUILayout.Label("목록이 없습니다.", smallLabelStyle);
            return selectedId;
        }

        float rowHeight = GetScaledControlHeight(uiScale) + 2f;
        float listHeight = Mathf.Min(options.Count, MaxDropdownRows) * rowHeight;
        scrollPosition = GUILayout.BeginScrollView(
            scrollPosition,
            false,
            true,
            GUILayout.Height(Mathf.Max(rowHeight, listHeight)));

        for (int i = 0; i < options.Count; i++)
        {
            DebugOption option = options[i];
            if (!GUILayout.Button(option.Label, buttonStyle, ControlHeightOption()))
                continue;

            selectedId = option.Id;
            isOpen = false;
            break;
        }

        GUILayout.EndScrollView();
        return selectedId;
    }

    private void DrawClearSelectionButton(ref string selectedId, string label)
    {
        if (GUILayout.Button(label, buttonStyle, ControlHeightOption()))
            selectedId = string.Empty;
    }

    private void DrawSlotGrid(
        int count,
        int columns,
        Func<int, string> labelBuilder,
        Action<int> onClick,
        GUILayoutOption heightOption = null)
    {
        int safeColumns = Mathf.Max(1, columns);

        for (int i = 0; i < count; i++)
        {
            if (i % safeColumns == 0)
                GUILayout.BeginHorizontal();

            GUILayoutOption option = heightOption ?? SlotHeightOption();
            if (GUILayout.Button(labelBuilder(i), buttonStyle, option))
                onClick(i);

            if (i % safeColumns == safeColumns - 1 || i == count - 1)
                GUILayout.EndHorizontal();
        }
    }

    private void DrawIntStepper(string label, ref int value, int min, int max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: {value}", smallLabelStyle, GUILayout.Width(130f * uiScale));
        if (GUILayout.Button("-1", buttonStyle, ControlHeightOption()))
            value = Mathf.Clamp(value - 1, min, max);
        if (GUILayout.Button("+1", buttonStyle, ControlHeightOption()))
            value = Mathf.Clamp(value + 1, min, max);
        GUILayout.EndHorizontal();
    }

    private void DrawStatStepper(string label, string value, Action<int> applyDelta)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: {value}", smallLabelStyle, GUILayout.Width(145f * uiScale));

        for (int i = 0; i < StatDeltas.Length; i++)
        {
            int delta = StatDeltas[i];
            string text = delta > 0 ? $"+{delta}" : delta.ToString();

            if (!GUILayout.Button(text, buttonStyle, ControlHeightOption()))
                continue;

            applyDelta(delta);
            BattleEffectDebugTool.RefreshBattle();
        }

        GUILayout.EndHorizontal();
    }

    private CharacterRuntimeData GetSelectedRuntime()
    {
        return BattleEffectDebugTool.GetPartyRuntime(selectedPartyIndex);
    }

    private List<DebugOption> BuildCharacterOptions()
    {
        List<DebugOption> result = new();

        if (DataManager.Instance?.CharacterDatabase?.GetAll() == null)
            return result;

        foreach (KeyValuePair<string, CharacterMasterData> pair in DataManager.Instance.CharacterDatabase.GetAll())
        {
            CharacterMasterData data = pair.Value;
            if (data == null)
                continue;

            AddOptionIfMatches(result, data.CharacterId, data.Name);
        }

        SortOptions(result);
        return result;
    }

    private List<DebugOption> BuildSkillOptions()
    {
        List<DebugOption> result = new();
        List<SkillMasterData> allSkills = DataManager.Instance?.SkillDatabase?.GetAll();

        if (allSkills == null)
            return result;

        for (int i = 0; i < allSkills.Count; i++)
        {
            SkillMasterData data = allSkills[i];
            if (data == null)
                continue;

            AddOptionIfMatches(result, data.SkillId, data.Name);
        }

        SortOptions(result);
        return result;
    }

    private List<DebugOption> BuildRelicOptions()
    {
        List<DebugOption> result = new();
        IReadOnlyList<RelicData> allRelics = DataManager.Instance?.RelicDatabase?.GetAll();

        if (allRelics == null)
            return result;

        for (int i = 0; i < allRelics.Count; i++)
        {
            RelicData data = allRelics[i];
            if (data == null ||
                data is CompoundData ||
                BattleEffectDebugTool.IsCompoundId(data.FragmentId))
            {
                continue;
            }

            AddOptionIfMatches(result, data.FragmentId, data.Name);
        }

        SortOptions(result);
        return result;
    }

    private List<DebugOption> BuildRuneOptions()
    {
        List<DebugOption> result = new();
        List<RuneData> allRunes = DataManager.Instance?.RuneDatabase?.GetAll();

        if (allRunes == null)
            return result;

        for (int i = 0; i < allRunes.Count; i++)
        {
            RuneData data = allRunes[i];
            if (data == null)
                continue;

            AddOptionIfMatches(result, data.RuneId, data.Name);
        }

        SortOptions(result);
        return result;
    }

    private List<DebugOption> BuildCompoundOptions()
    {
        List<DebugOption> result = new();
        IReadOnlyList<CompoundData> allCompounds = DataManager.Instance?.CompoundDatabase?.GetAll();

        if (allCompounds == null)
            return result;

        for (int i = 0; i < allCompounds.Count; i++)
        {
            CompoundData data = allCompounds[i];
            if (data == null)
                continue;

            AddOptionIfMatches(result, data.CompoundId, data.Name);
        }

        SortOptions(result);
        return result;
    }

    private List<DebugOption> BuildGridEffectOptions()
    {
        List<DebugOption> result = new();
        IReadOnlyDictionary<string, GridEffectData> allEffects = DataManager.Instance?.GridEffectDatabase?.GetAll();

        if (allEffects == null)
            return result;

        foreach (KeyValuePair<string, GridEffectData> pair in allEffects)
        {
            GridEffectData data = pair.Value;
            if (data == null)
                continue;

            AddOptionIfMatches(result, data.GridEffectID, data.Name);
        }

        SortOptions(result);
        return result;
    }

    private void AddOptionIfMatches(List<DebugOption> result, string id, string name)
    {
        DebugOption option = new(id, name);

        if (string.IsNullOrWhiteSpace(option.Id) || !MatchesSearch(option))
            return;

        result.Add(option);
    }

    private bool MatchesSearch(DebugOption option)
    {
        if (string.IsNullOrWhiteSpace(idSearch))
            return true;

        string query = idSearch.Trim();
        return ContainsIgnoreCase(option.Id, query) ||
               ContainsIgnoreCase(option.Name, query);
    }

    private static bool ContainsIgnoreCase(string value, string query)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void SortOptions(List<DebugOption> options)
    {
        options.Sort((left, right) => string.Compare(left.Id, right.Id, StringComparison.Ordinal));
    }

    private static string ResolveSelectedLabel(IReadOnlyList<DebugOption> options, string selectedId)
    {
        if (string.IsNullOrWhiteSpace(selectedId))
            return "선택 없음";

        for (int i = 0; i < options.Count; i++)
        {
            if (string.Equals(options[i].Id, selectedId.Trim(), StringComparison.Ordinal))
                return options[i].Label;
        }

        return selectedId.Trim();
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

    private void PlaceGrid(int gridIndex, string gridEffectId)
    {
        if (BattleEffectDebugTool.TryPlaceGridEffect(gridIndex, gridEffectId))
            Debug.Log($"[BattleEffectDebug] PlaceGridEffect:{gridEffectId} Grid:{gridIndex}");
        else
            Debug.LogWarning($"[BattleEffectDebug] Failed PlaceGridEffect:{gridEffectId} Grid:{gridIndex}");
    }

    private void RemoveGrid(int gridIndex)
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
                $"Armor:{runtime.CurrentShield} " +
                $"Recovery:{runtime.CostRecovery} " +
                $"Skills:{JoinDisplaySkills(runtime)} " +
                $"Relics:{JoinPassiveRelics(runtime)} " +
                $"Runes:{JoinIds(runtime.EquippedRuneIds)} " +
                $"Compound:{BattleEffectDebugTool.GetCompoundSlotId(runtime)}");
        }
    }

    private void EnsureStyles()
    {
        smallLabelStyle = new GUIStyle(GUI.skin.label)
        {
            wordWrap = true,
            fontSize = Mathf.RoundToInt(12f * uiScale)
        };

        headerStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Bold,
            fontSize = Mathf.RoundToInt(13f * uiScale)
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = Mathf.RoundToInt(12f * uiScale),
            wordWrap = true
        };

        toggleStyle = new GUIStyle(GUI.skin.toggle)
        {
            fontSize = Mathf.RoundToInt(12f * uiScale),
            wordWrap = true
        };

        textFieldStyle = new GUIStyle(GUI.skin.textField)
        {
            fontSize = Mathf.RoundToInt(12f * uiScale)
        };

        handleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.LowerRight,
            fontSize = Mathf.RoundToInt(20f * uiScale)
        };
    }

    private void DrawResizeHandle()
    {
        Rect handleRect = new(
            windowRect.width - ResizeHandleSize,
            windowRect.height - ResizeHandleSize,
            ResizeHandleSize,
            ResizeHandleSize);

        GUI.Label(handleRect, "\u25E2", handleStyle);

        Event current = Event.current;
        int controlId = GUIUtility.GetControlID(FocusType.Passive);

        switch (current.GetTypeForControl(controlId))
        {
            case EventType.MouseDown:
                if (handleRect.Contains(current.mousePosition) && current.button == 0)
                {
                    isResizing = true;
                    GUIUtility.hotControl = controlId;
                    current.Use();
                }
                break;

            case EventType.MouseDrag:
                if (isResizing && GUIUtility.hotControl == controlId)
                {
                    windowRect.width = Mathf.Max(MinWindowWidth, current.mousePosition.x);
                    windowRect.height = Mathf.Max(MinWindowHeight, current.mousePosition.y);
                    current.Use();
                }
                break;

            case EventType.MouseUp:
                if (isResizing && GUIUtility.hotControl == controlId)
                {
                    isResizing = false;
                    GUIUtility.hotControl = 0;
                    current.Use();
                }
                break;
        }
    }

    private GUIStyle EditorLikeHeaderStyle()
    {
        return headerStyle;
    }

    private GUILayoutOption ControlHeightOption()
    {
        return GUILayout.Height(GetScaledControlHeight(uiScale));
    }

    private GUILayoutOption SlotHeightOption()
    {
        return GUILayout.Height(GetScaledControlHeight(uiScale) * 2f);
    }

    private float SectionSpacing()
    {
        return 8f * uiScale;
    }

    private static string JoinDisplaySkills(CharacterRuntimeData runtime)
    {
        if (runtime == null)
            return string.Empty;

        List<string> ids = new();
        for (int i = 0; i < BattleEffectDebugTool.SkillDisplaySlotCount; i++)
        {
            string id = BattleEffectDebugTool.GetSkillDisplaySlotId(runtime, i);
            if (!string.IsNullOrWhiteSpace(id))
                ids.Add($"{SkillSlotNames[i]}:{id.Trim()}");
        }

        return ids.Count > 0 ? string.Join(", ", ids) : "None";
    }

    private static string JoinPassiveRelics(CharacterRuntimeData runtime)
    {
        if (runtime == null)
            return string.Empty;

        List<string> ids = new();
        for (int i = 0; i < BattleEffectDebugTool.PassiveRelicSlotCount; i++)
        {
            string id = BattleEffectDebugTool.GetPassiveRelicSlotId(runtime, i);
            if (!string.IsNullOrWhiteSpace(id))
                ids.Add(id.Trim());
        }

        return ids.Count > 0 ? string.Join(", ", ids) : "None";
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

    private static string ShortId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return "비어 있음";

        string trimmed = id.Trim();
        return trimmed.Length <= 24 ? trimmed : trimmed.Substring(0, 21) + "...";
    }
}
