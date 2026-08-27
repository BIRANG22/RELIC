using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

/// <summary>
/// �� ���� �÷� ����, ��� ����, �̸� �� ���� ǥ�ø� �����մϴ�.
/// </summary>
public class ErosionSelectCarousel : MonoBehaviour
{
    [System.Serializable]
    private sealed class TrialItem
    {
        [Tooltip("�÷� �׸��� ��Ʈ ������Ʈ�Դϴ�.")]
        public Transform target;

        [Tooltip("�÷� ���� ��ư�Դϴ�. ��� �θ� Target���� �ڵ� Ž���մϴ�.")]
        public Button button;

        [Tooltip("���õǾ��� �� ǥ���� ������Ʈ�Դϴ�.")]
        public GameObject selectedVisual;

        [Tooltip("��� ���¿��� ǥ���� LOCK ������Ʈ�Դϴ�.")]
        public GameObject lockVisual;

        [Tooltip("�ڽ� Name ������Ʈ�� TMP �ؽ�Ʈ�Դϴ�.")]
        public TMP_Text nameText;

        [Tooltip("�ڽ� Effect ������Ʈ�� TMP �ؽ�Ʈ�Դϴ�.")]
        public TMP_Text effectText;

        [Tooltip("Effect �ؽ�Ʈ�� Localize String Event�Դϴ�. ��� �θ� Effect���� �ڵ� Ž���մϴ�.")]
        public LocalizeStringEvent effectLocalizer;

        [Tooltip("���� ���¿� ���� ���� ������ �׷����Դϴ�.")]
        public Graphic tintGraphic;

        [HideInInspector] public string unlockedName;
        [HideInInspector] public string unlockedEffect;
        [HideInInspector] public string unlockedEffectKey;
        [HideInInspector] public bool textCached;
    }

    [Header("Trial Items")]
    [Tooltip("�÷� 1, �÷� 2, �÷� 3 ������ �����մϴ�.")]
    [SerializeField]
    private TrialItem[] trialItems = new TrialItem[TrialSelectionState.TrialCount];

    [SerializeField] private bool autoBindTrialItems = true;
    [SerializeField] private bool allowLegacyErosionNames = true;

    [Header("Locked Text")]
    [SerializeField] private string lockedNameText = "???";

    [Header("Selection Visual")]
    [SerializeField] private bool changeTintColor = true;
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color unselectedColor = new Color(1f, 1f, 1f, 0.55f);

    [Header("Sound")]
    [SerializeField] private bool playClickSound = true;
    [SerializeField, SoundId(SoundCategory.Sfx)] private string clickSfx = AudioIds.Sfx.NormalButtonClick;
    [Range(0f, 1f)]
    [SerializeField] private float clickVolume = 1f;

    private readonly UnityEngine.Events.UnityAction[] clickActions =
        new UnityEngine.Events.UnityAction[TrialSelectionState.TrialCount];

    private bool isInitialized;

    public int SelectedMask => TrialSelectionState.SelectedMask;

    public int CurrentIndex
    {
        get
        {
            for (int i = 0; i < TrialSelectionState.TrialCount; i++)
            {
                if (TrialSelectionState.IsSelected(i))
                    return i;
            }

            return -1;
        }
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();

        TrialSelectionState.SelectionChanged -= RefreshVisuals;
        TrialSelectionState.SelectionChanged += RefreshVisuals;

        TrialUnlockProgress.ProgressChanged -= RefreshVisuals;
        TrialUnlockProgress.ProgressChanged += RefreshVisuals;

        RefreshVisuals();
    }

    private void OnDisable()
    {
        TrialSelectionState.SelectionChanged -= RefreshVisuals;
        TrialUnlockProgress.ProgressChanged -= RefreshVisuals;
    }

    private void OnDestroy()
    {
        UnbindButtons();
        TrialSelectionState.SelectionChanged -= RefreshVisuals;
        TrialUnlockProgress.ProgressChanged -= RefreshVisuals;
    }

    private void OnValidate()
    {
        EnsureArraySize();
    }

