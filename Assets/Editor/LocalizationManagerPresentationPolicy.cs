using UnityEngine;

public static class LocalizationManagerPresentationPolicy
{
    public const int PageSize = 50;

    public static void GetPageBounds(int totalCount, int requestedPage, out int start, out int end)
    {
        int safeTotal = Mathf.Max(0, totalCount);
        int lastPage = safeTotal > 0 ? (safeTotal - 1) / PageSize : 0;
        int page = Mathf.Clamp(requestedPage, 0, lastPage);
        start = page * PageSize;
        end = Mathf.Min(safeTotal, start + PageSize);
    }
}
