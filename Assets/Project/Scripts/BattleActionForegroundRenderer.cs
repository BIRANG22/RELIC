using System.Collections.Generic;
using Relic.Gameplay.Monster;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 전투 실행 중 현재 행동 유닛과 효과 대상 유닛을 BattleEffect Plane보다 앞으로 표시합니다.
/// 중첩 행동(반격 등)이 끝나면 직전 행동의 전경 상태로 되돌아갑니다.
/// </summary>
public sealed class BattleActionForegroundRenderer : MonoBehaviour
{
    private struct SortingState
    {
        public bool Enabled;
        public int SortingLayerId;
        public int SortingOrder;
    }

    private static readonly Stack<List<BattleActionForegroundRenderer>> focusFrames = new();

    private readonly Stack<SortingState> stateStack = new();
    private SortingGroup sortingGroup;

    public static void Show(Transform actor, IEnumerable<Transform> targets = null)
    {
        List<BattleActionForegroundRenderer> frame = new();
        focusFrames.Push(frame);

        if (!TryResolveForegroundSorting(out int sortingLayerId, out int sortingOrder))
            return;

        HashSet<GameObject> roots = new();
        AddRoot(actor, roots);

        if (targets != null)
        {
            foreach (Transform target in targets)
                AddRoot(target, roots);
        }

        foreach (GameObject root in roots)
        {
            if (root == null)
                continue;

            BattleActionForegroundRenderer entry = root.GetComponent<BattleActionForegroundRenderer>();
            if (entry == null)
                entry = root.AddComponent<BattleActionForegroundRenderer>();

            entry.PushForeground(sortingLayerId, sortingOrder);
            frame.Add(entry);
        }
    }

    public static void ReplaceCurrent(Transform actor, IEnumerable<Transform> targets = null)
    {
        if (focusFrames.Count > 0)
            Clear();

        Show(actor, targets);
    }

    public static void Clear()
    {
        if (focusFrames.Count <= 0)
            return;

        List<BattleActionForegroundRenderer> frame = focusFrames.Pop();

        for (int i = frame.Count - 1; i >= 0; i--)
        {
            BattleActionForegroundRenderer entry = frame[i];
            if (entry != null)
                entry.PopForeground();
        }
    }

    public static void ClearAll()
    {
        while (focusFrames.Count > 0)
            Clear();
    }

    private static void AddRoot(Transform source, HashSet<GameObject> roots)
    {
        if (source == null)
            return;

        BattleCharacter character = source.GetComponentInParent<BattleCharacter>();
        if (character != null)
        {
            roots.Add(character.gameObject);
            return;
        }

        MonsterUnit monster = source.GetComponentInParent<MonsterUnit>();
        if (monster != null)
        {
            roots.Add(monster.gameObject);
            return;
        }

        roots.Add(source.gameObject);
    }

    private static bool TryResolveForegroundSorting(out int sortingLayerId, out int sortingOrder)
    {
        BattleEffectPlaneSlideController controller = BattleEffectPlaneSlideController.Instance;

        if (controller == null)
        {
            controller = Object.FindFirstObjectByType<BattleEffectPlaneSlideController>(
                FindObjectsInactive.Include);
        }

        if (controller != null &&
            controller.TryGetForegroundSorting(out sortingLayerId, out sortingOrder))
        {
            return true;
        }

        sortingLayerId = SortingLayer.NameToID("Default");
        sortingOrder = 30000;
        return true;
    }

    private void PushForeground(int sortingLayerId, int sortingOrder)
    {
        if (sortingGroup == null)
        {
            sortingGroup = GetComponent<SortingGroup>();
            if (sortingGroup == null)
            {
                sortingGroup = gameObject.AddComponent<SortingGroup>();
                sortingGroup.enabled = false;
            }
        }

        stateStack.Push(new SortingState
        {
            Enabled = sortingGroup.enabled,
            SortingLayerId = sortingGroup.sortingLayerID,
            SortingOrder = sortingGroup.sortingOrder
        });

        sortingGroup.enabled = true;
        sortingGroup.sortingLayerID = sortingLayerId;
        sortingGroup.sortingOrder = sortingOrder;
    }

    private void PopForeground()
    {
        if (sortingGroup == null || stateStack.Count <= 0)
            return;

        SortingState state = stateStack.Pop();
        sortingGroup.sortingLayerID = state.SortingLayerId;
        sortingGroup.sortingOrder = state.SortingOrder;
        sortingGroup.enabled = state.Enabled;
    }

    private void OnDisable()
    {
        while (stateStack.Count > 0)
            PopForeground();
    }
}
