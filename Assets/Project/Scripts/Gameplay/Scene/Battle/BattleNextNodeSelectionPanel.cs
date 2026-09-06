using System;
using System.Collections;
using System.Collections.Generic;
using Relic.Gameplay.Data;
using UnityEngine;
using UnityEngine.UI;

public class BattleNextNodeSelectionPanel : MonoBehaviour
{
    [Serializable]
    private class ChoiceLayoutSet
    {
        [Tooltip("해당 선택지 개수에서 NextNodeChoice_1, 2, 3 순서로 적용할 UI 위치입니다.")]
        public Vector2[] anchoredPositions = Array.Empty<Vector2>();

        [Tooltip("해당 선택지 개수에서 NextNodeChoice_1, 2, 3 순서로 적용할 로컬 스케일입니다.")]
        public Vector3[] localScales = Array.Empty<Vector3>();
    }

    [SerializeField] private BattleNextNodeChoiceButton[] choices;
    [SerializeField] private BattleNextNodeChoiceButton choicePrefab;
    [SerializeField, Min(1)] private int maxChoiceCount = 3;

    [Header("Choice Layout - 1개 표시")]
    [SerializeField] private ChoiceLayoutSet oneChoiceLayout = new()
    {
        anchoredPositions = new[]
        {
            Vector2.zero
        },
        localScales = new[]
        {
            Vector3.one
        }
    };

    [Header("Choice Layout - 2개 표시")]
    [SerializeField] private ChoiceLayoutSet twoChoiceLayout = new()
    {
        anchoredPositions = new[]
        {
            Vector2.zero,
            Vector2.zero
        },
        localScales = new[]
        {
            Vector3.one,
            Vector3.one
        }
    };

    [Header("Choice Layout - 3개 표시")]
    [SerializeField] private ChoiceLayoutSet threeChoiceLayout = new()
    {
        anchoredPositions = new[]
        {
            Vector2.zero,
            Vector2.zero,
            Vector2.zero
        },
        localScales = new[]
        {
            Vector3.one,
            Vector3.one,
            Vector3.one
        }
    };

    [Header("Choice Hover Scale")]
    [Tooltip("생성된 NextNodeChoice에 마우스를 올렸을 때 기본 스케일에 곱해지는 값입니다. 예: 1.1은 10% 확대됩니다.")]
    [SerializeField] private Vector3 hoverScaleMultiplier = new(1.1f, 1.1f, 1.1f);

    private readonly List<int> visibleNodeIndices = new();
    private Coroutine layoutApplyRoutine;

    public IReadOnlyList<int> VisibleNodeIndices => visibleNodeIndices;

    private void Awake()
    {
        ResolveChoices();
    }

    private void OnDisable()
    {
        if (layoutApplyRoutine != null)
        {
            StopCoroutine(layoutApplyRoutine);
            layoutApplyRoutine = null;
        }
    }

    public void Open(MapRuntimeData runtime, Action<int> onSelected)
    {
        ResolveChoices();
        visibleNodeIndices.Clear();

        int capacity = Mathf.Min(maxChoiceCount, choices.Length);
        List<GeneratedMapNodeData> nodes =
            MapRuntimeProgressUtility.CollectSelectableNextNodes(runtime, capacity);

        // 지도에서 실제로 보이는 순서와 NextNodeChoice의 순서를 일치시킵니다.
        // Position.y가 큰 노드가 화면 위쪽에 있으므로 위 -> 아래 순서로 정렬합니다.
        nodes.Sort(CompareNodeTopToBottom);

        int visibleChoiceCount = Mathf.Min(nodes.Count, choices.Length);

        for (int i = 0; i < choices.Length; i++)
        {
            BattleNextNodeChoiceButton choice = choices[i];
            if (choice == null)
                continue;

            if (i < nodes.Count)
            {
                GeneratedMapNodeData node = nodes[i];
                choice.Bind(node, onSelected);
                visibleNodeIndices.Add(node.NodeIndex);
            }
            else
            {
                choice.Clear();
            }
        }

        // 비활성 상태에서 Transform을 먼저 설정하면 패널 활성화 직후
        // Unity UI 레이아웃 갱신이 위치를 다시 덮어쓸 수 있으므로 먼저 패널을 엽니다.
        gameObject.SetActive(true);

        ApplyLayoutImmediately(visibleChoiceCount);

        if (layoutApplyRoutine != null)
            StopCoroutine(layoutApplyRoutine);

        layoutApplyRoutine = StartCoroutine(ReapplyLayoutAfterUiUpdate(visibleChoiceCount));
    }

    public void Close()
    {
        if (layoutApplyRoutine != null)
        {
            StopCoroutine(layoutApplyRoutine);
            layoutApplyRoutine = null;
        }

        gameObject.SetActive(false);
    }

    private void ResolveChoices()
    {
        List<BattleNextNodeChoiceButton> resolved = new(
            GetComponentsInChildren<BattleNextNodeChoiceButton>(true));

        if (choicePrefab != null)
        {
            while (resolved.Count < maxChoiceCount)
            {
                BattleNextNodeChoiceButton choice = Instantiate(choicePrefab, transform);
                choice.name = $"NextNodeChoice_{resolved.Count + 1}";
                resolved.Add(choice);
            }
        }

        // 이름에 붙은 번호를 우선 사용해서 Inspector의 1/2/3 설정과
        // 실제 NextNodeChoice_1/2/3이 항상 같은 순서로 대응되도록 합니다.
        resolved.Sort(CompareChoiceOrder);

        for (int i = 0; i < resolved.Count; i++)
            EnsureManualLayout(resolved[i]);

        choices = resolved.ToArray();
    }

