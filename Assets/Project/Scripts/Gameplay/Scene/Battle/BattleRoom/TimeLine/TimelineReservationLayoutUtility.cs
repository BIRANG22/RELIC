using System.Collections.Generic;

public static class TimelineReservationLayoutUtility
{
    public sealed class InsertionPreviewLayout
    {
        public int PlaceholderStart { get; }
        public int[] ItemStarts { get; }

        public InsertionPreviewLayout(int placeholderStart, int[] itemStarts)
        {
            PlaceholderStart = placeholderStart;
            ItemStarts = itemStarts;
        }
    }

    public static InsertionPreviewLayout BuildInsertionPreview(
        IReadOnlyList<int> occupiedOrders,
        int movingItemIndex,
        int insertionIndex,
        int movingOccupiedOrders,
        int maximumOrders)
    {
        if (occupiedOrders == null || maximumOrders <= 0)
            return null;

        int safeMovingOrders = UnityEngine.Mathf.Max(1, movingOccupiedOrders);
        List<int> remainingIndices = new(occupiedOrders.Count);
        int total = safeMovingOrders;
        for (int i = 0; i < occupiedOrders.Count; i++)
        {
            if (i == movingItemIndex)
                continue;

            remainingIndices.Add(i);
            total += UnityEngine.Mathf.Max(1, occupiedOrders[i]);
        }

        if (total > maximumOrders)
            return null;

        int normalizedInsertion = UnityEngine.Mathf.Clamp(
            insertionIndex,
            0,
            remainingIndices.Count);
        int[] itemStarts = new int[occupiedOrders.Count];
        for (int i = 0; i < itemStarts.Length; i++)
            itemStarts[i] = -1;

        int cursor = 0;
        int placeholderStart = -1;
        for (int position = 0; position <= remainingIndices.Count; position++)
        {
            if (position == normalizedInsertion)
            {
                placeholderStart = cursor;
                cursor += safeMovingOrders;
            }

            if (position >= remainingIndices.Count)
                continue;

            int sourceIndex = remainingIndices[position];
            itemStarts[sourceIndex] = cursor;
            cursor += UnityEngine.Mathf.Max(1, occupiedOrders[sourceIndex]);
        }

        return new InsertionPreviewLayout(placeholderStart, itemStarts);
    }

    public static int FindNearestInsertion(float pointerPosition, IReadOnlyList<float> actionCenters)
    {
        if (actionCenters == null || actionCenters.Count == 0)
            return 0;

        for (int i = 0; i < actionCenters.Count - 1; i++)
        {
            float midpoint = (actionCenters[i] + actionCenters[i + 1]) * 0.5f;
            if (pointerPosition < midpoint)
                return i;
        }

        float lastGap = actionCenters.Count > 1
            ? actionCenters[actionCenters.Count - 1] - actionCenters[actionCenters.Count - 2]
            : 1f;
        float lastBoundary = actionCenters[actionCenters.Count - 1] + lastGap * 0.5f;
        return pointerPosition < lastBoundary ? actionCenters.Count - 1 : actionCenters.Count;
    }

    public static List<T> BuildReorderedList<T>(
        IReadOnlyList<T> source,
        T movingItem,
        int insertionIndex)
        where T : class
    {
        if (source == null || movingItem == null)
            return null;

        int sourceIndex = -1;
        for (int i = 0; i < source.Count; i++)
        {
            if (ReferenceEquals(source[i], movingItem))
            {
                sourceIndex = i;
                break;
            }
        }

        if (sourceIndex < 0)
            return null;

        List<T> result = new(source.Count);
        for (int i = 0; i < source.Count; i++)
        {
            if (i != sourceIndex)
                result.Add(source[i]);
        }

        int normalizedIndex = insertionIndex;
        if (sourceIndex < normalizedIndex)
            normalizedIndex--;

        normalizedIndex = UnityEngine.Mathf.Clamp(normalizedIndex, 0, result.Count);
        result.Insert(normalizedIndex, movingItem);
        return result;
    }
}
