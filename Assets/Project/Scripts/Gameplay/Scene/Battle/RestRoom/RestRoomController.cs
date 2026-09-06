using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Relic.Gameplay.Data;

public class RestRoomController : MonoBehaviour
{
    [Header("Ally Spawn")]
    [SerializeField] private Transform[] allySpawnPoints;
    [SerializeField] private float allySpawnScale = 0.7f;

    [Header("Upgrade")]
    [SerializeField] private EventSkillAwakenSelectionPanelUI skillAwakenSelectionPanel;
    [SerializeField] private GameObject upgradeButtonRoot;
    [SerializeField] private Image upgradeButtonBackImage;
    [SerializeField] private TMP_Text upgradeButtonText;
    [SerializeField] private Color disabledUpgradeButtonColor = new Color32(0x7E, 0x7E, 0x7E, 0xFF);

    [Header("Heal")]
    [SerializeField, Range(0f, 1f)] private float healHpRatio = 0.3f;
    [SerializeField] private GameObject healButtonRoot;
    [SerializeField] private GameObject healTextRoot;
    [SerializeField] private CanvasGroup healTextCanvasGroup;
    [SerializeField, Min(0f)] private float healTextFadeDuration = 0.25f;
    [SerializeField, Min(0f)] private float healTextHoldDuration = 1f;
    [SerializeField] private Image healButtonBackImage;
    [SerializeField] private TMP_Text healButtonText;
    [SerializeField] private Color disabledHealButtonColor = new Color32(0x7E, 0x7E, 0x7E, 0xFF);

    [Header("Shop")]
    [SerializeField] private RestRoomShopPanel shopPanel;

    [Header("Progression")]
    [SerializeField] private GameObject nextButtonRoot;

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
    private Coroutine healTextRoutine;

    private bool IsRestActionLocked => isRestUsed;

    private void Awake()
    {
        EnsureShopPanelSpawner();
        EnsureNextButtonRoot();
        EnsureUpgradeButtonReferences();
        EnsureHealButtonReferences();
        EnsureHealTextReferences();
        EnsureSkillAwakenSelectionPanel();
    }

    private void OnEnable()
    {
        isRestUsed = false;
        hasPendingSkillAwakenResult = false;
        pendingSkillAwakenResultTarget = default;

        EnsureShopPanelSpawner();
        EnsureNextButtonRoot();
        EnsureUpgradeButtonReferences();
        EnsureHealButtonReferences();
        EnsureHealTextReferences();
        EnsureSkillAwakenSelectionPanel();
        EnsurePlayerHudReferences();
        SetHealTextVisibleImmediate(false);
        SetNextButtonVisible(false);
        SetRestActionButtonsVisible(true);
        SetUpgradeButtonDisabledFeedback(false);
        SetHealButtonDisabledFeedback(false);
        SpawnPartyAllies();
        SpawnPlayerHUDs();
    }

    private void OnDisable()
    {
        SetNextButtonVisible(false);

        if (skillAwakenResultRoutine != null)
        {
            StopCoroutine(skillAwakenResultRoutine);
            skillAwakenResultRoutine = null;
        }

        if (skillAwakenSelectionPanel != null)
            skillAwakenSelectionPanel.Close();

        if (healTextRoutine != null)
        {
            StopCoroutine(healTextRoutine);
            healTextRoutine = null;
        }

        SetHealTextVisibleImmediate(false);
        skillAwakenOptions.Clear();
        hasPendingSkillAwakenResult = false;
        pendingSkillAwakenResultTarget = default;
        ClearPlayerHUDs();
    }

    public void OnRestButtonClicked()
    {
        if (UIPanelButton.IsMenuPanelOpen)
            return;

        if (SteamBattleStateSynchronizer.TryBlockSharedBattleStateEdit())
            return;

        if (IsRestActionLocked)
            return;

        isRestUsed = true;
        if (skillAwakenSelectionPanel != null)
            skillAwakenSelectionPanel.Close();

        RecoverAllPartyHPByRatio(healHpRatio);
        RefreshPlayerHUDs();
        PlayHealVfxOnSpawnedAllies();
        StartHealTextFeedback();
        SetHealButtonDisabledFeedback(true);
        SetUpgradeButtonDisabledFeedback(true);
        SetRestActionButtonsVisible(false);
        SetNextButtonVisible(true);
    }

