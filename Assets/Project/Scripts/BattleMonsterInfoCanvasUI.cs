using Relic.Gameplay.Data;
using Relic.Gameplay.Monster;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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

    [Header("Monster Camera Focus")]
    [Tooltip("몬스터 정보 확인 시 이동할 메인 카메라입니다. 비워두면 Camera.main을 사용합니다.")]
    [SerializeField] private Camera mainCamera;
    [Tooltip("캐릭터를 선택하지 않은 기본 전투 카메라 위치입니다. MonsterInfoPanel을 닫으면 이 위치로 돌아갑니다.")]
    [SerializeField] private Vector3 defaultCameraPosition = new Vector3(0f, 0f, -17.5f);
    [Tooltip("몬스터 정보 확인 중 사용할 카메라 Z 위치입니다.")]
    [SerializeField] private float focusCameraZ = -15f;
    [Tooltip("기존 카메라 X 위치에서 몬스터 X 위치 쪽으로 이동하는 비율입니다. 1이면 몬스터 X에 완전히 맞춥니다.")]
    [SerializeField, Range(0f, 1f)] private float focusAmountX = 0.45f;
    [Tooltip("기존 카메라 Y 위치에서 몬스터 Y 위치 쪽으로 이동하는 비율입니다. 1이면 몬스터 Y에 완전히 맞춥니다.")]
    [SerializeField, Range(0f, 1f)] private float focusAmountY = 0.45f;
    [Tooltip("몬스터 쪽으로 카메라가 이동할 때 추가할 X/Y 오프셋입니다.")]
    [SerializeField] private Vector2 focusOffset = Vector2.zero;
    [Tooltip("몬스터 쪽으로 이동하거나 원래 위치로 돌아오는 시간입니다.")]
    [SerializeField, Min(0f)] private float cameraMoveDuration = 0.25f;

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
    private Coroutine cameraMoveCoroutine;
    private bool hasMonsterCameraFocus;
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
        MonsterUnit.MonsterInfoSelectionChanged -= HandleMonsterInfoSelectionChanged;
        MonsterUnit.MonsterInfoSelectionChanged += HandleMonsterInfoSelectionChanged;

        MonsterUnit selectedMonster = MonsterUnit.CurrentInfoSelectedMonster;
        if (selectedMonster != null && selectedMonster.RuntimeData != null && !selectedMonster.RuntimeData.IsDead)
            Show(selectedMonster);
    }

    private void OnDisable()
    {
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

        int statusSignature = CalculateStatusSignature(boundRuntime.StatusEffects);
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

        // 몬스터를 클릭한 순간부터 BattleCharacterPanel/BattleSlot이 내려가는 동작과
        // 카메라 포커스를 동시에 진행합니다. MonsterInfoPanel 자체는 아직 표시하지 않고,
        // 두 전투 UI가 내려간 뒤에만 활성화/페이드인합니다.
        FocusCameraOnMonster(monster);

        // 몬스터를 클릭한 순간에는 MonsterInfoPanel을 화면에 띄우지 않습니다.
        // 먼저 기존 BattleCharacterPanel/BattleSlot의 선택 전환 이동을 끝낸 뒤
        // RevealAfterBattleUiMovementRoutine에서 활성화하고 페이드인합니다.
        if (monsterInfoPanel != null)
            monsterInfoPanel.SetActive(false);

        EnsureCanvasGroup();
        SetCanvasGroupHiddenImmediate();

        RefreshMonsterName();

        ConfigurePreview(boundRuntime.MonsterId);

        RebuildSkills();
        RebuildStatusEffects();
        lastStatusSignature = CalculateStatusSignature(boundRuntime.StatusEffects);

        // MonsterSkill / MonsterStatus는 런타임에 생성되므로 기존 블러 복제본에는
        // 새 자식 구조가 포함되지 않을 수 있습니다. 동적 UI 생성이 끝난 뒤
        // MonsterInfoCanvas의 복제본을 무효화하고 즉시 다시 구성합니다.
        RefreshBlurReplica();

        StopRevealCoroutine();
        if (isActiveAndEnabled)
        {
            revealCoroutine = StartCoroutine(RevealAfterBattleUiMovementRoutine());
        }
        else
        {
            if (monsterInfoPanel != null)
                monsterInfoPanel.SetActive(true);
            StartPreviewAnimation();
            SetCanvasGroupVisibleImmediate();
        }
    }

    public void Close()
    {
        // 몬스터 선택 상태까지 함께 해제해 기존 공격 범위/선택 표시도 정상적으로 정리합니다.
        if (MonsterUnit.CurrentInfoSelectedMonster != null)
            MonsterUnit.ClearMonsterInfoSelection();
        else
            HidePanelOnly();
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

        HideErosionVisual();

        if (monsterInfoPanel != null)
            monsterInfoPanel.SetActive(true);

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

    private void FocusCameraOnMonster(MonsterUnit monster)
    {
        if (monster == null)
            return;

        EnsureMainCamera();
        if (mainCamera == null)
            return;

        // 몬스터 정보 카메라는 직전에 선택했던 캐릭터의 카메라 위치를 기준으로 하지 않습니다.
        // 캐릭터 미선택 상태의 기본 카메라 위치를 기준으로 몬스터 쪽으로 이동합니다.
        hasMonsterCameraFocus = true;

        Vector3 monsterPosition = monster.transform.position;
        Vector3 targetPosition = new Vector3(
            Mathf.Lerp(defaultCameraPosition.x, monsterPosition.x, focusAmountX) + focusOffset.x,
            Mathf.Lerp(defaultCameraPosition.y, monsterPosition.y, focusAmountY) + focusOffset.y,
            focusCameraZ);

        StartCameraMove(targetPosition, false);
    }

    private void RestoreCameraSmooth()
    {
        if (!hasMonsterCameraFocus)
            return;

        EnsureMainCamera();
        if (mainCamera == null)
        {
            hasMonsterCameraFocus = false;
            return;
        }

        // MonsterInfoPanel을 닫은 뒤에는 이전 캐릭터 포커스 위치가 아니라
        // 캐릭터 미선택 상태의 기본 카메라 위치로 돌아갑니다.
        StartCameraMove(defaultCameraPosition, true);
    }

    private void RestoreCameraImmediate()
    {
        if (cameraMoveCoroutine != null)
        {
            StopCoroutine(cameraMoveCoroutine);
            cameraMoveCoroutine = null;
        }

        if (!hasMonsterCameraFocus)
            return;

        EnsureMainCamera();
        if (mainCamera != null)
            mainCamera.transform.position = defaultCameraPosition;

        hasMonsterCameraFocus = false;
    }

    private void StartCameraMove(Vector3 targetPosition, bool clearFocusWhenComplete)
    {
        if (cameraMoveCoroutine != null)
        {
            StopCoroutine(cameraMoveCoroutine);
            cameraMoveCoroutine = null;
        }

        if (!isActiveAndEnabled || cameraMoveDuration <= 0f)
        {
            if (mainCamera != null)
                mainCamera.transform.position = targetPosition;

            if (clearFocusWhenComplete)
                hasMonsterCameraFocus = false;
            return;
        }

        cameraMoveCoroutine = StartCoroutine(CameraMoveRoutine(targetPosition, clearFocusWhenComplete));
    }

    private IEnumerator CameraMoveRoutine(Vector3 targetPosition, bool clearFocusWhenComplete)
    {
        if (mainCamera == null)
        {
            cameraMoveCoroutine = null;
            yield break;
        }

        Vector3 startPosition = mainCamera.transform.position;
        float elapsed = 0f;

        while (elapsed < cameraMoveDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / cameraMoveDuration);
            t = t * t * (3f - 2f * t);
            mainCamera.transform.position = Vector3.LerpUnclamped(startPosition, targetPosition, t);
            yield return null;
        }

        if (mainCamera != null)
            mainCamera.transform.position = targetPosition;

        if (clearFocusWhenComplete)
            hasMonsterCameraFocus = false;

        cameraMoveCoroutine = null;
    }

    private void EnsureMainCamera()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
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

        List<StatusEffectRuntimeData> mergedStatuses = MergeStatusEffects(boundRuntime.StatusEffects);

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

    private static int CalculateStatusSignature(List<StatusEffectRuntimeData> statuses)
    {
        if (statuses == null || statuses.Count == 0)
            return 0;

        unchecked
        {
            int hash = 17;
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
