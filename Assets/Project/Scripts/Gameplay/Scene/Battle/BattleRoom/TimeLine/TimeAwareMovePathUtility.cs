using System.Collections.Generic;
using UnityEngine;

public readonly struct TimedMoveTransition
{
    public int Time { get; }
    public Vector2Int From { get; }
    public Vector2Int To { get; }

    public TimedMoveTransition(int time, Vector2Int from, Vector2Int to)
    {
        Time = time;
        From = from;
        To = to;
    }
}

public static class TimeAwareMovePathUtility
{
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.right,
        Vector2Int.left,
        Vector2Int.up,
        Vector2Int.down
    };

    private readonly struct State
    {
        public Vector2Int Coord { get; }
        public int Time { get; }

        public State(Vector2Int coord, int time)
        {
            Coord = coord;
            Time = time;
        }
    }

    public static List<Vector2Int> FindShortestPath(
        Vector2Int start,
        Vector2Int target,
        int width,
        int height,
        int maxSteps,
        ISet<Vector2Int> staticBlocked,
        IReadOnlyList<TimedMoveTransition> timedTransitions)
    {
        if (!IsValid(start, width, height) ||
            !IsValid(target, width, height) ||
            maxSteps < 0)
        {
            return null;
        }

        if (start == target)
            return new List<Vector2Int>();

        Queue<State> open = new();
        HashSet<(Vector2Int coord, int time)> visited = new();
        Dictionary<(Vector2Int coord, int time), (Vector2Int coord, int time)> parents = new();
        Dictionary<(Vector2Int coord, int time), Vector2Int> steps = new();
        State initial = new(start, 0);

        open.Enqueue(initial);
        visited.Add((start, 0));

        while (open.Count > 0)
        {
            State current = open.Dequeue();

            if (current.Time >= maxSteps)
                continue;

            for (int i = 0; i < Directions.Length; i++)
            {
                Vector2Int step = Directions[i];
                Vector2Int next = current.Coord + step;
                int nextTime = current.Time + 1;

                if (!IsValid(next, width, height) ||
                    (staticBlocked != null && staticBlocked.Contains(next)) ||
                    HasTimedCollision(current.Coord, next, current.Time, timedTransitions))
                {
                    continue;
                }

                (Vector2Int coord, int time) key = (next, nextTime);

                if (!visited.Add(key))
                    continue;

                parents[key] = (current.Coord, current.Time);
                steps[key] = step;

                if (next == target)
                    return Reconstruct(key, parents, steps);

                open.Enqueue(new State(next, nextTime));
            }
        }

        return null;
    }

    private static bool HasTimedCollision(
        Vector2Int from,
        Vector2Int to,
        int time,
        IReadOnlyList<TimedMoveTransition> transitions)
    {
        if (transitions == null)
            return false;

        for (int i = 0; i < transitions.Count; i++)
        {
            TimedMoveTransition transition = transitions[i];

            if (transition.Time != time)
                continue;

            if (to == transition.To)
                return true;

            if (from == transition.To && to == transition.From)
                return true;
        }

        return false;
    }

    private static List<Vector2Int> Reconstruct(
        (Vector2Int coord, int time) end,
        IReadOnlyDictionary<(Vector2Int coord, int time), (Vector2Int coord, int time)> parents,
        IReadOnlyDictionary<(Vector2Int coord, int time), Vector2Int> steps)
    {
        List<Vector2Int> result = new();
        (Vector2Int coord, int time) cursor = end;

        while (cursor.time > 0)
        {
            result.Add(steps[cursor]);
            cursor = parents[cursor];
        }

        result.Reverse();
        return result;
    }

    private static bool IsValid(Vector2Int coord, int width, int height)
    {
        return coord.x >= 0 && coord.x < width && coord.y >= 0 && coord.y < height;
    }
}
