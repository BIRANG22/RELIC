using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Relic.Gameplay.Data;

public class RestRoomController : MonoBehaviour
{
    [Header("Ally Spawn")]
    [SerializeField] private Transform[] allySpawnPoints;
    [SerializeField] private float allySpawnScale = 0.7f;
    [Tooltip("휴식방 캐릭터 호버 시 표시할 Character Resources UI입니다. 비워두면 씬에서 자동으로 찾습니다.")]
    [SerializeField] private BattleCharacterResourcesUI characterResourcesUI;

    [Header("Upgrade")]
    [SerializeField] private EventSkillAwakenSelectionPanelUI skillAwakenSelectionPanel;
    [SerializeField] private GameObject upgradeButtonRoot;
    [SerializeField] private Image upgradeButtonBackImage;
    [SerializeField] private TMP_Text upgradeButtonText;
    [SerializeField] private Color disabledUpgradeButtonColor = new Color32(0x7E, 0x7E, 0x7E, 0xFF);

    [Header("Heal")]
    [SerializeField, Range(0f, 1f)] private float healHpRatio = 0.3f;
    [SerializeField] private GameObject healButtonRoot;
    [Tooltip("회복 버튼을 눌렀을 때만 활성화되는 Heal 연출 오브젝트입니다. RestRoom 내부의 Heal을 연결하세요.")]
    [SerializeField] private GameObject healEffectRoot;
    [SerializeField, Min(0.1f)] private float healEffectDuration = 2f;
    [SerializeField] private GameObject healTextRoot;
    [SerializeField] private Image healButtonBackImage;
    [SerializeField] private TMP_Text healButtonText;
    [SerializeField] private Color disabledHealButtonColor = new Color32(0x7E, 0x7E, 0x7E, 0xFF);

    [Header("Meditation")]
    [SerializeField] private BattleRewardSkillPanelUI meditationSkillPanel;
    [SerializeField] private GameObject meditationButtonRoot;
    [SerializeField] private GameObject meditationTextRoot;
    [SerializeField, Min(0f)] private float hoverFadeDuration = 0.2f;
    [SerializeField, Min(0)] private int restErosionReduction = 10;
    [Header("Selected Text")]
    [SerializeField] private GameObject selectedTextRoot;
    [SerializeField] private GameObject selectedHealRoot;
    [SerializeField] private GameObject selectedMeditationRoot;
    [SerializeField, Min(0f)] private float actionButtonFadeDuration = 0.25f;
    private Coroutine actionButtonFadeRoutine;
    private bool restChoicePending;
    private Button boundMeditationButton;

    [Header("Shop")]
    [SerializeField] private RestRoomShopPanel shopPanel;

    [Header("Progression")]
    [SerializeField] private GameObject nextButtonRoot;
    private Button nextButton;
    private bool nextTransitionStarted;

    [Header("Player HUD")]
    [SerializeField] private Transform playerHudRoot;
    [SerializeField] private PlayerHUDSlot playerHudPrefab;
    [SerializeField] private Transform[] playerHudPositionAnchors = new Transform[3];
    [SerializeField] private float playerHudScale = 0.4f;
    [SerializeField] private bool autoFindPlayerHudReferences = true;

    private bool isRestUsed;
    private BattleUnitAnimator[] spawnedAllyAnimators;
    private PlayerHUDSlot[] spawnedPlayerHuds;
    private Color upgradeButtonBackDefaultColor = Color.white;
    private Color upgradeButtonTextDefaultColor = Color.white;
    private bool hasUpgradeButtonDefaultColors;
    private Color healButtonBackDefaultColor = Color.white;
    private Color healButtonTextDefaultColor = Color.white;
    private bool hasHealButtonDefaultColors;

    private readonly List<EventSkillAwakenSelectionPanelEntry> skillAwakenOptions = new();
    private EventChoiceSkillAwakenTarget pendingSkillAwakenResultTarget;
    private bool hasPendingSkillAwakenResult;
    private Coroutine skillAwakenResultRoutine;
    private Coroutine healEffectRoutine;
    private Transform healEffectTransform;
    private Vector3 healEffectOriginalLocalPosition;
    private bool healEffectPositionCaptured;
    private Button boundHealButton;

    private bool IsRestActionLocked => isRestUsed;

    private void Awake()
    {
        EnsureShopPanelSpawner();
        EnsureNextButtonRoot();
        EnsureUpgradeButtonReferences();
        EnsureHealButtonReferences();
        EnsureHealTextReferences();
        EnsureMeditationReferences();
        PrepareHealEffectRoot();
        EnsureSkillAwakenSelectionPanel();
    }

    private void OnEnable()
    {
        // A RestRoom instance may be reused after abandoning an exploration.
        // Restart its scene animators so entry visuals do not stay at their previous end states.
        ResetRestRoomSceneAnimations();
        isRestUsed = false;
        restChoicePending = false;
        if (actionButtonFadeRoutine != null) { StopCoroutine(actionButtonFadeRoutine); actionButtonFadeRoutine = null; }
        nextTransitionStarted = false;
        hasPendingSkillAwakenResult = false;
        pendingSkillAwakenResultTarget = default;

        EnsureShopPanelSpawner();
        EnsureNextButtonRoot();
        EnsureUpgradeButtonReferences();
        EnsureHealButtonReferences();
        BindHealButton();
        EnsureMeditationReferences();
        BindMeditationButton();
        EnsureHealTextReferences();
        PrepareHealEffectRoot();
        EnsureSkillAwakenSelectionPanel();
        EnsurePlayerHudReferences();
        // Previous visit may have hidden or reparented the shared resource UI.
        ResolveCharacterResourcesUI();
        if (characterResourcesUI != null && characterResourcesUI.gameObject.activeInHierarchy)
            characterResourcesUI.HideAll();
        SetHealTextVisibleImmediate(false);
        SetMeditationTextVisible(false);
        UpdateRestDescription();
        SetNextButtonVisible(false);
        ResetSelectedText();
        RestoreRestButtonTransitions();
        SetRestActionButtonsVisible(true);
        SetUpgradeButtonDisabledFeedback(false);
        SetHealButtonDisabledFeedback(false);
        SpawnPartyAllies();
        SpawnPlayerHUDs();
    }

