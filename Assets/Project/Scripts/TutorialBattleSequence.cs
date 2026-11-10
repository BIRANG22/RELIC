using System;
using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Components;

/// <summary>
/// Executes the pre-battle presentation authored in the Tutorial worksheet.
/// Normal turn planning and input remain owned by BattleRoomLoader.
/// </summary>
public sealed class TutorialBattleSequence : MonoBehaviour
{
    public static bool IsPresentationRunning { get; private set; }
    // Use the runner that actually started the current tutorial. Searching with
    // FindObjectsInactive.Include can return the previous room's disabled runner.
    private static TutorialBattleSequence activeTutorialRunner;

    private static TutorialBattleSequence GetActiveTutorialRunner()
    {
        if (activeTutorialRunner != null && activeTutorialRunner.isActiveAndEnabled &&
            activeTutorialRunner.gameObject.activeInHierarchy)
            return activeTutorialRunner;

        // Defensive recovery for reloaded battle rooms; never select an inactive
        // or disabled runner simply because it happens to be enumerated first.
        foreach (TutorialBattleSequence candidate in FindObjectsByType<TutorialBattleSequence>(
                     FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (candidate != null && candidate.isActiveAndEnabled &&
                candidate.gameObject.activeInHierarchy && candidate.routine != null)
            {
                activeTutorialRunner = candidate;
                return candidate;
            }
        }
        return null;
    }

    private static string activeBattleTutorialMapId = string.Empty;
    public static string ActiveTutorialMapId => activeBattleTutorialMapId;
    public static bool IsFinalTutorialRunning => IsPresentationRunning &&
        string.Equals(activeBattleTutorialMapId, "Map_27", StringComparison.OrdinalIgnoreCase);
    public static bool IsFirstTutorialRunning => IsPresentationRunning && !IsFinalTutorialRunning;
    public static bool CanClickTutorialMonster() => IsFinalTutorialRunning && ActiveBattleGuideStep == 4;
    public static bool CanClickTutorialPlayer() => IsFinalTutorialRunning &&
        ActiveBattleGuideStep == 10;

    // World-object clicks in Tutorial_03 are permitted only on the actual
    // required runtime unit, independently of legacy Tutorial_01 input gates.
    public static bool CanClickFinalTutorialWorldMonster(MonsterUnit monster)
    {
        return CanClickTutorialMonster() && monster != null &&
            monster.RuntimeData != null &&
            string.Equals(monster.RuntimeData.MonsterId, "Mon_07", StringComparison.OrdinalIgnoreCase);
    }

    public static bool CanClickFinalTutorialWorldCharacter(string characterId)
    {
        return CanClickTutorialPlayer() &&
            string.Equals(characterId, "Char_03", StringComparison.Ordinal);
    }

    private static int finalTutorialWorldClickSerial;
    private static string finalTutorialWorldClickCharacterId = string.Empty;
    public static void NotifyFinalTutorialWorldCharacterClicked(string characterId)
    {
        if (!CanClickTutorialPlayer() || !CanSelectTutorialCharacter(characterId)) return;
        finalTutorialWorldClickCharacterId = characterId;
        finalTutorialWorldClickSerial++;
    }
    private static int finalTutorialCompoundUseSerial;
    public static void NotifyFinalTutorialCompoundUsed()
    {
        if (IsFinalTutorialRunning && ActiveBattleGuideStep == 11)
            finalTutorialCompoundUseSerial++;
    }


    // 현재 진행 중인 전투 튜토리얼의 단계입니다. 튜토리얼이 아니면 -1을 반환합니다.
    public static int ActiveBattleGuideStep
    {
        get
        {
            if (!IsPresentationRunning) return -1;
            TutorialBattleSequence sequence = GetActiveTutorialRunner();
            return sequence != null ? sequence.tutorialStageIndex : -1;
        }
    }

    // 기존 스킬 선택 경로를 유지하면서 현재 안내 단계에서 허용한 스킬만 선택합니다.
    public static bool CanUseBattleSkill(string skillId, string moveId, string firstSkillId)
    {
        int step = ActiveBattleGuideStep;
        if (step < 0) return true;
        if (IsFinalTutorialRunning)
        {
            TutorialBattleSequence runner = GetActiveTutorialRunner();
            BattleTimelineController timeline = FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);
            string characterId = timeline?.SelectedCharacter?.CharacterId;
            if (runner == null || timeline == null) return false;
            if ((step == 19 || step == 24) && skillId == moveId)
                return runner.IsFinalExpectedCharacterSelected(timeline);
            if ((step == 12 || step == 22 || step == 26) && skillId == firstSkillId)
                return runner.IsFinalExpectedCharacterSelected(timeline);
            if (step == 13 && timeline.SelectedCharacter != null &&
                skillId == timeline.SelectedCharacter.UniqueSkillId)
                return runner.IsFinalExpectedCharacterSelected(timeline);
            return false;
        }
        if (step == 24)
        {
            if (string.IsNullOrEmpty(moveId) || skillId != moveId)
                return false;

            // 이동 버튼을 연타해 선택 상태가 해제되는 것을 방지합니다.
            PlayerSkillReservationController reservation =
                FindFirstObjectByType<PlayerSkillReservationController>(FindObjectsInactive.Include);
            return reservation == null || !reservation.IsMoveSkillSelectionActive();
        }
        if (step == 26)
        {
            if (string.IsNullOrEmpty(firstSkillId) || skillId != firstSkillId)
                return false;

            TutorialBattleSequence sequence =
                GetActiveTutorialRunner();
            BattleTimelineController timeline =
                FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);
            if (sequence == null || timeline == null || sequence.skill26ReservationBaseline < 0)
                return false;

            // 힐트의 첫 기억은 단 한 번만 예약할 수 있습니다.
            return CountCharacterCommands(timeline, "Char_01") <
                   sequence.skill26ReservationBaseline + 1;
        }
        if (step == 28)
            return !string.IsNullOrEmpty(firstSkillId) && skillId == firstSkillId;
        return false;
    }

    // 튜토리얼 안내 중에는 해당 단계에서 허용한 단축키만 입력할 수 있습니다.
    public static bool IsShortcutAllowed(KeyCode key)
    {
        if (!IsPresentationRunning) return true;
        TutorialBattleSequence sequence = GetActiveTutorialRunner();
        if (sequence == null) return false;
        if (IsFinalTutorialRunning)
        {
            return sequence.tutorialStageIndex switch
            {
                11 => key == KeyCode.D,
                12 or 22 or 26 => key == KeyCode.Q,
                13 => key == KeyCode.R,
                19 or 24 => key == KeyCode.A,
                21 => key == KeyCode.S,
                27 => key == KeyCode.Space,
                _ => false
            };
        }
        switch (sequence.tutorialStageIndex)
        {
            case 24: return key == KeyCode.A;
            case 26:
            case 28: return key == KeyCode.Q;
            case 27: return key == KeyCode.Tab;
            case 29: return key == KeyCode.Space;
            default: return false;
        }
    }

    // 27번 단계에서 카야를 선택했다면 28번 예약이 끝나기 전까지 다른 캐릭터로 전환하지 않습니다.
    // 마우스, HUD, Tab, 타임라인 선택에도 동일한 캐릭터 제한을 적용합니다.
    public static bool CanSelectTutorialCharacter(string characterId)
    {
        if (!IsPresentationRunning) return true;
        TutorialBattleSequence sequence = GetActiveTutorialRunner();
        if (sequence == null) return false;
        if (IsFinalTutorialRunning)
        {
            string required = sequence.tutorialStageIndex switch
            {
                10 or 11 or 12 or 13 => "Char_03",
                18 or 19 or 20 or 21 or 22 => "Char_02",
                23 or 24 or 25 or 26 => "Char_01",
                _ => null
            };
            return required != null && string.Equals(characterId, required, StringComparison.Ordinal);
        }
        if (sequence.tutorialStageIndex < 24 || sequence.tutorialStageIndex > 28)
            return true;
        if (sequence.tutorialStageIndex == 27 || sequence.tutorialStageIndex == 28)
            return characterId == "Char_02";
        return characterId == "Char_01";
    }

    // 튜토리얼 이동 안내 중 25번 단계에서는 Grid_16만 선택할 수 있습니다.
    // 버튼 클릭에 사용한 마우스 입력이 다음 단계의 그리드 클릭으로 전달되지 않도록 방지합니다.
    private bool grid25InputArmed;
    public static bool CanClickTutorialGrid(int gridIndex)
    {
        if (!IsPresentationRunning) return true;
        TutorialBattleSequence sequence = GetActiveTutorialRunner();
        if (sequence == null) return false;
        if (IsFinalTutorialRunning)
            return sequence.grid25InputArmed &&
                ((sequence.tutorialStageIndex == 20 && gridIndex == 32) ||
                 (sequence.tutorialStageIndex == 25 && gridIndex == 17));
        return sequence.tutorialStageIndex == 25 &&
               sequence.grid25InputArmed && gridIndex == 16;
    }

    public static bool CanAcceptTutorialMove(PlayerReservedCommand command)
    {
        if (!IsPresentationRunning) return true;
        TutorialBattleSequence sequence = GetActiveTutorialRunner();
        if (sequence == null || command == null) return false;
        if (IsFinalTutorialRunning)
        {
            BattleTimelineController timeline = FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);
            if (timeline == null || timeline.ActiveSlotIndex !=
                (sequence.tutorialStageIndex == 20 ? 1 : 2)) return false;
            return sequence.grid25InputArmed &&
                ((sequence.tutorialStageIndex == 20 && command.CharacterId == "Char_02" && command.ReservedMoveGridIndex == 32) ||
                 (sequence.tutorialStageIndex == 25 && command.CharacterId == "Char_01" && command.ReservedMoveGridIndex == 17));
        }
        return sequence.tutorialStageIndex == 25 &&
               sequence.grid25InputArmed && command.CharacterId == "Char_01" && command.ReservedMoveGridIndex == 16;
    }

    public static bool IsBlockingBattleStart { get; private set; }
    public bool IsInitialBattleHudReady { get; private set; }

    public void NotifyInitialBattleHudReady() => IsInitialBattleHudReady = true;
    // 대화 및 안내 UI에서 클릭 시 호출할 이벤트를 관리합니다.
    private readonly Dictionary<GraphicRaycaster, bool> blockedBattleRaycasters = new();
    private readonly Dictionary<Collider, bool> blockedGridColliders = new();
    private readonly Dictionary<Collider2D, bool> blockedGridColliders2D = new();
    private GameObject dialogueInputBlocker;
    private Canvas tutorialSortingCanvas;
    private bool previousOverrideSorting;
    private int previousSortingOrder;
    private BattleRoomLoader loader;
    private Coroutine routine;
    private bool continuationMode;
    private int continuationStart;
    private int continuationEnd;

    public static bool IsContinuationActive { get; private set; }
    public static int CurrentContinuationOrder { get; private set; }

    public static void StartContinuation(string mapId, int firstOrder, int lastOrder)
    {
        if (IsPresentationRunning || IsContinuationActive) return;
        var steps = DataManager.Instance?.TutorialDatabase?.GetForMap(mapId);
        if (steps == null || !System.Linq.Enumerable.Any(steps, x => x.Order == firstOrder)) return;
        // Prefer an active runner. An inactive BattleRoom may still exist in the scene
        // while the event room is active, and cannot start coroutines.
        // Include disabled components: completing the initial tutorial disables
        // its runner. Saved reward checkpoints restore the room without Begin().
        TutorialBattleSequence runner = null;
        foreach (TutorialBattleSequence candidate in FindObjectsByType<TutorialBattleSequence>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate != null && candidate.gameObject.activeInHierarchy)
            {
                runner = candidate;
                break;
            }
        }
        if (runner == null)
        {
            // The active room can be reconstructed from a saved checkpoint.
            // Never start a coroutine on the inactive BattleRoom template.
            BattleRoomLoader activeBattle = FindFirstObjectByType<BattleRoomLoader>(FindObjectsInactive.Exclude);
            if (activeBattle != null && activeBattle.gameObject.activeInHierarchy)
            {
                runner = activeBattle.GetComponent<TutorialBattleSequence>();
                if (runner == null)
                    runner = activeBattle.gameObject.AddComponent<TutorialBattleSequence>();
            }
        }
        if (runner == null)
        {
            EventRoomController eventRoom = FindFirstObjectByType<EventRoomController>(FindObjectsInactive.Exclude);
            if (eventRoom != null && eventRoom.gameObject.activeInHierarchy)
            {
                runner = eventRoom.GetComponent<TutorialBattleSequence>();
                if (runner == null)
                    runner = eventRoom.gameObject.AddComponent<TutorialBattleSequence>();
            }
        }
        if (runner != null && runner.gameObject.activeInHierarchy)
            runner.enabled = true;
        if (runner == null || !runner.gameObject.activeInHierarchy || !runner.isActiveAndEnabled)
        {
            Debug.LogWarning($"[Tutorial] No active runner for {mapId}, steps {firstOrder}-{lastOrder}.");
            return;
        }

        activeTutorialRunner = runner;
        runner.continuationMode = true;
        runner.continuationStart = firstOrder;
        runner.continuationEnd = lastOrder;
        runner.routine = runner.StartCoroutine(runner.Run(steps));
        IsContinuationActive = true;
        IsPresentationRunning = true;
    }

    private static readonly HashSet<string> completedTargets = new HashSet<string>();
    public static void NotifyTargetSelected(string target)
    {
        if (!string.IsNullOrEmpty(target)) completedTargets.Add(target);
    }

    private GameObject dialogRoot;
    private GameObject tooltipRoot;
    private GameObject focusRoot;
    private RectTransform overlayRoot;
    private RectTransform worldGridFocusProxy;
    private RectTransform compositeUiFocusProxy;
    [SerializeField, Min(0f)] private float focusSlideDuration = 0.45f;
    private Coroutine focusSlideRoutine;
    private TMP_Text dialogText;
    private TMP_Text guideText;
    private GameObject dialogTextComplete;
    private GameObject tooltipTextComplete;
    private TMP_Text dialogCompleteLabel;
    private TMP_Text tooltipCompleteLabel;
    private Image portrait;
    private GameObject tutorialModeRoot;
    [SerializeField, Min(0f)] private float textCompleteFadeDuration = 0.2f;
    private readonly Dictionary<GameObject, Coroutine> completeFades = new();
    [SerializeField, Min(1f)] private float dialogueCharactersPerSecond = 35f;
    [Header("Dialogue Advance Sound")]
    [SerializeField, SoundId(SoundCategory.Sfx)] private string dialogueAdvanceSoundId = AudioIds.Sfx.TutorialTextAdvance;
    [SerializeField, Range(0f, 1f)] private float dialogueAdvanceSoundVolume = 0.5f;
    private readonly Dictionary<Renderer, bool> gridRenderers = new();
    private BattleTurnExecutor tutorialTurnExecutor;
    private bool tutorialControlInputOpened;
    private bool tutorialEndTurnClicked;
    private UnityEngine.UI.Button tutorialEndTurnButton;
    // Block pointer feedback on unrelated battle buttons without disabling their normal handlers.
    private readonly Dictionary<CanvasGroup, bool> tutorialButtonRaycastStates = new();
    private readonly HashSet<CanvasGroup> tutorialCreatedButtonGroups = new();
    private int lastTutorialPointerStage = int.MinValue;
    private int lastTooltipScreenWidth = -1;
    private int lastTooltipScreenHeight = -1;
    private bool tooltipLayoutDirty;

    public static bool CanClickTutorialEndTurn()
    {
        if (!IsPresentationRunning) return true;
        TutorialBattleSequence sequence = GetActiveTutorialRunner();
        return sequence != null &&
            (IsFinalTutorialRunning ? sequence.tutorialStageIndex == 27 : sequence.tutorialStageIndex == 29);
    }

    private void Update()
    {
        if (IsFinalTutorialRunning && tutorialStageIndex == 4 && Input.GetMouseButtonDown(0))
        {
            Camera camera = Camera.main;
            Vector3 screenPosition = Input.mousePosition;
            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (camera == null)
                Debug.LogWarning($"[T03CLICK] MouseDown stage4: NO Camera.main, overUI={overUi}", this);
            else
            {
                Ray ray = camera.ScreenPointToRay(screenPosition);
                RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray);
                System.Text.StringBuilder hitNames = new System.Text.StringBuilder();
                foreach (RaycastHit2D hit in hits)
                {
                    if (hit.collider == null) continue;
                    MonsterUnit monster = hit.collider.GetComponentInParent<MonsterUnit>();
                    if (hitNames.Length > 0) hitNames.Append(" | ");
                    hitNames.Append(hit.collider.name).Append(" [monster=")
                        .Append(monster != null ? monster.RuntimeData?.MonsterId : "none")
                        .Append("]");
                }
                Debug.Log($"[T03CLICK] MouseDown stage4 screen={screenPosition} overUI={overUi} physicsHits={hits.Length} hitList={hitNames}", this);
                // OnMouseDown messages are delivered to the Collider2D GameObject,
                // which may differ from the GameObject carrying MonsterUnit.
                // Use the verified physics hit to forward this world click through
                // the same MonsterUnit selection path, only during T03 step 4.
                if (!overUi)
                {
                    foreach (RaycastHit2D hit in hits)
                    {
                        if (hit.collider == null) continue;
                        MonsterUnit targetMonster = hit.collider.GetComponentInParent<MonsterUnit>();
                        if (!CanClickFinalTutorialWorldMonster(targetMonster))
                        {
                            Debug.LogWarning($"[T03CLICK] World target rejected: target={targetMonster?.RuntimeData?.MonsterId ?? "null"}, running={IsPresentationRunning}, map={activeBattleTutorialMapId}, activeStep={ActiveBattleGuideStep}, localStep={tutorialStageIndex}, runner={GetActiveTutorialRunner()?.name ?? "null"}", this);
                            continue;
                        }
                        Debug.Log($"[T03CLICK] Forward physics hit to MonsterUnit: {targetMonster.name}", targetMonster);
                        targetMonster.TryHandleFinalTutorialWorldClick();
                        break;
                    }
                }
            }
        }
        // Tutorial_03 step 10 selects Haze through her world BoxCollider2D.
        // Unity OnMouseDown can be skipped after a continuous room transition;
        // use the same verified physics click path as the Draugr step.
        if (IsFinalTutorialRunning && tutorialStageIndex == 10 && Input.GetMouseButtonDown(0))
        {
            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            Camera worldCamera = Camera.main;
            if (!overUi && worldCamera != null)
            {
                foreach (RaycastHit2D hit in Physics2D.GetRayIntersectionAll(worldCamera.ScreenPointToRay(Input.mousePosition)))
                {
                    if (hit.collider == null) continue;
                    BattleCharacter character = hit.collider.GetComponentInParent<BattleCharacter>();
                    if (character == null || character.RuntimeData == null ||
                        !CanClickFinalTutorialWorldCharacter(character.RuntimeData.CharacterId)) continue;
                    Debug.Log($"[T03CLICK] Step10 Haze physics hit: {hit.collider.name}, character={character.RuntimeData.CharacterId}", character);
                    character.TryHandleFinalTutorialWorldClick();
                    break;
                }
            }
        }
        if (!IsPresentationRunning || continuationMode) return;
        if (TutorialPresentationRefreshPolicy.ShouldRefreshButtons(
                lastTutorialPointerStage,
                tutorialStageIndex))
        {
            lastTutorialPointerStage = tutorialStageIndex;
            ApplyTutorialButtonRaycasts();
        }
    }

    private void ApplyTutorialButtonRaycasts()
    {
        BattleCharacterPanelUI panel = FindFirstObjectByType<BattleCharacterPanelUI>(FindObjectsInactive.Include);
        if (panel != null)
        {
            foreach (Button button in panel.GetComponentsInChildren<Button>(true))
                ConfigureTutorialButton(button);
        }
        BattleTurnExecutor executor = FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);
        if (executor != null) ConfigureTutorialButton(executor.EndTurnButton);
    }

    private void ConfigureTutorialButton(Button button)
    {
        if (button == null) return;
        CanvasGroup group = button.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = button.gameObject.AddComponent<CanvasGroup>();
            tutorialCreatedButtonGroups.Add(group);
        }
        if (!tutorialButtonRaycastStates.ContainsKey(group))
            tutorialButtonRaycastStates[group] = group.blocksRaycasts;
        string name = button.gameObject.name;
        bool allowed = IsFinalTutorialRunning ? tutorialStageIndex switch
        {
            11 => name.Equals("Compound", StringComparison.OrdinalIgnoreCase),
            12 or 22 or 26 => name.Equals("Skill01", StringComparison.OrdinalIgnoreCase),
            13 => name.Equals("Ultimate", StringComparison.OrdinalIgnoreCase),
            19 or 24 => name.Equals("Move", StringComparison.OrdinalIgnoreCase),
            21 => name.Equals("Flip", StringComparison.OrdinalIgnoreCase),
            18 => name.Equals("Char02", StringComparison.OrdinalIgnoreCase),
            23 => name.Equals("Char01", StringComparison.OrdinalIgnoreCase),
            27 => button == FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include)?.EndTurnButton,
            _ => false
        } : tutorialStageIndex switch
        {
            24 => name.Equals("Move", StringComparison.OrdinalIgnoreCase) || name.Equals("MoveButton", StringComparison.OrdinalIgnoreCase),
            26 or 28 => name.Equals("Skill01", StringComparison.OrdinalIgnoreCase) || name.Equals("Skill01Button", StringComparison.OrdinalIgnoreCase),
            27 => name.IndexOf("Tab", StringComparison.OrdinalIgnoreCase) >= 0,
            29 => button == FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include)?.EndTurnButton,
            _ => false
        };
        group.blocksRaycasts = allowed && tutorialButtonRaycastStates[group];
    }

    private void RestoreTutorialButtonRaycasts()
    {
        foreach (var pair in tutorialButtonRaycastStates)
            if (pair.Key != null) pair.Key.blocksRaycasts = pair.Value;
        tutorialButtonRaycastStates.Clear();
        foreach (CanvasGroup group in tutorialCreatedButtonGroups)
            if (group != null) Destroy(group);
        tutorialCreatedButtonGroups.Clear();
        lastTutorialPointerStage = int.MinValue;
    }

    private int tutorialStageIndex;
    private int skill26ReservationBaseline = -1;
    private int skill28ReservationBaseline = -1;

    // 예약 명령이 현재 튜토리얼 단계에서 허용되는지 검사합니다.
    public static bool CanAcceptTutorialCommand(PlayerReservedCommand command, BattleTimelineController timeline)
    {
        if (!IsPresentationRunning || command == null || timeline == null) return true;
        TutorialBattleSequence sequence = GetActiveTutorialRunner();
        if (sequence == null) return true;
        // 다른 그리드나 다른 캐릭터의 이동 예약은 현재 단계에서 차단합니다.
        if (IsFinalTutorialRunning)
        {
            // Flip commands also carry their current grid index, so validate
            // them before treating commands with a grid index as movement.
            // Otherwise the Grid_32/Grid_17 movement gate rejects Flip.
            if (command.IsFacingFlipCommand)
                return sequence.tutorialStageIndex == 21 &&
                    sequence.IsFinalExpectedCharacterSelected(timeline) &&
                    command.CharacterId == "Char_02" &&
                    timeline.ActiveSlotIndex == 1;
            if (command.ReservedMoveGridIndex >= 0)
                return CanAcceptTutorialMove(command);
            if (!sequence.IsFinalExpectedCharacterSelected(timeline) ||
                command.CharacterId != timeline.SelectedCharacter?.CharacterId)
                return false;
            int stage = sequence.tutorialStageIndex;
            int slot = stage <= 13 ? 0 : stage <= 22 ? 1 : 2;
            if (timeline.ActiveSlotIndex != slot) return false;
            if (stage == 12 || stage == 22 || stage == 26)
                return command.SkillId == timeline.SelectedCharacter.AbilitySkillId;
            if (stage == 13)
                return command.SkillId == timeline.SelectedCharacter.UniqueSkillId;
            return false;
        }
        if (command.ReservedMoveGridIndex >= 0)
            return CanAcceptTutorialMove(command);
        if (sequence.tutorialStageIndex == 25)
            return false;
        if (sequence.tutorialStageIndex == 26)
            return command.CharacterId == "Char_01" && sequence.skill26ReservationBaseline >= 0 &&
                CountCharacterCommands(timeline, "Char_01") < sequence.skill26ReservationBaseline + 1;
        if (sequence.tutorialStageIndex == 28)
            return command.CharacterId == "Char_02" && sequence.skill28ReservationBaseline >= 0 &&
                CountCharacterCommands(timeline, "Char_02") < sequence.skill28ReservationBaseline + 2;
        return true;
    }



    public void Begin(BattleRoomLoader battleLoader, string mapId)
    {
        if (routine != null) StopCoroutine(routine);
        continuationMode = false;
        loader = battleLoader;
        activeTutorialRunner = this;
        activeBattleTutorialMapId = mapId ?? string.Empty;
        IsPresentationRunning = true;
        // In Map_27, dialogue 1-3 must precede monster planning and the battle-start intro.
        // Keep the loader's initial plan paused until those lines finish.
        IsBlockingBattleStart = true;
        IsInitialBattleHudReady = false;
        var steps = DataManager.Instance?.TutorialDatabase?.GetForMap(mapId);
        if (steps == null || steps.Count == 0)
        {
            IsPresentationRunning = false;
            IsBlockingBattleStart = false;
            return;
        }
        routine = StartCoroutine(Run(steps));
    }

    private IEnumerator Run(IReadOnlyList<TutorialData> steps)
    {
        // Wait for the standard room entry and its character movement to finish.
        while (!continuationMode && BattleSceneController.IsBattleRoomIntroPlaying)
            yield return null;
        yield return null;
        ResolveUi();
        // Map_27 begins with its three encounter dialogue lines before the
        // battle-start intro. Its HUD is intentionally not ready at this point.
        if (!IsFinalTutorialRunning) HideGridVisuals();
        HideUi();

        string previousGuideTarget = null;
        foreach (TutorialData step in steps)
        {
            if (step == null) continue;
            if (!continuationMode && step.Order >= 30) break;
            if (continuationMode && (step.Order < continuationStart || step.Order > continuationEnd)) continue;
            if (continuationMode) CurrentContinuationOrder = step.Order;
            string phase = step.Phase ?? string.Empty;
            string output = step.TextOutput ?? string.Empty;
            bool sameGuideTarget = output.Equals("TutorialTooltip", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(previousGuideTarget)
                && string.Equals(previousGuideTarget, step.HighlightTarget, StringComparison.OrdinalIgnoreCase);
            previousGuideTarget = output.Equals("TutorialTooltip", StringComparison.OrdinalIgnoreCase)
                ? step.HighlightTarget : null;
            // Map_27: after dialogues 1-3, allow the loader to plan the monsters,
            // play its original battle-start intro once, and raise the reservation HUD.
            // Only then show the monster-click guide (order 4).
            if (!continuationMode && IsFinalTutorialRunning && step.Order == 4 && IsBlockingBattleStart)
            {
                HideUi();
                IsBlockingBattleStart = false;
                while (!IsInitialBattleHudReady) yield return null;
                while (BattleSceneController.IsBattleRoomIntroPlaying || BattleMapIntroText.IsAnyPlayingOrVisible())
                    yield return null;
                yield return null;
            }
            // HUD guides run after battle-start presentation and timeline slots are visible.
            if (IsCharacterHudGuide(step) && IsBlockingBattleStart)
            {
                HideUi();
                IsBlockingBattleStart = false;
                while (!IsInitialBattleHudReady) yield return null;
                // 이전 단계에서 차단한 그리드 표시를 복구하고 준비 완료를 기다립니다.
                RestoreGridVisuals();
                GridManager preparedGrid = FindFirstObjectByType<GridManager>(FindObjectsInactive.Exclude);
                if (preparedGrid != null)
                    preparedGrid.SetGridVisible(true);
                yield return WaitUntilGridRendered();
            }
            if (phase.Equals("SpawnMonsters", StringComparison.OrdinalIgnoreCase))
            {
                HideUi();
                RestoreGridVisuals();
                loader.CompleteDeferredMonsterSpawn();
                yield return loader.PlayTutorialMonsterEntrance();
                continue;
            }

            if (output.Equals("BattleMapIntroText", StringComparison.OrdinalIgnoreCase))
            {
                HideUi();
                yield return BattleMapIntroText.ShowMessageAndWait(Localize(step));
            }
            else if (output.Equals("Dialogbox", StringComparison.OrdinalIgnoreCase))
            {
                SetBattleUiPointerBlocked(true);
                ShowDialogue(step);
                yield return RevealDialogueAndWaitForClick();
                yield return FadeOutComplete(dialogTextComplete);
                SetBattleUiPointerBlocked(false);
            }
            else if (output.Equals("TutorialTooltip", StringComparison.OrdinalIgnoreCase))
            {
                tutorialStageIndex = step.Order;
                grid25InputArmed = false;
                if (!continuationMode && (IsFinalTutorialRunning
                    ? step.Order == 20 || step.Order == 25
                    : step.Order == 25))
                {
                    // 24번 이동 버튼 클릭이나 키 입력이 25번 그리드 입력으로 처리되지 않게 합니다.
                    while (Input.GetMouseButton(0) || Input.touchCount > 0)
                        yield return null;
                    yield return null;
                    grid25InputArmed = true;
                }
                // Only the first hands-on control step opens normal reservation input.
                // BattleRoomLoader still owns the later turn / input lifecycle.
                if ((IsFinalTutorialRunning ? step.Order == 4 : step.Order == 24) && !tutorialControlInputOpened)
                {
                    tutorialTurnExecutor = FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);
                    if (tutorialTurnExecutor != null)
                    {
                        tutorialTurnExecutor.SetBattleInputReady(true);
                        tutorialControlInputOpened = true;
                    }
                }
                // Map_27: the monster-info panel and tutorial dialogue may leave the
                // player-input-ready flag false after closing the panel. Compound_03
                // is immediate-use, but still requires an input-ready battle turn.
                // Restore only readiness (not SetBattleInputReady, which re-applies
                // turn-start equipment effects and may reset the selected slot).
                if (!continuationMode && IsFinalTutorialRunning && step.Order == 11)
                {
                    BattleTurnExecutor compoundExecutor = tutorialTurnExecutor != null
                        ? tutorialTurnExecutor
                        : FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Exclude);
                    if (compoundExecutor != null && !compoundExecutor.IsExecuting &&
                        !compoundExecutor.CanAcceptPlayerInput)
                    {
                        compoundExecutor.SetPlayerInputReady(true);
                        if (!compoundExecutor.CanAcceptPlayerInput)
                        {
                            Debug.LogWarning("[Tutorial_03] Step11: player input restored, but turn not ready; checking monster-plan readiness.");
                            compoundExecutor.SetMonsterPlanReady(true);
                        }
                        Debug.Log($"[Tutorial_03] Step11 compound input ready={compoundExecutor.CanAcceptPlayerInput}");
                    }

                }
                // 타임라인 및 행동 소개 단계에서는 안내를 넘길 수 있지만 실제 UI 조작은 차단합니다.
                bool blockGuideInput = !continuationMode &&
                    (IsBattleUiExplanationGuide(step) ||
                     (IsFinalTutorialRunning && (step.Order == 5 || step.Order == 6 || step.Order == 28)));
                ShowGuide(step, sameGuideTarget);
                if (IsFinalTutorialRunning && step.Order == 28) ShowFinalTutorialCompletionOverlay();
                if (blockGuideInput)
                {
                    SetBattleUiPointerBlocked(true);
                    if (dialogueInputBlocker != null) dialogueInputBlocker.SetActive(true);
                }
                // ClickAny 단계는 화면 클릭으로 진행하며 그 외 단계는 지정된 조건을 따릅니다.
                string advance = step.Advance ?? string.Empty;
                if (advance.Equals("ClickTarget", StringComparison.OrdinalIgnoreCase) ||
                    advance.Equals("ReserveMove", StringComparison.OrdinalIgnoreCase) ||
                    advance.Equals("ReserveAction", StringComparison.OrdinalIgnoreCase) ||
                    advance.Equals("ReserveActionTwice", StringComparison.OrdinalIgnoreCase))
                {
                    if (continuationMode)
                    {
                        string targetKey = step.Order == 33 ? "reward" : step.Order == 36 ? "equip" : "event";
                        completedTargets.Remove(targetKey);
                        while (!completedTargets.Contains(targetKey)) yield return null;
                    }
                    else yield return WaitForRequiredBattleAction(step);
                }
                else
                    yield return WaitForClick();
                if (IsFinalTutorialRunning && step.Order == 7)
                {
                    // The monster info close event schedules HUD restoration on the next frame.
                    // During tutorial input gating, the normal input-ready check can leave
                    // BattleCharacterPanel and BattleSlot at their lowered positions.
                    // Let the close event settle, then restore the tutorial reservation HUD
                    // before continuing to the next dialogue.
                    while (MonsterUnit.CurrentInfoSelectedMonster != null)
                        yield return null;
                    yield return null;
                    yield return null;
                    BattleCharacterPanelUI characterPanel = FindFirstObjectByType<BattleCharacterPanelUI>(FindObjectsInactive.Exclude);
                    if (characterPanel != null)
                    {
                        characterPanel.BeginTutorialReservationPanelMove();
                        while (characterPanel != null && characterPanel.isActiveAndEnabled &&
                               !characterPanel.IsAtReservationPosition)
                            yield return null;
                    }
                    yield return FadeOutComplete(tooltipTextComplete);
                }
                else if (IsFinalTutorialRunning && step.Order == 27)
                {
                    // Clear the step-27 guide and focus immediately on End Turn.
                    // No tutorial focus may remain during the execution animation.
                    HideUi();
                    RestoreTutorialButtonRaycasts();
                    BattleTurnExecutor executor = FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);
                    if (executor != null)
                    {
                        // Wait for the actual turn execution before proceeding to step 28.
                        while (!executor.IsExecuting)
                            yield return null;
                        while (executor.IsExecuting || !executor.CanAcceptPlayerInput)
                            yield return null;
                    }
                }
                else
                    yield return FadeOutComplete(tooltipTextComplete);
                if (blockGuideInput)
                {
                    SetBattleUiPointerBlocked(false);
                    if (dialogueInputBlocker != null) dialogueInputBlocker.SetActive(false);
                }
            }

            if (phase.Equals("StartBattle", StringComparison.OrdinalIgnoreCase))
            {
                HideUi();
                // The existing loader is already waiting on deferred monster spawn.
                // It owns monster planning, battle intro, turn 1 and HUD activation.
                if (loader != null) loader.CompleteDeferredMonsterSpawn();
                break;
            }
        }

        HideUi();
        routine = null;
        SetBattleUiPointerBlocked(false);
        ReleaseDialogueInputBlocker();
        RestoreTutorialButtonRaycasts();
        if (activeTutorialRunner == this)
        {
            activeTutorialRunner = null;
            IsPresentationRunning = false;
            IsBlockingBattleStart = false;
            activeBattleTutorialMapId = string.Empty;
        }
        if (continuationMode)
        {
            IsContinuationActive = false;
            CurrentContinuationOrder = 0;
            continuationMode = false;
        }
        enabled = false;
    }

    private static bool IsBattleUiExplanationGuide(TutorialData step)
    {
        if (step == null || !string.Equals(step.Advance, "ClickAny", StringComparison.OrdinalIgnoreCase))
            return false;

        string key = step.TextKey ?? string.Empty;
        return string.Equals(key, "tutorial.t01.battleslot.01", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(key, "tutorial.t01.battleslot.02", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(key, "tutorial.t01.action.01", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(key, "tutorial.t01.action.02", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBattleSlotGuide(TutorialData step)
    {
        string target = step?.HighlightTarget?.Replace('\\', '/').Trim('/');
        return !string.IsNullOrEmpty(target) &&
            (target.Equals("BattleSlot/Back", StringComparison.OrdinalIgnoreCase) ||
             target.EndsWith("/BattleSlot/Back", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsCharacterHudGuide(TutorialData step)
    {
        string target = step?.HighlightTarget?.Replace('\\', '/').Trim('/');
        return !string.IsNullOrEmpty(target) &&
            (target.Equals("Char01/Back", StringComparison.OrdinalIgnoreCase) ||
             target.Equals("CharHUD/Char01/Back", StringComparison.OrdinalIgnoreCase));
    }

    private void HideGridVisuals()
    {
        gridRenderers.Clear();
        foreach (GridCell cell in FindObjectsByType<GridCell>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            foreach (Renderer renderer in cell.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || gridRenderers.ContainsKey(renderer)) continue;
                gridRenderers.Add(renderer, renderer.enabled);
                renderer.enabled = false;
            }
        }
    }

    private void RestoreGridVisuals()
    {
        foreach (var entry in gridRenderers)
            if (entry.Key != null) entry.Key.enabled = entry.Value;
        gridRenderers.Clear();
    }

    private static string Localize(TutorialData step)
    {
        if (string.IsNullOrWhiteSpace(step.TextKey)) return string.Empty;
        string result = GameLocalization.Get(step.TextKey, step.TextKO ?? string.Empty);
        return NormalizeLineBreaks(result);
    }

    private static string NormalizeLineBreaks(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        // Text may originate from spreadsheet cells or localization tables.
        return value.Replace("<br />", "\n").Replace("<br/>", "\n")
            .Replace("<br>", "\n").Replace("<BR>", "\n")
            .Replace("\\r\\n", "\n").Replace("\\n", "\n")
            .Replace("\r\n", "\n").Replace("\r", "\n");
    }

    private void ResolveUi()
    {
        Transform tutorialRoot = null;
        foreach (Transform candidate in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (candidate.name == "TutorialMode" && candidate.gameObject.scene.IsValid())
            {
                tutorialRoot = candidate;
                break;
            }
        }
        if (tutorialRoot == null)
        {
            Debug.LogError("[TutorialBattleSequence] TutorialMode not found.", this);
            return;
        }
        tutorialModeRoot = tutorialRoot.gameObject;
        // Scene designers may leave the entire TutorialMode hierarchy inactive.
        if (!tutorialModeRoot.activeSelf) tutorialModeRoot.SetActive(true);
        Transform dialog = Find(tutorialRoot, "Dialogbox");
        Transform tooltip = Find(tutorialRoot, "TutorialTooltip");
        Transform focus = Find(tutorialRoot, "HighlightFrame");
        overlayRoot = Find(tutorialRoot, "FocusOverlay") as RectTransform;
        dialogRoot = dialog != null ? dialog.gameObject : null;
        tooltipRoot = tooltip != null ? tooltip.gameObject : null;
        focusRoot = focus != null ? focus.gameObject : null;
        dialogText = dialog != null ? Find(dialog, "Dialog")?.GetComponent<TMP_Text>() : null;
        guideText = tooltip != null ? Find(tooltip, "Text")?.GetComponent<TMP_Text>() : null;
        if (guideText == null && tooltip != null) guideText = tooltip.GetComponentInChildren<TMP_Text>(true);
        ProtectTutorialText(dialogText);
        ProtectTutorialText(guideText);
        dialogTextComplete = dialog != null ? Find(dialog, "TextComplete")?.gameObject : null;
        tooltipTextComplete = tooltip != null ? Find(tooltip, "TextComplete")?.gameObject : null;
        dialogCompleteLabel = dialogTextComplete != null ? dialogTextComplete.GetComponentInChildren<TMP_Text>(true) : null;
        tooltipCompleteLabel = tooltipTextComplete != null ? tooltipTextComplete.GetComponentInChildren<TMP_Text>(true) : null;
        // TextComplete 문구가 다른 로컬라이징 컴포넌트에 의해 덮어쓰이지 않게 보호합니다.
        ProtectTutorialText(dialogCompleteLabel);
        ProtectTutorialText(tooltipCompleteLabel);
        portrait = dialog != null ? Find(dialog, "Icon")?.GetComponent<Image>() : null;
        CreateDialogueInputBlocker(tutorialRoot);
    }

    // TutorialBattleSequence resolves TextKey itself, so no second component may
    // bind or overwrite the text while the dialogue typewriter is running.
    private static void ProtectTutorialText(TMP_Text target)
    {
        if (target == null) return;
        if (target.GetComponent<LocalizationAutoBindingIgnore>() == null)
            target.gameObject.AddComponent<LocalizationAutoBindingIgnore>();
        var staticWriter = target.GetComponent<LocalizedTMPText>();
        if (staticWriter != null) staticWriter.enabled = false;
        var dynamicWriter = target.GetComponent<DynamicLocalizedTMPText>();
        if (dynamicWriter != null) dynamicWriter.enabled = false;
        var legacyWriter = target.GetComponent<LocalizeStringEvent>();
        if (legacyWriter != null) legacyWriter.enabled = false;
    }

    private static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; ++i)
        {
            Transform result = Find(root.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }

    private void HideUi()
    {
        if (dialogueInputBlocker != null) dialogueInputBlocker.SetActive(false);
        if (dialogRoot != null) dialogRoot.SetActive(false);
        if (tooltipRoot != null) tooltipRoot.SetActive(false);
        if (focusRoot != null) focusRoot.SetActive(false);
        if (focusSlideRoutine != null)
        {
            StopCoroutine(focusSlideRoutine);
            focusSlideRoutine = null;
        }
        if (overlayRoot != null) overlayRoot.gameObject.SetActive(false);
        ResetComplete(dialogTextComplete);
        ResetComplete(tooltipTextComplete);
    }

    private static CanvasGroup EnsureCompleteCanvasGroup(GameObject target)
    {
        if (target == null) return null;
        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null) group = target.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        return group;
    }

    private void ResetComplete(GameObject target)
    {
        if (target == null) return;
        if (completeFades.TryGetValue(target, out Coroutine fade) && fade != null)
            StopCoroutine(fade);
        completeFades.Remove(target);
        CanvasGroup group = EnsureCompleteCanvasGroup(target);
        if (group != null) group.alpha = 0f;
        target.SetActive(false);
    }

    private void SetComplete(GameObject target, bool visible)
    {
        if (target == null) return;
        if (completeFades.TryGetValue(target, out Coroutine previous) && previous != null)
            StopCoroutine(previous);
        completeFades.Remove(target);
        CanvasGroup group = EnsureCompleteCanvasGroup(target);
        if (visible)
        {
            target.SetActive(true);
            group.alpha = 0f;
            if (isActiveAndEnabled)
                completeFades[target] = StartCoroutine(FadeComplete(target, 0f, 1f));
            else group.alpha = 1f;
        }
        else
        {
            if (group != null) group.alpha = 0f;
            target.SetActive(false);
        }
    }

    private IEnumerator FadeComplete(GameObject target, float from, float to)
    {
        if (target == null) yield break;
        CanvasGroup group = EnsureCompleteCanvasGroup(target);
        if (group == null) yield break;
        float duration = Mathf.Max(0.001f, textCompleteFadeDuration);
        float elapsed = 0f;
        group.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        group.alpha = to;
        if (to <= 0f) target.SetActive(false);
        completeFades.Remove(target);
    }

    private IEnumerator FadeOutComplete(GameObject target)
    {
        if (target == null || !target.activeInHierarchy) yield break;
        if (completeFades.TryGetValue(target, out Coroutine previous) && previous != null)
            StopCoroutine(previous);
        completeFades.Remove(target);
        CanvasGroup group = EnsureCompleteCanvasGroup(target);
        yield return FadeComplete(target, group.alpha, 0f);
    }

    // A step can override the completion prompt without changing the prefab text.
    // Translation uses the same Text table as other tutorial dialogue.
    private void SetCompletionPrompt(TutorialData step, TMP_Text label)
    {
        if (step == null || label == null) return;
        string key = step.CompleteTextKey;
        string korean = step.CompleteTextKO;
        if (string.IsNullOrWhiteSpace(key))
        {
            string advance = step.Advance ?? string.Empty;
            key = advance.Equals("ClickAny", StringComparison.OrdinalIgnoreCase)
                ? "tutorial.prompt.click_any"
                : "tutorial.prompt.press_button";
        }
        // If no explicit worksheet fallback exists, preserve the configured prefab
        // phrase for ClickAny. Button prompts should be defined by worksheet key.
        string fallback = !string.IsNullOrWhiteSpace(korean) ? korean :
            (step.Advance != null && step.Advance.Equals("ClickAny", StringComparison.OrdinalIgnoreCase)
                ? label.text : string.Empty);
        label.text = NormalizeLineBreaks(GameLocalization.Get(key, fallback));
    }

    private void ShowDialogue(TutorialData step)
    {
        HideUi();
        if (dialogueInputBlocker != null) dialogueInputBlocker.SetActive(true);
        if (dialogText != null)
        {
            dialogText.maxVisibleCharacters = int.MaxValue;
            dialogText.text = Localize(step);
            LocaleTMPFontGuard.Prepare(dialogText);
            dialogText.ForceMeshUpdate();
            dialogText.maxVisibleCharacters = 0;
        }
        if (portrait != null)
        {
            Sprite sprite = null;
            bool has = DataManager.Instance?.CharacterIconDatabase?.TryGetIcon(step.SpeakerId, out sprite) == true;
            portrait.sprite = has ? sprite : null;
            portrait.enabled = has;
        }
        if (dialogRoot != null) dialogRoot.SetActive(true);
        SetCompletionPrompt(step, dialogCompleteLabel);
        SetComplete(dialogTextComplete, false);
    }

    private void ShowGuide(TutorialData step, bool keepExistingFocus)
    {
        // Consecutive guides for the same UI target should keep the existing
        // FocusOverlay and HighlightFrame instead of reopening their animation.
        if (keepExistingFocus && overlayRoot != null && overlayRoot.gameObject.activeSelf)
        {
            if (dialogRoot != null) dialogRoot.SetActive(false);
            if (tooltipRoot != null) tooltipRoot.SetActive(false);
            ResetComplete(tooltipTextComplete);
        }
        else
        {
            HideUi();
            keepExistingFocus = false;
        }
        if (guideText != null) guideText.text = Localize(step);
        if (tooltipRoot != null) tooltipRoot.SetActive(true);
        tooltipLayoutDirty = true;
        // Tooltip displays the whole message immediately.
        SetComplete(tooltipTextComplete, true);
        SetCompletionPrompt(step, tooltipCompleteLabel);
        if (focusRoot == null || string.IsNullOrWhiteSpace(step.HighlightTarget)) return;
        Transform target = null;
        foreach (Transform item in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (item.gameObject.scene.IsValid() && item.gameObject.activeInHierarchy &&
                MatchesHighlightTarget(item, step.HighlightTarget))
            {
                target = item;
                break;
            }
        }
        // RewardSlot is instantiated as RewardSlot(Clone) under Contant at runtime.
        // Resolve the active slot instead of falling back to an unrelated button.
        if (IsFinalTutorialRunning && step.Order == 4)
        {
            // 런타임 이름의 일련번호가 아니라 몬스터 데이터 ID로 드라우그를 찾습니다.
            MonsterUnit draugr = FindFinalTutorialDraugr();
            if (draugr != null) target = draugr.transform;
            else Debug.LogWarning("[Tutorial_03] 드라우그(Mon_07)를 찾을 수 없습니다.", this);
        }
        if (IsFinalTutorialRunning && (step.Order == 18 || step.Order == 23))
        {
            // The actual Char_Select entries are custom pointer targets, not Buttons.
            // Use the same resolved object for focus and the click completion gate.
            target = FindFinalCharacterSelectTarget(step.Order == 18 ? "Char02" : "Char01");
        }
        if (IsFinalTutorialRunning && step.Order == 10)
        {
            string targetCharacterId = "Char_03";
            // Runtime prefab names/ordering can change. Resolve by character database ID.
            target = null;
            foreach (BattleCharacter actor in FindObjectsByType<BattleCharacter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (actor.RuntimeData?.CharacterId == targetCharacterId) { target = actor.transform; break; }
        }
        if (continuationMode && step.Order == 33)
        {
            BattleRewardPanelUI rewardPanel = FindFirstObjectByType<BattleRewardPanelUI>(FindObjectsInactive.Exclude);
            BattleRewardSlotUI rewardSlot = rewardPanel != null
                ? rewardPanel.GetComponentInChildren<BattleRewardSlotUI>(false) : null;
            if (rewardSlot != null) target = rewardSlot.transform;
        }
        if (target == null && continuationMode && step.Order == 36)
        {
            BattleRewardRelicPanelUI relicPanel = FindFirstObjectByType<BattleRewardRelicPanelUI>(FindObjectsInactive.Exclude);
            if (relicPanel != null)
            {
                Transform relicContent = relicPanel.transform.Find("content") ?? relicPanel.transform.Find("Contant") ?? relicPanel.transform.Find("Content");
                if (relicContent != null)
                    target = relicContent.Find("Char1") ?? relicContent.Find("Char01");
                if (target == null)
                {
                    foreach (Transform child in relicPanel.GetComponentsInChildren<Transform>(true))
                        if (child.name == "Char1" || child.name == "Char01") { target = child; break; }
                }
            }
        }
        // Map_28 / step 5 focuses the first selectable event slot, not the
        // scroll view container. Accept the existing Excel target as an alias.
        if (continuationMode && step.Order == 5 &&
            string.Equals(step.MapId, "Map_28", StringComparison.OrdinalIgnoreCase))
        {
            EventRoomController eventRoom = FindFirstObjectByType<EventRoomController>(FindObjectsInactive.Exclude);
            if (eventRoom != null)
            {
                foreach (EventChoiceSlotUI slot in eventRoom.GetComponentsInChildren<EventChoiceSlotUI>(true))
                {
                    if (slot != null && slot.gameObject.activeInHierarchy &&
                        slot.name.Equals("EventChoiceSlot_1", StringComparison.OrdinalIgnoreCase))
                    {
                        target = slot.transform;
                        break;
                    }
                }
            }
        }
        RectTransform source = target as RectTransform;
        if (IsFinalTutorialRunning && (step.Order == 5 || step.Order == 6) && target != null)
            source = CreateCompositeUiFocusProxy(target) ?? source;
        if (source == null && target != null)
        {
            // 최종 튜토리얼의 몬스터/캐릭터는 이미지가 아닌 클릭용 2D 박스 콜라이더를 강조합니다.
            bool useColliderFocus = IsFinalTutorialRunning && (step.Order == 4 || step.Order == 10);
            source = useColliderFocus
                ? CreateWorldBoxColliderFocusProxy(target)
                : CreateWorldGridFocusProxy(target);
        }
        if (source == null && continuationMode)
            Debug.LogWarning($"[TutorialBattleSequence] Focus target not found: {step.HighlightTarget} (Order {step.Order})", this);
        if (source != null && focusRoot.transform is RectTransform frame)
        {
            RectTransform parent = frame.parent as RectTransform;
            if (parent != null)
            {
                Vector3[] corners = new Vector3[4];
                source.GetWorldCorners(corners);
                Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
                Vector2 max = new Vector2(float.MinValue, float.MinValue);
                foreach (Vector3 corner in corners)
                {
                    Vector2 local = parent.InverseTransformPoint(corner);
                    min = Vector2.Min(min, local);
                    max = Vector2.Max(max, local);
                }
                frame.anchorMin = frame.anchorMax = new Vector2(.5f, .5f);
                frame.anchoredPosition = (min + max) * .5f - parent.rect.center;
                frame.sizeDelta = max - min + Vector2.one * 16f;
                UpdateHighlightCorners(frame);
                focusRoot.SetActive(true);
                if (!keepExistingFocus) ShowFocusOverlay(source);
                // Excel TooltipPosition controls guide placement; blank defaults to Below.
                bool isAbove = string.Equals(step.TooltipPosition?.Trim(), "Above", StringComparison.OrdinalIgnoreCase);
                if (isAbove)
                    PositionTooltipAbove(source);
                else
                    PositionTooltipBelow(source);
                KeepTooltipInsideScreen();
            }
        }
    }

    // The final message has no interactive target: dim the whole battle view and
    // place its tooltip at screen center instead of leaving a highlight cutout.
    private void ShowFinalTutorialCompletionOverlay()
    {
        if (focusSlideRoutine != null)
        {
            StopCoroutine(focusSlideRoutine);
            focusSlideRoutine = null;
        }
        if (focusRoot != null) focusRoot.SetActive(false);
        if (overlayRoot != null)
        {
            RectTransform top = Find(overlayRoot, "Top") as RectTransform;
            RectTransform bottom = Find(overlayRoot, "Bottom") as RectTransform;
            RectTransform left = Find(overlayRoot, "Left") as RectTransform;
            RectTransform right = Find(overlayRoot, "Right") as RectTransform;
            if (top != null)
            {
                SetOverlayPanel(top, overlayRoot.rect);
                overlayRoot.gameObject.SetActive(true);
                top.gameObject.SetActive(true);
                if (bottom != null) bottom.gameObject.SetActive(false);
                if (left != null) left.gameObject.SetActive(false);
                if (right != null) right.gameObject.SetActive(false);
            }
        }
        RectTransform tooltip = tooltipRoot != null ? tooltipRoot.transform as RectTransform : null;
        if (tooltip != null)
        {
            tooltip.anchorMin = tooltip.anchorMax = new Vector2(.5f, .5f);
            tooltip.pivot = new Vector2(.5f, .5f);
            tooltip.anchoredPosition = Vector2.zero;
        }
    }

    // 몬스터 정보 목록은 Content 컨테이너 전체 너비가 아닌 실제 표시 프리팹 너비로 강조합니다.
    // 높이는 제목(Name)부터 마지막 표시 항목까지 포함하되, 가로 범위는 항목 프리팹으로만 정합니다.
    private RectTransform CreateCompositeUiFocusProxy(Transform group)
    {
        RectTransform frame = focusRoot != null ? focusRoot.transform as RectTransform : null;
        RectTransform parent = frame != null ? frame.parent as RectTransform : null;
        if (parent == null || group == null) return null;

        Transform content = group.Find("Contant") ?? group.Find("Content") ?? group.Find("content");
        Transform heading = group.Find("Name");
        Canvas groupCanvas = group.GetComponentInParent<Canvas>();
        Camera groupCamera = groupCanvas != null && groupCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? groupCanvas.worldCamera : null;
        Canvas parentCanvas = parent.GetComponentInParent<Canvas>();
        Camera parentCamera = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? parentCanvas.worldCamera : null;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        bool hasEntry = false;

        // Only the actual item roots set X bounds, never the stretched Content/SkillList rect.
        if (content != null)
        {
            foreach (Transform child in content)
            {
                RectTransform rect = child as RectTransform;
                if (rect == null || !rect.gameObject.activeInHierarchy ||
                    rect.rect.width <= 0f || rect.rect.height <= 0f) continue;
                Vector3[] corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                foreach (Vector3 corner in corners)
                {
                    Vector2 screen = RectTransformUtility.WorldToScreenPoint(groupCamera, corner);
                    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            parent, screen, parentCamera, out Vector2 local)) continue;
                    minX = Mathf.Min(minX, local.x);
                    maxX = Mathf.Max(maxX, local.x);
                    minY = Mathf.Min(minY, local.y);
                    maxY = Mathf.Max(maxY, local.y);
                    hasEntry = true;
                }
            }
        }
        if (!hasEntry) return null;

        // Include the title vertically, without allowing its potentially stretched width to enlarge X.
        if (heading is RectTransform titleRect && heading.gameObject.activeInHierarchy)
        {
            Vector3[] corners = new Vector3[4];
            titleRect.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(groupCamera, corner);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        parent, screen, parentCamera, out Vector2 local)) continue;
                minY = Mathf.Min(minY, local.y);
                maxY = Mathf.Max(maxY, local.y);
            }
        }

        if (compositeUiFocusProxy == null)
        {
            GameObject proxy = new GameObject("TutorialMonsterInfoGroupFocus", typeof(RectTransform));
            compositeUiFocusProxy = proxy.GetComponent<RectTransform>();
            compositeUiFocusProxy.SetParent(parent, false);
        }
        Vector2 min = new Vector2(minX, minY);
        Vector2 max = new Vector2(maxX, maxY);
        compositeUiFocusProxy.anchorMin = compositeUiFocusProxy.anchorMax = new Vector2(.5f, .5f);
        compositeUiFocusProxy.pivot = new Vector2(.5f, .5f);
        compositeUiFocusProxy.anchoredPosition = (min + max) * .5f - parent.rect.center;
        compositeUiFocusProxy.sizeDelta = Vector2.Max(new Vector2(16f, 16f), max - min);
        return compositeUiFocusProxy;
    }

    // 최종 전투의 드라우그와 헤이즈는 스프라이트가 아닌 BoxCollider2D의 실제 영역을 강조합니다.
    // 콜라이더 로컬 모서리를 월드/화면 좌표로 변환하므로 Offset, Scale, Rotation도 반영됩니다.
    private RectTransform CreateWorldBoxColliderFocusProxy(Transform worldTarget)
    {
        RectTransform frame = focusRoot != null ? focusRoot.transform as RectTransform : null;
        RectTransform parent = frame != null ? frame.parent as RectTransform : null;
        BoxCollider2D box = worldTarget != null ? worldTarget.GetComponent<BoxCollider2D>() : null;
        if (box == null && worldTarget != null)
            box = worldTarget.GetComponentInChildren<BoxCollider2D>(true);
        Camera camera = Camera.main;
        if (parent == null || box == null || camera == null)
        {
            Debug.LogWarning($"[TutorialBattleSequence] BoxCollider2D focus unavailable: {worldTarget?.name}", this);
            return null;
        }

        Canvas canvas = parent.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera : null;
        Vector2 halfSize = box.size * .5f;
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            {
                Vector3 localCorner = new Vector3(box.offset.x + halfSize.x * x,
                    box.offset.y + halfSize.y * y, 0f);
                Vector3 worldCorner = box.transform.TransformPoint(localCorner);
                Vector3 screen = camera.WorldToScreenPoint(worldCorner);
                if (screen.z <= 0f) continue;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, uiCamera, out Vector2 local))
                    continue;
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }
        if (min.x == float.MaxValue) return null;
        if (worldGridFocusProxy == null)
        {
            GameObject proxy = new GameObject("TutorialWorldGridFocusTarget", typeof(RectTransform));
            worldGridFocusProxy = proxy.GetComponent<RectTransform>();
            worldGridFocusProxy.SetParent(parent, false);
        }
        worldGridFocusProxy.anchorMin = worldGridFocusProxy.anchorMax = new Vector2(.5f, .5f);
        worldGridFocusProxy.pivot = new Vector2(.5f, .5f);
        worldGridFocusProxy.anchoredPosition = (min + max) * .5f - parent.rect.center;
        worldGridFocusProxy.sizeDelta = new Vector2(Mathf.Max(12f, max.x - min.x), Mathf.Max(12f, max.y - min.y));
        return worldGridFocusProxy;
    }

    // MeshRenderer 기반 월드 그리드 좌표를 화면 안내용 UI RectTransform으로 변환합니다.
    private RectTransform CreateWorldGridFocusProxy(Transform worldTarget)
    {
        RectTransform frame = focusRoot != null ? focusRoot.transform as RectTransform : null;
        RectTransform parent = frame != null ? frame.parent as RectTransform : null;
        Renderer renderer = worldTarget != null ? worldTarget.GetComponent<Renderer>() : null;
        if (renderer == null && worldTarget != null)
            renderer = worldTarget.GetComponentInChildren<Renderer>(true);
        Camera camera = Camera.main;
        if (parent == null || renderer == null || camera == null)
        {
            Debug.LogWarning("[TutorialBattleSequence] Grid focus requires a renderer, camera and UI parent.", this);
            return null;
        }

        Bounds bounds = renderer.bounds;
        Canvas canvas = parent.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera : null;
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 point = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                    Vector3 screen = camera.WorldToScreenPoint(point);
                    if (screen.z <= 0f) continue;
                    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, uiCamera, out Vector2 local))
                        continue;
                    min = Vector2.Min(min, local);
                    max = Vector2.Max(max, local);
                }
        if (min.x == float.MaxValue) return null;
        if (worldGridFocusProxy == null)
        {
            GameObject proxy = new GameObject("TutorialWorldGridFocusTarget", typeof(RectTransform));
            worldGridFocusProxy = proxy.GetComponent<RectTransform>();
            worldGridFocusProxy.SetParent(parent, false);
        }
        worldGridFocusProxy.anchorMin = worldGridFocusProxy.anchorMax = new Vector2(.5f, .5f);
        worldGridFocusProxy.pivot = new Vector2(.5f, .5f);
        worldGridFocusProxy.anchoredPosition = (min + max) * .5f - parent.rect.center;
        worldGridFocusProxy.sizeDelta = new Vector2(Mathf.Max(12f, max.x - min.x), Mathf.Max(12f, max.y - min.y));
        return worldGridFocusProxy;
    }

    // 몬스터 생성 순서에 따라 RuntimeMonster 번호가 바뀌어도 동일한 드라우그를 선택합니다.
    private static bool IsFinalTutorialDraugr(MonsterUnit monster)
    {
        return monster != null && monster.RuntimeData != null &&
            string.Equals(monster.RuntimeData.MonsterId, "Mon_07", StringComparison.OrdinalIgnoreCase);
    }

    private static MonsterUnit FindFinalTutorialDraugr()
    {
        foreach (MonsterUnit monster in FindObjectsByType<MonsterUnit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (IsFinalTutorialDraugr(monster)) return monster;
        return null;
    }

    private static bool MatchesHighlightTarget(Transform item, string key)
    {
        if (item == null || string.IsNullOrWhiteSpace(key)) return false;
        string normalized = key.Replace('\\', '/').Trim('/');
        if (!normalized.Contains("/")) return item.name.Equals(normalized, StringComparison.OrdinalIgnoreCase);
        string[] segments = normalized.Split('/');
        Transform current = item;
        for (int i = segments.Length - 1; i >= 0; --i)
        {
            if (current == null) return false;
            string currentName = current.name;
            if (currentName.EndsWith("(Clone)", StringComparison.OrdinalIgnoreCase))
                currentName = currentName.Substring(0, currentName.Length - "(Clone)".Length);
            if (!currentName.Equals(segments[i], StringComparison.OrdinalIgnoreCase)) return false;
            current = current.parent;
        }
        return true;
    }

    private void PositionTooltipAbove(RectTransform source)
    {
        if (tooltipRoot == null || source == null) return;
        RectTransform tooltip = tooltipRoot.transform as RectTransform;
        RectTransform parent = tooltip != null ? tooltip.parent as RectTransform : null;
        if (parent == null) return;
        Canvas sourceCanvas = source.GetComponentInParent<Canvas>();
        Camera sourceCamera = sourceCanvas != null && sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? sourceCanvas.worldCamera : null;
        Canvas tooltipCanvas = tooltip.GetComponentInParent<Canvas>();
        Camera tooltipCamera = tooltipCanvas != null && tooltipCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? tooltipCanvas.worldCamera : null;
        Vector3[] corners = new Vector3[4];
        source.GetWorldCorners(corners);
        Vector3 topMiddle = (corners[1] + corners[2]) * .5f;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(sourceCamera, topMiddle);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, tooltipCamera, out Vector2 local)) return;
        const float gap = 12f;
        tooltip.anchorMin = tooltip.anchorMax = new Vector2(.5f, .5f);
        tooltip.pivot = new Vector2(.5f, 0f);
        tooltip.anchoredPosition = local - parent.rect.center + Vector2.up * gap;
    }

    private void LateUpdate()
    {
        if (IsPresentationRunning &&
            tooltipRoot != null &&
            tooltipRoot.activeInHierarchy &&
            TutorialPresentationRefreshPolicy.ShouldRefreshTooltip(
                tooltipLayoutDirty,
                lastTooltipScreenWidth,
                lastTooltipScreenHeight,
                Screen.width,
                Screen.height))
        {
            KeepTooltipInsideScreen();
        }
    }

    // 현재 화면 좌표를 캔버스 크기에 맞게 변환하여 위치를 계산합니다.
    private void KeepTooltipInsideScreen()
    {
        RectTransform tooltip = tooltipRoot != null ? tooltipRoot.transform as RectTransform : null;
        RectTransform parent = tooltip != null ? tooltip.parent as RectTransform : null;
        if (tooltip == null || parent == null) return;

        lastTooltipScreenWidth = Screen.width;
        lastTooltipScreenHeight = Screen.height;
        tooltipLayoutDirty = false;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltip);
        Canvas.ForceUpdateCanvases();
        Canvas canvas = tooltip.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.rootCanvas.worldCamera : null;
        Vector3[] corners = new Vector3[4];
        tooltip.GetWorldCorners(corners);
        float left = float.MaxValue, right = float.MinValue;
        float bottom = float.MaxValue, top = float.MinValue;
        foreach (Vector3 corner in corners)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, corner);
            left = Mathf.Min(left, screenPoint.x);
            right = Mathf.Max(right, screenPoint.x);
            bottom = Mathf.Min(bottom, screenPoint.y);
            top = Mathf.Max(top, screenPoint.y);
        }
        const float margin = 12f;
        float availableWidth = Mathf.Max(0f, Screen.width - margin * 2f);
        float availableHeight = Mathf.Max(0f, Screen.height - margin * 2f);
        float dx = right - left > availableWidth
            ? Screen.width * 0.5f - (left + right) * 0.5f
            : left < margin ? margin - left : right > Screen.width - margin ? Screen.width - margin - right : 0f;
        float dy = top - bottom > availableHeight
            ? Screen.height * 0.5f - (bottom + top) * 0.5f
            : bottom < margin ? margin - bottom : top > Screen.height - margin ? Screen.height - margin - top : 0f;
        if (Mathf.Abs(dx) < 0.01f && Mathf.Abs(dy) < 0.01f) return;
        Vector2 originScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, tooltip.position);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, originScreen, uiCamera, out Vector2 originLocal) &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, originScreen + new Vector2(dx, dy), uiCamera, out Vector2 shiftedLocal))
            tooltip.anchoredPosition += shiftedLocal - originLocal;
    }

    // The Line sprite contains four fixed-size corner marks. Do not stretch it:
    // crop each original corner into a separate UI mask and place the masks on
    // the corresponding HighlightFrame corners. This preserves sprite pixels.
    private static void UpdateHighlightCorners(RectTransform frame)
    {
        RectTransform line = Find(frame, "Line") as RectTransform;
        if (line == null) return;
        Image original = line.GetComponent<Image>();
        if (original == null || original.sprite == null) return;

        // Use the authored dimensions, not the (possibly stretched) dimensions.
        // The default Line image is 100 x 100.
        Vector2 originalSize = line.sizeDelta;
        if (line.anchorMin != line.anchorMax || originalSize.x <= 0f || originalSize.y <= 0f)
            originalSize = original.sprite.rect.size / Mathf.Max(1f, original.sprite.pixelsPerUnit);
        if (originalSize.x <= 0f || originalSize.y <= 0f)
            originalSize = new Vector2(100f, 100f);

        // Restore the original image's authored size; it serves only as a
        // template for the four cropped corner images below.
        line.anchorMin = line.anchorMax = new Vector2(.5f, .5f);
        line.pivot = new Vector2(.5f, .5f);
        line.anchoredPosition = Vector2.zero;
        line.sizeDelta = originalSize;
        line.localScale = Vector3.one;
        original.enabled = false;

        string[] names = { "TutorialCorner_TL", "TutorialCorner_TR", "TutorialCorner_BL", "TutorialCorner_BR" };
        Vector2[] pivots = { new Vector2(0f, 1f), new Vector2(1f, 1f),
                             new Vector2(0f, 0f), new Vector2(1f, 0f) };
        for (int i = 0; i < names.Length; i++)
        {
            Vector2 corner = pivots[i];
            Transform existing = frame.Find(names[i]);
            RectTransform mask;
            if (existing == null)
            {
                var maskObject = new GameObject(names[i], typeof(RectTransform), typeof(RectMask2D));
                mask = maskObject.GetComponent<RectTransform>();
                mask.SetParent(frame, false);
                var graphicObject = new GameObject("CornerGraphic", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform graphic = graphicObject.GetComponent<RectTransform>();
                graphic.SetParent(mask, false);
            }
            else mask = existing as RectTransform;
            if (mask == null) continue;

            mask.anchorMin = mask.anchorMax = corner;
            mask.pivot = corner;
            mask.anchoredPosition = Vector2.zero;
            mask.sizeDelta = originalSize * .5f;
            mask.localScale = Vector3.one;
            RectTransform cropped = mask.Find("CornerGraphic") as RectTransform;
            if (cropped == null) continue;
            cropped.anchorMin = cropped.anchorMax = corner;
            cropped.pivot = corner;
            cropped.anchoredPosition = Vector2.zero;
            cropped.sizeDelta = originalSize;
            cropped.localScale = Vector3.one;
            Image graphicImage = cropped.GetComponent<Image>();
            graphicImage.sprite = original.sprite;
            graphicImage.color = original.color;
            graphicImage.material = original.material;
            graphicImage.type = Image.Type.Simple;
            graphicImage.preserveAspect = original.preserveAspect;
            graphicImage.raycastTarget = false;
            mask.gameObject.SetActive(true);
        }
    }

    // The guide is centered beneath the highlighted HUD element (not beneath the screen).
    private void PositionTooltipBelow(RectTransform source)
    {
        if (tooltipRoot == null || source == null) return;
        RectTransform tooltip = tooltipRoot.transform as RectTransform;
        RectTransform parent = tooltip != null ? tooltip.parent as RectTransform : null;
        if (parent == null) return;

        Canvas sourceCanvas = source.GetComponentInParent<Canvas>();
        Camera sourceCamera = sourceCanvas != null && sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? sourceCanvas.worldCamera : null;
        Canvas tooltipCanvas = tooltip.GetComponentInParent<Canvas>();
        Camera tooltipCamera = tooltipCanvas != null && tooltipCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? tooltipCanvas.worldCamera : null;

        Vector3[] corners = new Vector3[4];
        source.GetWorldCorners(corners);
        Vector3 bottomMiddle = (corners[0] + corners[3]) * 0.5f;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(sourceCamera, bottomMiddle);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, tooltipCamera, out Vector2 local))
            return;

        const float gap = 12f;
        tooltip.anchorMin = tooltip.anchorMax = new Vector2(0.5f, 0.5f);
        tooltip.pivot = new Vector2(0.5f, 1f);
        tooltip.anchoredPosition = local - parent.rect.center + Vector2.down * gap;
    }

    // Match the four-panel FocusOverlay used by the existing battle tutorial.
    // The target remains unobscured while the surrounding HUD is dimmed.
    private void ShowFocusOverlay(RectTransform source)
    {
        if (overlayRoot == null || source == null) return;
        RectTransform top = Find(overlayRoot, "Top") as RectTransform;
        RectTransform bottom = Find(overlayRoot, "Bottom") as RectTransform;
        RectTransform left = Find(overlayRoot, "Left") as RectTransform;
        RectTransform right = Find(overlayRoot, "Right") as RectTransform;
        if (top == null || bottom == null || left == null || right == null)
        {
            Debug.LogWarning("[TutorialBattleSequence] FocusOverlay needs Top, Bottom, Left and Right panels.", this);
            return;
        }

        // Convert through screen coordinates to support canvases with different scaling.
        Canvas targetCanvas = source.GetComponentInParent<Canvas>();
        Camera sourceCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? targetCanvas.worldCamera : null;
        Canvas overlayCanvas = overlayRoot.GetComponentInParent<Canvas>();
        Camera overlayCamera = overlayCanvas != null && overlayCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? overlayCanvas.worldCamera : null;
        Vector3[] corners = new Vector3[4];
        source.GetWorldCorners(corners);
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        foreach (Vector3 corner in corners)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(sourceCamera, corner);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRoot, screen, overlayCamera, out Vector2 point))
                return;
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
        const float padding = 8f;
        Rect r = overlayRoot.rect;
        float x0 = Mathf.Clamp(min.x - padding, r.xMin, r.xMax);
        float x1 = Mathf.Clamp(max.x + padding, r.xMin, r.xMax);
        float y0 = Mathf.Clamp(min.y - padding, r.yMin, r.yMax);
        float y1 = Mathf.Clamp(max.y + padding, r.yMin, r.yMax);
        bool wasVisible = overlayRoot.gameObject.activeSelf;
        if (focusSlideRoutine != null)
        {
            StopCoroutine(focusSlideRoutine);
            focusSlideRoutine = null;
        }
        SetOverlayPanel(top, Rect.MinMaxRect(r.xMin, y1, r.xMax, r.yMax));
        SetOverlayPanel(bottom, Rect.MinMaxRect(r.xMin, r.yMin, r.xMax, y0));
        SetOverlayPanel(left, Rect.MinMaxRect(r.xMin, y0, x0, y1));
        SetOverlayPanel(right, Rect.MinMaxRect(x1, y0, r.xMax, y1));
        overlayRoot.gameObject.SetActive(true);
        top.gameObject.SetActive(true);
        bottom.gameObject.SetActive(true);
        left.gameObject.SetActive(true);
        right.gameObject.SetActive(true);

        // Animate only the initial reveal, not each page of the same guide.
        if (!wasVisible && focusSlideDuration > 0f)
        {
            RectTransform[] panels = { top, bottom, left, right };
            Vector2[] finals = { top.anchoredPosition, bottom.anchoredPosition,
                left.anchoredPosition, right.anchoredPosition };
            Vector2[] starts = {
                finals[0] + Vector2.up * r.height,
                finals[1] + Vector2.down * r.height,
                finals[2] + Vector2.left * r.width,
                finals[3] + Vector2.right * r.width
            };
            for (int i = 0; i < panels.Length; i++)
                panels[i].anchoredPosition = starts[i];
            focusSlideRoutine = StartCoroutine(AnimateFocusSlide(panels, starts, finals));
        }
    }

    private IEnumerator AnimateFocusSlide(RectTransform[] panels, Vector2[] starts, Vector2[] finals)
    {
        float elapsed = 0f;
        while (elapsed < focusSlideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, focusSlideDuration));
            t = 1f - Mathf.Pow(1f - t, 3f);
            for (int i = 0; i < panels.Length; i++)
                if (panels[i] != null)
                    panels[i].anchoredPosition = Vector2.LerpUnclamped(starts[i], finals[i], t);
            yield return null;
        }
        for (int i = 0; i < panels.Length; i++)
            if (panels[i] != null) panels[i].anchoredPosition = finals[i];
        focusSlideRoutine = null;
    }

    private static void SetOverlayPanel(RectTransform panel, Rect rect)
    {
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = rect.center;
        panel.sizeDelta = rect.size;
    }

    // First click during typing reveals the whole message; the next click advances.
    private IEnumerator RevealDialogueAndWaitForClick()
    {
        if (dialogText == null)
        {
            SetComplete(dialogTextComplete, true);
            yield return WaitForClick();
            yield break;
        }

        LocaleTMPFontGuard.Prepare(dialogText);
        dialogText.ForceMeshUpdate();
        int totalCharacters = dialogText.textInfo.characterCount;
        int shown = 0;
        float progress = 0f;
        // Prevent the click that opened the dialogue from immediately skipping it.
        yield return null;

        while (shown < totalCharacters)
        {
            if (IsAdvancePressed())
            {
                dialogText.maxVisibleCharacters = int.MaxValue;
                SetComplete(dialogTextComplete, true);
                // Consume this press; require a separate click to advance.
                yield return null;
                yield return WaitForClick();
                yield break;
            }
            progress += Time.unscaledDeltaTime * dialogueCharactersPerSecond;
            int next = Mathf.Min(totalCharacters, Mathf.FloorToInt(progress));
            if (next > shown)
            {
                shown = next;
                dialogText.maxVisibleCharacters = shown;
            }
            yield return null;
        }
        dialogText.maxVisibleCharacters = int.MaxValue;
        SetComplete(dialogTextComplete, true);
        yield return WaitForClick();
    }

    private bool IsFinalExpectedCharacterSelected(BattleTimelineController timeline)
    {
        if (timeline?.SelectedCharacter == null) return false;
        int stage = tutorialStageIndex;
        string expected = stage >= 10 && stage <= 13 ? "Char_03" :
            stage >= 18 && stage <= 22 ? "Char_02" :
            stage >= 23 && stage <= 26 ? "Char_01" : null;
        return expected != null && timeline.SelectedCharacter.CharacterId == expected;
    }

    private static bool HasFinalCommand(BattleTimelineController timeline, int baseline,
        string characterId, Func<PlayerReservedCommand, bool> predicate)
    {
        if (timeline == null || CountCharacterCommands(timeline, characterId) <= baseline) return false;
        for (int i = 0; i < timeline.SlotCount; ++i)
        {
            var commands = timeline.GetPlayerCommands(i);
            if (commands == null) continue;
            foreach (var command in commands)
                if (command != null && command.CharacterId == characterId && predicate(command))
                    return true;
        }
        return false;
    }

    private static Transform FindFinalGuideTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target)) return null;
        foreach (Transform item in Resources.FindObjectsOfTypeAll<Transform>())
            if (item != null && item.gameObject.scene.IsValid() && item.gameObject.activeInHierarchy &&
                MatchesHighlightTarget(item, target)) return item;
        return null;
    }

    private static Transform FindFinalCharacterSelectTarget(string name)
    {
        // Resolve by the actual BattleCharacterPanelUI hierarchy rather than a
        // strict full scene path, which can differ for instantiated UI prefabs.
        foreach (BattleCharacterPanelUI panel in
            FindObjectsByType<BattleCharacterPanelUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (panel == null || !panel.gameObject.scene.IsValid()) continue;
            Transform root = panel.transform.Find("Char_Select");
            Transform target = root != null ? root.Find(name) : null;
            if (target != null && target.gameObject.activeInHierarchy)
                return target;
            foreach (BattleCharacterSelectTarget selection in
                panel.GetComponentsInChildren<BattleCharacterSelectTarget>(true))
                if (selection != null && selection.gameObject.activeInHierarchy &&
                    string.Equals(selection.name, name, StringComparison.OrdinalIgnoreCase))
                    return selection.transform;
        }
        return null;
    }

    private IEnumerator WaitForFinalTutorialAction(TutorialData step)
    {
        BattleTimelineController timeline = FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);
        PlayerSkillReservationController reservation = FindFirstObjectByType<PlayerSkillReservationController>(FindObjectsInactive.Include);
        BattleTurnExecutor executor = FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);
        string character = step.Order >= 10 && step.Order <= 13 ? "Char_03" :
            step.Order >= 18 && step.Order <= 22 ? "Char_02" : "Char_01";
        int before = CountCharacterCommands(timeline, character);
        int compoundBefore = finalTutorialCompoundUseSerial;
        bool clicked = false;
        Button button = null;
        UnityEngine.Events.UnityAction onClick = () => clicked = true;
        if (step.Order == 4)
        {
            bool monsterSelected = false;
            Action<MonsterUnit> onMonster = unit => { if (IsFinalTutorialDraugr(unit)) monsterSelected = true; };
            MonsterUnit.MonsterInfoSelectionChanged += onMonster;
            while (!monsterSelected) yield return null;
            MonsterUnit.MonsterInfoSelectionChanged -= onMonster;
            yield break;
        }
        if (step.Order == 7)
        {
            Transform buttonTarget = FindFinalGuideTarget(step.HighlightTarget);
            button = buttonTarget != null ?
                buttonTarget.GetComponent<Button>() ?? buttonTarget.GetComponentInChildren<Button>(true) : null;
            if (button == null)
            {
                Debug.LogError("[Tutorial_03] MonsterInfoPanel/BackButton was not found.", this);
                yield break;
            }
            button.onClick.AddListener(onClick);
            while (!clicked) yield return null;
            button.onClick.RemoveListener(onClick);
            yield break;
        }
        if (step.Order == 10)
        {
            // Haze is selected by clicking her world BoxCollider2D.
            int clickBaseline = finalTutorialWorldClickSerial;
            while (finalTutorialWorldClickSerial <= clickBaseline ||
                   !string.Equals(finalTutorialWorldClickCharacterId, "Char_03", StringComparison.Ordinal) ||
                   !IsFinalExpectedCharacterSelected(timeline))
                yield return null;
            yield break;
        }
        if (step.Order == 18 || step.Order == 23)
        {
            // Char_Select entries use BattleCharacterSelectTarget (IPointerClickHandler),
            // not UnityEngine.UI.Button. Never skip the tutorial when lookup fails.
            string expectedId = step.Order == 18 ? "Char_02" : "Char_01";
            string expectedName = step.Order == 18 ? "Char02" : "Char01";
            TutorialCharacterSelectionClickRelay relay = null;
            bool selectionClicked = false;
            Action onSelection = () => selectionClicked = true;
            bool warned = false;
            while (true)
            {
                if (relay == null)
                {
                    Transform selection = FindFinalCharacterSelectTarget(expectedName);
                    if (selection == null)
                    {
                        if (!warned)
                        {
                            Debug.LogWarning($"[Tutorial_03] Waiting for character selection target at order {step.Order}: {expectedName}", this);
                            warned = true;
                        }
                        yield return null;
                        continue;
                    }
                    relay = selection.GetComponent<TutorialCharacterSelectionClickRelay>();
                    if (relay == null)
                        relay = selection.gameObject.AddComponent<TutorialCharacterSelectionClickRelay>();
                    relay.Clicked += onSelection;
                }
                // The explicit click must occur during this stage AND actually select
                // the expected character. Merely being selected is not enough.
                if (selectionClicked &&
                    string.Equals(timeline?.SelectedCharacter?.CharacterId, expectedId, StringComparison.Ordinal))
                    break;
                if (selectionClicked) selectionClicked = false;
                yield return null;
            }
            if (relay != null) relay.Clicked -= onSelection;
            yield break;
        }
        if (step.Order == 27)
        {
            tutorialEndTurnClicked = false;
            tutorialEndTurnButton = executor != null ? executor.EndTurnButton : null;
            if (tutorialEndTurnButton != null)
                tutorialEndTurnButton.onClick.AddListener(OnTutorialEndTurnClicked);
            while (!tutorialEndTurnClicked && !Input.GetKeyDown(KeyCode.Space) &&
                   (executor == null || !executor.IsExecuting)) yield return null;
            if (tutorialEndTurnButton != null)
            {
                tutorialEndTurnButton.onClick.RemoveListener(OnTutorialEndTurnClicked);
                tutorialEndTurnButton = null;
            }
            yield break;
        }
        if (step.Order == 19 || step.Order == 24)
        {
            while (reservation == null || !reservation.IsMoveSkillSelectionActive()) yield return null;
            yield break;
        }
        if (step.Order == 11)
        {
            while (finalTutorialCompoundUseSerial == compoundBefore) yield return null;
            yield break;
        }
        if (step.Order == 20 || step.Order == 25)
        {
            int grid = step.Order == 20 ? 32 : 17;
            while (!HasFinalCommand(timeline, before, character,
                c => c.ReservedMoveGridIndex == grid)) yield return null;
            yield break;
        }
        if (step.Order == 21)
        {
            while (!HasFinalCommand(timeline, before, character,
                c => c.IsFacingFlipCommand)) yield return null;
            yield break;
        }
        if (step.Order == 12 || step.Order == 13 || step.Order == 22 || step.Order == 26)
        {
            bool ultimate = step.Order == 13;
            while (!HasFinalCommand(timeline, before, character,
                c => c.SkillId == (ultimate ? c.UserRuntime?.UniqueSkillId : c.UserRuntime?.AbilitySkillId)))
                yield return null;
        }
    }

    // Advance values are validated against the actual selection/reservation state,
    // not against an arbitrary click or a tooltip-dismiss button.
    private IEnumerator WaitForRequiredBattleAction(TutorialData step)
    {
        if (IsFinalTutorialRunning)
        {
            yield return WaitForFinalTutorialAction(step);
            yield break;
        }
        BattleTimelineController timeline = FindFirstObjectByType<BattleTimelineController>(FindObjectsInactive.Include);
        PlayerSkillReservationController reservation = FindFirstObjectByType<PlayerSkillReservationController>(FindObjectsInactive.Include);
        string advance = step.Advance ?? string.Empty;
        int baseline = CountReservedPlayerCommands(timeline);
        int kayaBaseline = CountCharacterCommands(timeline, "Char_02");
        int hiltBaseline = CountCharacterCommands(timeline, "Char_01");
        if (step.Order == 26) skill26ReservationBaseline = hiltBaseline;
        if (step.Order == 28) skill28ReservationBaseline = kayaBaseline;
        bool moveButton = step.Order == 24;
        bool kayaSelect = step.Order == 27;
        bool endTurn = step.Order == 29;
        tutorialEndTurnClicked = false;
        if (endTurn)
        {
            if (tutorialTurnExecutor == null)
                tutorialTurnExecutor = FindFirstObjectByType<BattleTurnExecutor>(FindObjectsInactive.Include);
            tutorialEndTurnButton = tutorialTurnExecutor != null ? tutorialTurnExecutor.EndTurnButton : null;
            if (tutorialEndTurnButton != null)
                tutorialEndTurnButton.onClick.AddListener(OnTutorialEndTurnClicked);
        }
        yield return null;
        while (true)
        {
            if (endTurn)
            {
                if (tutorialEndTurnClicked || Input.GetKeyDown(KeyCode.Space)) break;
            }
            else if (kayaSelect)
            {
                if (timeline != null && timeline.SelectedCharacter != null &&
                    timeline.SelectedCharacter.CharacterId == "Char_02") break;
            }
            else if (advance.Equals("ReserveActionTwice", StringComparison.OrdinalIgnoreCase))
            {
                if (timeline != null && timeline.SelectedCharacter != null &&
                    timeline.SelectedCharacter.CharacterId == "Char_02" &&
                    CountCharacterCommands(timeline, "Char_02") >= kayaBaseline + 2) break;
            }
            else if (advance.Equals("ClickTarget", StringComparison.OrdinalIgnoreCase))
            {
                if (moveButton && reservation != null && reservation.IsMoveSkillSelectionActive()) break;
            }
            else if (advance.Equals("ReserveMove", StringComparison.OrdinalIgnoreCase))
            {
                if (HasNewMoveToGrid(timeline, baseline, 16) &&
                    (reservation == null || !reservation.IsMoveSkillSelectionActive())) break;
            }
            else if (advance.Equals("ReserveAction", StringComparison.OrdinalIgnoreCase))
            {
                if (CountCharacterCommands(timeline, "Char_01") > hiltBaseline) break;
            }
            yield return null;
        }
        if (tutorialEndTurnButton != null)
        {
            tutorialEndTurnButton.onClick.RemoveListener(OnTutorialEndTurnClicked);
            tutorialEndTurnButton = null;
        }
    }

    private void OnTutorialEndTurnClicked() => tutorialEndTurnClicked = true;

    private static bool HasNewMoveToGrid(BattleTimelineController timeline, int baseline, int gridIndex)
    {
        if (timeline == null || CountReservedPlayerCommands(timeline) <= baseline) return false;
        for (int i = 0; i < timeline.SlotCount; i++)
        {
            var commands = timeline.GetPlayerCommands(i);
            if (commands == null) continue;
            foreach (var command in commands)
                if (command != null && command.CharacterId == "Char_01" && command.ReservedMoveGridIndex == gridIndex)
                    return true;
        }
        return false;
    }

    private static int CountCharacterCommands(BattleTimelineController timeline, string characterId)
    {
        if (timeline == null) return 0;
        int count = 0;
        for (int i = 0; i < timeline.SlotCount; i++)
        {
            var commands = timeline.GetPlayerCommands(i);
            if (commands == null) continue;
            foreach (var command in commands)
                if (command != null && command.CharacterId == characterId) count++;
        }
        return count;
    }

    private static int CountReservedPlayerCommands(BattleTimelineController timeline)
    {
        if (timeline == null) return 0;
        int count = 0;
        for (int i = 0; i < timeline.SlotCount; i++)
        {
            var commands = timeline.GetPlayerCommands(i);
            if (commands != null) count += commands.Count;
        }
        return count;
    }

    // 확인창을 닫는 클릭을 튜토리얼 다음 단계 입력으로 처리하지 않습니다.
    // 마우스 버튼이 해제될 때까지 입력을 차단합니다.
    private static bool suppressAdvanceUntilRelease;
    private static int suppressAdvanceThroughFrame = -1;
    private static readonly List<RaycastResult> advancePointerHits = new List<RaycastResult>();

    public static void NotifyModalInteraction()
    {
        suppressAdvanceUntilRelease = true;
        suppressAdvanceThroughFrame = Mathf.Max(suppressAdvanceThroughFrame, Time.frameCount + 1);
    }

    private static bool IsAdvancePressed()
    {
        bool pointerHeld = Input.GetMouseButton(0) || Input.touchCount > 0;
        if (suppressAdvanceUntilRelease)
        {
            if (!pointerHeld)
            {
                suppressAdvanceUntilRelease = false;
                suppressAdvanceThroughFrame = Mathf.Max(suppressAdvanceThroughFrame, Time.frameCount + 1);
            }
            return false;
        }

        if (Time.frameCount <= suppressAdvanceThroughFrame)
            return false;

        // 포커스 안내가 끝나기 전에는 확인창 클릭이 다음 단계 대화를 진행하지 않게 합니다.
        if (UIManager.Instance != null && UIManager.Instance.IsConfirmDialogOpen)
            return false;

        // Opening or operating the menu never counts as a tutorial advance click.
        BattleMenuEscapeInputController menuController =
            FindFirstObjectByType<BattleMenuEscapeInputController>(FindObjectsInactive.Include);
        if (menuController != null && menuController.IsMenuPanelVisible)
            return false;

        bool pressed = Input.GetMouseButtonDown(0) ||
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
        if (!pressed)
            return false;

        // Button.onClick이 Update보다 먼저 실행될 수 있으므로 입력 순서를 고려합니다.
        // 클릭 입력이 튜토리얼의 다음 단계로 중복 전달되지 않도록 방지합니다.
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            Vector2 position = Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
            var pointerData = new PointerEventData(eventSystem) { position = position };
            advancePointerHits.Clear();
            eventSystem.RaycastAll(pointerData, advancePointerHits);
            foreach (RaycastResult hit in advancePointerHits)
            {
                Transform target = hit.gameObject != null ? hit.gameObject.transform : null;
                while (target != null)
                {
                    string targetName = target.name;
                    if (targetName == "SkipButton" || targetName == "Check(Clone)" ||
                        targetName == "TutorialSkipButton" || targetName == "MenuButton" ||
                        targetName == "MenuPanel" || targetName == "MenuRoot")
                    {
                        NotifyModalInteraction();
                        return false;
                    }
                    target = target.parent;
                }
            }
        }
        return true;
    }

    private IEnumerator WaitForClick()
    {
        // Avoid advancing on the same click that opened this step.
        yield return null;
        while (!IsAdvancePressed())
            yield return null;

        PlayDialogueAdvanceSound();
    }

    private void PlayDialogueAdvanceSound()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(
            ResolveDialogueAdvanceSoundId(dialogueAdvanceSoundId),
            dialogueAdvanceSoundVolume);
    }

    public static string ResolveDialogueAdvanceSoundId(string serializedSoundId)
    {
        return string.IsNullOrWhiteSpace(serializedSoundId)
            ? AudioIds.Sfx.TutorialTextAdvance
            : serializedSoundId.Trim();
    }

    // HUD 준비 신호만으로는 그리드 표시가 완료됐다고 판단하지 않습니다.
    // GridManager 및 GridCell의 준비 상태를 함께 확인합니다.
    private IEnumerator WaitUntilGridRendered()
    {
        while (true)
        {
            GridManager grid = FindFirstObjectByType<GridManager>(FindObjectsInactive.Exclude);
            bool ready = grid != null && grid.IsGridVisible;
            if (ready)
            {
                GridCell[] cells = grid.GetComponentsInChildren<GridCell>(false);
                ready = cells.Length > 0;
                int visibleCount = 0;
                foreach (GridCell cell in cells)
                {
                    if (cell == null || !cell.gameObject.activeInHierarchy) continue;
                    Renderer[] renderers = cell.GetComponentsInChildren<Renderer>(false);
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                        {
                            visibleCount++;
                            break;
                        }
                    }
                }
                ready = ready && visibleCount > 0;
            }
            if (ready)
            {
                // 필요한 그리드와 UI가 준비될 때까지 한 프레임씩 대기합니다.
                yield return new WaitForEndOfFrame();
                yield break;
            }
            yield return null;
        }
    }

    // HUD가 입력을 차단할 수 있으므로 캔버스의 기존 상태를 확인합니다.
    // 해당 컴포넌트의 원래 활성 상태는 대화가 끝난 뒤 복원합니다.
    private void SetBattleUiPointerBlocked(bool blocked)
    {
        if (!blocked)
        {
            foreach (var pair in blockedBattleRaycasters)
                if (pair.Key != null) pair.Key.enabled = pair.Value;
            blockedBattleRaycasters.Clear();
            foreach (var pair in blockedGridColliders)
                if (pair.Key != null) pair.Key.enabled = pair.Value;
            blockedGridColliders.Clear();
            foreach (var pair in blockedGridColliders2D)
                if (pair.Key != null) pair.Key.enabled = pair.Value;
            blockedGridColliders2D.Clear();
            return;
        }

        if (blockedBattleRaycasters.Count > 0) return;
        foreach (GraphicRaycaster raycaster in FindObjectsByType<GraphicRaycaster>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (raycaster == null || !raycaster.enabled) continue;
            Transform t = raycaster.transform;
            bool battleUi = false;
            while (t != null)
            {
                if (t.name == "BattleHUDCanvas" || t.name == "BattleCharacterPanel" || t.name == "BattleSlot")
                {
                    battleUi = true;
                    break;
                }
                t = t.parent;
            }
            if (!battleUi) continue;
            blockedBattleRaycasters[raycaster] = raycaster.enabled;
            raycaster.enabled = false;
        }
        GridManager grid = FindFirstObjectByType<GridManager>(FindObjectsInactive.Exclude);
        if (grid == null) return;
        foreach (Collider collider in grid.GetComponentsInChildren<Collider>(false))
        {
            blockedGridColliders[collider] = collider.enabled;
            collider.enabled = false;
        }
        foreach (Collider2D collider in grid.GetComponentsInChildren<Collider2D>(false))
        {
            blockedGridColliders2D[collider] = collider.enabled;
            collider.enabled = false;
        }
    }

    private void OnDisable()
    {
        RestoreTutorialButtonRaycasts();
        if (tutorialEndTurnButton != null)
        {
            tutorialEndTurnButton.onClick.RemoveListener(OnTutorialEndTurnClicked);
            tutorialEndTurnButton = null;
        }
        RestoreTutorialButtonRaycasts();
        if (activeTutorialRunner == this)
        {
            activeTutorialRunner = null;
            IsPresentationRunning = false;
            IsBlockingBattleStart = false;
            activeBattleTutorialMapId = string.Empty;
        }
        if (continuationMode)
        {
            IsContinuationActive = false;
            CurrentContinuationOrder = 0;
            continuationMode = false;
        }
        RestoreGridVisuals();
        HideUi();
        SetBattleUiPointerBlocked(false);
        ReleaseDialogueInputBlocker();
    }

    private void CreateDialogueInputBlocker(Transform tutorialRoot)
    {
        if (dialogueInputBlocker != null || tutorialRoot == null) return;
        // The transparent screen sits over the battle UI but below the tutorial dialogue.
        dialogueInputBlocker = new GameObject("TutorialDialogueInputBlocker", typeof(RectTransform),
            typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
        // 루트 Canvas를 기준으로 화면 전체의 입력을 정확히 차단합니다.
        // BattleRoom 하위 부모 RectTransform의 크기는 화면과 다를 수 있습니다.
        dialogueInputBlocker.transform.SetParent(null, false);
        Canvas blockerCanvas = dialogueInputBlocker.GetComponent<Canvas>();
        blockerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        blockerCanvas.overrideSorting = true;
        blockerCanvas.sortingOrder = 30000;
        RectTransform rect = dialogueInputBlocker.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image blockerImage = dialogueInputBlocker.GetComponent<Image>();
        blockerImage.color = new Color(0f, 0f, 0f, 0f);
        blockerImage.raycastTarget = true;
        // Keep the existing tutorial artwork above the blocker.
        tutorialSortingCanvas = tutorialRoot.GetComponent<Canvas>();
        if (tutorialSortingCanvas == null) tutorialSortingCanvas = tutorialRoot.gameObject.AddComponent<Canvas>();
        previousOverrideSorting = tutorialSortingCanvas.overrideSorting;
        previousSortingOrder = tutorialSortingCanvas.sortingOrder;
        tutorialSortingCanvas.overrideSorting = true;
        tutorialSortingCanvas.sortingOrder = 30001;
        if (tutorialRoot.GetComponent<GraphicRaycaster>() == null)
            tutorialRoot.gameObject.AddComponent<GraphicRaycaster>();
        dialogueInputBlocker.SetActive(false);
    }

    private void ReleaseDialogueInputBlocker()
    {
        if (dialogueInputBlocker != null)
        {
            Destroy(dialogueInputBlocker);
            dialogueInputBlocker = null;
        }
        if (tutorialSortingCanvas != null)
        {
            tutorialSortingCanvas.overrideSorting = previousOverrideSorting;
            tutorialSortingCanvas.sortingOrder = previousSortingOrder;
            tutorialSortingCanvas = null;
        }
    }
}
// Listens alongside the existing BattleCharacterSelectTarget without changing its
// normal character selection behavior or requiring a Unity UI Button component.
public sealed class TutorialCharacterSelectionClickRelay : MonoBehaviour, IPointerClickHandler
{
    public event Action Clicked;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button == PointerEventData.InputButton.Left)
            Clicked?.Invoke();
    }
}
