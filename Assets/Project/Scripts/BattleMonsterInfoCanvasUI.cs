using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 배틀씬의 몬스터 상세 정보 패널을 관리합니다.
/// 이 컴포넌트는 비활성화되는 MonsterInfoPanel이 아니라 항상 활성 상태인 MonsterInfoCanvas에 붙여 주세요.
/// MonsterUnit의 월드 오브젝트를 클릭하면 패널을 열고, ESC를 누르면 닫습니다.
/// </summary>
public sealed class BattleMonsterInfoCanvasUI : MonoBehaviour
{
    private static BattleMonsterInfoCanvasUI activeInstance;
    private static int lastClosedByEscapeFrame = -1;
    private static int lastClosedByRightClickFrame = -1;

    public static bool WasClosedByRightClickThisFrame => lastClosedByRightClickFrame == Time.frameCount;

    public static bool TryHandleRightClickIfOpen()
    {
        if (WasClosedByRightClickThisFrame)
            return true;
        if (activeInstance == null || !activeInstance.HasPanelContextActive())
            return false;
        if (activeInstance.IsMenuPanelOpen())
            return false;

        lastClosedByRightClickFrame = Time.frameCount;
        activeInstance.Close();
        return true;
    }

    /// <summary>
    /// 같은 프레임의 ESC 입력이 BattleMenu까지 전달되지 않도록 사용합니다.
    /// </summary>
    public static bool WasClosedByEscapeThisFrame => lastClosedByEscapeFrame == Time.frameCount;

    /// <summary>
    /// MonsterInfoPanel이 열려 있으면 ESC 우선 처리 대상으로 닫습니다.
    /// BattleMenuEscapeInputController에서도 호출하므로 스크립트 실행 순서와 관계없이
    /// 같은 ESC로 메뉴가 함께 열리는 것을 막을 수 있습니다.
    /// </summary>
    public static bool TryHandleEscapeIfOpen()
    {
        // 패널이 아직 화면에 활성화되기 전이라도, 몬스터 클릭 후
        // BattleCharacterPanel/BattleSlot 이동을 기다리는 중이면 ESC를
        // MonsterInfo 닫기 입력으로 소비합니다.
        if (activeInstance == null || !activeInstance.HasPanelContextActive())
            return false;

        // MenuPanel이 열려 있다면 ESC는 메뉴 닫기에 먼저 사용합니다.
        if (activeInstance.IsMenuPanelOpen())
            return false;

        lastClosedByEscapeFrame = Time.frameCount;
        activeInstance.Close();
        return true;
    }

    [Header("Panel")]
    [SerializeField] private GameObject monsterInfoPanel;
    [SerializeField] private TMP_Text monsterNameText;
    [SerializeField] private Button backButton;
    [SerializeField] private CanvasGroup monsterInfoCanvasGroup;

    [Header("Monster Preview")]
    [SerializeField] private Image previewImage;

    [Header("Battle Overlay UI")]
    [Tooltip("전투 화면의 Erosion 오브젝트입니다. 비워두면 이름이 Erosion인 오브젝트를 자동으로 찾습니다.")]
    [SerializeField] private GameObject erosionRoot;

    [Header("Open Animation")]
    [Tooltip("BattleCharacterPanel/BattleSlot 이동이 끝난 뒤 MonsterInfoPanel이 나타나는 페이드 시간입니다.")]
    [SerializeField, Min(0f)] private float fadeInDuration = 0.15f;

    [Header("Skill List")]
    [SerializeField] private Transform skillContent;
    [SerializeField] private MonsterInfoSkillItemUI monsterSkillPrefab;

    [Header("Status Effect List")]
    [SerializeField] private Transform statusContent;
    [SerializeField] private MonsterInfoStatusItemUI monsterStatusPrefab;

    [Header("Auto Find Names")]
    [SerializeField] private string panelObjectName = "MonsterInfoPanel";
    [SerializeField] private string monsterNameObjectName = "MonsterName";
    [SerializeField] private string backButtonObjectName = "BackButton";
    [SerializeField] private string previewObjectName = "Preview";
    [SerializeField] private string previewImageObjectName = "image";
    [SerializeField] private string skillListObjectName = "SkillList";
    [SerializeField] private string statusEffectListObjectName = "StatusEffectList";
    [SerializeField] private string contentObjectName = "Contant";
    [SerializeField] private string contentFallbackObjectName = "Content";
    [SerializeField] private string menuPanelObjectName = "MenuPanel";