    private void ResetRestRoomSceneAnimations()
    {
        Animator[] animators = GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator actor = animators[i];
            if (actor == null || actor.runtimeAnimatorController == null)
                continue;

            // Rebind even inactive children: they can be re-enabled later in the room.
            actor.Rebind();
            if (actor.isActiveAndEnabled)
                actor.Update(0f);
        }
    }

    private void OnDisable()
    {
        if (actionButtonFadeRoutine != null) { StopCoroutine(actionButtonFadeRoutine); actionButtonFadeRoutine = null; }
        restChoicePending = false;
        UnbindHealButton();
        UnbindMeditationButton();
        UnbindNextButton();
        SetNextButtonVisible(false);

        if (skillAwakenResultRoutine != null)
        {
            StopCoroutine(skillAwakenResultRoutine);
            skillAwakenResultRoutine = null;
        }

        if (skillAwakenSelectionPanel != null)
            skillAwakenSelectionPanel.Close();

        SetHealTextVisibleImmediate(false);
        SetMeditationTextVisible(false);
        StopHealEffect();
        skillAwakenOptions.Clear();
        hasPendingSkillAwakenResult = false;
        pendingSkillAwakenResultTarget = default;
        ClearPlayerHUDs();
        RestoreRestResourceCanvasParent();
    }

    public void OnRestButtonClicked()
    {
        if (restChoicePending) return;
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        if (SteamBattleStateSynchronizer.TryBlockSharedBattleStateEdit())
            return;

        if (IsRestActionLocked)
            return;

        isRestUsed = true;
        if (skillAwakenSelectionPanel != null)
            skillAwakenSelectionPanel.Close();

        int[] hpBefore = CapturePartyHPBeforeRest();
        RecoverAllPartyHPByRatio(healHpRatio);
        BattleErosionRuntimeService.Add(-restErosionReduction, "RestRoomHeal");
        ShowRestHealPresentation(hpBefore);
        RefreshPlayerHUDs();
        PlayHealVfxOnSpawnedAllies();
        PlayHealEffectRoot();
        // Heal_Text is a hover-only object; do not play a click-triggered fade.
        FadeOutRestActionButtons(false);
        SetNextButtonVisible(true);
    }

    public void OnUpgradeButtonClicked()
    {
        // 기존 Unity Button OnClick에 기억 강화 이벤트가 남아 있어도
        // 새 휴식방에서는 명상(기억 발현)으로 처리합니다.
        // Unity의 Persistent OnClick은 런타임 AddListener보다 먼저 실행될 수 있습니다.
        if (meditationButtonRoot != null ||
            (FindChildRecursive(transform, "MeditationButton") != null))
        {
            OnMeditationButtonClicked();
            return;
        }
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        if (SteamBattleStateSynchronizer.TryBlockSharedBattleStateEdit())
            return;

        if (IsRestActionLocked)
            return;

        EnsureSkillAwakenSelectionPanel();
        RefreshSkillAwakenOptions();

        if (skillAwakenOptions.Count == 0)
        {
            BattleWarningUI.ShowMessage(GameLocalization.Get(
                "battle.rest.no_upgradable_equipped_memory",
                GameLocalization.Get("event.memory.none_upgradable")));
            return;
        }

        if (skillAwakenSelectionPanel == null)
        {
            Debug.LogWarning("[RestRoomController] EventSkillAwakenSelectionPanelUI 없음");
            return;
        }

        SetRestActionButtonsVisible(false);
        SetNextButtonVisible(false);

        bool opened = skillAwakenSelectionPanel.Open(
            skillAwakenOptions,
            OnSkillAwakenSelected,
            OnUpgradeCancelButtonClicked,
            OnSkillAwakenPanelClosed);

        if (!opened)
        {
            BattleWarningUI.ShowMessage(GameLocalization.Get(
                "battle.rest.cannot_open_memory_upgrade"));
            if (!isRestUsed)
                SetRestActionButtonsVisible(true);
        }
    }

    public void OnUpgradeCancelButtonClicked()
    {
        skillAwakenOptions.Clear();
        hasPendingSkillAwakenResult = false;
        pendingSkillAwakenResultTarget = default;

        if (!isRestUsed)
        {
            SetRestActionButtonsVisible(true);
            SetNextButtonVisible(false);
        }
    }

    // 기존 씬/프리팹에 남아 있는 버튼 이벤트가 깨지지 않도록 유지합니다.
    public void OnTuningButtonClicked()
    {
        OnUpgradeButtonClicked();
    }

    public void OnNextButtonClicked()
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        if (SteamBattleStateSynchronizer.TryBlockSharedBattleStateEdit())
            return;

        if (!isRestUsed || nextTransitionStarted)
            return;

        EnsureNextButtonRoot();
        if (nextButtonRoot == null || !nextButtonRoot.activeInHierarchy)
            return;

        nextTransitionStarted = true;
        if (nextButton != null) nextButton.interactable = false;
        CompleteCurrentNode();

        BattleSceneController sceneController =
            Object.FindFirstObjectByType<BattleSceneController>(FindObjectsInactive.Include);

        if (sceneController != null)
            sceneController.ReturnToMap(() => SetNextButtonVisible(false));
        else
        {
            nextTransitionStarted = false;
            if (nextButton != null) nextButton.interactable = true;
            Debug.LogWarning("[RestRoomController] BattleSceneController not found");
        }
    }

    private void RecoverAllPartyHPByRatio(float ratio)
    {
        if (DataManager.Instance == null)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        float safeRatio = Mathf.Clamp01(ratio);

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            string characterId = partyStore.GetCharacterId(i);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (!DataManager.Instance.CharacterRuntimeStore.TryGet(
                    characterId,
                    out CharacterRuntimeData runtimeData))
            {
                continue;
            }

            int maxHp = Mathf.Max(0, runtimeData.MaxHP);

            if (maxHp <= 0 &&
                DataManager.Instance.CharacterDatabase.TryGet(
                    characterId,
                    out CharacterMasterData masterData))
            {
                maxHp = Mathf.Max(0, masterData.MaxHP);
                runtimeData.MaxHP = maxHp;
            }

            if (maxHp <= 0)
                continue;

            int healAmount = Mathf.CeilToInt(maxHp * safeRatio);
            healAmount = BattleEquipmentEffectService.ModifyRestHealAmountForParty(healAmount);
            runtimeData.CurrentHP = Mathf.Clamp(runtimeData.CurrentHP + healAmount, 0, maxHp);
        }

        Debug.Log($"[RestRoomController] 모든 파티원 HP {Mathf.RoundToInt(safeRatio * 100f)}% 회복 완료");
    }

    private void SpawnPartyAllies()
    {
        spawnedAllyAnimators = null;

        if (DataManager.Instance == null)
            return;

        if (allySpawnPoints == null || allySpawnPoints.Length == 0)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        CharacterPrefabDatabase prefabDatabase = DataManager.Instance.CharacterPrefabDatabase;

        if (partyStore == null || prefabDatabase == null)
            return;

        spawnedAllyAnimators = new BattleUnitAnimator[allySpawnPoints.Length];

        for (int i = 0; i < allySpawnPoints.Length; i++)
        {
            Transform point = allySpawnPoints[i];

            if (point == null)
                continue;

            ClearPoint(point);

            string characterId = partyStore.GetCharacterId(i);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (!prefabDatabase.TryGetRestPrefab(characterId, out GameObject restRoomPrefab))
            {
                Debug.LogWarning($"[RestRoomController] Rest room world prefab not found: {characterId}");
                continue;
            }

            GameObject ally = Instantiate(restRoomPrefab, point);
            ally.transform.localPosition = Vector3.zero;
            ally.transform.localRotation = Quaternion.identity;
            ally.transform.localScale = Vector3.one * Mathf.Max(0f, allySpawnScale);

            BattleMapSelectionCharacterMarker marker =
                ally.GetComponent<BattleMapSelectionCharacterMarker>();
            if (marker == null)
                marker = ally.AddComponent<BattleMapSelectionCharacterMarker>();
            if (prefabDatabase.TryGetBattlePrefab(characterId, out GameObject battlePrefab))
                marker.ConfigureTransitionAnimator(battlePrefab);

            ConfigureCharacterResourceHover(
                ally,
                i,
                MapRoomController.ResolveMovingCharacterAnchor(ally, point));
            spawnedAllyAnimators[i] = ally.GetComponentInChildren<BattleUnitAnimator>(true);
        }
    }


    private void ConfigureCharacterResourceHover(GameObject ally, int partySlotIndex, Transform worldAnchor)
    {
        if (ally == null)
            return;

        ResolveCharacterResourcesUI();
        if (characterResourcesUI == null)
            return;

        bool configured = false;

        Collider2D[] colliders2D = ally.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders2D.Length; i++)
        {
            Collider2D collider = colliders2D[i];
            if (collider == null)
                continue;

            BattleMapCharacterResourceHover hover =
                collider.GetComponent<BattleMapCharacterResourceHover>();
            if (hover == null)
                hover = collider.gameObject.AddComponent<BattleMapCharacterResourceHover>();

            hover.Configure(
                characterResourcesUI,
                partySlotIndex,
                worldAnchor != null ? worldAnchor : ally.transform,
                BattleCharacterResourcesUI.AnchorMode.Rest);
            configured = true;
        }

        Collider[] colliders = ally.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null)
                continue;

            BattleMapCharacterResourceHover hover =
                collider.GetComponent<BattleMapCharacterResourceHover>();
            if (hover == null)
                hover = collider.gameObject.AddComponent<BattleMapCharacterResourceHover>();

            hover.Configure(
                characterResourcesUI,
                partySlotIndex,
                worldAnchor != null ? worldAnchor : ally.transform,
                BattleCharacterResourcesUI.AnchorMode.Rest);
            configured = true;
        }

        if (!configured)
        {
            Debug.LogWarning(
                $"[RestRoomController] 휴식방 캐릭터에 Collider/Collider2D가 없어 자원 UI 호버를 연결할 수 없습니다: {ally.name}",
                ally);
        }
    }

    private void ResolveCharacterResourcesUI()
    {
        if (characterResourcesUI != null)
            return;

        characterResourcesUI = Object.FindFirstObjectByType<BattleCharacterResourcesUI>(FindObjectsInactive.Include);
    }

    private void PlayHealVfxOnSpawnedAllies()
    {
        CacheSpawnedAllyAnimatorsIfNeeded();
        if (spawnedAllyAnimators == null)
            return;

        // Rest room actors are spawned at their own anchors. The battle prefab's
        // VFX spawn point can have a battle-only offset, so override it only
        // while playing the heal effect and restore it immediately afterwards.
        var spawnField = typeof(BattleUnitAnimator).GetField(
            "vfxSpawnPoint",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        for (int i = 0; i < spawnedAllyAnimators.Length; i++)
        {
            BattleUnitAnimator animator = spawnedAllyAnimators[i];
            if (animator == null)
                continue;

            Transform spawn = allySpawnPoints != null && i < allySpawnPoints.Length
                ? allySpawnPoints[i] : null;
            Transform ally = spawn != null && spawn.childCount > 0 ? spawn.GetChild(0) : null;
            Transform restAnchor = ally != null ? ally : spawn;
            if (spawnField == null || restAnchor == null)
            {
                animator.PlayHeal();
                continue;
            }

            Transform originalSpawn = spawnField.GetValue(animator) as Transform;
            try
            {
                spawnField.SetValue(animator, restAnchor);
                animator.PlayHeal();
            }
            finally
            {
                spawnField.SetValue(animator, originalSpawn);
            }
        }
    }

    private void SpawnPlayerHUDs()
    {
        ClearPlayerHUDs();
        EnsurePlayerHudReferences();

        if (DataManager.Instance == null)
            return;

        if (playerHudPrefab == null || playerHudRoot == null)
            return;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;

        if (partyStore == null)
            return;

        int hudCount = Mathf.Max(0, partyStore.MaxPartyCountValue);
        spawnedPlayerHuds = new PlayerHUDSlot[hudCount];

        for (int i = 0; i < hudCount; i++)
        {
            string characterId = partyStore.GetCharacterId(i);

            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            if (!DataManager.Instance.CharacterRuntimeStore.TryGet(
                    characterId,
                    out CharacterRuntimeData runtimeData))
            {
                continue;
            }

            Transform anchor = GetPlayerHudAnchor(i);

            if (anchor == null)
                anchor = playerHudRoot;

            ClearPlayerHudAnchor(anchor);

            PlayerHUDSlot hud = Instantiate(playerHudPrefab, anchor);
            ApplyPlayerHudTransform(hud, anchor);
            hud.Bind(runtimeData);
            hud.SetCommandSelected(false);

            spawnedPlayerHuds[i] = hud;
        }
    }

    private void RefreshPlayerHUDs()
    {
        if (spawnedPlayerHuds == null || spawnedPlayerHuds.Length == 0)
        {
            SpawnPlayerHUDs();
            return;
        }

        for (int i = 0; i < spawnedPlayerHuds.Length; i++)
        {
            if (spawnedPlayerHuds[i] != null)
                spawnedPlayerHuds[i].Refresh();
        }
    }

    private void ClearPlayerHUDs()
    {
        if (spawnedPlayerHuds != null)
        {
            for (int i = spawnedPlayerHuds.Length - 1; i >= 0; i--)
            {
                if (spawnedPlayerHuds[i] != null)
                    Destroy(spawnedPlayerHuds[i].gameObject);
            }
        }

        spawnedPlayerHuds = null;

        if (playerHudPositionAnchors == null)
            return;

        for (int i = 0; i < playerHudPositionAnchors.Length; i++)
            ClearPlayerHudAnchor(playerHudPositionAnchors[i]);
    }

    private void ClearPlayerHudAnchor(Transform anchor)
    {
        if (anchor == null)
            return;

        for (int i = anchor.childCount - 1; i >= 0; i--)
        {
            Transform child = anchor.GetChild(i);

            if (child != null && child.GetComponent<PlayerHUDSlot>() != null)
                Destroy(child.gameObject);
        }
    }

    private void ApplyPlayerHudTransform(PlayerHUDSlot hud, Transform anchor)
    {
        if (hud == null || anchor == null)
            return;

        Transform hudTransform = hud.transform;

        if (hudTransform.parent != anchor)
            hudTransform.SetParent(anchor, false);

        RectTransform rect = hud.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * Mathf.Max(0f, playerHudScale);
        }

        hudTransform.localPosition = Vector3.zero;
        hudTransform.localRotation = Quaternion.identity;
        hudTransform.localScale = Vector3.one * Mathf.Max(0f, playerHudScale);
        hud.SetBaseScale(Vector3.one * Mathf.Max(0f, playerHudScale));
    }

    private Transform GetPlayerHudAnchor(int index)
    {
        EnsurePlayerHudReferences();

        if (index < 0)
            return null;

        if (playerHudPositionAnchors != null && index < playerHudPositionAnchors.Length)
        {
            Transform anchor = playerHudPositionAnchors[index];

            if (anchor != null)
                return anchor;
        }

        if (playerHudRoot != null && index < playerHudRoot.childCount)
            return playerHudRoot.GetChild(index);

        return null;
    }

    private void EnsurePlayerHudReferences()
    {
        if (!autoFindPlayerHudReferences)
            return;

        if (playerHudRoot == null)
        {
            Transform rootTransform = FindSceneTransformByName("PlayerHUD_Root");

            if (rootTransform != null)
                playerHudRoot = rootTransform;
        }

        if (playerHudRoot == null)
            return;

        if (playerHudPositionAnchors == null || playerHudPositionAnchors.Length < 3)
            playerHudPositionAnchors = new Transform[3];

        for (int i = 0; i < playerHudPositionAnchors.Length; i++)
        {
            if (playerHudPositionAnchors[i] != null)
                continue;

            string anchorName = "HUD_Pos_" + (i + 1).ToString("00");
            Transform anchor = FindDirectChildByName(playerHudRoot, anchorName);

            if (anchor != null)
                playerHudPositionAnchors[i] = anchor;
            else if (i < playerHudRoot.childCount)
                playerHudPositionAnchors[i] = playerHudRoot.GetChild(i);
        }
    }

    private Transform FindDirectChildByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child != null && child.name == targetName)
                return child;
        }

        return null;
    }

    private void CacheSpawnedAllyAnimatorsIfNeeded()
    {
        if (spawnedAllyAnimators != null && spawnedAllyAnimators.Length > 0)
            return;

        if (allySpawnPoints == null || allySpawnPoints.Length == 0)
            return;

        spawnedAllyAnimators = new BattleUnitAnimator[allySpawnPoints.Length];

        for (int i = 0; i < allySpawnPoints.Length; i++)
        {
            Transform point = allySpawnPoints[i];

            if (point != null)
                spawnedAllyAnimators[i] = point.GetComponentInChildren<BattleUnitAnimator>(true);
        }
    }

    private void ClearPoint(Transform point)
    {
        for (int i = point.childCount - 1; i >= 0; i--)
        {
            GameObject child = point.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }

    private void EnsureShopPanelSpawner()
    {
        if (shopPanel != null)
            return;

        shopPanel = GetComponentInChildren<RestRoomShopPanel>(true);

        if (shopPanel != null)
            return;

        Transform shopPanelTransform = FindSceneTransformByName("ShopPanel");

        if (shopPanelTransform == null || !shopPanelTransform.IsChildOf(transform))
            return;

        shopPanel = shopPanelTransform.GetComponent<RestRoomShopPanel>();
    }

    private void EnsureNextButtonRoot()
    {
        // The reward, event and rest rooms share the same NextButton.
        BattleRewardPanelUI rewardPanel = Object.FindFirstObjectByType<BattleRewardPanelUI>(FindObjectsInactive.Include);
        Button sharedButton = rewardPanel != null ? rewardPanel.SharedNextButton : null;
        if (sharedButton != null)
        {
            if (nextButton != null && nextButton != sharedButton)
                nextButton.onClick.RemoveListener(OnNextButtonClicked);
            nextButton = sharedButton;
            nextButtonRoot = sharedButton.gameObject;
            return;
        }

        if (nextButtonRoot == null)
        {
            Transform found = FindSceneTransformByName("NextButton");
            if (found != null) nextButtonRoot = found.gameObject;
        }
        if (nextButtonRoot != null)
            nextButton = nextButtonRoot.GetComponent<Button>();
    }

    private void BindNextButton()
    {
        EnsureNextButtonRoot();
        if (nextButton == null) return;
        nextButton.onClick.RemoveListener(OnNextButtonClicked);
        nextButton.onClick.AddListener(OnNextButtonClicked);
    }

    private void UnbindNextButton()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNextButtonClicked);
    }

    private void SetNextButtonVisible(bool visible)
    {
        EnsureNextButtonRoot();
        if (nextButtonRoot == null) return;
        if (visible)
        {
            BindNextButton();
            if (nextButton != null) nextButton.interactable = !nextTransitionStarted;
            TMP_Text label = nextButtonRoot.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = GameLocalization.Get("common.proceed", "진행");
        }
        nextButtonRoot.SetActive(visible);
    }

    private void EnsureSelectedTextReferences()
    {
        if (selectedTextRoot == null)
        {
            Transform found = FindSceneTransformByName("Selected_Text");
            if (found != null && found.IsChildOf(transform)) selectedTextRoot = found.gameObject;
        }
        if (selectedTextRoot == null) return;
        if (selectedHealRoot == null)
        {
            Transform child = selectedTextRoot.transform.Find("Heal");
            if (child != null) selectedHealRoot = child.gameObject;
        }
        if (selectedMeditationRoot == null)
        {
            Transform child = selectedTextRoot.transform.Find("Meditation");
            if (child != null) selectedMeditationRoot = child.gameObject;
        }
    }

    private void ResetSelectedText()
    {
        EnsureSelectedTextReferences();
        if (selectedHealRoot != null) selectedHealRoot.SetActive(false);
        if (selectedMeditationRoot != null) selectedMeditationRoot.SetActive(false);
        if (selectedTextRoot != null) selectedTextRoot.SetActive(false);
    }

    private void ShowSelectedText(bool meditation)
    {
        EnsureSelectedTextReferences();
        if (selectedTextRoot != null) selectedTextRoot.SetActive(true);
        if (selectedHealRoot != null) selectedHealRoot.SetActive(!meditation);
        if (selectedMeditationRoot != null) selectedMeditationRoot.SetActive(meditation);
    }

    private void FadeOutRestActionButtons(bool meditation)
    {
        if (actionButtonFadeRoutine != null) StopCoroutine(actionButtonFadeRoutine);
        actionButtonFadeRoutine = StartCoroutine(FadeOutRestActionButtonsRoutine(meditation));
    }

    private readonly Dictionary<Button, Selectable.Transition> restButtonTransitions = new Dictionary<Button, Selectable.Transition>();

    private void RestoreRestButtonTransitions()
    {
        foreach (var pair in restButtonTransitions)
            if (pair.Key != null) pair.Key.transition = pair.Value;
        restButtonTransitions.Clear();
    }

    private IEnumerator FadeOutRestActionButtonsRoutine(bool meditation)
    {
        GameObject[] roots = { healButtonRoot, meditationButtonRoot };
        CanvasGroup[] groups = new CanvasGroup[roots.Length];
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null) continue;
            groups[i] = roots[i].GetComponent<CanvasGroup>();
            if (groups[i] == null) groups[i] = roots[i].AddComponent<CanvasGroup>();
            groups[i].interactable = false;
            groups[i].blocksRaycasts = false;
            // Keep the authored colors intact while fading; do not apply Button.disabledColor.
            Button uiButton = roots[i].GetComponent<Button>();
            if (uiButton != null)
            {
                if (!restButtonTransitions.ContainsKey(uiButton)) restButtonTransitions.Add(uiButton, uiButton.transition);
                uiButton.transition = Selectable.Transition.None;
            }
        }
        float elapsed = 0f;
        float duration = Mathf.Max(0f, actionButtonFadeDuration);
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / duration);
            foreach (CanvasGroup group in groups) if (group != null) group.alpha = alpha;
            yield return null;
        }
        SetRestActionButtonsVisible(false);
        SetHealTextVisibleImmediate(false);
        SetMeditationTextVisible(false);
        actionButtonFadeRoutine = null;
        // Meditation is completed only when Skill_panel's close callback fires.
        if (!meditation) ShowSelectedText(false);
    }

    private void SetRestActionButtonsVisible(bool visible)
    {
        EnsureUpgradeButtonReferences();
        EnsureHealButtonReferences();

        if (upgradeButtonRoot != null && upgradeButtonRoot != meditationButtonRoot)
            upgradeButtonRoot.SetActive(false);

        if (healButtonRoot != null)
        {
            CanvasGroup group = healButtonRoot.GetComponent<CanvasGroup>();
            if (group != null) { group.alpha = visible ? 1f : 0f; group.interactable = visible; group.blocksRaycasts = visible; }
            healButtonRoot.SetActive(visible);
        }
        if (meditationButtonRoot != null)
        {
            CanvasGroup group = meditationButtonRoot.GetComponent<CanvasGroup>();
            if (group != null) { group.alpha = visible ? 1f : 0f; group.interactable = visible; group.blocksRaycasts = visible; }
            meditationButtonRoot.SetActive(visible);
        }
    }

    private void EnsureUpgradeButtonReferences()
    {
        if (upgradeButtonRoot == null)
        {
            Transform upgradeButtonTransform = FindSceneTransformByName("UpgradeButton");

            if (upgradeButtonTransform != null)
                upgradeButtonRoot = upgradeButtonTransform.gameObject;
        }

        if (upgradeButtonRoot == null)
            return;

        if (upgradeButtonBackImage == null)
        {
            Transform backTransform = FindChildRecursive(upgradeButtonRoot.transform, "back");

            if (backTransform != null)
                upgradeButtonBackImage = backTransform.GetComponent<Image>();
        }

        if (upgradeButtonBackImage == null)
            upgradeButtonBackImage = upgradeButtonRoot.GetComponentInChildren<Image>(true);

        if (upgradeButtonText == null)
        {
            Transform textTransform = FindChildRecursive(upgradeButtonRoot.transform, "Text (TMP)");

            if (textTransform != null)
                upgradeButtonText = textTransform.GetComponent<TMP_Text>();
        }

        if (upgradeButtonText == null)
            upgradeButtonText = upgradeButtonRoot.GetComponentInChildren<TMP_Text>(true);

        CacheUpgradeButtonDefaultColorsIfNeeded();
    }

    private void CacheUpgradeButtonDefaultColorsIfNeeded()
    {
        if (hasUpgradeButtonDefaultColors)
            return;

        if (upgradeButtonBackImage != null)
            upgradeButtonBackDefaultColor = upgradeButtonBackImage.color;

        if (upgradeButtonText != null)
            upgradeButtonTextDefaultColor = upgradeButtonText.color;

        hasUpgradeButtonDefaultColors = true;
    }

    private void SetUpgradeButtonDisabledFeedback(bool disabled)
    {
        EnsureUpgradeButtonReferences();

        if (upgradeButtonRoot == null)
            return;

        if (upgradeButtonBackImage != null)
            upgradeButtonBackImage.color = disabled ? disabledUpgradeButtonColor : upgradeButtonBackDefaultColor;

        if (upgradeButtonText != null)
            upgradeButtonText.color = disabled ? disabledUpgradeButtonColor : upgradeButtonTextDefaultColor;

        Button button = upgradeButtonRoot.GetComponent<Button>();

        if (button != null)
            button.interactable = !disabled;

        Collider2D collider2d = upgradeButtonRoot.GetComponent<Collider2D>();

        if (collider2d != null)
            collider2d.enabled = !disabled;

        MonoBehaviour[] behaviours = upgradeButtonRoot.GetComponents<MonoBehaviour>();

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];

            if (behaviour == null || behaviour == this)
                continue;

            string behaviourName = behaviour.GetType().Name;

            if (behaviourName.Contains("Clunk") ||
                behaviourName.Contains("Animation") ||
                behaviourName.Contains("Hover") ||
                behaviourName.Contains("Scale"))
            {
                behaviour.enabled = !disabled;
            }
        }
    }

    private void BindHealButton()
    {
        EnsureHealButtonReferences();
        Button button = healButtonRoot != null ? healButtonRoot.GetComponent<Button>() : null;
        if (boundHealButton != null && boundHealButton != button)
            boundHealButton.onClick.RemoveListener(OnRestButtonClicked);
        boundHealButton = button;
        if (boundHealButton != null)
        {
            boundHealButton.onClick.RemoveListener(OnRestButtonClicked);
            boundHealButton.onClick.AddListener(OnRestButtonClicked);
        }
        else
            Debug.LogWarning("[RestRoomController] HealButton requires a Button component.", this);
    }

    private void UnbindHealButton()
    {
        if (boundHealButton != null)
            boundHealButton.onClick.RemoveListener(OnRestButtonClicked);
        boundHealButton = null;
    }

    private int[] CapturePartyHPBeforeRest()
    {
        int[] before = new int[3];
        DataManager manager = DataManager.Instance;
        if (manager?.PartyRuntimeStore == null || manager.CharacterRuntimeStore == null)
            return before;
        for (int i = 0; i < before.Length; i++)
        {
            string id = manager.PartyRuntimeStore.GetCharacterId(i);
            if (!string.IsNullOrWhiteSpace(id) &&
                manager.CharacterRuntimeStore.TryGet(id, out CharacterRuntimeData character) &&
                character != null)
                before[i] = character.CurrentHP;
        }
        return before;
    }

    private void ShowRestHealPresentation(int[] before)
    {
        DataManager manager = DataManager.Instance;
        if (manager?.PartyRuntimeStore == null || manager.CharacterRuntimeStore == null || before == null)
            return;

        Transform[] anchors = new Transform[3];
        ResolveCharacterResourcesUI();
        EnsureRestResourceCanvasActive();
        Canvas canvas = characterResourcesUI != null
            ? characterResourcesUI.GetComponentInParent<Canvas>(true)
            : GetComponentInParent<Canvas>(true);
        for (int i = 0; i < before.Length; i++)
        {
            string id = manager.PartyRuntimeStore.GetCharacterId(i);
            if (string.IsNullOrWhiteSpace(id) ||
                !manager.CharacterRuntimeStore.TryGet(id, out CharacterRuntimeData character) ||
                character == null)
                continue;

            Transform spawn = allySpawnPoints != null && i < allySpawnPoints.Length
                ? allySpawnPoints[i] : null;
            if (spawn == null)
                continue;
            Transform ally = spawn.childCount > 0 ? spawn.GetChild(0) : null;
            anchors[i] = ally != null ? MapRoomController.ResolveMovingCharacterAnchor(ally.gameObject, spawn) : spawn;
            // Show the calculated healing amount, not the capped HP delta.
            // This feedback remains visible even when the character is at full HP.
            int maxHp = Mathf.Max(0, character.MaxHP);
            if (maxHp <= 0 && manager.CharacterDatabase != null &&
                manager.CharacterDatabase.TryGet(id, out CharacterMasterData masterData))
                maxHp = Mathf.Max(0, masterData.MaxHP);
            if (maxHp > 0)
            {
                int healAmount = Mathf.CeilToInt(maxHp * Mathf.Clamp01(healHpRatio));
                healAmount = BattleEquipmentEffectService.ModifyRestHealAmountForParty(healAmount);
                if (healAmount > 0)
                    BattleDamageTextPopupUI.ShowEventHealthRecovery(anchors[i], healAmount, canvas, 48f);
            }
        }

        // Display party HUD even if every member is at full health.
        // Zero-value popup text remains suppressed by the earlier restored check.
        if (characterResourcesUI != null && characterResourcesUI.gameObject.activeInHierarchy)
            characterResourcesUI.ShowEventHealthChangeAll(anchors, before, useRestPositions: true);
    }

    // Character Resources may live under an inactive MenuRoot. Activating only
    // the child is not sufficient; place it directly beneath the active UI viewport
    // so rest-room recovery can display without opening the menu itself.
    // Keep the scene hierarchy stable across repeated RestRoom entries.
    private Transform restResourceOriginalParent;
    private int restResourceOriginalSiblingIndex = -1;
    private bool restResourceMovedByRest;

    private void RestoreRestResourceCanvasParent()
    {
        if (!restResourceMovedByRest)
            return;
        restResourceMovedByRest = false;
        if (characterResourcesUI == null || restResourceOriginalParent == null)
            return;
        Transform resource = characterResourcesUI.transform;
        resource.SetParent(restResourceOriginalParent, false);
        if (restResourceOriginalSiblingIndex >= 0)
            resource.SetSiblingIndex(Mathf.Min(restResourceOriginalSiblingIndex, resource.parent.childCount - 1));
        restResourceOriginalParent = null;
        restResourceOriginalSiblingIndex = -1;
        // The UI controller hides its own child panels after each recovery.
        characterResourcesUI.HideAll();
    }

    private void EnsureRestResourceCanvasActive()
    {
        if (characterResourcesUI == null)
            return;

        Transform resource = characterResourcesUI.transform;
        if (!characterResourcesUI.gameObject.activeInHierarchy)
        {
            Canvas canvas = resource.GetComponentInParent<Canvas>(true);
            Transform activeParent = canvas != null ? canvas.transform : null;
            if (canvas != null)
            {
                Transform viewport = canvas.transform.Find("Resolution Viewport");
                if (viewport != null && viewport.gameObject.activeInHierarchy)
                    activeParent = viewport;
            }
            if (activeParent != null && activeParent.gameObject.activeInHierarchy && resource.parent != activeParent)
            {
                if (!restResourceMovedByRest)
                {
                    restResourceOriginalParent = resource.parent;
                    restResourceOriginalSiblingIndex = resource.GetSiblingIndex();
                }
                restResourceMovedByRest = true;
                resource.SetParent(activeParent, false);
            }
        }
        if (!characterResourcesUI.gameObject.activeSelf)
            characterResourcesUI.gameObject.SetActive(true);
    }

    private void EnsureHealButtonReferences()
    {
        if (healButtonRoot == null)
        {
            Transform healButtonTransform = FindSceneTransformByName("HealButton");

            if (healButtonTransform != null)
                healButtonRoot = healButtonTransform.gameObject;
        }

        if (healButtonRoot == null)
            return;

        if (healButtonBackImage == null)
        {
            Transform backTransform = FindChildRecursive(healButtonRoot.transform, "back");

            if (backTransform != null)
                healButtonBackImage = backTransform.GetComponent<Image>();
        }

        if (healButtonBackImage == null)
            healButtonBackImage = healButtonRoot.GetComponentInChildren<Image>(true);

        if (healButtonText == null)
        {
            Transform textTransform = FindChildRecursive(healButtonRoot.transform, "Text (TMP)");

            if (textTransform != null)
                healButtonText = textTransform.GetComponent<TMP_Text>();
        }

        if (healButtonText == null)
            healButtonText = healButtonRoot.GetComponentInChildren<TMP_Text>(true);

        CacheHealButtonDefaultColorsIfNeeded();
    }

    private void CacheHealButtonDefaultColorsIfNeeded()
    {
        if (hasHealButtonDefaultColors)
            return;

        if (healButtonBackImage != null)
            healButtonBackDefaultColor = healButtonBackImage.color;

        if (healButtonText != null)
            healButtonTextDefaultColor = healButtonText.color;

        hasHealButtonDefaultColors = true;
    }

    private void SetHealButtonDisabledFeedback(bool disabled)
    {
        EnsureHealButtonReferences();

        if (healButtonRoot == null)
            return;

        if (healButtonBackImage != null)
            healButtonBackImage.color = disabled ? disabledHealButtonColor : healButtonBackDefaultColor;

        if (healButtonText != null)
            healButtonText.color = disabled ? disabledHealButtonColor : healButtonTextDefaultColor;

        Button button = healButtonRoot.GetComponent<Button>();

        if (button != null)
            button.interactable = !disabled;

        Collider2D collider2d = healButtonRoot.GetComponent<Collider2D>();

        if (collider2d != null)
            collider2d.enabled = !disabled;

        MonoBehaviour[] behaviours = healButtonRoot.GetComponents<MonoBehaviour>();

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];

            if (behaviour == null || behaviour == this)
                continue;

            string behaviourName = behaviour.GetType().Name;

            if (behaviourName.Contains("Clunk") ||
                behaviourName.Contains("Animation") ||
                behaviourName.Contains("Hover") ||
                behaviourName.Contains("Scale"))
            {
                behaviour.enabled = !disabled;
            }
        }
    }


    private void PrepareHealEffectRoot()
    {
        if (healEffectRoot == null)
        {
            // Only search within this RestRoom; never select another room's Heal effect.
            Transform found = FindChildRecursive(transform, "Heal");
            if (found != null && found != transform &&
                (healButtonRoot == null || !found.IsChildOf(healButtonRoot.transform)))
                healEffectRoot = found.gameObject;
        }
        if (healEffectRoot == null) return;

        if (!healEffectPositionCaptured)
        {
            // Heal is a regular Transform, not a RectTransform.
            // Its authored rest position is Y = 0, independent of button hover.
            healEffectTransform = healEffectRoot.transform;
            Vector3 position = healEffectTransform.localPosition;
            healEffectOriginalLocalPosition = new Vector3(position.x, 0f, position.z);
            healEffectPositionCaptured = true;
        }

        RestoreHealEffectPosition();

        // A button hover transform must never move the Heal animation root.
        RectTransform effectRect = healEffectRoot.GetComponent<RectTransform>();
        if (effectRect != null)
        {
            UIChildHoverTransform[] hovers = GetComponentsInChildren<UIChildHoverTransform>(true);
            for (int i = 0; i < hovers.Length; i++)
                hovers[i]?.ExcludeTarget(effectRect);
        }
        if (healEffectRoutine == null)
            healEffectRoot.SetActive(false);
    }

    private void RestoreHealEffectPosition()
    {
        if (healEffectPositionCaptured && healEffectTransform != null)
            healEffectTransform.localPosition = healEffectOriginalLocalPosition;
    }

    private void LateUpdate()
    {
        // The reward panel shares NextButton and may overwrite its label.
        // Keep the rest-room action localized to the proceed caption.
        if (isActiveAndEnabled && nextButtonRoot != null && nextButtonRoot.activeInHierarchy)
        {
            TMP_Text nextLabel = nextButton != null ? nextButton.GetComponentInChildren<TMP_Text>(true) : null;
            if (nextLabel != null)
            {
                string proceed = GameLocalization.Get("common.proceed", "진행");
                if (nextLabel.text != proceed) nextLabel.text = proceed;
            }
        }
        if (healEffectRoot == null || !healEffectPositionCaptured)
            return;

        // Animations on parent objects can still write Heal.position/CanvasGroup
        // even when Heal has no Animator of its own. Keep the effect at the
        // fixed RestRoom position, and show it only during click playback.
        RestoreHealEffectPosition();
        if (healEffectRoutine == null)
        {
            if (healEffectRoot.activeSelf)
                healEffectRoot.SetActive(false);
        }
        else
        {
            if (!healEffectRoot.activeSelf)
                healEffectRoot.SetActive(true);
            CanvasGroup group = healEffectRoot.GetComponent<CanvasGroup>();
            if (group != null && group.alpha != 1f)
                group.alpha = 1f;
        }
    }

    private void PlayHealEffectRoot()
    {
        PrepareHealEffectRoot();
        if (healEffectRoot == null) return;
        if (healEffectRoutine != null)
            StopCoroutine(healEffectRoutine);
        RestoreHealEffectPosition();
        healEffectRoot.SetActive(true);
        RestoreHealEffectPosition();
        CanvasGroup group = healEffectRoot.GetComponent<CanvasGroup>();
        if (group != null) group.alpha = 1f;
        Animator[] animators = healEffectRoot.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator == null || animator.runtimeAnimatorController == null) continue;
            animator.Rebind();
            if (animator.isActiveAndEnabled) animator.Update(0f);
        }
        RestoreHealEffectPosition();
        healEffectRoutine = StartCoroutine(HideHealEffectAfterDelay());
    }

    private IEnumerator HideHealEffectAfterDelay()
    {
        yield return new WaitForSecondsRealtime(healEffectDuration);
        healEffectRoutine = null;
        if (healEffectRoot != null) healEffectRoot.SetActive(false);
    }

    private void StopHealEffect()
    {
        if (healEffectRoutine != null)
        {
            StopCoroutine(healEffectRoutine);
            healEffectRoutine = null;
        }
        if (healEffectRoot != null) healEffectRoot.SetActive(false);
    }

    private void EnsureHealTextReferences()
    {
        if (healTextRoot == null)
        {
            Transform found = FindSceneTransformByName("Heal_Text");
            if (found != null && found.IsChildOf(transform))
                healTextRoot = found.gameObject;
        }

        if (healTextRoot == null || healButtonRoot == null)
            return;

        // Preserve the designer-authored anchored position, and display by
        // SetActive rather than by CanvasGroup opacity or a Y animation.
        RestHealTextHoverToggle hover = healButtonRoot.GetComponent<RestHealTextHoverToggle>();
        if (hover == null)
            hover = healButtonRoot.AddComponent<RestHealTextHoverToggle>();
        hover.Configure(healTextRoot, hoverFadeDuration);
    }

    private void SetHealTextVisibleImmediate(bool visible)
    {
        EnsureHealTextReferences();
        if (healTextRoot != null)
        {
            if (!healTextRoot.activeSelf) healTextRoot.SetActive(true);
            CanvasGroup group = healTextRoot.GetComponent<CanvasGroup>();
            if (group == null) group = healTextRoot.AddComponent<CanvasGroup>();
            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = false;
        }
    }

    private void EnsureMeditationReferences()
    {
        if (meditationButtonRoot == null)
        {
            Transform found = FindSceneTransformByName("MeditationButton");
            if (found != null && found.IsChildOf(transform)) meditationButtonRoot = found.gameObject;
        }
        if (meditationTextRoot == null)
        {
            Transform found = FindSceneTransformByName("Meditation_Text");
            if (found != null && found.IsChildOf(transform)) meditationTextRoot = found.gameObject;
        }
        if (meditationButtonRoot != null && meditationTextRoot != null)
        {
            RestHealTextHoverToggle hover = meditationButtonRoot.GetComponent<RestHealTextHoverToggle>();
            if (hover == null) hover = meditationButtonRoot.AddComponent<RestHealTextHoverToggle>();
            hover.Configure(meditationTextRoot, hoverFadeDuration);
        }
        if (meditationSkillPanel == null) meditationSkillPanel = BattleRewardSkillPanelUI.FindPanel();
    }

    private void SetMeditationTextVisible(bool visible)
    {
        if (meditationTextRoot == null) return;
        if (!meditationTextRoot.activeSelf) meditationTextRoot.SetActive(true);
        CanvasGroup group = meditationTextRoot.GetComponent<CanvasGroup>();
        if (group == null) group = meditationTextRoot.AddComponent<CanvasGroup>();
        group.alpha = visible ? 1f : 0f;
        group.blocksRaycasts = false;
    }

    private void BindMeditationButton()
    {
        Button button = meditationButtonRoot != null ? meditationButtonRoot.GetComponent<Button>() : null;
        if (boundMeditationButton != null) boundMeditationButton.onClick.RemoveListener(OnMeditationButtonClicked);
        boundMeditationButton = button;
        if (button != null) button.onClick.AddListener(OnMeditationButtonClicked);
    }

    private void UnbindMeditationButton()
    {
        if (boundMeditationButton != null) boundMeditationButton.onClick.RemoveListener(OnMeditationButtonClicked);
        boundMeditationButton = null;
    }

    private string GetRestEffectiveHealPercentText()
    {
        // Same equipment modifier as the actual HP restoration calculation.
        const int sampleMaxHp = 10000;
        int baseHeal = Mathf.CeilToInt(sampleMaxHp * Mathf.Clamp01(healHpRatio));
        int effectiveHeal = BattleEquipmentEffectService.ModifyRestHealAmountForParty(baseHeal);
        float percent = effectiveHeal * 100f / sampleMaxHp;
        return Mathf.Approximately(percent, Mathf.Round(percent))
            ? Mathf.RoundToInt(percent).ToString()
            : percent.ToString("0.#");
    }

    private void UpdateRestDescription()
    {
        if (healTextRoot == null) return;
        // 회복량 증가 유물의 실제 계산 규칙과 동일하게 기준 생명력에 대한 증가율을 계산합니다.
        string fallback = $"생명력을 {GetRestEffectiveHealPercentText()}% 회복하고 침식도가 {restErosionReduction} 감소합니다.";
        TMP_Text text = healTextRoot.GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.text = fallback;
    }

    public void OnMeditationButtonClicked()
    {
        if (UIPanelButton.IsMenuPanelOpen || SteamBattleStateSynchronizer.TryBlockSharedBattleStateEdit() || IsRestActionLocked || restChoicePending)
            return;
        EnsureMeditationReferences();
        if (meditationSkillPanel == null) return;
        DataManager dm = DataManager.Instance;
        if (dm?.PartyRuntimeStore == null || dm.SkillDatabase == null || dm.CharacterRuntimeStore == null) return;

        List<SkillMasterData> offers = new List<SkillMasterData>();
        for (int slot = 0; slot < 3; slot++)
        {
            string id = dm.PartyRuntimeStore.GetCharacterId(slot);
            if (string.IsNullOrWhiteSpace(id) || !dm.CharacterRuntimeStore.TryGet(id, out CharacterRuntimeData character))
                return;
            HashSet<string> equipped = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            if (character.EquippedSkillIds != null)
                foreach (string skillId in character.EquippedSkillIds)
                    if (!string.IsNullOrWhiteSpace(skillId)) equipped.Add(skillId.Trim());
            List<SkillMasterData> candidates = new List<SkillMasterData>();
            foreach (SkillMasterData skill in dm.SkillDatabase.GetAll())
                if (skill != null && skill.Category == Category.Core &&
                    SkillRarityUtility.IsCoreDropRarity(skill.Rarity) &&
                    SkillRarityUtility.IsBaseSkillVariant(skill.SkillId) &&
                    !equipped.Contains(skill.SkillId) && SkillOwnershipPolicy.CanEquip(skill, id))
                    candidates.Add(skill);
            if (candidates.Count == 0) { BattleWarningUI.ShowMessage("발현할 기억이 없습니다."); return; }
            // 각 레어도의 기본 확률은 30/30/30/10. 후보가 없는 레어도의 가중치는
            // 남아 있는 레어도에 비례해 재분배되며, 같은 레어도 내에서 균등 추첨합니다.
            float totalWeight = 0f;
            foreach (SkillRarity rarity in new[] { SkillRarity.Common, SkillRarity.Rare, SkillRarity.Epic, SkillRarity.Unique })
                if (candidates.Exists(x => x.Rarity == rarity)) totalWeight += rarity == SkillRarity.Unique ? 10f : 30f;
            float roll = UnityEngine.Random.value * totalWeight;
            SkillRarity selectedRarity = candidates[0].Rarity;
            foreach (SkillRarity rarity in new[] { SkillRarity.Common, SkillRarity.Rare, SkillRarity.Epic, SkillRarity.Unique })
            {
                if (!candidates.Exists(x => x.Rarity == rarity)) continue;
                selectedRarity = rarity;
                roll -= rarity == SkillRarity.Unique ? 10f : 30f;
                if (roll <= 0f) break;
            }
            List<SkillMasterData> sameRarity = candidates.FindAll(x => x.Rarity == selectedRarity);
            offers.Add(sameRarity[UnityEngine.Random.Range(0, sameRarity.Count)]);
        }
        BattleRewardData reward = new BattleRewardData
        {
            Type = BattleRewardType.Skill,
            RewardId = offers[0].SkillId,
            SourceKey = "RestMeditation|RestOffers=" + string.Join(",", offers.ConvertAll(x => x.SkillId)) + "|"
        };
        if (!meditationSkillPanel.Open(reward, () =>
        {
            isRestUsed = true;
            restChoicePending = false;
            ShowSelectedText(true);
            SetNextButtonVisible(true);
        })) BattleWarningUI.ShowMessage("기억 선택 패널을 열 수 없습니다.");
        else
        {
            restChoicePending = true;
            FadeOutRestActionButtons(true);
            SetNextButtonVisible(false);
        }
    }

    private void EnsureSkillAwakenSelectionPanel()
    {
        if (skillAwakenSelectionPanel != null)
            return;

        skillAwakenSelectionPanel = GetComponentInChildren<EventSkillAwakenSelectionPanelUI>(true);

        if (skillAwakenSelectionPanel != null)
            return;

        Transform panelTransform = FindSceneTransformByName("SkillAwakenSelectionPanel");
        if (panelTransform != null && panelTransform.IsChildOf(transform))
            skillAwakenSelectionPanel = panelTransform.GetComponent<EventSkillAwakenSelectionPanelUI>();
    }

    private void RefreshSkillAwakenOptions()
    {
        skillAwakenOptions.Clear();

        foreach (CharacterRuntimeData character in EnumeratePartyCharacters())
        {
            if (character == null || string.IsNullOrWhiteSpace(character.CharacterId))
                continue;

            SkillInventoryEquipService.EnsureEquippedSkillArray(character);
            AddSkillAwakenOption(character, EventChoiceSkillSlotKind.Passive, -1, character.PassiveSkillId);
            AddSkillAwakenOption(character, EventChoiceSkillSlotKind.Unique, 0, character.UniqueSkillId);
            AddSkillAwakenOption(character, EventChoiceSkillSlotKind.Ability, 1, character.AbilitySkillId);

            if (character.EquippedSkillIds == null)
                continue;

            for (int slotIndex = 2; slotIndex < character.EquippedSkillIds.Length; slotIndex++)
            {
                AddSkillAwakenOption(
                    character,
                    EventChoiceSkillSlotKind.Equipped,
                    slotIndex,
                    character.EquippedSkillIds[slotIndex]);
            }
        }
    }

    private void AddSkillAwakenOption(
        CharacterRuntimeData character,
        EventChoiceSkillSlotKind slotKind,
        int slotIndex,
        string skillId)
    {
        if (character == null || string.IsNullOrWhiteSpace(character.CharacterId) || string.IsNullOrWhiteSpace(skillId))
            return;

        if (!TryGetUpgradeableSkill(skillId, out string normalizedSkillId, out string upgradeSkillId))
            return;

        EventChoiceSkillAwakenTarget target = new(
            character.CharacterId,
            slotKind,
            slotIndex,
            normalizedSkillId,
            upgradeSkillId);

        DataManager.Instance.SkillDatabase.TryGet(normalizedSkillId, out SkillMasterData currentSkill);

        skillAwakenOptions.Add(new EventSkillAwakenSelectionPanelEntry(
            target,
            GetCharacterDisplayName(character.CharacterId),
            GetSkillSlotDisplayName(slotKind, slotIndex),
            GetSkillDisplayName(normalizedSkillId),
            GetSkillDisplayName(upgradeSkillId),
            GetSkillSprite(normalizedSkillId, currentSkill)));
    }

    private bool OnSkillAwakenSelected(EventChoiceSkillAwakenTarget target)
    {
        if (SteamBattleStateSynchronizer.TryBlockSharedBattleStateEdit())
            return false;

        if (IsRestActionLocked)
            return false;

        if (!TryUpgradeSelectedSkill(target, out string resultMessage))
        {
            if (!string.IsNullOrWhiteSpace(resultMessage))
                BattleWarningUI.ShowMessage(resultMessage);
            return false;
        }

        isRestUsed = true;
        hasPendingSkillAwakenResult = true;
        pendingSkillAwakenResultTarget = target;
        skillAwakenOptions.Clear();

        RefreshPlayerHUDs();
        SetHealButtonDisabledFeedback(true);
        SetUpgradeButtonDisabledFeedback(true);
        SetRestActionButtonsVisible(false);
        SetNextButtonVisible(false);
        return true;
    }

    private void OnSkillAwakenPanelClosed()
    {
        if (!hasPendingSkillAwakenResult)
        {
            if (!isRestUsed)
                SetRestActionButtonsVisible(true);
            return;
        }

        if (!isActiveAndEnabled)
            return;

        if (skillAwakenResultRoutine != null)
            StopCoroutine(skillAwakenResultRoutine);

        skillAwakenResultRoutine = StartCoroutine(PlaySkillAwakenResultRoutine());
    }

    private IEnumerator PlaySkillAwakenResultRoutine()
    {
        EventChoiceSkillAwakenTarget target = pendingSkillAwakenResultTarget;

        if (skillAwakenSelectionPanel != null && target.IsValid)
            yield return skillAwakenSelectionPanel.PlayResultSkill(target, true);

        hasPendingSkillAwakenResult = false;
        pendingSkillAwakenResultTarget = default;
        skillAwakenResultRoutine = null;
        SetRestActionButtonsVisible(false);
        SetNextButtonVisible(true);
    }

    private bool TryUpgradeSelectedSkill(
        EventChoiceSkillAwakenTarget target,
        out string resultMessage)
    {
        resultMessage = string.Empty;

        if (!target.IsValid)
        {
            resultMessage = GameLocalization.Get("event.memory.select_upgrade_required");
            return false;
        }

        CharacterRuntimeStore characterStore = DataManager.Instance?.CharacterRuntimeStore;
        if (characterStore == null ||
            !characterStore.TryGet(target.CharacterId, out CharacterRuntimeData character) ||
            character == null)
        {
            resultMessage = "선택한 캐릭터를 찾을 수 없습니다.";
            return false;
        }

        SkillInventoryEquipService.EnsureEquippedSkillArray(character);

        if (!TryReadSkillFromAwakenTarget(character, target, out string currentSkillId) ||
            !IsSameId(currentSkillId, target.SkillId))
        {
            resultMessage = "선택한 기억 장착 상태가 변경되었습니다.";
            return false;
        }

        if (!TryGetUpgradeableSkill(currentSkillId, out _, out string upgradeSkillId) ||
            !IsSameId(upgradeSkillId, target.UpgradeSkillId))
        {
            resultMessage = "선택한 기억은 강화할 수 없습니다.";
            return false;
        }

        if (!TryApplySelectedSkillUpgrade(character, target, upgradeSkillId))
        {
            resultMessage = "선택한 기억을 강화하지 못했습니다.";
            return false;
        }

        characterStore.AddOrUpdate(character);
        EquippedSkillPanelUI.RefreshAll();
        SkillInventoryPanelUI.RefreshAll();
        resultMessage = $"기억 강화: {GetSkillDisplayName(upgradeSkillId)}";
        return true;
    }

    private bool TryApplySelectedSkillUpgrade(
        CharacterRuntimeData character,
        EventChoiceSkillAwakenTarget target,
        string upgradeSkillId)
    {
        switch (target.SlotKind)
        {
            case EventChoiceSkillSlotKind.Passive:
                if (!IsSameId(character.PassiveSkillId, target.SkillId))
                    return false;

                character.PassiveSkillId = upgradeSkillId;
                return true;

            case EventChoiceSkillSlotKind.Unique:
                if (!IsSameId(character.UniqueSkillId, target.SkillId))
                    return false;

                character.UniqueSkillId = upgradeSkillId;
                ReplaceMirroredEquippedSkill(character, 0, target.SkillId, upgradeSkillId);
                return true;

            case EventChoiceSkillSlotKind.Ability:
                if (!IsSameId(character.AbilitySkillId, target.SkillId))
                    return false;

                character.AbilitySkillId = upgradeSkillId;
                ReplaceMirroredEquippedSkill(character, 1, target.SkillId, upgradeSkillId);
                return true;

            case EventChoiceSkillSlotKind.Equipped:
                if (character.EquippedSkillIds == null ||
                    target.SlotIndex < 0 ||
                    target.SlotIndex >= character.EquippedSkillIds.Length ||
                    !IsSameId(character.EquippedSkillIds[target.SlotIndex], target.SkillId))
                {
                    return false;
                }

                character.EquippedSkillIds[target.SlotIndex] = upgradeSkillId;
                return true;

            default:
                return false;
        }
    }

    private static void ReplaceMirroredEquippedSkill(
        CharacterRuntimeData character,
        int slotIndex,
        string currentSkillId,
        string upgradeSkillId)
    {
        if (character?.EquippedSkillIds == null ||
            slotIndex < 0 ||
            slotIndex >= character.EquippedSkillIds.Length ||
            !IsSameId(character.EquippedSkillIds[slotIndex], currentSkillId))
        {
            return;
        }

        character.EquippedSkillIds[slotIndex] = upgradeSkillId;
    }

    private static bool TryReadSkillFromAwakenTarget(
        CharacterRuntimeData character,
        EventChoiceSkillAwakenTarget target,
        out string skillId)
    {
        skillId = string.Empty;

        if (character == null)
            return false;

        switch (target.SlotKind)
        {
            case EventChoiceSkillSlotKind.Passive:
                skillId = character.PassiveSkillId;
                return true;

            case EventChoiceSkillSlotKind.Unique:
                skillId = character.UniqueSkillId;
                return true;

            case EventChoiceSkillSlotKind.Ability:
                skillId = character.AbilitySkillId;
                return true;

            case EventChoiceSkillSlotKind.Equipped:
                if (character.EquippedSkillIds == null ||
                    target.SlotIndex < 0 ||
                    target.SlotIndex >= character.EquippedSkillIds.Length)
                {
                    return false;
                }

                skillId = character.EquippedSkillIds[target.SlotIndex];
                return true;

            default:
                return false;
        }
    }

    private bool TryGetUpgradeableSkill(
        string skillId,
        out string normalizedSkillId,
        out string upgradeSkillId)
    {
        normalizedSkillId = string.IsNullOrWhiteSpace(skillId) ? string.Empty : skillId.Trim();
        upgradeSkillId = string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedSkillId) ||
            SkillRarityUtility.IsUpgradeSkillVariant(normalizedSkillId) ||
            DataManager.Instance == null ||
            DataManager.Instance.SkillDatabase == null ||
            !DataManager.Instance.SkillDatabase.TryGet(normalizedSkillId, out SkillMasterData skill) ||
            !SkillRarityUtility.CanUpgrade(skill) ||
            !SkillRarityUtility.TryGetPairedVariantId(normalizedSkillId, out upgradeSkillId) ||
            string.IsNullOrWhiteSpace(upgradeSkillId) ||
            !DataManager.Instance.SkillDatabase.TryGet(upgradeSkillId, out _))
        {
            normalizedSkillId = string.Empty;
            upgradeSkillId = string.Empty;
            return false;
        }

        upgradeSkillId = upgradeSkillId.Trim();
        return true;
    }

    private IEnumerable<CharacterRuntimeData> EnumeratePartyCharacters()
    {
        if (DataManager.Instance == null || DataManager.Instance.CharacterRuntimeStore == null)
            yield break;

        PartyRuntimeStore partyStore = DataManager.Instance.PartyRuntimeStore;
        CharacterRuntimeStore characterStore = DataManager.Instance.CharacterRuntimeStore;
        HashSet<string> yielded = new();

        if (partyStore == null)
            yield break;

        for (int i = 0; i < partyStore.MaxPartyCountValue; i++)
        {
            string characterId = partyStore.GetCharacterId(i);
            if (string.IsNullOrWhiteSpace(characterId))
                continue;

            characterId = characterId.Trim();
            if (!yielded.Add(characterId))
                continue;

            if (characterStore.TryGet(characterId, out CharacterRuntimeData character) && character != null)
                yield return character;
        }
    }

    private string GetCharacterDisplayName(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return GameLocalization.Get("common.character");

        string normalizedId = characterId.Trim();
        if (DataManager.Instance?.CharacterDatabase != null &&
            DataManager.Instance.CharacterDatabase.TryGet(normalizedId, out CharacterMasterData character) &&
            character != null)
        {
            string displayName = GameDataLocalization.CharacterName(character);
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;
        }

        return normalizedId;
    }

    private string GetSkillDisplayName(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
            return GameLocalization.Get("common.memory");

        string normalizedId = skillId.Trim();
        if (DataManager.Instance?.SkillDatabase != null &&
            DataManager.Instance.SkillDatabase.TryGet(normalizedId, out SkillMasterData skill) &&
            skill != null)
        {
            string displayName = GameDataLocalization.SkillName(skill);
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;
        }

        return normalizedId;
    }

    private static string GetSkillSlotDisplayName(EventChoiceSkillSlotKind slotKind, int slotIndex)
    {
        return slotKind switch
        {
            EventChoiceSkillSlotKind.Passive => "본능 기억",
            EventChoiceSkillSlotKind.Unique => "발현 기억",
            EventChoiceSkillSlotKind.Ability => "구현 기억",
            EventChoiceSkillSlotKind.Equipped => $"장착 기억 {slotIndex + 1}",
            _ => "장착 기억"
        };
    }

    private Sprite GetSkillSprite(string skillId, SkillMasterData skill)
    {
        if (skill != null && skill.Icon != null)
            return skill.Icon;

        if (string.IsNullOrWhiteSpace(skillId) ||
            DataManager.Instance == null ||
            DataManager.Instance.SkillIconDatabase == null)
        {
            return null;
        }

        return DataManager.Instance.SkillIconDatabase.TryGetIcon(skillId.Trim(), out Sprite icon)
            ? icon
            : null;
    }

    private static bool IsSameId(string left, string right)
    {
        return string.Equals(
            left?.Trim(),
            right?.Trim(),
            System.StringComparison.Ordinal);
    }

    private Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
            return null;

        if (string.Equals(root.name, targetName, System.StringComparison.Ordinal))
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildRecursive(root.GetChild(i), targetName);

            if (result != null)
                return result;
        }

        return null;
    }

    private Transform FindSceneTransformByName(string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
            return null;

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];

            if (candidate == null ||
                !candidate.gameObject.scene.IsValid() ||
                !candidate.gameObject.scene.isLoaded)
            {
                continue;
            }

            if (string.Equals(candidate.name, targetName, System.StringComparison.Ordinal))
                return candidate;
        }

        return null;
    }

    private void CompleteCurrentNode()
    {
        if (DataManager.Instance == null)
            return;

        MapRuntimeData runtime = DataManager.Instance.MapRuntimeStore.Get();

        if (runtime == null)
            return;

        string nodeKey = runtime.CurrentNodeIndex.ToString();

        if (!runtime.ClearedMapIds.Contains(nodeKey))
            runtime.ClearedMapIds.Add(nodeKey);

        if (!runtime.VisitedMapIds.Contains(nodeKey))
            runtime.VisitedMapIds.Add(nodeKey);

        DataManager.Instance.MapRuntimeStore.Set(runtime);
        SaveSystem.Instance?.ClearBattleRoomResumeState();
        SaveSystem.Instance?.SaveCheckpoint();

        Debug.Log(
            $"[RestRoomController] Complete Node / Node:{runtime.CurrentNodeIndex} / Map:{runtime.CurrentMapId}"
        );
    }
}

/// <summary>
/// Rest Heal label only appears while the mouse is over HealButton.
/// Its RectTransform position is fixed to the authored placement.
/// </summary>
[DisallowMultipleComponent]
public sealed class RestHealTextHoverToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private CanvasGroup group;
    private Coroutine fadeRoutine;
    private float duration = 0.2f;

    public void Configure(GameObject target, float fadeSeconds = 0.2f)
    {
        if (target == null) return;
        if (!target.activeSelf) target.SetActive(true);
        group = target.GetComponent<CanvasGroup>();
        if (group == null) group = target.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        duration = Mathf.Max(0f, fadeSeconds);
        group.alpha = 0f;
    }

    public void OnPointerEnter(PointerEventData eventData) => FadeTo(1f);
    public void OnPointerExit(PointerEventData eventData) => FadeTo(0f);
    private void OnDisable()
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = null;
        if (group != null) group.alpha = 0f;
    }
    private void FadeTo(float alpha)
    {
        if (group == null) return;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(Fade(alpha));
    }
    private IEnumerator Fade(float target)
    {
        float start = group.alpha;
        float elapsed = 0f;
        if (duration <= 0f) { group.alpha = target; yield break; }
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        group.alpha = target;
        fadeRoutine = null;
    }
}
