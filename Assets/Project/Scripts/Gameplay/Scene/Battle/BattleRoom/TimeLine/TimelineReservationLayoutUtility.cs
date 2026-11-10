using System.Collections.Generic;

public static class TimelineReservationLayoutUtility
{
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
