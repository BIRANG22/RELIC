using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ErosionSelectCarousel : MonoBehaviour
{
    [System.Serializable]
    private sealed class ErosionItem
    {
        [Tooltip("이동시킬 침식 난이도 이미지입니다. Erosion_0~5를 순서대로 넣어주세요.")]
        public Transform target;

        [HideInInspector] public Vector3 baseLocalPosition;
        [HideInInspector] public Vector2 baseAnchoredPosition;
        [HideInInspector] public RectTransform rectTransform;
    }

    [Header("Erosion Items")]
    [Tooltip("Erosion_0, Erosion_1, Erosion_2, Erosion_3, Erosion_4, Erosion_5 순서대로 넣어주세요.")]
    [SerializeField] private ErosionItem[] erosionItems = new ErosionItem[6];

    [Tooltip("켜져 있으면 자식 오브젝트 이름 Erosion_0~5를 자동으로 찾아 연결합니다.")]
    [SerializeField] private bool autoBindErosionItems = true;

    [Tooltip("시작할 때 중앙에 둘 침식 난이도 번호입니다.")]
    [SerializeField] private int startIndex = 0;

    [Tooltip("끝에서 한 번 더 넘기면 반대쪽으로 이어지게 합니다.")]
    [SerializeField] private bool wrapSelection = true;

    [Tooltip("Wrap Selection이 켜져 있을 때 끝과 끝을 실제 옆 위치로 배치합니다. 예: Erosion_0 중앙일 때 Erosion_5는 X -600에 배치됩니다.")]
    [SerializeField] private bool useCircularWrapPositions = true;

    [Header("Navigation Buttons")]
    [Tooltip("누르면 현재 중앙 이미지가 왼쪽으로 지나가고, 오른쪽 이미지가 중앙으로 옵니다.")]
    [SerializeField] private Button prevButton;

    [Tooltip("누르면 현재 중앙 이미지가 오른쪽으로 지나가고, 왼쪽 이미지가 중앙으로 옵니다.")]
    [SerializeField] private Button nextButton;

    [Tooltip("켜져 있으면 버튼 OnClick을 스크립트에서 자동으로 연결합니다.")]
    [SerializeField] private bool bindNavigationButtonClicks = true;

    [Tooltip("끝에서 더 이동할 수 없을 때 버튼을 비활성화합니다. Wrap Selection이 켜져 있으면 적용되지 않습니다.")]
    [SerializeField] private bool disableButtonsAtEnds = true;

    [Header("Input Block")]
    [Tooltip("로비 메뉴가 열려 있을 때 A/D, 방향키, 이전/다음 버튼 입력을 막습니다.")]
    [SerializeField] private bool blockInputWhenLobbyMenuOpen = true;

    [Tooltip("로비 메뉴를 열고 닫는 컨트롤러입니다. 비워두면 자동으로 찾습니다.")]
    [SerializeField] private LobbyMenuController lobbyMenuController;

    [Tooltip("로비 메뉴 패널입니다. LobbyMenuController를 찾지 못했을 때 활성 상태 확인용으로 사용합니다.")]
    [SerializeField] private GameObject menuPanel;

    [Tooltip("켜져 있으면 LobbyMenuController를 자동으로 찾습니다.")]
    [SerializeField] private bool autoFindLobbyMenuController = true;

    [Tooltip("켜져 있는 동안 침식 난이도 입력을 막을 패널들입니다. 메뉴 안의 설정/확인 프리팹이 있다면 필요할 때 등록할 수 있습니다.")]
    [SerializeField] private GameObject[] inputBlockingPanels;

    [Header("Keyboard")]
    [Tooltip("A 키 입력을 사용합니다.")]
    [SerializeField] private bool useAKey = true;

    [Tooltip("D 키 입력을 사용합니다.")]
    [SerializeField] private bool useDKey = true;

    [Tooltip("왼쪽/오른쪽 방향키 입력을 함께 사용합니다.")]
    [SerializeField] private bool useArrowKeys = true;

    [Tooltip("입력 후 다음 입력을 받을 때까지의 짧은 대기 시간입니다.")]
    [SerializeField] private float inputCooldown = 0.08f;

    [Header("Movement")]
    [Tooltip("한 단계 이동 거리입니다. 현재 설정 기준 Erosion_0=0, Erosion_1=600이므로 600을 사용합니다.")]
    [SerializeField] private float spacingX = 600f;

    [Tooltip("켜져 있으면 시작 시 현재 오브젝트 위치를 기준 위치로 저장합니다. Erosion_0=0, Erosion_1=600처럼 직접 배치한 값을 그대로 사용합니다.")]
    [SerializeField] private bool useCurrentPositionsAsBase = true;

    [Tooltip("회전에 걸리는 시간입니다.")]
    [SerializeField] private float moveDuration = 0.25f;

    [Tooltip("Time.timeScale의 영향을 받지 않게 움직입니다.")]
    [SerializeField] private bool useUnscaledTime = true;

    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine moveCoroutine;
    private int currentIndex;
    private float nextInputAllowedTime;
    private bool isInitialized;

    public int CurrentIndex => currentIndex;

    private void Awake()
    {
        FindInputBlockReferencesIfNeeded();
        Initialize();
    }

    private void OnEnable()
    {
        FindInputBlockReferencesIfNeeded();
        Initialize();
        currentIndex = Mathf.Clamp(startIndex, 0, GetLastIndex());
        ApplyPositions(true);
        RefreshNavigationButtons();
    }

    private void OnDisable()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        UnbindNavigationButtons();
    }

    private void Update()
    {
        if (!isActiveAndEnabled)
            return;

        if (IsInputBlocked())
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (GetTime() < nextInputAllowedTime)
            return;

        if ((useAKey && keyboard.aKey.wasPressedThisFrame) || (useArrowKeys && keyboard.leftArrowKey.wasPressedThisFrame))
        {
            MoveLeftImageToCenter();
            BlockInputForCooldown();
            return;
        }

        if ((useDKey && keyboard.dKey.wasPressedThisFrame) || (useArrowKeys && keyboard.rightArrowKey.wasPressedThisFrame))
        {
            MoveRightImageToCenter();
            BlockInputForCooldown();
        }
    }

    [ContextMenu("Move Right Image To Center")]
    public void MoveRightImageToCenter()
    {
        if (IsInputBlocked())
            return;

        // 현재 중앙 이미지가 왼쪽으로 지나가고, 오른쪽 이미지가 중앙으로 옵니다.
        MoveSelection(1);
    }

    [ContextMenu("Move Left Image To Center")]
    public void MoveLeftImageToCenter()
    {
        if (IsInputBlocked())
            return;

        // 현재 중앙 이미지가 오른쪽으로 지나가고, 왼쪽 이미지가 중앙으로 옵니다.
        MoveSelection(-1);
    }

    public void SetSelection(int index, bool instant = false)
    {
        Initialize();

        int lastIndex = GetLastIndex();
        if (lastIndex < 0)
            return;

        currentIndex = Mathf.Clamp(index, 0, lastIndex);
        ApplyPositions(instant);
        RefreshNavigationButtons();
    }

    private void MoveSelection(int direction)
    {
        Initialize();

        int lastIndex = GetLastIndex();
        if (lastIndex < 0 || direction == 0)
            return;

        int nextIndex = currentIndex + direction;

        if (wrapSelection)
        {
            if (nextIndex < 0)
                nextIndex = lastIndex;
            else if (nextIndex > lastIndex)
                nextIndex = 0;
        }
        else
        {
            nextIndex = Mathf.Clamp(nextIndex, 0, lastIndex);
        }

        if (nextIndex == currentIndex)
            return;

        currentIndex = nextIndex;
        ApplyPositions(false);
        RefreshNavigationButtons();
    }

    private bool IsInputBlocked()
    {
        if (blockInputWhenLobbyMenuOpen)
        {
            FindInputBlockReferencesIfNeeded();

            if (lobbyMenuController != null)
                return lobbyMenuController.IsMenuOpen;

            // LobbyMenuController를 찾지 못했을 때만 패널 활성 상태를 예비로 확인합니다.
            // 컨트롤러가 연결된 상태에서는 컨트롤러가 기록한 일시정지 상태만 믿어야
            // 비활성처럼 쓰는 메뉴 패널 때문에 A/D가 계속 막히는 일을 피할 수 있습니다.
            if (menuPanel != null && menuPanel.activeInHierarchy)
                return true;
        }

        if (inputBlockingPanels != null)
        {
            for (int i = 0; i < inputBlockingPanels.Length; i++)
            {
                GameObject panel = inputBlockingPanels[i];
                if (panel != null && panel.activeInHierarchy)
                    return true;
            }
        }

        return false;
    }

    private void FindInputBlockReferencesIfNeeded()
    {
        if (!autoFindLobbyMenuController)
            return;

        if (lobbyMenuController == null)
            lobbyMenuController = FindFirstObjectByType<LobbyMenuController>(FindObjectsInactive.Include);

        if (lobbyMenuController != null && menuPanel == null)
            menuPanel = lobbyMenuController.MenuPanel;
    }

    private void Initialize()
    {
        if (isInitialized)
            return;

        if (autoBindErosionItems)
            AutoBindItems();

        CacheBasePositions();
        BindNavigationButtons();
        isInitialized = true;
    }

    private void AutoBindItems()
    {
        if (erosionItems == null || erosionItems.Length != 6)
            erosionItems = new ErosionItem[6];

        for (int i = 0; i < erosionItems.Length; i++)
        {
            if (erosionItems[i] == null)
                erosionItems[i] = new ErosionItem();

            if (erosionItems[i].target != null)
                continue;

            Transform found = transform.Find("Erosion_" + i);
            if (found != null)
                erosionItems[i].target = found;
        }
    }

    private void CacheBasePositions()
    {
        if (erosionItems == null)
            return;

        for (int i = 0; i < erosionItems.Length; i++)
        {
            ErosionItem item = erosionItems[i];
            if (item == null || item.target == null)
                continue;

            item.rectTransform = item.target as RectTransform;

            if (useCurrentPositionsAsBase)
            {
                item.baseLocalPosition = item.target.localPosition;
                if (item.rectTransform != null)
                    item.baseAnchoredPosition = item.rectTransform.anchoredPosition;
            }
            else
            {
                item.baseLocalPosition = new Vector3(i * spacingX, item.target.localPosition.y, item.target.localPosition.z);
                item.baseAnchoredPosition = new Vector2(i * spacingX, item.rectTransform != null ? item.rectTransform.anchoredPosition.y : item.target.localPosition.y);
            }
        }
    }

    private void BindNavigationButtons()
    {
        if (!bindNavigationButtonClicks)
            return;

        if (prevButton != null)
        {
            prevButton.onClick.RemoveListener(MoveRightImageToCenter);
            prevButton.onClick.AddListener(MoveRightImageToCenter);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(MoveLeftImageToCenter);
            nextButton.onClick.AddListener(MoveLeftImageToCenter);
        }
    }

    private void UnbindNavigationButtons()
    {
        if (!bindNavigationButtonClicks)
            return;

        if (prevButton != null)
            prevButton.onClick.RemoveListener(MoveRightImageToCenter);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(MoveLeftImageToCenter);
    }

    private void ApplyPositions(bool instant)
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        if (instant || moveDuration <= 0f)
        {
            ApplyTargetPositions(1f, null, null);
            return;
        }

        moveCoroutine = StartCoroutine(AnimatePositions());
    }

    private IEnumerator AnimatePositions()
    {
        Vector3[] startLocalPositions = new Vector3[erosionItems.Length];
        Vector2[] startAnchoredPositions = new Vector2[erosionItems.Length];
        Vector3[] targetLocalPositions = new Vector3[erosionItems.Length];
        Vector2[] targetAnchoredPositions = new Vector2[erosionItems.Length];

        for (int i = 0; i < erosionItems.Length; i++)
        {
            ErosionItem item = erosionItems[i];
            if (item == null || item.target == null)
                continue;

            startLocalPositions[i] = item.target.localPosition;
            startAnchoredPositions[i] = item.rectTransform != null ? item.rectTransform.anchoredPosition : Vector2.zero;
            GetTargetPosition(i, out targetLocalPositions[i], out targetAnchoredPositions[i]);
        }

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += GetDeltaTime();
            float t = Mathf.Clamp01(elapsed / moveDuration);
            float eased = moveCurve != null ? moveCurve.Evaluate(t) : Mathf.SmoothStep(0f, 1f, t);

            ApplyTargetPositions(eased, startLocalPositions, startAnchoredPositions, targetLocalPositions, targetAnchoredPositions);
            yield return null;
        }

        ApplyTargetPositions(1f, startLocalPositions, startAnchoredPositions, targetLocalPositions, targetAnchoredPositions);
        moveCoroutine = null;
    }

    private void ApplyTargetPositions(float t, Vector3[] startLocalPositions, Vector2[] startAnchoredPositions)
    {
        Vector3[] targetLocalPositions = new Vector3[erosionItems.Length];
        Vector2[] targetAnchoredPositions = new Vector2[erosionItems.Length];

        for (int i = 0; i < erosionItems.Length; i++)
            GetTargetPosition(i, out targetLocalPositions[i], out targetAnchoredPositions[i]);

        ApplyTargetPositions(t, startLocalPositions, startAnchoredPositions, targetLocalPositions, targetAnchoredPositions);
    }

    private void ApplyTargetPositions(float t, Vector3[] startLocalPositions, Vector2[] startAnchoredPositions, Vector3[] targetLocalPositions, Vector2[] targetAnchoredPositions)
    {
        for (int i = 0; i < erosionItems.Length; i++)
        {
            ErosionItem item = erosionItems[i];
            if (item == null || item.target == null)
                continue;

            if (item.rectTransform != null)
            {
                Vector2 start = startAnchoredPositions != null ? startAnchoredPositions[i] : item.rectTransform.anchoredPosition;
                item.rectTransform.anchoredPosition = Vector2.LerpUnclamped(start, targetAnchoredPositions[i], t);
            }
            else
            {
                Vector3 start = startLocalPositions != null ? startLocalPositions[i] : item.target.localPosition;
                item.target.localPosition = Vector3.LerpUnclamped(start, targetLocalPositions[i], t);
            }
        }
    }

    private void GetTargetPosition(int itemIndex, out Vector3 localPosition, out Vector2 anchoredPosition)
    {
        ErosionItem item = erosionItems[itemIndex];

        if (item == null || item.target == null)
        {
            localPosition = Vector3.zero;
            anchoredPosition = Vector2.zero;
            return;
        }

        int clampedCurrentIndex = Mathf.Clamp(currentIndex, 0, GetLastIndex());
        float x;

        if (wrapSelection && useCircularWrapPositions)
        {
            int relativeIndex = GetCircularRelativeIndex(itemIndex, clampedCurrentIndex);
            x = relativeIndex * spacingX;
        }
        else
        {
            float baseCurrentX = useCurrentPositionsAsBase
                ? GetBaseX(clampedCurrentIndex)
                : clampedCurrentIndex * spacingX;

            x = useCurrentPositionsAsBase
                ? GetBaseX(itemIndex) - baseCurrentX
                : (itemIndex - clampedCurrentIndex) * spacingX;
        }

        localPosition = item.baseLocalPosition;
        localPosition.x = x;

        anchoredPosition = item.baseAnchoredPosition;
        anchoredPosition.x = x;
    }

    private int GetCircularRelativeIndex(int itemIndex, int centerIndex)
    {
        int count = GetValidItemCount();
        if (count <= 0)
            return itemIndex - centerIndex;

        int relativeIndex = itemIndex - centerIndex;
        int halfCount = count / 2;

        if (relativeIndex > halfCount)
            relativeIndex -= count;
        else if (relativeIndex < -halfCount)
            relativeIndex += count;

        return relativeIndex;
    }

    private int GetValidItemCount()
    {
        if (erosionItems == null)
            return 0;

        int count = 0;
        for (int i = 0; i < erosionItems.Length; i++)
        {
            if (erosionItems[i] != null && erosionItems[i].target != null)
                count++;
        }

        return count;
    }

    private float GetBaseX(int index)
    {
        if (erosionItems == null || index < 0 || index >= erosionItems.Length)
            return index * spacingX;

        ErosionItem item = erosionItems[index];
        if (item == null || item.target == null)
            return index * spacingX;

        return item.rectTransform != null ? item.baseAnchoredPosition.x : item.baseLocalPosition.x;
    }

    private int GetLastIndex()
    {
        if (erosionItems == null)
            return -1;

        for (int i = erosionItems.Length - 1; i >= 0; i--)
        {
            if (erosionItems[i] != null && erosionItems[i].target != null)
                return i;
        }

        return -1;
    }

    private void RefreshNavigationButtons()
    {
        if (!disableButtonsAtEnds || wrapSelection)
            return;

        int lastIndex = GetLastIndex();
        if (lastIndex < 0)
            return;

        if (prevButton != null)
            prevButton.interactable = currentIndex < lastIndex;

        if (nextButton != null)
            nextButton.interactable = currentIndex > 0;
    }

    private void BlockInputForCooldown()
    {
        nextInputAllowedTime = GetTime() + Mathf.Max(0f, inputCooldown);
    }

    private float GetTime()
    {
        return useUnscaledTime ? Time.unscaledTime : Time.time;
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }
}