    public void OnUpgradeButtonClicked()
    {
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
            BattleWarningUI.ShowMessage("강화 가능한 장착 기억이 없습니다.");
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
            BattleWarningUI.ShowMessage("기억 강화 선택 패널을 열 수 없습니다.");
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

        if (!isRestUsed)
            return;

        EnsureNextButtonRoot();
        if (nextButtonRoot == null || !nextButtonRoot.activeInHierarchy)
            return;

        CompleteCurrentNode();

        BattleSceneController sceneController =
            Object.FindFirstObjectByType<BattleSceneController>(FindObjectsInactive.Include);

        if (sceneController != null)
            sceneController.ReturnToMap();
        else
            Debug.LogWarning("[RestRoomController] BattleSceneController not found");
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

            if (ally.GetComponent<BattleMapSelectionCharacterMarker>() == null)
                ally.AddComponent<BattleMapSelectionCharacterMarker>();

            spawnedAllyAnimators[i] = ally.GetComponentInChildren<BattleUnitAnimator>(true);
        }
    }

    private void PlayHealVfxOnSpawnedAllies()
    {
        CacheSpawnedAllyAnimatorsIfNeeded();

        if (spawnedAllyAnimators == null)
            return;

        for (int i = 0; i < spawnedAllyAnimators.Length; i++)
            spawnedAllyAnimators[i]?.PlayHeal();
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
            Destroy(point.GetChild(i).gameObject);
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
        if (nextButtonRoot != null)
            return;

        Transform nextButtonTransform = FindSceneTransformByName("NextButton");

        if (nextButtonTransform != null)
            nextButtonRoot = nextButtonTransform.gameObject;
    }

    private void SetNextButtonVisible(bool visible)
    {
        EnsureNextButtonRoot();

        if (nextButtonRoot != null)
            nextButtonRoot.SetActive(visible);
    }

    private void SetRestActionButtonsVisible(bool visible)
    {
        EnsureUpgradeButtonReferences();
        EnsureHealButtonReferences();

        if (upgradeButtonRoot != null)
            upgradeButtonRoot.SetActive(visible);

        if (healButtonRoot != null)
            healButtonRoot.SetActive(visible);
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


    private void EnsureHealTextReferences()
    {
        if (healTextRoot == null)
        {
            Transform healTextTransform = FindSceneTransformByName("Heal_Text");

            if (healTextTransform != null && healTextTransform.IsChildOf(transform))
                healTextRoot = healTextTransform.gameObject;
        }

        if (healTextRoot == null)
            return;

        if (healTextCanvasGroup == null)
            healTextCanvasGroup = healTextRoot.GetComponent<CanvasGroup>();

        if (healTextCanvasGroup == null)
            healTextCanvasGroup = healTextRoot.AddComponent<CanvasGroup>();

        healTextCanvasGroup.interactable = false;
        healTextCanvasGroup.blocksRaycasts = false;
    }

    private void StartHealTextFeedback()
    {
        EnsureHealTextReferences();

        if (healTextRoot == null || healTextCanvasGroup == null)
            return;

        if (healTextRoutine != null)
            StopCoroutine(healTextRoutine);

        healTextRoutine = StartCoroutine(PlayHealTextFeedbackRoutine());
    }

    private IEnumerator PlayHealTextFeedbackRoutine()
    {
        EnsureHealTextReferences();

        if (healTextRoot == null || healTextCanvasGroup == null)
        {
            healTextRoutine = null;
            yield break;
        }

        healTextRoot.SetActive(true);
        healTextCanvasGroup.alpha = 0f;

        float fadeDuration = Mathf.Max(0f, healTextFadeDuration);
        if (fadeDuration <= 0f)
        {
            healTextCanvasGroup.alpha = 1f;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                healTextCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            healTextCanvasGroup.alpha = 1f;
        }

        if (healTextHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(healTextHoldDuration);

        if (fadeDuration <= 0f)
        {
            healTextCanvasGroup.alpha = 0f;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                healTextCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            healTextCanvasGroup.alpha = 0f;
        }

        healTextRoot.SetActive(false);
        healTextRoutine = null;
    }

    private void SetHealTextVisibleImmediate(bool visible)
    {
        EnsureHealTextReferences();

        if (healTextCanvasGroup != null)
        {
            healTextCanvasGroup.alpha = visible ? 1f : 0f;
            healTextCanvasGroup.interactable = false;
            healTextCanvasGroup.blocksRaycasts = false;
        }

        if (healTextRoot != null)
            healTextRoot.SetActive(visible);
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
            resultMessage = "강화할 기억을 선택해야 합니다.";
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
            return "캐릭터";

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
            return "기억";

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