    private void ApplyLayoutImmediately(int visibleChoiceCount)
    {
        if (visibleChoiceCount <= 0)
            return;

        // 현재 프레임의 레이아웃 계산을 먼저 끝낸 뒤 Inspector 값을 최종 적용합니다.
        Canvas.ForceUpdateCanvases();

        RectTransform panelRect = transform as RectTransform;
        if (panelRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);

        ApplyChoiceTransforms(visibleChoiceCount);
    }

    private IEnumerator ReapplyLayoutAfterUiUpdate(int visibleChoiceCount)
    {
        // SetActive/Bind 이후 같은 프레임의 LayoutGroup 계산이 끝난 뒤 다시 적용합니다.
        yield return null;

        if (!gameObject.activeInHierarchy)
        {
            layoutApplyRoutine = null;
            yield break;
        }

        ApplyLayoutImmediately(visibleChoiceCount);

        // Canvas의 실제 렌더 직전 단계에서도 한 번 더 보장합니다.
        yield return new WaitForEndOfFrame();

        if (gameObject.activeInHierarchy)
            ApplyChoiceTransforms(visibleChoiceCount);

        layoutApplyRoutine = null;
    }

    private void ApplyChoiceTransforms(int visibleChoiceCount)
    {
        if (choices == null || choices.Length == 0)
            return;

        ChoiceLayoutSet layoutSet = GetLayoutSet(visibleChoiceCount);
        if (layoutSet == null)
            return;

        int count = Mathf.Min(visibleChoiceCount, choices.Length);

        for (int i = 0; i < count; i++)
        {
            BattleNextNodeChoiceButton choice = choices[i];
            if (choice == null)
                continue;

            EnsureManualLayout(choice);

            RectTransform rectTransform = choice.transform as RectTransform;

            if (layoutSet.anchoredPositions != null &&
                i < layoutSet.anchoredPositions.Length)
            {
                Vector2 targetPosition = layoutSet.anchoredPositions[i];

                if (rectTransform != null)
                {
                    // LayoutGroup의 Driven 값 영향을 받지 않도록 위치를 직접 확정합니다.
                    rectTransform.anchoredPosition = targetPosition;
                }
                else
                {
                    Vector3 localPosition = choice.transform.localPosition;
                    localPosition.x = targetPosition.x;
                    localPosition.y = targetPosition.y;
                    choice.transform.localPosition = localPosition;
                }
            }

            Vector3 baseScale = Vector3.one;

            if (layoutSet.localScales != null && i < layoutSet.localScales.Length)
                baseScale = layoutSet.localScales[i];

            BattleNextNodeChoiceHoverScale hoverScale =
                choice.GetComponent<BattleNextNodeChoiceHoverScale>();

            if (hoverScale == null)
                hoverScale = choice.gameObject.AddComponent<BattleNextNodeChoiceHoverScale>();

            hoverScale.Configure(baseScale, hoverScaleMultiplier);
        }
    }

    private ChoiceLayoutSet GetLayoutSet(int visibleChoiceCount)
    {
        return visibleChoiceCount switch
        {
            1 => oneChoiceLayout,
            2 => twoChoiceLayout,
            3 => threeChoiceLayout,
            _ => threeChoiceLayout
        };
    }

    private static void EnsureManualLayout(BattleNextNodeChoiceButton choice)
    {
        if (choice == null)
            return;

        // VerticalLayoutGroup 등이 Choice의 위치를 자동 제어하지 않도록 제외합니다.
        LayoutElement layoutElement = choice.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = choice.gameObject.AddComponent<LayoutElement>();

        layoutElement.ignoreLayout = true;
    }

    private static int CompareNodeTopToBottom(
        GeneratedMapNodeData a,
        GeneratedMapNodeData b)
    {
        if (ReferenceEquals(a, b))
            return 0;

        if (a == null)
            return 1;

        if (b == null)
            return -1;

        int yCompare = b.Position.y.CompareTo(a.Position.y);
        if (yCompare != 0)
            return yCompare;

        // 같은 높이에 있는 경우에도 실행마다 순서가 흔들리지 않도록 고정합니다.
        return a.NodeIndex.CompareTo(b.NodeIndex);
    }

    private static int CompareChoiceOrder(
        BattleNextNodeChoiceButton a,
        BattleNextNodeChoiceButton b)
    {
        if (ReferenceEquals(a, b))
            return 0;

        if (a == null)
            return 1;

        if (b == null)
            return -1;

        int aNumber = GetChoiceNumber(a.name);
        int bNumber = GetChoiceNumber(b.name);

        if (aNumber != bNumber)
            return aNumber.CompareTo(bNumber);

        return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
    }

    private static int GetChoiceNumber(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return int.MaxValue;

        const string prefix = "NextNodeChoice_";
        int prefixIndex = objectName.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (prefixIndex < 0)
            return int.MaxValue;

        string numberText = objectName.Substring(prefixIndex + prefix.Length);
        return int.TryParse(numberText, out int number)
            ? number
            : int.MaxValue;
    }
}