    public void ToggleTrial(int trialIndex)
    {
        if (trialIndex < 0 || trialIndex >= TrialSelectionState.TrialCount)
            return;

        if (!TrialUnlockProgress.IsUnlocked(trialIndex))
            return;

        if (!CanLocalPlayerMutateHostOnlyState())
            return;

        TrialSelectionState.Toggle(trialIndex);
        PlayClickSound();
        PublishHostSnapshotAfterLocalMutation();
    }

    public void ToggleTrial1() => ToggleTrial(0);
    public void ToggleTrial2() => ToggleTrial(1);
    public void ToggleTrial3() => ToggleTrial(2);

    public void SetTrialSelected(int trialIndex, bool selected)
    {
        if (selected && !TrialUnlockProgress.IsUnlocked(trialIndex))
            return;

        TrialSelectionState.SetSelected(trialIndex, selected);
    }

    public bool IsTrialSelected(int trialIndex)
    {
        return TrialSelectionState.IsSelected(trialIndex);
    }

    [ContextMenu("Clear Trial Selection")]
    public void ClearSelection()
    {
        TrialSelectionState.Clear();
    }

    [ContextMenu("Auto Bind Trial Items")]
    public void AutoBindTrialItems()
    {
        EnsureArraySize();

        for (int i = 0; i < TrialSelectionState.TrialCount; i++)
        {
            TrialItem item = trialItems[i];
            Transform target = item.target;

            if (target == null)
            {
                target = FindChildRecursive(transform, "Trial_" + (i + 1));

                if (target == null)
                    target = FindChildRecursive(transform, "Trial" + (i + 1));

                if (target == null && allowLegacyErosionNames)
                    target = FindChildRecursive(transform, "Erosion_" + i);

                item.target = target;
            }

            if (target == null)
                continue;

            if (item.button == null)
                item.button = target.GetComponent<Button>() ?? target.GetComponentInChildren<Button>(true);

            if (item.tintGraphic == null)
                item.tintGraphic = target.GetComponent<Graphic>() ?? target.GetComponentInChildren<Graphic>(true);

            if (item.selectedVisual == null)
                item.selectedVisual = FindNamedChild(target, "Selected");

            if (item.lockVisual == null)
                item.lockVisual = FindNamedChild(target, "LOCK");

            if (item.nameText == null)
                item.nameText = FindTextChild(target, "Name");

            if (item.effectText == null)
                item.effectText = FindTextChild(target, "Effect");

            if (item.effectLocalizer == null && item.effectText != null)
                item.effectLocalizer = item.effectText.GetComponent<LocalizeStringEvent>();

            CacheUnlockedText(item);
        }

        if (Application.isPlaying && isInitialized)
        {
            UnbindButtons();
            BindButtons();
            RefreshVisuals();
        }
    }

    private void Initialize()
    {
        if (isInitialized)
            return;

        EnsureArraySize();

        if (autoBindTrialItems)
            AutoBindTrialItems();
        else
            CacheAllUnlockedTexts();

        BindButtons();
        isInitialized = true;
    }

    private void EnsureArraySize()
    {
        if (trialItems != null && trialItems.Length == TrialSelectionState.TrialCount)
        {
            for (int i = 0; i < trialItems.Length; i++)
            {
                if (trialItems[i] == null)
                    trialItems[i] = new TrialItem();
            }

            return;
        }

        TrialItem[] resized = new TrialItem[TrialSelectionState.TrialCount];

        if (trialItems != null)
        {
            int copyCount = Mathf.Min(trialItems.Length, resized.Length);
            for (int i = 0; i < copyCount; i++)
                resized[i] = trialItems[i];
        }

        for (int i = 0; i < resized.Length; i++)
        {
            if (resized[i] == null)
                resized[i] = new TrialItem();
        }

        trialItems = resized;
    }

    private void BindButtons()
    {
        for (int i = 0; i < TrialSelectionState.TrialCount; i++)
        {
            TrialItem item = trialItems[i];
            if (item == null || item.button == null)
                continue;

            int capturedIndex = i;
            clickActions[i] = () => ToggleTrial(capturedIndex);
            item.button.onClick.RemoveListener(clickActions[i]);
            item.button.onClick.AddListener(clickActions[i]);
        }
    }

    private void UnbindButtons()
    {
        if (trialItems == null)
            return;

        int count = Mathf.Min(trialItems.Length, clickActions.Length);
        for (int i = 0; i < count; i++)
        {
            TrialItem item = trialItems[i];
            if (item == null || item.button == null || clickActions[i] == null)
                continue;

            item.button.onClick.RemoveListener(clickActions[i]);
            clickActions[i] = null;
        }
    }

