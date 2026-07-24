using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 기존 침식도 캐러셀 컴포넌트의 연결을 유지하면서
/// 세 개의 Trial을 개별적으로 선택하거나 해제하는 컨트롤러입니다.
/// </summary>
public class ErosionSelectCarousel : MonoBehaviour
{
    [System.Serializable]
    private sealed class TrialItem
    {
        [Tooltip("Trial 항목의 루트 오브젝트입니다.")]
        public Transform target;

        [Tooltip("Trial을 선택하거나 해제할 버튼입니다. 비워두면 Target에서 자동으로 찾습니다.")]
        public Button button;

        [Tooltip("선택되었을 때만 표시할 오브젝트입니다. 예: 테두리, 체크 표시, 하이라이트")]
        public GameObject selectedVisual;

        [Tooltip("선택 상태에 따라 색을 변경할 그래픽입니다. 비워두면 Target에서 자동으로 찾습니다.")]
        public Graphic tintGraphic;
    }

    [Header("Trial Items")]
    [Tooltip("Trial 1, Trial 2, Trial 3 순서로 연결합니다.")]
    [SerializeField]
    private TrialItem[] trialItems = new TrialItem[TrialSelectionState.TrialCount];

    [Tooltip("자식 오브젝트 이름 Trial_1, Trial_2, Trial_3을 찾아 자동으로 연결합니다.")]
    [SerializeField] private bool autoBindTrialItems = true;

    [Tooltip("Trial 이름으로 찾지 못하면 기존 Erosion_0, Erosion_1, Erosion_2 이름도 확인합니다.")]
    [SerializeField] private bool allowLegacyErosionNames = true;

    [Header("Selection Visual")]
    [SerializeField] private bool changeTintColor = true;
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color unselectedColor = new Color(1f, 1f, 1f, 0.55f);

    [Header("Sound")]
    [SerializeField] private bool playClickSound = true;
    [SerializeField] private SfxType clickSfx = SfxType.NormalButtonClick;
    [Range(0f, 1f)]
    [SerializeField] private float clickVolume = 1f;

    private readonly UnityEngine.Events.UnityAction[] clickActions =
        new UnityEngine.Events.UnityAction[TrialSelectionState.TrialCount];

    private bool isInitialized;

    /// <summary>
    /// 선택된 Trial 비트 마스크입니다. 아무것도 선택하지 않으면 0입니다.
    /// </summary>
    public int SelectedMask => TrialSelectionState.SelectedMask;

    /// <summary>
    /// 기존 외부 코드 호환용입니다. 선택된 Trial이 없으면 -1을 반환합니다.
    /// 여러 개가 선택된 경우 가장 낮은 번호를 반환합니다.
    /// </summary>
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
        RefreshVisuals();
    }

    private void OnDisable()
    {
        TrialSelectionState.SelectionChanged -= RefreshVisuals;
    }

    private void OnDestroy()
    {
        UnbindButtons();
        TrialSelectionState.SelectionChanged -= RefreshVisuals;
    }

    private void OnValidate()
    {
        EnsureArraySize();
    }

    /// <summary>
    /// 인스펙터 Button OnClick에서도 사용할 수 있는 공용 토글 함수입니다.
    /// trialIndex는 0부터 시작합니다.
    /// </summary>
    public void ToggleTrial(int trialIndex)
    {
        if (trialIndex < 0 || trialIndex >= TrialSelectionState.TrialCount)
            return;

        TrialSelectionState.Toggle(trialIndex);
        PlayClickSound();
    }

    public void ToggleTrial1()
    {
        ToggleTrial(0);
    }

    public void ToggleTrial2()
    {
        ToggleTrial(1);
    }

    public void ToggleTrial3()
    {
        ToggleTrial(2);
    }

    public void SetTrialSelected(int trialIndex, bool selected)
    {
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
                item.selectedVisual = FindSelectedVisual(target);
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

            bool selected = TrialSelectionState.IsSelected(i);

            if (item.selectedVisual != null)
                item.selectedVisual.SetActive(selected);

            if (changeTintColor && item.tintGraphic != null)
                item.tintGraphic.color = selected ? selectedColor : unselectedColor;
        }
    }

    private void PlayClickSound()
    {
        if (!playClickSound || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySfx(clickSfx, clickVolume);
    }

    private static GameObject FindSelectedVisual(Transform target)
    {
        string[] names = { "Selected", "Select", "On", "Check", "Highlight" };

        for (int i = 0; i < names.Length; i++)
        {
            Transform found = FindChildRecursive(target, names[i]);
            if (found != null && found != target)
                return found.gameObject;
        }

        return null;
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