    private readonly List<MonsterInfoSkillItemUI> spawnedSkillItems = new();
    private readonly List<MonsterInfoStatusItemUI> spawnedStatusItems = new();

    private MonsterUnit boundMonster;
    private MonsterRuntimeData boundRuntime;
    private int lastStatusSignature = int.MinValue;
    private Coroutine revealCoroutine;
    private Coroutine previewAnimationCoroutine;
    private Sprite[] currentPreviewFrames;
    private float currentPreviewFramesPerSecond = 6f;
    private BattleCharacterPanelUI battleCharacterPanel;
    private CanvasGroup erosionCanvasGroup;
    private bool erosionVisualOverridden;
    private float erosionOriginalAlpha = 1f;
    private bool erosionOriginalInteractable = true;
    private bool erosionOriginalBlocksRaycasts = true;

    private void Awake()
    {
        activeInstance = this;
        ResolveReferences();
        BindBackButton();

        if (monsterInfoPanel != null)
            monsterInfoPanel.SetActive(false);

        SetCanvasGroupHiddenImmediate();
    }

    private void OnEnable()
    {
        activeInstance = this;
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        MonsterUnit.MonsterInfoSelectionChanged -= HandleMonsterInfoSelectionChanged;
        MonsterUnit.MonsterInfoSelectionChanged += HandleMonsterInfoSelectionChanged;

        MonsterUnit selectedMonster = MonsterUnit.CurrentInfoSelectedMonster;
        if (selectedMonster != null && selectedMonster.RuntimeData != null && !selectedMonster.RuntimeData.IsDead)
            Show(selectedMonster);
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
        if (activeInstance == this)
            activeInstance = null;
        MonsterUnit.MonsterInfoSelectionChanged -= HandleMonsterInfoSelectionChanged;
        StopRevealCoroutine();
        StopPreviewAnimation();
        RestoreCameraImmediate();
        RestoreErosionVisual();
        ClearSpawnedItems();
        boundMonster = null;
        boundRuntime = null;
        lastStatusSignature = int.MinValue;
    }

    private void OnDestroy()
    {
        if (activeInstance == this)
            activeInstance = null;
        if (backButton != null)
            backButton.onClick.RemoveListener(Close);
    }

    private void Update()
    {
        // 실제 MonsterInfoPanel이 아직 비활성 상태여도, 몬스터를 선택하고
        // BattleCharacterPanel/BattleSlot 이동 완료를 기다리는 중이라면
        // MonsterInfo가 열리는 과정으로 간주합니다.
        if (!HasPanelContextActive())
            return;

        if (Input.GetMouseButtonDown(1) && TryHandleRightClickIfOpen())
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // MenuPanel이 MonsterInfoPanel 위에 열려 있는 동안에는
            // ESC를 MonsterInfo에서 소비하지 않습니다. 먼저 MenuPanel이 닫혀야 합니다.
            if (IsMenuPanelOpen())
                return;

            lastClosedByEscapeFrame = Time.frameCount;
            Close();
            return;
        }

        if (boundMonster != null)
            boundRuntime = boundMonster.RuntimeData;

        RefreshMonsterName();

        if (boundRuntime == null || boundRuntime.IsDead)
        {
            Close();
            return;
        }