    private void RefreshVisuals()
    {
        if (trialItems == null)
            return;

        int count = Mathf.Min(trialItems.Length, TrialSelectionState.TrialCount);
        for (int i = 0; i < count; i++)
        {
            TrialItem item = trialItems[i];
            if (item == null)
                continue;

            CacheUnlockedText(item);

            bool unlocked = TrialUnlockProgress.IsUnlocked(i);
            bool selected = unlocked && TrialSelectionState.IsSelected(i);

            if (!unlocked && TrialSelectionState.IsSelected(i))
                TrialSelectionState.SetSelected(i, false);

            if (item.lockVisual != null)
                item.lockVisual.SetActive(!unlocked);

            if (item.selectedVisual != null)
                item.selectedVisual.SetActive(selected);

            if (item.button != null)
                item.button.interactable = unlocked && CanLocalPlayerMutateHostOnlyState();

            if (item.nameText != null)
                item.nameText.text = unlocked ? item.unlockedName : lockedNameText;

            if (item.effectText != null)
            {
                if (unlocked)
                    ApplyEffectText(item, item.unlockedEffectKey, item.unlockedEffect);
                else
                    ApplyEffectText(
                        item,
                        TrialUnlockProgress.GetUnlockRequirementKey(i),
                        TrialUnlockProgress.GetUnlockRequirementText(i));
            }

            if (changeTintColor && item.tintGraphic != null)
                item.tintGraphic.color = selected ? selectedColor : unselectedColor;
        }
    }

    private void CacheAllUnlockedTexts()
    {
        if (trialItems == null)
            return;

        for (int i = 0; i < trialItems.Length; i++)
            CacheUnlockedText(trialItems[i]);
    }

    private static void CacheUnlockedText(TrialItem item)
    {
        if (item == null || item.textCached)
            return;

        item.unlockedName = item.nameText != null ? item.nameText.text : string.Empty;
        item.unlockedEffect = item.effectText != null ? item.effectText.text : string.Empty;
        item.unlockedEffectKey = item.effectLocalizer != null
            ? item.effectLocalizer.StringReference.TableEntryReference.Key
            : string.Empty;
        item.textCached = true;
    }

    private static void ApplyEffectText(TrialItem item, string localizationKey, string fallback)
    {
        if (item == null || item.effectText == null)
            return;

        if (item.effectLocalizer == null)
        {
            item.effectText.text = fallback ?? string.Empty;
            return;
        }

        if (!string.IsNullOrWhiteSpace(localizationKey))
        {
            item.effectLocalizer.StringReference = new LocalizedString(
                GameLocalization.TableName,
                localizationKey);
            item.effectLocalizer.RefreshString();
            item.effectText.text = GameLocalization.Get(localizationKey, fallback);
            return;
        }

        item.effectText.text = fallback ?? string.Empty;
    }

    private void PlayClickSound()
    {
        if (!playClickSound || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(clickSfx, clickVolume);
    }

    private static bool CanLocalPlayerMutateHostOnlyState()
    {
        SteamLobbySharedStateSynchronizer synchronizer =
            SteamLobbySharedStateSynchronizer.Instance;
        return synchronizer == null ||
               synchronizer.CanLocalPlayerMutateHostOnlyState();
    }

    private static void PublishHostSnapshotAfterLocalMutation()
    {
        SteamLobbySharedStateSynchronizer.Instance
            ?.PublishHostSnapshotAfterLocalMutation();
    }

    private static GameObject FindNamedChild(Transform target, string childName)
    {
        Transform found = FindChildRecursive(target, childName);
        return found != null ? found.gameObject : null;
    }

    private static TMP_Text FindTextChild(Transform target, string childName)
    {
        Transform found = FindChildRecursive(target, childName);
        return found != null ? found.GetComponent<TMP_Text>() : null;
    }

    private static Transform FindChildRecursive(Transform root, string objectName)
    {
        if (root == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == objectName)
                return child;

            Transform nested = FindChildRecursive(child, objectName);
            if (nested != null)
                return nested;
        }

        return null;
    }
}