        int statusSignature = CalculateStatusSignature(boundRuntime.StatusEffects, boundRuntime.CurrentShield);
        if (statusSignature != lastStatusSignature)
        {
            RebuildStatusEffects();
            lastStatusSignature = statusSignature;
            RefreshBlurReplica();
        }
    }

    private bool IsMenuPanelOpen()
    {
        GameObject[] objects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < objects.Length; i++)
        {
            GameObject candidate = objects[i];

            if (candidate == null || candidate.name != menuPanelObjectName)
                continue;

            if (candidate.activeInHierarchy)
                return true;
        }

        return false;
    }

    public void Show(MonsterUnit monster)
    {
        if (monster == null || monster.RuntimeData == null || monster.RuntimeData.IsDead)
            return;

        ResolveReferences();
        BindBackButton();

        boundMonster = monster;
        boundRuntime = monster.RuntimeData;

        // 캐릭터 정보 패널처럼 선택과 동시에 카메라 이동 및 패널 표시를 시작합니다.
        FocusCameraOnMonster(monster);
        StopRevealCoroutine();

        RefreshMonsterName();
        ConfigurePreview(boundRuntime.MonsterId);
        RebuildSkills();
        RebuildStatusEffects();
        lastStatusSignature = CalculateStatusSignature(boundRuntime.StatusEffects, boundRuntime.CurrentShield);

        RestoreErosionVisual();
        if (monsterInfoPanel != null)
            monsterInfoPanel.SetActive(true);
        Canvas.ForceUpdateCanvases();
        RefreshBlurReplica();
        StartPreviewAnimation();
        EnsureCanvasGroup();
        SetCanvasGroupVisibleImmediate();
    }

    public void Close()
    {
        // 먼저 몬스터 선택과 패널을 닫은 뒤 이전 캐릭터 선택을 복원합니다.
        // 이벤트 처리 도중 즉시 캐릭터를 고르면 UI 갱신 순서에 따라 다시 해제될 수 있습니다.
        bool hadMonsterInfo = HasPanelContextActive();
        if (MonsterUnit.CurrentInfoSelectedMonster != null)
            MonsterUnit.ClearMonsterInfoSelection();
        else
            HidePanelOnly();

        if (hadMonsterInfo && isActiveAndEnabled)
            StartCoroutine(RestoreCharacterSelectionNextFrame());
    }

    private IEnumerator RestoreCharacterSelectionNextFrame()
    {
        yield return null;

        // 이미 다른 몬스터를 열었다면 이전 캐릭터로 되돌리지 않습니다.
        if (MonsterUnit.CurrentInfoSelectedMonster != null)
            yield break;

        BattleTimelineController timeline = FindFirstObjectByType<BattleTimelineController>(
            FindObjectsInactive.Exclude);
        if (timeline != null && timeline.IsPlayerReservationInputActive)
            timeline.RestoreLastSelectedCharacterAfterMonsterInfo();
    }

    private void HandleMonsterInfoSelectionChanged(MonsterUnit monster)
    {
        if (monster == null)
        {
            HidePanelOnly();
            return;
        }

        Show(monster);
    }

    private void HidePanelOnly()
    {
        StopRevealCoroutine();
        StopPreviewAnimation();
        ClearPreview();
        RestoreCameraSmooth();
        RestoreErosionVisual();
        SetCanvasGroupHiddenImmediate();
        ClearSpawnedItems();

        boundMonster = null;
        boundRuntime = null;
        lastStatusSignature = int.MinValue;

        if (monsterNameText != null)
            monsterNameText.text = string.Empty;

        if (monsterInfoPanel != null)
            monsterInfoPanel.SetActive(false);

        SetCanvasGroupHiddenImmediate();
    }

    private IEnumerator RevealAfterBattleUiMovementRoutine()
    {
        if (boundMonster == null || boundRuntime == null || boundRuntime.IsDead)
        {
            revealCoroutine = null;
            yield break;
        }

        // Keep the battle Erosion gauge visible while inspecting a monster.
        RestoreErosionVisual();

        if (monsterInfoPanel != null)
            monsterInfoPanel.SetActive(true);

        // 동적 스킬/상태 UI는 패널이 활성화된 뒤에만 블러 복제본에 정상 반영된다.
        Canvas.ForceUpdateCanvases();
        RefreshBlurReplica();

        StartPreviewAnimation();

        EnsureCanvasGroup();
        SetCanvasGroupHiddenImmediate();

        if (monsterInfoCanvasGroup == null)
        {
            revealCoroutine = null;
            yield break;
        }

        if (fadeInDuration <= 0f)
        {
            SetCanvasGroupVisibleImmediate();
            revealCoroutine = null;
            yield break;
        }

        monsterInfoCanvasGroup.interactable = false;
        monsterInfoCanvasGroup.blocksRaycasts = false;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            monsterInfoCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        SetCanvasGroupVisibleImmediate();
        revealCoroutine = null;
    }
    /// <summary>
    /// MonsterInfoCanvas 아래에 런타임으로 생성된 Skill/Status UI가
    /// 블러용 복제 hierarchy에도 반영되도록 복제본을 다시 만듭니다.
    /// </summary>
    private void RefreshBlurReplica()
    {
        if (!UIBlurBackgroundManager.HasInstance)
            return;

        UIBlurBackgroundManager manager = UIBlurBackgroundManager.Instance;
        manager.InvalidateReplicaSource(gameObject);
        manager.RefreshPresentation();
    }

    private void HideErosionVisual()
    {
        if (erosionVisualOverridden)
            return;

        if (erosionRoot == null)
            erosionRoot = GameObject.Find("Erosion");

        if (erosionRoot == null)
            return;

        erosionCanvasGroup = erosionRoot.GetComponent<CanvasGroup>();
        if (erosionCanvasGroup == null)
            erosionCanvasGroup = erosionRoot.AddComponent<CanvasGroup>();

        erosionOriginalAlpha = erosionCanvasGroup.alpha;
        erosionOriginalInteractable = erosionCanvasGroup.interactable;
        erosionOriginalBlocksRaycasts = erosionCanvasGroup.blocksRaycasts;
        erosionVisualOverridden = true;

        erosionCanvasGroup.alpha = 0f;
        erosionCanvasGroup.interactable = false;
        erosionCanvasGroup.blocksRaycasts = false;
    }

    private void RestoreErosionVisual()
    {
        if (!erosionVisualOverridden)
            return;

        if (erosionCanvasGroup != null)
        {
            erosionCanvasGroup.alpha = erosionOriginalAlpha;
            erosionCanvasGroup.interactable = erosionOriginalInteractable;
            erosionCanvasGroup.blocksRaycasts = erosionOriginalBlocksRaycasts;
        }

        erosionVisualOverridden = false;
        erosionCanvasGroup = null;
    }

    private static void FocusCameraOnMonster(MonsterUnit monster)
    {
        if (monster == null)
            return;

        BattleCameraController cameraController = BattleCameraController.Instance;
        if (cameraController != null)
            cameraController.FocusMonsterInfoForGridIndex(monster.transform, monster.MainGridIndex);
    }

    private static void RestoreCameraSmooth()
    {
        BattleCameraController cameraController = BattleCameraController.Instance;
        if (cameraController != null)
            cameraController.ReturnDefaultFromMonsterInfoFocus();
    }

    private static void RestoreCameraImmediate()
    {
        BattleCameraController cameraController = BattleCameraController.Instance;
        if (cameraController != null)
            cameraController.ReturnDefaultFromMonsterInfoFocus();
    }

    private void EnsureBattleCharacterPanel()
    {
        if (battleCharacterPanel != null)
            return;

        battleCharacterPanel = UnityEngine.Object.FindFirstObjectByType<BattleCharacterPanelUI>(FindObjectsInactive.Include);
    }

    private void EnsureCanvasGroup()
    {
        if (monsterInfoCanvasGroup != null)
            return;

        if (monsterInfoPanel != null)
            monsterInfoCanvasGroup = monsterInfoPanel.GetComponent<CanvasGroup>();
    }

    private void SetCanvasGroupHiddenImmediate()
    {
        EnsureCanvasGroup();
        if (monsterInfoCanvasGroup == null)
            return;

        monsterInfoCanvasGroup.alpha = 0f;
        monsterInfoCanvasGroup.interactable = false;
        monsterInfoCanvasGroup.blocksRaycasts = false;
    }

    private void SetCanvasGroupVisibleImmediate()
    {
        EnsureCanvasGroup();
        if (monsterInfoCanvasGroup == null)
            return;

        monsterInfoCanvasGroup.alpha = 1f;
        monsterInfoCanvasGroup.interactable = true;
        monsterInfoCanvasGroup.blocksRaycasts = true;
    }

    private void StopRevealCoroutine()
    {
        if (revealCoroutine == null)
            return;

        StopCoroutine(revealCoroutine);
        revealCoroutine = null;
    }

    private void ConfigurePreview(string monsterId)
    {
        StopPreviewAnimation();
        ClearPreview();

        if (previewImage == null)
            return;

        DataManager dataManager = DataManager.Instance;
        MonsterPrefabDatabase database = dataManager != null ? dataManager.MonsterPrefabDatabase : null;
        if (database == null || !database.TryGetEntry(monsterId, out MonsterPrefabDatabase.Entry entry) || entry == null)
            return;

        currentPreviewFrames = entry.idlePreviewSprites;
        currentPreviewFramesPerSecond = Mathf.Max(0.01f, entry.previewFramesPerSecond);

        RectTransform previewRect = previewImage.rectTransform;
        if (previewRect != null)
            previewRect.sizeDelta = entry.previewSize;

        previewImage.preserveAspect = true;

        Sprite firstFrame = GetFirstValidPreviewFrame(currentPreviewFrames);
        previewImage.sprite = firstFrame;
        previewImage.enabled = firstFrame != null;
    }

    private void StartPreviewAnimation()
    {
        StopPreviewAnimation();

        if (!isActiveAndEnabled || previewImage == null || currentPreviewFrames == null || currentPreviewFrames.Length == 0)
            return;

        previewAnimationCoroutine = StartCoroutine(PreviewAnimationRoutine());
    }

    private IEnumerator PreviewAnimationRoutine()
    {
        int frameIndex = 0;
        float elapsed = 0f;
        float frameDuration = 1f / Mathf.Max(0.01f, currentPreviewFramesPerSecond);

        while (previewImage != null && currentPreviewFrames != null && currentPreviewFrames.Length > 0)
        {
            Sprite frame = FindNextValidPreviewFrame(ref frameIndex);
            if (frame == null)
            {
                previewImage.enabled = false;
                yield break;
            }

            previewImage.sprite = frame;
            previewImage.enabled = true;

            elapsed = 0f;
            while (elapsed < frameDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        previewAnimationCoroutine = null;
    }

    private Sprite FindNextValidPreviewFrame(ref int frameIndex)
    {
        if (currentPreviewFrames == null || currentPreviewFrames.Length == 0)
            return null;

        for (int checkedCount = 0; checkedCount < currentPreviewFrames.Length; checkedCount++)
        {
            int index = frameIndex % currentPreviewFrames.Length;
            frameIndex = (frameIndex + 1) % currentPreviewFrames.Length;

            Sprite frame = currentPreviewFrames[index];
            if (frame != null)
                return frame;
        }

        return null;
    }

    private static Sprite GetFirstValidPreviewFrame(Sprite[] frames)
    {
        if (frames == null)
            return null;

        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null)
                return frames[i];
        }

        return null;
    }

    private void StopPreviewAnimation()
    {
        if (previewAnimationCoroutine == null)
            return;

        StopCoroutine(previewAnimationCoroutine);
        previewAnimationCoroutine = null;
    }

    private void ClearPreview()
    {
        currentPreviewFrames = null;
        currentPreviewFramesPerSecond = 6f;

        if (previewImage == null)
            return;

        previewImage.sprite = null;
        previewImage.enabled = false;
    }

    private void HandleLocaleChanged(Locale _)
    {
        if (HasPanelContextActive() && boundRuntime != null)
            RebuildSkills();
    }

    private void RebuildSkills()
    {
        ClearSkillItems();

        if (boundRuntime == null || skillContent == null || monsterSkillPrefab == null)
            return;

        List<string> skillIds = GetOrderedSkillIds(boundRuntime);
        DataManager dataManager = DataManager.Instance;

        for (int i = 0; i < skillIds.Count; i++)
        {
            string skillId = skillIds[i];
            if (dataManager == null || dataManager.MonsterSkillDatabase == null)
                continue;

            MonsterSkillData skillData = dataManager.MonsterSkillDatabase.Get(skillId);
            if (skillData == null)
                continue;

            MonsterInfoSkillItemUI item = Instantiate(monsterSkillPrefab, skillContent, false);
            item.gameObject.name = $"MonsterSkill_{skillId}";
            item.gameObject.SetActive(true);
            item.Bind(skillData);
            spawnedSkillItems.Add(item);
        }
    }

    private void RebuildStatusEffects()
    {
        ClearStatusItems();

        if (boundRuntime == null || statusContent == null || monsterStatusPrefab == null)
            return;

        List<StatusEffectRuntimeData> mergedStatuses = BuildDisplayStatuses(boundRuntime.StatusEffects, boundRuntime.CurrentShield);

        for (int i = 0; i < mergedStatuses.Count; i++)
        {
            StatusEffectRuntimeData statusData = mergedStatuses[i];
            if (statusData == null || !statusData.IsValid())
                continue;

            MonsterInfoStatusItemUI item = Instantiate(monsterStatusPrefab, statusContent, false);
            item.gameObject.name = $"MonsterStatus_{statusData.EffectId}";
            item.gameObject.SetActive(true);
            item.Bind(statusData);
            spawnedStatusItems.Add(item);
        }
    }

    private static List<string> GetOrderedSkillIds(MonsterRuntimeData runtime)
    {
        List<string> result = new();
        HashSet<string> added = new(StringComparer.OrdinalIgnoreCase);

        if (runtime == null)
            return result;

        if (runtime.PossibleSkillIdsByActionIndex != null)
        {
            for (int i = 0; i < runtime.PossibleSkillIdsByActionIndex.Length; i++)
                AddSkillId(runtime.PossibleSkillIdsByActionIndex[i], result, added);
        }

        if (runtime.PossSkillIds != null)
        {
            for (int i = 0; i < runtime.PossSkillIds.Count; i++)
                AddSkillId(runtime.PossSkillIds[i], result, added);
        }

        return result;
    }

    private static void AddSkillId(string skillId, List<string> result, HashSet<string> added)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return;

        string normalizedId = skillId.Trim();
        if (normalizedId == "0" || !added.Add(normalizedId))
            return;

        result.Add(normalizedId);
    }

    // Shield is stored as a combat resource. Add a display-only effect without
    // changing the actual status list or affecting shield calculations.
    private static List<StatusEffectRuntimeData> BuildDisplayStatuses(
        List<StatusEffectRuntimeData> statuses, int currentShield)
    {
        List<StatusEffectRuntimeData> result = MergeStatusEffects(statuses);
        result.RemoveAll(status => string.Equals(status.EffectId, "E_Armor", StringComparison.OrdinalIgnoreCase));
        if (currentShield > 0)
            result.Add(new StatusEffectRuntimeData("E_Armor", currentShield, 1));
        return result;
    }

    private static List<StatusEffectRuntimeData> MergeStatusEffects(List<StatusEffectRuntimeData> source)
    {
        List<StatusEffectRuntimeData> result = new();
        Dictionary<string, StatusEffectRuntimeData> merged = new(StringComparer.OrdinalIgnoreCase);

        if (source == null)
            return result;

        for (int i = 0; i < source.Count; i++)
        {
            StatusEffectRuntimeData status = source[i];
            if (status == null || !status.IsValid() || string.IsNullOrWhiteSpace(status.EffectId))
                continue;

            string effectId = status.EffectId.Trim();
            if (merged.TryGetValue(effectId, out StatusEffectRuntimeData existing))
            {
                existing.Stack += status.Stack;
                existing.TurnCount = Mathf.Max(existing.TurnCount, status.TurnCount);
                existing.IsPassive |= status.IsPassive;
                continue;
            }

            StatusEffectRuntimeData copy = new StatusEffectRuntimeData(effectId, status.Stack, status.TurnCount)
            {
                IsPassive = status.IsPassive,
                SourceSkillId = status.SourceSkillId
            };

            merged.Add(effectId, copy);
            result.Add(copy);
        }

        return result;
    }

    private static int CalculateStatusSignature(List<StatusEffectRuntimeData> statuses, int currentShield)
    {
        if (statuses == null || statuses.Count == 0)
            return currentShield;

        unchecked
        {
            int hash = 17;
            hash = hash * 31 + currentShield;
            hash = hash * 31 + statuses.Count;

            for (int i = 0; i < statuses.Count; i++)
            {
                StatusEffectRuntimeData status = statuses[i];
                if (status == null)
                {
                    hash = hash * 31;
                    continue;
                }

                hash = hash * 31 + (status.EffectId != null ? status.EffectId.GetHashCode() : 0);
                hash = hash * 31 + status.Stack;
                hash = hash * 31 + status.TurnCount;
                hash = hash * 31 + (status.IsPassive ? 1 : 0);
            }

            return hash;
        }
    }

    private void RefreshMonsterName()
    {
        if (monsterNameText == null)
            return;

        if (boundRuntime == null)
        {
            monsterNameText.text = string.Empty;
            return;
        }

        string displayName = boundRuntime.GetDisplayName();
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = !string.IsNullOrWhiteSpace(boundRuntime.Name)
                ? boundRuntime.Name
                : boundRuntime.MonsterId;

        monsterNameText.text = displayName ?? string.Empty;
    }

    private bool IsPanelOpen()
    {
        return monsterInfoPanel != null && monsterInfoPanel.activeInHierarchy;
    }

    /// <summary>
    /// MonsterInfoPanel이 실제로 표시 중이거나, 몬스터를 클릭한 뒤
    /// BattleCharacterPanel/BattleSlot 이동 완료를 기다리는 중인지 반환합니다.
    /// </summary>
    private bool HasPanelContextActive()
    {
        return IsPanelOpen() || boundMonster != null || revealCoroutine != null;
    }

    private void ClearSpawnedItems()
    {
        ClearSkillItems();
        ClearStatusItems();
    }

    private void ClearSkillItems()
    {
        for (int i = 0; i < spawnedSkillItems.Count; i++)
        {
            MonsterInfoSkillItemUI item = spawnedSkillItems[i];
            if (item != null)
                Destroy(item.gameObject);
        }

        spawnedSkillItems.Clear();
    }

    private void ClearStatusItems()
    {
        for (int i = 0; i < spawnedStatusItems.Count; i++)
        {
            MonsterInfoStatusItemUI item = spawnedStatusItems[i];
            if (item != null)
                Destroy(item.gameObject);
        }

        spawnedStatusItems.Clear();
    }

    private void ResolveReferences()
    {
        if (monsterInfoPanel == null)
        {
            Transform panel = FindChildRecursive(transform, panelObjectName);
            if (panel != null)
                monsterInfoPanel = panel.gameObject;
        }

        Transform panelRoot = monsterInfoPanel != null ? monsterInfoPanel.transform : transform;

        if (monsterNameText == null)
        {
            Transform nameRoot = FindChildRecursive(panelRoot, monsterNameObjectName);
            if (nameRoot != null)
                monsterNameText = nameRoot.GetComponent<TMP_Text>() ?? nameRoot.GetComponentInChildren<TMP_Text>(true);
        }

        if (backButton == null)
        {
            Transform backButtonRoot = FindChildRecursive(panelRoot, backButtonObjectName);
            if (backButtonRoot != null)
                backButton = backButtonRoot.GetComponent<Button>() ?? backButtonRoot.GetComponentInChildren<Button>(true);
        }

        if (previewImage == null)
        {
            Transform previewRoot = FindChildRecursive(panelRoot, previewObjectName);
            if (previewRoot != null)
            {
                Transform imageRoot = FindChildRecursive(previewRoot, previewImageObjectName);
                if (imageRoot != null)
                    previewImage = imageRoot.GetComponent<Image>();
            }
        }

        if (skillContent == null)
            skillContent = FindListContent(panelRoot, skillListObjectName);

        if (statusContent == null)
            statusContent = FindListContent(panelRoot, statusEffectListObjectName);
    }

    private void BindBackButton()
    {
        if (backButton == null)
            return;

        // 중복 등록을 방지하면서 BackButton과 ESC가 동일한 Close 경로를 사용하게 합니다.
        backButton.onClick.RemoveListener(Close);
        backButton.onClick.AddListener(Close);
    }

    private Transform FindListContent(Transform root, string listObjectName)
    {
        Transform listRoot = FindChildRecursive(root, listObjectName);
        if (listRoot == null)
            return null;

        Transform content = FindDirectChild(listRoot, contentObjectName);
        if (content == null)
            content = FindDirectChild(listRoot, contentFallbackObjectName);

        if (content == null)
            content = FindChildRecursive(listRoot, contentObjectName);
        if (content == null)
            content = FindChildRecursive(listRoot, contentFallbackObjectName);

        return content;
    }

    private static Transform FindDirectChild(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child != null && child.name == childName)
                return child;
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child == null)
                continue;

            if (child.name == childName)
                return child;

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
